# Architecture

Every system starts with two questions: **what is the shape of the data**, and
**what tasks do we perform on it**. The answers are artifacts — the schema
(§2) and the action registry (§3) — and everything else in this repo derives
from them. This document is the baseline for Grid Infect and for the engine
kernel the next game reuses; the gate tests (§6) keep it honest mechanically,
so amend it in the same change that amends the code.

## 1. Module graph

Boundaries live in the module graph, not the deployment topology. Four
assemblies, dependencies point strictly inward:

```
GridInfect.Game (Unity adapter)     GridInfect.Core.Tests (edit-mode / dotnet)
        │                                   │
        ▼                                   ▼
              GridInfect.Core  (rules, actions, generator, save model)
                        │
                        ▼
              Bloodhound.Engine  (reusable kernel: actions, log, RNG, JSON)
```

| Assembly | Contents | May reference |
|---|---|---|
| `Bloodhound.Engine` | Action dispatch/registry/log, `MiniJson`, `Pcg32`. Game-agnostic — the piece that moves to the next game (the logic game) unchanged. | nothing (`noEngineReferences`) |
| `GridInfect.Core` | Schema types, `Rules` (the mechanics), the actions, `LevelGenerator`, baked classic levels, `SaveCodec`, `Queries`. | `Bloodhound.Engine` only (`noEngineReferences`) |
| `GridInfect.Game` | Everything Unity: boot, camera, screens, board/piece views, input, tweens, the 0.3 s beat, save file IO. Parses input, dispatches one action or reads one query, renders the result. | Core, Engine, UnityEngine |
| `GridInfect.Core.Tests` | NUnit suites; run identically in Unity edit mode and under `dotnet test` via the mirror projects in `src/`. | Core, Engine |

Source lives once, under `unity/Assets/_Project/`; the `src/` solution
compiles the same files headless (`src/Core.Mirror`, `src/Tests.Mirror`) and
type-checks the adapter against API stubs (`src/Game.Mirror` +
`src/UnityStubs` — compile-only, never shipped).

A future `GridInfect.Services` assembly (ads/consent/IAP, per
`docs/REQUIREMENTS.md` §6–8) sits beside `GridInfect.Game`: SDK types stay
there and never leak into Core (R-1303).

## 2. Schema (the first artifact)

The model owns truth; invariants are enforced in constructors and types, so
the core never sees an unvalidated shape. Adapters parse raw input (JSON,
files, touches) into these types at the boundary.

| Type | Shape | Invariants |
|---|---|---|
| `Grid` | 6 rows × 11 columns, row-major `loc = i*11 + j` | fixed forever for this game (RULES §1) |
| `Cell` | byte values `0` void, `1` active, `2` wall, `3` switch, `4` infected, `5` trap, `99` undo mark | 2/3/5 immutable during play; 99 never visible between moves |
| `Tile` | enum, the 15 L/R/U/D arm combinations | **ordinal order is contract** (rand-domain + original enum order); never reorder |
| `Difficulty` | enum Beginner…Challenging | ordinal indexes the save arrays; never reorder |
| `LevelDef` | immutable board (66 bytes) + ordered `Tile[]` (1–8) | cell values ∈ {0,1,2,3,5}; validated at construction |
| `LevelSession` | working board, `PieceState[]`, repel queue, `ResetTripped`, `ResolutionPending`, `Solved` | mutated only by `Rules`, called only by actions |
| `Profile` | unlocked set, best times ms[5], run counts[5], muted | pure data; serialization only via `SaveCodec` (versioned JSON, expand/contract) |
| `GameState` | mode + classic id / free-play run + `Session` + `Profile` | wall-clock time enters **only** through action inputs |

`ResolutionPending` is the model's name for the original's 0.3 s presentation
beat: a placement leaves consequences pending; `board.resolve` lands them.
The original's touch-cancellation of that beat is a confirmed bug and is not
ported (R-107) — the adapter *fast-forwards* (resolves immediately) when input
arrives inside the beat. The core rejects any board action while pending, so
the invariant is structural, not conventional.

## 3. Action registry (the second artifact)

Actions are the only writers of meaningful state. Each has one owner module,
a validated input schema, version 1, and lands in the log. Mechanical reads
live in `Queries` and carry zero rules.

| Action | Input | Owner | Effect |
|---|---|---|---|
| `level.load` | `levelId` | LevelActions | enter a classic level (unlock gating is presentation policy, as in the original) |
| `level.generate` | `difficulty, seed, count=5` | LevelActions | start a Free Play run; generation is pure PCG32 from the logged seed |
| `level.reset` | — | LevelActions | in-level replay button while unsolved: full board reset |
| `piece.place` | `piece, i, j` | BoardActions | legality per RULES §3, spread per §4; leaves resolution pending |
| `board.resolve` | — | BoardActions | RULES §4.1 order: win check first, else reset if tripped, else repels in queue order |
| `piece.clear` | `piece` | BoardActions | undo per RULES §7 (99 marking, re-propagation, queue accumulation) |
| `progress.unlock` | `levelId` | ProfileActions | solving id N unlocks N+1; dispatched by the adapter on solve, so replay reproduces progression |
| `settings.mute` | `muted` | ProfileActions | audio preference |
| `freeplay.begin` | `nowMs` | FreePlayActions | BEGIN pressed; run clock starts (wall clock via input) |
| `freeplay.advance` | — | FreePlayActions | next generated level, clock keeps running |
| `freeplay.complete` | `nowMs` | FreePlayActions | 5th solve: best time iff lower, count++, rejects a backward clock |
| `freeplay.abort` | — | FreePlayActions | leave a run; nothing recorded |

