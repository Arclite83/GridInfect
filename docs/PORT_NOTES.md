# Grid Infect — Unity/C# Port Notes

What in the original does **not** translate directly, what is dead, and
what is coupled to cocos2d-x 2.2.3 in ways a port must consciously
replace. Paths relative to `grid-infect-cocos2dx/`.

---

## 1. Engine dependency and repo state

- The cocos2d-x engine is **not vendored** in this repo. The project is
  a cocos2d-x 2.2.3 `projects/<name>` checkout: `proj.ios/…/project.pbxproj`
  references `…/cocos2d-x-2.2.3/projects/GridInfect/`, and
  `proj.android/build_native.sh` computes `COCOS2DX_ROOT="$DIR/../../.."`.
  Nothing engine-side can be consulted here; every engine behavior cited
  in these docs is from the 2.2.3 public API semantics.
- **The snapshot does not build as committed**, on any platform:
  - Android: `proj.android/jni/Android.mk` line 10 lists
    `../../Classes/GPG/GameCenter.cpp`, which **does not exist in the
    repo** (the JNI bridge that connected the C++ `GPGSManager` calls to
    the Java `GameCenter.java`). Its absence also means every
    `GPGSManager::…` symbol the scenes call had its real Android
    implementation in that missing file.
  - iOS: `Classes/GPG/GPGSManager.cpp` is in the Xcode target and
    references an undefined global `g_gameConfig` (3 uses) and
    **cocos2d-x 3.x APIs** (`cocos2d::Director::getInstance()`,
    `EventDispatcher::dispatchCustomEvent`) that do not exist in 2.2.3 —
    the multiplayer half (marked "by Jacky 2014.05.22") was pasted from
    a 3.x sample and cannot compile against this project.
  - The 10 other `proj.*` directories are cocos2d-x template shells
    (win32/winrt/wp8/wp8-xaml/mac/linux/tizen/blackberry/marmalade);
    none reference the GPG code and none were plausibly shipped.
- Practical consequence: **the C++ core (`Classes/Core/*`,
  `Classes/LevelBuilder.*`, `Classes/Renderers/*` minus GPGS calls) is
  the porting surface**; treat all of `Classes/GPG/` as a dead
  integration.

## 2. Behavior coupled to the cocos2d-x action system (load-bearing)

- The 0.3 s deferred resolution (`RULES.md` §4.1) is implemented as a
  `CCSequence(CCDelayTime(0.3), CCCallFuncN)` **running on a 1×1 sprite
  (tag 999)**, and is cancelled by `stopAllActions()` on that sprite in
  `ccTouchBegan` (`Renderers/LevelMenuScene.cpp`). Cancellation
  suppresses the win check, repels, and the board reset for the pending
  placement. **Correction (2026-08-30):** this note previously said
  levels 41/87 (ids 40/86) are winnable only through the cancellation —
  that was wrong; both are winnable cleanly by placing their single
  necessary trap-tripping piece last, where the win-check-before-reset
  ordering makes the trip free (see `RULES.md` §4.1 correction for
  verified solutions). The author has confirmed the cancellation is a
  bug: the Unity port does **not** reproduce it — resolution always
  runs. The only behavior change is that reset traps always fire on
  non-winning placements, which is the intended rule.
- The synchronous re-resolution during undo (`Game::clearPiece` calling
  `delayThenCheckForWin()` directly, per remaining piece) bypasses the
  0.3 s delay entirely. Port both paths, not one unified one, if
  fidelity matters (`RULES.md` §7).
- Win detection (`onLevelSolved`) can fire during an undo; the original
  UI tolerates this because the popup animation is idempotent.

## 3. Dead services and their call sites

