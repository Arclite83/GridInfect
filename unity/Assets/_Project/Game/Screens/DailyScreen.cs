using System;
using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using S = GridInfect.Game.PresentationConfig.Style;
using Grid = GridInfect.Core.Grid;

namespace GridInfect.Game
{
    // Daily: one month of the calendar at a time, in the board's own
    // language. The HUD, a month row of chips, the weekday bands as
    // silkscreen (the ramp is by weekday, so the header never changes),
    // the well holding the days as tiles, two readout badges, and today's
    // board in a tray slot with the one lit control. A solved day is an
    // infected tile, today wears the copper ring, a past day is dormant
    // glass, a future day is out of bounds. A tap selects a day: the slot
    // shows it and its one lit control opens it (behind the loader when
    // the cache has not generated it yet). Swipe pages months.
    public sealed class DailyScreen : AppScreen
    {
        const int Columns = 7;
        const int Rows = 6;
        const float SwipePct = 0.15f;      // of the short edge

        static int _month = -1;            // year * 12 + month - 1, remembered across visits

        GameObject _page, _selection;
        TextMesh _monthLabel, _yearLabel, _slotNumber, _slotCaption, _dateLine, _infoLine, _bestLine;
        UiButton _prev, _next, _todayChip, _play;
        DateTime _selected;
        readonly System.Collections.Generic.List<(Rect bounds, Vector2 centre, DateTime date)> _tiles =
            new System.Collections.Generic.List<(Rect, Vector2, DateTime)>();
        Vector2 _playCentre, _playSize;
        GameObject _slotGlass;
        float _slotX, _slotY, _slotSize;

        string _today;
        DateTime _todayDate;
        float _cell, _gap, _pitch, _wellW, _wellH, _wellY, _headerY, _badgeY;
        Vector2 _press;
        bool _pressed, _infoPending;

        int TodayMonth => _todayDate.Year * 12 + _todayDate.Month - 1;
        int EpochMonth => DailyCalendar.Epoch.Year * 12 + DailyCalendar.Epoch.Month - 1;


