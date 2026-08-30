using System;

namespace GridInfect.Core
{
    /// <summary>
    /// The 15 piece types — every non-empty combination of the four cardinal
    /// arms. Ordinal order is contract: it is the rand()%15 domain in the
    /// generator and the original C enum order (Enums.h). Never reorder.
    /// </summary>
    public enum Tile : byte
    {
        L, R, U, D, LR, LU, LD, RU, RD, UD, LRU, LRD, LUD, RUD, LRUD
    }

    /// <summary>Ordinal order indexes the save arrays (original Enums.h order). Never reorder.</summary>
    public enum Difficulty : byte
    {
        Beginner, Easy, Medium, Hard, Challenging
    }

    public enum GameMode : byte
    {
        Classic, FreePlay
    }

    /// <summary>Cardinal direction of spread / repel travel.</summary>
    public enum Dir : byte
    {
        L, R, U, D
    }

    /// <summary>
    /// Board cell values — the wire values of the original int board, kept
    /// verbatim (RULES.md §1.1). 2/3/5 are immutable during play; 1 ↔ 4
    /// (and transiently 99, inside undo only) are the mutable states.
    /// </summary>
    public static class Cell
    {
        public const byte Void = 0;
        public const byte Active = 1;
        public const byte Wall = 2;
        public const byte RepelSwitch = 3;
        public const byte Infected = 4;
        public const byte ResetTrap = 5;
        public const byte UndoMark = 99;
    }

    /// <summary>Fixed board geometry (RULES.md §1): 6 rows × 11 columns, row-major.</summary>
    public static class Grid
    {
        public const int Width = 11;
        public const int Height = 6;
        public const int Cells = Width * Height;
        public const int SpreadRange = 10; // >= max(Width, Height) - 1: an unobstructed arm reaches the edge

        public static int Loc(int i, int j) => i * Width + j;
        public static bool InBounds(int i, int j) => i >= 0 && i < Height && j >= 0 && j < Width;
    }

    /// <summary>Arm lookup for tiles. Bit order L=1, R=2, U=4, D=8, table in Tile ordinal order.</summary>
    public static class TileArms
    {
        static readonly byte[] Arms =
        {
            //  L  R  U  D   LR  LU  LD  RU  RD  UD  LRU LRD LUD RUD LRUD
            1, 2, 4, 8, 3, 5, 9, 6, 10, 12, 7, 11, 13, 14, 15
        };

        public static bool Has(Tile tile, Dir dir) => (Arms[(int)tile] & (1 << (int)dir)) != 0;

        public static int Count(Tile tile)
        {
            int a = Arms[(int)tile], n = 0;
            while (a != 0) { n += a & 1; a >>= 1; }
            return n;
        }

        public static Dir Opposite(Dir dir)
        {
            switch (dir)
            {
                case Dir.L: return Dir.R;
                case Dir.R: return Dir.L;
                case Dir.U: return Dir.D;
                default: return Dir.U;
            }
        }

        /// <summary>Row delta for one step in a direction.</summary>
        public static int Di(Dir dir) => dir == Dir.U ? -1 : dir == Dir.D ? 1 : 0;

        /// <summary>Column delta for one step in a direction.</summary>
        public static int Dj(Dir dir) => dir == Dir.L ? -1 : dir == Dir.R ? 1 : 0;
    }

    /// <summary>
    /// Immutable level definition: the board layout and the ordered piece
    /// list. Invariants are enforced here so nothing downstream ever sees an
    /// unvalidated shape.
    /// </summary>
    public sealed class LevelDef
    {
        public const int MaxPieces = 8; // tray capacity (renderer allocates 8 slots)

        readonly byte[] _board;

        /// <summary>Classic level id 0..127, or -1 for generated levels.</summary>
        public readonly int ClassicId;
        public readonly Tile[] Pieces;

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
            Pieces = (Tile[])pieces.Clone();
            ClassicId = classicId;
        }

        public byte BoardAt(int loc) => _board[loc];

        /// <summary>Copy the initial board into a session's working array.</summary>
        public void CopyBoardTo(byte[] target) => Array.Copy(_board, target, Grid.Cells);
    }

    /// <summary>A queued repel: origin switch cell and travel direction (back along the line).</summary>
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

    /// <summary>Placement state of one piece: in the tray (I=J=-1) or on the board.</summary>
    public struct PieceState
    {
        public Tile Tile;
        public bool Placed;
        public sbyte I;
        public sbyte J;
    }
}
