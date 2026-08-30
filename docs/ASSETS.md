# Grid Infect — Asset Inventory & Presentation Spec

All gameplay assets live flat in `grid-infect-cocos2dx/Resources/`
(plus `Resources/fonts/`). cocos2d-x multi-resolution directories
(`iphone/ipad/ipadhd`, scaffolded in `Classes/AppMacros.h`) are **not
used** — one asset set serves all devices, and `AppDelegate.cpp` never
calls `setDesignResolutionSize`, so every scene lays out by percentages
of the actual screen (`getVisibleSize()`); asset scale factors below are
relative to screen height/width, not pixels.

"Used" below = referenced from `Classes/` source. Eleven files are
shipped but unreferenced (§5) — leftovers from an earlier UI pass.

---

## 1. Art inventory (Resources/, PNG unless noted)

### Board & pieces (the core set a port must re-skin)

| File | Size (px) | Role |
|---|---|---|
| `tile_gray.png` | 150×150 | base tile sprite every cell starts as (invisible for voids) |
| `tile_blue.png` | 150×150 | cell value 1 — active, uninfected |
| `tile_red.png` | 150×150 | cell value 4 — infected |
| `tile_yellow.png` | 150×150 | cell value 2 — wall |
| `tile_purple.png` | 150×150 | cell value 3 — repel switch |
| `tile_black.png` | 150×150 | cell value 5 — reset trap |
| `piece_L.png` … `piece_LRUD.png` (15 files) | 150×150 | one per tile type; filename = arm set (`piece_RU.png` etc.) |
| `event_placeholder.png` | 1×1 | invisible node (tag 999) used purely as the anchor for the 0.3 s resolution timer (`LevelMenuScene.cpp`) |

### Screens & chrome (used)

| File | Size | Role |
|---|---|---|
| `background.png` | 1920×1080 | all scenes, stretched to screen |
| `logo.png` | 1252×230 | main menu logo |
| `popup_bg.png` / `popup_bg_pressed.png` | 974×732 | mode cards, difficulty cards, BEGIN/COMPLETE popup, info popup |
| `level_box.png` / `level_box_pressed.png` | 140×140 | classic level-select cell |
| `padlock.png` | 50×50 | locked level overlay (opacity 192) |
| `message_1/3/5/10/26/29.png` | 2060×1548 | tutorial overlays on classic ids 0/2/4/9/25/28; **all tutorial text is baked into these bitmaps** — sticky-note art, transcribed in §7. The fiction calls pieces "bugs" |
| `btn_up.png`, `btn_down.png` (+`_pressed`) | 136×68 | classic menu paging |
| `btn_menu.png`, `btn_replay.png`, `btn_next.png` | 100×100 | solved-popup buttons (no distinct pressed art) |
| `btn_menu_framed.png`, `btn_replay_framed.png`, `btn_home_framed.png`, `btn_info_framed.png`, `btn_achievements_framed.png` (+`_pressed`) | 140×140 | in-level / menu chrome buttons |
| `btn_leaderboards_framed.png` (+`_pressed`) | 364×140 | free-play leaderboard buttons |
| `btn_mute_on.png`, `btn_mute_off.png` (+`_pressed`) | 140×140 | mute toggle (all scenes) |
| `gplay_promo_bg.png` | 931×700 | first-run Google sign-in promo |
| `btn_g+.png` / `btn_g+_pressed.png` | 140×140 | **Google+ branding** (§4) |
| `Red-signin_Long_base.png` / `_press.png` | 492×108 | **Google's official red sign-in button art** (§4) |
| `btn_no_thanks.png` | 492×108 | promo decline button |

## 2. Audio, fonts, strings

| File | Details | Role |
|---|---|---|
| `Resources/POL-pencil-maze-long.wav` | 48.0 s, 44.1 kHz, 16-bit stereo, 8.5 MB | background music, looped from the main menu on (`SoundManager::playBgMusic`), paused/resumed on app background (`AppDelegate.cpp`). **Third-party licensed track** (§4) |
| `Resources/click.wav` | 0.03 s, 44.1 kHz, 16-bit stereo | played on piece pickup and successful drop (`LevelMenuScene.cpp` `ccTouchBegan`/`ccTouchEnded`) |
| `Resources/fonts/Overhaul.ttf` | 1.2 MB TTF | the only font; every runtime string uses it. Name table says: "This font is in the public domain." (family "Overhaul", made with FontLab) — see §4 |

