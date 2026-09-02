# Next pass — decisions (2026-09-02, rev 3)

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
3. **New elements** (see the table below). In order: short arms, the
   area piece, forbidden cells, diagonal arms, then mirrors and relays.
   Walls used deliberately throughout. Fixed pieces as world spice. Cut:
   pre-infected cells, rotation, knight jumps, multi-strain infection.
4. **Content.** ~1000 levels as worlds of 20–25, one element per world, batch
   generated and graded, edges of each world hand-reviewed. The 128 classic
   levels move to a **Legacy** mode: unchanged rules, no hints. Their vector
   tests remain the rules oracle.
5. **Modes.** Timed Free Play is retired. **Daily** (date-seeded, same board
   for everyone, streaks, timer as a stat with a par, friends board only) is
   the headline. **Endless by grade**, no clock. No global leaderboards.
6. **Naming.** Not "Grid Infect 2". New app on the stores regardless.
7. **Platform.** Mobile only. Android first, iOS fast follow. Web, premium,
   Steam, subscription catalogues: considered and rejected.
8. **Monetisation.** Interstitial on solve (existing cadence rules), rewarded
   ad earns a lock (below), remove-ads at one price removes interstitials
   only. No consumables, no energy. Replaces R-603 (rewarded skip).
9. **Ads stack.** AdMob via the Google Mobile Ads Unity plugin, UMP for
   consent, Unity IAP for remove-ads — i.e. REQUIREMENTS §6–8 as written.
   Chosen over Unity LevelPlay because the consent flow ships with it, the
   spec already exists, and Unity Ads can be added later as a mediated
   network if fill needs it. iOS ATT/SKAdNetwork stay with the iOS follow.

## Lock (the hint replacement)

One tool, no explanation text: **Lock** places one unplaced piece at its
solution cell and locks it there (lock icon, snap animation, cannot be
lifted, survives a reset-trap full reset). If a player's piece occupies that
cell it returns to the tray. Which piece: the solver's next forced
deduction from the player's current correct pieces when a solver exists;
until then, the unplaced piece with the largest solution coverage. Needs a
stored solution per level, which generator v2 produces and the classic
vectors already hold — so Legacy can have Lock too.

Engine: `PieceState.Locked`; input refuses to lift; `FullReset` skips locked
pieces; undo re-propagation treats them as ordinary placed pieces.

Economy: wallet, cap 10, start 5, +1 per rewarded ad, +1 per 7-day daily
streak. Remove-ads does not change it. The cap only bounds free grants;
ad-earned locks are revenue, so there is no reason to cap them tighter.

## New element candidates

Scored on: does it add a deduction a human can make (vs. friction), rules
cost, and presentation cost. Any element that infects off a piece's own row
and column (area, diagonal, mirror, relay) invalidates the row/column
retraction in `Rules.ResetBoard`, so the first one of those to land comes
with **RulesV2**: undo = restore initial board, re-propagate placed pieces
in index order. Classic/Legacy stays on the frozen rules and its 128
vectors; new content runs on V2. That refactor is paid once.

| Element | What it does | Deduction value | Cost | Call |
|---|---|---|---|---|
| Short arms | Arm reaches 1 or 2 cells, not the edge | High: local reasoning, walls matter more | Trivial (per-arm range) | Build first |
| Area piece ("blot") | Infects its 3×3 neighbourhood; walls/switches/traps inside are inert | High: a blob family alongside lines; pairs with diagonals | Low rules; medium VFX | Build second, needs V2 |
| Forbidden cells | Must stay clean; a placement whose spread would hit one is illegal and bounces | High: prunes options instead of truncating coverage (the trap without punishment) | Low | Build third |
| Diagonal arms | Four more directions, curated tile set | High | Medium (Dir, generator, shader, solver family) | Committed, after the above |
| Mirror tile | Turns an incoming arm 90° | Medium-high, laser-puzzle paths; reads well in VFX | Medium, needs V2 | After diagonals |
| Relay cell | When infected, emits its own arm(s) | High, chain reactions | Medium, needs V2 | After mirrors |
| One-way wall | Blocks from one side only | Medium | Low | Filler, any time |
| Decoy piece | One tray piece is not needed | Medium (breaks the exact-count tell) | Zero | World spice |
| Fixed piece | Pre-placed, immovable | Medium | Low | World spice |
| Blot with arms | 3×3 core plus arms | Combinatorial; hold until blot is proven | Low once blot exists | Later |
| Multi-strain | Two infection colours, cells demand one | Large but doubles art and rules | High | Shelved |
| Knight jump | Infects at (±1,±2) | Novelty, poor legibility | Low | No |
| Pre-infected cell | Starts at 4 | None without a switch | — | Cut |
| Rotation | Player rotates pieces | Negative: collapses tile identity | — | Never |

## Build order (serial)

1. Deduction solver + grader in Core (delegable: closed interface, exact
   oracle, no art).
2. Generator v2.
3. 1000-level bake, world structure, Legacy mode.
4. Daily + Endless replace timed Free Play.
5. Lock tool (Core + view), then AdMob + UMP + Unity IAP. First Android
   store build.
6. RulesV2, then short arms, blot, forbidden cells, diagonals, mirrors,
   relays — each as new worlds through the same pipeline. iOS follow lands
   alongside whichever of these is current.

Fixed date ⇒ step 6 flexes first, then level count. Step 1 cannot be cut.
