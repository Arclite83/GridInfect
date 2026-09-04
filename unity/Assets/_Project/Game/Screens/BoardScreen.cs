using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;

namespace GridInfect.Game
{
    // The board screen laid out per STYLE-GUIDE §7-§8: a 96 px HUD with a
    // glass chip either side and the level label between them, the lock
    // counter as a mono badge under the right chip, the well below, and a
    // 150 px tray of component slots along the bottom.
    public sealed class BoardScreen : AppScreen
    {
        BoardView _board;
        PieceView[] _pieces;
        LevelSession _bound;
        TextMesh _title;
        TextMesh _caption;
        UiButton _backButton;
        UiButton _resetButton;
        UiButton _lockButton;
        GameObject _tray;
        GameObject _popup;
        GameObject _beginCover;
        int _dragIndex = -1;
        bool _popupOpen;

        static float ScreenW => UnityEngine.Screen.width;
        static float ScreenH => UnityEngine.Screen.height;

        // Boxes and type come off the short edge so a control keeps its shape
        // when the screen turns; positions stay fractions of their own axis.
        static float Short => PresentationConfig.ShortEdge;

        // A chip's box from its label: 12 px type, 8 x 14 padding (§7).
        static Vector2 ChipSize(string label) =>
            new Vector2(S.Px(S.ChipPadX * 2f + label.Length * S.ChipText * 0.66f), S.Px(S.ChipPadY * 2f + S.ChipText * 1.25f));

        protected override void Build()
        {
            float w = ScreenW, h = ScreenH;

            // HUD: items bottom-aligned in the 96 px band, 22 px in from
            // either edge. Same top bar as every other screen: back on the
            // left, the screen's one action on the right.
            float hudBottom = h / 2f - S.Px(S.HudHeight - S.HudBottomPad);
            var chip = ChipSize("RESET");
            float chipY = hudBottom + chip.y / 2f;
            _backButton = UiButton.Make(Root.transform, "MENU",
                new Vector2(-w / 2f + S.Px(S.HudInset) + chip.x / 2f, chipY), chip,
                BoardTheme.ButtonBg, BoardTheme.Text, GoBack);
            Buttons.Add(_backButton);
            _resetButton = UiButton.Make(Root.transform, "RESET",
                new Vector2(w / 2f - S.Px(S.HudInset) - chip.x / 2f, chipY), chip,
                BoardTheme.ButtonBg, BoardTheme.Text, ResetLevel);
            Buttons.Add(_resetButton);

            // Level label: Chakra Petch 26 px in ink, with a mono 11 px
            // caption above it. The caption doubles as the mode's readout
            // (clock, streak) where a mode has one.
            _title = Ui.MakeText("title", Root.transform, "", S.Px(S.HudLevel), BoardTheme.Text, 2);
            Ui.SetPos(_title.gameObject, 0f, hudBottom + S.Px(S.HudLevel) * 0.55f);
            _caption = Ui.MakeText("caption", Root.transform, "GI-REV B", S.Px(S.HudCaption), BoardTheme.TextDim, 2, mono: true);
            Ui.SetPos(_caption.gameObject, 0f, hudBottom + S.Px(S.HudLevel) * 1.1f + S.Px(S.HudCaption) * 0.9f);

            // The one tool (stage 5): spends a lock, places one piece at its
            // solution cell and locks it. The counter badge under RESET, off
            // the board: mono 13 px copperHi on black 35%.
            var badge = new Vector2(S.Px(S.BadgePadX * 2f + 7 * S.BadgeText * 0.62f), S.Px(S.BadgePadY * 2f + S.BadgeText * 1.25f));
            _lockButton = UiButton.Make(Root.transform, "",
                new Vector2(w / 2f - S.Px(S.HudInset) - badge.x / 2f, h / 2f - S.Px(S.BadgeTop) - badge.y / 2f), badge,
                GlassStyle.Badge(BoardPalette.Default), BoardTheme.Copper, LockPiece, 20, pads: false, padAlpha: 1f, mono: true);
            Buttons.Add(_lockButton);
            RefreshLockLabel();

            App.State.SessionChanged += OnSessionChanged;
            Bind(App.State.Session);

            if (App.State.Mode == GameMode.FreePlay && App.State.FreePlayRun == null)
            {
                ShowBeginCover();
            }
        }

