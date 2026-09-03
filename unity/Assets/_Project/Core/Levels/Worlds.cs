using System;
using System.Collections.Generic;

namespace GridInfect.Core
{
    // A world: a named, ordered list of generated levels sharing an element
    // set. Baked from docs/worlds/*.jsonl by tools/bake_worlds.py — never
    // hand-edited. Progression: solving level N of a world unlocks N+1;
    // finishing a world unlocks the next (Queries, progress.unlockWorld*).
    public sealed class World
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string[] Elements;
        public readonly int Index;        // position in Worlds.All
        public readonly int Count;        // levels in this world

        internal World(string id, string name, string[] elements, int index, int count)
        {
            Id = id;
            Name = name;
            Elements = elements;
            Index = index;
            Count = count;
        }
    }

    public static class Worlds
    {
        static World[] _all;
        static Dictionary<string, World> _byId;
        static readonly Dictionary<int, LevelDef> Cache = new Dictionary<int, LevelDef>();

        public static World[] All
        {
            get
            {
                if (_all == null) Build();
                return _all;
            }
        }

        public static int Count => All.Length;
        public static int TotalLevels => WorldData.Offsets[WorldData.Offsets.Length - 1];

        public static World Get(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id ?? "", out World w) ? w : null;
        }

        public static World First => All[0];

        // The world after `id` in launch order, or null on the last one.
        public static World Next(string id)
        {
            World w = Get(id);
            return w != null && w.Index + 1 < All.Length ? All[w.Index + 1] : null;
        }

        public static LevelDef Level(string worldId, int index)
        {
            int flat = Flat(worldId, index);
            if (!Cache.TryGetValue(flat, out LevelDef def))
            {
                Cache[flat] = def = Decode(flat);
            }
            return def;
        }

        // The stored solution (the generator's sampled one) in a winning order.
        public static (int piece, int cell)[] Solution(string worldId, int index)
        {
            string[] parts = WorldData.Solutions[Flat(worldId, index)].Split(' ');
            var result = new (int, int)[parts.Length];
            for (int n = 0; n < parts.Length; n++)
            {
                string[] pc = parts[n].Split('@');
                result[n] = (int.Parse(pc[0]), int.Parse(pc[1]));
            }
            return result;
        }

        public static int Grade(string worldId, int index) => WorldData.Grades[Flat(worldId, index)];
        public static ulong Seed(string worldId, int index) => WorldData.Seeds[Flat(worldId, index)];
        public static string Hash(string worldId, int index) => WorldData.Hashes[Flat(worldId, index)];
        public static bool Reviewed(string worldId, int index) => WorldData.Reviewed[Flat(worldId, index)];

        static int Flat(string worldId, int index)
        {
            World w = Get(worldId) ?? throw new ArgumentException($"unknown world '{worldId}'");
            if (index < 0 || index >= w.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return WorldData.Offsets[w.Index] + index;
        }

        static void Build()
        {
            var all = new World[WorldData.Ids.Length];
            var byId = new Dictionary<string, World>(StringComparer.Ordinal);
            for (int n = 0; n < all.Length; n++)
            {
                string[] elements = WorldData.Elements[n].Length == 0 ? Array.Empty<string>() : WorldData.Elements[n].Split(',');
                all[n] = new World(WorldData.Ids[n], WorldData.Names[n], elements, n,
                    WorldData.Offsets[n + 1] - WorldData.Offsets[n]);
                byId[all[n].Id] = all[n];
            }
            _all = all;
            _byId = byId;
        }

        static LevelDef Decode(int flat)
        {
            string text = WorldData.Boards[flat];
            if (text.Length != Grid.Cells) throw new InvalidOperationException($"world level {flat}: baked board has {text.Length} cells");
            var board = new byte[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++) board[loc] = (byte)(text[loc] - '0');
            string[] names = WorldData.Pieces[flat].Split(',');
            var tiles = new Tile[names.Length];
            for (int k = 0; k < names.Length; k++) tiles[k] = ClassicLevels.ParseTile(names[k]);
            return new LevelDef(board, tiles);
        }
    }
}
