using System;
using System.IO;

namespace GridInfect.Core.Tests
{
    internal static class TestPaths
    {
        static string _repoRoot;

        public static string RepoRoot
        {
            get
            {
                if (_repoRoot != null) return _repoRoot;
                foreach (string start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
                {
                    string dir = start;
                    while (!string.IsNullOrEmpty(dir))
                    {
                        if (File.Exists(Path.Combine(dir, "docs", "test_vectors.json")))
                        {
                            _repoRoot = dir;
                            return dir;
                        }
                        dir = Path.GetDirectoryName(dir);
                    }
                }
                throw new FileNotFoundException(
                    "could not locate repo root (docs/test_vectors.json) above " +
                    $"'{AppContext.BaseDirectory}' or '{Environment.CurrentDirectory}'");
            }
        }

        public static string VectorsPath => Path.Combine(RepoRoot, "docs", "test_vectors.json");
    }
}
