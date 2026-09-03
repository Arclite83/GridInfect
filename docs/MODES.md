# Grid Infect — Modes, Progression, Scoring, Services

Sources cited per claim; all paths relative to `grid-infect-cocos2dx/`.
The two game modes are `Classic` and `FreePlay`
(`Classes/Core/Enums.h` `enum GameMode`).

---

## 1. Classic mode

### 1.1 Structure and progression

- 128 hand-shipped levels (`Classes/Core/Level.h` `Count = 128`;
  boards in `Classes/Core/Level.cpp` `initByLevel`, cases 0–127).
  Displayed 1-based ("Level 1" … "Level 128",
  `Classes/Renderers/LevelMenuScene.cpp` init).
- Level select is a paged grid (`Classes/Renderers/ClassicMenuScene.cpp`
  `init`): 32 buttons per page (8 columns × 4 rows), 4 pages, up/down
  paging buttons animate the whole grid by one screen height over 0.2 s.
  The current page persists for the session in the `Game` singleton
  (`getClassicMenuPage`/`setClassicMenuPage`, `Classes/Core/Game.cpp`).
- Level 1 (id 0) is always playable (`isUnlocked || i == 0`); every other
  level requires its unlock flag. Locked levels show a padlock overlay at
  opacity 192 and a disabled button.
- Solving level id N unlocks id N+1 — and nothing else
  (`LevelMenuScene.cpp` `IOnLevelSolved::onLevelSolved`:
  `SaveData::Unlock(nextLevel)` guarded by `nextLevel < Level::Count`).
  Unlocks persist immediately (§4).
- Solving shows the COMPLETE popup with Menu / Replay / Next buttons
  (Next omitted on the last level). Replay on a solved level reloads it
  fresh; the in-game replay button on an *unsolved* level performs a full
  board reset instead (`LevelMenu::replayCallback`).
- Tutorial overlays: full-screen message images are drawn behind the
  board on specific levels (`LevelMenuScene.cpp` init switch on
  `getLevelId()`): id 0 → `message_1.png`, id 2 → `message_3.png`,
  id 4 → `message_5.png`, id 9 → `message_10.png`, id 25 →
  `message_26.png`, id 28 → `message_29.png`. These images carry the
  only tutorial/rule text in the game (they are baked bitmaps, see
  `ASSETS.md`).
- No scoring, stars, or timing in Classic. Solved/unsolved (via unlock
  flags) is the only tracked state.

### 1.2 Classic achievements (Google Play Game Services)

Awarded in `onLevelSolved` (`LevelMenuScene.cpp`), by solved level id
(else-if chain):

| Trigger (level id) | Achievement ID | Note in source |
|---|---|---|
| 4 | `CgkIyMfpr_gNEAIQAg` | comment: "Firewall" |
| 9 | `CgkIyMfpr_gNEAIQAw` | |
| 28 | `CgkIyMfpr_gNEAIQBA` | |
| 63 (`Level::Count/2 - 1`, "halfDone") | `CgkIyMfpr_gNEAIQBQ` | |
| 127 (`Level::Count - 1`, "allDone") | `CgkIyMfpr_gNEAIQAQ` | |

These fire on every solve of the trigger level (GPGS unlock is
idempotent server-side). Names/descriptions of achievements are not in
the code (UNKNOWN, §6).

## 2. Free Play mode (the timed mode)

### 2.1 Run structure

- The Free Play menu (`Classes/Renderers/FreePlayMenuScene.cpp`) offers
  five difficulties: Beginner, Easy, Medium, Hard, Challenging
  (`Enums.h` `enum Difficulty`).
- Choosing a difficulty immediately generates **5 levels**
  (`LevelBuilder::generateLevel(difficulty)` ×5,
  `FreePlayMenuScene.cpp` `freePlayNCallback`) and enters the level
  scene. See `GENERATOR.md`.
- The scene shows a BEGIN popup; the board is hidden until BEGIN is
  pressed (`LevelMenuScene.cpp` init `FreePlay` branch, `beginLevel`).

### 2.2 Exact timing rules

- The timer **starts when BEGIN is pressed**: `_timeStarted =
  millisecondNow()` (`LevelMenu::beginLevel`), where `millisecondNow` is
  wall-clock `CCTime::gettimeofdayCocos2d` converted to milliseconds
  (`LevelMenuScene.cpp` `millisecondNow`). Level 1's board binds in the
  same frame.
