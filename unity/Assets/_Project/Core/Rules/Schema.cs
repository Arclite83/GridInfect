using System;

namespace GridInfect.Core
{
    // Ordinal order is contract: the rand()%15 domain and the original enum order. Never reorder.
    public enum Tile : byte
    {
        L, R, U, D, LR, LU, LD, RU, RD, UD, LRU, LRD, LUD, RUD, LRUD
    }

    // Ordinal order indexes the save arrays. Never reorder.
    public enum Difficulty : byte
    {
        Beginner, Easy, Medium, Hard, Challenging
    }

    public enum GameMode : byte
    {
        Classic, FreePlay, World, Daily, Endless
    }

    // The four cardinal arms are the classic contract (bits 0..3 of TileArms);
    // the diagonals (stage 11) extend the enum without touching them.
    public enum Dir : byte
    {
        L, R, U, D, UL, UR, DL, DR
    }

    // Wire values of the original int board (RULES §1.1). 2/3/5 never change during play.
    public static class Cell
    {
        public const byte Void = 0;
        public const byte Active = 1;
        public const byte Wall = 2;
        public const byte RepelSwitch = 3;
        public const byte Infected = 4;
        public const byte ResetTrap = 5;
        public const byte Forbidden = 6;    // RulesV2 (stage 10): must stay clean; a spread that would hit it is illegal
        public const byte UndoMark = 99;
    }

    public static class Grid
    {
        // Portrait (R-1103). The original shipped 11 wide by 6 tall; the board
        // was transposed by (i, j) -> (j, i) with piece arms remapped L<->U and
        // R<->D, which is an exact conjugate of the original rules — the
        // recorded solutions in docs/test_vectors.json still replay step for
        // step. Applied once by tools/transpose_board_to_portrait.py.
        public const int Width = 6;
        public const int Height = 11;
        public const int Cells = Width * Height;
        public const int SpreadRange = 10; // >= max(Width, Height) - 1: an unobstructed arm reaches the edge

        public static int Loc(int i, int j) => i * Width + j;
        public static bool InBounds(int i, int j) => i >= 0 && i < Height && j >= 0 && j < Width;
    }

    public static class TileArms
    {
        static readonly byte[] Arms =
        {
            //  L  R  U  D   LR  LU  LD  RU  RD  UD  LRU LRD LUD RUD LRUD
            1, 2, 4, 8, 3, 5, 9, 6, 10, 12, 7, 11, 13, 14, 15
        };

        public static bool Has(Tile tile, Dir dir) => (Arms[(int)tile] & (1 << (int)dir)) != 0;

        public static int Mask(Tile tile) => Arms[(int)tile];

        // The tile with exactly these arms (bit = (int)Dir); throws on 0.
        public static Tile FromMask(int mask)
        {
            for (int t = 0; t < Arms.Length; t++)
            {
                if (Arms[t] == mask) return (Tile)t;
            }
            throw new ArgumentException($"no tile has arm mask {mask}");
        }

        public static int Count(Tile tile)
        {
            int a = Arms[(int)tile], n = 0;
            while (a != 0) { n += a & 1; a >>= 1; }
            return n;
        }

        // Game.cpp visits a piece's arms L,R,U,D. The board is transposed from
        // the original's landscape shape (Grid), and under that map the order
        // is U,D,L,R. It travels with the board because it is observable: the
        // repel queue is built in this order and RULES §4.1 resolves it in
        // queue order, and the generator spends one RNG draw per step.
        public static readonly Dir[] SpreadOrder = { Dir.U, Dir.D, Dir.L, Dir.R };

        // RulesV2 visits the cardinal arms in the classic order, then the
        // diagonals; the repel queue is built in this order.
        public static readonly Dir[] SpreadOrderV2 = { Dir.U, Dir.D, Dir.L, Dir.R, Dir.UL, Dir.UR, Dir.DL, Dir.DR };

        public static Dir Opposite(Dir dir)
        {
            switch (dir)
            {
                case Dir.L: return Dir.R;
                case Dir.R: return Dir.L;
                case Dir.U: return Dir.D;
                case Dir.D: return Dir.U;
                case Dir.UL: return Dir.DR;
                case Dir.UR: return Dir.DL;
                case Dir.DL: return Dir.UR;
                default: return Dir.UL;
            }
        }

        public static int Di(Dir dir) => dir == Dir.U || dir == Dir.UL || dir == Dir.UR ? -1
            : dir == Dir.D || dir == Dir.DL || dir == Dir.DR ? 1 : 0;

