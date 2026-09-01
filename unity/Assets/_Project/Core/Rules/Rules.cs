namespace GridInfect.Core
{
    // Line-faithful port of Game.cpp (grid-infect-cocos2dx/Classes/Core),
    // spec in docs/RULES.md, proven by replaying every golden board in
    // docs/test_vectors.json. Only actions call these mutators.
    public static class Rules
    {
        // Game::getBoardPosition — -1 out of bounds
        public static int GetBoardPosition(LevelSession s, int i, int j)
        {
            if (!Grid.InBounds(i, j)) return -1;
            return s.Board[Grid.Loc(i, j)];
        }

        // Game::changeBoard — writes only in-bounds, non-void, differing cells
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

        // LevelMenu::ccTouchEnded legality: cell holds 1 or 4, piece in tray,
        // no other placed piece on the cell
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

        // Game::setPiece — clear-then-place, fresh repel queue and trip flag,
        // spread now; consequences wait for board.resolve
        public static void SetPiece(LevelSession s, int pieceIndex, int i, int j)
        {
            ClearPiece(s, pieceIndex);

            s.RepelQueue.Clear();
            s.ResetTripped = false;

            s.Pieces[pieceIndex].Placed = true;
            s.Pieces[pieceIndex].I = (sbyte)i;
            s.Pieces[pieceIndex].J = (sbyte)j;

            PropagatePiece(s, pieceIndex, fireEvents: true);
            s.ResolutionPending = true;
        }

        public static void Resolve(LevelSession s)
        {
            s.ResolutionPending = false;
            ResolveCore(s);
        }

        // Game::delayThenCheckForWin — order is contract (RULES §4.1): win
        // first, else reset if tripped, else repels; running the queue does
        // not empty it (only the next placement does)
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

        // Game::clearPiece — undo (RULES §7): row/col retraction with 99
        // marks, re-propagate remaining pieces in index order with a
        // synchronous resolution each (queue accumulates; can win or full-
        // reset mid-undo), then 99 reversion and a resync event per cell
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

        // Game::propagatePiece — offset-major rings 1..10, inner order
        // TileArms.SpreadOrder; 2/3/5 stop a direction, 99 skips without stopping,
        // voids and edges are passed over (infection jumps gaps)
        public static void PropagatePiece(LevelSession s, int pieceIndex, bool fireEvents)
        {
            ref PieceState piece = ref s.Pieces[pieceIndex];
            ChangeBoard(s, piece.I, piece.J, Cell.Infected, fireEvents);

            System.Span<bool> stopped = stackalloc bool[4];

            for (int offset = 1; offset <= Grid.SpreadRange; offset++)
            {
                for (int n = 0; n < TileArms.SpreadOrder.Length; n++)
                {
                    Dir dir = TileArms.SpreadOrder[n];
                    int d = (int)dir;
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
                        // skip, don't stop
                    }
                    else
                    {
                        ChangeBoard(s, i, j, Cell.Infected, fireEvents);
                    }
                }
            }
        }

        // Game::propagateRepel — 4 -> 1 along the walk; only a placed piece
        // stops it (walls and voids are walked over)
        public static void PropagateRepel(LevelSession s, Repel repel)
        {
            for (int offset = 1; offset <= Grid.SpreadRange; offset++)
            {
                int i = repel.I + TileArms.Di(repel.Direction) * offset;
                int j = repel.J + TileArms.Dj(repel.Direction) * offset;

                for (int k = 0; k < s.Pieces.Length; k++)
                {
                    if (s.Pieces[k].Placed && s.Pieces[k].I == i && s.Pieces[k].J == j)
                        return;
                }

                if (GetBoardPosition(s, i, j) == Cell.Infected)
                {
                    ChangeBoard(s, i, j, Cell.Active, fireEvents: true);
                }
            }
        }

        // Game::checkForWin — only value 1 blocks; 99 marks don't, so a
        // mid-undo check can "win" early (faithful quirk, ARCHITECTURE §5)
        public static bool CheckForWin(LevelSession s)
        {
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                if (s.Board[loc] == Cell.Active) return false;
            }
            return true;
        }

        // Game::resetBoard — on the cleared piece's row/col: 4 -> 1, and
        // 1 -> 99 so re-propagation can't infect what was uninfected
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

        // Game::fullReset — 4 -> 1 everywhere, all pieces to the tray
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
