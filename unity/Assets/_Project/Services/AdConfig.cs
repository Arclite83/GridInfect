using UnityEngine;

namespace GridInfect.Services
{
    // DEPENDENCIES §5 step 5: the one asset holding per-platform ad unit ids
    // and the test-ads flag. The AdMob *app* id is not here — it lives only in
    // GoogleMobileAdsSettings.asset, which the plugin injects into the Android
    // manifest and Info.plist at build (step 2).
    //
    // The repo default is demo units with UseTestAds on (R-604). Production
    // ids are typed into this asset for a release build and never committed:
    // clicking your own live ads is the fastest way to lose an AdMob account,
    // and a committed live id is one careless run away from that.
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Grid Infect/Ad Config")]
    public sealed class AdConfig : ScriptableObject
    {
        [Tooltip("Demo unit ids and Google's test-device behaviour. Leave on for every build that is not a store release.")]
        public bool UseTestAds = true;

        [Header("Android")]
        public string AndroidInterstitial = DemoAdUnits.AndroidInterstitial;
        public string AndroidRewarded = DemoAdUnits.AndroidRewarded;

        [Header("iOS (R-803, ships with the iOS follow)")]
        public string IosInterstitial = DemoAdUnits.IosInterstitial;
        public string IosRewarded = DemoAdUnits.IosRewarded;

        [Header("Test devices")]
        [Tooltip("Device ids logged by the SDK on first run. Required so your own device sees test ads against production unit ids.")]
        public string[] TestDeviceIds = new string[0];

        public string Interstitial => Resolve(
            UseTestAds ? DemoAdUnits.AndroidInterstitial : AndroidInterstitial,
            UseTestAds ? DemoAdUnits.IosInterstitial : IosInterstitial);

        public string Rewarded => Resolve(
            UseTestAds ? DemoAdUnits.AndroidRewarded : AndroidRewarded,
            UseTestAds ? DemoAdUnits.IosRewarded : IosRewarded);

        static string Resolve(string android, string ios)
        {
#if UNITY_IOS && !UNITY_EDITOR
            return ios;
#else
            return android;
#endif
        }

        public static AdConfig Load()
        {
            var config = Resources.Load<AdConfig>("AdConfig");
            return config != null ? config : CreateInstance<AdConfig>();
        }
    }
}
