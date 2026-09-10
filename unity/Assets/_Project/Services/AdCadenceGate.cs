using System;

namespace GridInfect.Services
{
    // The interstitial decision, with no clock and no storage in it: the
    // adapter supplies both. Pure C# so the whole cadence is testable
    // headless in the mirror solution, which is where a cadence bug has to
    // be caught — in front of a player it is invisible until it is a review.
    //
    // The rule (2026-09-10 cadence pass, R-602): both gates must pass, not
    // either. A count gate alone lets a fast Endless G1 run show an ad every
    // ~90 s of play; a time gate alone is worse in the other direction —
    // past G4 nearly every solve clears the timer, so the ad lands on almost
    // every hard-won board and the tax rises with difficulty. ANDed, the
    // share of interrupted wins stays flat across the whole grade ramp.

    // The persisted counters. Adapter-owned and device-local: never in
    // Profile and never in the action log, because a wall clock in the log
    // breaks deterministic replay.
    [Serializable]
    public sealed class AdCounters
    {
        public int LifetimeSolves;        // outside the tutorial
        public int SolvesSinceAd;
        public float PlaySecondsSinceAd;  // foreground board time, not wall clock
        public int AdsToday;
        public int DayStamp;              // local day index the count belongs to
    }

    // Every cadence value is clamped on read. Nothing feeds these remotely
    // yet; the clamps exist before anything can, so the worst a bad value
    // can ever do is be useless rather than ship an ad every two seconds.
    public static class AdCadenceLimits
    {
        public const int GraceMin = 0, GraceMax = 100;
        public const int SolvesMin = 1, SolvesMax = 10;
        public const float SecondsMin = 60f, SecondsMax = 900f;
        public const int PerDayMin = 1, PerDayMax = 40;

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }

    public sealed class AdCadenceGate
    {
        readonly AdCadence _cadence;

        public AdCounters Counters { get; }

        public AdCadenceGate(AdCadence cadence, AdCounters counters)
        {
            _cadence = cadence ?? new AdCadence();
            Counters = counters ?? new AdCounters();
        }

        public int Grace => AdCadenceLimits.Clamp(
            _cadence.GraceLifetimeSolves, AdCadenceLimits.GraceMin, AdCadenceLimits.GraceMax);

        public int SolvesBetween => AdCadenceLimits.Clamp(
            _cadence.MinSolvesBetweenAds, AdCadenceLimits.SolvesMin, AdCadenceLimits.SolvesMax);

        public float SecondsBetween => AdCadenceLimits.Clamp(
            _cadence.MinSecondsBetweenAds, AdCadenceLimits.SecondsMin, AdCadenceLimits.SecondsMax);

        public int PerDay => AdCadenceLimits.Clamp(
            _cadence.MaxAdsPerDay, AdCadenceLimits.PerDayMin, AdCadenceLimits.PerDayMax);

        public bool InterstitialEnabled => _cadence.InterstitialEnabled;
        public bool RewardedEnabled => _cadence.RewardedEnabled;

        // The day cap is per local calendar day. Crossing midnight resets it;
        // the stamp is whatever day index the adapter hands over, so the gate
        // itself never reads a clock.
        public void RollDay(int today)
        {
            if (Counters.DayStamp == today) return;
            Counters.DayStamp = today;
            Counters.AdsToday = 0;
        }

        // A solve outside the tutorial. The tutorial's ten boards count for
        // nothing: not toward the grace, not toward the gate.
        public void CountSolve()
        {
            Counters.LifetimeSolves++;
            Counters.SolvesSinceAd++;
        }

        public void AddPlaySeconds(float dt)
        {
            if (dt > 0f) Counters.PlaySecondsSinceAd += dt;
        }

        public bool ShouldShow(int today)
        {
            RollDay(today);
            if (!_cadence.InterstitialEnabled) return false;
            if (Counters.LifetimeSolves < Grace) return false;
            if (Counters.SolvesSinceAd < SolvesBetween) return false;
            if (Counters.PlaySecondsSinceAd < SecondsBetween) return false;
            if (Counters.AdsToday >= PerDay) return false;
            return true;
        }

        public void NoteShown(int today)
        {
            RollDay(today);
            Counters.AdsToday++;
            ResetGates();
        }

        // A player who just watched 30 s for a Lock does not get an
        // interstitial on the same board (NEXT_PASS decision 8: the rewarded
        // placement is user-initiated, and charging for it twice reads as a
        // bait).
        public void NoteRewarded()
        {
            if (_cadence.RewardedResetsGates) ResetGates();
        }

        void ResetGates()
        {
            Counters.SolvesSinceAd = 0;
            Counters.PlaySecondsSinceAd = 0f;
        }
    }
}
