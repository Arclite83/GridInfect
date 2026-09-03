using System;

namespace GridInfect.Services
{
    // The SDK boundary (ARCHITECTURE §1, R-1303): the Game assembly talks to
    // these interfaces and to nothing from an ads, consent or purchasing
    // SDK; Core never references this assembly at all. The SDK-backed
    // implementations (Google Mobile Ads + UMP, Unity IAP; DEPENDENCIES
    // §5) live beside these files once the packages are imported and are
    // selected in Bootstrap; until then the Null services keep every
    // build playable (R-801: blocking ads never blocks gameplay).

    public enum ConsentOutcome
    {
        NotRequired,   // outside the EEA/UK, or already answered
        Obtained,
        Declined,
        Unavailable,   // no network, SDK error: play on, no ads
    }

    public interface IConsentService
    {
        // R-801: update consent info, show the form when required, then report.
        void Request(Action<ConsentOutcome> outcome);
        // R-802: a privacy options entry exists whenever the SDK says one is required.
        bool PrivacyOptionsRequired { get; }
        void ShowPrivacyOptions(Action closed);
        bool CanRequestAds { get; }
    }

    public interface IAdService
    {
        // R-601: initialize only after consent allows ad requests.
        void Initialize(Action ready);
        bool InterstitialReady { get; }
        // R-602: shown on solved-popup dismissal, under AdCadence.
        void ShowInterstitial(Action closed);
        bool RewardedReady { get; }
        // The rewarded placement earns one lock (NEXT_PASS decision 8).
        void ShowRewarded(Action<bool> rewarded);
    }

    public interface IPurchaseService
    {
        void Initialize(Action ready);
        // R-701: the single non-consumable; owning it suppresses interstitials only.
        bool RemoveAdsOwned { get; }
        void BuyRemoveAds(Action<bool> owned);
        // R-702: restore flow.
        void Restore(Action<bool> owned);
    }

    // R-602 cadence, designer-editable. A plain class here; the Unity asset
    // wrapper (AdCadenceConfig ScriptableObject) hands it over.
    [Serializable]
    public sealed class AdCadence
    {
        public int MinSolvesBeforeFirstAd = 3;
        public float MinSecondsBetweenAds = 90f;
    }

    // R-604: demo unit ids in development builds; production ids live only
    // in the release config asset, never in code.
    public static class DemoAdUnits
    {
        public const string AndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string AndroidRewarded = "ca-app-pub-3940256099942544/5224354917";
    }

    public sealed class NullConsentService : IConsentService
    {
        public void Request(Action<ConsentOutcome> outcome) => outcome?.Invoke(ConsentOutcome.NotRequired);
        public bool PrivacyOptionsRequired => false;
        public void ShowPrivacyOptions(Action closed) => closed?.Invoke();
        public bool CanRequestAds => false;
    }

    public sealed class NullAdService : IAdService
    {
        public void Initialize(Action ready) => ready?.Invoke();
        public bool InterstitialReady => false;
        public void ShowInterstitial(Action closed) => closed?.Invoke();
        public bool RewardedReady => false;
        public void ShowRewarded(Action<bool> rewarded) => rewarded?.Invoke(false);
    }

    public sealed class NullPurchaseService : IPurchaseService
    {
        public void Initialize(Action ready) => ready?.Invoke();
        public bool RemoveAdsOwned => false;
        public void BuyRemoveAds(Action<bool> owned) => owned?.Invoke(false);
        public void Restore(Action<bool> owned) => owned?.Invoke(false);
    }

    // The one place an implementation is chosen. Swap the Null services for
    // the SDK-backed ones when the packages are imported (stage 6 follow-up).
    public static class Bootstrap
    {
        public static IConsentService Consent() => new NullConsentService();
        public static IAdService Ads() => new NullAdService();
        public static IPurchaseService Purchases() => new NullPurchaseService();
    }
}