        protected override void OnExit()
        {
            App.State.SessionChanged -= OnSessionChanged;
            Unbind();
            Substrate.SetLevel(null);
        }

        // ---- session binding ----

        void OnSessionChanged(LevelSession session)
        {
            ClosePopup();
            Bind(session);
        }

        void Bind(LevelSession session)
        {
            Unbind();
            _bound = session;
            if (session == null) return;

            _board = new BoardView(Root.transform, session);

            // The tray: one component slot per piece, the glyph at 58/74 of
            // the slot. Slots sit under the pieces so a lifted piece leaves
            // its socket visible.
            int count = session.Pieces.Length;
            float slot = TraySlotSize(count);
            int trayGlyph = Mathf.RoundToInt(slot * S.TrayNextGlyph / S.TraySlot);
            _tray = new GameObject("tray");
            _tray.transform.SetParent(Root.transform, false);
            _pieces = new PieceView[count];
            for (int k = 0; k < count; k++)
            {
                Vector2 at = TraySlot(k);
                var socket = Ui.MakeGlass($"slot:{k}", _tray.transform, new Vector2(slot, slot), GlassStyle.TraySlot(BoardPalette.Default, false), 4);
                Ui.SetPos(socket, at.x, at.y);
                _pieces[k] = new PieceView(Root.transform, k, session.Def.Specs[k], slot, trayGlyph, at);
            }
            SyncPieces();   // givens are already on the board
            session.LevelSolved += OnSolved;
            session.PiecesUnbound += OnPiecesUnbound;

            string level;
            switch (App.State.Mode)
            {
                case GameMode.Classic:
                    _caption.text = "LEGACY";
                    _title.text = $"LEVEL {App.State.ClassicLevelId + 1}";
                    level = (App.State.ClassicLevelId + 1).ToString("00");
                    break;
                case GameMode.World:
                    _caption.text = Worlds.Get(App.State.WorldId).Name.ToUpperInvariant();
                    _title.text = $"LEVEL {App.State.WorldIndex + 1}";
                    level = (App.State.WorldIndex + 1).ToString("00");
                    break;
                case GameMode.Daily:
                    _caption.text = "DAILY";
                    _title.text = App.State.DailyRun.DateUtc;
                    level = "DAILY";
                    break;
                case GameMode.Endless:
                    _caption.text = $"ENDLESS  GRADE {(int)App.State.EndlessRun.Grade}";
                    _title.text = $"LEVEL {App.State.EndlessRun.Index + 1}";
                    level = "ENDLESS";
                    break;
                default:
                    _caption.text = $"{App.State.Difficulty}".ToUpperInvariant();
                    _title.text = $"LEVEL {App.State.FreePlayIndex + 1}";
                    level = "FREE";
                    break;
            }
            Substrate.SetLevel(level);
            RefreshLockLabel();   // the price of a hint is per level, not per session
        }

