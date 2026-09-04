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
        }

        public void ClearPreview()
        {
            foreach (int loc in _previewLocs)
            {
                int i = loc / Grid.Width, j = loc % Grid.Width;
                if (_state.KindAt(i, j) == BoardStateTexture.Kind.Preview) _state.SetSettled(i, j, _session.Board[loc]);
            }
            _previewLocs.Clear();
            _previewPiece = _previewI = _previewJ = -1;
        }

        // ---- layout ----

        // The band the lattice gets: above the tray and the well's bottom
        // pad, below the HUD and the well's top pad (STYLE-GUIDE §4: board top
        // 138, tray 150). Measured in the guide's px, so it is independent of
        // the cell size and the fit below cannot chase its own tail.
        static void MeasureBand(out float bottom, out float top)
        {
            float h = UnityEngine.Screen.height;
            bottom = Style.Px(Style.TrayHeight + Style.WellPad);
            top = h - Style.Px(Style.BoardTop + Style.WellPad);
        }

        // Whichever of the three binds. Height caps the cell at the guide's
        // 54 px (and, on a short screen, the original's 11% of height); width
        // is the phone's short edge across six columns; the band is what stops
        // an 11-tall board growing into the tray.
        static float MeasureCellSize()
        {
            MeasureBand(out float bottom, out float top);
            float byHeight = Mathf.Min(UnityEngine.Screen.height * PresentationConfig.CellHeightPct, Style.Px(Style.Cell));
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
        public void BeginWave(int i, int j)
        {
            _batch = Batch.Wave;
            _waveI = i;
            _waveJ = j;
            _waveTime = _boardTime;
            _hopsClicked = 0;
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
                // A cell on one of the seed's lines (row, column, or — with
                // diagonal arms — a diagonal) rides the wave; depth is the
                // ring it sits on, the same for every arm direction.
                int di = System.Math.Abs(i - _waveI), dj = System.Math.Abs(j - _waveJ);
                if (_batch == Batch.Wave && (i == _waveI || j == _waveJ || di == dj))
                {
                    int depth = System.Math.Max(di, dj);
                    int dr = i == _waveI ? 0 : (i > _waveI ? 1 : -1);
                    int dc = j == _waveJ ? 0 : (j > _waveJ ? 1 : -1);
                    float start = _waveTime + depth * Vfx.Hop;
                    _state.Set(i, j, value, start, BoardStateTexture.PackDir(dr, dc),
                        BoardStateTexture.Kind.Infecting);
                    ClickHop(depth, start);
                    return;
                }
                // Re-propagation during an undo, or a board arriving whole.
                _state.SetSettled(i, j, value);
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

            _state.SetSettled(i, j, value);
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
                int reach = spec.ReachOf(dir);
                for (int offset = 1; offset <= Grid.SpreadRange; offset++)
                {
                    if (reach != 0 && offset > reach) break;
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