| Service | Call sites | Status |
|---|---|---|
| Google Play Game Services (achievements, leaderboards, sign-in) | `Renderers/LevelMenuScene.cpp` (`onLevelSolved`), `Renderers/FreePlayMenuScene.cpp` (menu build + leaderboard buttons), `Renderers/MainMenuScene.cpp` (sign-in button, promo popup, achievements button) via `Classes/GPG/GPGSManager.*` | Dead. The native C++ gpg SDK is **never initialized in this snapshot** — no call to `GPGSManager::InitServices` exists, so `gameServices` is a null `unique_ptr` and, as committed, every `UnlockAchievement`/`SubmitHighScore`/`Show*` call dereferences null (the shipped Android build presumably initialized it in the missing `GameCenter.cpp`). All achievement/leaderboard IDs are catalogued in `MODES.md` |
| Google+ sign-in UI assets | `Resources/Red-signin_Long_*`, `btn_g+*`, promo popup | Google+ is shut down; branding must not be reused |
| Crashlytics (pre-Firebase) | `proj.ios/AppController.mm` (`startWithAPIKey:`), `proj.android/src/.../GridInfect.java` (`Crashlytics.start`) | Dead service. Note: the iOS API key is hardcoded in `AppController.mm` and an API secret is committed in `proj.android/crashlytics.properties` — treat both as leaked/revoked; do not carry forward |
| Turn-based multiplayer (`QuickMatch`, `TakeTurn`, `ParseMatchData`, …) | `Classes/GPG/GPGSManager.{h,cpp}` only — no scene calls them | Dead, uncompilable (§1). Do not port |

The sign-in button logic itself has a bug worth knowing before "fixing"
it silently: `MainMenu::gPlayButtonCallback` calls
`BeginUserInitiatedSignIn()` when `IsSignedIn()` is **true** and
`SignOut()` when false (`MainMenuScene.cpp`) — inverted relative to its
own icon handling.

## 4. cocos2d-x specifics needing Unity equivalents

| Original | Where | Unity note |
|---|---|---|
| Scene graph of tagged children (`getChildByTag`) — cells are tags 0–65, pieces 300–307, popup widgets 400–1000 | `LevelMenuScene.cpp` | Replace with direct references; the tag scheme is an implementation detail, but cell-tag = `i*11+j` mirrors the original's board encoding (the port transposes the board — ARCHITECTURE §2) |
| `CCTextureCache` texture swaps for cell state | `bindLevel`, `onChangeBoardIndex` | Sprite swap on a per-cell renderer; no animation (see `ASSETS.md` §6) |
| `CCMoveTo` (always linear) for every animation | all scenes | Unity tweens must use linear interpolation to match feel; durations in `ASSETS.md` §6 |
| `CCTransitionFade(0.5f)` between scenes | all scenes | 0.5 s fade on every navigation |
| Targeted touch delegate, `swallowsTouches=true`, priority 0; single-finger drag; hit tests via `boundingBox().containsPoint` on scaled sprites | `LevelMenuScene.cpp` | Hit areas are the full scaled sprite rects (11% of screen height squares), not the art's visible shape |
| `CCTime::gettimeofdayCocos2d` wall-clock ms for the Free Play timer and RNG seed | `LevelMenuScene.cpp`, `LevelBuilder.cpp` | Wall clock, not game time: backgrounding does not pause the timer; clock-backward is detected and aborts the run (`MODES.md` §2.2) |
| `setAnimationInterval(1.0 / 90)` | `AppDelegate.cpp` | 90 fps target |
| `SimpleAudioEngine` looping BGM + effect; pause/resume on app background | `SoundManager.cpp`, `AppDelegate.cpp` | Straightforward `AudioSource` port |
| `CCFileUtils::getWritablePath()` + hand-rolled text save | `SaveData.cpp` | Format in `MODES.md` §4; port to `Application.persistentDataPath` or PlayerPrefs (decide whether to keep import compatibility) |
| No design resolution set; all layout = percentages of `getVisibleSize()` | `AppDelegate.cpp`, all scenes | The 480×320 machinery in `AppMacros.h` is scaffolding, unused. Landscape-only (`proj.ios/Info.plist`, `AndroidManifest.xml` `screenOrientation="landscape"`) |

