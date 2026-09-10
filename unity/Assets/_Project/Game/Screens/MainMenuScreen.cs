using GridInfect.Core;
using UnityEngine;
using L = GridInfect.Game.PresentationConfig.Layout;
using O = GridInfect.Game.PresentationConfig.Offer;

namespace GridInfect.Game
{
    // Portrait: a title block in the top third, the four ways in stacked at
    // the middle where a thumb sits, and settings pinned low. Audio, privacy
    // and the progress reset all live behind SETTINGS now rather than
    // stacking up under the menu.
    public sealed class MainMenuScreen : AppScreen
    {
        TitleView _title;

        protected override void Build()
        {
            float h = UnityEngine.Screen.height;

            // The wordmark (STYLE-GUIDE §12) across the content width, the
            // type capped at the old title size so a wide screen does not
            // blow it up. It stands alone: the tagline under it said what
            // the wordmark and the first level already say.
            _title = TitleView.Make(Root.transform, L.ContentWidth, L.TitleText * 1.15f, new Vector2(0f, h * 0.28f), 2);

            // DAILY and ENDLESS replace timed Free Play (stage 4; its actions
            // stay for log replay), and the 128 classic levels live on as
            // LEGACY: unchanged rules, no hints. Four ways in, none of them
            // louder than the others.
            var size = new Vector2(L.ContentWidth, L.ButtonHeight);
            // WORLDS, and plain glass: it says where it goes, and the
            // infection is what a beaten level wears now — a menu row in the
            // same red reads as one already done.
            Buttons.Add(UiButton.Make(Root.transform, Str.MenuWorlds,
                new Vector2(0f, L.StackRowY(0, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new WorldSelectScreen())));
            Buttons.Add(UiButton.Make(Root.transform, Str.MenuDaily,
                new Vector2(0f, L.StackRowY(1, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new DailyScreen())));
            Buttons.Add(UiButton.Make(Root.transform, Str.MenuEndless,
                new Vector2(0f, L.StackRowY(2, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new EndlessScreen())));
            Buttons.Add(UiButton.Make(Root.transform, Str.MenuLegacy,
                new Vector2(0f, L.StackRowY(3, 4, L.ButtonHeight, 0f)), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () => App.Screens.Show(new ClassicSelectScreen())));

            // Settings is a gear in the top-right corner, where every other
            // screen puts its one right-hand chip. As a dimmed full-width row
            // at the bottom it was both hard to read and as loud as the four
            // things that actually are the game.
            //
            // Both corner chips are icon chips at the icon size (39 px on the
            // reference screen, 44 to the finger): they were BarHeight
            // squares, 29 px, and the help mark inside was the label "?" at
            // 12 px — the smallest thing on the menu, for the button a new
            // player needs first.
            var chip = new Vector2(L.IconChip, L.IconChip);
            int icon = UiButton.IconPx(chip);
            var palette = BoardPalette.Default;
            Buttons.Add(UiButton.MakeIcon(Root.transform, "settings", BugGlyph.Gear(palette, icon),
                new Vector2(-L.BackPos.x, L.BackPos.y), chip, () => App.Screens.Show(new SettingsScreen())));

            // The rules sit beside it, reachable before the first tap on
            // anything else — which is when a player wants them. ChipPitch
            // plus a Gap, not a bare gap and not the pitch alone: the pitch
            // is the closest two chips may sit without their copper pads
            // smearing into one another, which is a floor, not a spacing.
            // At the floor the help mark still read as crowding the gear —
            // two marks in one cluster rather than two controls.
            Buttons.Add(UiButton.MakeIcon(Root.transform, "help", BugGlyph.Question(palette, icon),
                new Vector2(-L.BackPos.x - L.ChipPitch(chip.x) - L.Gap, L.BackPos.y), chip,
                () => App.Screens.Show(new RulesScreen())));

            // The tutorial, under the four ways in: a shorter chip, the same
            // glass and the same ink as the rows above it (nothing on this
            // screen is dimmed; hierarchy is size). It is the way back into
            // the series for anyone who skipped it or wants a step again.
            float below = L.StackRowY(3, 4, L.ButtonHeight, 0f) - L.ButtonHeight / 2f - L.Gap - L.BarHeight / 2f;
            Buttons.Add(UiButton.Make(Root.transform, Str.MenuTutorial,
                new Vector2(0f, below), new Vector2(L.ContentWidth * 0.5f, L.BarHeight),
                BoardTheme.ButtonBg, BoardTheme.Text, OpenTutorial));

            // NO ADS, a row under the tutorial and off to the right: smaller
            // than the chip above it, because hierarchy on this screen is
            // size and this is the control a new player needs least. Full
            // ink like everything else — there is no dim ink here (§BoardTheme),
            // and a purchase control is a bad place to start making one.
            // It is here from first launch rather than appearing after
            // the first interstitial — someone who would rather pay than
            // watch should not have to sit through one to learn they can.
            // Once owned it leaves the layout entirely; no spent control is
            // left behind saying "Purchased".
            if (!App.Ads.Purchases.RemoveAdsOwned)
            {
                var noAds = new Vector2(L.ContentWidth * 0.42f, L.BarHeight);
                float x = (L.ContentWidth - noAds.x) / 2f;
                float y = below - L.BarHeight / 2f - L.Gap - noAds.y / 2f;
                Buttons.Add(UiButton.Make(Root.transform, Str.MenuNoAds,
                    new Vector2(x, y), noAds,
                    BoardTheme.ButtonBg, BoardTheme.Text, OpenRemoveAds));
            }

            if (!App.State.Profile.TutorialSeen) OfferTutorial();
        }

        public override void Tick(float dt)
        {
            _title?.Tick(dt);
            if (_offerPhase == OfferPhase.None) return;
            _offerT += dt;
            if (_offerPhase == OfferPhase.In)
            {
                if (OfferIn()) { _offerPhase = OfferPhase.None; SetOfferEnabled(true); }
            }
            else if (OfferOut())
            {
                _offerPhase = OfferPhase.None;
                DestroyOffer();
            }
        }

        // Where the series is picked up: the step after the last one beaten,
        // or the first again once every step has been.
        void OpenTutorial()
        {
            int step = App.State.Profile.TutorialStep;
            int index = step >= TutorialLevels.Count ? 0 : step;
            App.Screens.Show(new BoardScreen(),
                prepare: () => App.Do(GridInfectActions.TutorialLoad, Inputs.Tutorial(index)).Applied);
        }

        // R-701: the one non-consumable. The store sheet and its localised
        // price belong to the purchase SDK, so this hands off and does
        // nothing else; on success the menu rebuilds without the chip.
        // Behind NullPurchaseService the callback reports false and the tap
        // is inert, which is the correct behaviour until the SDK lands.
        void OpenRemoveAds()
        {
            App.Ads.Purchases.BuyRemoveAds(owned =>
            {
                if (owned) App.Screens.Show(new MainMenuScreen(), instant: true);
            });
        }

        // The first open: one offer, answered once either way (tutorial.seen).
        // A solid plate, not the board popup's glass: here it sits over the
        // title and the four rows, and through glass they were the
        // message's background. The menu under it is shut off until it is
        // answered, as the board is under the COMPLETE popup.
        //
        // It arrives rather than being there. The plate used to be built
        // already halfway on — the dim at full strength and the slide
        // already running on the menu's first frame, which is the one frame
        // the title has to itself. Now the menu stands alone for a beat, the
        // screen goes dark, and the plate comes up from under the edge and
        // settles the way the title's bug lands. Its two chips are dead
        // until it has: a plate still in the air is not a thing a finger
        // aimed at.
        //
        // The plate is PlateWidth, not ContentWidth. At the rows' own width
        // it shared both edges with the stack under it and read as one more
        // row rather than as a message on top of them.
        enum OfferPhase { None, In, Out }

        GameObject _offer;
        GameObject _offerPanel;
        SpriteRenderer _offerDim;
        readonly System.Collections.Generic.List<UiButton> _offerButtons = new System.Collections.Generic.List<UiButton>();
        OfferPhase _offerPhase;
        float _offerT;
        float _offerFrom;      // the plate's start, local px below its rest
        bool _menuShut;        // the rows underneath, off

        void OfferTutorial()
        {
            _offer = new GameObject("offer");
            _offer.transform.SetParent(Root.transform, false);
            _offerDim = Ui.MakeRect("dim", _offer.transform,
                new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height),
                BoardPalette.Alpha(BoardTheme.PanelDim, 0f), 40).GetComponent<SpriteRenderer>();

            float short_ = PresentationConfig.ShortEdge;
            float width = L.PlateWidth;
            float height = short_ * 0.40f;
            var panel = new GameObject("panel");
            panel.transform.SetParent(_offer.transform, false);
            _offerPanel = panel;
            Ui.MakeGlass("bg", panel.transform, new Vector2(width, height), GlassStyle.Plate(BoardPalette.Default), 41);
            var title = Ui.MakeText("title", panel.transform, Str.MenuOfferTitle, L.HeadingText, BoardTheme.Text, 42, bold: true);
            Ui.SetPos(title.gameObject, 0f, short_ * 0.10f);
            var line = Ui.MakeText("line", panel.transform, Str.MenuOfferLine, L.BodyText, BoardTheme.Text, 42);
            Ui.SetPos(line.gameObject, 0f, short_ * 0.03f);

            // Two chips inside the plate's own width, a Gap between them and
            // a Gap of plate either side, so the copper pads stay on the
            // plate rather than hanging off its edges.
            float y = -short_ * 0.08f;
            var size = new Vector2((width - L.Gap * 3f) / 2f, L.BarHeight);
            float x = (size.x + L.Gap) / 2f;
            AddOfferButton(UiButton.Make(panel.transform, Str.MenuOfferAccept, new Vector2(-x * L.Dir, y), size,
                BoardTheme.Primary, BoardTheme.TextOnAccent, () =>
                {
                    App.Do(GridInfectActions.TutorialSeen);
                    App.Screens.Show(new BoardScreen(),
                        prepare: () => App.Do(GridInfectActions.TutorialLoad, Inputs.Tutorial(0)).Applied);
                }, 43));
            AddOfferButton(UiButton.Make(panel.transform, Str.MenuOfferSkip, new Vector2(x * L.Dir, y), size,
                BoardTheme.ButtonBg, BoardTheme.Text, () =>
                {
                    App.Do(GridInfectActions.TutorialSeen);
                    CloseOffer();
                }, 43));

            // From under the bottom edge: the whole plate clear of the screen
            // whatever the aspect, so nothing of it is on screen at rest.
            _offerFrom = -(UnityEngine.Screen.height / 2f + height);
            _offerPhase = OfferPhase.In;
            _offerT = 0f;
            OfferIn();
        }

        void AddOfferButton(UiButton button)
        {
            button.Enabled = false;
            _offerButtons.Add(button);
            Buttons.Add(button);
        }

        void SetOfferEnabled(bool enabled)
        {
            foreach (var button in _offerButtons) button.Enabled = enabled;
        }

        // The screen darkens, then the plate rises past its rest and settles
        // back onto it. True once both have finished.
        bool OfferIn()
        {
            float dim = Mathf.Clamp01((_offerT - O.Wait) / O.Dim);
            SetDim(dim);
            // The menu goes dead when the screen starts going dark, not when
            // the offer is built: a menu that looks live and answers nothing
            // is worse than one that visibly has something over it, and the
            // wait is the beat where the player is reading the title. A tap
            // that lands inside it navigates and takes the offer with it —
            // which is right, because it has not been answered, and the menu
            // will make it again next time it is on screen.
            if (!_menuShut && dim > 0f)
            {
                _menuShut = true;
                foreach (var button in Buttons)
                {
                    if (!_offerButtons.Contains(button)) button.Enabled = false;
                }
            }

            float t = Mathf.Clamp01((_offerT - O.Wait - O.RiseAt) / O.Rise);
            // Ease out with a little back in the tail: it arrives with weight
            // rather than coasting to a stop.
            float u = t - 1f;
            SetPlate(1f + (O.Overshoot + 1f) * u * u * u + O.Overshoot * u * u);
            return dim >= 1f && t >= 1f;
        }

        // SKIP: the plate drops back the way it came and takes the dark with
        // it. TUTORIAL does not run this — the navigation's own fade covers
        // the plate leaving.
        bool OfferOut()
        {
            float t = Mathf.Clamp01(_offerT / O.Fall);
            SetPlate(1f - t * t * t);          // ease in: it falls away
            SetDim(1f - Mathf.Clamp01(_offerT / (O.Fall + O.Dim * 0.5f)));
            return _offerT >= O.Fall + O.Dim * 0.5f;
        }

        void SetDim(float k)
        {
            if (_offerDim != null) _offerDim.color = BoardPalette.Alpha(BoardTheme.PanelDim, BoardTheme.PanelDim.a * k);
        }

        // k = 0 off the bottom edge, 1 at rest; unclamped, so the overshoot
        // in the ease can carry the plate past its resting place.
        void SetPlate(float k)
        {
            if (_offerPanel != null)
            {
                _offerPanel.transform.localPosition = new Vector3(0f, Mathf.LerpUnclamped(_offerFrom, 0f, k), 0f);
            }
        }

        void CloseOffer()
        {
            if (_offer == null || _offerPhase == OfferPhase.Out) return;
            SetOfferEnabled(false);
            _offerPhase = OfferPhase.Out;
            _offerT = 0f;
        }

        void DestroyOffer()
        {
            if (_offer == null) return;
            foreach (var button in _offerButtons) Buttons.Remove(button);
            _offerButtons.Clear();
            Object.Destroy(_offer);
            _offer = null;
            _offerPanel = null;
            _offerDim = null;
            foreach (var button in Buttons) button.Enabled = true;
        }
    }
}
