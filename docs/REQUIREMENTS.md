# Grid Infect Unity Rebuild — Requirements

Date: 2026-08-30. Companion: [`DEPENDENCIES.md`](DEPENDENCIES.md).

## Ingest artifact status

All six ingest artifacts exist and were used; nothing below is derived from
the C++ directly.

| Artifact | Status |
|---|---|
| `RULES.md` | present, complete (board, spread, repel, reset, undo, win) |
| `GENERATOR.md` | present, complete (sampling, carving, RNG, difficulty configs) |
| `test_vectors.json` | present — 128/128 levels; `verify_test_vectors.py` re-run 2026-08-30, all pass |
| `MODES.md` | present, complete (Classic, Free Play, saves, dead GPGS) |
| `ASSETS.md` | present, complete (inventory, licenses, timing spec, tutorial copy) |
| `PORT_NOTES.md` | present, complete (dead code, engine couplings, porting surface) |

**Missing from the ingest specs** (they document the 2014 game; these topics
did not exist in it): ads, IAP, consent/privacy, analytics, accessibility,
store compliance, and the new art direction. Every requirement in those areas
is marked **NEW — needs approval**. See the UNKNOWN section at the end.

## Reading guide

- **MVP** = required for the first Play Store internal-testing build that
  serves a real ad. That build is Android-only, Classic mode only, with the
  consent-gated interstitial path working end to end.
- **LATER** = everything else, including all iOS-only work and Free Play.
- Trace cites the spec section the requirement was extracted from. **NEW**
  items are decisions the specs cannot source; they are collected again in
  the approval roll-up near the end.

---

