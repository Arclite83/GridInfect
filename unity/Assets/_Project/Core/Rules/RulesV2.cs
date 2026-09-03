using System;

namespace GridInfect.Core
{
    // RulesV2 (docs/RULES_V2.md): the classic placement path (rings of
    // arms, walls stop, switches repel, traps trip, voids are jumped, win
    // before reset before repels) generalised to PieceSpec — eight arm
    // directions, per-arm reach, the 3x3 area — plus forbidden cells and
    // relay cells, with a clean undo: restore the initial board and
    // re-propagate the placed pieces in index order. The repel queue is
    // fresh at every placement and emptied after it runs; the V1 queue
    // accumulation quirk is not carried. Legacy stays on Rules.
    public sealed class RulesV2 : IRules
    {
        public static readonly RulesV2 Instance = new RulesV2();

        public bool CanPlace(LevelSession s, int pieceIndex, int i, int j)
        {
            if (pieceIndex < 0 || pieceIndex >= s.Pieces.Length) return false;
            if (s.Pieces[pieceIndex].Placed) return false;
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Placed && s.Pieces[k].I == i && s.Pieces[k].J == j) return false;
            }
            int bp = Rules.GetBoardPosition(s, i, j);
            if (bp != Cell.Active && bp != Cell.Infected) return false;
            // A spread that would touch a forbidden cell is illegal (the
            // piece bounces back to the tray in the adapter).
            return !WouldHitForbidden(s, pieceIndex, i, j);
        }

        public void SetPiece(LevelSession s, int pieceIndex, int i, int j)
        {
            if (s.Pieces[pieceIndex].Placed) ClearPiece(s, pieceIndex);

            s.RepelQueue.Clear();
            s.ResetTripped = false;

            s.Pieces[pieceIndex].Placed = true;
            s.Pieces[pieceIndex].I = (sbyte)i;
            s.Pieces[pieceIndex].J = (sbyte)j;

            Propagate(s, s.Board, pieceIndex, fireEvents: true, forbiddenCheck: false);
            s.ResolutionPending = true;
        }

        public void Resolve(LevelSession s)
        {
            s.ResolutionPending = false;
            ResolveCore(s);
        }

        // RULES §4.1 order kept: win first, else reset if tripped, else the
        // repels in queue order — then the queue is done with.
        void ResolveCore(LevelSession s)
        {
            bool win = Rules.CheckForWin(s);
            s.Solved = win;
            if (win)
            {
                s.RepelQueue.Clear();
                s.RaiseLevelSolved();
                return;
            }
            if (s.ResetTripped)
            {
                s.RepelQueue.Clear();
                FullReset(s);
                return;
            }
            for (int r = 0; r < s.RepelQueue.Count; r++)
            {
                Rules.PropagateRepel(s, s.RepelQueue[r]);
            }
            s.RepelQueue.Clear();
        }

        // Undo: the board goes back to its initial state and every piece
        // still placed re-propagates in index order, then one resolution.
        public void ClearPiece(LevelSession s, int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= s.Pieces.Length) return;
            if (!s.Pieces[pieceIndex].Placed) return;

            s.Pieces[pieceIndex].Placed = false;
            s.Pieces[pieceIndex].I = -1;
            s.Pieces[pieceIndex].J = -1;
            s.Pieces[pieceIndex].Locked = false;

            Rebuild(s);
            ResolveCore(s);

            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    int loc = Grid.Loc(i, j);
                    if (s.Board[loc] != Cell.Void) s.RaiseCellChanged(i, j, s.Board[loc]);
                }
            }
        }

        // The board a session would have from its placed pieces alone:
        // initial board plus their spreads in index order, resolved once
        // (tests and tools; the session is left rebuilt).
        public static byte[] Rebuilt(LevelSession s)
        {
            Instance.Rebuild(s);
            Instance.ResolveCore(s);
            return (byte[])s.Board.Clone();
        }

        // Initial board, then the placed pieces' spreads in index order.
        void Rebuild(LevelSession s)
        {
            s.Def.CopyBoardTo(s.Board);
            s.RepelQueue.Clear();
            s.ResetTripped = false;
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Placed) Propagate(s, s.Board, k, fireEvents: false, forbiddenCheck: false);
            }
        }

        // As the classic reset (4 -> 1, unlocked pieces to the tray), with
        // locked pieces staying and re-propagating.
        public void FullReset(LevelSession s)
        {
            s.Resets++;
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
            bool anyLocked = false;
            for (int k = 0; k < s.Pieces.Length; k++)
            {
                if (s.Pieces[k].Locked) { anyLocked = true; continue; }
                s.Pieces[k].Placed = false;
                s.Pieces[k].I = -1;
                s.Pieces[k].J = -1;
            }
            s.RaisePiecesUnbound();
            s.RepelQueue.Clear();
            s.ResetTripped = false;
            if (anyLocked)
            {
                for (int k = 0; k < s.Pieces.Length; k++)
                {
                    if (s.Pieces[k].Locked) Propagate(s, s.Board, k, fireEvents: true, forbiddenCheck: false);
                }
                s.RepelQueue.Clear();
                s.ResetTripped = false;
            }
        }

        // ---- the spread ----

        // Would placing this piece here infect a forbidden cell (directly,
        // through its area, or through a relay it lights)? Simulated on a
        // scratch board; nothing is raised or queued.
        public static bool WouldHitForbidden(LevelSession s, int pieceIndex, int i, int j)
        {
            bool anyForbidden = false;
            for (int loc = 0; loc < Grid.Cells && !anyForbidden; loc++) anyForbidden = s.Board[loc] == Cell.Forbidden;
            if (!anyForbidden) return false;
            var scratch = (byte[])s.Board.Clone();
            var probe = new Walk(s, scratch, fireEvents: false, forbiddenCheck: true);
            probe.Spread(s.Def.Specs[pieceIndex], i, j);
            return probe.HitForbidden;
        }

        static void Propagate(LevelSession s, byte[] board, int pieceIndex, bool fireEvents, bool forbiddenCheck)
        {
            ref PieceState piece = ref s.Pieces[pieceIndex];
            var walk = new Walk(s, board, fireEvents, forbiddenCheck);
            walk.Spread(s.Def.Specs[pieceIndex], piece.I, piece.J);
        }

        // One propagation: the piece's cell, its area, its arms; relay cells
        // lit along the way spread their own arms (each at most once per
        // propagation, on its 1 -> 4 transition).
        struct Walk
        {
            readonly LevelSession _s;
            readonly byte[] _board;
            readonly bool _fire;
            readonly bool _check;
            public bool HitForbidden;

            public Walk(LevelSession s, byte[] board, bool fireEvents, bool forbiddenCheck)
            {
                _s = s;
                _board = board;
                _fire = fireEvents;
                _check = forbiddenCheck;
                HitForbidden = false;
            }

            public void Spread(PieceSpec spec, int i, int j)
            {
                Infect(i, j);
                if (spec.Area)
                {
                    // Walls, switches and traps inside the area are inert; a
                    // forbidden cell inside it is a hit.
                    for (int di = -1; di <= 1; di++)
                    {
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            if (di == 0 && dj == 0) continue;
                            int ai = i + di, aj = j + dj;
                            if (!Grid.InBounds(ai, aj)) continue;
                            byte v = _board[Grid.Loc(ai, aj)];
                            if (v == Cell.Forbidden) HitForbidden = true;
                            else if (v == Cell.Active) Infect(ai, aj);
                        }
                    }
                }
                Arms(spec.Arms, spec.Reach, i, j);
            }

            // Rings 1..SpreadRange (or the arm's reach), inner order
            // TileArms.SpreadOrderV2; 2/3/5 stop a direction, 6 is a hit,
            // voids and the edge are passed over.
            void Arms(int arms, uint reach, int i0, int j0)
            {
                if (arms == 0) return;
                int stopped = 0;
                for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                {
                    for (int n = 0; n < TileArms.SpreadOrderV2.Length; n++)
                    {
                        Dir dir = TileArms.SpreadOrderV2[n];
                        int d = (int)dir;
                        if ((arms & (1 << d)) == 0 || (stopped & (1 << d)) != 0) continue;
                        int limit = (int)(reach >> (4 * d)) & 0xF;
                        if (limit != 0 && offset > limit) { stopped |= 1 << d; continue; }

                        int i = i0 + TileArms.Di(dir) * offset;
                        int j = j0 + TileArms.Dj(dir) * offset;
                        if (!Grid.InBounds(i, j)) continue;
                        byte bp = _board[Grid.Loc(i, j)];
                        if (bp == Cell.Wall)
                        {
                            stopped |= 1 << d;
                        }
                        else if (bp == Cell.RepelSwitch)
                        {
                            stopped |= 1 << d;
                            if (!_check) _s.RepelQueue.Add(new Repel(i, j, TileArms.Opposite(dir)));
                        }
                        else if (bp == Cell.ResetTrap)
                        {
                            stopped |= 1 << d;
                            if (!_check) _s.ResetTripped = true;
                        }
                        else if (bp == Cell.Forbidden)
                        {
                            stopped |= 1 << d;
                            HitForbidden = true;
                        }
                        else
                        {
                            Infect(i, j);
                        }
                    }
                }
            }

            void Infect(int i, int j)
            {
                int loc = Grid.Loc(i, j);
                byte v = _board[loc];
                if (v != Cell.Active && v != Cell.Infected) return;
                if (v == Cell.Infected) return;
                _board[loc] = Cell.Infected;
                if (_fire) _s.RaiseCellChanged(i, j, Cell.Infected);
                byte relay = _s.Def.CellDataAt(loc);
                if (relay != 0) Arms(relay, 0, i, j);
            }
        }
    }
}