        // The one reconciliation: where a piece view sits is a pure function of
        // the piece state — its cell if placed, its tray slot if not — so the
        // view cannot drift out of step with the rules. Every path that
        // changes what is placed ends here rather than moving views itself.
        //
        // The tray is where a piece starts, not where it always is: a level can
        // ship givens (world / daily / endless levels carry locked placements,
        // docs/GENERATOR_V2.md), and those pieces are on the board before the
        // player touches anything. Building every view at its tray slot left
        // the given's cells lit with its piece stuck in the tray, unliftable
        // and with its cell refusing every other piece.
        void SyncPieces(bool animate = false)
        {
            if (_bound == null || _pieces == null || _board == null) return;
            for (int k = 0; k < _pieces.Length; k++)
            {
                if (k == _dragIndex) continue;   // the finger owns that one
                PieceState piece = _bound.Pieces[k];
                _pieces[k].SetLocked(piece.Locked);
                // The board LOD on a tile, the tray LOD in a slot (STYLE-GUIDE §6).
                _pieces[k].SetGlyphSize(piece.Placed ? _board.GlyphPx : TrayGlyphPx);
                Vector2 to = piece.Placed ? _board.CellCenter(piece.I, piece.J) : _pieces[k].TraySlot;
                if (animate)
                {
                    App.Tweens.MoveTo(_pieces[k].Root.transform, new Vector3(to.x, to.y, 0f),
                        piece.Placed ? PresentationConfig.DropSnap : PresentationConfig.TrayReturn);
                }
                else
                {
                    // A fresh binding: no travel, and no tween left running
                    // that would drag the piece off the cell a frame later.
                    App.Tweens.Cancel(_pieces[k].Root.transform);
                    _pieces[k].SetPos(to);
                }
            }
        }

        void Unbind()
        {
            if (_bound != null)
            {
                _bound.LevelSolved -= OnSolved;
                _bound.PiecesUnbound -= OnPiecesUnbound;
                _bound = null;
            }
            _board?.Dispose();
            _board = null;
            if (_pieces != null)
            {
                foreach (var piece in _pieces)
                {
                    if (piece?.Root != null) Object.Destroy(piece.Root);
                }
                _pieces = null;
            }
            if (_tray != null) Object.Destroy(_tray);
            _tray = null;
            _dragIndex = -1;
        }

        // The original racked the tray into eight fixed slots and centred on
        // slot 3. The guide's tray is component slots on a 30 px gap, centred
        // on the pieces the level actually has, shrinking only if they would
        // not fit the width — which, at a real maximum of six, they do. The
        // slot follows the board's cell too, so a small board on a short
        // screen keeps the guide's 74:54 slot-to-tile ratio.
        float TraySlotSize(int count)
        {
            float byGuide = S.Px(S.TraySlot);
            float byBoard = (_board != null ? _board.CellSize : S.Px(S.Cell)) * S.TraySlot / S.Cell;
            float byWidth = ScreenW * PresentationConfig.BoardWidthPct
                            / (count * PresentationConfig.TraySlotPitch);
            return Mathf.Min(byGuide, Mathf.Min(byBoard, byWidth));
        }

        Vector2 TraySlot(int k)
        {
            int count = _bound != null ? _bound.Pieces.Length : PresentationConfig.TraySlots;
            float size = TraySlotSize(count);
            float x = (k - (count - 1) / 2f) * size * PresentationConfig.TraySlotPitch;
            float y = -ScreenH / 2f + S.Px(S.TrayHeight) / 2f;
            return new Vector2(x, y);
        }

        int TrayGlyphPx => Mathf.RoundToInt(TraySlotSize(_bound != null ? _bound.Pieces.Length : PresentationConfig.TraySlots) * S.TrayNextGlyph / S.TraySlot);

        // ---- input ----

        public override bool OnPress(Vector2 world)
        {
            if (_popupOpen || _beginCover != null || _bound == null) return true;

            for (int k = _pieces.Length - 1; k >= 0; k--)
            {
                if (!_pieces[k].HitTest(world)) continue;
                if (_bound.Pieces[k].Locked)
                {
                    // A locked given cannot be lifted (GENERATOR_V2 "Locks at
                    // load"). Swallowing the touch outright reads as a dead
                    // piece rather than a fixed one, so it leans and settles.
                    NudgeLocked(k);
                    return true;
                }

                if (_bound.Pieces[k].Placed)
                {
                    // Touching a placed piece unplaces it instantly (RULES §3).
                    _board.BeginUndo();
                    var clear = App.Do(GridInfectActions.PieceClear, Inputs.PieceClear(k));
                    _board.EndBatch();
                    if (!clear.Applied) return true;
                }
                _dragIndex = k;
                App.Tweens.Cancel(_pieces[k].Root.transform);
                _pieces[k].SetGlyphSize(_board.GlyphPx);   // heading for a tile: the board LOD
                _pieces[k].SetPos(world);
                return true;
            }
            return false;
        }