## 1. Core rules engine

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-101 | MVP | Fixed 11×6 board (11 rows, 6 columns — transposed from the original's 6×11 for portrait, R-1103), row-major `int[66]` (`loc = i*6 + j`), cell values 0 void / 1 active / 2 wall / 3 repel switch / 4 infected / 5 reset trap, plus transient 99 used only inside undo. Values 2/3/5 immutable during play. | RULES §1, §1.1 |
| R-102 | MVP | The 128 classic boards and piece lists load from data derived from `docs/test_vectors.json` (single source of truth; an editor importer bakes it into a runtime asset — no hand-transcribed C arrays). | RULES §1; test_vectors.json |
| R-103 | MVP | Pieces: the 15 L/R/U/D arm-combination tile types, ordered per-level list (2–6 shipped; duplicates legal), no rotation, tray capacity 8. `Tile` and `Difficulty` enum ordinal orders preserved. | RULES §2; PORT_NOTES §5 |
| R-104 | MVP | Placement per RULES §3: drop legal iff cell value ∈ {1,4} and no other placed piece occupies it; any illegal drop returns the piece to its tray slot (clearing it if it was placed); touching a piece unplaces it instantly; `setPiece` is clear-then-place. | RULES §3 |
| R-105 | MVP | Infection spread bit-exact per RULES §4: synchronous, offset 1–10 outer loop, L,R,U,D inner order; walls/switches/traps stop a direction; void, edges, and infected cells are passed over without stopping; 99 cells skipped without stopping; event/repel-queue ordering preserved (offset-major, then L,R,U,D). | RULES §4 |
| R-106 | MVP | Deferred resolution 0.3 s after a successful drop, in this order: win check first (a winning placement ignores tripped traps and queued repels), else full reset if tripped, else run repels in queue order. | RULES §4.1 |
| R-107 | MVP | Resolution is **never cancellable**. The original's touch-cancellation (`LevelMenuScene.cpp:693`, `stopAllActions` on the 0.3 s timer node) is a confirmed bug and is not ported — resolution always runs, win check first. Verified consequence (2026-08-30): **no level depends on the bug** — exhaustive search + replayed clean solutions prove ids 40/86 winnable by ordering their single trap-tripping placement last (RULES §4.1 correction). A touch landing inside the 0.3 s beat fast-forwards the pending resolution (it resolves immediately, then the touch is processed) so input never queues or drops. | RULES §4.1 (corrected); fast-forward choice **NEW — needs approval** |
| R-108 | MVP | Repel semantics per RULES §5: origin = switch cell, direction = opposite of incoming spread; walk 1–10 un-infecting 4→1; stopped only by a placed piece; queue cleared only at next placement (running it does not empty it). | RULES §5 |
| R-109 | MVP | Reset trap semantics per RULES §6: on non-winning resolution, all 4→1 and all pieces return to tray; static cells untouched. | RULES §6 |
| R-110 | MVP | Undo semantics per RULES §7: instant clear, row/column retraction with 99 marking, re-propagation of remaining pieces in piece-index order with *synchronous* resolution per piece, repel-queue accumulation across undos, mid-undo win possible. | RULES §7 |
| R-111 | MVP | Win when no cell has value 1; not all pieces need placing; solved event drives progression. | RULES §8 |
| R-112 | MVP | No loss condition, timer failure, or move limit; unlimited free undo; in-level reset button = full reset while unsolved, fresh reload once solved. | RULES §9; MODES §1.1 |
| R-113 | MVP | The core exposes board-change / level-solved / pieces-unbound as plain C# events; the Unity layer is a pure listener (no game logic in renderers except the cancellable 0.3 s timer, which the Unity layer owns). | RULES §10; PORT_NOTES §2, §6 |
| R-114 | MVP | Mechanical equivalence proven by replaying all 128 vectors — every per-step golden board — in edit-mode tests. The stored solutions for ids 40/86 still record the original's exploit; run `docs/tools/regen_clean_solutions_40_86.py` once to regenerate those two entries exploit-free (verified clean solutions in RULES §4.1 correction), after which no vector anywhere models the cancellation. | test_vectors.json; user constraint |
| R-115 | MVP | Single active touch: one drag at a time, additional simultaneous touches ignored. The original's multi-touch behavior is UNKNOWN in the spec; this is the port's decision. | **NEW — needs approval** (resolves RULES UNKNOWN) |

## 2. Classic progression

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-201 | MVP | 128 levels; level 1 always playable; solving level N unlocks N+1 only; unlocks persist immediately. | MODES §1.1 |
| R-202 | MVP | Level-select screen shows all 128 levels with locked/unlocked state (the original's 4×32 paging is presentation; the overhaul may re-lay it out). | MODES §1.1 |
| R-203 | MVP | Solved popup with Menu / Replay / Next actions; Next absent on level 128; Replay on a solved level reloads fresh. | MODES §1.1 |
| R-204 | LATER | Tutorial guidance on level ids 0, 2, 4, 9, 25, 28, re-authored as real text (copy verbatim from ASSETS §7 — "bugs" fiction included); the baked message bitmaps are not reused. | MODES §1.1; ASSETS §7 |
| R-205 | LATER | The five Classic achievement triggers (ids 4, 9, 28, 63, 127) re-mapped to a live service — Google Play Games Services v2 on Android, Game Center on iOS. Old GPGS IDs are dead. | MODES §1.2; PORT_NOTES §3; service choice **NEW — needs approval** |

## 3. Timed mode (Free Play)

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-301 | LATER | Five difficulties (Beginner…Challenging); choosing one generates 5 levels up front; BEGIN popup gates the board. | MODES §2.1 |
| R-302 | LATER | Timer: starts at BEGIN, wall-clock milliseconds, runs continuously across the 5 levels, stops on the 5th solve; backgrounding does not pause; negative computed duration aborts the run to the menu. | MODES §2.2 |
| R-303 | LATER | Running HUD (difficulty, n/5, live clock at ≥20 Hz refresh) and the `m:ss:mmm` time format as default copy; overhaul may restyle. | MODES §2.2 |
| R-304 | LATER | Per-difficulty local best time (overwrite iff lower or none) and completion count, persisted. | MODES §2.3 |
| R-305 | CUT | Unlock ladder dropped: every difficulty is always playable. (Original gated each behind 3 completions of the previous; locked buttons showed the remaining count.) | MODES §2.4; **changed from original** |
| R-306 | LATER | Five timed leaderboards re-mapped to a live service (same choice as R-205). | MODES §2.3; **NEW — needs approval** |

## 4. Generator integration

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-401 | LATER | Port the generator semantically exactly: difficulty configs, rejection sampling with full draw order (rejected samples still consume draws), window-shrink table **including the UD missing-bottom-margin bug**, uniqueness + row/column exclusivity, probabilistic carving `(rand%20)-offset > 4` with continue-on-fail. | GENERATOR §2–§4 |
| R-402 | LATER | Replace `rand()` with a fixed-constant PCG32 owned by the Core assembly, seed passed explicitly, draw order preserved — generation becomes bit-reproducible against itself (the original never was cross-platform; there is no compatibility to preserve). | GENERATOR §1 (explicit port guidance); PORT_NOTES §5 |
| R-403 | LATER | Generator golden tests: fixed seeds → generated boards captured once from the C# implementation and locked in edit-mode tests. | GENERATOR §1; test artifact is **NEW** |
| R-404 | LATER | Guard `piecesToSet ≤ 6` (row-exclusivity makes >6 spin forever) and assert sampling windows stay positive if configs ever change. | GENERATOR §3 note, §7 |

## 5. Persistence

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-501 | MVP | Persist: unlocked level set (MVP) plus, when their features land, per-difficulty best times and completion counts, and audio preference. Write-through on every change. | MODES §4 |
| R-502 | MVP | Format: a versioned JSON file in `Application.persistentDataPath`. **No import of the old `GridInfectSave.txt`** — the original was device-local and 12 years old; its parsing quirks die here. | **NEW — needs approval** (replaces MODES §4 format) |
| R-503 | LATER | Remove-ads entitlement cached in the save and revalidated against store receipts on launch. | **NEW — needs approval** |

## 6. Ads (revenue path — no spec exists; all NEW)

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-601 | MVP | AdMob via the Google Mobile Ads Unity plugin; the SDK initializes only after the consent gate (R-801) reports ads may be requested. | **NEW — needs approval** |
| R-602 | MVP | One format at MVP: interstitial on dismissal of the solved popup — first ad no earlier than the 3rd solve of a session, minimum 90 s between ads; cadence values live in a designer-editable config asset, not code. | **NEW — needs approval** |
| R-603 | LATER | Rewarded ad as an opt-in "skip this level" on the pause/reset surface (the game's only natural reward sink; it adds a mechanic the original lacked). | **NEW — needs approval** |
| R-604 | MVP | Development builds use Google's demo ad unit IDs plus registered test-device IDs; production unit IDs exist only in the release config asset (see DEPENDENCIES §5). | **NEW — needs approval** |
| R-605 | MVP | Play compliance for ads: `AD_ID` permission declared (the plugin adds it), Data safety form covers ads/device identifiers, app is declared not child-directed. | **NEW — needs approval** |
| R-606 | LATER | Minimal analytics event set (level start/solve/fail-reset, ad shown/clicked, session) to tune the ad cadence in R-602. | **NEW — needs approval** |

## 7. Remove-ads IAP (all NEW)

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-701 | LATER | Single non-consumable `remove_ads` product via Unity IAP on both stores; owning it suppresses interstitials permanently (rewarded skip stays available — it is user-initiated). Price point is the user's call. | **NEW — needs approval** |
| R-702 | LATER | Restore-purchases flow (App Store review requirement; free on Play via receipt query). | **NEW — needs approval** |

## 8. Consent & privacy

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-801 | MVP | Google UMP consent flow on first launch: update consent info, show the form when required (EEA/UK), and only then allow ad requests. Blocking ads ≠ blocking gameplay: the game is playable regardless of consent outcome. | **NEW — needs approval** |
| R-802 | MVP | A "Privacy options" entry point (settings surface) re-opens the UMP privacy form whenever UMP reports it is required. | **NEW — needs approval** |
| R-803 | LATER | iOS: ATT prompt with usage description and SKAdNetwork identifiers in Info.plist (ships with the iOS build, not the Android MVP). | **NEW — needs approval** |
| R-804 | MVP | A hosted privacy policy URL, linked in the Play listing and in-app (required by Play and AdMob). | **NEW — needs approval** |
| R-805 | MVP | Nothing from the dead 2014 integrations ships: no GPGS/Google+ code or branding art, and the committed Crashlytics key/secret are treated as leaked — never reused. | PORT_NOTES §3; ASSETS §4 |

## 9. Settings

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-901 | MVP | A settings surface exists with at minimum the privacy-options entry (R-802); remove-ads purchase/restore and audio controls join it as those features land. | **NEW — needs approval** |
| R-902 | LATER | Audio: looping background music and an SFX set (pickup/drop at minimum), mute persisted, pause/resume on app background. The original POL track and Overhaul.ttf are **not** carried over (licenses unverified, ASSETS §8) — replacements come from the presentation overhaul. | ASSETS §2, §6; MODES §4; replacement choice **NEW** |

## 10. Accessibility (no spec exists; all NEW)

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-1001 | MVP | Cell states distinguishable by shape/glyph/pattern, never color alone — the original's pure blue/red/yellow/purple/black coding fails colorblind players. Baked into the new tile art from the start (retrofit is dearer). | **NEW — needs approval** |
| R-1002 | LATER | Touch targets: board cells and tray pieces ≥ 48 dp effective hit area (original cells at 11 % of screen height already clear this on phones; verify on small devices). | **NEW**; layout baseline ASSETS §6 |
| R-1003 | LATER | All text is real, scalable TextMeshPro text — no text baked into bitmaps (pairs with R-204). | **NEW** |
| R-1004 | LATER | If the overhaul adds motion/flash effects, a reduce-motion toggle in settings. | **NEW** |

## 11. Presentation guardrails (overhaul is free within these)

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-1101 | MVP | Model state changes are legible the same frame they occur; any added cosmetic animation must not obscure board state. The 0.3 s resolution beat is presentation only — input during it fast-forwards resolution (R-107), it is never an input-cancellation window. | ASSETS §6; RULES §4.1 (corrected) |
| R-1102 | LATER | The ASSETS §6 timing table (0.10 s drop snap, 0.15 s tray return, 0.5 s scene fades, etc., all linear) is the starting values for the new presentation — free to change, but changes are deliberate, not accidental. | ASSETS §6 |
| R-1103 | MVP | **Portrait-only** (revised — the original shipped landscape-locked). The board is transposed to 11 rows × 6 columns so it stands up on a phone, and is sized by whichever of three limits binds — height cap, width budget, or the band left between tray and HUD. The tray centres on the pieces a level actually has (six at most) rather than a fixed eight-slot rack. | ASSETS §3, §6; PORT_NOTES §4 |
| R-1104 | MVP | Target frame rate 60 (the original targeted 90; nothing in a turn-based puzzle needs it, and battery + thermals on an ad-supported title argue down). | **NEW — needs approval** |

## 12. Platform & compliance

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-1201 | MVP | Android internal-testing release on Play as an AAB targeting API 36 (mandatory for new apps from 2026-08-31). Full settings in DEPENDENCIES §7. | **NEW** (Play policy) |
| R-1202 | LATER | iOS build + TestFlight distribution. | **NEW** |
| R-1203 | MVP | A fresh application identifier owned by the user (`com.bloodhoundstudios.gridinfect` and app id 478989149128 are not assumed available — see UNKNOWN). | **NEW — needs input** |

## 13. Architecture & test (hard constraints, restated as requirements)

| ID | Pri | Requirement | Trace |
|---|---|---|---|
| R-1301 | MVP | Rules engine, generator, and save model live in `GridInfect.Core`, a plain C# assembly definition with `noEngineReferences: true` — zero UnityEngine types anywhere in it. The Unity layer consumes it only through its public API and C# events. | user constraint; PORT_NOTES §6 |
| R-1302 | MVP | `docs/test_vectors.json` replays in edit-mode tests in the editor on macOS — no device, no player build, runnable headless (`-runTests -testPlatform EditMode`). | user constraint |
| R-1303 | MVP | Third-party SDK code (ads, consent, IAP, analytics) is referenced only from `GridInfect.Services`; `GridInfect.Core` and `GridInfect.Game` never reference an SDK assembly. | **NEW** (derived from the asmdef constraint) |

---

## NEW items awaiting approval (roll-up)

Decisions the ingest specs cannot source. Each is my recommendation; veto
individually.

1. **R-115** — single-touch input (original multi-touch behavior unknowable from the repo).
1a. **R-107** — the cancellation bug is *not* ported (you confirmed bug); the sub-decision needing approval is fast-forward-on-touch during the 0.3 s beat (vs. briefly ignoring input).
2. **R-502** — fresh JSON save format, no legacy `GridInfectSave.txt` import.
3. **R-601/602/604/605** — AdMob; interstitial-on-solve as the only MVP format (3rd-solve grace, 90 s cap); demo-ID discipline; Play ad declarations.
4. **R-603** — rewarded "skip level" (adds a mechanic; easiest honest reward sink).
5. **R-606** — minimal analytics via Unity Analytics (LATER).
6. **R-701/702** — non-consumable remove-ads via Unity IAP; price TBD by you.
7. **R-801/802/804** — UMP consent flow, privacy-options entry, hosted privacy policy.
8. **R-205/306** — Play Games Services v2 / Game Center as the achievements/leaderboards replacement (LATER).
9. **R-1001** — colorblind-safe tile encoding as an art-direction constraint.
10. **R-1104** — 60 fps target instead of the original 90.
11. **R-901/902** — replacement music/font/SFX (originals have unverified licenses).
12. **R-1203** — new bundle ID (needs your naming decision).

## UNKNOWN — what the ingest specs did not answer

From the specs' own UNKNOWN sections, still open:

- **Original multi-touch behavior** (RULES) — bypassed by decision R-115.
- **Whether the 0.3 s cancellation was intentional** (RULES) — resolved 2026-08-30: the author confirmed it is a bug; R-107 removes it, and an independent exhaustive search proved no level requires it (RULES §4.1 correction).
- **Achievement/leaderboard display names, icons, descriptions** (MODES §6) — only IDs survive; new names must be authored for R-205/R-306.
- **Whether the old Play/App Store listings still exist and who controls them** (MODES §6) — decides new-app vs. update, and R-1203.
- **`POL-pencil-maze-long.wav` license and `Overhaul.ttf` provenance** (ASSETS §8) — sidestepped by not shipping them (R-902); reopen only if you want the original track back.
- **Which libc `rand()` shipped** (GENERATOR) — irrelevant under R-402.
- **Missing `GameCenter.cpp` / which platforms shipped** (PORT_NOTES §7) — irrelevant to the rebuild.

Not answered anywhere in the specs (the 2014 game predates these concerns) —
all resolved above as NEW decisions pending your approval:

- Ad formats, placement, cadence, and mediation posture.
- Consent, privacy policy hosting, ATT posture, data-safety declarations.
- Remove-ads product, price, and store setup.
- Analytics scope.
- Art direction, replacement audio/font, accessibility posture.
- Target audience / content rating declarations for the Play listing.
- Whether Free Play should surface seeds (daily challenge etc.) now that
  generation is deterministic under R-402 — the original could not; parked as
  a LATER design question, no requirement written.
