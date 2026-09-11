// The AdMob + UMP implementations of IAdService and IConsentService.
//
// UNVERIFIED AGAINST THE REAL SDK. Nothing in this file has ever been
// compiled: the Google Mobile Ads plugin is a .unitypackage, not a registry
// package, so it cannot be restored in CI or in an authoring session. The
// GRIDINFECT_ADMOB define keeps the whole file out of every build until you
// import the plugin and add the symbol, which is deliberate — an unverified
// SDK call must not be able to reach a player.
//
// Provenance, so you know what to check and what not to bother checking:
//
//   * The consent sequence (ConsentInformation.Update → ConsentForm
//     .LoadAndShowConsentFormIfRequired → CanRequestAds → MobileAds.Initialize),
//     ShowPrivacyOptionsForm, PrivacyOptionsRequirementStatus and
//     RequestConfiguration.TestDeviceIds are all transcribed from
//     DEPENDENCIES §5 step 3–4, which was written against the official
//     quick-start. Treat those as sound.
//   * The ad-loading calls (InterstitialAd.Load, RewardedAd.Load, the
//     FullScreenContentCallback events, the Reward payload) are NOT in that
//     document. They are the part to check against IntelliSense on first
//     import, and the part most likely to need a small edit.
//
// When it compiles, delete this header down to the provenance note and say so
// in the commit. Leaving the warning on verified code is its own bug.

#if GRIDINFECT_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace GridInfect.Services
{
    // R-801: update consent info, show the form when required, then report.
    // Gameplay never waits on any of this — a failure path still calls back.
    public sealed class UmpConsentService : IConsentService
    {
        public void Request(Action<ConsentOutcome> outcome)
        {
            var parameters = new ConsentRequestParameters();
            ConsentInformation.Update(parameters, error =>
            {
                if (error != null)
                {
                    outcome?.Invoke(ConsentOutcome.Unavailable);
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) { outcome?.Invoke(ConsentOutcome.Unavailable); return; }
                    outcome?.Invoke(ConsentInformation.CanRequestAds
                        ? ConsentOutcome.Obtained
                        : ConsentOutcome.Declined);
                });
            });
        }

        // R-802: the settings entry exists only while UMP says one is required.
        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptions(Action closed) =>
            ConsentForm.ShowPrivacyOptionsForm(_ => closed?.Invoke());

        public bool CanRequestAds => ConsentInformation.CanRequestAds;
    }

    public sealed class AdMobAdService : IAdService
    {
        readonly AdConfig _config;
        InterstitialAd _interstitial;
        RewardedAd _rewarded;

        public AdMobAdService(AdConfig config) => _config = config ?? AdConfig.Load();

        public void Initialize(Action ready)
        {
            // R-604: registered devices see test ads even against production
            // unit ids. Clicking a live ad on your own device is how accounts
            // get banned, so this is not optional bookkeeping.
            if (_config.TestDeviceIds != null && _config.TestDeviceIds.Length > 0)
            {
                MobileAds.SetRequestConfiguration(new RequestConfiguration
                {
                    TestDeviceIds = new System.Collections.Generic.List<string>(_config.TestDeviceIds),
                });
            }

            MobileAds.Initialize(_ =>
            {
                LoadInterstitial();
                LoadRewarded();
                ready?.Invoke();
            });
        }

        // --- interstitial ---------------------------------------------------

        public bool InterstitialReady => _interstitial != null && _interstitial.CanShowAd();

        void LoadInterstitial()
        {
            _interstitial?.Destroy();
            _interstitial = null;
            InterstitialAd.Load(_config.Interstitial, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) return;   // no fill: AdGate simply does not show one
                _interstitial = ad;
            });
        }

        public void ShowInterstitial(Action closed)
        {
            var ad = _interstitial;
            if (ad == null || !ad.CanShowAd()) { closed?.Invoke(); return; }

            // The caller's continuation must run exactly once, whether the ad
            // closes cleanly or fails to present. A dropped callback here is a
            // soft-locked solved popup.
            bool done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                LoadInterstitial();      // the next one starts loading immediately
                closed?.Invoke();
            }

            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += _ => Finish();
            ad.Show();
        }

        // --- rewarded -------------------------------------------------------

        public bool RewardedReady => _rewarded != null && _rewarded.CanShowAd();

        void LoadRewarded()
        {
            _rewarded?.Destroy();
            _rewarded = null;
            RewardedAd.Load(_config.Rewarded, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) return;
                _rewarded = ad;
            });
        }

        public void ShowRewarded(Action<bool> rewarded)
        {
            var ad = _rewarded;
            if (ad == null || !ad.CanShowAd()) { rewarded?.Invoke(false); return; }

            // Earned is decided by the SDK's reward callback, not by the close:
            // a player who backs out early gets no Lock, and the caller must
            // still hear about it exactly once.
            bool earned = false, done = false;
            void Finish()
            {
                if (done) return;
                done = true;
                LoadRewarded();
                rewarded?.Invoke(earned);
            }

            ad.OnAdFullScreenContentClosed += Finish;
            ad.OnAdFullScreenContentFailed += _ => Finish();
            ad.Show(_ => earned = true);
        }
    }
}
#endif
