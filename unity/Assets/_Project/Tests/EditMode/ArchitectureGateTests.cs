using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    /// <summary>
    /// Mechanical boundary gates (cheap to amend in a normal change, so the
    /// layering iterates instead of eroding). In Unity the asmdefs enforce
    /// the module graph at compile time; these tests enforce the same rules
    /// here, source-level, so `dotnet test` fails on the same violations.
    /// </summary>
    [TestFixture]
    public class ArchitectureGateTests
    {
        static string EngineDir => Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Engine");
        static string CoreDir => Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Core");

        [Test]
        public void EngineAndCoreNeverTouchUnity()
        {
            // R-1301: zero UnityEngine anywhere in the pure assemblies.
            foreach (string file in Sources(EngineDir, CoreDir))
            {
                string text = File.ReadAllText(file);
                Assert.That(text, Does.Not.Contain("UnityEngine"), Relative(file));
                Assert.That(text, Does.Not.Contain("UnityEditor"), Relative(file));
            }
        }

        [Test]
        public void KernelNeverReferencesTheGame()
        {
            // Dependency direction: Bloodhound.Engine is reusable; GridInfect
            // depends on it, never the reverse.
            foreach (string file in Sources(EngineDir))
            {
                Assert.That(File.ReadAllText(file), Does.Not.Contain("GridInfect"), Relative(file));
            }
        }

        [Test]
        public void RulesAreOnlyMutatedThroughActionsAndRules()
        {
            // The one-owner rule, source-level: no file outside Core/Rules and
            // Core/Actions calls into the Rules mutators.
            var mutators = new Regex(@"Rules\.(SetPiece|Resolve|ClearPiece|FullReset|PropagatePiece|PropagateRepel|ResetBoard|ChangeBoard)\(");
            foreach (string file in Sources(Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Game")))
            {
                Assert.That(mutators.IsMatch(File.ReadAllText(file)), Is.False,
                    $"{Relative(file)} mutates rules state directly; dispatch an action instead");
            }
        }

        [Test]
        public void EveryDeclaredActionNameIsRegisteredAndNothingElse()
        {
            var declared = new HashSet<string>();
            foreach (FieldInfo field in typeof(GridInfectActions).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    declared.Add((string)field.GetRawConstantValue());
                }
            }

            var registry = new Bloodhound.Engine.ActionRegistry<GameState>();
            GridInfectActions.RegisterAll(registry);
            var registered = new HashSet<string>();
            foreach (var action in registry.All) registered.Add(action.Name);

            Assert.That(registered, Is.EquivalentTo(declared),
                "GridInfectActions constants and the registry must list the same actions");
        }

        [Test]
        public void ActionNamesFollowTheAggregateVerbConvention()
        {
            var convention = new Regex("^[a-z]+\\.[a-z]+$");
            var registry = new Bloodhound.Engine.ActionRegistry<GameState>();
            GridInfectActions.RegisterAll(registry);
            foreach (var action in registry.All)
            {
                Assert.That(convention.IsMatch(action.Name), Is.True, $"'{action.Name}' breaks aggregate.verb");
                Assert.That(action.Version, Is.GreaterThanOrEqualTo(1));
            }
        }

        [Test]
        public void ActionRegistryIsDocumentedInArchitectureMd()
        {
            // The registry is a founding artifact; the document of record must
            // name every action. Mechanical, so the doc cannot silently rot.
            string doc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "ARCHITECTURE.md"));
            var registry = new Bloodhound.Engine.ActionRegistry<GameState>();
            GridInfectActions.RegisterAll(registry);
            foreach (var action in registry.All)
            {
                Assert.That(doc, Does.Contain("`" + action.Name + "`"),
                    $"ARCHITECTURE.md is missing action '{action.Name}'");
            }
        }

        static IEnumerable<string> Sources(params string[] dirs)
        {
            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        static string Relative(string file) => file.Substring(TestPaths.RepoRoot.Length + 1);
    }
}
