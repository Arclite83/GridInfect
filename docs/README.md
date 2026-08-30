# Grid Infect — Port Specification

Portable specification of the original Grid Infect (cocos2d-x 2.2.3,
Bloodhound Studios, 2014) for a Unity/C# rebuild. Everything here was
extracted from `../grid-infect-cocos2dx/`; every claim cites its source
file. Nothing is redesigned.

| File | Contents |
|---|---|
| [`RULES.md`](RULES.md) | Complete mechanical spec: board, cell types, placement, exact infection-spread semantics, repel switches, reset traps, undo, win/loss. Reimplementable without reading the C++ |
| [`GENERATOR.md`](GENERATOR.md) | The free-play level generator: solution sampling, board carving, constraints, difficulty configs, RNG (and why it is **not** seed-deterministic) |
| [`test_vectors.json`](test_vectors.json) | All 128 shipped levels: boards, piece lists, verified solutions, and per-placement golden board states. Two levels are flagged as winnable only via a timing quirk (`requires_reset_cancel_exploit`) |
| [`MODES.md`](MODES.md) | Classic progression, timed Free Play rules, unlock ladders, save format, achievements/leaderboards (Google Play Game Services — dead) |
| [`ASSETS.md`](ASSETS.md) | Art/audio/font/string inventory with sizes, third-party license flags, transcribed tutorial copy, and the full presentation-timing spec (durations, easing, input feedback) |
| [`PORT_NOTES.md`](PORT_NOTES.md) | What does not translate: engine-coupled behavior, dead services, broken/missing files in this snapshot, C++-isms |
| [`tools/verify_test_vectors.py`](tools/verify_test_vectors.py) | Executable reference implementation of the core rules; verifies every vector. A C# port matching its outputs is mechanically equivalent to the original |

Verify the vectors any time:

```
python3 docs/tools/verify_test_vectors.py
```
