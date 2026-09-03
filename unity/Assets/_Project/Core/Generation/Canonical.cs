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
        public static string Encode(LevelDef def)
        {
            string best = null;
            for (int t = 0; t < 4; t++)
            {
                string text = Transform(def, flipH: (t & 1) != 0, flipV: (t & 2) != 0);
                if (best == null || string.CompareOrdinal(text, best) < 0) best = text;
            }
            return best;
        }

        public static string Hash(LevelDef def) => Fnv1a64(Encode(def)).ToString("x16");

        public static string Transform(LevelDef def, bool flipH, bool flipV)
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
            var tiles = new int[def.Pieces.Length];
            for (int k = 0; k < tiles.Length; k++)
            {
                int mask = TileArms.Mask(def.Pieces[k]);
                if (flipH) mask = Swap(mask, Dir.L, Dir.R);
                if (flipV) mask = Swap(mask, Dir.U, Dir.D);
                tiles[k] = (int)TileArms.FromMask(mask);
            }
            Array.Sort(tiles);
            for (int k = 0; k < tiles.Length; k++)
            {
                if (k > 0) sb.Append(',');
                sb.Append(tiles[k]);
            }
            return sb.ToString();
        }

        static int Swap(int mask, Dir a, Dir b)
        {
            int ba = 1 << (int)a, bb = 1 << (int)b;
            bool hasA = (mask & ba) != 0, hasB = (mask & bb) != 0;
            mask &= ~(ba | bb);
            if (hasA) mask |= bb;
            if (hasB) mask |= ba;
            return mask;
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