Adding a capability = a new action (or a new version of one); never an
in-place break of a logged contract. Rejections are answers, not errors: a
failed `Validate` returns a reason, logs nothing, mutates nothing.

## 4. The action log

The load-bearing primitive (`Bloodhound.Engine.ActionLog`): append-only
`{seq, action, version, input}` with a per-run id; `seq` within the run is the
idempotency key. Everything falls out of the one structure:

- **Replay**: fold the log over fresh state through the same dispatcher.
  `VectorReplayTests` proves it — every level's test run is replayed from its
  own log and must land on the same board.
- **Determinism**: RNG seeds and wall-clock stamps are action *inputs*, so a
  log replays bit-identically — including generated Free Play boards.
- **Audit/bug reports**: `ActionLog.ToJson()` is a complete reproduction of a
  session (a save file that can also carry the log later, retries, sync — all
  additive when wanted).

## 5. The rules, and where the truth comes from

`GridInfect.Core.Rules` is a line-faithful port of the 2014 `Game.cpp`,
specified in `docs/RULES.md`. Proof of equivalence is layered:

1. **Placement path**: all 128 shipped levels replay their recorded solutions
   with every per-step golden board (`docs/test_vectors.json`, R-114).
2. **Undo path** (no shipped vectors): adversarial scenarios cross-checked
   against a second independent port — `tools/gen_undo_fixtures.py` extends
   the Python reference with a literal `clearPiece` and bakes expected
   outcomes into `UndoFixtures.g.cs`.
3. **Generator**: structural invariants, the GENERATOR §5 solvability proof
   replayed through the real rules, and golden seed lock-ins.

Faithfully ported quirks (contract, do not "fix" silently):
- repel queue is cleared only at the next placement, so undo re-propagation
  accumulates and re-runs it (RULES §7);
- `ResetTripped` survives a winning resolution and can full-reset mid-undo;
- the win check tests only for value 1, so a mid-undo check **ignores 99
  marks**: clearing a piece can fire a "win" while a protected cell is still
  uninfected (`mid_undo_win_check_ignores_99_marks` documents it). Flagged as
  a design decision for the author: keep or fix in a versioned change.

## 6. Mechanical gates

Cheap to amend in a normal change, so the layering iterates instead of
ossifying or eroding:

- **asmdefs** enforce the module graph in Unity (`noEngineReferences` on
  Engine and Core).
- **`ArchitectureGateTests`** enforce the same rules under `dotnet test`:
  no `UnityEngine` in Engine/Core sources, no `GridInfect` in Engine sources,
  no direct `Rules` mutation from the adapter, registry ⇔ constants ⇔ this
  document all in sync, `aggregate.verb` naming.
- **CI** (`.github/workflows/ci.yml`) runs the full suite on every push.

## 7. Performance posture

The hot path is spread propagation: flat `byte[66]`, struct pieces and
repels, stackalloc direction flags, no LINQ, no allocation per placement
beyond log entries (human-rate). JSON exists only at boundaries (save, log
serialization, test fixtures). The renderer redraws cells only on
`CellChanged`, never per frame; text and sprites come from one shared
texture/font. Frame budget target is 60 fps (R-1104) with a turn-based load
that rounds to zero — the discipline is the point, the next game inherits it.

## 8. Presentation contract

The Unity layer owns exactly one piece of timing: scheduling `board.resolve`
0.3 s after a drop (fast-forwarding on input). Everything else it does is
listen (`CellChanged` / `LevelSolved` / `PiecesUnbound` — R-113) and render.
The baseline look is 100% procedural (one white texture, built-in font, zero
packages, zero serialized scene content — any empty scene boots via
`RuntimeInitializeOnLoadMethod`), so the project runs on a fresh clone; the
art overhaul replaces looks without touching structure. Timing table:
`PresentationConfig` (from ASSETS §6, linear everywhere). Accessibility is
structural from day one: every special cell state carries a shape glyph,
never color alone (R-1001).

## 9. Change policy

- New capability → new action or new schema version; expand/contract on the
  save format (`SaveCodec`: additive fields with defaults, unknown keys
  tolerated).
- Behavior changes to rules/generator are versioned changes with golden-test
  updates in the same commit — a golden diff is a player-visible change.
- Definition precedes estimation: state what a thing is (here, in schema and
  registry terms) before sizing it.