        public static int Dj(Dir dir) => dir == Dir.L || dir == Dir.UL || dir == Dir.DL ? -1
            : dir == Dir.R || dir == Dir.UR || dir == Dir.DR ? 1 : 0;

        public static bool IsDiagonal(Dir dir) => (int)dir >= (int)Dir.UL;
    }

    // A piece for RulesV2: a set of arms that all reach to the edge, either
    // cardinal (L, R, U, D: the fifteen classic tiles) or diagonal (UL, UR,
    // DL, DR) but never both, or the 3x3 area blot (stage 9). There is no
    // per-arm reach: the blot is the one short-range piece. Text form: the
    // cardinal arms as a tile name ("LRD"), or "+"-joined diagonal tokens
    // ("ul+dr"), or "A" the area — e.g. "LRD", "ul+dr", "A", "L+A".
    public readonly struct PieceSpec : IEquatable<PieceSpec>
    {
        public readonly byte Arms;    // bit = (int)Dir
        public readonly bool Area;

        public const byte CardinalMask = 0x0F;
        public const byte DiagonalMask = 0xF0;

        public PieceSpec(byte arms, bool area = false)
        {
            if ((arms & CardinalMask) != 0 && (arms & DiagonalMask) != 0)
                throw new ArgumentException("a piece has cardinal arms or diagonal arms, never both");
            Arms = arms;
            Area = area;
        }

        public bool Has(Dir dir) => (Arms & (1 << (int)dir)) != 0;
        public bool IsEmpty => Arms == 0 && !Area;
        public bool HasCardinal => (Arms & CardinalMask) != 0;
        public bool HasDiagonal => (Arms & DiagonalMask) != 0;

        public static PieceSpec FromTile(Tile tile) => new PieceSpec((byte)TileArms.Mask(tile));

        public PieceSpec WithArm(Dir dir, bool present = true) =>
            new PieceSpec((byte)(present ? Arms | (1 << (int)dir) : Arms & ~(1 << (int)dir)), Area);

        public PieceSpec WithArea(bool area) => new PieceSpec(Arms, area);

        // Exactly a classic tile: cardinal, no area, at least one arm.
        public bool IsTile => HasCardinal && !Area;

        public Tile ToTile() => TileArms.FromMask(Arms & CardinalMask);

        // The cardinal part as a tile for code that only draws arms; Tile.L
        // when there is none (callers check IsTile first).
        public Tile Projection => HasCardinal ? TileArms.FromMask(Arms & CardinalMask) : Tile.L;

        public static readonly string[] DiagonalNames = { "ul", "ur", "dl", "dr" };

        public string Encode()
        {
            var sb = new System.Text.StringBuilder();
            if (HasCardinal) sb.Append(TileArms.FromMask(Arms & CardinalMask).ToString());
            for (int d = 4; d < 8; d++)
            {
                if (!Has((Dir)d)) continue;
                if (sb.Length > 0) sb.Append('+');
                sb.Append(DiagonalNames[d - 4]);
            }
            if (Area)
            {
                if (sb.Length > 0) sb.Append('+');
                sb.Append('A');
            }
            return sb.ToString();
        }

        public static PieceSpec Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new FormatException("empty piece spec");
            int arms = 0;
            bool area = false;
            foreach (string raw in text.Split('+'))
            {
                string token = raw.Trim();
                if (token == "A") { area = true; continue; }
                if (token.Length > 0 && char.IsDigit(token[token.Length - 1]))
                    throw new FormatException($"reach on '{token}': every arm reaches the edge");
                int diag = Array.IndexOf(DiagonalNames, token);
                if (diag >= 0) arms |= 1 << (4 + diag);
                else arms |= TileArms.Mask(ClassicLevels.ParseTile(token));
            }
            if ((arms & CardinalMask) != 0 && (arms & DiagonalMask) != 0)
                throw new FormatException($"piece spec '{text}' mixes cardinal and diagonal arms");
            if (arms == 0 && !area) throw new FormatException($"piece spec '{text}' has no arms and no area");
            return new PieceSpec((byte)arms, area);
        }

