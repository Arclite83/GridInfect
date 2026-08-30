using System;
using System.Collections.Generic;

namespace GridInfect.Core
{
    public sealed class LevelSession
    {
        public readonly LevelDef Def;
        public readonly byte[] Board = new byte[Grid.Cells];
        public readonly PieceState[] Pieces;
        public readonly List<Repel> RepelQueue = new List<Repel>(8);

        public bool ResetTripped;

        // The 0.3 s beat, reified: a placement leaves consequences pending until
        // board.resolve. Never cancellable — adapters fast-forward instead (R-107).
        public bool ResolutionPending;

        public bool Solved;

        public event Action<int, int, byte> CellChanged;   // onChangeBoardIndex(i, j, value)
        public event Action LevelSolved;                    // onLevelSolved
        public event Action PiecesUnbound;                  // onUnbindPieces

        public LevelSession(LevelDef def)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            def.CopyBoardTo(Board);
            Pieces = new PieceState[def.Pieces.Length];
            for (int k = 0; k < Pieces.Length; k++)
            {
                Pieces[k] = new PieceState { Tile = def.Pieces[k], Placed = false, I = -1, J = -1 };
            }
        }

        internal void RaiseCellChanged(int i, int j, byte value) => CellChanged?.Invoke(i, j, value);
        internal void RaiseLevelSolved() => LevelSolved?.Invoke();
        internal void RaisePiecesUnbound() => PiecesUnbound?.Invoke();
    }
}
