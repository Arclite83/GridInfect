using System;

namespace GridInfect.Core
{
    /// <summary>
    /// The 128 shipped classic levels. Data lives in ClassicLevelData.g.cs,
    /// generated from docs/test_vectors.json by tools/bake_levels.py — the
    /// vectors are the single source of truth; nothing here is
    /// hand-transcribed (REQUIREMENTS R-102). Boards are 66-char strings of
    /// cell digits; pieces are comma-joined tile names.
    /// </summary>
    public static class ClassicLevels
    {
        public const int Count = 128;

        static readonly LevelDef[] Cache = new LevelDef[Count];

        public static LevelDef Get(int id)
        {
            if (id < 0 || id >= Count) throw new ArgumentOutOfRangeException(nameof(id));
            return Cache[id] ?? (Cache[id] = Decode(id));
        }

        static LevelDef Decode(int id)
        {
            string boardText = ClassicLevelData.Boards[id];
            if (boardText.Length != Grid.Cells)
                throw new InvalidOperationException($"level {id}: baked board has {boardText.Length} cells");

            var board = new byte[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                char c = boardText[loc];
                if (c < '0' || c > '9')
                    throw new InvalidOperationException($"level {id}: bad cell char '{c}'");
                board[loc] = (byte)(c - '0');
            }

            string[] names = ClassicLevelData.Pieces[id].Split(',');
            var tiles = new Tile[names.Length];
            for (int k = 0; k < names.Length; k++)
            {
                tiles[k] = ParseTile(names[k]);
            }

            return new LevelDef(board, tiles, id);
        }

        public static Tile ParseTile(string name)
        {
            switch (name)
            {
                case "L": return Tile.L;
                case "R": return Tile.R;
                case "U": return Tile.U;
                case "D": return Tile.D;
                case "LR": return Tile.LR;
                case "LU": return Tile.LU;
                case "LD": return Tile.LD;
                case "RU": return Tile.RU;
                case "RD": return Tile.RD;
                case "UD": return Tile.UD;
                case "LRU": return Tile.LRU;
                case "LRD": return Tile.LRD;
                case "LUD": return Tile.LUD;
                case "RUD": return Tile.RUD;
                case "LRUD": return Tile.LRUD;
                default: throw new FormatException($"unknown tile name '{name}'");
            }
        }
    }
}