- Solving levels 1–4 advances immediately to the next generated level
  within the same scene (`onLevelSolved` FreePlay branch →
  `Game::nextFreePlayLevel` + `bindLevel`); the timer keeps running.
  There is no pause between levels.
- Solving the 5th level stops the clock: `_timeStopped =
  millisecondNow()`, `duration = _timeStopped - _timeStarted`
  (milliseconds).
- Timer granularity is milliseconds of wall-clock time; the display
  refreshes every **0.05 s** via a repeating `CCDelayTime(0.05)` action
  (`LevelMenuScene.cpp` init FreePlay branch,
  `updateFreePlayDisplay`).
- HUD text while running: `"<Difficulty>   <n>/5   <time>"` where n is
  the 1-based level index. Time format (both HUD and final popup):
  minutes printed only when > 0 (and taken mod 60), seconds mod 60
  zero-padded to 2, milliseconds zero-padded to 3, colon-separated —
  e.g. `07:123`, `1:07:123`. The final popup reads
  `"COMPLETED IN: \n<time> "` (trailing space included).
- **Cheat guard**: if the computed running duration is ever negative
  (system clock moved backward), the scene immediately bails to the Free
  Play menu (`updateFreePlayDisplay`, comment "CHEAT-PROOF").
- Undo, resets, and reset traps carry no time penalty beyond the time
  they cost the player. Backgrounding the app does not pause the clock
  (wall-clock based).

### 2.3 Records, counts, and leaderboards

On completing the 5th level (`onLevelSolved` FreePlay branch):

- **Local best time** per difficulty: overwritten iff no previous best
  (`0`) or the new duration is lower (`SaveData::GetBestTime` /
  `SetBestTime`).
- **Completion count** per difficulty is incremented
  (`SaveData::IncrementFreePlayCount`) — this drives unlocks (§2.4).
- If signed in to GPGS, the duration (ms) is submitted to the
  difficulty's leaderboard (`GPGSManager::SubmitHighScore`):

  | Difficulty | Leaderboard ID |
  |---|---|
  | Beginner | `CgkIyMfpr_gNEAIQBw` |
  | Easy | `CgkIyMfpr_gNEAIQCA` |
  | Medium | `CgkIyMfpr_gNEAIQCQ` |
  | Hard | `CgkIyMfpr_gNEAIQCg` |
  | Challenging | `CgkIyMfpr_gNEAIQCw` |

- The Free Play menu shows a per-difficulty best time
  (`--:--:---` when none) and a leaderboard button per difficulty
  (`leaderboardNCallback` → `GPGSManager::ShowLeaderboard`).

### 2.4 Difficulty unlock ladder

Computed at Free Play menu build time (`FreePlayMenuScene.cpp` init).
Beginner is always available. Each next difficulty needs the previous
one **completed 3 times**:

- Easy: `GetFreePlayCount(Beginner) >= 3`
- Medium: Easy count ≥ 3 (shows "MEDIUM LOCKED" while Beginner < 3,
  else "PLAY EASY n MORE TIME(S)")
- Hard: Medium count ≥ 3 (gated behind Easy ≥ 3 for its label)
- Challenging: Hard count ≥ 3 (gated behind Medium ≥ 3 for its label)

Locked buttons are disabled; their labels count down
`"PLAY <PREV> n MORE TIME"`/`"TIMES"`.

**Quirk:** reaching each threshold also unlocks a GPGS achievement, but
the unlock call is made **every time the Free Play menu is built** with
the threshold met (not once at the moment of unlocking):
Easy `CgkIyMfpr_gNEAIQDQ`, Medium `CgkIyMfpr_gNEAIQDg`,
Hard `CgkIyMfpr_gNEAIQDw`, Challenging `CgkIyMfpr_gNEAIQEA`.

## 3. Scoring

There is no point scoring anywhere. Classic tracks only unlocks; Free
Play's score is elapsed time (lower is better), locally and on
leaderboards.

## 4. Save data

`Classes/Core/SaveData.cpp` — a plain-text file `GridInfectSave.txt` in
`CCFileUtils::getWritablePath()`, rewritten in full on every change
(`Save()`), loaded once at first access (constructor). Line formats:

```
MUTE:<0|1>
SKIPGPLAYPROMO:<0|1>
UNLOCKED:<levelId>          (one line per unlocked level)
LEADERBOARD<d>:<bestTimeMs> (d = 0..4 per Difficulty enum order)
FREEPLAYCOUNT<d>:<count>
```

