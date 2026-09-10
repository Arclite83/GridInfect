using System;
using GridInfect.Services;
using UnityEngine;

namespace GridInfect.Game
{
    // The adapter's side of the ads contract: consent first, then the SDK;
    // an interstitial on solved-popup dismissal under the cadence rules
    // unless remove-ads is owned; a rewarded placement that earns a lock.
    // The decision itself is AdCadenceGate, which has no clock and no
    // storage in it; this class is the clock, the storage and the SDK.
    // Every game-state change is still an action — nothing here is one.
    public sealed class AdGate
    {
        public readonly IConsentService Consent;
        public readonly IAdService Ads;
        public readonly IPurchaseService Purchases;
        public readonly AdCadenceGate Cadence;

        bool _adsReady;
        bool _dirty;

        public AdGate(IConsentService consent, IAdService ads, IPurchaseService purchases, AdCadence cadence)
        {
            Consent = consent;
            Ads = ads;
            Purchases = purchases;
            Cadence = new AdCadenceGate(cadence ?? new AdCadence(), AdCounterStore.Load());
            Cadence.RollDay(Today);
        }

        public static AdGate Create()
        {
            var config = Resources.Load<AdCadenceConfig>("AdCadence");
            return new AdGate(Bootstrap.Consent(), Bootstrap.Ads(), Bootstrap.Purchases(),
                config != null ? config.Cadence : null);
        }

        // The local calendar day as a plain index. The gate never reads a
        // clock itself, so this is the only place a day is decided.
        static int Today => (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays;

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

        // Foreground board time, not wall clock: an app left open in a pocket
        // must not accrue credit toward an ad. GameApp passes false whenever
        // the player is not on a board.
        public void Tick(float dt, bool onBoard)
        {
            if (!onBoard || dt <= 0f) return;
            Cadence.AddPlaySeconds(dt);
            _dirty = true;
        }

        // A solve outside the tutorial. BoardScreen decides which those are.
        public void CountSolve()
        {
            Cadence.CountSolve();
            _dirty = true;
        }

        // R-602 + R-701: on dismissing the solved popup.
        public bool MaybeShowInterstitial(Action closed)
        {
            if (!_adsReady || Purchases.RemoveAdsOwned || !Ads.InterstitialReady) return false;
            if (!Cadence.ShouldShow(Today)) return false;
            Cadence.NoteShown(Today);
            Flush();
            Ads.ShowInterstitial(closed);
            return true;
        }

        public bool RewardedAvailable => _adsReady && Cadence.RewardedEnabled && Ads.RewardedReady;

        // The rewarded placement: the caller dispatches locks.grant on true.
        // Watching one resets the interstitial gates either way — the offer
        // was taken, and charging for the same board twice reads as a bait.
        public void ShowRewarded(Action<bool> rewarded)
        {
            Ads.ShowRewarded(earned =>
            {
                Cadence.NoteRewarded();
                Flush();
                rewarded?.Invoke(earned);
            });
        }

        // Called on pause and quit; the per-frame tick only marks dirty, so
        // the accrued seconds cost one write per app suspension, not per frame.
        public void Flush()
        {
            if (!_dirty) return;
            AdCounterStore.Save(Cadence.Counters);
            _dirty = false;
        }
    }
}
