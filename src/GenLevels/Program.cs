using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using GridInfect.Core;
using GridInfect.Core.Generation;
using GridInfect.Core.Solving;

namespace GridInfect.GenLevels
{
    // gen_levels --grade G2 --count 25 --seed 1000 --pieces 3-5 --out file.jsonl
    //            [--grades G2-G3] [--elements walls,shortarms] [--short-arm-chance 10]
    //            [--min-active 6] [--max-active 40] [--min-run 1] [--max-run 5]
    //            [--end-wall 14] [--gaps] [--base-chance 15] [--falloff 1] [--shape-bias 0]
    //            [--distance 2] [--shared-lines] [--symmetric-tiles] [--dup-tiles]
    //            [--max-walls 12] [--max-prune 12] [--cap 4000] [--max-seeds N] [--threads 1] [--quiet] [--spec-json]
    // Writes one JSON object per accepted level and prints the acceptance
    // report (per rejection reason, seeds tried, wall clock) to stderr.
    public static class Program
    {
        public static int Main(string[] args)
        {
            var spec = new GenSpec();
            int count = 25;
            ulong seed = 1;
            long maxSeeds = long.MaxValue;
            string outPath = null;
            bool quiet = false;
            bool specOnly = false;
            int threads = 1;
            Grade? grade = null;

            for (int a = 0; a < args.Length; a++)
            {
                string next() => a + 1 < args.Length ? args[++a] : throw new ArgumentException($"{args[a]} needs a value");
                switch (args[a])
                {
                    case "--grade": grade = (Grade)Enum.Parse(typeof(Grade), next()); break;
                    case "--grades":
                    {
                        string[] range = next().Split('-');
                        spec.MinGrade = (Grade)Enum.Parse(typeof(Grade), range[0]);
                        spec.MaxGrade = (Grade)Enum.Parse(typeof(Grade), range[range.Length - 1]);
                        break;
                    }
                    case "--elements":
                    {
                        spec.Elements = Element.None;
                        foreach (string name in next().Split(','))
                        {
                            spec.Elements |= (Element)Enum.Parse(typeof(Element), name, ignoreCase: true);
                        }
                        break;
                    }
                    case "--short-arm-chance": spec.ShortArmChance = int.Parse(next()); break;
                    case "--count": count = int.Parse(next()); break;
                    case "--seed": seed = ulong.Parse(next()); break;
                    case "--max-seeds": maxSeeds = long.Parse(next()); break;
                    case "--pieces":
                    {
                        string[] range = next().Split('-');
                        spec.MinPieces = int.Parse(range[0]);
                        spec.MaxPieces = range.Length > 1 ? int.Parse(range[1]) : spec.MinPieces;
                        break;
                    }
                    case "--out": outPath = next(); break;
                    case "--min-active": spec.Carve.MinActive = int.Parse(next()); break;
                    case "--max-active": spec.Carve.MaxActive = int.Parse(next()); break;
                    case "--base-chance": spec.Carve.BaseChance = int.Parse(next()); break;
                    case "--falloff": spec.Carve.Falloff = int.Parse(next()); break;
                    case "--shape-bias": spec.Carve.ShapeBias = int.Parse(next()); break;
                    case "--min-run": spec.Carve.MinRun = int.Parse(next()); break;
                    case "--max-run": spec.Carve.MaxRun = int.Parse(next()); break;
                    case "--end-wall": spec.Carve.EndWallChance = int.Parse(next()); break;
                    case "--gaps": spec.Carve.Mode = CarveMode.Gaps; break;
                    case "--distance": spec.MinPieceDistance = int.Parse(next()); break;
                    case "--shared-lines": spec.ExclusiveLines = false; break;
                    case "--symmetric-tiles": spec.AllowSymmetricTiles = true; break;
                    case "--max-walls": spec.MaxWalls = int.Parse(next()); break;
                    case "--max-prune": spec.MaxPruneSteps = int.Parse(next()); break;
                    case "--cap": spec.SolutionCap = int.Parse(next()); break;
                    case "--dup-tiles": spec.AllowDuplicateTiles = true; break;
                    case "--quiet": quiet = true; break;
                    case "--threads": threads = Math.Max(1, int.Parse(next())); break;
                    case "--spec-json": specOnly = true; break;
                    default:
                        Console.Error.WriteLine($"unknown argument {args[a]}");
                        return 2;
                }
            }
            if (grade.HasValue) { spec.MinGrade = grade.Value; spec.MaxGrade = grade.Value; }
            if (specOnly)
            {
                Console.WriteLine(spec.ToJson());
                return 0;
            }

            var rejections = new int[Enum.GetValues(typeof(Rejection)).Length];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var watch = Stopwatch.StartNew();
            long tried = 0;
            int accepted = 0, duplicates = 0;
            TextWriter output = outPath != null ? new StreamWriter(outPath, false, new UTF8Encoding(false)) : Console.Out;
            try
            {
                // Seeds are generated in chunks (in parallel when asked) and
                // consumed in seed order, so the output does not depend on
                // the thread count.
                int chunk = threads * 8;
                var levels = new GeneratedLevel[chunk];
                var whys = new Rejection[chunk];
                for (ulong s = seed; accepted < count && tried < maxSeeds; s += (ulong)chunk)
                {
                    int n = (int)Math.Min(chunk, maxSeeds - tried);
                    if (threads > 1)
                    {
                        System.Threading.Tasks.Parallel.For(0, n, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = threads },
                            i => levels[i] = GeneratorV2.Generate(spec, s + (ulong)i, out whys[i]));
                    }
                    else
                    {
                        for (int i = 0; i < n; i++) levels[i] = GeneratorV2.Generate(spec, s + (ulong)i, out whys[i]);
                    }
                    for (int i = 0; i < n && accepted < count; i++)
                    {
                        tried++;
                        var level = levels[i];
                        if (level == null) { rejections[(int)whys[i]]++; continue; }
                        if (!seen.Add(level.Hash)) { duplicates++; continue; }
                        accepted++;
                        output.WriteLine(Encode(level));
                    }
                }
            }
            finally
            {
                if (outPath != null) output.Dispose();
            }
            watch.Stop();

