using GridInfect.Services;
using UnityEngine;

namespace GridInfect.Game
{
    // Five scalars of device-local ad bookkeeping. PlayerPrefs rather than a
    // SavePort-style file on purpose: losing these is harmless (the player
    // gets one fewer ad), so the versioning, migration and IO the save model
    // carries would all be spent on nothing. They stay out of Profile and out
    // of the action log — see AdCounters.
    public static class AdCounterStore
    {
        const string KLifetime = "ad.lifetimeSolves";
        const string KSolves = "ad.solvesSinceAd";
        const string KSeconds = "ad.playSecondsSinceAd";
        const string KToday = "ad.adsToday";
        const string KStamp = "ad.dayStamp";

        public static AdCounters Load() => new AdCounters
        {
            LifetimeSolves = PlayerPrefs.GetInt(KLifetime, 0),
            SolvesSinceAd = PlayerPrefs.GetInt(KSolves, 0),
            PlaySecondsSinceAd = PlayerPrefs.GetFloat(KSeconds, 0f),
            AdsToday = PlayerPrefs.GetInt(KToday, 0),
            DayStamp = PlayerPrefs.GetInt(KStamp, 0),
        };

        public static void Save(AdCounters c)
        {
            if (c == null) return;
            PlayerPrefs.SetInt(KLifetime, c.LifetimeSolves);
            PlayerPrefs.SetInt(KSolves, c.SolvesSinceAd);
            PlayerPrefs.SetFloat(KSeconds, c.PlaySecondsSinceAd);
            PlayerPrefs.SetInt(KToday, c.AdsToday);
            PlayerPrefs.SetInt(KStamp, c.DayStamp);
            PlayerPrefs.Save();
        }
    }
}
