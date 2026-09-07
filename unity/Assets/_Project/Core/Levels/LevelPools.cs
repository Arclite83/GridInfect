using System;

namespace GridInfect.Core
{
    // Shared decoding for baked and cached levels (WorldData, LevelCache).
    internal static class LevelPools
    {
        public static (int piece, int cell)[] Pairs(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<(int, int)>();
            string[] parts = text.Split(' ');
            var result = new (int, int)[parts.Length];
            for (int n = 0; n < parts.Length; n++)
            {
                string[] pc = parts[n].Split('@');
                result[n] = (int.Parse(pc[0]), int.Parse(pc[1]));
            }
            return result;
        }

        public static string PairsText((int piece, int cell)[] pairs)
        {
            var parts = new string[pairs.Length];
            for (int n = 0; n < pairs.Length; n++) parts[n] = pairs[n].piece + "@" + pairs[n].cell;
            return string.Join(" ", parts);
        }

        public static LevelDef Decode(string boardText, string piecesText, string relaysText)
        {
            if (boardText.Length != Grid.Cells) throw new InvalidOperationException($"baked board has {boardText.Length} cells");
            var board = new byte[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++) board[loc] = (byte)(boardText[loc] - '0');
            // Baked and generated content runs on RulesV2; Legacy stays on the classic rules.
            string[] names = piecesText.Split(',');
            var specs = new PieceSpec[names.Length];
            for (int k = 0; k < names.Length; k++) specs[k] = PieceSpec.Parse(names[k]);
            byte[] cellData = null;
            if (!string.IsNullOrEmpty(relaysText))
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

        public static string BoardText(LevelDef def)
        {
            var chars = new char[Grid.Cells];
            for (int loc = 0; loc < Grid.Cells; loc++) chars[loc] = (char)('0' + def.BoardAt(loc));
            return new string(chars);
        }

        public static string PiecesText(LevelDef def)
        {
            var names = new string[def.Specs.Length];
            for (int k = 0; k < names.Length; k++) names[k] = def.Specs[k].Encode();
            return string.Join(",", names);
        }

        public static string RelaysText(LevelDef def)
        {
            if (!def.HasRelays) return "";
            var parts = new System.Collections.Generic.List<string>();
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (def.CellDataAt(loc) != 0) parts.Add(loc + ":" + def.CellDataAt(loc));
            }
            return string.Join(" ", parts);
        }
    }
}
