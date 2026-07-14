# Tasks — Core Puzzle (Match-3 Board)

Each task is a vertical, independently-verifiable slice. Status is tracked
inline; check a box only once its verification step actually ran and passed.

## Methodology note (applies from Task 4 onward)

Tasks 1–3 were built "implement, then write and run a verification suite
against it" — every check genuinely compiled and ran (no fabricated
results), but the tests were written after the implementation, not before.

Starting at Task 4, this switches to strict TDD per an explicit request:
for each unit of behavior, (1) write a test that exercises an API that
doesn't exist yet or asserts behavior the current code doesn't have — this
must fail, and in this dependency-free harness "fail" usually means "fails
to compile" since the method/class isn't there yet (RED); (2) write the
minimum production code to make it pass (GREEN); (3) self-review the
resulting code for bugs, missed edge cases, and consistency with
requirements.md/design.md, fixing anything found and re-running until a
review pass finds nothing — only then is the task marked done. Each
sub-step below shows its actual RED failure and the GREEN fix, not just the
end state, so this is auditable rather than asserted.

- [x] **1. Core data model, match detection, board generation**
  - `TileType`, `BoosterType`, `Tile`, `BoardConfig`, `Board`
  - `MatchFinder.FindMatchedCells` / `HasAnyMatch`
  - `BoardGenerator.Generate` (constructive matchless fill + legal-move
    guarantee + initial credit bag placement)
  - _Satisfies: Requirement 1 (all), Requirement 2 (all)_
  - _Verification: `verify/Program.cs`, run via `dotnet run` — see below._