**Strings**: there is no localization system and no strings file. All
runtime text is hardcoded English literals in `Classes/Renderers/*.cpp`:
`"Level N"`, `"FREE PLAY"`, `"BEGIN"`, `"COMPLETE"`,
`"COMPLETED IN: "`, difficulty names, `"n/5"`,
`"<DIFF>\nBEST TIME:\n…"`, `"--:--:---"`, `"PLAY <DIFF>\nn MORE
TIME(S)"`, `"<DIFF>\nLOCKED"`,
`"CLASSIC MODE\n \n128 LEVELS\nOF INCREASING\nDIFFICULTY"`,
`"FREE PLAY\n \nSOLVE 5 LEVELS,\nCOMPETE FOR THE\nFASTEST TIMES"`, and
the info popup (`"Bloodhound Studios\nwww.bloodhoundstudios.com\n \nBased
in Unionville, CT\nCopyright 2014\n \nCreated By: Christopher Mahar"`).
Tutorial copy exists only inside `message_*.png` bitmaps.

## 3. Platform-shell assets (outside Resources/)

- `proj.ios/`: `Icon-29/40/50/57/58/72/76/80/100/114/120/144/152.png`
  app icons; `Default*.png` launch images (iPhone/iPad,
  portrait/landscape); `Info.plist` forces landscape
  (`UISupportedInterfaceOrientations` — the game is landscape-only).
- `proj.android/res/drawable-*dpi/icon.png` launcher icons;
  `res/values/strings.xml` (app name + GPGS `app_id` 478989149128).
- `proj.wp8-xaml/.../SplashScreenImage.jpg`,
  `proj.tizen/.../mainmenu.png`, `proj.blackberry/icon.png` — template
  shells.
- `Resources/bloodhound_studios_splash.jpg` (90 KB) is **unreferenced**
  in code (§5).

## 4. Licensed / third-party content flags

| Asset | Status |
|---|---|
| `POL-pencil-maze-long.wav` | `POL-` prefix and filename match PlayOnLoop.com's catalog naming ("Pencil Maze"). PlayOnLoop tracks are royalty-free under an attribution license (or paid no-attribution license). **License and purchase records unverified — do not ship without checking** (§6) |
| `Red-signin_Long_base/press.png` | Google's official Google+ sign-in button assets (from the Google+ iOS SDK asset pack). Google branding — dead service; do not reuse |
| `btn_g+.png` (+pressed) | Google+ logo derivative. Same as above |
| `Overhaul.ttf` | Embedded name table: copyright "This font is in the public domain."; trademark field asks to refer to copyright notices. Likely the free "Overhaul" display font. Low risk, provenance unverified (§6) |
| Google Play Games / Google+ / GoogleOpenSource frameworks, Crashlytics.framework | Binary SDKs, removed from the repo in the hygiene commit; were vendored third-party code, dead services |
| Everything else (tiles, pieces, buttons, popups, messages, logo, click.wav) | Presumed first-party Bloodhound Studios art; no contrary marking found |

## 5. Shipped but unreferenced files

No code path loads these (verified by grep across `Classes/`,
`proj.android/src`, `proj.ios`): `bloodhound_studios_splash.jpg`,
`btn_achievements.png`, `btn_classic.png`, `btn_classic_pressed.png`,
`btn_free_play.png`, `btn_free_play_pressed.png`, `btn_home.png`,
`btn_leaderboards.png`, `btn_start.png`, `loading.png`,
`shadow_box.png`. They ship in the package but never draw. (Unframed
`btn_home`/`btn_leaderboards`/`btn_achievements` have framed variants
that ARE used.)

## 6. Presentation timing & feel (what the original actually did)

Document of record for how the game *reads*, independent of the art.
All motion uses `CCMoveTo` — **linear, no easing curves anywhere in the
game code**. All durations from `Classes/Renderers/*.cpp`.

### Frame rate

- `pDirector->setAnimationInterval(1.0 / 90)` — a 90 fps target
  (`Classes/AppDelegate.cpp`), unusually high for 2014; actual rate was
  device/vsync-limited.

### Board layout metrics (LevelMenuScene::init)

- Cell sprite height = **11% of screen height**; cell pitch = size ×
  1.05 both axes (5% gutters).
- Grid anchored: column 5 (of 0–10) at screen-center X; row 0 centered
  at 80% of screen height, rows descending.
