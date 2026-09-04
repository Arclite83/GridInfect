using System;
using System.Text;

namespace GridInfect.Core.Generation
{
    // Canonical form under the board's symmetry group: identity, horizontal
    // flip (arms L<->R), vertical flip (U<->D), and the 180° turn (both).
    // Tiles are sorted by ordinal, so tray order does not split a level into
    // several. The hash is FNV-1a 64 of the smallest of the four encodings.
    public static class Canonical
    {
        public static string Encode(LevelDef def, (int piece, int cell)[] locks = null)
        {
            string best = null;
            for (int t = 0; t < 4; t++)
            {
                string text = Transform(def, flipH: (t & 1) != 0, flipV: (t & 2) != 0, locks);
                if (best == null || string.CompareOrdinal(text, best) < 0) best = text;
            }
            return best;
        }

        public static string Hash(LevelDef def, (int piece, int cell)[] locks = null) => Fnv1a64(Encode(def, locks)).ToString("x16");

        public static string Transform(LevelDef def, bool flipH, bool flipV, (int piece, int cell)[] locks = null)
        {
            var sb = new StringBuilder(Grid.Cells + 1 + def.Pieces.Length * 3);
            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    int si = flipV ? Grid.Height - 1 - i : i;
                    int sj = flipH ? Grid.Width - 1 - j : j;
                    sb.Append((char)('0' + def.BoardAt(Grid.Loc(si, sj))));
                }
            }
            sb.Append('|');
            // Classic tiles keep the stage-2 encoding (sorted ordinals), so
            // every hash in docs/worlds stays valid; anything else encodes
            // the transformed specs, sorted.
            bool allTiles = true;
            foreach (PieceSpec spec in def.Specs) allTiles &= spec.IsTile;
            var keys = new string[def.Specs.Length];
            for (int k = 0; k < keys.Length; k++)
            {
                PieceSpec spec = Flip(def.Specs[k], flipH, flipV);
                keys[k] = allTiles ? ((int)spec.ToTile()).ToString("D2") : spec.Encode();
            }
            Array.Sort(keys, string.CompareOrdinal);
            for (int k = 0; k < keys.Length; k++)
            {
                if (k > 0) sb.Append(',');
                sb.Append(allTiles ? int.Parse(keys[k]).ToString() : keys[k]);
            }
            if (def.HasRelays)
            {
                sb.Append('|');
                for (int i = 0; i < Grid.Height; i++)
                {
                    for (int j = 0; j < Grid.Width; j++)
                    {
                        int si = flipV ? Grid.Height - 1 - i : i;
                        int sj = flipH ? Grid.Width - 1 - j : j;
                        int arms = def.CellDataAt(Grid.Loc(si, sj));
                        sb.Append(arms == 0 ? "0" : FlipArms(arms, flipH, flipV).ToString("x2"));
                    }
                }
            }
            // Pre-placed pieces: the piece's transformed spec at its
            // transformed cell, sorted, so a level with a lock never shares
            // a hash with the same board without one.
            if (locks != null && locks.Length > 0)
            {
                var entries = new string[locks.Length];
                for (int n = 0; n < locks.Length; n++)
                {
                    int i = locks[n].cell / Grid.Width, j = locks[n].cell % Grid.Width;
                    int ti = flipV ? Grid.Height - 1 - i : i;
                    int tj = flipH ? Grid.Width - 1 - j : j;
                    entries[n] = Flip(def.Specs[locks[n].piece], flipH, flipV).Encode() + "@" + Grid.Loc(ti, tj);
                }
                Array.Sort(entries, string.CompareOrdinal);
                sb.Append("|lock:").Append(string.Join(",", entries));
            }
            return sb.ToString();
        }

        // Arms move with the board: a horizontal flip swaps L/R (and UL/UR,
        // DL/DR), a vertical one U/D (and UL/DL, UR/DR); reach travels with
        // its arm.
        public static PieceSpec Flip(PieceSpec spec, bool flipH, bool flipV)
        {
            var result = new PieceSpec(0, 0, spec.Area);
            for (int d = 0; d < 8; d++)
            {
                var dir = (Dir)d;
                if (!spec.Has(dir)) continue;
                var to = FlipDir(dir, flipH, flipV);
                result = result.WithArm(to).WithReach(to, spec.ReachOf(dir));
            }
            return result;
        }

        public static int FlipArms(int arms, bool flipH, bool flipV)
        {
            int result = 0;
            for (int d = 0; d < 8; d++)
            {
                if ((arms & (1 << d)) != 0) result |= 1 << (int)FlipDir((Dir)d, flipH, flipV);
            }
            return result;
        }

        public static Dir FlipDir(Dir dir, bool flipH, bool flipV)
        {
            int di = TileArms.Di(dir), dj = TileArms.Dj(dir);
            if (flipH) dj = -dj;
            if (flipV) di = -di;
            for (int d = 0; d < 8; d++)
            {
                if (TileArms.Di((Dir)d) == di && TileArms.Dj((Dir)d) == dj) return (Dir)d;
            }
            throw new ArgumentException("no such direction");
        }

        public static ulong Fnv1a64(string text)
        {
            ulong h = 14695981039346656037ul;
            foreach (char c in text)
            {
                h ^= c;
                h *= 1099511628211ul;
            }
            return h;
        }
    }
}