        void NudgeLocked(int k)
        {
            PieceState piece = _bound.Pieces[k];
            if (!piece.Placed || _board == null) return;
            Vector2 home = _board.CellCenter(piece.I, piece.J);
            Transform t = _pieces[k].Root.transform;
            // From the cell, not from wherever a previous nudge left it.
            t.localPosition = new Vector3(home.x, home.y, 0f);
            float lift = _board.CellSize * PresentationConfig.LockedNudgePct;
            App.Tweens.MoveTo(t, new Vector3(home.x, home.y + lift, 0f), PresentationConfig.LockedNudge,
                () => App.Tweens.MoveTo(t, new Vector3(home.x, home.y, 0f), PresentationConfig.LockedNudge));
        }

        public override void OnDrag(Vector2 world)
        {
            if (_dragIndex < 0) return;
            _pieces[_dragIndex].SetPos(world);
            // The pending trace under the finger (STYLE-GUIDE §5).
            var (i, j) = _board.CellAt(world);
            if (i >= 0) _board.ShowPreview(_dragIndex, i, j);
            else _board.ClearPreview();
        }

        public override void OnRelease(Vector2 world)
        {
            if (_dragIndex < 0 || _bound == null) return;
            int index = _dragIndex;
            _dragIndex = -1;
            _board.ClearPreview();

            var (i, j) = _board.CellAt(world);
            if (i >= 0)
            {
                // The wave is open across the dispatch: every CellChanged the
                // spread raises inside it is scheduled off this seed, and the
                // action is still applied on the frame the touch landed.
                _board.BeginWave(i, j);
                var place = App.Do(GridInfectActions.PiecePlace, Inputs.PiecePlace(index, i, j));
                _board.EndBatch(place.Applied);
                if (place.Applied)
                {
                    Vector2 center = _board.CellCenter(i, j);
                    App.Tweens.MoveTo(_pieces[index].Root.transform,
                        new Vector3(center.x, center.y, 0f), PresentationConfig.DropSnap);
                    App.ScheduleResolve();
                    return;
                }
            }
            // Illegal drop: back to the tray slot (the piece was already
            // cleared on touch, so board state is consistent).
            ReturnToTray(index, PresentationConfig.TrayReturn);
        }

        void ReturnToTray(int k, float duration)
        {
            _pieces[k].SetGlyphSize(TrayGlyphPx);
            App.Tweens.MoveTo(_pieces[k].Root.transform,
                new Vector3(_pieces[k].TraySlot.x, _pieces[k].TraySlot.y, 0f), duration);
        }

        // ---- lock tool ----

        void LockPiece()
        {
            if (_bound == null || _popupOpen || _beginCover != null) return;
            App.FastForwardResolve();
            _board.BeginWave(0, 0);
            var result = App.Do(GridInfectActions.PieceLock);
            _board.EndBatch(result.Applied);
            if (!result.Applied) return;
            // The locked piece lands on its cell, anything it evicted returns
            // to the tray — both fall out of reconciling against the rules.
            SyncPieces(animate: true);
            RefreshLockLabel();
            App.ScheduleResolve();
        }

        // With an empty wallet the button becomes the rewarded placement
        // (NEXT_PASS decision 8): watch an ad, earn one lock. On a replay the
        // tool costs nothing (piece.lock never touches the wallet there), so
        // the button says HINT instead of counting down a price it will not
        // charge — and stays live at wallet 0.
        void RefreshLockLabel()
        {
            if (_lockButton?.Label == null) return;
            bool replay = Queries.IsReplay(App.State);
            int locks = App.State.Profile.Locks;
            bool rewarded = !replay && locks == 0 && App.Ads.RewardedAvailable;
            _lockButton.Label.text = replay ? "HINT" : rewarded ? "+1 LOCK" : $"LOCK {locks:00}";
            _lockButton.OnClick = rewarded ? EarnLock : (System.Action)LockPiece;
            _lockButton.Enabled = (replay || locks > 0 || rewarded) && !_popupOpen;
        }

