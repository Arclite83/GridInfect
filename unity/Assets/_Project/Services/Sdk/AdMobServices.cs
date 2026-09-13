// The AdMob + UMP implementations of IAdService and IConsentService.
// Compiled and run on device 2026-09-13 (GMA v11.5.0).
//
// Provenance: the consent sequence (ConsentInformation.Update → ConsentForm
// .LoadAndShowConsentFormIfRequired → CanRequestAds → MobileAds.Initialize),
// ShowPrivacyOptionsForm, PrivacyOptionsRequirementStatus and
// RequestConfiguration.TestDeviceIds come from DEPENDENCIES §5 step 3–4. The
// ad-loading calls (InterstitialAd.Load, RewardedAd.Load, the
// FullScreenContentCallback events, the Reward payload) were written from
// the plugin's own API and confirmed against its metadata as above.

#if GRIDINFECT_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace GridInfect.Services
{
    // R-801: update consent info, show the form when required, then report.
    // Gameplay never waits on any of this — a failure path still calls back.
    //
    // UMP raises its callbacks on the Android UI thread, not Unity's. The
    // plugin's RaiseAdEventsOnUnityMainThread covers ad events only, so the
    // consent ones are hopped by hand through the same executor the plugin
    // uses. Two reasons: the continuation reaches game state and, through
    // MobileAds.Initialize, Unity objects that only the main thread may
    // create; and an exception thrown while still on the Android thread
    // leaves Unity's Java proxy as an uncaught java.lang.Error and kills the
    // process, with nothing in the Unity log.
    public sealed class UmpConsentService : IConsentService
    {
        static void OnMain(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        public void Request(Action<ConsentOutcome> outcome)
        {
            // Creates the executor's GameObject; must run on the main thread,
            // and MobileAds.Initialize (which would) only comes after consent.
            MobileAdsEventExecutor.Initialize();

            var parameters = new ConsentRequestParameters();
            ConsentInformation.Update(parameters, error => OnMain(() =>
            {
                if (error != null)
                {
                    outcome?.Invoke(ConsentOutcome.Unavailable);
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMain(() =>
                {
                    if (formError != null) { outcome?.Invoke(ConsentOutcome.Unavailable); return; }
                    outcome?.Invoke(ConsentInformation.CanRequestAds()
                        ? ConsentOutcome.Obtained
                        : ConsentOutcome.Declined);
                }));
            }));
        }

        // R-802: the settings entry exists only while UMP says one is required.
        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void ShowPrivacyOptions(Action closed) =>
            ConsentForm.ShowPrivacyOptionsForm(_ => OnMain(() => closed?.Invoke()));

        public bool CanRequestAds => ConsentInformation.CanRequestAds();
    }

    public sealed class AdMobAdService : IAdService
    {
        readonly AdConfig _config;
        InterstitialAd _interstitial;
        RewardedAd _rewarded;

        public AdMobAdService(AdConfig config) => _config = config ?? AdConfig.Load();

        public void Initialize(Action ready)
        {
            // Every callback below touches game state (the solved popup, the
            // wallet). Without this the SDK raises them on its own thread.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

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
