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

    public enum Dir : byte
    {
        L, R, U, D
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

        public static int Di(Dir dir) => dir == Dir.U ? -1 : dir == Dir.D ? 1 : 0;

        public static int Dj(Dir dir) => dir == Dir.L ? -1 : dir == Dir.R ? 1 : 0;
    }

    public sealed class LevelDef
    {
        public const int MaxPieces = 8; // tray capacity (renderer allocates 8 slots)

        readonly byte[] _board;

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

        public void CopyBoardTo(byte[] target) => Array.Copy(_board, target, Grid.Cells);
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
    }
}
