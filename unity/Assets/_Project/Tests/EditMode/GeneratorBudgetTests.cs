using System;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The generator's allocation budget. It runs on a worker while the
    // player plays, and a managed runtime stops every thread to collect:
    // garbage on the worker is a hitch on the main thread, whichever core
    // either runs on. Before the budget the constructor allocated 1–19 MB
    // per seed (a heap stack per spread, a line map per candidate given, a
    // string key per covering set, fresh search tables per count), which
    // was hundreds of megabytes a second per core and a visibly chopped
    // drag; the figures pinned here are several times the current cost.
    public class GeneratorBudgetTests
    {
        const int Seeds = 24;
        const ulong From = 123456789;
        const double SpecCeilingMb = 2.5;     // any one spec's average per seed
        const double MeanCeilingMb = 1.0;     // the average across the specs below

        static readonly (string name, GenSpec spec)[] Specs =
        {
            ("Endless G2", DailySpec.Endless(Grade.G2)),
            ("Endless G4", DailySpec.Endless(Grade.G4)),
            ("Endless G5", DailySpec.Endless(Grade.G5)),
            ("Daily Monday", DailySpec.For(DayOfWeek.Monday)),
            ("Daily Thursday", DailySpec.For(DayOfWeek.Thursday)),
            ("Daily Saturday", DailySpec.For(DayOfWeek.Saturday)),
        };

        [Test]
        public void AGeneratedSeedStaysUnderItsAllocationBudget()
        {
            // A runtime that does not count per-thread allocation reports
            // nothing here; the mirror run (dotnet test) is the gate then.
            long probed;
            try
            {
                long probeBefore = GC.GetAllocatedBytesForCurrentThread();
                GC.KeepAlive(new byte[1 << 20]);
                probed = GC.GetAllocatedBytesForCurrentThread() - probeBefore;
            }
            catch (Exception)
            {
                probed = 0;
            }
            if (probed < (1 << 20)) Assert.Ignore("this runtime does not report per-thread allocation");

            double total = 0;
            foreach (var (name, spec) in Specs)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (ulong seed = From; seed < From + Seeds; seed++) GeneratorV2.Generate(spec, seed);
                double perSeed = (GC.GetAllocatedBytesForCurrentThread() - before) / 1048576.0 / Seeds;
                total += perSeed;
                Assert.That(perSeed, Is.LessThan(SpecCeilingMb), $"{name}: {perSeed:F2} MB per seed");
            }
            Assert.That(total / Specs.Length, Is.LessThan(MeanCeilingMb), $"mean across specs: {total / Specs.Length:F2} MB per seed");
        }
    }
}
