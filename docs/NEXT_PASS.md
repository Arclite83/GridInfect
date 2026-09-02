# Next pass — decisions (2026-09-02)

Outcome of the level-design and business assessment. Numbers come from
`tools/level_metrics.py` (exact solution counts over the 128 classic levels
and 120 generated boards per difficulty). The full write-up with charts is the
"Grid Infect Next Pass" artifact; this file is the part that binds the repo.

## What the data showed

| Metric | Classic 128 | Generator (Challenging) |
|---|---|---|
| Unique-solution boards | 27 / 128 | 1 / 120 |
| Median valid solutions, levels 1–80 | 2–4 | — |
| Median valid solutions, levels 81–128 | 13–29 | 78 (p90 1270) |
| Levels using reset traps | 100 (all of 81–128) | 0 |
| Levels using repel switches | 18 (none after 61) | 0 |
| Levels needing every piece | 127 | — |

- The classic set has a size curve, not a difficulty curve.
- The generator emits no obstacles; its only difficulty axis is pieces × cells.
- Cell-level "sole cover" deduction never fires in this ruleset (0/128,
  0/600): any piece placed on a cell covers it. Deduction is about **lines**
  (row and column segments an arm can own). A human-rules solver must be
  written in those terms.
- Static coverage and order-aware solution counts agree on all 128: switches
  and traps never invalidate a covering set; they cut alternatives.

## Decisions

1. **Pipeline first.** A tiered, line-based deduction solver and grader in
   `GridInfect.Core`, with the 128 as its regression set. Critical path.
   Direction families (rows, columns, later diagonals) are data, not code
   paths, so diagonals are a table entry when they come.
2. **Generator v2** = sample solution → carve → place obstacles to prune to a
   unique solution → accept only if the solver finishes without guessing →
   grade → canonicalise under flips and dedupe. Walls are the primary
   pruning tool; wall density and shape are per-world tunables.
3. **New elements.** Walls used deliberately (now). Fixed pieces (after).
   Diagonal arms: committed, own worlds and a daily variant, after the
   pipeline. Relay cells: after diagonals. Pre-infected cells: cut (without a
   switch on the line it is a placeable void). Rotation: never.
4. **Content.** ~1000 levels as worlds of 20–25, one element per world, batch
   generated and graded, edges of each world hand-reviewed. The 128 classic
   levels move to a **Legacy** mode: unchanged rules, no hints. Their vector
   tests remain the rules oracle.
5. **Modes.** Timed Free Play is retired. **Daily** (date-seeded, same board
   for everyone, streaks, timer as a stat with a par, friends board only) is
   the headline. **Endless by grade**, no clock. No global leaderboards.
6. **Naming.** Not "Grid Infect 2". New app on the stores regardless.
7. **Platform.** Mobile only, Android and iOS together. Web, premium, Steam,
   subscription catalogues: considered and rejected.
8. **Monetisation.** Interstitial on solve (existing cadence rules), rewarded
   ad earns a hint, remove-ads at one price removes interstitials only. No
   consumables, no energy. Replaces R-603 (rewarded skip).
9. **Ads stack.** Unity Ads via LevelPlay, Unity IAP for remove-ads. A
   TCF-registered CMP is still required (Unity ships none); pick at the ads
   step. iOS ATT/SKAdNetwork/privacy manifest join the launch scope.
   R-601, R-604, R-605, R-801, R-802 currently say AdMob/UMP and need
   rewriting at that step.

## Procedural hints

The hint is the **solver run from the player's current board**, on device,
never a stored solution replayed (the player's state diverges from any stored
path as soon as they place a piece the solution lacks).

1. *Misplaced piece*: a placed piece not in the unique solution → highlight
   it, say nothing else.
2. *Next deduction*: first tier rule that fires from the current state →
   highlight its line segment, state the rule in one sentence.
3. *Escalation*: second hint on the same step narrows to candidate cells,
   third reveals the placement. One hint each.

Precondition: the level is unique and deduction-solvable, which is exactly
the generator's acceptance gate. Legacy levels do not qualify → no hints.
The deduction trace is baked with each level as a validation artifact and to
derive par time.

Hint economy: wallet, cap 3, start 3, +1 per rewarded ad, +1 per 7-day daily
streak. Remove-ads does not change it.

## Build order (serial)

1. Deduction solver + grader in Core (delegable: closed interface, exact
   oracle, no art).
2. Generator v2.
3. 1000-level bake, world structure, Legacy mode.
4. Daily + Endless replace timed Free Play.
5. Hints (Core), then LevelPlay + CMP + Unity IAP. First store builds.
6. Diagonals, then relays, each as new worlds through the same pipeline.

Fixed date ⇒ step 6 flexes first, then level count. Step 1 cannot be cut.
Shipping both stores at launch is scope that should not flex.