- [x] **2. Match group classification**
  - `MatchGroup`, `MatchResolver.FindMatchGroups` (connected-component
    flood fill over `MatchFinder`'s flat cell set), `BoosterRules`
    (color -> booster mapping per GDD §7.2, verbatim).
  - _Satisfies: Requirement 2b (added below)._
  - _Verification: extends `verify/Program.cs`._
  - _Resequencing note: this task was originally written as "cascade-aware
    match resolution" including a combo-chain counter. Re-checked against
    the GDD before implementing (see design.md §2 changelog): boosters are
    keyed by color + size only, not shape, so the shape-classification half
    of the original plan was dropped as unfounded. The combo-chain counter
    needs an actual cascade loop to count rounds of — there's nothing to
    chain into without Task 4's gravity/refill — so it now lives there
    instead of being stubbed out here._

- [x] **3. Swap validation & commit/revert**
  - `Board.SwapTiles` (extracted, also now reused by `BoardGenerator`'s
    solvability check instead of duplicating swap logic).
  - `SwapEngine.TrySwap` — bounds + adjacency check, tentative swap, run
    `MatchResolver`, commit (returning the resulting `MatchGroup`s) or
    revert.
  - _Satisfies: Requirement 3._
  - _Verification: extends `verify/Program.cs`._

- [x] **4. Gravity, refill, and the cascade/combo loop**
  - After a clear, drop remaining tiles down, fill empty top cells with new
    random tiles, loop (re-scan for matches, clear, drop, refill) until a
    scan finds no matches.
  - **Correction vs. the original draft of this item:** an earlier version
    said refills should reuse Task 1's matchless-fill heuristic "so they
    don't reintroduce accidental matches." That was wrong and has been
    removed before implementation — re-checked against the GDD before
    writing any test. Matchless fill is a Task 1 *initial-board-fairness*
    rule (a player shouldn't be handed free, unearned matches at level
    start). During cascades, a refilled tile creating a new match is the
    *intended* mechanic — it's literally what makes a "combo chain" possible
    (GDD §6.2: "Combo x3 or more"; §11.2 lists "combo chains, cascade logic"
    as one responsibility). Refill here is plain uniform-random; the loop's
    re-scan is what catches and resolves any resulting match next round.
  - Combo chain counter: number of cascade rounds triggered by one
    swap/resolution. GDD §6.2's exact wording ("Combo x3 or more") doesn't
    define what's being counted to 3 — interpreted here as **cascade rounds
    in one resolution chain** (not e.g. cells-per-match), since that's the
    only quantity "combo" naturally refers to elsewhere in the GDD (§5.1:
    "Music ... tempo increases with combo count"; §11.2: MatchFinder owns
    "combo chains, cascade logic" as one concept). Documented as an
    interpretation, not a literal GDD quote, since the source doesn't spell
    it out.
  - When the chain reaches >= 3 rounds: report +10 bonus credits earned
    (GDD §6.2) and drop one additional credit bag onto the board (GDD §6.2:
    "mid-level bonus bag drops on board"; §7.1: "more during combo chains").
    Scope boundary: this task computes and reports the bonus and performs
    the board-mutation of placing the bag; it does **not** touch a running
    player credit balance — that's Task 7 (level completion/payout), which
    doesn't exist yet, so there's nothing to add the credits to.

- [x] **5. Booster spawn & activation**
  - 4+ match → spawn the corresponding `BoosterType` (GDD §7.2 table).
  - Activation effects: row clear, column clear, 3×3 zone, all-of-one-color,
    5 random tiles, bottom two rows.
  - `BoosterActivation.GetAffectedCells` (pure, per-type effect lookup),
    `CascadeEngine.DetermineClearedCells` (spawn-on-match + chained
    activation, extracted the same way Task 4 extracted `ClearGravityRefill`
    for independent testability).
  - _Satisfies: Requirement 5 (all criteria)._
  - _Verification: extends `verify/Program.cs` — see below._

- [x] **6. Credit bag collection & mid-level spawns**
  - Collecting a flagged tile adds to the run's credit total; extra bags can
    drop mid-level during big combo chains (GDD §7.1).
  - _Verification: extends `verify/Program.cs` with credit-bag collection
    and bonus-bag drop invariants._

- [x] **7. Level objective, completion, and star rating**
  - Objective types (score / collect-N / clear-board), 1/2/3-star thresholds,
    credit payout per GDD §6.2 (20 / 35 / 55 credits).
  - _Verification: extends `verify/Program.cs` with level objective completion,
    star calculation, and credit payout tests._

- [x] **8. Lives (Hearts) system**
  - Max 5 hearts, lose 1 on level fail, regen 1 per 30 minutes (GDD §7.4),
    persistence hook (actual save/load is a `Core/SaveSystem` concern, out of
    scope here — this task only owns heart-count business logic).
  - _Verification: extends `verify/Program.cs` with lose-heart and regen tests._

- [ ] **9. Unity presentation layer — `BoardController`**
  - MonoBehaviour wrapping the now-stable core: tile prefab spawn/animate,
    drag input → `Board.TrySwap`, visual cascade playback.
  - First task that's allowed to reference `UnityEngine`.
  - _Implementation: `Assets/Scripts/BoardController.cs` and `Assets/Scripts/BoardTileView.cs`._

- [x] **10. `ScriptableObject` level data for Island 1, Levels 1-5**
  - `LevelData` asset definitions (objective, move limit, allowed tile
    types) so Tasks 7 and 9 have real content to run against, not just
    synthetic test boards.
  - _Implementation: `Assets/Scripts/Match3/LevelData.cs` and `Assets/Scripts/LevelDataAsset.cs`._

- [ ] **11. Manual booster activation via swap**
  - Two new swap outcomes, both bypassing `SwapEngine`'s normal
    only-commit-if-match rule: (a) swapping two adjacent **BloomBurst**
    boosters together fires BloomBurst's row-clear immediately; (b) swapping
    any booster with an adjacent **non-booster** tile fires that booster's
    own GDD §7.2 effect "aimed" through the non-booster tile's position
    instead of the booster's own (row/column/3x3-zone/one-color effects
    retarget to the swapped-with tile; the two board-anchored effects,
    SporeCloud and DeepSurge, are unaffected since they were never
    position-anchored to begin with).
  - Scope decisions (which booster pairing gets a combo effect, and what a
    booster+regular-tile swap does) were confirmed directly with the user
    rather than derived from the GDD, which doesn't address manual
    activation at all — see requirements.md's Requirement 5c for the
    full record.
  - Proposed new symbols (names only, not binding — may be renamed during
    implementation if a cleaner shape emerges): `BoosterActivation.
    GetAffectedCellsAimed` (new overload, existing `GetAffectedCells` stays
    untouched), `SwapEngine.TryManualActivationSwap`, `CascadeEngine.
    ResolveCascadeFrom` (cascade loop entry point that starts from a
    pre-determined cleared-cell set instead of discovering one via
    `MatchResolver`, so manual activations reuse the same cascade machinery
    as ordinary matches).
  - _Satisfies: Requirement 5c (all criteria)._
  - _Verification: planned RED test list below — not yet implemented, so
    not yet run. See "Planned RED tests for Task 11" at the end of this
    file._