## 5. C++ constructs that will not port literally

- **Singletons with file-scope global state.** `Game`, `SaveData`,
  `SoundManager`, `EventHandler` are singletons whose actual state lives
  in translation-unit globals (`Game.cpp`: `_gameMode`, `_levels`,
  `_repelsToRun`, `_resetTripped`, the four `_xStopped` flags;
  `LevelMenuScene.cpp`: `_touchedPiece`, `_timeStarted`). None of it is
  instance state. A C# port naturally moves these into class fields —
  fine, but preserve the *lifetimes*: e.g. `_repelsToRun` persists
  across undos until the next placement (`RULES.md` §5, §7), and the
  classic-menu page persists for the app session.
- **`Level::_solved` is a single file-scope global shared by all Level
  instances** (`Level.cpp` line 12), despite the accessor pair
  suggesting per-level state. Observable behavior documented in
  `RULES.md` §8; a per-instance flag is equivalent given how the
  original constructs levels, but know the original's shape.
- **The event system is single-listener.** `EventHandler::setOn…Listener`
  clears the listener list each registration ("FOR NOW, ONLY 1 PER",
  `EventHandler.cpp`); the interfaces' methods are non-pure virtuals
  whose only implementations live in `LevelMenuScene.cpp` and C-cast
  `this` to `LevelMenu*`. This is an interface facade over a hard
  renderer coupling — in Unity, plain C# events on the game core
  reproduce it.
- **Memory is leaked freely** (levels, pieces, repels are `new`ed and
  never deleted; `setLevel` drops old levels without freeing). No
  gameplay consequence; C# GC makes it moot.
- **`rand()`/`srand()` global RNG** shared process-wide, seeded once
  from the wall clock (`GENERATOR.md` §1). `System.Random` or a custom
  PRNG changes every generated board by construction; there is no
  compatibility to preserve because the original was never reproducible.
- Board cells are `int`s in a flat `int[66]`; piece/tile enums are
  plain C enums whose **ordinal values matter** (`Difficulty` indexes
  the save arrays; `Tile` is the `rand() % 15` domain and the
  `tileString` table order in `LevelBuilder.cpp`). Keep the orders.

## 6. Suggested porting surface

The clean core→renderer seam the original almost has:

- Port `Level` (data), `Piece`, `Repel`, `Game` (rules, spread, repels,
  reset, undo, win) and `LevelBuilder` (generation) as pure C# with the
  board-change/level-solved/unbind events exposed as C# events —
  `docs/test_vectors.json` verifies this layer completely (every level,
  placement-by-placement board states).
- Rebuild the scenes (`MainMenu`, `ClassicMenu`, `FreePlayMenu`,
  `LevelMenu`) as Unity UI against `MODES.md` + `ASSETS.md` §6; nothing
  in them beyond the 0.3 s timer (§2) carries game logic.
- Drop `Classes/GPG/` entirely; re-express achievements/leaderboards
  against whatever service replaces GPGS, using the trigger conditions
  in `MODES.md`.

## 7. UNKNOWN

- **The missing `Classes/GPG/GameCenter.cpp`** — the only file that
  could show how Android actually initialized GPGS and mapped
  `GPGSManager` statics to the Java layer. Resolve from the author's
  archives or by decompiling the shipped APK
  (`com.bloodhoundstudios.gridinfect`).
- **Which platforms actually shipped.** iOS and Android projects are
  fleshed out (icons, IDs, Crashlytics config); the rest are template
  shells. Store listings would confirm.
- **Engine-default behaviors** relied on implicitly (touch dispatch
  ordering with `CCMenu` at priority −128 vs. the layer's delegate at 0,
  exact `CCTransitionFade` curve). Resolve against a
  `cocos2d-x-2.2.3` checkout if pixel/feel-perfect parity is wanted.
