# Grid Infect

A Unity/C# rebuild of Grid Infect (Bloodhound Studios, 2014 — originally
cocos2d-x). Drag pieces onto an 11×6 grid; each piece infects along its arms;
infect every cell to win. 128 shipped classic levels plus a timed Free Play
mode with generated boards.

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
| [`tools/`](tools/) | Mechanical derivations: level baking, undo-fixture generation |
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

The baseline presentation is deliberately placeholder (one white texture,
built-in font, everything procedural under URP): the mechanics, architecture,
and tests are the deliverable; the art/feel overhaul lands on top without
touching structure.

## Regenerating derived files

```sh
python3 tools/bake_levels.py          # docs/test_vectors.json -> ClassicLevelData.g.cs
python3 tools/gen_undo_fixtures.py    # Python reference -> UndoFixtures.g.cs
python3 docs/tools/verify_test_vectors.py   # sanity: vectors self-verify
```

## Status

- Core rules, generator, save model, action log: done, fully tested.
- Unity adapter: written and compile-checked against API stubs (`src/`);
  first in-editor run happens on a machine with Unity installed.
- Later waves (ads, IAP, consent, services, art): specified in
  `docs/REQUIREMENTS.md` / `docs/DEPENDENCIES.md`, deliberately not in this
  baseline.