- Piece tray: pieces at 7.5% of screen height from the bottom, scale =
  11% of screen height, slot pitch = piece width × 1.1 × 1.1, 8 slots
  centered on slot index 3.
- Cells render at opacity 0 (voids) or 255 — no intermediate fades.

### Input-to-feedback

| Event | Feedback | Delay/duration |
|---|---|---|
| Touch a tray/board piece | `click.wav`; if the piece was placed it is cleared from the board **instantly** (all cell texture swaps same frame); any pending 0.3 s resolution is cancelled | 0 ms |
| Drag | piece sprite tracks the finger 1:1 (position = touch location, no smoothing) | 0 ms |
| Drop on a legal cell | piece animates to cell center, `click.wav`; **infection spread applies instantly this frame** (every infected cell's texture swaps at once — there is no per-cell cadence, wavefront, or stagger) | move: 0.10 s linear |
| Spread resolution (win check → reset or repels) | scheduled after the drop | **0.30 s** delay (`CCDelayTime(0.3f)` on node tag 999; cancellable — `RULES.md` §4.1) |
| Repel retraction / full reset | all affected cells swap texture at once when resolution runs | instant at +0.30 s |
| Drop on an illegal cell / off board | piece animates back to its tray slot | 0.15 s linear |
| Win | popup sequence (below) | starts at +0.30 s after the winning drop |

**The infection has no propagation animation.** The "spread" reads as an
instantaneous flood-fill; the only temporal staging in the whole
mechanic is the 0.3 s beat between the flood and its consequences
(repel retraction / board reset / COMPLETE popup). A port that animates
the wavefront is changing presentation the original did not have — and
must keep resolution cancellable-by-touch within the window if it wants
to preserve the behavior of levels 41/87 (`RULES.md` §4.1).

### Cell state change

A cell changing state is a **texture swap only** (`onChangeBoardIndex` →
`setTexture`). No tween, flash, scale pop, or particles.

### Menus & popups

| Animation | Duration |
|---|---|
| Scene-to-scene transition (every navigation) | 0.5 s `CCTransitionFade` |
| Solved: framed menu/replay buttons slide off (down) | 0.50 s |
| Solved: COMPLETE popup bg, text, menu/replay/next buttons slide in | 0.15 s |
| Free Play BEGIN popup dismiss | 0.15 s |
| Free Play HUD (difficulty, n/5, running clock) refresh tick | every 0.05 s |
| Classic level-select page up/down slide | 0.20 s |
| Google sign-in promo slide in/out | 0.40 s |
| Info popup slide in/out | 0.30 s |

### Audio behavior

- BG music starts at main menu (`MainMenu::init`), loops forever, is
  paused on app background and resumed on foreground
  (`AppDelegate.cpp`), stopped/restarted by the mute toggle
  (persisted, `SoundManager.cpp`).
- The only sound effect is `click.wav`, on piece pickup and successful
  drop. No sounds for spread, repel, reset, win, or menu buttons.

## 7. Tutorial copy (transcribed from the message bitmaps)

Handwritten-style sticky notes; positions on screen vary per image so
the note sits next to the relevant board feature. Verbatim text:

| Image (level shown on) | Text |
|---|---|
| `message_1.png` (Level 1) | "Drag bugs onto tiles. Infection spreads from the arrows. Infect the entire board!" |
| `message_3.png` (Level 3) | Note 1: "Many levels have more than one solution." Note 2: "Infection can jump gaps in tiles" |
| `message_5.png` (Level 5) | "Yellow tiles block the infection." |
| `message_10.png` (Level 10) | "Purple tiles will repel the infection if a bug tries to infect them!" |
| `message_26.png` (Level 26) | "If the board is infected before the repel fires, you still win!" — **explicit confirmation that the win-check-before-repel ordering (`RULES.md` §4.1) is intentional design** |
| `message_29.png` (Level 29) | "Black tiles will reset the board if a bug tries to infect them!" |

## 8. UNKNOWN

- **`POL-pencil-maze-long.wav` license terms/receipt.** Resolve against
  PlayOnLoop.com's catalog ("Pencil Maze") and the studio's purchase
  records. Assume attribution required until shown otherwise.
- **`Overhaul.ttf` exact provenance** (which foundry release). The
  embedded name table claims public domain; verify by matching the file
  hash against known distributions before shipping it.
