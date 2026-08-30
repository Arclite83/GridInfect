namespace GridInfect.Core
{
    /// <summary>
    /// The mechanical rules of Grid Infect — a line-faithful port of
    /// Game.cpp (grid-infect-cocos2dx/Classes/Core), specified in
    /// docs/RULES.md and proven equivalent by replaying every per-step golden
    /// board in docs/test_vectors.json.
    ///
    /// These are the pure decision/mutation functions actions apply. Nothing
    /// here is public policy: only actions call into this class.
    ///
    /// Deliberate divergence from the original, none of which changes board
    /// math: the 0.3 s resolution-cancellation quirk is not ported (confirmed
    /// bug, REQUIREMENTS R-107) — resolution state is modeled explicitly as
    /// LevelSession.ResolutionPending and always resolves.
    /// </summary>
    public static class Rules
    {
        /// <summary>Game::getBoardPosition — -1 when out of bounds.</summary>
        public static int GetBoardPosition(LevelSession s, int i, int j)
        {
            if (!Grid.InBounds(i, j)) return -1;
            return s.Board[Grid.Loc(i, j)];
        }

        /// <summary>
        /// Game::changeBoard — writes only in-bounds, non-void cells whose
        /// value differs; fires the cell event on the placement path.
        /// </summary>
        public static bool ChangeBoard(LevelSession s, int i, int j, byte value, bool fireEvents)
        {
            if (!Grid.InBounds(i, j)) return false;
            int loc = Grid.Loc(i, j);
            if (s.Board[loc] != Cell.Void && s.Board[loc] != value)
            {
                s.Board[loc] = value;
                if (fireEvents) s.RaiseCellChanged(i, j, value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// LevelMenu::ccTouchEnded placement legality: the target must hold 1
        /// or 4, the piece must be in the tray, and no other placed piece may
        /// occupy the cell.
        /// </summary>
        public static bool CanPlace(LevelSession s, int pieceIndex, int i, int j)
        {
            if (pieceIndex < 0 || pieceIndex >= s.Pieces.Length) return false;
            if (s.Pieces[pieceIndex].Placed) return false;
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Placed && s.Pieces[k].I == i && s.Pieces[k].J == j) return false;
            }
            int bp = GetBoardPosition(s, i, j);
            return bp == Cell.Active || bp == Cell.Infected;
        }

        /// <summary>
        /// Game::setPiece — clear-then-place, reset the repel queue and trip
        /// flag, spread. Resolution (win check / reset / repels) is a separate
        /// step: the placement leaves the session ResolutionPending.
        /// </summary>
        public static void SetPiece(LevelSession s, int pieceIndex, int i, int j)
        {
            ClearPiece(s, pieceIndex); // no-op for a tray piece; literal safety in the original

            s.RepelQueue.Clear();
            s.ResetTripped = false;

            s.Pieces[pieceIndex].Placed = true;
            s.Pieces[pieceIndex].I = (sbyte)i;
            s.Pieces[pieceIndex].J = (sbyte)j;

            PropagatePiece(s, pieceIndex, fireEvents: true);
            s.ResolutionPending = true;
        }

        /// <summary>
        /// Game::delayThenCheckForWin, run via the board.resolve action after
        /// the presentation beat. Order is contract (RULES.md §4.1): win check
        /// first — a winning placement ignores tripped traps and queued
        /// repels — else full reset if tripped, else repels in queue order.
        /// Running the queue does not empty it (it is cleared at the next
        /// placement).
        /// </summary>
        public static void Resolve(LevelSession s)
        {
            s.ResolutionPending = false;
            ResolveCore(s);
        }

        static void ResolveCore(LevelSession s)
        {
            bool win = CheckForWin(s);
            s.Solved = win;
            if (win)
            {
                s.RaiseLevelSolved();
                return;
            }
            if (s.ResetTripped)
            {
                FullReset(s);
                return;
            }
            for (int r = 0; r < s.RepelQueue.Count; r++)
            {
                PropagateRepel(s, s.RepelQueue[r]);
            }
        }

        /// <summary>
        /// Game::clearPiece — the undo path (RULES.md §7), ported literally:
        /// row/column retraction with 99 marking, re-propagation of the
        /// remaining pieces in piece-index order each followed by a
        /// synchronous resolution (which re-runs the still-uncleared repel
        /// queue and can win or full-reset mid-undo), then 99 reversion and a
        /// renderer resync event per non-void cell.
        /// </summary>
        public static void ClearPiece(LevelSession s, int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= s.Pieces.Length) return;
            if (!s.Pieces[pieceIndex].Placed) return;

            ResetBoard(s, s.Pieces[pieceIndex].I, s.Pieces[pieceIndex].J);

            s.Pieces[pieceIndex].Placed = false;
            s.Pieces[pieceIndex].I = -1;
            s.Pieces[pieceIndex].J = -1;

            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Placed)
                {
                    PropagatePiece(s, k, fireEvents: false);
                    ResolveCore(s);
                }
            }

            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    int loc = Grid.Loc(i, j);
                    if (s.Board[loc] != Cell.Void)
                    {
                        if (s.Board[loc] == Cell.UndoMark) s.Board[loc] = Cell.Active;
                        s.RaiseCellChanged(i, j, s.Board[loc]);
                    }
                }
            }
        }

        /// <summary>
        /// Game::propagatePiece — offset-major rings 1..10, inner direction
        /// order L,R,U,D. Walls/switches/traps stop a direction; 99 is skipped
        /// without stopping; voids, edges, and infected cells are written over
        /// (no-op) without stopping — infection jumps gaps.
        /// </summary>
        public static void PropagatePiece(LevelSession s, int pieceIndex, bool fireEvents)
        {
            ref PieceState piece = ref s.Pieces[pieceIndex];
            ChangeBoard(s, piece.I, piece.J, Cell.Infected, fireEvents);

            System.Span<bool> stopped = stackalloc bool[4];

            for (int offset = 1; offset <= Grid.SpreadRange; offset++)
            {
                for (int d = 0; d < 4; d++)
                {
                    Dir dir = (Dir)d; // enum order L,R,U,D matches the fixed inner order
                    if (stopped[d] || !TileArms.Has(piece.Tile, dir)) continue;

                    int i = piece.I + TileArms.Di(dir) * offset;
                    int j = piece.J + TileArms.Dj(dir) * offset;
                    int bp = GetBoardPosition(s, i, j);
                    if (bp == Cell.Wall)
                    {
                        stopped[d] = true;
                    }
                    else if (bp == Cell.RepelSwitch)
                    {
                        stopped[d] = true;
                        s.RepelQueue.Add(new Repel(i, j, TileArms.Opposite(dir)));
                    }
                    else if (bp == Cell.ResetTrap)
                    {
                        stopped[d] = true;
                        s.ResetTripped = true;
                    }
                    else if (bp == Cell.UndoMark)
                    {
                        // skip: do not change, do not stop
                    }
                    else
                    {
                        ChangeBoard(s, i, j, Cell.Infected, fireEvents);
                    }
                }
            }
        }

        /// <summary>
        /// Game::propagateRepel — walk 1..10 from the switch; the whole repel
        /// stops at the first placed piece; 4 -> 1 along the way; nothing else
        /// stops it. Events always fire on this path (as in the original).
        /// </summary>
        public static void PropagateRepel(LevelSession s, Repel repel)
        {
            for (int offset = 1; offset <= Grid.SpreadRange; offset++)
            {
                int i = repel.I + TileArms.Di(repel.Direction) * offset;
                int j = repel.J + TileArms.Dj(repel.Direction) * offset;

                for (int k = 0; k < s.Pieces.Length; k++)
                {
                    if (s.Pieces[k].Placed && s.Pieces[k].I == i && s.Pieces[k].J == j)
                        return; // hit a piece: the entire repel is done
                }

                if (GetBoardPosition(s, i, j) == Cell.Infected)
                {
                    ChangeBoard(s, i, j, Cell.Active, fireEvents: true);
                }
            }
        }

        /// <summary>Game::checkForWin — solved when no cell holds value 1.</summary>
        public static bool CheckForWin(LevelSession s)
        {
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (s.Board[loc] == Cell.Active) return false;
            }
            return true;
        }

        /// <summary>
        /// Game::resetBoard — undo's row/column retraction: on the cleared
        /// piece's row or column, 4 -> 1 and 1 -> 99 (the 99 mark protects
        /// cells that were uninfected from re-propagation). Direct writes, no
        /// events (the undo path resyncs at the end).
        /// </summary>
        public static void ResetBoard(LevelSession s, int pieceI, int pieceJ)
        {
            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    if (i != pieceI && j != pieceJ) continue;
                    int loc = Grid.Loc(i, j);
                    if (s.Board[loc] == Cell.Active) s.Board[loc] = Cell.UndoMark;
                    else if (s.Board[loc] == Cell.Infected) s.Board[loc] = Cell.Active;
                }
            }
        }

        /// <summary>
        /// Game::fullReset — every infected cell reverts to active (event per
        /// cell), every piece returns to the tray, pieces-unbound fires.
        /// Static cells are untouched.
        /// </summary>
        public static void FullReset(LevelSession s)
        {
            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    int loc = Grid.Loc(i, j);
                    if (s.Board[loc] == Cell.Infected)
                    {
                        s.Board[loc] = Cell.Active;
                        s.RaiseCellChanged(i, j, s.Board[loc]);
                    }
                }
            }
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                s.Pieces[k].Placed = false;
                s.Pieces[k].I = -1;
                s.Pieces[k].J = -1;
            }
            s.RaisePiecesUnbound();
        }
    }
}
