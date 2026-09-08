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

        string _today;
        DateTime _todayDate;
        float _cell, _gap, _pitch, _wellW, _wellH, _wellY, _headerY, _badgeY;
        Vector2 _press;
        bool _pressed, _infoPending;

        int TodayMonth => _todayDate.Year * 12 + _todayDate.Month - 1;
        int EpochMonth => DailyCalendar.Epoch.Year * 12 + DailyCalendar.Epoch.Month - 1;

        static readonly string[] MonthNames =
        {
            "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE",
            "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER",
        };
        static readonly string[] DayNames = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

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
            var title = Ui.MakeText("title", Root.transform, "DAILY", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            var caption = Ui.MakeText("caption", Root.transform, "GI-CAL REV B", S.Px(S.HudCaption), BoardTheme.TextDim, 2, mono: true);
            Ui.SetPos(caption.gameObject, 0f, L.TopBarY + L.HeadingText * 0.95f);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));
            _todayChip = UiButton.Make(Root.transform, "TODAY", new Vector2(-L.BackPos.x, L.BackPos.y), L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => { Go(TodayMonth); Select(_todayDate); });
            Buttons.Add(_todayChip);

            // Month row: an arrow chip each side, the year as silkscreen over the name.
            float monthY = L.TopBarY - S.Px(70f);
            var arrow = new Vector2(S.Px(34f), S.Px(30f));
            _prev = UiButton.Make(Root.transform, "◀", new Vector2(-L.ContentWidth / 2f + arrow.x / 2f, monthY), arrow,
                BoardTheme.ButtonBg, BoardTheme.Text, () => Go(_month - 1));
            _next = UiButton.Make(Root.transform, "▶", new Vector2(L.ContentWidth / 2f - arrow.x / 2f, monthY), arrow,
                BoardTheme.ButtonBg, BoardTheme.Text, () => Go(_month + 1));
            Buttons.Add(_prev);
            Buttons.Add(_next);
            _yearLabel = Ui.MakeText("year", Root.transform, "", S.Px(10f), BoardTheme.TextDim, 2, mono: true);
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

            // Weekday bands: the day and its grade band as silkscreen. Every
            // element is in every daily, so the band is the whole header.
            for (int c = 0; c < Columns; c++)
            {
                var day = (DayOfWeek)((c + 1) % 7);   // Monday first
                float x = (c - (Columns - 1) / 2f) * _pitch;
                var band = DailyCalendar.Band(day);
                string grade = band.min == band.max ? $"G{(int)band.min}" : $"G{(int)band.min}-{(int)band.max}";
                var name = Ui.MakeText($"wk:{c}", Root.transform, DayNames[c], S.Px(10f), BoardTheme.Text, 2, mono: true);
                Ui.SetPos(name.gameObject, x, _headerY + S.Px(12f));
                var gradeText = Ui.MakeText($"band:{c}", Root.transform, grade, S.Px(11f), palette.CopperLo, 2, mono: true);
                Ui.SetPos(gradeText.gameObject, x, _headerY - S.Px(2f));
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
            _yearLabel.text = year.ToString();
            _monthLabel.text = MonthNames[month - 1];
            _prev.Enabled = _month > EpochMonth;
            _next.Enabled = _month < TodayMonth;
            SetChipDim(_prev, !_prev.Enabled);
            SetChipDim(_next, !_next.Enabled);
            SetChipDim(_todayChip, _month == TodayMonth && _selected == _todayDate);

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
                float x = (col - (Columns - 1) / 2f) * _pitch;
                float y = _wellY + ((Rows - 1) / 2f - row) * _pitch;
                bool isToday = date == _todayDate;
                if (date < DailyCalendar.Epoch)
                {
                    Tile($"day:{d}", x, y, GoneStyle(palette), "", Color.clear);
                    continue;
                }
                if (date > _todayDate)
                {
                    Tile($"day:{d}", x, y, FutureStyle(palette), d.ToString(), BoardPalette.Alpha(palette.Ink, 0.28f));
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
                    var pointer = Ui.MakeGlass("pointer", _page.transform, new Vector2(S.Px(6f), S.Px(6f)), GlassStyle.Pad(palette), 9);
                    Ui.SetPos(pointer, x, y + _cell / 2f + S.Px(6f));
                }
                var tile = Tile($"day:{d}", x, y, done ? SolvedStyle(palette) : OpenStyle(palette), d.ToString(),
                    done ? BoardTheme.TextOnAccent : BoardTheme.Text);
                if (done)
                {
                    int px = Mathf.RoundToInt(_cell * 0.32f);
                    var check = Ui.MakeSprite("check", tile.transform, BugGlyph.Check(palette, px), 12);
                    check.transform.localPosition = new Vector3(_cell * 0.28f, -_cell * 0.28f, 0f);
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
            Badge("streak", $"STREAK {streak:00}", -L.ContentWidth / 2f, true, streak > 0);
            Badge("month", $"{MonthNames[month - 1].Substring(0, 3)} {solved:00}/{playable:00}", L.ContentWidth / 2f, false, true);

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
            SetChipDim(_todayChip, _month == TodayMonth && _selected == _todayDate);
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

        void Badge(string name, string text, float edgeX, bool left, bool lit)
        {
            var size = new Vector2(S.Px(S.BadgePadX * 2f + text.Length * S.BadgeText * 0.62f), S.Px(S.BadgePadY * 2f + S.BadgeText * 1.25f));
            float x = left ? edgeX + size.x / 2f : edgeX - size.x / 2f;
            var badge = UiButton.Make(_page.transform, text, new Vector2(x, _badgeY), size,
                GlassStyle.Badge(BoardPalette.Default), lit ? BoardTheme.Copper : BoardPalette.Alpha(BoardTheme.Copper, 0.45f),
                null, 20, pads: false, padAlpha: 1f, mono: true);
            badge.Enabled = false;
        }

        static void SetChipDim(UiButton chip, bool dim)
        {
            chip.Label.color = dim ? BoardTheme.TextDim : BoardTheme.Text;
        }

        // ---- the selected day's slot ----

        void BuildTray(float h, BoardPalette palette)
        {
            float slot = S.Px(S.TraySlot);
            float y = -h / 2f + S.Px(112f);
            float slotX = -L.ContentWidth / 2f + slot / 2f + S.Px(8f);

            var glass = Ui.MakeGlass("slot", Root.transform, new Vector2(slot, slot), GlassStyle.TraySlot(palette, true), 5);
            Ui.SetPos(glass, slotX, y);
            _slotNumber = Ui.MakeText("slot:day", Root.transform, "", slot * 0.42f, BoardTheme.TextOnAccent, 6);
            Ui.SetPos(_slotNumber.gameObject, slotX, y);
            _slotCaption = Ui.MakeText("slot:caption", Root.transform, "", S.Px(S.TrayCaption), BoardTheme.TextDim, 6, mono: true);
            Ui.SetPos(_slotCaption.gameObject, slotX, y - slot / 2f - S.Px(12f));

            float infoX = slotX + slot / 2f + S.Px(22f);
            _dateLine = Ui.MakeText("date", Root.transform, "", S.Px(16f), BoardTheme.Text, 6, anchor: TextAnchor.MiddleLeft);
            Ui.SetPos(_dateLine.gameObject, infoX, y + S.Px(24f));
            _infoLine = Ui.MakeText("band", Root.transform, "", S.Px(11f), BoardTheme.Text, 6, mono: true, anchor: TextAnchor.MiddleLeft);
            Ui.SetPos(_infoLine.gameObject, infoX, y + S.Px(8f));
            _bestLine = Ui.MakeText("best", Root.transform, "", S.Px(11f), BoardTheme.Text, 6, mono: true, anchor: TextAnchor.MiddleLeft);
            Ui.SetPos(_bestLine.gameObject, infoX, y - S.Px(8f));

            _playSize = new Vector2(L.ContentWidth * 0.36f, L.BarHeight);
            _playCentre = new Vector2(infoX + _playSize.x / 2f, y - S.Px(32f));
        }

        // The slot reads the selected day: the date, its band, the bug and
        // cell counts once the board exists (the cache is on it), the best
        // time if it has been solved, and BEGIN or PLAY AGAIN.
        void RefreshSlot()
        {
            string dateUtc = DailySpec.Format(_selected);
            bool solved = Queries.IsDailySolved(App.State.Profile, dateUtc);
            long best = Queries.DailyBestMs(App.State.Profile, dateUtc);
            var band = DailyCalendar.Band(_selected.DayOfWeek);
            string grade = band.min == band.max ? $"G{(int)band.min}" : $"G{(int)band.min}-{(int)band.max}";

            _slotNumber.text = _selected.Day.ToString();
            _slotCaption.text = _selected == _todayDate ? "TODAY" : "PAST";
            _dateLine.text = $"{DayNames[((int)_selected.DayOfWeek + 6) % 7]} {_selected.Day:00} {MonthNames[_selected.Month - 1].Substring(0, 3)}";
            if (DailyCalendar.IsReady(_selected))
            {
                var level = DailyCalendar.For(_selected);
                _infoLine.text = $"{grade} · {level.Def.Specs.Length} BUGS · {CellsToInfect(level)} CELLS";
                _infoPending = false;
            }
            else
            {
                _infoLine.text = grade;
                _infoPending = true;
            }
            _bestLine.text = solved ? $"BEST {Queries.FormatTime(best)}" : _infoPending ? "GENERATING" : "UNPLAYED";

            // BEGIN is the one lit control; PLAY AGAIN is plain glass. The
            // chip is rebuilt rather than restyled, so it is always one object.
            if (_play != null)
            {
                Buttons.Remove(_play);
                UnityEngine.Object.Destroy(_play.Root);
            }
            _play = UiButton.Make(Root.transform, solved ? "PLAY AGAIN" : "BEGIN", _playCentre, _playSize,
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
            float dx = world.x - _press.x;
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
            string dateUtc = DailySpec.Format(date);
            App.Screens.Show(new BoardScreen(), prepare: () =>
                App.Do(GridInfectActions.DailyBegin, Inputs.DailyBegin(dateUtc, GameApp.NowMs())).Applied);
        }

        // ---- materials: the guide's tile table, in glass ----

        static Color White(BoardPalette p, float a) => BoardPalette.Alpha(p.Tip, a);
        static Color Black(BoardPalette p, float a) => BoardPalette.Alpha(p.Shade, a);

        // A past, unplayed day: the dormant component.
        static GlassStyle OpenStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = White(p, 0.62f), FillMid = White(p, 0.28f), MidStop = 0.55f, FillBottom = White(p, 0.4f), Radius = S.TileRadius,
            Border = White(p, 0.35f), BorderPx = 1f, TopLight = White(p, 0.85f),
            Shadow = Black(p, 0.38f), ShadowOffset = new Vector2(0f, -7f), ShadowBlur = 16f,
        };

        // A solved day: infected glass, the light inside the tile.
        static GlassStyle SolvedStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = BoardPalette.Alpha(p.InfectHi, 0.95f), FillMid = p.Infect, MidStop = 0.55f, FillBottom = p.InfectLo, Radius = S.TileRadius,
            Border = White(p, 0.4f), BorderPx = 1f, TopLight = White(p, 0.85f),
            Glow = BoardPalette.Alpha(p.Infect, 0.5f), GlowPx = 14f,
            Shadow = Black(p, 0.38f), ShadowOffset = new Vector2(0f, -7f), ShadowBlur = 16f,
        };

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

        // Today's ring: copper, a point of it, glowing.
        static GlassStyle RingStyle(BoardPalette p) => new GlassStyle
        {
            FillTop = p.CopperHi, FillBottom = p.CopperHi, Radius = S.TileRadius + 2f,
            Glow = BoardPalette.Alpha(p.CopperHi, 0.8f), GlowPx = 10f,
        };
    }

    // Endless: pick a grade, no clock, a streak of solves without a reset.
    public sealed class EndlessScreen : AppScreen
    {
        protected override void Build()
        {
            var title = Ui.MakeText("title", Root.transform, "ENDLESS", L.HeadingText, BoardTheme.Text, 2);
            Ui.SetPos(title.gameObject, 0f, L.TopBarY);
            Buttons.Add(UiButton.Make(Root.transform, "MENU", L.BackPos, L.BackSize,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new MainMenuScreen())));

            var profile = App.State.Profile;
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            for (int g = 1; g <= 5; g++)
            {
                var grade = (Core.Solving.Grade)g;
                float y = L.StackRowY(g - 1, 5, L.ButtonHeight, 0f);
                Buttons.Add(UiButton.Make(Root.transform, $"GRADE {g}", new Vector2(0f, y), size,
                    BoardTheme.ButtonBg, BoardTheme.Text, () =>
                    {
                        // The run seed was picked at boot and its opening
                        // boards have been generating since (Warmup); the
                        // seed enters the log, so the run replays. A board
                        // the cache has not reached yet generates behind
                        // the transition's LOADING card rather than freezing
                        // this menu with its buttons still live.
                        App.Screens.Show(new BoardScreen(), prepare: () =>
                            App.Do(GridInfectActions.EndlessBegin, Inputs.EndlessBegin(grade, (long)App.TakeEndlessSeed())).Applied);
                    }));
                var best = Ui.MakeText($"best:{g}", Root.transform, $"BEST {profile.EndlessBest[g - 1]}",
                    L.LabelText, BoardTheme.Accent, 2);
                Ui.SetPos(best.gameObject, L.ContentWidth / 2f - L.Gap * 2.5f, y);
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
