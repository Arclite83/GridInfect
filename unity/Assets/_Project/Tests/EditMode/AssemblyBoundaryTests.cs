using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace GridInfect.Core.Tests
{
    // R-1303 / EXECUTION_PLAN stage 6: SDK types live in GridInfect.Services
    // and nowhere else. Core and Game talk to the interfaces.
    //
    // A source scan rather than a reflection scan, on purpose: the SDK
    // assemblies are not present in this environment (and are not present in
    // CI), so there is nothing to reflect over, and a reflection test would
    // pass vacuously for exactly as long as the packages are missing — which
    // is the whole window the rule needs guarding.
    public sealed class AssemblyBoundaryTests
    {
        // Namespaces that only the Services assembly may name.
        static readonly string[] SdkNamespaces =
        {
            "GoogleMobileAds",
            "UnityEngine.Purchasing",
            "Unity.Services",
        };

        static DirectoryInfo ProjectRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "unity", "Assets")))
                dir = dir.Parent;
            Assert.NotNull(dir, "could not locate the repo root from the test directory");
            return new DirectoryInfo(Path.Combine(dir.FullName, "unity", "Assets", "_Project"));
        }

        static IEnumerable<string> SourcesUnder(string folder)
        {
            var dir = new DirectoryInfo(Path.Combine(ProjectRoot().FullName, folder));
            Assert.IsTrue(dir.Exists, folder + " not found at " + dir.FullName);
            return dir.GetFiles("*.cs", SearchOption.AllDirectories).Select(f => f.FullName);
        }

        static void AssertNoSdkReferences(string folder)
        {
            var offenders = new List<string>();
            foreach (string path in SourcesUnder(folder))
            {
                string text = File.ReadAllText(path);
                foreach (string ns in SdkNamespaces)
                {
                    // "using GoogleMobileAds" or a fully-qualified mention.
                    if (text.Contains("using " + ns) || text.Contains(ns + "."))
                        offenders.Add(Path.GetFileName(path) + " names " + ns);
                }
            }

            Assert.IsEmpty(offenders,
                folder + " must reach the SDKs through GridInfect.Services only (R-1303):\n  " +
                string.Join("\n  ", offenders));
        }

        [Test]
        public void CoreNamesNoSdk() => AssertNoSdkReferences("Core");

        [Test]
        public void GameNamesNoSdk() => AssertNoSdkReferences("Game");

        // Core is stricter still: no UnityEngine at all (R-1301, noEngineReferences).
        [Test]
        public void CoreNamesNoUnityEngine()
        {
            var offenders = SourcesUnder("Core")
                .Where(p => File.ReadAllText(p).Contains("using UnityEngine"))
                .Select(Path.GetFileName)
                .ToList();
            Assert.IsEmpty(offenders, "Core is pure C# (R-1301): " + string.Join(", ", offenders));
        }

        // The SDK-facing files are fenced so that an unimported, uncompiled
        // implementation can never reach a build. If someone drops the guard,
        // this fails before the plugin is in the project.
        [Test]
        public void SdkImplementationsAreFencedBehindDefines()
        {
            var dir = new DirectoryInfo(Path.Combine(ProjectRoot().FullName, "Services", "Sdk"));
            Assert.IsTrue(dir.Exists, "Services/Sdk not found");

            foreach (var file in dir.GetFiles("*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file.FullName);
                bool fenced = text.Contains("#if GRIDINFECT_ADMOB") || text.Contains("#if GRIDINFECT_IAP");
                Assert.IsTrue(fenced, file.Name + " must be guarded by a GRIDINFECT_* define");
                Assert.IsTrue(text.TrimEnd().EndsWith("#endif", StringComparison.Ordinal),
                    file.Name + " guard must close at end of file");
            }
        }
    }
}
