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

- [x] **9. Unity presentation layer — `BoardController`**
  - MonoBehaviour wrapping the now-stable core: tile prefab spawn/animate,
    drag input → `SwapEngine.TrySwap`, visual cascade playback. Now also
    routes drag input through `SwapEngine.TryManualActivationSwap` →
    `CascadeEngine.ResolveCascadeFrom` first (Requirement 5c precedence,
    design.md §3.6 composition), falling through to `TrySwap` only when no
    manual activation fires.
  - First task that's allowed to reference `UnityEngine`.
  - _Implementation: `Assets/Scripts/BoardController.cs` and `Assets/Scripts/BoardTileView.cs`._
  - _Scene/prefab wiring (confirmed by reading the YAML): `scene1.unity` has
    a `BoardController` GameObject with `tilePrefab`→`Tile.prefab` and
    `tilesParent` wired (`levelDataAsset` left null → falls back to a default
    `BoardConfig`, which is fine), a `TilesParent`, an orthographic Main
    Camera framed on a 9×9 board (pos 4.4,-4.4,-10; size 5) carrying a
    `Physics2DRaycaster`, and an `EventSystem` with `InputSystemUIInputModule`.
    `Tile.prefab` = `SpriteRenderer` + `BoxCollider2D` (needed for the 2D
    raycaster to hit sprites) + `BoardTileView` (its `_spriteRenderer`/
    `_labelText` wired) + a `TextMesh` label child. So the drag-input path has
    every component it needs._
  - _Verification via the live Editor (the project was open in Unity 6.3 LTS
    while this ran, so a headless batch compile couldn't take the project
    lock; instead the running Editor's `Logs/Editor.log` was read directly):
    scripts **compile cleanly against real `UnityEngine`** — `CompileScripts:
    648ms`, `domain reloads=1`, and zero `error CS####` — which is the one
    thing `verify/` structurally can't check. The Editor then **entered Play
    Mode with the scene loaded** (Unity refuses to enter Play Mode with
    compiler errors) and logged **no runtime exceptions** — no
    `NullReferenceException`, and notably no `UnassignedReferenceException`
    (which would fire if the serialized refs were unwired), so
    `Start()`→`BoardGenerator.Generate()`→`CreateBoardViews()` ran clean. (The
    only "error" lines in the log are unrelated Unity licensing/Connect 401
    noise.)_
  - _Editor playtest — DONE (Unity 6.3 LTS, screenshots captured at each
    step). All four confirmed on screen:_
    1. _**Grid render:** 9×9 of correctly color-coded tiles (M/C/L/F/W/S →
       orange/red/green/pink/blue/yellow), single-letter labels, credit-bag
       `*` suffix shown. Spot-checked matchless at generation (no pre-existing
       3-run), confirming `BoardGenerator`'s invariant holds live._
    2. _**Drag-swap:** swapping `(4,2)`F ↔ `(4,3)`C formed C-C-C at row 4
       cols 0–2; the swap committed (didn't revert) and the trio cleared.
       Verified by before/after diff: only the three cleared columns changed,
       each shifting down exactly one with a fresh top refill; all other cells
       byte-identical._
    3. _**Cascade playback:** same test — gravity dropped survivors and
       refilled the top; board settled matchless again. (A separate 4-match
       later also dropped a bonus credit bag, incidentally confirming the 3+
       combo-bonus path live.)_
    4. _**Manual booster activation (Task 11 path):** a Wave 4-match spawned a
       `TidalClear` booster, rendered unambiguously as `TC` at the spawn cell
       `(6,2)`. Swapping that `TC` with the adjacent non-booster at `(6,3)`
       fired immediately (did NOT revert) and cleared a 3×3 zone centered on
       the target — verified by before/after diff showing **only columns 2–4
       changed while columns 0,1,5,6,7,8 stayed byte-identical** (the unique
       signature of an aimed 3×3, not a row/column/color clear), and the `S*`
       credit bag inside the zone was collected._
  - _One presentation gap found and fixed during the playtest: boosters were
    labelled with a single letter that collided with tile letters (LeafWheel
    vs Leaf, SolarFlare/SporeCloud vs Sun). `BoardTileView.SetTile` now shows a
    distinct two-letter code (BB/LW/TC/SF/SC/DS); confirmed on screen as `TC`._

- [x] **10. `ScriptableObject` level data for Island 1, Levels 1-5**
  - `LevelData` asset definitions (objective, move limit, allowed tile
    types) so Tasks 7 and 9 have real content to run against, not just
    synthetic test boards.
  - _Implementation: `Assets/Scripts/Match3/LevelData.cs` and `Assets/Scripts/LevelDataAsset.cs`._

- [x] **11. Manual booster activation via swap**
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
  - _Verification: implemented and run — see "How Task 11 was verified"
    below. The pre-written plan is preserved under "Planned RED tests for
    Task 11" for the audit trail._
  - _Final symbol names (matched the proposal): `BoosterActivation.
    GetAffectedCellsAimed(board, booster, targetRow, targetCol, targetColor,
    rng)`, `SwapEngine.TryManualActivationSwap(...)` returning a new
    `ManualSwapResult { Triggered, ClearedCells }`, and `CascadeEngine.
    ResolveCascadeFrom(board, initialClearedCells, config, rng)`._

- [x] **12. Full level catalog — Island 1, Levels 1–30**
  - Author the full 30-level `LevelData` catalog the GDD §12.1 M1 scope calls
    for. Per GDD §8.2, all 30 belong to **Island 1 ("Coconut Isle")**, with
    `LevelNumber` 1–30 globally (Task 10's 5 levels are now the on-ramp).
  - Difficulty ramps level-over-level (rising Score/Collect targets, tighter
    move limits toward Level 30, all-6 tile types from the mid-teens on).
    Exact numbers are a design choice — see design.md §6 and Requirement 6.
  - New symbols (data lives in `LevelData.cs`, no `UnityEngine`):
    `LevelData.AllLevels` (all 30), `LevelData.IslandLevels(int island)`,
    `LevelData.LevelCount`, `LevelData.Island1`. `Island1Levels` stays (now
    the whole catalog, derived from `AllLevels`).
  - _Structure correction: first built as 6 islands × 5; corrected to Island 1
    = Levels 1–30 once the GDD (added to `docs/gdd/`) was checked against
    §8.2. See design.md's correction log._
  - _Satisfies: Requirement 6 (all criteria)._
  - _Verification: extends `verify/Program.cs`; see "How Task 12 was
    verified" below._

- [x] **13. Scoring rule** (Requirement 7 crit. 1)
  - `ScoringRules` (10 pts/tile × cascade-round combo multiplier). Surface
    `Score` and `TilesCleared` on `CascadeResult`, accumulated in both
    `ResolveCascade` and `ResolveCascadeFrom`. Engine, verify-testable.
  - _Verification: see "How Task 13 was verified" below._

- [x] **14. Difficulty tiers, reward scaling & objective-type revision**
    (Requirement 7 crit. 2–4, 6–8)
  - Add a `Difficulty` enum (Easy/Hard/VeryHard) to `LevelData`; tag all 30
    levels. Reward = star base (20/35/55) × difficulty multiplier — extend
    `LevelEvaluator`. Revise objective types: keep `Score`/`Collect`
    (Collect = tiles cleared), replace `ClearBoard` with `CollectBags`
    (seed the target bag count). Engine + data, verify-testable.
  - _Verification: see "How Task 14 was verified" below._

- [ ] **15. Level session — moves, progress, win/loss** (Requirement 7 crit. 5)
  - A `LevelSession` that consumes each move's `CascadeResult`, accumulates
    score / tiles-cleared / bags-collected into a `LevelProgress`, counts
    moves against the limit, and reports win (objective met) / loss (out of
    moves) plus the final `LevelResult` (stars + credits). Engine,
    verify-testable — the "missing middle" that makes a level actually
    playable to completion.

- [ ] **16. Unity level-select + results UI**
  - Level-select screen (levels with difficulty labels / star records) →
    load the chosen `LevelData` into `BoardController` + a `LevelSession`;
    a results panel (stars, credits earned) with replay/next. MonoBehaviours
    + scene(s); Editor playtest (like Task 9).

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

## How Task 11 was verified (strict TDD: RED confirmed before any production code)

Implemented against the pre-written plan below (kept intact as the audit
trail). Baseline before starting: 50 passed, 0 failed.

1. RED step: added 17 tests to `verify/Program.cs` (the 16 planned +
   1 review-driven, see step 4) referencing `BoosterActivation.
   GetAffectedCellsAimed`, `SwapEngine.TryManualActivationSwap`/
   `ManualSwapResult`, and `CascadeEngine.ResolveCascadeFrom` — none of which
   existed. Ran and confirmed a compile failure first: `CS0117:
   'BoosterActivation' does not contain a definition for 'GetAffectedCellsAimed'`,
   `CS0117: 'SwapEngine' does not contain a definition for
   'TryManualActivationSwap'`, and `CS0117: 'CascadeEngine' does not contain
   a definition for 'ResolveCascadeFrom'` (plus knock-on `CS0019` on `.Count`
   against the resulting method groups). The build failed, as required.
2. GREEN step:
   - `BoosterActivation`: extracted the existing GDD §7.2 switch into a
     private `ComputeEffect(board, booster, row, col, color, rng)`; the
     original `GetAffectedCells` now delegates to it reading the booster
     tile's own position/color, and the new `GetAffectedCellsAimed` delegates
     to it with an explicit target position + color. The only behavioral
     change to the shared body was reading SolarFlare's color from the
     `color` parameter instead of `tile.Type` — identical for the original
     caller, retargetable for the aimed one.
   - `SwapEngine.TryManualActivationSwap` + `ManualSwapResult`: bounds/
     adjacency guards, then condition 1 (both BloomBurst → aimed row-clear on
     the target cell, tie-break per design.md §3.6) and condition 2 (exactly
     one booster ^ one non-booster → booster's effect aimed through the
     non-booster's position/color). Both commit the swap and never consult
     the ordinary-match check, satisfying Requirement 5c's precedence note.
     Every other combination returns `NotTriggered` without touching the
     board.
   - `CascadeEngine.ResolveCascadeFrom`: extracted the fixed-point booster
     chain-expansion out of `DetermineClearedCells` into a shared private
     `ExpandBoosterChain` (REFACTOR — Task 5/6 tests stayed green, proving
     the extraction was behavior-preserving), then built the new entry point
     to run a provided cleared set through chain-expansion + bag-counting +
     gravity/refill as round 1, then continue the identical loop as
     `ResolveCascade`. An empty initial set degrades to `ResolveCascade`
     behavior.
   - Full suite re-run: 66 passed, 0 failed (no regressions in Tasks 1–10).
3. REVIEW pass — re-read all three changed core files against Requirement
   5c's four criteria one at a time:
   - Criterion 1 (two BloomBursts): covered, incl. the target-row tie-break
     via a vertical-swap test asserting the *source* row is untouched.
   - Criterion 2 (mixed booster pair falls through): covered for both
     BloomBurst+other and other+other booster pairs.
   - Criterion 3 (booster + non-booster, aimed, precedence): covered incl.
     the explicit "swap would also form an ordinary match, manual still
     fires" precedence test.
   - Criterion 4 (feeds the same cascade loop, chains): covered by the
     telescoping end-to-end test (`Rounds >= 2`) and the credit-bag test.
   - Found one real issue and fixed it: an aimed **SolarFlare** on a
     booster+regular swap lands the booster on the target cell, but the aimed
     effect clears the *target* color — which usually isn't the booster's own
     color — so the spent booster tile would have survived on the board, and
     if left as a booster, the chain-expansion would have re-fired it against
     its *own* color (contradicting criterion 3). Fixed by having
     `TryManualActivationSwap` **consume** the fired booster: drop its booster
     flag *and* add its cell to the cleared set, so it always leaves the board
     and never re-chains. Added a dedicated regression test
     ("an aimed SolarFlare is consumed and does not chain into its own
     color") — this is the +1 beyond the 16 planned.
   - Re-ran after the fix: **67 passed, 0 failed.** A second review pass over
     the diff found nothing further.

Below is the original pre-written plan, kept verbatim for the record.

## Planned RED tests for Task 11 (as written before implementation)

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

## How Task 12 was verified (strict TDD: RED confirmed before any production code)

Baseline before starting: 67 passed, 0 failed.

1. RED step: added 6 tests to `verify/Program.cs` referencing
   `LevelData.AllLevels`, `LevelData.IslandLevels(int)`,
   `LevelData.IslandCount`, and `LevelData.LevelsPerIsland` — none of which
   existed. Ran and confirmed a compile failure first: `CS0117: 'LevelData'
   does not contain a definition for 'IslandCount'` (and the same for
   `LevelsPerIsland`, `AllLevels`, `IslandLevels`), plus knock-on `CS0019` on
   `IslandLevels`-as-method-group comparisons. The build failed, as required.
2. GREEN step: refactored `LevelData` so a single `BuildAllLevels()` produces
   all 30 entries (Island 1 preserved byte-for-byte via a compact `Lvl(...)`
   helper), with `IslandCount`/`LevelsPerIsland` constants, an
   `IslandLevels(int)` view, and `Island1Levels` redefined as `IslandLevels(1)`
   (single source of truth). Static-initializer order checked: the tile-type
   sets and `AllLevels` are declared before `Island1Levels`, so the latter's
   initializer sees a populated catalog. Full suite re-run: **73 passed, 0
   failed** — the 6 new tests green and Task 10's `Island1Levels` test still
   green (proving Island 1 is genuinely unchanged, not just re-passing).
3. REVIEW pass — re-read `LevelData.cs` against Requirement 6's five criteria:
   - Count/structure (crit. 1): exactly 30 entries, 6 islands × 5, numbered
     1–5 within each — covered by two tests.
   - Per-level validity (crit. 2): every entry has moveLimit ≥ 14 (>0), ≥ 4
     allowed types (≥ 3), and default bags 1–2 (0 ≤ min ≤ max) — covered.
   - Uniqueness (crit. 3): `(Island, LevelNumber)` HashSet has 30 members.
   - Difficulty ramp (crit. 4): verified the encoded curve — max Score per
     island 850→1300→2000→2800→3800→4800, max Collect 12→16→20→26→32→40, both
     strictly rising; move limits tighten toward Island 6 (14). A test asserts
     the non-decreasing property so a future edit can't silently break it.
   - Island 1 unchanged (crit. 5): confirmed by the retained Task 10 test plus
     a new `Island1Levels`-derives-from-catalog test.
   - Noted two deliberate non-goals (per-level star thresholds; per-level
     `.asset` files) in design.md §6 rather than building them speculatively —
     neither is required by Requirement 6, and both are cheap follow-ups when
     a level-select/results UI needs them.

**Structure correction (after the GDD was added to the repo).** The steps
above built the catalog as **6 islands × 5 levels** — a guess made because the
primary-source GDD wasn't in-repo at the time. Once the GDD was added to
`docs/gdd/`, §8.2 showed **Island 1 spans Levels 1–30** (Islands 2–3 are
Levels 31–70 / 71–120, later milestones). Restructured to a single Island 1
with `LevelNumber` 1–30: replaced `IslandCount`/`LevelsPerIsland` with
`LevelCount = 30` and an `Island1` constant; `AllLevels` now lists 30
`island: 1` entries numbered 1–30 (same objective/target/move values, just
renumbered — the difficulty ramp is now level-over-level instead of
island-over-island). Updated the Task 12 tests (single-island assertions +
a level-order monotonic-ramp test) and the Task 10 test (Island 1 now has 30
levels, so it validates the first 5 rather than asserting the total is 5).
Re-ran: **72 passed, 0 failed.** Requirements 6 and design.md §6 updated; the
contradiction is logged in design.md's correction log.


## How Task 13 was verified (strict TDD: RED confirmed before any production code)

Baseline before starting: 72 passed, 0 failed.

1. RED step: added 3 tests referencing `ScoringRules` and
   `CascadeResult.TilesCleared`/`.Score` — none existed. Confirmed compile
   failure first: `CS0103: The name 'ScoringRules' does not exist` and
   `CS1061: 'CascadeResult' does not contain a definition for 'TilesCleared'`.
2. GREEN step: added `ScoringRules` (pure: `PointsPerTile = 10`, `RoundScore =
   tiles × 10 × comboRound`), added `TilesCleared`/`Score` to `CascadeResult`,
   and accumulated both per round in `ResolveCascade` and `ResolveCascadeFrom`
   (combo round = `rounds + 1`, so round 1 = ×1). The `CascadeResult`
   constructor gained two params; only the two production call sites use it,
   both updated — no test constructs it directly, so nothing else broke.
3. One test bug found and fixed (not a production bug): the cascade test first
   asserted a row-of-4 clears 4 tiles, but a 4-match spawns a booster and keeps
   its spawn cell (Task 5), so it clears 3. Relaxed the floor to 3 and kept the
   meaningful assertion — combo-weighted `Score ≥ 10 × TilesCleared`. Re-ran:
   **75 passed, 0 failed.**
4. REVIEW: booster spawn cells are (correctly) excluded from `TilesCleared`
   and `Score` since they aren't cleared; booster-chain-swept cells are
   included since they are. `ScoringRules` guards non-positive inputs. Prior
   cascade tests (Tasks 4/5/6) stayed green, confirming the added accumulation
   didn't disturb existing behavior.

## How Task 14 was verified (strict TDD: RED confirmed before any production code)

Baseline before starting: 75 passed, 0 failed.

1. RED step: added 6 tests referencing `Difficulty`, `LevelData.Difficulty`,
   `LevelObjectiveType.CollectBags`, and a difficulty-aware `LevelEvaluator.
   Evaluate` overload — none existed. Confirmed compile failure first:
   `CS0103: The name 'Difficulty' does not exist` and `'LevelObjectiveType'
   does not contain a definition for 'CollectBags'`.
2. GREEN step:
   - `LevelObjective.cs`: renamed enum `ClearBoard` → `CollectBags`; simplified
     validation so all three types require a positive target; `IsComplete`/
     `PerformanceValue` for `CollectBags` reuse the old `ClearBoard` behavior
     (`RemainingCount == 0`; performance = score). Added the `Difficulty` enum.
     `LevelEvaluator.Evaluate` gained an optional `Difficulty` param (default
     `Easy`) and scales the GDD §6.2 star base by the difficulty multiplier
     (Easy 1.0 / Hard 1.5 / VeryHard 2.0), rounded away-from-zero.
   - `LevelData.cs`: added a `Difficulty` field/param; the `Lvl` helper now
     takes a difficulty and, for `CollectBags` levels, seeds exactly `target`
     bags (min = max = target) so the objective is attainable. Tagged all 30
     levels Easy 1–10 / Hard 11–20 / VeryHard 21–30, and gave the six
     `CollectBags` levels positive bag targets (3,4,4,5,5,6).
   - `LevelDataAsset.cs`: default `objectiveTarget` 0 → 500 (0 is now invalid),
     added a `difficulty` field.
   - Updated the one existing Task 7 test that used `ClearBoard`/target 0 to
     `CollectBags`/target 3. Full suite: **80 passed, 0 failed** — Tasks 7/10/12
     stayed green, confirming the enum rename + reward change didn't regress.
3. REVIEW: confirmed the difficulty ramp is non-decreasing and uses all three
   tiers; reward rounding matches the documented examples (Hard 2★ = round(35 ×
   1.5) = 53, VeryHard 3★ = 110); `CollectBags` bag seeds fit `BoardConfig`'s
   `max ≤ rows×cols` bound; and the default-`Easy` overload keeps the flat
   20/35/55 payout for callers that don't pass a difficulty.