        protected override void Build()
        {
            float h = UnityEngine.Screen.height;
            var palette = BoardPalette.Default;
            _today = GameApp.TodayUtc();
            DailySpec.TryParseDate(_today, out _todayDate);
            if (_month < 0) _month = TodayMonth;
            _month = Mathf.Clamp(_month, EpochMonth, TodayMonth);
            _selected = _todayDate;

            // HUD (§7): the mode label between two chips.
            var title = Ui.MakeText("title", Root.transform, Str.DailyTitle, L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            var caption = Ui.MakeText("caption", Root.transform, "GI-CAL REV B", S.Px(S.HudCaption), BoardTheme.TextDim, 2, mono: true);
            Ui.SetPos(caption.gameObject, 0f, L.TopBarY + L.HeadingText * 0.95f);
            Buttons.Add(UiButton.Make(Root.transform, Str.NavMenu, L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));
            _todayChip = UiButton.Make(Root.transform, Str.DailyToday, new Vector2(-L.BackPos.x, L.BackPos.y), L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => { Go(TodayMonth); Select(_todayDate); });
            Buttons.Add(_todayChip);

            // Month row: an arrow chip each side, the year as silkscreen over the name.
            float monthY = L.TopBarY - S.Px(70f);
            var arrow = new Vector2(S.Px(40f), S.Px(34f));
            int arrowPx = UiButton.IconPx(arrow);
            _prev = UiButton.MakeIcon(Root.transform, "prev", BugGlyph.Prev(palette, arrowPx),
                new Vector2(L.Lead(arrow.x / 2f), monthY), arrow, () => Go(_month - 1));
            _next = UiButton.MakeIcon(Root.transform, "next", BugGlyph.Next(palette, arrowPx),
                new Vector2(L.Trail(arrow.x / 2f), monthY), arrow, () => Go(_month + 1));
            Buttons.Add(_prev);
            Buttons.Add(_next);
            _yearLabel = Ui.MakeText("year", Root.transform, "", S.Px(S.SmallText), BoardTheme.TextDim, 2, mono: true);
            Ui.SetPos(_yearLabel.gameObject, 0f, monthY + S.Px(15f));
            _monthLabel = Ui.MakeText("month", Root.transform, "", L.HeadingText * 1.1f, BoardTheme.Text, 2);
            Ui.SetPos(_monthLabel.gameObject, 0f, monthY - S.Px(4f));

            // Geometry: seven tiles across the content width at the guide's gap and well padding.
            _wellW = L.ContentWidth;
            _gap = S.Px(S.Gap);
            float pad = S.Px(S.WellPad);
            _cell = (_wellW - 2f * pad - (Columns - 1) * _gap) / Columns;
            _pitch = _cell + _gap;
            _wellH = 2f * pad + Rows * _cell + (Rows - 1) * _gap;
            _headerY = monthY - S.Px(54f);
            float wellTop = _headerY - S.Px(22f);
            _wellY = wellTop - _wellH / 2f;
            _badgeY = wellTop - _wellH - S.Px(28f);

            // Weekday bands: the day and its tier band as silkscreen. Every
            // element is in every daily, so the band is the whole header.
            for (int c = 0; c < Columns; c++)
            {
                var day = (DayOfWeek)((c + 1) % 7);   // Monday first
                float x = L.ColumnX(c, Columns, _pitch);   // and on the leading side
                var band = DailyCalendar.Band(day);
                string tier = Str.TierBandLabel(band.min, band.max);
                var name = Ui.MakeText($"wk:{c}", Root.transform, Str.Day(c + 1), S.Px(S.SmallText), BoardTheme.Text, 2, mono: true);
                Ui.SetPos(name.gameObject, x, _headerY + S.Px(12f));
                // The tier was CopperLo, 2.3:1 on the mask at 11 px: the
                // one number that says how hard the day is, in the least
                // legible ink on the screen. Ink now; the day name above it
                // is what carries the header's second tone.
                var tierText = Ui.MakeText($"band:{c}", Root.transform, tier, S.Px(12f), BoardTheme.Text, 2, mono: true);
                Ui.SetPos(tierText.gameObject, x, _headerY - S.Px(2f));
            }

            // The well (§4).
            var well = Ui.MakeGlass("well", Root.transform, new Vector2(_wellW, _wellH), GlassStyle.Well(palette), 5);
            Ui.SetPos(well, 0f, _wellY);

            // The selected day's board in a tray slot (§8) with the one lit control.
            BuildTray(h, palette);
            BuildPage();
            Select(_todayDate);
        }

        // ---- month page ----

        void Go(int month)
        {
            month = Mathf.Clamp(month, EpochMonth, TodayMonth);
            if (month == _month) return;
            _month = month;
            BuildPage();
        }

        void BuildPage()
        {
            if (_page != null) UnityEngine.Object.Destroy(_page);
            _page = new GameObject("month");
            _page.transform.SetParent(Root.transform, false);
            _tiles.Clear();

            int year = _month / 12, month = _month % 12 + 1;
            _yearLabel.text = Str.Num(year);
            _monthLabel.text = Str.Month(month);
            _prev.Enabled = _month > EpochMonth;
            _next.Enabled = _month < TodayMonth;
            _prev.SetDim(!_prev.Enabled);
            _next.SetDim(!_next.Enabled);
            _todayChip.SetDim(_month == TodayMonth && _selected == _todayDate);

            var palette = BoardPalette.Default;
            var profile = App.State.Profile;
            var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            int days = DateTime.DaysInMonth(year, month);
            int lead = ((int)first.DayOfWeek + 6) % 7;   // Monday = 0
            int playable = 0, solved = 0;
            for (int i = 0; i < Columns * Rows; i++)
            {
                int d = i - lead + 1;
                if (d < 1 || d > days) continue;
                var date = first.AddDays(d - 1);
                int col = i % Columns, row = i / Columns;
                float x = L.ColumnX(col, Columns, _pitch);
                float y = _wellY + ((Rows - 1) / 2f - row) * _pitch;
                bool isToday = date == _todayDate;
                if (date < DailyCalendar.Epoch)
                {
                    Tile($"day:{d}", x, y, GoneStyle(palette), "", Color.clear);
                    continue;
                }
                if (date > _todayDate)
                {
                    // Not pressable, so it may sit under the contrast floor,
                    // but 28% was a number you had to hunt for. 45% still
                    // reads as "not yet" next to a full-ink day.
                    Tile($"day:{d}", x, y, FutureStyle(palette), Str.Num(d), BoardPalette.Alpha(palette.Ink, 0.45f));
                    continue;
                }
                playable++;
                bool done = Queries.IsDailySolved(profile, DailySpec.Format(date));
                if (done) solved++;
                if (isToday)
                {
                    // The copper ring: a pad-coloured plate one ring wider under the tile, and a pointer above.
                    var ring = Ui.MakeGlass("ring", _page.transform, new Vector2(_cell + S.Px(4f), _cell + S.Px(4f)), RingStyle(palette), 9);
                    Ui.SetPos(ring, x, y);
                    var pointerStyle = GlassStyle.Pad(palette);
                    pointerStyle.FillTop = pointerStyle.FillBottom = BoardPalette.Alpha(palette.Copper, 0.8f);
                    pointerStyle.Glow = Color.clear;
                    var pointer = Ui.MakeGlass("pointer", _page.transform, new Vector2(S.Px(6f), S.Px(6f)), pointerStyle, 9);
                    Ui.SetPos(pointer, x, y + _cell / 2f + S.Px(6f));
                }
                var tile = Tile($"day:{d}", x, y, done ? BoardTheme.TileSolved() : BoardTheme.TileOpen(), Str.Num(d),
                    done ? BoardTheme.TextOnAccent : BoardTheme.Text);
                if (done)
                {
                    Ui.MarkSolved(tile.transform, _cell);
                }
                else
                {
                    float dot = S.Px(S.ChipPadDot);
                    var padStyle = GlassStyle.Pad(palette);
                    padStyle.FillTop = padStyle.FillBottom = BoardPalette.Alpha(palette.Copper, 0.8f);
                    padStyle.Glow = Color.clear;
                    var padDot = Ui.MakeGlass("pad", tile.transform, new Vector2(dot, dot), padStyle, 12);
                    Ui.SetPos(padDot, _cell * 0.36f, -_cell * 0.36f);
                }
                _tiles.Add((new Rect(x - _cell / 2f, y - _cell / 2f, _cell, _cell), new Vector2(x, y), date));
            }

            // Readouts (§7 counter style): the streak, and solved over playable this month.
            int streak = Queries.DailyStreakOn(profile, _today);
            // Both readouts in full copper: a zero streak used to dim its
            // badge to 2:1, which made the fact of a zero streak the thing
            // you could not read.
            Badge("streak", Str.Fmt(Str.DailyStreak, streak), L.Lead(0f), true);
            Badge("month", Str.Fmt(Str.DailyMonthCount, Str.MonthShort(month), solved, playable), L.Trail(0f), false);

            // The month's unplayed days go to the worker, newest first, behind
            // today's board and the recent archive.
            Warmup.ForMonth(LevelCache.Shared, year, month, _todayDate, profile);
            ShowSelection();
        }

        // The selected day: a lit ring on its tile (today's copper ring says
        // it already), and the slot read from it. Its board goes to the
        // front of the worker's queue so the slot fills in and BEGIN is instant.
        void Select(DateTime date)
        {
            _selected = date;
            LevelCache.Shared.Prefetch(DailySpec.For(date.DayOfWeek), DailySpec.SeedFor(date), -1);
            ShowSelection();
            RefreshSlot();
            _todayChip.SetDim(_month == TodayMonth && _selected == _todayDate);
        }

        void ShowSelection()
        {
            if (_selection != null) UnityEngine.Object.Destroy(_selection);
            _selection = null;
            if (_selected == _todayDate) return;
            foreach (var (bounds, centre, date) in _tiles)
            {
                if (date != _selected) continue;
                _selection = Ui.MakeGlass("selected", _page.transform, new Vector2(_cell + S.Px(4f), _cell + S.Px(4f)), SelectedStyle(BoardPalette.Default), 9);
                Ui.SetPos(_selection, centre.x, centre.y);
                return;
            }
        }

        GameObject Tile(string name, float x, float y, GlassStyle style, string label, Color textColor)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_page.transform, false);
            Ui.SetPos(root, x, y);
            Ui.MakeGlass("glass", root.transform, new Vector2(_cell, _cell), style, 10);
            if (label.Length > 0)
            {
                var text = Ui.MakeText("label", root.transform, label, _cell * 0.38f, textColor, 11);
                Ui.SetPos(text.gameObject, 0f, 0f);
            }
            return root;
        }

