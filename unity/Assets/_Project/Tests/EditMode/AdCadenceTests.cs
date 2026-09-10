using GridInfect.Services;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The interstitial cadence (R-602). AdCadenceGate has no clock and no
    // storage in it, so the whole rule is exercised here with a fake day
    // index and hand-fed seconds — a cadence bug is invisible in play until
    // it is a review, which makes this the only place it gets caught.
    public sealed class AdCadenceTests
    {
        const int Day = 100;

        static AdCadenceGate Gate(AdCadence cadence = null) =>
            new AdCadenceGate(cadence ?? new AdCadence(), new AdCounters());

        // Walk a gate forward by n solves, each taking `seconds`.
        static void Play(AdCadenceGate g, int solves, float seconds)
        {
            for (int i = 0; i < solves; i++)
            {
                g.AddPlaySeconds(seconds);
                g.CountSolve();
            }
        }

        [Test]
        public void DefaultsAreTheDecidedCadence()
        {
            var g = Gate();
            Assert.AreEqual(8, g.Grace);
            Assert.AreEqual(3, g.SolvesBetween);
            Assert.AreEqual(240f, g.SecondsBetween);
            Assert.AreEqual(8, g.PerDay);
        }

        [Test]
        public void NoAdBeforeTheLifetimeGrace()
        {
            var g = Gate();
            // Seven slow solves clear both gates and still get nothing: the
            // grace is what a Daily-only player used to never reach, because
            // the old counter was session-scoped.
            Play(g, 7, 600f);
            Assert.IsFalse(g.ShouldShow(Day));
            Assert.AreEqual(7, g.Counters.LifetimeSolves);
        }

        [Test]
        public void BothGatesRequired_CountAloneIsNotEnough()
        {
            var g = Gate();
            Play(g, 8, 5f);                       // past the grace, gates reset below
            g.NoteShown(Day);
            Play(g, 3, 10f);                      // 3 solves, only 30 s of play
            Assert.IsFalse(g.ShouldShow(Day), "fast Endless boards must not clear on count alone");
        }

        [Test]
        public void BothGatesRequired_TimeAloneIsNotEnough()
        {
            var g = Gate();
            Play(g, 8, 5f);
            g.NoteShown(Day);
            Play(g, 1, 600f);                     // one long G5 board, ten minutes
            Assert.IsFalse(g.ShouldShow(Day), "a hard-won solve must not be taxed on time alone");
        }

        [Test]
        public void BothGatesTogetherFire()
        {
            var g = Gate();
            Play(g, 8, 5f);
            g.NoteShown(Day);
            Play(g, 3, 100f);                     // 3 solves and 300 s
            Assert.IsTrue(g.ShouldShow(Day));
        }

        [Test]
        public void ShowingResetsBothGates()
        {
            var g = Gate();
            Play(g, 8, 100f);
            Assert.IsTrue(g.ShouldShow(Day));
            g.NoteShown(Day);
            Assert.AreEqual(0, g.Counters.SolvesSinceAd);
            Assert.AreEqual(0f, g.Counters.PlaySecondsSinceAd);
            Assert.IsFalse(g.ShouldShow(Day));
            // Lifetime solves are never reset — the grace is passed once.
            Assert.AreEqual(8, g.Counters.LifetimeSolves);
        }

        [Test]
        public void RewardedResetsTheInterstitialGates()
        {
            var g = Gate();
            Play(g, 8, 100f);
            Assert.IsTrue(g.ShouldShow(Day));
            g.NoteRewarded();
            Assert.IsFalse(g.ShouldShow(Day),
                "watching a rewarded ad for a Lock must not be followed by an interstitial");
        }

        [Test]
        public void RewardedResetIsConfigurable()
        {
            var g = Gate(new AdCadence { RewardedResetsGates = false });
            Play(g, 8, 100f);
            g.NoteRewarded();
            Assert.IsTrue(g.ShouldShow(Day));
        }

        [Test]
        public void DayCapHolds()
        {
            var g = Gate(new AdCadence { MaxAdsPerDay = 2 });
            Play(g, 8, 100f);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(g.ShouldShow(Day), "ad {0} of the day should fire", i + 1);
                g.NoteShown(Day);
                Play(g, 3, 100f);
            }
            Assert.IsFalse(g.ShouldShow(Day), "the third ad is over the cap");
        }

        [Test]
        public void DayCapResetsOnTheNextDay()
        {
            var g = Gate(new AdCadence { MaxAdsPerDay = 1 });
            Play(g, 8, 100f);
            g.NoteShown(Day);
            Play(g, 3, 100f);
            Assert.IsFalse(g.ShouldShow(Day));
            Assert.IsTrue(g.ShouldShow(Day + 1));
            Assert.AreEqual(0, g.Counters.AdsToday);
        }

        [Test]
        public void KillSwitchStopsEverything()
        {
            var g = Gate(new AdCadence { InterstitialEnabled = false });
            Play(g, 50, 600f);
            Assert.IsFalse(g.ShouldShow(Day));
        }

        // The clamps exist before anything remote can feed these. The worst a
        // bad value may do is be useless, never ship an ad every two seconds.
        [Test]
        public void AbsurdValuesAreClamped()
        {
            var g = Gate(new AdCadence
            {
                GraceLifetimeSolves = -5,
                MinSolvesBetweenAds = 0,
                MinSecondsBetweenAds = 0.1f,
                MaxAdsPerDay = 100000,
            });
            Assert.AreEqual(AdCadenceLimits.GraceMin, g.Grace);
            Assert.AreEqual(AdCadenceLimits.SolvesMin, g.SolvesBetween);
            Assert.AreEqual(AdCadenceLimits.SecondsMin, g.SecondsBetween);
            Assert.AreEqual(AdCadenceLimits.PerDayMax, g.PerDay);
        }

        [Test]
        public void ClampedFloorStillPacesTheFastestRun()
        {
            // Even at every clamped minimum, a 20 s Endless G1 board cannot
            // produce an ad more often than the 60 s floor allows.
            var g = Gate(new AdCadence
            {
                GraceLifetimeSolves = 0,
                MinSolvesBetweenAds = 0,
                MinSecondsBetweenAds = 0f,
                MaxAdsPerDay = 40,
            });
            Play(g, 2, 20f);                      // 40 s of play
            Assert.IsFalse(g.ShouldShow(Day));
            Play(g, 1, 20f);                      // 60 s
            Assert.IsTrue(g.ShouldShow(Day));
        }

        // The regression the whole pass exists for: under the old rule (a
        // 90 s timer and nothing else) a run of long boards took an ad on
        // nearly every solve. Under the new one it does not.
        [Test]
        public void HardBoardsAreNotTaxedEverySolve()
        {
            var g = Gate();
            Play(g, 8, 240f);                     // clear the grace
            g.NoteShown(Day);

            int ads = 0, solves = 0;
            for (int i = 0; i < 12; i++)          // twelve G5-length boards
            {
                g.AddPlaySeconds(240f);
                g.CountSolve();
                solves++;
                if (g.ShouldShow(Day)) { ads++; g.NoteShown(Day); }
            }
            Assert.AreEqual(4, ads, "one ad per three long solves, not one per solve");
            Assert.AreEqual(12, solves);
        }
    }
}