Parsing quirks to preserve if reading old saves: matching is
`line.find(...)` substring search anywhere in the line; the
`LEADERBOARD`/`FREEPLAYCOUNT` index is exactly **one character**, values
parsed with `atoi`. Unknown lines are ignored. Nothing is checksummed or
encrypted.

## 5. Google Play Game Services dependencies (all dead)

Everything below depended on GPGS and/or Google+ sign-in and does not
function today; see `PORT_NOTES.md` §3 for the code-level status
(the native GPGS integration in this snapshot is incomplete/broken).

- **Sign-in**: G+ button on the main menu
  (`Classes/Renderers/MainMenuScene.cpp` `gPlayButtonCallback` — note
  its signed-in/out logic is inverted relative to its icon updates:
  it calls `BeginUserInitiatedSignIn` when already signed in and
  `SignOut` when not, per the `IsSignedIn()` branch), plus a first-run
  promo popup ("sign in?") with Google-branded button art, suppressed
  permanently after one answer via `SKIPGPLAYPROMO`
  (`MainMenu::init`, `gPlayPromoYes/NoButtonCallback`).
- **Achievements**: 5 Classic (§1.2) + 4 Free Play unlock achievements
  (§2.4); achievements UI via `GPGSManager::ShowAchievements`
  (main menu button).
- **Leaderboards**: 5 timed leaderboards (§2.3) with per-difficulty
  show-UI buttons.
- **Turn-based multiplayer**: `GPGSManager` contains a large
  turn-based-match API surface (`QuickMatch`, `TakeTurn`, …, marked
  "by Jacky 2014.05.22") that is dead code — never called by any scene,
  and not compilable in this snapshot (`PORT_NOTES.md` §3).
- IDs: Android `APP_ID 478989149128`
  (`proj.android/res/values/*.xml`, referenced from
  `proj.android/AndroidManifest.xml`).
- **Crashlytics** (not GPGS, also dead): initialized on iOS with an
  API key in `proj.ios/AppController.mm` and on Android in
  `proj.android/src/.../GridInfect.java` (`Crashlytics.start`), with a
  committed API secret in `proj.android/crashlytics.properties`.

## 6. UNKNOWN

- **Achievement/leaderboard display names, icons, descriptions** — only
  IDs exist in code. Resolve via the Google Play Console for
  `com.bloodhoundstudios.gridinfect` (app id 478989149128), if it still
  exists.
- **Whether iOS shipped with working GPGS** — this snapshot never
  initializes the native SDK (`PORT_NOTES.md` §3). Resolve by inspecting
  a shipped IPA.
- **The unused `btn_classic.png` / `btn_free_play.png` / `btn_start.png`
  art** suggests an earlier main-menu layout; no code references remain
  (see `ASSETS.md` §5). Only the author could say what they were for.


---

## 5. Daily and Endless (rebuild, stage 4)

Timed Free Play is retired from the menu (`NEXT_PASS.md` decision 5); its
actions and tests stay so old logs replay. Two modes replace it.

### 5.1 Daily

- `daily.begin { dateUtc, nowMs }`: `dateUtc` is `yyyy-MM-dd` in UTC, so
  every device gets the same board. Seed = FNV-1a 64 of `"daily:" + date`;
  the board is the first seed at or after it that `GeneratorV2` accepts
  under the weekday's spec (`DailySpec.For`): Monday 3 pieces G1–G2 up to
  Sunday 5 pieces G4–G5, cardinal arms and walls only at launch (later
  stages rotate the element set here).
- The clock is a stat, not a rule: elapsed is shown in the HUD; par =
  `10 s + 15 s × trace length × (3 + grade) / 4`; the personal best per
  date is kept in the profile.
- `daily.complete { nowMs }`: rejects a backward clock and an unsolved
  board. Streak = consecutive completed dates (completing today's board
  again improves the best, never the streak); every 7th day sets
  `StreakGrantDue`, which stage 5 turns into `locks.grant { 1, "streak" }`.
- Friends leaderboard: out of stage 4. `IDailyScoreSink` in
  `GridInfect.Game` is the hook; the shipped sink is local.

### 5.2 Endless

- `endless.begin { grade, seed }`: a grade G1–G5 and a seed (the adapter
  picks the wall clock; it enters the log). Level n of the run is the
  first accepted seed from `seed + n × 100000` under `DailySpec.Endless`.
- `endless.advance`: the current board is solved; streak +1 if the board
  saw no full reset (`LevelSession.Resets == 0`), else back to 1; best
  streak per grade in the profile; next board loads at once.
- `endless.abort`: leave the run. No clock anywhere in the mode.