        // `leading`: hung inward from the leading edge, else from the trailing.
        void Badge(string name, string text, float edgeX, bool leading)
        {
            var size = new Vector2(S.Px(S.BadgePadX * 2f + text.Length * S.BadgeText * 0.62f), S.Px(S.BadgePadY * 2f + S.BadgeText * 1.25f));
            float x = leading ? edgeX + L.Dir * size.x / 2f : edgeX - L.Dir * size.x / 2f;
            var badge = UiButton.Make(_page.transform, text, new Vector2(x, _badgeY), size,
                GlassStyle.Badge(BoardPalette.Default), BoardTheme.Copper,
                null, 20, pads: false, padAlpha: 1f, mono: true);
            badge.Enabled = false;
        }

        // ---- the selected day's slot ----

        void BuildTray(float h, BoardPalette palette)
        {
            float slot = S.Px(S.TraySlot);
            float y = -h / 2f + S.Px(112f);
            float slotX = L.Lead(slot / 2f + S.Px(8f));

            // The slot is the selected day, so it is that day's tile at
            // size: the same glass the calendar draws, infected once solved.
            // It used to be the tray's black recess, which read as a hole
            // punched in the screen next to thirty pieces of light glass.
            _slotX = slotX;
            _slotY = y;
            _slotSize = slot;
            _slotNumber = Ui.MakeText("slot:day", Root.transform, "", slot * 0.42f, BoardTheme.Text, 8);
            Ui.SetPos(_slotNumber.gameObject, slotX, y);
            _slotCaption = Ui.MakeText("slot:caption", Root.transform, "", S.Px(S.TrayCaption), BoardTheme.TextDim, 6, mono: true);
            Ui.SetPos(_slotCaption.gameObject, slotX, y - slot / 2f - S.Px(12f));

            // The readouts hang off the slot inward, along the reading direction.
            float infoX = slotX + (slot / 2f + S.Px(22f)) * L.Dir;
            float readout = S.Px(12f);
            float bestY = y - S.Px(8f);
            _dateLine = Ui.MakeText("date", Root.transform, "", S.Px(16f), BoardTheme.Text, 6, anchor: L.Leading);
            Ui.SetPos(_dateLine.gameObject, infoX, y + S.Px(24f));
            _infoLine = Ui.MakeText("band", Root.transform, "", readout, BoardTheme.Text, 6, mono: true, anchor: L.Leading);
            Ui.SetPos(_infoLine.gameObject, infoX, y + S.Px(8f));
            _bestLine = Ui.MakeText("best", Root.transform, "", readout, BoardTheme.Text, 6, mono: true, anchor: L.Leading);
            Ui.SetPos(_bestLine.gameObject, infoX, bestY);

            // BEGIN is a lit chip, and a lit chip is bigger than its box: it
            // carries L.ChipGlow of halo all the way round. Centred a flat
            // 32 px down it cleared COMPLETE by two pixels and its halo
            // cleared nothing at all, so the readout's last line sat inside
            // the glow. The chip is hung off that line instead: half the
            // line, the halo, a gap, half the chip.
            _playSize = new Vector2(L.ContentWidth * 0.36f, L.BarHeight);
            _playCentre = new Vector2(infoX + _playSize.x / 2f * L.Dir,
                bestY - readout / 2f - L.ChipGlow - S.Px(S.Gap) - _playSize.y / 2f);
        }

