using GridInfect.Core;
using UnityEngine;
// UnityEngine also declares a Grid component; ours wins explicitly.
using Grid = GridInfect.Core.Grid;
using Vfx = GridInfect.Game.PresentationConfig.Infection;
using Style = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // The board: one quad, one material, one draw call
    // (docs/infection-vfx-spec.md, STYLE-GUIDE §4-§5). The quad carries the
    // recessed well and every tile; cell state lives in a data texture the
    // shader reads, and an infection change writes texels and nothing else —
    // no GameObjects, no meshes, no allocation per placement. The only
    // sprites over the board are static shape glyphs: the blocker shield on
    // every wall and the relay hub on every relay cell.
    //
    // The view never asks the rules what a wave looks like. It watches
    // CellChanged and derives each cell's place in the wave from the origin of
    // the placement it is already holding: cells arrive along straight rays, so
    // depth is the Manhattan distance from the seed and the entry direction is
    // the sign of the offset. The shader turns (start time, entry direction)
    // into the whole per-cell timeline.
    public sealed class BoardView
    {
        public readonly float CellSize;
        public readonly float Pitch;

        // Juice layers. Each is independent and default on, except the ghost
        // trail, which the spec ships off.
        public bool ArrivalPulse = true;
        public bool ConflictShake = true;
        public bool PlacementShake = true;   // STYLE-GUIDE §9: 2 px, 80 ms on every drop
        public bool EdgeSparks = true;
        public bool TraceDim = true;
        public bool HopAudio = true;
        public bool GhostTrail = false;

        public bool Muted
        {
            get => _audio.Muted;
            set => _audio.Muted = value;
        }

        // The board clock, and when the last cell of the current wave has
        // been reached on it: what a screen waits on before it takes the
        // board away from a wave still walking.
        public float BoardTime => _boardTime;
        public float WaveEnd { get; private set; }

        const string ShaderName = "GridInfect/Board";
        const int NoiseSeed = 20140531;   // the year the original shipped

        static readonly int IdBoardTime = Shader.PropertyToID("_BoardTime");
        static readonly int IdArrivalPulse = Shader.PropertyToID("_ArrivalPulse");
        static readonly int IdEdgeSparks = Shader.PropertyToID("_EdgeSparks");
        static readonly int IdTraceDim = Shader.PropertyToID("_TraceDim");
        static readonly int IdGhostTrail = Shader.PropertyToID("_GhostTrail");

        // Static shape glyphs over the board (walls, relays), and the drop
        // preview's bookkeeping.
        readonly System.Collections.Generic.List<GameObject> _glyphs = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<int> _previewLocs = new System.Collections.Generic.List<int>();
        readonly System.Collections.Generic.List<int> _conflictLocs = new System.Collections.Generic.List<int>();
        float _conflictUntil;
        Core.Solving.LineMap _lines;
        int _previewI = -1, _previewJ = -1;
        int _previewPiece = -1;

        readonly GameObject _root;
        readonly LevelSession _session;
        readonly BoardPalette _palette;
        readonly BoardStateTexture _state;
        readonly Texture2D _noise;          // shared and cached; not ours to destroy
        readonly Material _material;
        readonly Mesh _quad;
        readonly HopClickAudio _audio;
        readonly Vector3 _boardHome;
        readonly float _row0Y;
        readonly float _margin;             // quad px outside the lattice on every side

        float _boardTime;
        float _shakeUntil = float.NegativeInfinity;
        float _shakeDur = Vfx.ConflictShakeDur;

        Batch _batch = Batch.None;
        int _waveI = -1, _waveJ = -1;
        float _waveTime;
        int _hopsClicked;                    // one click per hop depth, not per cell

        // Where this wave's rays come from: the seed, and every relay lit
        // along the way (RULES_V2 §12), each with the arms it fires and the
        // moment it lit. A cell's place in the wave is its earliest arrival
        // along any source's ray, so a relay chain walks the board hop by
        // hop from turn to turn instead of landing whole.
        struct Source
        {
            public int I, J;
            public int Arms;
            public bool Area;
            public float Start;
        }
        readonly System.Collections.Generic.List<Source> _sources = new System.Collections.Generic.List<Source>();

        float _recedeBatchTime = float.NegativeInfinity;
        int _recedeIndex;

        // What the adapter is doing while events arrive. Only board.resolve is
        // unbracketed, which is exactly where the session's own ResetTripped
        // tells a repel apart from a trap reset.
        enum Batch { None, Wave, Undo, Reset }

        public BoardView(Transform parent, LevelSession session)
            : this(parent, session, BoardPalette.Default) { }

        public BoardView(Transform parent, LevelSession session, BoardPalette palette)
        {
            _session = session;
            _palette = palette;

            CellSize = MeasureCellSize();
            Pitch = CellSize * PresentationConfig.CellPitch;
            _row0Y = MeasureRow0Y(Pitch);

            _root = new GameObject("board");
            _root.transform.SetParent(parent, false);
            // The quad spans the COLS x ROWS lattice, gutters included, plus a
            // margin for the well, its ring and the glow that spills past it.
            _boardHome = new Vector3(0f, _row0Y - (Grid.Height - 1) * Pitch / 2f, 0.5f);
            _root.transform.localPosition = _boardHome;

            _state = new BoardStateTexture();
            _state.Fill(session);
            _noise = BoardNoise.Shared(Grid.Width * Vfx.Blocks, Grid.Height * Vfx.Blocks, NoiseSeed);

            _margin = Style.Px(Style.WellPad + palette.GlowPx * 1.2f + 4f);
            _quad = BuildQuad(Grid.Width * Pitch + 2f * _margin, Grid.Height * Pitch + 2f * _margin);
            var filter = _root.AddComponent<MeshFilter>();
            filter.sharedMesh = _quad;
            var renderer = _root.AddComponent<MeshRenderer>();
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[board] shader '{ShaderName}' not found — the board will not draw");
            }
            else
            {
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                renderer.sharedMaterial = _material;
                ApplyStaticMaterialState();
            }

            _audio = new HopClickAudio(_root.transform);

            BoardBloom.Ensure(Camera.main, _palette);

            session.CellChanged += OnCellChanged;
            session.PiecesUnbound += OnPiecesUnbound;
            Flush();
            DrawStaticGlyphs(parent);
        }

        // The glyph size on a tile: 44 px on a 54 px tile (STYLE-GUIDE §6).
        public int GlyphPx => Mathf.Max(8, Mathf.RoundToInt(CellSize * Style.GlyphOnTile / Style.Cell));

        // Walls carry the blocker shield (tile_BLOCKER), relay cells (RULES_V2
        // §12) a hub with one stub per arm — sprites over the board so the
        // shader stays a pure cell-value reader. Shape, not colour (R-1001).
        void DrawStaticGlyphs(Transform parent)
        {
            for (int loc = 0; loc < Grid.Cells; loc++)
            {
                int i = loc / Grid.Width, j = loc % Grid.Width;
                Vector2 c = CellCenter(i, j);
                byte value = _session.Def.BoardAt(loc);
                if (value == Cell.Wall)
                {
                    var shield = Ui.MakeSprite("blocker", parent, BugGlyph.Blocker(_palette, GlyphPx), 3);
                    shield.transform.localPosition = new Vector3(c.x, c.y, 0f);
                    _glyphs.Add(shield.gameObject);
                }
                byte arms = _session.Def.CellDataAt(loc);
                if (arms != 0)
                {
                    var relay = Ui.MakeSprite("relay", parent, BugGlyph.Relay(arms, _palette, GlyphPx), 3);
                    relay.transform.localPosition = new Vector3(c.x, c.y, 0f);
                    _glyphs.Add(relay.gameObject);
                }
            }
        }

        public void Dispose()
        {
            _session.CellChanged -= OnCellChanged;
            _session.PiecesUnbound -= OnPiecesUnbound;
            _audio.Dispose();
            _state.Dispose();
            if (_material != null) Object.Destroy(_material);
            if (_quad != null) Object.Destroy(_quad);
            if (_root != null) Object.Destroy(_root);
            foreach (var glyph in _glyphs) if (glyph != null) Object.Destroy(glyph);
            _glyphs.Clear();
        }

        // ---- drop preview ----

        // The pending trace (STYLE-GUIDE §5): while a piece is over a cell it
        // could go on, every cell it would light gets the preview look. The
        // reach comes from the solver's line map, which already mirrors the
        // stop set, so the view asks no rules question of its own. Cleared on
        // release, before the real wave is opened.
        public void ShowPreview(int piece, int i, int j)
        {
            if (piece == _previewPiece && i == _previewI && j == _previewJ) return;
            ClearPreview();
            if (!Grid.InBounds(i, j)) return;
            int loc = Grid.Loc(i, j);
            if (_session.Board[loc] != Cell.Active) return;
            if (_lines == null) _lines = new Core.Solving.LineMap(_session.Def);

            _previewPiece = piece;
            _previewI = i;
            _previewJ = j;
            var covered = _lines.Coverage(_session.Def.Specs[piece], loc);
            for (int target = 0; target < Grid.Cells; target++)
            {
                if (!covered.Has(target) || _session.Board[target] != Cell.Active) continue;
                int ti = target / Grid.Width, tj = target % Grid.Width;
                _state.Set(ti, tj, Cell.Active, _boardTime, BoardStateTexture.SeedDir, BoardStateTexture.Kind.Preview);
                _previewLocs.Add(target);
            }
            WarnPreview(_session.Def.Specs[piece], i, j);
        }

        // The part of the preview that is a warning: an arm that would run
        // into a forbidden cell (the drop would be refused) or a trap (the
        // board would reset) is shown in the conflict colour out to that
        // cell, and the cell itself pulses, while the finger is still there.
        // A blot's ring warns the forbidden cells inside it. Relay chains are
        // not traced. Same walk as the rules: walls and switches stop an arm,
        // voids and the edge are passed over.
        void WarnPreview(PieceSpec spec, int i0, int j0)
        {
            if (spec.Area)
            {
                for (int di = -1; di <= 1; di++)
                {
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        if (di == 0 && dj == 0) continue;
                        int ai = i0 + di, aj = j0 + dj;
                        if (Grid.InBounds(ai, aj) && _session.Board[Grid.Loc(ai, aj)] == Cell.Forbidden) Warn(ai, aj);
                    }
                }
            }
            for (int d = 0; d < 8; d++)
            {
                var dir = (Dir)d;
                if (!spec.Has(dir)) continue;
                int hit = -1;
                for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                {
                    int i = i0 + TileArms.Di(dir) * offset;
                    int j = j0 + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) continue;
                    byte value = _session.Board[Grid.Loc(i, j)];
                    if (value == Cell.Wall || value == Cell.RepelSwitch) break;
                    if (value == Cell.Forbidden || value == Cell.ResetTrap) { hit = offset; break; }
                }
                if (hit < 0) continue;
                for (int offset = 1; offset <= hit; offset++)
                {
                    int i = i0 + TileArms.Di(dir) * offset;
                    int j = j0 + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j) || _session.Board[Grid.Loc(i, j)] == Cell.Void) continue;
                    Warn(i, j);
                }
            }
        }

        void Warn(int i, int j)
        {
            int loc = Grid.Loc(i, j);
            _state.Set(i, j, _session.Board[loc], _boardTime, BoardStateTexture.SeedDir, BoardStateTexture.Kind.Warn);
            if (!_previewLocs.Contains(loc)) _previewLocs.Add(loc);
        }

        public void ClearPreview()
        {
            foreach (int loc in _previewLocs)
            {
                int i = loc / Grid.Width, j = loc % Grid.Width;
                float kind = _state.KindAt(i, j);
                if (kind == BoardStateTexture.Kind.Preview || kind == BoardStateTexture.Kind.Warn)
                    _state.SetSettled(i, j, _session.Board[loc], BoardStateTexture.IsPieceCell(_session, i, j));
            }
            _previewLocs.Clear();
            _previewPiece = _previewI = _previewJ = -1;
        }

        // ---- layout ----

        // The band the lattice gets: above the tray and the well's bottom
        // pad, below the HUD and the well's top pad (STYLE-GUIDE §4: board top
        // 88, tray 96). Measured in the guide's px, so it is independent of
        // the cell size and the fit below cannot chase its own tail.
        static void MeasureBand(out float bottom, out float top)
        {
            float h = UnityEngine.Screen.height;
            bottom = Style.Px(Style.TrayHeight + Style.WellPad);
            top = h - Style.Px(Style.BoardTop + Style.WellPad);
        }

        // Whichever of the three binds. Height caps the cell at CellMaxPx (and,
        // on a short screen, the original's 11% of height); width is the
        // phone's short edge across six columns; the band is what stops an
        // 11-tall board growing into the tray. The height cap used to be the
        // guide's 54 px design cell, which held the board small on a short,
        // wide screen where nothing else was asking for the room.
        static float MeasureCellSize()
        {
            MeasureBand(out float bottom, out float top);
            float byHeight = Mathf.Min(UnityEngine.Screen.height * PresentationConfig.CellHeightPct, Style.Px(PresentationConfig.CellMaxPx));
            float byWidth = UnityEngine.Screen.width * PresentationConfig.BoardWidthPct
                            / (Grid.Width * PresentationConfig.CellPitch);
            float byBand = (top - bottom) / (Grid.Height * PresentationConfig.CellPitch);
            return Mathf.Min(byHeight, Mathf.Min(byWidth, byBand));
        }

        // Row 0's centre: the board sits centred in that band, which is what
        // keeps it composed when a tall screen leaves far more room than a
        // wide one.
        static float MeasureRow0Y(float pitch)
        {
            MeasureBand(out float bottom, out float top);
            float centre = (bottom + top) / 2f - UnityEngine.Screen.height / 2f;
            return centre + (Grid.Height - 1) * pitch / 2f;
        }

        public Vector2 CellCenter(int i, int j)
        {
            // (Width - 1) / 2, not Width / 2: an even column count has no
            // middle column, and integer division put the board half a pitch
            // off centre the moment it stopped being 11 wide.
            float x = (j - (Grid.Width - 1) / 2f) * Pitch;
            return new Vector2(x, _row0Y - i * Pitch);
        }

        public (int i, int j) CellAt(Vector2 world)
        {
            for (int i = 0; i < Grid.Height; i++)
            {
                for (int j = 0; j < Grid.Width; j++)
                {
                    Vector2 c = CellCenter(i, j);
                    if (Mathf.Abs(world.x - c.x) <= Pitch / 2f && Mathf.Abs(world.y - c.y) <= Pitch / 2f)
                    {
                        return (i, j);
                    }
                }
            }
            return (-1, -1);
        }

        // ---- waves ----

        // Opened right before a placement is dispatched, closed right after:
        // every CellChanged raised in between belongs to this wave. A placement
        // landing mid-bleed opens a new wave on the same frame; cells already in
        // flight keep running off their own start times, which is the point of
        // putting the clock in the texture instead of in a tween.
        //
        // `spec` is the piece being dropped, so the seed's rays are the arms
        // it actually has; without it every direction is a candidate, which
        // is only wrong for a cell that a relay lit before the seed's own
        // ray would have got there.
        public void BeginWave(int i, int j, PieceSpec? spec = null)
        {
            _batch = Batch.Wave;
            _waveI = i;
            _waveJ = j;
            _waveTime = _boardTime;
            _hopsClicked = 0;
            WaveEnd = _waveTime;
            _sources.Clear();
            int arms = spec.HasValue ? spec.Value.Arms : 0xFF;
            bool area = !spec.HasValue || spec.Value.Area;
            _sources.Add(new Source { I = i, J = j, Arms = arms, Area = area, Start = _waveTime });
            // A relay under the piece fires the moment the piece lands on it.
            if (Grid.InBounds(i, j))
            {
                byte relay = _session.Def.CellDataAt(Grid.Loc(i, j));
                if (relay != 0) _sources.Add(new Source { I = i, J = j, Arms = relay, Start = _waveTime });
            }
        }

        // The earliest a source's ray reaches this cell, one hop per ring,
        // and the direction it comes in from. A ray is stopped by a wall, a
        // switch, a trap or an avoid cell between the source and the cell
        // (the same stop set the rules walk, Rules V2 §3) and passes over
        // voids and gaps. The seed's own cell arrives at once from nowhere.
        bool Arrival(int i, int j, out float start, out int dr, out int dc)
        {
            start = float.PositiveInfinity;
            dr = dc = 0;
            if (i == _waveI && j == _waveJ)
            {
                start = _waveTime;
                return true;
            }
            foreach (Source s in _sources)
            {
                int di = i - s.I, dj = j - s.J;
                int adi = System.Math.Abs(di), adj = System.Math.Abs(dj);
                if (s.Area && adi <= 1 && adj <= 1 && (di != 0 || dj != 0))
                {
                    Consider(s.Start + Vfx.Hop, System.Math.Sign(di), System.Math.Sign(dj), ref start, ref dr, ref dc);
                }
                // On a ray: same row, same column, or the same diagonal.
                if (!(di == 0 || dj == 0 || adi == adj)) continue;
                int sr = System.Math.Sign(di), sc = System.Math.Sign(dj);
                Dir dir = DirOf(sr, sc);
                if ((s.Arms & (1 << (int)dir)) == 0) continue;
                int offset = System.Math.Max(adi, adj);
                bool blocked = false;
                for (int o = 1; o < offset && !blocked; o++)
                {
                    byte v = _session.Def.BoardAt(Grid.Loc(s.I + sr * o, s.J + sc * o));
                    blocked = v == Cell.Wall || v == Cell.RepelSwitch || v == Cell.ResetTrap || v == Cell.Forbidden;
                }
                if (blocked) continue;
                Consider(s.Start + offset * Vfx.Hop, sr, sc, ref start, ref dr, ref dc);
            }
            return !float.IsPositiveInfinity(start);
        }

        static void Consider(float at, int sr, int sc, ref float start, ref int dr, ref int dc)
        {
            if (at >= start) return;
            start = at;
            dr = sr;
            dc = sc;
        }

        static Dir DirOf(int sr, int sc)
        {
            if (sr == 0) return sc < 0 ? Dir.L : Dir.R;
            if (sc == 0) return sr < 0 ? Dir.U : Dir.D;
            if (sr < 0) return sc < 0 ? Dir.UL : Dir.UR;
            return sc < 0 ? Dir.DL : Dir.DR;
        }

        // An undo retracts a piece and re-propagates the rest, then resyncs
        // every cell at once: that is a board correction, not a wave, so the
        // ink lifts off together instead of pretending to walk.
        public void BeginUndo() => _batch = Batch.Undo;

        // The replay button. Same simultaneous lift-off, and never a shake:
        // conflict shake is for the trap, not for the player asking politely.
        public void BeginReset() => _batch = Batch.Reset;

        // `applied` is the dispatch result: a rejected drop raised no events
        // and must not light a trap left over from an earlier trip.
        public void EndBatch(bool applied = true)
        {
            if (_batch == Batch.None) return;
            if (_batch == Batch.Wave && applied && _session.ResetTripped) FlashTrippedTrap();
            if (_batch == Batch.Wave && applied && PlacementShake && _boardTime >= _shakeUntil)
            {
                Shake(Vfx.PlacementShakeDur);
            }
            _batch = Batch.None;
            Flush();
        }

        void OnCellChanged(int i, int j, byte value)
        {
            if (value == Cell.Infected)
            {
                // A cell on a ray of the seed, or of a relay this wave has
                // already lit, rides the wave: its start is its earliest
                // arrival along any of them, one hop per ring. A relay lit
                // here becomes a source itself, so the cells its arms take
                // arrive after it, hop by hop, and a chain reads as a chain.
                if (_batch == Batch.Wave && Arrival(i, j, out float start, out int dr, out int dc))
                {
                    int depth = Mathf.RoundToInt((start - _waveTime) / Vfx.Hop);
                    if (start + Vfx.Hop > WaveEnd) WaveEnd = start + Vfx.Hop;
                    _state.Set(i, j, value, start, BoardStateTexture.PackDir(dr, dc),
                        BoardStateTexture.Kind.Infecting);
                    ClickHop(depth, start);
                    if (!(i == _waveI && j == _waveJ))
                    {
                        byte relay = _session.Def.CellDataAt(Grid.Loc(i, j));
                        if (relay != 0) _sources.Add(new Source { I = i, J = j, Arms = relay, Start = start });
                    }
                    return;
                }
                // Re-propagation during an undo, or a board arriving whole.
                _state.SetSettled(i, j, value, BoardStateTexture.IsPieceCell(_session, i, j));
                return;
            }

            if (value == Cell.Active && _state.ValueAt(i, j) == Cell.Infected)
            {
                // 4 -> 1. Two callers: a repel walking the infection back off a
                // ray, or a full reset. ResetTripped is still set while the
                // reset runs and clear on the repel path, so it tells them
                // apart without asking the rules anything.
                float start = _boardTime;
                if (_batch == Batch.None && !_session.ResetTripped)
                {
                    if (_recedeBatchTime != _boardTime)
                    {
                        _recedeBatchTime = _boardTime;
                        _recedeIndex = 0;
                    }
                    // Repels arrive in walk order, so the index is the hop
                    // depth; capped so a long queue cannot outrun one ray.
                    start += Mathf.Min(_recedeIndex++, Grid.SpreadRange) * Vfx.Hop;
                }
                _state.Set(i, j, value, start, _state.PackedDirAt(i, j), BoardStateTexture.Kind.Receding);
                return;
            }

            _state.SetSettled(i, j, value, BoardStateTexture.IsPieceCell(_session, i, j));
        }

        // Every full reset unbinds the pieces, but only a tripped trap is a
        // conflict; the replay button is not.
        void OnPiecesUnbound()
        {
            if (ConflictShake && _batch != Batch.Reset && _session.ResetTripped)
            {
                Shake(Vfx.ConflictShakeDur);
            }
        }

        void ClickHop(int depth, float at)
        {
            if (depth > 31 || (_hopsClicked & (1 << depth)) != 0) return;
            _hopsClicked |= 1 << depth;
            _audio.Schedule(at, depth);
        }

        // The trap that stopped a ray, so the conflict overprint lights up when
        // the beam reaches it rather than when the reset lands 300 ms later.
        // The walk mirrors the stop set in Rules.PropagatePiece — walls and
        // switches stop a direction, voids are passed over — and only runs once
        // the session has already confirmed a trap was tripped.
        void FlashTrippedTrap()
        {
            int piece = -1;
            for (int k = 0; k < _session.Pieces.Length; k++)
            {
                if (_session.Pieces[k].Placed && _session.Pieces[k].I == _waveI && _session.Pieces[k].J == _waveJ)
                {
                    piece = k;
                    break;
                }
            }
            if (piece < 0) return;

            PieceSpec spec = _session.Def.Specs[piece];
            for (int d = 0; d < 8; d++)
            {
                var dir = (Dir)d;
                if (!spec.Has(dir)) continue;
                for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                {
                    int i = _waveI + TileArms.Di(dir) * offset;
                    int j = _waveJ + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) break;
                    byte value = _session.Board[Grid.Loc(i, j)];
                    if (value == Cell.Wall || value == Cell.RepelSwitch || value == Cell.Forbidden) break;
                    if (value == Cell.ResetTrap)
                    {
                        _state.Set(i, j, value, _waveTime + offset * Vfx.Hop,
                            BoardStateTexture.SeedDir, BoardStateTexture.Kind.Conflict);
                        break;
                    }
                }
            }
        }

        // ---- frame ----

        public void Tick(float dt)
        {
            _boardTime += dt;
            if (_conflictLocs.Count > 0 && _boardTime >= _conflictUntil) SettleConflicts();
            if (_material != null) PushFrameMaterialState();
            _audio.Enabled = HopAudio;
            _audio.Tick(_boardTime);
            ApplyShake();
            Flush();
        }

        void PushFrameMaterialState()
        {
            _material.SetFloat(IdBoardTime, _boardTime);
            _material.SetFloat(IdArrivalPulse, ArrivalPulse ? 1f : 0f);
            _material.SetFloat(IdEdgeSparks, EdgeSparks ? 1f : 0f);
            _material.SetFloat(IdTraceDim, TraceDim ? 1f : 0f);
            _material.SetFloat(IdGhostTrail, GhostTrail ? 1f : 0f);
        }

        public void Flush() => _state.Flush();

        // A drop the rules refused because the piece's spread would touch a
        // forbidden cell: show why. Each offending arm is overprinted in the
        // conflict colour from the drop cell out to the forbidden cell it
        // would reach, one hop at a time, and the board shakes as it does
        // for a tripped trap; a blot's own ring does the same for the
        // forbidden cells inside it. The overprint decays on its own and the
        // texels settle back once it has (Tick). Relay chains are not traced.
        public void FlashForbidden(int piece, int i0, int j0)
        {
            if (_session == null || !Grid.InBounds(i0, j0)) return;
            PieceSpec spec = _session.Def.Specs[piece];
            bool any = false;
            if (spec.Area)
            {
                for (int di = -1; di <= 1; di++)
                {
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        if (di == 0 && dj == 0) continue;
                        int ai = i0 + di, aj = j0 + dj;
                        if (Grid.InBounds(ai, aj) && _session.Board[Grid.Loc(ai, aj)] == Cell.Forbidden)
                        {
                            Conflict(ai, aj, _boardTime + Vfx.Hop);
                            any = true;
                        }
                    }
                }
            }
            for (int d = 0; d < 8; d++)
            {
                var dir = (Dir)d;
                if (!spec.Has(dir)) continue;
                int hit = -1;
                for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                {
                    int i = i0 + TileArms.Di(dir) * offset;
                    int j = j0 + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j)) continue;
                    byte value = _session.Board[Grid.Loc(i, j)];
                    if (value == Cell.Wall || value == Cell.RepelSwitch || value == Cell.ResetTrap) break;
                    if (value == Cell.Forbidden) { hit = offset; break; }
                }
                if (hit < 0) continue;
                any = true;
                for (int offset = 1; offset <= hit; offset++)
                {
                    int i = i0 + TileArms.Di(dir) * offset;
                    int j = j0 + TileArms.Dj(dir) * offset;
                    if (!Grid.InBounds(i, j) || _session.Board[Grid.Loc(i, j)] == Cell.Void) continue;
                    Conflict(i, j, _boardTime + offset * Vfx.Hop);
                }
            }
            if (any) Shake(Vfx.ConflictShakeDur);
        }

        void Conflict(int i, int j, float at)
        {
            int loc = Grid.Loc(i, j);
            _state.Set(i, j, _session.Board[loc], at, BoardStateTexture.SeedDir, BoardStateTexture.Kind.Conflict);
            if (!_conflictLocs.Contains(loc)) _conflictLocs.Add(loc);
            float until = at + Vfx.ConflictFlashDur;
            if (until > _conflictUntil) _conflictUntil = until;
        }

        void SettleConflicts()
        {
            foreach (int loc in _conflictLocs)
            {
                int i = loc / Grid.Width, j = loc % Grid.Width;
                if (_state.KindAt(i, j) == BoardStateTexture.Kind.Conflict)
                    _state.SetSettled(i, j, _session.Board[loc], BoardStateTexture.IsPieceCell(_session, i, j));
            }
            _conflictLocs.Clear();
        }

        void Shake(float duration)
        {
            _shakeDur = duration;
            _shakeUntil = _boardTime + duration;
        }

        // 2 px: 80 ms on a placement, 120 ms on a conflict. Only the quad
        // moves; CellAt maps input off the logical layout, so a shake can
        // never mis-place a piece.
        void ApplyShake()
        {
            if (_boardTime >= _shakeUntil)
            {
                _root.transform.localPosition = _boardHome;
                return;
            }
            float decay = (_shakeUntil - _boardTime) / Mathf.Max(_shakeDur, 1e-4f);
            float phase = _boardTime * 110f;
            _root.transform.localPosition = _boardHome + new Vector3(
                Mathf.Sin(phase) * Vfx.ConflictShakePx * decay,
                Mathf.Cos(phase * 1.37f) * Vfx.ConflictShakePx * decay, 0f);
        }

        // ---- material ----

        void ApplyStaticMaterialState()
        {
            _material.SetTexture("_StateTex", _state.Texture);
            _material.SetTexture("_NoiseTex", _noise);

            _material.SetFloat("_Cols", Grid.Width);
            _material.SetFloat("_Rows", Grid.Height);
            _material.SetFloat("_Blocks", Vfx.Blocks);
            _material.SetFloat("_Bias", Vfx.Bias);
            _material.SetFloat("_TraceDur", Vfx.TraceDur);
            _material.SetFloat("_BleedDur", Vfx.BleedDur);
            _material.SetFloat("_GlowHold", Vfx.GlowHold);
            _material.SetFloat("_GlowFade", Vfx.GlowFade);

            // Layout: the quad is the lattice plus the margin on every side,
            // and every furniture size is the guide's px scaled to the device.
            _material.SetVector("_QuadPx", new Vector4(Grid.Width * Pitch + 2f * _margin, Grid.Height * Pitch + 2f * _margin, 0f, 0f));
            _material.SetVector("_LatticeOrigin", new Vector4(_margin, _margin, 0f, 0f));
            _material.SetFloat("_PitchPx", Pitch);
            _material.SetFloat("_CellFrac", 1f / PresentationConfig.CellPitch);
            _material.SetFloat("_RefScale", CellSize / Style.Cell);
            _material.SetFloat("_TileRadiusPx", CellSize * Style.TileRadius / Style.Cell);
            _material.SetFloat("_WellPadPx", Style.Px(Style.WellPad));
            _material.SetFloat("_WellRadiusPx", Style.Px(Style.WellRadius));
            _material.SetFloat("_GlowPx", Style.Px(_palette.GlowPx));
            _material.SetFloat("_TracePx", Style.Px(_palette.TraceWidthPx));
            _material.SetFloat("_BlotAmp", _palette.BlotAmp);

            _material.SetFloat("_HotEmission", _palette.HotEmission);
            _material.SetFloat("_RestEmission", _palette.RestEmission);
            _material.SetFloat("_PulseGain", Vfx.ArrivalPulseGain);
            _material.SetFloat("_PulseDur", Vfx.ArrivalPulseDur);
            _material.SetFloat("_SparkLife", Vfx.SparkLife);
            _material.SetFloat("_TraceDimLevel", Vfx.TraceDimLevel);
            _material.SetFloat("_GhostTrailDur", Vfx.GhostTrailDur);
            _material.SetFloat("_ConflictDur", Vfx.ConflictFlashDur);
            _material.SetFloat("_PreviewFade", Vfx.PreviewFadeDur);

            SetPaletteColor("_ColTip", _palette.Tip);
            SetPaletteColor("_ColShade", _palette.Shade);
            SetPaletteColor("_ColWellBg", _palette.WellBg);
            SetPaletteColor("_ColCopper", _palette.Copper);
            SetPaletteColor("_ColCopperHi", _palette.CopperHi);
            SetPaletteColor("_ColCopperLo", _palette.CopperLo);
            SetPaletteColor("_ColInfect", _palette.Infect);
            SetPaletteColor("_ColInfectHi", _palette.InfectHi);
            SetPaletteColor("_ColInfectLo", _palette.InfectLo);
            SetPaletteColor("_ColInfectGlow", _palette.InfectGlow);
            SetPaletteColor("_ColGlyphEdge", _palette.GlyphEdge);
            SetPaletteColor("_ColSpace", _palette.Space);
            SetPaletteColor("_ColSwitch", _palette.RepelSwitch);
            SetPaletteColor("_ColTrap", _palette.ResetTrap);
            SetPaletteColor("_ColConflict", _palette.Conflict);
        }

        // The palette is authored in sRGB and handed over as-is. Every _Col*
        // property is declared as a Color in the shader's Properties block,
        // and for those Unity does the sRGB-to-linear conversion itself on
        // SetColor when the project renders linear — the same treatment a
        // sprite tint or the camera clear colour gets. Converting here as well
        // squares the values: the first real run drew the board near black,
        // with the cell plates gone (#141C33 came out around #050609).
        void SetPaletteColor(string property, Color color)
        {
            _material.SetColor(property, color);
        }

        static Mesh BuildQuad(float width, float height)
        {
            float hw = width / 2f, hh = height / 2f;
            var mesh = new Mesh { name = "board-quad", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f),
                new Vector3(-hw, hh, 0f), new Vector3(hw, hh, 0f),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
