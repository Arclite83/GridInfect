using System;
using GridInfect.Services;
using UnityEngine;

namespace GridInfect.Game
{
    // The adapter's side of the ads contract: consent first, then the SDK;
    // an interstitial on solved-popup dismissal under the cadence rules
    // unless remove-ads is owned; a rewarded placement that earns a lock.
    // Pure timing and counting here; every state change is an action.
    public sealed class AdGate
    {
        public readonly IConsentService Consent;
        public readonly IAdService Ads;
        public readonly IPurchaseService Purchases;
        public readonly AdCadence Cadence;

        int _solvesThisSession;
        float _lastAdAt = float.NegativeInfinity;
        bool _adsReady;

        public AdGate(IConsentService consent, IAdService ads, IPurchaseService purchases, AdCadence cadence)
        {
            Consent = consent;
            Ads = ads;
            Purchases = purchases;
            Cadence = cadence ?? new AdCadence();
        }

        public static AdGate Create()
        {
            var config = Resources.Load<AdCadenceConfig>("AdCadence");
            return new AdGate(Bootstrap.Consent(), Bootstrap.Ads(), Bootstrap.Purchases(), config != null ? config.Cadence : null);
        }

        // R-801/R-601: the consent flow gates SDK initialization; gameplay
        // never waits on either.
        public void Start()
        {
            Purchases.Initialize(null);
            Consent.Request(_ =>
            {
                if (Consent.CanRequestAds) Ads.Initialize(() => _adsReady = true);
            });
        }

        public bool PrivacyOptionsAvailable => Consent.PrivacyOptionsRequired;

        public void ShowPrivacyOptions(Action closed) => Consent.ShowPrivacyOptions(closed);

        public void CountSolve() => _solvesThisSession++;

        // R-602 + R-701: on dismissing the solved popup.
        public bool MaybeShowInterstitial(Action closed)
        {
            if (!_adsReady || Purchases.RemoveAdsOwned || !Ads.InterstitialReady) return false;
            if (_solvesThisSession < Cadence.MinSolvesBeforeFirstAd) return false;
            if (Time.unscaledTime - _lastAdAt < Cadence.MinSecondsBetweenAds) return false;
            _lastAdAt = Time.unscaledTime;
            Ads.ShowInterstitial(closed);
            return true;
        }

        public bool RewardedAvailable => _adsReady && Ads.RewardedReady;

        // The rewarded placement: the caller dispatches locks.grant on true.
        public void ShowRewarded(Action<bool> rewarded) => Ads.ShowRewarded(rewarded);
    }
}