        // The slot reads the selected day: the date, its tier band, the bug
        // and cell counts once the board exists (the cache is on it), whether
        // it is done, and BEGIN or PLAY AGAIN. No clock: the daily is scored
        // on solving it, and most people play this game with a cup of tea.
        void RefreshSlot()
        {
            string dateUtc = DailySpec.Format(_selected);
            bool solved = Queries.IsDailySolved(App.State.Profile, dateUtc);
            var band = DailyCalendar.Band(_selected.DayOfWeek);
            string tier = Str.TierBandLabel(band.min, band.max);

            if (_slotGlass != null) UnityEngine.Object.Destroy(_slotGlass);
            _slotGlass = Ui.MakeGlass("slot", Root.transform, new Vector2(_slotSize, _slotSize),
                solved ? BoardTheme.TileSolved() : BoardTheme.TileOpen(), 5);
            Ui.SetPos(_slotGlass, _slotX, _slotY);
            _slotNumber.color = solved ? BoardTheme.TextOnAccent : BoardTheme.Text;
            _slotNumber.text = Str.Num(_selected.Day);
            _slotCaption.text = _selected == _todayDate ? Str.DailyToday : Str.DailyPast;
            _dateLine.text = Str.Fmt(Str.DailyDateLine, Str.Day(((int)_selected.DayOfWeek + 6) % 7 + 1),
                _selected.Day, Str.MonthShort(_selected.Month));
            if (DailyCalendar.IsReady(_selected))
            {
                var level = DailyCalendar.For(_selected);
                _infoLine.text = tier + " \u00b7 " + Str.Fmt(Str.DailyBugs, level.Def.Specs.Length)
                    + " \u00b7 " + Str.Fmt(Str.DailyCells, CellsToInfect(level));
                _infoPending = false;
            }
            else
            {
                _infoLine.text = tier;
                _infoPending = true;
            }
            _bestLine.text = solved ? Str.DailyComplete : _infoPending ? Str.DailyGenerating : Str.DailyUnplayed;

            // BEGIN is the one lit control; PLAY AGAIN is plain glass. The
            // chip is rebuilt rather than restyled, so it is always one object.
            if (_play != null)
            {
                Buttons.Remove(_play);
                UnityEngine.Object.Destroy(_play.Root);
            }
            _play = UiButton.Make(Root.transform, solved ? Str.DailyPlayAgain : Str.DailyBegin, _playCentre, _playSize,
                solved ? BoardTheme.ButtonBg : BoardTheme.Primary, solved ? BoardTheme.Text : BoardTheme.TextOnAccent,
                () => Play(_selected));
            Buttons.Add(_play);
        }

