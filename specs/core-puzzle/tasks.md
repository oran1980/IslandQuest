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

- [ ] **5. Booster spawn & activation**
  - 4+ match → spawn the corresponding `BoosterType` (GDD §7.2 table).
  - Activation effects: row clear, column clear, 3×3 zone, all-of-one-color,
    5 random tiles, bottom two rows.

- [ ] **6. Credit bag collection & mid-level spawns**
  - Collecting a flagged tile adds to the run's credit total; extra bags can
    drop mid-level during big combo chains (GDD §7.1).

- [ ] **7. Level objective, completion, and star rating**
  - Objective types (score / collect-N / clear-board), 1/2/3-star thresholds,
    credit payout per GDD §6.2 (20 / 35 / 55 credits).

- [ ] **8. Lives (Hearts) system**
  - Max 5 hearts, lose 1 on level fail, regen 1 per 30 minutes (GDD §7.4),
    persistence hook (actual save/load is a `Core/SaveSystem` concern, out of
    scope here — this task only owns heart-count business logic).

- [ ] **9. Unity presentation layer — `BoardController`**
  - MonoBehaviour wrapping the now-stable core: tile prefab spawn/animate,
    drag input → `Board.TrySwap`, visual cascade playback.
  - First task that's allowed to reference `UnityEngine`.

- [ ] **10. `ScriptableObject` level data for Island 1, Levels 1–5**
  - `LevelData` asset definitions (objective, move limit, allowed tile
    types) so Tasks 7 and 9 have real content to run against, not just
    synthetic test boards.

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
