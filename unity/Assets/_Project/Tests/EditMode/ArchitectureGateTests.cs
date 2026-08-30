using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // Mechanical boundary gates — the asmdefs enforce the module graph in
    // Unity; these enforce the same rules under `dotnet test`.
    [TestFixture]
    public class ArchitectureGateTests
    {
        static string EngineDir => Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Engine");
        static string CoreDir => Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Core");
        static string GameDir => Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Game");

        [Test]
        public void EngineAndCoreNeverTouchUnity()
        {
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
            foreach (string file in Sources(EngineDir))
            {
                Assert.That(File.ReadAllText(file), Does.Not.Contain("GridInfect"), Relative(file));
            }
        }

        [Test]
        public void AdapterMutatesOnlyThroughActions()
        {
            var mutators = new Regex(@"Rules\.(SetPiece|Resolve|ClearPiece|FullReset|PropagatePiece|PropagateRepel|ResetBoard|ChangeBoard)\(");
            foreach (string file in Sources(GameDir))
            {
                Assert.That(mutators.IsMatch(File.ReadAllText(file)), Is.False,
                    $"{Relative(file)} mutates rules state directly; dispatch an action instead");
            }
        }

        [Test]
        public void RegistryConstantsAndArchitectureDocAgree()
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

            Assert.That(registered, Is.EquivalentTo(declared));

            string doc = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "ARCHITECTURE.md"));
            foreach (string name in registered)
            {
                Assert.That(doc, Does.Contain("`" + name + "`"), $"ARCHITECTURE.md is missing action '{name}'");
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