            if (!quiet)
            {
                var sb = new StringBuilder();
                sb.Append($"accepted {accepted} of {tried} seeds in {watch.Elapsed.TotalSeconds:F1} s");
                sb.Append($" (grade {(grade.HasValue ? grade.Value.ToString() : "any")}, pieces {spec.MinPieces}-{spec.MaxPieces})");
                sb.Append($"; rate {(tried == 0 ? 0 : 100.0 * accepted / tried):F1}%; duplicates {duplicates}");
                sb.Append("; rejected:");
                foreach (Rejection r in Enum.GetValues(typeof(Rejection)))
                {
                    if (r != Rejection.None && rejections[(int)r] > 0) sb.Append($" {r}={rejections[(int)r]}");
                }
                Console.Error.WriteLine(sb.ToString());
            }
            return accepted < count ? 1 : 0;
        }

        // {"seed":..,"grade":"G2","effort":9,"board":"66 chars","pieces":"L,RD",
        //  "solution":[[piece,cell],...],"trace":[[tier,piece,cell],...],"hash":"..","walls":n}
        public static string Encode(GeneratedLevel level)
        {
            var sb = new StringBuilder();
            sb.Append("{\"seed\":").Append(level.Seed);
            sb.Append(",\"grade\":\"").Append(level.Grade).Append('"');
            sb.Append(",\"effort\":").Append(level.Effort);
            sb.Append(",\"board\":\"");
            for (int loc = 0; loc < Grid.Cells; loc++) sb.Append((char)('0' + level.Def.BoardAt(loc)));
            var specs = new string[level.Def.Specs.Length];
            for (int k = 0; k < specs.Length; k++) specs[k] = level.Def.Specs[k].Encode();
            sb.Append("\",\"pieces\":\"").Append(string.Join(",", specs)).Append('"');
            sb.Append(",\"solution\":[");
            for (int n = 0; n < level.Solution.Length; n++)
            {
                if (n > 0) sb.Append(',');
                sb.Append('[').Append(level.Solution[n].piece).Append(',').Append(level.Solution[n].cell).Append(']');
            }
            sb.Append("],\"trace\":[");
            for (int n = 0; n < level.Trace.Length; n++)
            {
                if (n > 0) sb.Append(',');
                sb.Append('[').Append((int)level.Trace[n].Tier).Append(',').Append(level.Trace[n].Piece).Append(',').Append(level.Trace[n].Cell).Append(']');
            }
            sb.Append("],\"hash\":\"").Append(level.Hash).Append('"');
            sb.Append(",\"walls\":").Append(level.Walls);
            if (level.Def.HasRelays)
            {
                sb.Append(",\"relays\":[");
                bool first = true;
                for (int loc = 0; loc < Grid.Cells; loc++)
                {
                    if (level.Def.CellDataAt(loc) == 0) continue;
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('[').Append(loc).Append(',').Append(level.Def.CellDataAt(loc)).Append(']');
                }
                sb.Append(']');
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
