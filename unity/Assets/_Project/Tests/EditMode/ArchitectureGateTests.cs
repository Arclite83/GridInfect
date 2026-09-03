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
        static string ServicesDir => Path.Combine(TestPaths.RepoRoot, "unity", "Assets", "_Project", "Services");

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

        // R-1303 (stage 6): SDK types stay in GridInfect.Services. Core and
        // Game never name an ads, consent or purchasing SDK; Core never names
        // the Services assembly; Services never reaches into the game.
        [Test]
        public void SdkTypesNeverLeaveTheServicesAssembly()
        {
            string[] sdk = { "GoogleMobileAds", "UnityEngine.Purchasing", "Unity.Services", "UnityEngine.Advertisements" };
            foreach (string file in Sources(EngineDir, CoreDir, GameDir))
            {
                string text = File.ReadAllText(file);
                foreach (string ns in sdk) Assert.That(text, Does.Not.Contain(ns), Relative(file));
            }
            foreach (string file in Sources(EngineDir, CoreDir))
            {
                Assert.That(File.ReadAllText(file), Does.Not.Contain("GridInfect.Services"), Relative(file));
            }
            foreach (string file in Sources(ServicesDir))
            {
                string text = File.ReadAllText(file);
                Assert.That(text, Does.Not.Contain("GridInfect.Core"), Relative(file));
                Assert.That(text, Does.Not.Contain("GridInfect.Game"), Relative(file));
            }
            string coreAsmdef = File.ReadAllText(Path.Combine(CoreDir, "GridInfect.Core.asmdef"));
            Assert.That(coreAsmdef, Does.Not.Contain("Services"));
            string servicesAsmdef = File.ReadAllText(Path.Combine(ServicesDir, "GridInfect.Services.asmdef"));
            Assert.That(servicesAsmdef, Does.Not.Contain("GridInfect.Core"));
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