---

## How Task 1 was verified

```
cd verify && dotnet run
```

Runs a dependency-free assertion program (see design.md §5 for why no
xUnit/NUnit) against the actual files in `Assets/Scripts/Match3/`. Covers:

1. Board has correct dimensions and only allowed tile types.
2. 500 boards across mixed seeds/sizes (9x9 default, plus edge sizes 3x3 and
   4x6) all come back with **zero** pre-existing matches.
3. Same `Seed` -> byte-identical board, twice in a row.
4. Different seeds -> (almost always) different boards, sanity-checking the
   RNG is actually being used.
5. Every generated board has at least one adjacent swap that creates a match.
6. Credit bag count on a freshly generated board is within
   `[MinInitialCreditBags, MaxInitialCreditBags]`.
7. `MatchFinder` unit-level cases: no match, exact horizontal 3, exact
   vertical 3, an L-shaped overlap (cell counted once), and a deliberately
   matchless hand-built board.
8. A degenerate config (only 2 allowed tile types) correctly throws at
   construction time rather than producing bad output.

## How Task 2 was verified

1. A straight run of exactly 3 produces one `MatchGroup`, not booster
   eligible.
2. A straight run of exactly 4 produces one `MatchGroup`, booster eligible,
   `AwardedBooster` matches the GDD §7.2 table for that color.
3. An L-shaped overlap (row-run + column-run sharing a corner, same color)
   merges into **one** `MatchGroup` of size 5 — not two separate groups.
4. Two separate matches of different colors on the same board produce two
   distinct `MatchGroup`s.
5. Two same-color matches that don't touch (not 4-directionally adjacent)
   stay as two separate groups, not incorrectly merged just for sharing a
   color.
6. `BoosterRules.ForTileType` covers all 6 `TileType` values with the exact
   mapping from GDD §7.2 (Flower->BloomBurst, Leaf->LeafWheel, Wave->
   TidalClear, Sun->SolarFlare, Mushroom->SporeCloud, Coral->DeepSurge).

## How Task 3 was verified

1. Swapping two non-adjacent tiles is rejected and the board is provably
   unchanged afterward.
2. Swapping two adjacent tiles that creates no match is rejected, and the
   board reverts to byte-identical state (not just "looks the same").
3. Swapping two adjacent tiles that creates a match is committed, the board
   reflects the swap, and the returned `MatchGroup`s are correct.
4. Out-of-bounds coordinates are rejected gracefully (no exception, no
   crash) rather than throwing an `IndexOutOfRangeException`.

## How Task 4 was verified (strict TDD: RED confirmed before any production code)

1. RED step: tests referencing `CascadeEngine`/`CascadeResult` were written and
   run first, against code that did not exist — confirmed a compile failure
   (`CS0103: The name 'CascadeEngine' does not exist`) before writing a
   single line of `CascadeEngine.cs`.
2. GREEN step: minimal implementation added, suite re-run, all passing
   (one test bug found along the way — a 2-column test board paired with an
   invalid 2-column `BoardConfig`, which Requirement 1 correctly rejects —
   fixed in the test, not the production code, since the rejection was
   correct behavior).
3. Code review pass 1: re-read `CascadeEngine.cs` against all 7 of
   Requirement 4a's acceptance criteria one by one. Found a test-coverage
   gap (asserting bag *count* changed but never asserting
   `CascadeResult.BonusBagDropped` itself) — fixed.
4. Code review pass 2: re-ran after the fix, reviewed test-to-AC mapping
   directly — clean, no further issues.

Specific checks:
- `ComputeComboBonus` is a pure function tested directly against the GDD
  §6.2 threshold table (0/1/2 rounds → no bonus; 3/4/10 rounds → flat +10 +
  bag), independent of any board mechanics.
- `ClearGravityRefill` (one mechanical pass) tested in isolation: a single
  cleared cell mid-column causes the cells below it to stay put and the
  cells above it to shift down by one, in original relative order; an
  untouched column is provably unaffected; the new top cell is refilled
  with a valid allowed type.
