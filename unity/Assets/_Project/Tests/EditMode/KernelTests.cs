using System.Collections.Generic;
using Bloodhound.Engine;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    /// <summary>Bloodhound.Engine kernel: JSON boundary, RNG contract, dispatch/log semantics.</summary>
    [TestFixture]
    public class KernelTests
    {
        [Test]
        public void Pcg32MatchesReferenceSequence()
        {
            // Known-answer test: the canonical pcg32 demo (pcg-random.org),
            // pcg32_srandom(42, 54). Locks our implementation to real PCG.
            var rng = new Pcg32(42, 54);
            var expected = new uint[] { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (uint want in expected)
            {
                Assert.That(rng.NextUInt(), Is.EqualTo(want));
            }
        }

        [Test]
        public void Pcg32BoundedDrawIsModulo()
        {
            var raw = new Pcg32(7, 11);
            var bounded = new Pcg32(7, 11);
            for (int n = 0; n < 100; n++)
            {
                Assert.That(bounded.Next(20), Is.EqualTo((int)(raw.NextUInt() % 20)));
            }
        }

        [Test]
        public void MiniJsonRoundTripsNestedStructures()
        {
            var value = new Dictionary<string, object>
            {
                ["text"] = "quote\" slash\\ newline\n tab\t unicodeé",
                ["int"] = 42L,
                ["neg"] = -17L,
                ["big"] = 9007199254740993L, // > 2^53: must survive as long
                ["float"] = 0.25,
                ["yes"] = true,
                ["no"] = false,
                ["nothing"] = null,
                ["list"] = new List<object> { 1L, "two", new Dictionary<string, object> { ["k"] = 3L } },
            };
            var parsed = (Dictionary<string, object>)MiniJson.Parse(MiniJson.Write(value));
            Assert.That(parsed["text"], Is.EqualTo(value["text"]));
            Assert.That(parsed["int"], Is.EqualTo(42L));
            Assert.That(parsed["neg"], Is.EqualTo(-17L));
            Assert.That(parsed["big"], Is.EqualTo(9007199254740993L));
            Assert.That(parsed["float"], Is.EqualTo(0.25));
            Assert.That(parsed["yes"], Is.True);
            Assert.That(parsed["no"], Is.False);
            Assert.That(parsed["nothing"], Is.Null);
            var list = (List<object>)parsed["list"];
            Assert.That(((Dictionary<string, object>)list[2])["k"], Is.EqualTo(3L));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("[1,]")]
        [TestCase("{\"a\":1,}")]
        [TestCase("{\"a\" 1}")]
        [TestCase("tru")]
        [TestCase("1 2")]
        public void MiniJsonRejectsMalformedInput(string bad)
        {
            Assert.Throws<System.FormatException>(() => MiniJson.Parse(bad));
        }

        [Test]
        public void RejectedDispatchLogsNothingAndMutatesNothing()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            var result = dispatcher.Dispatch(GridInfectActions.PiecePlace, Inputs.PiecePlace(0, 0, 0));
            Assert.That(result.Applied, Is.False);
            Assert.That(result.Rejection, Does.Contain("no level loaded"));
            Assert.That(dispatcher.Log.Count, Is.Zero);
            Assert.That(dispatcher.State.Session, Is.Null);
        }

        [Test]
        public void SchemaViolationsRejectInsteadOfThrowing()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            var result = dispatcher.Dispatch(GridInfectActions.LevelLoad,
                new Dictionary<string, object> { ["levelId"] = "zero" });
            Assert.That(result.Applied, Is.False);
            Assert.That(result.Rejection, Does.Contain("levelId"));
            Assert.That(dispatcher.Log.Count, Is.Zero);
        }

        [Test]
        public void UnknownActionThrows()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            Assert.Throws<KeyNotFoundException>(() => dispatcher.Dispatch("no.such"));
        }

        [Test]
        public void AppliedDispatchLogsSequentially()
        {
            var dispatcher = GridInfectActions.CreateDispatcher();
            dispatcher.Dispatch(GridInfectActions.LevelLoad, Inputs.LevelLoad(0));
            dispatcher.Dispatch(GridInfectActions.SettingsMute, Inputs.Muted(true));
            Assert.That(dispatcher.Log.Count, Is.EqualTo(2));
            Assert.That(dispatcher.Log[0].Seq, Is.EqualTo(1));
            Assert.That(dispatcher.Log[1].Seq, Is.EqualTo(2));
            Assert.That(dispatcher.Log[1].Action, Is.EqualTo(GridInfectActions.SettingsMute));
        }
    }
}
