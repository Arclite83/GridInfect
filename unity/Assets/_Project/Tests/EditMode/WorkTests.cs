using System;
using System.Collections.Generic;
using System.Threading;
using Bloodhound.Engine;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // The kernel's background work: jobs run in priority order on Work's
    // own threads, completions land only through Pump, and Map fans a loop
    // across the width it is told and no wider — every thread of it at the
    // lowest OS priority, so a scan never takes a core from the frame.
    public class WorkTests
    {
        [Test]
        public void MapRunsEveryIndexExactlyOnce()
        {
            var work = new Work(1, 4);
            var hits = new int[1000];
            work.Map(hits.Length, i => Interlocked.Increment(ref hits[i]));
            foreach (int h in hits) Assert.That(h, Is.EqualTo(1));
        }

        [Test]
        public void MapFansOutNoWiderThanParallelism()
        {
            var work = new Work(1, 3);
            var threads = new HashSet<int>();
            var gate = new object();
            int inFlight = 0, peak = 0;
            work.Map(200, i =>
            {
                int now = Interlocked.Increment(ref inFlight);
                lock (gate)
                {
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                    if (now > peak) peak = now;
                }
                Thread.SpinWait(2000);
                Interlocked.Decrement(ref inFlight);
            });
            Assert.That(peak, Is.LessThanOrEqualTo(3));
            Assert.That(threads.Count, Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void MapAtWidthOneRunsInOrderOnTheCallingThread()
        {
            var work = new Work(1, 1);
            var order = new List<int>();
            int caller = Thread.CurrentThread.ManagedThreadId;
            work.Map(20, i =>
            {
                Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(caller));
                order.Add(i);
            });
            Assert.That(order, Is.EqualTo(new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }));
        }

        [Test]
        public void MapRethrowsTheBodysException()
        {
            var work = new Work(1, 4);
            var e = Assert.Throws<InvalidOperationException>(() =>
                work.Map(64, i => { if (i == 7) throw new InvalidOperationException("seed 7"); }));
            Assert.That(e.Message, Is.EqualTo("seed 7"));
        }

        [Test]
        public void EveryThreadOfAJobRunsAtTheLowestPriority()
        {
            var work = new Work(1, 4);
            var seen = work.Run(() =>
            {
                var priorities = new ThreadPriority[32];
                work.Map(priorities.Length, i =>
                {
                    Thread.SpinWait(5000);
                    priorities[i] = Thread.CurrentThread.Priority;
                });
                return priorities;
            }).Wait();
            foreach (ThreadPriority p in seen) Assert.That(p, Is.EqualTo(ThreadPriority.Lowest));
        }

        [Test]
        public void JobsRunLowestPriorityFirstAndCompleteThroughPump()
        {
            var work = new Work(1);
            var release = new ManualResetEventSlim(false);
            var ran = new List<string>();
            var landed = new List<string>();
            int pumpThread = Thread.CurrentThread.ManagedThreadId;

            // The worker is busy with this one while the rest queue up.
            var first = work.Run(() => { release.Wait(); return "gate"; }, -100, "gate", r => landed.Add(r));
            var later = new List<WorkItem<string>>
            {
                work.Run(() => { lock (ran) ran.Add("p10"); return "p10"; }, 10, "p10", r => { Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(pumpThread)); landed.Add(r); }),
                work.Run(() => { lock (ran) ran.Add("p0"); return "p0"; }, 0, "p0", r => landed.Add(r)),
                work.Run(() => { lock (ran) ran.Add("p5"); return "p5"; }, 5, "p5", r => landed.Add(r)),
            };
            Assert.That(work.Raise("p10", 1), Is.True);        // moves up, behind p0
            Assert.That(work.Cancel("p5"), Is.True);           // taken back before it ran
            release.Set();
            first.Wait();
            foreach (var item in later)
            {
                if (item.Key != "p5") item.Wait();
            }
            Assert.That(later[2].Cancelled, Is.True);
            Assert.That(ran, Is.EqualTo(new List<string> { "p0", "p10" }));

            Assert.That(landed, Is.Empty, "nothing lands before Pump");
            work.Pump();
            Assert.That(landed, Is.EqualTo(new List<string> { "gate", "p0", "p10" }));
        }

        [Test]
        public void AFailedJobFaultsItsHandleAndRunsNoCompletion()
        {
            var work = new Work(1);
            bool completed = false;
            var item = work.Run<int>(() => throw new InvalidOperationException("boom"), 0, null, _ => completed = true);
            var e = Assert.Throws<InvalidOperationException>(() => item.Wait());
            Assert.That(e.Message, Is.EqualTo("boom"));
            Assert.That(item.Faulted, Is.True);
            work.Pump();
            Assert.That(completed, Is.False);
        }
    }
}
