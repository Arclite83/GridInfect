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
    // wrapper (AdCadenceConfig ScriptableObject) hands it over. Scalars and
    // flags only, deliberately: the moment a change needs new logic it needs
    // a build, and config that carries logic is a second codebase with no
    // tests. AdCadenceGate clamps every value on read.
    [Serializable]
    public sealed class AdCadence
    {
        // Kill switches, per placement.
        public bool InterstitialEnabled = true;
        public bool RewardedEnabled = true;

        // Lifetime, not per session. The old session-scoped counter never
        // tripped for a Daily-only player: one solve, then the app closes and
        // the count starts over, so they never saw an ad at all.
        public int GraceLifetimeSolves = 8;

        // Both gates are required. See AdCadenceGate for why either alone
        // fails at one end of the difficulty ramp.
        public int MinSolvesBetweenAds = 3;
        public float MinSecondsBetweenAds = 240f;

        public int MaxAdsPerDay = 8;
        public bool RewardedResetsGates = true;
    }

    // R-604: demo unit ids in development builds; production ids live only
    // in the release config asset, never in code.
    // Android ids and the iOS interstitial are the ones recorded in
    // DEPENDENCIES §5 step 4. The iOS rewarded id is NOT in that list and is
    // unverified — check it against Google's sample-unit page before the iOS
    // follow ships. Nothing on the Android launch path reads it.
    public static class DemoAdUnits
    {
        public const string AndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string AndroidRewarded = "ca-app-pub-3940256099942544/5224354917";
        public const string IosInterstitial = "ca-app-pub-3940256099942544/4411468910";
        public const string IosRewarded = "ca-app-pub-3940256099942544/1712485313";
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

    // The one place an implementation is chosen, and the only thing the
    // SDK import has to change — which is why it is a define and not an edit.
    // GRIDINFECT_ADMOB is a scripting define symbol you add by hand after
    // importing the plugin (it is a .unitypackage, so nothing can detect it);
    // GRIDINFECT_IAP is a versionDefine on the Services asmdef and appears on
    // its own when com.unity.purchasing is in the manifest.
    //
    // Without either symbol the Null services keep every build playable, which
    // is R-801's rule in its strongest form: no ads, no consent, no store, and
    // the game still runs.
    public static class Bootstrap
    {
#if GRIDINFECT_ADMOB
        public static IConsentService Consent() => new UmpConsentService();
        public static IAdService Ads() => new AdMobAdService(AdConfig.Load());
#else
        public static IConsentService Consent() => new NullConsentService();
        public static IAdService Ads() => new NullAdService();
#endif

#if GRIDINFECT_IAP
        public static IPurchaseService Purchases() => new UnityIapPurchaseService();
#else
        public static IPurchaseService Purchases() => new NullPurchaseService();
#endif
    }
}
