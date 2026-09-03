using System;

namespace GridInfect.Core
{
    // A level from a baked pool: what a loader needs and nothing else.
    public sealed class PoolLevel
    {
        public LevelDef Def;
        public (int piece, int cell)[] Solution;   // locked pieces first
        public (int piece, int cell)[] Locks;
        public Solving.Grade Grade;
        public int TraceLength;
        public ulong Seed;
        public string Hash;
        public int Index;                          // position in its pool
    }

    // The Daily's boards: seven baked pools, one per weekday, from
    // docs/daily/*.jsonl (tools/gen_daily.sh, tools/bake_worlds.py). A date
    // maps to its weekday's pool and to the week number since the epoch,
    // so every device opens the same board and no generation runs on the
    // device (MODES.md §5.1).
    public static class DailyPool
    {
        // A Monday; week n of the pool cycle starts n weeks after it.
        public static readonly DateTime Epoch = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        public static string PoolId(DayOfWeek day) => "d" + (day == DayOfWeek.Sunday ? 7 : (int)day);

        public static int Count(DayOfWeek day)
        {
            int pool = Pool(day);
            return DailyData.Offsets[pool + 1] - DailyData.Offsets[pool];
        }

        public static int IndexFor(DateTime date)
        {
            int days = (int)(date.Date - Epoch).TotalDays;
            int week = days >= 0 ? days / 7 : -((-days + 6) / 7);
            int count = Count(date.DayOfWeek);
            return ((week % count) + count) % count;
        }

        public static PoolLevel For(DateTime date) => Get(date.DayOfWeek, IndexFor(date));

        public static PoolLevel Get(DayOfWeek day, int index)
        {
            int flat = Flat(day, index);
            return new PoolLevel
            {
                Def = Decode(flat),
                Solution = LevelPools.Pairs(DailyData.Solutions[flat]),
                Locks = LevelPools.Pairs(DailyData.Locks[flat]),
                Grade = (Solving.Grade)DailyData.Grades[flat],
                TraceLength = DailyData.TraceLengths[flat],
                Seed = DailyData.Seeds[flat],
                Hash = DailyData.Hashes[flat],
                Index = index,
            };
        }

        public static string[] Elements(DayOfWeek day) =>
            DailyData.Elements[Pool(day)].Length == 0 ? Array.Empty<string>() : DailyData.Elements[Pool(day)].Split(',');

        static int Pool(DayOfWeek day)
        {
            int pool = Array.IndexOf(DailyData.Ids, PoolId(day));
            if (pool < 0) throw new InvalidOperationException($"no daily pool for {day}");
            return pool;
        }

        static int Flat(DayOfWeek day, int index)
        {
            int pool = Pool(day);
            if (index < 0 || index >= DailyData.Offsets[pool + 1] - DailyData.Offsets[pool]) throw new ArgumentOutOfRangeException(nameof(index));
            return DailyData.Offsets[pool] + index;
        }

        static LevelDef Decode(int flat) => LevelPools.Decode(DailyData.Boards[flat], DailyData.Pieces[flat], DailyData.Relays[flat]);
    }

    // Shared decoding for the baked pools (WorldData, DailyData).
    internal static class LevelPools
    {
        public static (int piece, int cell)[] Pairs(string text)
        {
            if (text.Length == 0) return Array.Empty<(int, int)>();
            string[] parts = text.Split(' ');
            var result = new (int, int)[parts.Length];
            for (int n = 0; n < parts.Length; n++)
            {
                string[] pc = parts[n].Split('@');
                result[n] = (int.Parse(pc[0]), int.Parse(pc[1]));
            }
            return result;
        }

        public static LevelDef Decode(string boardText, string piecesText, string relaysText)
        {
            if (boardText.Length != Grid.Cells) throw new InvalidOperationException($"baked board has {boardText.Length} cells");
            var board = new byte[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++) board[loc] = (byte)(boardText[loc] - '0');
            // Baked content runs on RulesV2 (stage 7); Legacy stays on the classic rules.
            string[] names = piecesText.Split(',');
            var specs = new PieceSpec[names.Length];
            for (int k = 0; k < names.Length; k++) specs[k] = PieceSpec.Parse(names[k]);
            byte[] cellData = null;
            if (relaysText.Length > 0)
            {
                cellData = new byte[Grid.Cells];
                foreach (string entry in relaysText.Split(' '))
                {
                    string[] parts = entry.Split(':');
                    cellData[int.Parse(parts[0])] = (byte)int.Parse(parts[1]);
                }
            }
            return new LevelDef(board, specs, cellData);
        }
    }
}
