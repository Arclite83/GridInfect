using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // docs/infection-vfx-spec.md is the source of truth for the infection
    // look, and the shader plus the palette are the only places that consume
    // it. These are source gates, like ArchitectureGateTests: the presentation
    // layer lives in a Unity-only assembly, so what is checkable headlessly is
    // that the numbers and the colours have not drifted from the spec, and
    // that the shader still has exactly one place to get a colour from.
    [TestFixture]
    public class InfectionVfxSpecTests
    {
        static string SpecPath => Path.Combine(TestPaths.RepoRoot, "docs", "infection-vfx-spec.md");

        static string GamePath(params string[] parts)
        {
            var segments = new List<string> { TestPaths.RepoRoot, "unity", "Assets", "_Project", "Game" };
            segments.AddRange(parts);
            return Path.Combine(segments.ToArray());
        }

        static string ShaderPath => GamePath("Shaders", "GridInfectBoard.shader");
        static string PalettePath => GamePath("View", "BoardPalette.cs");
        static string ConfigPath => GamePath("PresentationConfig.cs");

        // "| Hop delay | 40 ms | `_Hop` |" -> ("_Hop", "40 ms")
        static Dictionary<string, string> LockedParameters()
        {
            var table = new Dictionary<string, string>();
            var row = new Regex(@"^\|\s*[^|]+\|\s*([^|]+?)\s*\|\s*`(_\w+)`\s*\|\s*$");
            foreach (string line in File.ReadAllLines(SpecPath))
            {
                Match match = row.Match(line);
                if (match.Success) table[match.Groups[2].Value] = match.Groups[1].Value;
            }
            return table;
        }

        // "| Infected fill | `#00D9FF` | ... |" -> ("Infected fill", "#00D9FF")
        static Dictionary<string, string> PaletteTable()
        {
            var table = new Dictionary<string, string>();
            var row = new Regex(@"^\|\s*([^|]+?)\s*\|\s*`(#[0-9A-Fa-f]{6})`[^|]*\|");
            foreach (string line in File.ReadAllLines(SpecPath))
            {
                Match match = row.Match(line);
                if (match.Success) table[match.Groups[1].Value] = match.Groups[2].Value;
            }
            return table;
        }

        static float Constant(string source, string name)
        {
            Match match = Regex.Match(source, @"public const (?:float|int) " + name + @"\s*=\s*([0-9.]+)f?;");
            Assert.That(match.Success, Is.True, $"PresentationConfig.Infection.{name} is missing");
            return float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        [Test]
        public void LockedParametersMatchTheSpec()
        {
            var spec = LockedParameters();
            string config = File.ReadAllText(ConfigPath);

            // Spec column -> the constant that feeds the shader uniform. Every
            // locked row has to be here, so a new row fails until it is wired.
            var wired = new Dictionary<string, (string name, float scale)>
            {
                ["_Blocks"] = ("Blocks", 1f),
                ["_Hop"] = ("Hop", 1000f),
                ["_Bias"] = ("Bias", 1f),
                ["_GlowHold"] = ("GlowHold", 1000f),
                ["_GlowFade"] = ("GlowFade", 1000f),
                ["_TraceDur"] = ("TraceDur", 1000f),
                ["_BleedDur"] = ("BleedDur", 1000f),
            };

            Assert.That(spec.Keys, Is.EquivalentTo(wired.Keys),
                "docs/infection-vfx-spec.md locked-parameter table and the wiring here disagree");

            foreach (var pair in wired)
            {
                float expected = float.Parse(
                    Regex.Match(spec[pair.Key], @"[0-9.]+").Value,
                    System.Globalization.CultureInfo.InvariantCulture);
                float actual = Constant(config, pair.Value.name) * pair.Value.scale;
                Assert.That(actual, Is.EqualTo(expected).Within(1e-4),
                    $"{pair.Key}: spec says {spec[pair.Key]}, PresentationConfig.Infection.{pair.Value.name} says {actual}");
            }
        }

        [Test]
        public void EveryLockedParameterIsAShaderProperty()
        {
            string shader = File.ReadAllText(ShaderPath);
            foreach (string name in LockedParameters().Keys)
            {
                // _Hop never reaches the shader: start times are baked per cell
                // by the wave scheduler, which is what lets several traces be in
                // flight at once off one uniform clock.
                if (name == "_Hop") continue;
                Assert.That(shader, Does.Match(@"(?m)^\s*" + name + @"\s*\("),
                    $"{name} is a locked parameter but the board shader does not declare it");
            }
        }

        [Test]
        public void PaletteDefaultsMatchTheSpec()
        {
            string palette = File.ReadAllText(PalettePath);
            foreach (var entry in PaletteTable())
            {
                Assert.That(palette, Does.Contain($"Hex(\"{entry.Value}\")"),
                    $"spec palette row '{entry.Key}' is {entry.Value}, which BoardPalette does not define");
            }
        }

        [Test]
        public void BoardShaderSamplesNoLiteralColour()
        {
            // Colours reach the shader only as _Col* uniforms fed from
            // BoardPalette, so swapping the asset restyles the whole board with
            // no code or shader edits (acceptance criterion 8). The property
            // block declares them; the body may not invent one.
            int propertyBlockEnd = File.ReadAllText(ShaderPath).IndexOf("SubShader", System.StringComparison.Ordinal);
            Assert.That(propertyBlockEnd, Is.GreaterThan(0));
            string body = File.ReadAllText(ShaderPath).Substring(propertyBlockEnd);

            Assert.That(body, Does.Not.Match(@"#[0-9A-Fa-f]{6}"), "literal hex colour in the shader body");
            foreach (Match match in Regex.Matches(body, @"(?:float3|float4|half3|half4)\s*\([^)]*\)"))
            {
                Assert.That(match.Value, Does.Not.Match(@"\d\s*,\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+"),
                    $"literal colour constant in the shader body: {match.Value}");
            }
        }

        [Test]
        public void JuiceLayersAreIndependentSwitchesOnTheBoard()
        {
            string view = File.ReadAllText(GamePath("View", "BoardView.cs"));
            // "Each is an independent bool on the board controller, default on
            // unless noted" — the ghost trail is the one the spec ships off.
            foreach (string layer in new[] { "ArrivalPulse", "ConflictShake", "EdgeSparks", "TraceDim", "HopAudio" })
            {
                Assert.That(view, Does.Contain($"public bool {layer} = true;"),
                    $"juice layer '{layer}' is not an independent default-on switch on BoardView");
            }
            Assert.That(view, Does.Contain("public bool GhostTrail = false;"),
                "the ghost trail ships off");
        }
    }
}
