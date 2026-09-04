# Grid Infect

A Unity/C# rebuild of Grid Infect (Bloodhound Studios, 2014 — originally
cocos2d-x). Drag pieces onto a 6-wide, 11-tall grid; each piece infects along its arms;
infect every cell to win. The 128 shipped classic levels
live on as Legacy; play is 17 generated worlds (unique, deduction-solvable,
graded), a Daily (same board for everyone) and Endless by grade.

The rebuild is mechanically equivalent to the original by construction: the
rules engine replays all 128 shipped levels' solutions against per-step golden
board states extracted from the 2014 code.

## Repo map

| Path | What it is |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | The baseline: schema, action registry, module graph, gates. Start here |
| [`unity/`](unity/) | The Unity project (Unity 6). All C# lives here, once |
| [`unity/Assets/_Project/Engine/`](unity/Assets/_Project/Engine/) | `Bloodhound.Engine` — reusable, game-agnostic kernel (actions, log, RNG, JSON) |
| [`unity/Assets/_Project/Core/`](unity/Assets/_Project/Core/) | `GridInfect.Core` — rules, actions, generator, levels, save model. Pure C#, zero UnityEngine |
| [`unity/Assets/_Project/Game/`](unity/Assets/_Project/Game/) | Unity adapter: procedural UI, input, the 0.3 s beat. No game logic |
| [`unity/Assets/_Project/Tests/EditMode/`](unity/Assets/_Project/Tests/EditMode/) | NUnit suites (vector replay, undo fixtures, generator goldens, gates) |
| [`src/`](src/) | dotnet mirror solution: builds and tests the same sources headless, no Unity needed |
| [`docs/`](docs/) | The port specification extracted from the original (rules, generator, modes, assets, requirements, dependencies) + `test_vectors.json` |
| [`docs/NEXT_PASS.md`](docs/NEXT_PASS.md), [`docs/EXECUTION_PLAN.md`](docs/EXECUTION_PLAN.md) | The next-pass decisions and the staged plan with its status table |
| [`docs/GENERATOR_V2.md`](docs/GENERATOR_V2.md), [`docs/RULES_V2.md`](docs/RULES_V2.md) | The deduction solver, trace grader, sampler and constructor (solution-first, subtractive, minimal); the rules every generated level runs on |
| [`docs/worlds/`](docs/worlds/), [`docs/daily/`](docs/daily/) | The shipped worlds and the seven Daily pools as JSONL (one header line with the generator spec, one level per line), baked into `WorldData.g.cs` / `DailyData.g.cs` |
| [`src/GenLevels/`](src/GenLevels/) | The offline level generator behind `tools/gen_levels`, `tools/gen_worlds.sh` and `tools/gen_daily.sh` |
| [`grid-infect-style/`](grid-infect-style/) | The locked visual style (`STYLE-GUIDE.md`): bugs on a printed circuit board. Tokens, the vector asset generator, the reference mockups |
| [`grid-infect-bug-glyph/`](grid-infect-bug-glyph/) | The bug glyph grammar (`BUG-GLYPH-SPEC.md`) and its generator; the Unity rasteriser (`View/BugGlyph.cs`) is a port of it |
| [`docs/infection-vfx-spec.md`](docs/infection-vfx-spec.md) | The infection animation and its locked parameters, plus an "As built" section recording every deviation, including the style pass |
| [`tools/`](tools/) | Mechanical derivations: level and world baking, undo fixtures, the solver oracle and its golden, world generation |
| [`grid-infect-cocos2dx/`](grid-infect-cocos2dx/) | The original 2014 source, kept as reference. Not built |

## Run the tests (no Unity required)

```sh
cd src
dotnet test    # golden vector replay (128 levels), undo cross-check, generator goldens, gates
```

Requires a .NET 8 SDK. The same tests run inside Unity's edit-mode runner.

## Open the game (Unity)

Full first-open walkthrough (what to commit, folder layout, asset policy):
[`docs/UNITY_SETUP.md`](docs/UNITY_SETUP.md).

1. Unity Hub → Add → the `unity/` folder (not the repo root). The pinned
   editor is in `ProjectSettings/ProjectVersion.txt` (currently
   **6000.5.10f1**); any newer Unity 6 patch works — accept the upgrade if
   Hub offers one.
2. Open the project, let it import (first open generates `Library/` and meta
   files — commit the `.meta` files it creates). URP is set up automatically:
   an editor script creates and assigns the pipeline asset under
   `Assets/Settings/` on first open.
3. Press **Play** in any empty scene — the game boots procedurally
   (`RuntimeInitializeOnLoadMethod`), no scene setup required. Set the Game
   view to a portrait resolution (e.g. 1080×2340).
4. Tests: Window → General → Test Runner → EditMode → Run All. Headless:
   `Unity -batchmode -runTests -testPlatform EditMode -projectPath unity`.
   The suite is a deliberately limited, load-bearing subset (golden replay,
   undo cross-check, generator goldens, gates) — mirror-run by CI on every
   push.

Presentation is still 100% procedural — no imported art, no serialized scene
content — and it is now the locked style (`grid-infect-style/STYLE-GUIDE.md`):
a printed circuit board under every screen, frosted-glass tiles in a recessed
well, the infection as light inside the glass, and the bug glyphs rasterised
at runtime from the same grammar as the SVGs. The board is one quad, one
material, one draw call, with cell state in a data texture the shader reads
(`docs/infection-vfx-spec.md`). Colour lives in a single `BoardPalette`
asset, which also carries the blue and breadboard skins. The chrome (glass
chips, the lock badge, tray slots, popups) is sized from the guide's px
tokens in `PresentationConfig.Style`. The two typefaces (Chakra Petch, Share
Tech Mono) are requested by name and fall back to a system face until their
TTFs are imported.

## Regenerating derived files

```sh
python3 tools/bake_levels.py          # docs/test_vectors.json -> ClassicLevelData.g.cs
python3 tools/gen_undo_fixtures.py    # Python reference -> UndoFixtures.g.cs
python3 tools/gen_level_metrics_golden.py   # Python solver oracle -> docs/level_metrics_classic.json
python3 tools/bake_worlds.py          # docs/worlds/*.jsonl -> WorldData.g.cs
tools/gen_worlds.sh                   # regenerate docs/worlds/*.jsonl from their recorded seeds (needs .NET)
tools/gen_levels --grade G3 --count 25 --seed 1 --pieces 4-5   # ad-hoc batch generation (JSONL)
python3 docs/tools/verify_test_vectors.py   # sanity: vectors self-verify
```

## Status

- Core rules, generator, save model, action log: done, fully tested.
- Next pass (`docs/EXECUTION_PLAN.md`): solver, generator v2, worlds,
  Daily/Endless, Lock, RulesV2 and the five new elements are in; ads,
  consent and remove-ads wait on the SDK packages and a device build.
- Board VFX, portrait layout across every screen: written, and verified by
  numbers and by a WebGL port of the shader — see `docs/infection-vfx-spec.md`.
- Visual style pass (PCB, glass, bug glyphs): written against the locked
  guide; the glyph rasteriser is verified against the SVG sheet headlessly,
  the three shaders have not been compiled by Unity yet.
- **Nothing has been through the Unity editor yet.** The adapter is
  compile-checked against API stubs (`src/`) and the shader has never been
  compiled by Unity. `docs/UNITY_SETUP.md` §6 is the first-run checklist,
  ordered by what is most likely to bite.
- Later waves (ads, IAP, consent, services): specified in
  `docs/REQUIREMENTS.md` / `docs/DEPENDENCIES.md`, deliberately not in this
  baseline.