        void EarnLock()
        {
            App.Ads.ShowRewarded(earned =>
            {
                if (earned) App.Do(GridInfectActions.LocksGrant, Inputs.LocksGrant(1, GrantLocksAction.Rewarded));
                RefreshLockLabel();
            });
        }

        // ---- session reactions ----

        // A full reset (the replay button, or a tripped trap) unbinds every
        // piece the level did not lock. Reconciling rather than sweeping the
        // tray is what keeps a locked given on its cell: the rules re-placed
        // it before this ran, so the sync leaves it exactly where it is.
        void OnPiecesUnbound() => SyncPieces(animate: true);

        void OnSolved()
        {
            if (_popupOpen) return;
            App.Ads.CountSolve();
            if (App.State.Mode == GameMode.Classic)
            {
                int levelId = App.State.ClassicLevelId;
                int next = Queries.NextClassicId(levelId);
                if (next >= 0)
                {
                    App.Do(GridInfectActions.ProgressUnlock, Inputs.Unlock(next));
                }
                ShowSolvedPopup(next >= 0
                    ? () => App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(next))
                    : (System.Action)null,
                    () => App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(levelId)));
            }
            else if (App.State.Mode == GameMode.World)
            {
                // Solving level N unlocks N+1; the last level finishes the
                // world (index == Count) and opens the next one.
                string worldId = App.State.WorldId;
                int index = App.State.WorldIndex;
                World world = Worlds.Get(worldId);
                App.Do(GridInfectActions.ProgressUnlockWorldLevel, Inputs.UnlockWorldLevel(worldId, index + 1));
                System.Action next = null;
                if (index + 1 < world.Count)
                {
                    next = () => App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(worldId, index + 1));
                }
                else
                {
                    World following = Worlds.Next(worldId);
                    if (following != null)
                    {
                        App.Do(GridInfectActions.ProgressUnlockWorld, Inputs.UnlockWorld(following.Id));
                        next = () => App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(following.Id, 0));
                    }
                }
                ShowSolvedPopup(next, () => App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(worldId, index)));
            }
            else if (App.State.Mode == GameMode.Daily)
            {
                var run = App.State.DailyRun;
                if (!run.Completed)
                {
                    App.Do(GridInfectActions.DailyComplete, Inputs.Now(GameApp.NowMs()));
                    App.DailyScores.Submit(run.DateUtc, Queries.ElapsedMs(run, GameApp.NowMs()), run.ParMs);
                    if (run.StreakGrantDue)
                    {
                        App.Do(GridInfectActions.LocksGrant, Inputs.LocksGrant(1, "streak")); // +1 lock every 7-day streak
                    }
                }
                long elapsed = Queries.ElapsedMs(run, GameApp.NowMs());
                long best = Queries.DailyBestMs(App.State.Profile, run.DateUtc);
                OpenPopup($"SOLVED IN {Queries.FormatDuration(elapsed)}\nPAR {Queries.FormatDuration(run.ParMs)}   BEST {Queries.FormatDuration(best)}\nSTREAK {App.State.Profile.DailyStreak}");
                AddPopupButton("MENU", new Vector2(0f, -Short * 0.06f),
                    new Vector2(L.ContentWidth / 3f, L.BarHeight), () => App.Screens.Show(new DailyScreen()));
            }
            else if (App.State.Mode == GameMode.Endless)
            {
                // No popup between Endless levels — the streak is in the HUD —
                // but the next board is generated here on the device, and the
                // high grades are seconds of solver work. That goes behind the
                // transition's LOADING card instead of stopping the frame with
                // the solved board still on screen.
                App.Screens.Show(new BoardScreen(),
                    prepare: () => App.Do(GridInfectActions.EndlessAdvance).Applied);
            }
            else
            {
                if (App.State.FreePlayIndex < App.State.FreePlayDefs.Length - 1)
                {
                    App.Do(GridInfectActions.FreePlayAdvance); // no pause between levels
                }
                else
                {
                    App.Do(GridInfectActions.FreePlayComplete, Inputs.Now(GameApp.NowMs()));
                    long duration = Queries.ElapsedMs(App.State.FreePlayRun, GameApp.NowMs());
                    ShowCompletedPopup(duration);
                }
            }
        }

        // ---- free play chrome ----

        void ShowBeginCover()
        {
            _beginCover = new GameObject("beginCover");
            _beginCover.transform.SetParent(Root.transform, false);
            // A recessed cover over the board area only; the HUD stays visible.
            var cover = Ui.MakeGlass("bg", _beginCover.transform,
                new Vector2(ScreenW * PresentationConfig.BoardWidthPct, ScreenH * 0.86f),
                GlassStyle.Well(BoardPalette.Default), 30);
            Ui.SetPos(cover, 0f, -ScreenH * 0.07f);
            var begin = UiButton.Make(_beginCover.transform, "BEGIN",
                new Vector2(0f, 0f), new Vector2(L.ContentWidth * 0.55f, L.ButtonHeight),
                BoardTheme.Primary, BoardTheme.TextOnAccent, () =>
                {
                    App.Do(GridInfectActions.FreePlayBegin, Inputs.Now(GameApp.NowMs()));
                    Buttons.RemoveAll(b => b.Root != null && b.Root.transform.parent == _beginCover.transform);
                    Object.Destroy(_beginCover);
                    _beginCover = null;
                }, 31);
            Buttons.Add(begin);
        }

        public override void Tick(float dt)
        {
            if (_board != null)
            {
                _board.Muted = App.State.Profile.Muted;
                _board.Tick(dt);
            }

            if (App.State.Mode == GameMode.Daily)
            {
                var daily = App.State.DailyRun;
                if (daily == null || daily.Completed) return;
                long elapsedDaily = Queries.ElapsedMs(daily, GameApp.NowMs());
                if (elapsedDaily < 0) elapsedDaily = 0; // a backward clock is refused at daily.complete
                _caption.text = $"{Queries.FormatDuration(elapsedDaily)}   PAR {Queries.FormatDuration(daily.ParMs)}";
                return;
            }
            if (App.State.Mode == GameMode.Endless)
            {
                var endless = App.State.EndlessRun;
                if (endless != null) _caption.text = $"SOLVED {endless.Index}   STREAK {endless.Streak}   BEST {App.State.Profile.EndlessBest[(int)endless.Grade - 1]}";
                return;
            }
            if (App.State.Mode != GameMode.FreePlay) return;
            var run = App.State.FreePlayRun;
            if (run == null || run.Completed) return;

            long elapsed = Queries.ElapsedMs(run, GameApp.NowMs());
            if (elapsed < 0)
            {
                // Cheat guard (MODES §2.2): clock moved backward — abort to menu.
                App.Do(GridInfectActions.FreePlayAbort);
                App.Screens.Show(new FreePlayMenuScreen());
                return;
            }
            _caption.text = $"{App.State.Difficulty}   {App.State.FreePlayIndex + 1}/5   {Queries.FormatDuration(elapsed)}"
                .ToUpperInvariant();
        }

        // ---- popups ----

        void ShowSolvedPopup(System.Action next, System.Action replay)
        {
            OpenPopup("COMPLETE");
            float y = -Short * 0.06f;
            float step = L.ContentWidth / 3f;
            var size = new Vector2(step * 0.82f, L.BarHeight);
            AddPopupButton("MENU", new Vector2(-step, y), size, GoBack);
            AddPopupButton("REPLAY", new Vector2(0f, y), size, replay);
            if (next != null)
            {
                AddPopupButton("NEXT", new Vector2(step, y), size, next);
            }
        }

        void ShowCompletedPopup(long durationMs)
        {
            OpenPopup($"COMPLETED IN:\n{Queries.FormatDuration(durationMs)}");
            AddPopupButton("MENU", new Vector2(0f, -Short * 0.06f),
                new Vector2(L.ContentWidth / 3f, L.BarHeight),
                () => App.Screens.Show(new FreePlayMenuScreen()));
        }

        void OpenPopup(string message)
        {
            _popupOpen = true;
            _backButton.Enabled = false;
            _resetButton.Enabled = false;
            _lockButton.Enabled = false;

            _popup = new GameObject("popup");
            _popup.transform.SetParent(Root.transform, false);
            Ui.MakeRect("dim", _popup.transform, new Vector2(ScreenW, ScreenH), BoardTheme.PanelDim, 40);

            // A glass panel (the chip material at the well's radius), ink type.
            var panel = new GameObject("panel");
            panel.transform.SetParent(_popup.transform, false);
            Ui.MakeGlass("bg", panel.transform, new Vector2(L.ContentWidth, Short * 0.36f), GlassStyle.Panel(BoardPalette.Default), 41);
            var text = Ui.MakeText("message", panel.transform, message, L.HeadingText, BoardTheme.Text, 42);
            Ui.SetPos(text.gameObject, 0f, Short * 0.08f);

            // Slide in (0.15 s, linear).
            panel.transform.localPosition = new Vector3(0f, -ScreenH, 0f);
            App.Tweens.MoveTo(panel.transform, Vector3.zero, PresentationConfig.PopupSlide);
            _popupPanel = panel;
        }

        GameObject _popupPanel;

        // R-602: dismissing the solved popup is the interstitial's moment;
        // the button's own action runs when the ad closes (or at once).
        void AddPopupButton(string label, Vector2 center, Vector2 size, System.Action onClick)
        {
            var button = UiButton.Make(_popupPanel.transform, label, center, size,
                BoardTheme.Primary, BoardTheme.TextOnAccent,
                () => { if (!App.Ads.MaybeShowInterstitial(onClick)) onClick?.Invoke(); }, 43);
            Buttons.Add(button);
        }

        void ClosePopup()
        {
            if (!_popupOpen) return;
            _popupOpen = false;
            _backButton.Enabled = true;
            _resetButton.Enabled = true;
            RefreshLockLabel();
            Buttons.RemoveAll(b => b.Root == null ||
                (b != _backButton && b != _resetButton && b != _lockButton));
            if (_popup != null) Object.Destroy(_popup);
            _popup = null;
            _popupPanel = null;
        }

        // ---- top bar ----

        void GoBack()
        {
            if (App.State.Mode == GameMode.FreePlay)
            {
                App.Do(GridInfectActions.FreePlayAbort);
                App.Screens.Show(new FreePlayMenuScreen());
            }
            else if (App.State.Mode == GameMode.World)
            {
                App.Screens.Show(new WorldLevelSelectScreen(App.State.WorldId));
            }
            else if (App.State.Mode == GameMode.Daily)
            {
                App.Screens.Show(new DailyScreen());
            }
            else if (App.State.Mode == GameMode.Endless)
            {
                App.Do(GridInfectActions.EndlessAbort);
                App.Screens.Show(new EndlessScreen());
            }
            else
            {
                App.Screens.Show(new ClassicSelectScreen());
            }
        }

        void ResetLevel()
        {
            if (_bound == null) return;
            if (_bound.Solved)
            {
                // Replay on a solved level reloads it fresh (MODES §1.1).
                if (App.State.Mode == GameMode.Classic)
                {
                    App.Do(GridInfectActions.LevelLoad, Inputs.LevelLoad(App.State.ClassicLevelId));
                }
                else if (App.State.Mode == GameMode.World)
                {
                    App.Do(GridInfectActions.WorldLoad, Inputs.WorldLoad(App.State.WorldId, App.State.WorldIndex));
                }
            }
            else
            {
                _board.BeginReset();
                App.Do(GridInfectActions.LevelReset);
                _board.EndBatch();
            }
        }
    }
}