        public bool Equals(PieceSpec other) => Arms == other.Arms && Area == other.Area;
        public override bool Equals(object obj) => obj is PieceSpec o && Equals(o);
        public override int GetHashCode() => Arms ^ (Area ? 1 << 8 : 0);
        public static bool operator ==(PieceSpec a, PieceSpec b) => a.Equals(b);
        public static bool operator !=(PieceSpec a, PieceSpec b) => !a.Equals(b);
        public override string ToString() => Encode();
    }

    public sealed class LevelDef
    {
        public const int MaxPieces = 8; // tray capacity (renderer allocates 8 slots)

        readonly byte[] _board;
        readonly byte[] _cellData;

        public readonly int ClassicId;
        // The classic view of the pieces (V1 rules read PieceState.Tile). For
        // V2 pieces that are not classic tiles this is a projection; V2 code
        // reads Specs.
        public readonly Tile[] Pieces;
        public readonly PieceSpec[] Specs;
        // 1 = the frozen classic rules; 2 = RulesV2 (docs/RULES_V2.md).
        public readonly int Version;

        public LevelDef(byte[] board, Tile[] pieces, int classicId = -1)
        {
            if (board == null || board.Length != Grid.Cells)
                throw new ArgumentException($"board must have exactly {Grid.Cells} cells");
            if (pieces == null || pieces.Length < 1 || pieces.Length > MaxPieces)
                throw new ArgumentException($"piece count must be 1..{MaxPieces}");
            foreach (byte v in board)
            {
                if (v != Cell.Void && v != Cell.Active && v != Cell.Wall &&
                    v != Cell.RepelSwitch && v != Cell.ResetTrap)
                    throw new ArgumentException($"illegal cell value {v} in level definition");
            }
            foreach (Tile t in pieces)
            {
                if ((byte)t > (byte)Tile.LRUD)
                    throw new ArgumentException($"illegal tile value {(byte)t}");
            }
            _board = (byte[])board.Clone();
            _cellData = new byte[Grid.Cells];
            Pieces = (Tile[])pieces.Clone();
            Specs = new PieceSpec[pieces.Length];
            for (int k = 0; k < pieces.Length; k++) Specs[k] = PieceSpec.FromTile(pieces[k]);
            ClassicId = classicId;
            Version = 1;
        }

        // A V2 level: forbidden cells allowed, relay arms in cellData (a
        // non-zero arm mask on an active cell, stage 12), any PieceSpec.
        public LevelDef(byte[] board, PieceSpec[] specs, byte[] cellData = null)
        {
            if (board == null || board.Length != Grid.Cells)
                throw new ArgumentException($"board must have exactly {Grid.Cells} cells");
            if (specs == null || specs.Length < 1 || specs.Length > MaxPieces)
                throw new ArgumentException($"piece count must be 1..{MaxPieces}");
            if (cellData != null && cellData.Length != Grid.Cells)
                throw new ArgumentException($"cell data must have exactly {Grid.Cells} cells");
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                byte v = board[loc];
                if (v != Cell.Void && v != Cell.Active && v != Cell.Wall &&
                    v != Cell.RepelSwitch && v != Cell.ResetTrap && v != Cell.Forbidden)
                    throw new ArgumentException($"illegal cell value {v} in level definition");
                if (cellData != null && cellData[loc] != 0 && v != Cell.Active)
                    throw new ArgumentException($"relay arms on a non-active cell at {loc}");
            }
            foreach (PieceSpec spec in specs)
            {
                if (spec.IsEmpty) throw new ArgumentException("empty piece spec");
            }
            _board = (byte[])board.Clone();
            _cellData = cellData != null ? (byte[])cellData.Clone() : new byte[Grid.Cells];
            Specs = (PieceSpec[])specs.Clone();
            Pieces = new Tile[specs.Length];
            for (int k = 0; k < specs.Length; k++) Pieces[k] = specs[k].Projection;
            ClassicId = -1;
            Version = 2;
        }

        public byte BoardAt(int loc) => _board[loc];

        // Relay arm mask (bit = (int)Dir) on an active cell; 0 elsewhere.
        public byte CellDataAt(int loc) => _cellData[loc];

        public bool HasRelays
        {
            get
            {
                for (int loc = 0; loc < Grid.Cells; loc++) if (_cellData[loc] != 0) return true;
                return false;
            }
        }

        public void CopyBoardTo(byte[] target) => Array.Copy(_board, target, Grid.Cells);

        public void CopyCellDataTo(byte[] target) => Array.Copy(_cellData, target, Grid.Cells);

        // The same pieces and cell data on another board (the generator's pruners).
        public LevelDef WithBoard(byte[] board) => new LevelDef(board, Specs, _cellData);
    }

    public readonly struct Repel
    {
        public readonly sbyte I;
        public readonly sbyte J;
        public readonly Dir Direction;

        public Repel(int i, int j, Dir direction)
        {
            I = (sbyte)i;
            J = (sbyte)j;
            Direction = direction;
        }
    }

    public struct PieceState
    {
        public Tile Tile;
        public bool Placed;
        public sbyte I;
        public sbyte J;
        public bool Locked;   // placed by the Lock tool: cannot be lifted, survives a full reset
    }
}