        static int CellsToInfect(PlayableLevel level)
        {
            int n = 0;
            for (int loc = 0; loc < Grid.Cells; loc++) if (level.Def.BoardAt(loc) == Cell.Active) n++;
            return n;
        }

        public override void Tick(float dt)
        {
            if (_infoPending && DailyCalendar.IsReady(_selected)) RefreshSlot();
        }

        // ---- input: a tap opens a day, a swipe pages the month ----

        public override bool OnPress(Vector2 world)
        {
            _press = world;
            _pressed = true;
            return true;
        }

        public override void OnRelease(Vector2 world)
        {
            if (!_pressed) return;
            _pressed = false;
            // A swipe against the reading direction is "next", so the
            // gesture mirrors with the pager it stands in for.
            float dx = (world.x - _press.x) * L.Dir;
            if (Mathf.Abs(dx) > L.ShortEdgeUnit * SwipePct)
            {
                Go(dx < 0f ? _month + 1 : _month - 1);
                return;
            }
            if ((world - _press).magnitude > _cell * 0.5f) return;
            foreach (var (bounds, centre, date) in _tiles)
            {
                if (bounds.Contains(world)) { Select(date); return; }
            }
        }

        void Play(DateTime date)
        {
            // Date and clock are the adapter's inputs; both enter the log. The
            // dispatch runs behind the transition's LOADING card
            // (ScreenManager.Show): a cached board lands at once, an
            // ungenerated one generates there, and a rejection cancels the
            // navigation instead of landing on an empty board.
            // To the front of the queue, so the card is waiting on something
            // the worker is actually on: `ready` only ever clears because a
            // prefetch landed.
            string dateUtc = DailySpec.Format(date);
            LevelCache.Shared.Prefetch(DailySpec.For(date.DayOfWeek), DailySpec.SeedFor(date), -1);
            App.Screens.Show(new BoardScreen(),
                prepare: () => App.Do(GridInfectActions.DailyBegin,
                    Inputs.DailyBegin(dateUtc, GameApp.NowMs())).Applied,
                ready: () => DailyCalendar.IsReady(date));
        }

