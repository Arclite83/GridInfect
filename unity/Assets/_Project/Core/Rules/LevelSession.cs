using System;
using System.Collections.Generic;

namespace GridInfect.Core
{
    /// <summary>
    /// The live play state of one level: the working board, piece placements,
    /// the repel queue, and the resolution flags. Mutated exclusively by
    /// <see cref="Rules"/>, which is called exclusively by actions.
    ///
    /// Events mirror the original EventHandler surface (RULES.md §10): the
    /// Unity layer is a pure listener. Replays with no listeners attached pay
    /// nothing.
    /// </summary>
    public sealed class LevelSession
    {
        public readonly LevelDef Def;
        public readonly byte[] Board = new byte[Grid.Cells];
        public readonly PieceState[] Pieces;
        public readonly List<Repel> RepelQueue = new List<Repel>(8);

        /// <summary>Set when spread reaches a reset trap; cleared at the next placement.</summary>
        public bool ResetTripped;

        /// <summary>
        /// True between a placement and its resolution — the model's name for
        /// the original's 0.3 s presentation beat. While pending, no other
        /// board action may run; adapters fast-forward by dispatching
        /// board.resolve first (REQUIREMENTS R-107: the resolution is never
        /// cancellable).
        /// </summary>
        public bool ResolutionPending;

        /// <summary>Result of the most recent resolution's win check (RULES.md §8).</summary>
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