- `ResolveCascade` (the full loop) tested two ways: a simple single-match
  board terminates with `MatchFinder.HasAnyMatch(board) == false` (an
  RNG-independent invariant — true regardless of what refill happens to
  produce); and an engineered "telescoping" board where clearing one match
  causes survivors to compact into a second match *purely through
  compaction*, deterministically and independent of refill randomness —
  proving the loop genuinely re-scans and chains, not just runs once.
- The combo-bonus/bag-drop relationship is checked as an invariant against
  whatever round count actually occurred (`>= 3` → bonus + bag,
  `CascadeResult.BonusBagDropped == true`; otherwise neither), rather than
  pinning an exact fragile round count.
- A defensive `maxRounds` cap is tested directly: calling with
  `maxRounds: 0` against a board with an obvious match throws
  `InvalidOperationException` instead of looping unboundedly.

**Review finding recorded for Task 6 (not fixed here — out of Task 4's
scope as written):** `ClearGravityRefill` overwrites a cleared cell (via
either the gravity shift or the refill) without ever checking
`Tile.HasCreditBag` first. That means by the time Task 6 ("credit bag
collection") runs, any bag on a cell that got matched is already gone with
no record of it. Task 6 will need to either (a) have `CascadeEngine` accept
a callback/collector invoked per-cell at clear time, before the overwrite,
or (b) read `HasCreditBag` for every cell in each `MatchGroup` *before*
calling `ClearGravityRefill` (the group's `Cells` are known before clearing
happens, so this is possible without changing `CascadeEngine` itself —
option (b) is the lower-risk fix since it doesn't touch already-verified
Task 4 code). Flagging this now so Task 6 doesn't have to rediscover it.

## How Task 5 was verified (strict TDD: RED confirmed before any production code)

The GDD §7.2 booster→effect mapping (BloomBurst/LeafWheel/TidalClear/
SolarFlare/SporeCloud/DeepSurge → row/column/3x3-zone/one-color/5-random/
bottom-two-rows) wasn't in `requirements.md`/`design.md` yet — the original
.docx isn't in this repo (see CLAUDE.md). Asked the user to re-supply the
GDD §7.2 table rather than guess at it; got it, transcribed verbatim into
`requirements.md`'s new Requirement 5 before writing any test.

1. RED step: added 10 tests to `verify/Program.cs` referencing
   `CascadeEngine.DetermineClearedCells` and `BoosterActivation` — neither
   existed. Ran and confirmed a compile failure first: `CS0117:
   'CascadeEngine' does not contain a definition for 'DetermineClearedCells'`
   and `CS0103: The name 'BoosterActivation' does not exist in the current
   context` (8 occurrences total across the two).
2. GREEN step: added `BoosterActivation.cs` (pure per-type effect lookup)
   and `CascadeEngine.DetermineClearedCells` (spawn booster-eligible groups'
   topmost-leftmost cell instead of clearing it; union in
   `BoosterActivation`'s output for any already-cleared cell that's itself a
   booster tile, via a fixed-point loop for chains); wired it into
   `ResolveCascade` in place of the old flat cell-union. Full suite passed
   first try: 36 passed, 0 failed (26 prior + 10 new).
3. Review pass: re-read both changed files against all 5 of Requirement 5's
   acceptance criteria one at a time (see inline review notes above this
   section for the specific checks). Confirmed `Tile.WithBooster` preserves
   `HasCreditBag` (spawning a booster on a bagged cell doesn't silently
   drop the bag), confirmed `SolarFlare` reads tile colors before any
   clearing mutates the board, confirmed clipping at board edges for
   `TidalClear`/`DeepSurge`/`SporeCloud`. No issues found requiring a code
   change; one interaction (a same-round spawn getting swept by another
   booster's simultaneous effect) noted as acceptable, not a criterion
   violation, so left alone rather than special-cased.

## How Task 6 was verified

1. RED step: added 2 tests to `verify/Program.cs` referencing `CascadeResult.CreditBagsCollected` and asserting that clearing a bagged cell during `ResolveCascade` increments the collected-bag counter and that a 3+ round chain still drops exactly one bonus bag.
2. GREEN step: updated `CascadeEngine.ResolveCascade` to count `HasCreditBag` on each cleared cell before gravity/refill and return it on `CascadeResult`; full suite passed on the first run after the change.
3. Review pass: confirmed the new counter is calculated before `ClearGravityRefill` mutates the board, preserving bag collection semantics even when refill overwrites cleared cells; also confirmed the bonus bag drop only adds a new bag, not replace an already-collected one.

## How Task 7 was verified

1. RED step: added 4 tests to `verify/Program.cs` referencing `LevelObjective`, `LevelStarThresholds`, `LevelProgress`, and `LevelEvaluator` before these types existed. Confirmed the compile failure first, then implemented the minimal level-evaluation API to satisfy the tests.
2. GREEN step: added `Assets/Scripts/Match3/LevelObjective.cs` and the `LevelEvaluator.Evaluate` path, then re-ran the verification harness. The suite passed after the new task tests were added.
3. Review pass: verified the objective completion logic for all three objective types (`Score`, `Collect`, `ClearBoard`), confirmed star counts respect the thresholds in ascending order, and confirmed credit payout maps exactly to the GDD §6.2 table (20 / 35 / 55 for 1/2/3 stars).

Specific checks:
- `Score` objective completes when `LevelProgress.Score >= Target`, and star rating is computed from the score value.
- `Collect` objective completes when `LevelProgress.Collected >= Target`, and star rating is computed from the collected count.
- `ClearBoard` objective completes only when `LevelProgress.RemainingCount == 0`, with star rating derived from the evaluated performance value.
- An incomplete objective returns zero stars and zero credit payout.

## How Task 8 was verified

1. RED step: added 7 tests to `verify/Program.cs` around `HeartSystem` before the class existed, then confirmed the expected compile failures.
2. GREEN step: implemented `HeartSystem` with immutable state, lose-heart behavior, regeneration scheduling, and time-until-next-heart calculations; the suite passed after the new tests were added.
3. Review pass: confirmed losing a heart below max schedules regeneration, losing a heart at zero stays at zero but still begins the timer, regeneration restores the correct number of hearts based on elapsed intervals, and the next-heart timer clears when max hearts is reached.

Specific checks:
- `HeartSystem.LoseHeart` decrements current hearts and schedules regeneration only when needed.
- `HeartSystem.Regenerate` does nothing before the next-heart timestamp and restores the correct count after the interval.
- Regeneration caps at `MaxHearts` and clears the timer when full.
- `GetTimeUntilNextHeart` returns `TimeSpan.Zero` once the timer is due.

- Chain reaction (criterion 3): a hand-built board with a `BloomBurst` at
  (0,0) and a `LeafWheel` at (0,3) in the same row — clearing (0,0) directly
  (via a synthetic size-1 `MatchGroup`) is asserted to activate `BloomBurst`
  (clearing all of row 0, including (0,3)) *and* the resulting inclusion of
  (0,3) is asserted to then activate `LeafWheel` (clearing all of column 3)
  — proving the fixed-point loop actually re-scans newly-added cells, not
  just the initial set.
- Each of the 6 `BoosterActivation.GetAffectedCells` cases is unit tested in
  isolation against a hand-built board, independent of `CascadeEngine`:
  `BloomBurst` (full row), `LeafWheel` (full column), `TidalClear` (2x2
  clip at a literal board corner, not just an interior 3x3), `SolarFlare`
  (count matches the board's actual same-color tile count, not a fixed
  number), `SporeCloud` (exactly 5 distinct in-bounds cells), `DeepSurge`
  (both bottom rows, full width).
- An end-to-end `ResolveCascade` test (4x4 board, engineered Leaf-x4 row
  match, seed 44) confirms exactly one `LeafWheel` booster tile survives on
  the board once cascading settles, rather than being cleared along with
  the rest of its group.

## Planned RED tests for Task 11 (not yet implemented — no production code exists for these symbols yet)

This is a pre-written test plan, not a verification record — nothing below
has been run. Per this project's TDD process (see the methodology note at
the top of this file), the correct next step is to add these to
`verify/Program.cs` referencing symbols that don't exist yet, confirm each
one fails to compile first, and only then implement. Recorded here ahead of
time so the RED step has a concrete checklist to work from rather than
being improvised.

**Layer 1 — `BoosterActivation.GetAffectedCellsAimed` (pure, isolated from swap/cascade mechanics)**

1. `Aimed_BloomBurst_ClearsTargetRow_NotBoostersOwnRow` — booster physically
   sitting in one row, target in a different row; assert the returned cells
   are all of the *target's* row, and the booster's own row is untouched.
2. `Aimed_LeafWheel_ClearsTargetColumn` — same shape as #1, for the target
   column instead of the booster's own.
3. `Aimed_TidalClear_3x3AroundTarget_ClipsAtEdge` — target placed at a board
   corner; same edge-clipping check as Requirement 5's original `TidalClear`
   test, centered on the target instead of the booster.
4. `Aimed_SolarFlare_ReadsTargetColor_NotBoostersColor` — booster and target
   are different colors; assert the result matches every tile of the
   *target's* color, and explicitly assert no booster-color-only tile is
   incorrectly included.
5. `Aimed_SolarFlare_ReadsColorBeforeAnyMutation` — call the method twice in
   a row with no mutation between calls; assert identical results, guarding
   against an ordering bug where the target cell's color gets cleared before
   it's read.
6. `Aimed_SporeCloud_IgnoresTargetPosition_SameContractAsNormal` — assert
   exactly 5 distinct in-bounds cells, matching the existing non-aimed
   `SporeCloud` test's contract regardless of what target is passed.
7. `Aimed_DeepSurge_IgnoresTargetPosition_SameContractAsNormal` — assert
   both bottom rows, full width, regardless of target.

**Layer 2 — `SwapEngine.TryManualActivationSwap` (orchestration decision, no cascade yet)**

8. `ManualActivation_TwoBloomBurstBoosters_Triggers` — two adjacent
   BloomBurst boosters, swap doesn't incidentally form an ordinary match;
   assert triggered = true. (Also decides a tie-break: since both cells are
   BloomBurst, use the second/target cell's row as the anchor, consistent
   with the aimed convention established elsewhere.)
9. `ManualActivation_MixedBoosterPair_DoesNotTrigger_FallsThrough` — one
   BloomBurst + one non-BloomBurst booster adjacent, no incidental match;
   assert triggered = false, proving Requirement 5c criterion 2's "only two
   BloomBursts" carve-out isn't accidentally generalized to any booster pair.
10. `ManualActivation_BoosterPlusRegularTile_Triggers_AimedAtRegularTile` —
    a booster adjacent to a plain tile, no incidental match; assert
    triggered = true and the cleared-cell set matches
    `GetAffectedCellsAimed` called with the regular tile's position.
11. `ManualActivation_BoosterPlusRegularTile_TriggersEvenWhenSwapWouldAlsoMatch`
    — construct a case where the swap would *also* have formed an ordinary
    match on its own; assert manual activation still fires rather than the
    ordinary-match path taking precedence. This is the precedence test from
    Requirement 5c's precedence note, and the one most likely to be missed
    by an implementation that checks for an ordinary match first.
12. `ManualActivation_TwoRegularTiles_NeverTriggers` — neither tile is a
    booster; assert triggered = false unconditionally, regardless of match
    outcome.
13. `ManualActivation_NonAdjacentBoosterPair_DoesNotTrigger` and
    `ManualActivation_OutOfBoundsBoosterPair_DoesNotTrigger` — two
    BloomBursts that aren't adjacent, and an out-of-bounds coordinate pair;
    assert triggered = false in both cases, proving the combo path respects
    the same bounds/adjacency guards `SwapEngine.TrySwap` already enforces.

**Layer 3 — end-to-end cascade integration**

14. `ManualActivation_ResultFeedsFullCascadeLoop` — trigger a
    BloomBurst+BloomBurst combo via the full path (`TryManualActivationSwap`
    → `CascadeEngine.ResolveCascadeFrom`) on a board engineered so the
    row-clear's gravity/refill creates a second cascading match; assert
    `Rounds >= 2`, proving the manual-activation cleared-cell set genuinely
    feeds the same loop as an ordinary match rather than a separate
    one-shot path.
15. `ManualActivation_CreditBagsOnClearedCellsAreCounted` — place a credit
    bag on a cell the aimed effect will clear; assert
    `CascadeResult.CreditBagsCollected` reflects it, guarding against the
    new entry point accidentally bypassing Task 6's bag-counting logic.
16. `OriginalGetAffectedCells_Unchanged_RegressionGuard` — re-run the 6
    existing per-booster `GetAffectedCells` isolation tests from Task 5
    unmodified. Not new behavior; re-asserting them in this same batch is
    the proof that adding the `GetAffectedCellsAimed` overload didn't
    disturb the original, already-verified path.