        // ---- materials: the guide's tile table, in glass ----

        static Color White(BoardPalette p, float a) => BoardPalette.Alpha(p.Tip, a);
        static Color Black(BoardPalette p, float a) => BoardPalette.Alpha(p.Shade, a);

        // A day not out yet: out of bounds, one shade deeper than the well.
        static GlassStyle FutureStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = Black(p, 0.12f), FillBottom = Black(p, 0.12f), Radius = S.TileRadius,
            Border = Black(p, 0.12f), BorderPx = 1f,
        };

        // Before the epoch: nothing there.
        static GlassStyle GoneStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = Black(p, 0.05f), FillBottom = Black(p, 0.05f), Radius = S.TileRadius,
            Border = Black(p, 0.1f), BorderPx = 1f,
        };

        // The selected day's ring: lit glass, so it reads apart from today's copper.
        static GlassStyle SelectedStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = White(p, 0.85f), FillBottom = White(p, 0.85f), Radius = S.TileRadius + 2f,
            Glow = White(p, 0.5f), GlowPx = 8f,
        };

        // Today's ring: copper, and only copper. It was CopperHi at full
        // strength under a 10 px glow, which put it level with the lit
        // selection ring and left the calendar with two things shouting. A
        // ring says which day is today; the selection is what the screen is
        // pointing at, and only one of the two needs to glow.
        static GlassStyle RingStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = BoardPalette.Alpha(p.Copper, 0.7f), FillBottom = BoardPalette.Alpha(p.Copper, 0.7f),
            Radius = S.TileRadius + 2f,
        };
    }

    // Endless: pick a tier, no clock, a streak of solves without a reset.
    public sealed class EndlessScreen : AppScreen
    {
        protected override void Build()
        {
            var title = Ui.MakeText("title", Root.transform, Str.EndlessTitle, L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, Str.NavMenu, L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var profile = App.State.Profile;
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            for (int g = 1; g <= 5; g++)
            {
                var grade = (Core.Solving.Grade)g;
                float y = L.StackRowY(g - 1, 5, L.ButtonHeight, 0f);
                Buttons.Add(UiButton.Make(Root.transform, Str.Fmt(Str.TierName, g), new Vector2(0f, y), size,
                    BoardTheme.ButtonBg, BoardTheme.Text, () =>
                    {
                        // The seed is taken here rather than inside prepare:
                        // the loading card has to know which board it is
                        // waiting on, and taking it now also starts the run's
                        // boards generating a beat earlier. It enters the log,
                        // so the run still replays.
                        ulong seed = App.TakeEndlessSeed();
                        Warmup.ForEndlessRun(LevelCache.Shared, grade, seed);
                        App.Screens.Show(new BoardScreen(),
                            prepare: () => App.Do(GridInfectActions.EndlessBegin,
                                Inputs.EndlessBegin(grade, (long)seed)).Applied,
                            ready: () => LevelCache.Shared.Has(DailySpec.Endless(grade), seed));
                    }));
                // Hung off the row's right edge, not centred on a fixed x:
                // a centred readout grows both ways, so BEST 100 reached
                // past the row that carries it while BEST 7 sat somewhere
                // else again. Right-anchored, the column is straight and
                // nothing can walk over the edge.
                var best = Ui.MakeText($"best:{g}", Root.transform, Str.Fmt(Str.EndlessBest, profile.EndlessBest[g - 1]),
                    L.LabelText, BoardTheme.Accent, 2, anchor: L.Trailing);
                Ui.SetPos(best.gameObject, L.Trail(L.Gap), y);
            }
        }
    }

    // Where a completed daily's time goes beyond the local profile. Friends
    // leaderboards (Play Games Services v2) are out of stage 4; the shipped
    // sink keeps it local.
    public interface IDailyScoreSink
    {
        void Submit(string dateUtc, long elapsedMs);
    }

    public sealed class LocalDailyScoreSink : IDailyScoreSink
    {
        public void Submit(string dateUtc, long elapsedMs) { }
    }
}
