# Design — Core Puzzle (Match-3 Board)

Implements: `requirements.md` (Requirements 1–2 now; 3–4 stubbed for later
tasks). Maps to GDD §11.1 folder layout and §11.2 class table.

## 0. Target engine (tech notes)

The GDD specifies "Unity 2023 LTS." **That version line no longer exists** —
Unity's release model changed and went straight from the 2022 LTS line to
Unity 6; there is no "2023 LTS." The current equivalent recommendation is
**Unity 6.3 LTS** (2-year support window, through December 2027). This is a
correction to an outdated fact in the source GDD, not a design choice — the
GDD's actual intent ("use the current LTS release for stability") is
unaffected, only the specific version number was stale.

Practical implication: nothing in `Assets/Scripts/Match3/*.cs` depends on
this either way (it's plain C#, zero `UnityEngine` references — see §1
below), so this only matters once Task 9's `BoardController` and the Unity
project itself are opened in the actual Editor. Install via Unity Hub,
selecting Unity 6.3 LTS, not a bare/standalone installer, so version
upgrades stay manageable.

## 1. Layering decision

GDD §11.2 lists `BoardController` and `MatchFinder` as if they were one
Unity-coupled layer. We split that into two layers instead:

- **`IslandQuest.Match3` (engine-agnostic core)** — plain C#, zero
  `UnityEngine` references. Lives at `Assets/Scripts/Match3/*.cs`. Holds all
  board state and rules: generation, match detection, (later) swap/cascade/
  booster/credit logic. Unity will compile these files as ordinary scripts
  with no extra setup — nothing below depends on MonoBehaviour.
- **`BoardController` (Unity presentation, later task)** — a MonoBehaviour
  that owns input (drag detection), tile prefab instantiation/animation, and
  forwards player intent into the core layer's API. It reads results back to
  decide what to animate. No game *rules* live here, only presentation.

Rationale: the GDD's win condition for M1 ("Working Match-3 board") is rules
correctness, and rules correctness is exactly what's cheap to get wrong and
expensive to debug inside the Unity Editor. Keeping rules engine-agnostic
means they can be compiled and unit-verified with a plain `dotnet` toolchain
in CI, independent of an Editor license/GPU, and the same files drop straight
into the Unity project with no porting step later.

## 2. Data model

```
TileType (enum)      Flower | Leaf | Wave | Sun | Mushroom | Coral
                      — GDD §7.2, one per nature/survival theme.

BoosterType (enum)   None | BloomBurst | LeafWheel | TidalClear
                      | SolarFlare | SporeCloud | DeepSurge
                      — GDD §7.2 "Booster on 4+ match" column.
                      Defined now (Task 1) so Tile's shape is stable;
                      booster *spawn/activation logic* is Task 5.

MatchGroup (class, Task 2)
  TileType Type
  IReadOnlyCollection<(int Row,int Col)> Cells
  int Size => Cells.Count
  bool IsBoosterEligible => Size >= 4         — GDD §7.2 exact threshold
  BoosterType AwardedBooster                  — via BoosterRules.ForTileType

BoosterRules (static class, Task 2)
  One color -> one booster, per the GDD §7.2 table verbatim. Correction vs.
  an earlier draft of this document: eligibility depends only on match
  *size* and *color*, not shape. The GDD does not distinguish a straight
  run-of-4 from an L/T merge of two runs that happens to total 4+ cells —
  there is one booster per color, full stop. (An earlier version of this
  design doc speculated shape mattered, by analogy to other match-3 games;
  that wasn't grounded in this GDD and has been removed.)

Tile (readonly struct)
  TileType Type
  BoosterType Booster   (defaults to None until Task 5 wires it up)
  bool HasCreditBag

BoardConfig (class, immutable after construction)
  int Rows = 9, int Columns = 9        — GDD §7.1
  TileType[] AllowedTileTypes          — defaults to all 6
  int? Seed                            — null = nondeterministic
  int MinInitialCreditBags = 1
  int MaxInitialCreditBags = 2         — GDD §7.1 "approx. 1-2 per level"

Board (class)
  Tile[,] grid, indexer Board[row, col]
  Rows, Columns, InBounds(r,c), Clone()
```

`Tile` is a struct (value type) so swapping/cloning never aliases shared
mutable state between board snapshots — important once Task 3's
swap-then-validate-then-revert flow needs a cheap "undo."

## 3. Algorithms

### 3.1 Match detection (`MatchFinder.FindMatchedCells`)

Single linear sweep per axis using a run-length scan with a sentinel index
(`c <= Columns`, `r <= Rows`) so the final run in each row/column flushes
without a separate end-of-loop special case:

```
for each row:
  runStart = 0
  for c in 1..Columns inclusive:
    sameAsPrev = (c < Columns) && type[r,c] == type[r,c-1]
    if not sameAsPrev:
      if (c - runStart) >= 3: mark cells [runStart, c) as matched
      runStart = c
(mirrored for columns)
```

Cost: O(Rows×Columns), single pass per axis, no allocations beyond the result
set. Cells in both a horizontal and vertical run land in the same `HashSet`,
satisfying Requirement 2.3 (no duplicates) for free.

This becomes the single source of truth every other rule calls: generation
calls it to verify "no pre-existing match" (Req 1.3) and to test legal moves
(Req 1.4); the future swap/cascade code (Task 3–4) calls it again rather than
re-implementing detection.

### 3.1b Grouping matched cells into MatchGroups (`MatchResolver`, Task 2)

`MatchFinder` returns a flat set of matched cells with no notion of which
cells belong to the same physical blob — a board can have two unrelated
matches of the same color in the same scan, and a flat set can't tell them
apart. `MatchResolver.FindMatchGroups` runs a 4-directional flood fill
restricted to cells that are (a) in the matched-cells set and (b) the same
`TileType`, turning the flat set into connected components. An L/T-shaped
overlap (a horizontal and vertical run sharing a corner cell, same color)
correctly merges into **one** `MatchGroup`, since that's one connected blob —
this matters because `IsBoosterEligible` is a per-group threshold (`Size >=
4`), and double-counting a merged blob as two separate groups would award two
boosters where the GDD's table implies one.

### 3.2 Board generation (`BoardGenerator.Generate`)

Two steps, chosen specifically to avoid the more common but slower
"randomize, then rescan and patch" approach:

**Step A — constructive matchless fill.** Fill row-major, left-to-right,
top-to-bottom. At each cell, start from the full allowed-type list and remove:
- the type that would extend a horizontal run (when the two tiles
  immediately to the left already match each other), and
- the type that would extend a vertical run (when the two tiles immediately
  above already match each other).

With `AllowedTileTypes.Length >= 3` (enforced by `BoardConfig`'s constructor),
at most 2 types get removed, so a legal choice always remains. By induction —
each cell is placed only after confirming no run of 3 exists in any
already-placed cell — the **entire finished board is provably matchless** by
construction. No rescan-and-fix pass, no rejection loop, no edge case where
generation has to retry because of bad luck. (A unit test below confirms this
empirically across many seeds and board sizes as a guard against a future
refactor breaking the invariant.)

**Step B — guarantee a legal move exists.** Constructive fill doesn't promise
the player *can* do anything — it's plausible, if rare, for a freshly
generated 9×9 board to have zero adjacent swaps that create a match.
`HasLegalMove` checks every adjacent pair (swap → check `MatchFinder` →
swap back) and `Generate` retries (regenerate from scratch, new RNG state)
up to 25 times if it's ever false. On a 9×9 grid with 6 tile types the
probability of needing more than one attempt is negligible; the retry exists
as a correctness guarantee, not a tuning knob, and a degenerate `BoardConfig`
(e.g. exactly 3 allowed types on a tiny grid) that genuinely can't satisfy
both invariants will exhaust attempts and throw `InvalidOperationException`
with a message pointing at the config, rather than silently handing the
player an unsolvable board.

**Step C — place credit bags.** After the board is finalized, pick a random
count in `[MinInitialCreditBags, MaxInitialCreditBags]` and flag that many
distinct random cells with `HasCreditBag = true`. Done last, after the board
shape is locked in, so it never interacts with the matchless-fill invariant.

### 3.3 Determinism

All randomness flows through one `System.Random` instance seeded from
`BoardConfig.Seed` (or unseeded/time-based if null). No other source of
randomness exists in this module, so a given seed reproduces an identical
board — needed for unit tests, and useful later for "replay this level"
debugging or fairness audits.

### 3.4 Cascade resolution (`CascadeEngine`, Task 4)

Implements Requirement 4a. Row index convention (not previously stated
explicitly, made explicit here since gravity needs a direction): **row 0 is
the top of the board, increasing row index moves down** — consistent with
`BoardGenerator`'s row-major top-to-bottom fill order.

```
ResolveCascade(board, config, rng):
  rounds = []
  loop:
    groups = MatchResolver.FindMatchGroups(board)
    if groups is empty: break
    clear every cell in every group (mark empty)
    for each column:
      compact surviving (non-empty) cells downward, preserving relative order
      fill newly-emptied cells at the top with Random.Choice(config.AllowedTileTypes)
    rounds.append(groups)
  if rounds.Count >= 3:
    bonusCredits = 10
    drop 1 new credit bag on a random non-bag cell
  else:
    bonusCredits = 0
  return CascadeResult(rounds.Count, bonusCredits, rounds)
```

Two deliberate non-choices, both corrected before implementation (see
tasks.md Task 4's correction note for the first):

- **Refill is plain uniform-random, not matchless-fill.** A refill that
  happens to create a match is the mechanism by which a cascade chain
  continues -- the loop's next iteration catches it via the same
  `MatchResolver` call everything else uses. Forcing matchless refill would
  make multi-round cascades rare-to-impossible, contradicting the GDD's
  documented combo system (§6.2, §5.1, §11.2) existing at all.
- **An explicit round cap, not an unbounded loop.** Unlike initial
  generation (which guarantees a result algorithmically and retries from
  scratch up to a cap), a cascade is open-ended by nature -- there's no
  "scratch" to retry from mid-game. `ResolveCascade` takes a generous
  `maxRounds` safety cap (50) and throws if exceeded, on the premise that
  hitting it indicates a bug (e.g. a refill step that isn't actually
  shrinking the empty-cell count) rather than a legitimate player cascade --
  50 rounds from one swap is not a real scenario with 6 tile types on a 9x9
  board.

### 3.5 Booster spawn & activation (`BoosterActivation`, `CascadeEngine.DetermineClearedCells`, Task 5)

Implements Requirement 5. Two new pieces, each independently testable:

- **`BoosterActivation.GetAffectedCells(board, row, col, rng)`** — pure
  (read-only) function mapping a booster tile's `BoosterType` + position to
  the extra cells its effect designates, per the GDD §7.2 table in
  requirements.md. No board mutation, so every one of the 6 cases is unit
  tested directly against a hand-built board with no `CascadeEngine`
  involvement at all.
- **`CascadeEngine.DetermineClearedCells(board, groups, rng)`** — extracted
  from `ResolveCascade`'s inline set-building (same pattern as Task 4
  extracting `ClearGravityRefill` for independent testability). For each
  `MatchGroup`: if booster-eligible, mutate the board so its spawn cell
  (topmost, then leftmost — see requirements.md's interpretation note) picks
  up `AwardedBooster` and is excluded from the cleared set; every other
  group cell clears normally. Then it repeatedly scans the accumulated
  cleared set for any cell that is itself a booster tile not yet processed,
  and unions in `BoosterActivation.GetAffectedCells` for it — a fixed-point
  loop so a chain of boosters clearing other boosters (Requirement 5
  criterion 3) resolves correctly, however deep. `ResolveCascade` calls this
  once per round in place of its old flat union-of-cells step; nothing else
  about the round loop changes.

**Known edge case, not specially handled:** if a booster-eligible group's
chosen spawn cell (topmost-leftmost) happens to already hold a *different*
pre-existing booster tile, that old booster is silently overwritten rather
than activated. This requires two boosters to end up 4-directionally
adjacent/connected in the same color group, which is rare, and the fix
(prefer a non-booster cell as the spawn point, if the group has one) is
straightforward if it turns out to matter — flagging here rather than
building it speculatively, per the project's stated style of not solving
problems that can't yet be observed to occur.

### 3.6 Manual booster activation via swap (Requirement 5c, Task 11)

#### Why this needs a new code path, not a modification to existing ones

Three pieces of already-verified code exist that this feature builds on top
of, and each gets extended via a **new, additive symbol** rather than a
signature change — consistent with how Task 6 extended Task 4's
`CascadeEngine` without touching `ClearGravityRefill` itself:

- **`BoosterActivation.GetAffectedCells`** (Requirement 5) always reads the
  booster's own position and color. `GetAffectedCellsAimed` is a new
  overload taking an explicit target position (and, for `SolarFlare`, an
  explicit target color) instead of deriving both from the booster tile
  itself. The original method's 6 per-booster tests (Task 5) stay
  completely untouched and keep passing against the original signature.
- **`SwapEngine.TrySwap`** (Task 3) only ever asks "does this swap create an
  ordinary match?" `TryManualActivationSwap` is a **separate, new method**
  checking the two Requirement 5c conditions (two BloomBursts; booster +
  non-booster). It does not replace or wrap `TrySwap` — see the composition
  note below for how a caller uses both together.
- **`CascadeEngine.ResolveCascade`** (Task 4) always *discovers* its initial
  cleared-cell set by calling `MatchResolver.FindMatchGroups` itself.
  `ResolveCascadeFrom` is a new entry point that accepts an
  already-determined cleared-cell set as a parameter instead, then runs the
  exact same gravity/refill/re-scan loop. This lets a manually-triggered
  booster effect (which isn't a `MatchGroup` at all — it's a direct effect
  lookup) feed into the identical cascade machinery an ordinary match uses,
  rather than duplicating the loop.

#### Composition: how a caller actually uses these together

This is a decision this document should make explicit, since Requirement
5c's "bypasses `SwapEngine`'s normal rule" wording doesn't by itself say
*where* that bypass is decided. The intended flow, for `BoardController` (or
any future caller) to follow:

1. Call `SwapEngine.TryManualActivationSwap` first.
2. If it reports "triggered," it has already committed the swap and
   returned the resulting cleared-cell set — feed that directly into
   `CascadeEngine.ResolveCascadeFrom`. Do not also call `TrySwap`.
3. If it reports "not triggered" (neither Requirement 5c condition applied),
   fall through to the existing `SwapEngine.TrySwap` exactly as before —
   ordinary match-or-revert behavior, unchanged.

This ordering is what satisfies the precedence note in Requirement 5c
criterion 3 (manual activation fires *regardless* of whether the swap would
also form an ordinary match) — checking manual-activation conditions first,
unconditionally, rather than as a fallback after an ordinary-match check
fails.

#### The BloomBurst+BloomBurst tie-break

When both swapped tiles are BloomBurst boosters, "whose row clears" is
genuinely ambiguous — both are the same booster type. This spec picks the
**second/target cell's row** (i.e. whichever tile the player dragged *onto*,
consistent with the same target-position convention `GetAffectedCellsAimed`
already establishes for the booster+regular-tile case) — deterministic,
not GDD-derived (the GDD doesn't address this at all), and worth revisiting
if playtesting shows the other tile's row would feel more intuitive.

#### Consuming the fired booster (added during implementation)

`GetAffectedCellsAimed` returns only the cells the *effect* designates. For
row/column/3×3 aims the fired booster lands on the target cell, which is
inside its own effect, so it clears as a byproduct. But an aimed
**SolarFlare** clears the *target's* color, which generally isn't the
booster's own color (Sun) — so the spent booster tile would neither be in
the cleared set nor removed, and worse, `ResolveCascadeFrom`'s chain
expansion would see it still sitting there as a live booster and re-fire it
against its *own* color, directly contradicting criterion 3's "not the
booster's own color." `TryManualActivationSwap` therefore **consumes** the
fired booster after computing the aimed cells: it drops that cell's booster
flag (so chain expansion can't re-activate it) and adds the cell to the
cleared set (so a spent booster always leaves the board, regardless of
effect type). This wasn't in the original write-up of this section; it was
found in the Task 11 review pass and is recorded in the correction log
below.

## 4. What's explicitly deferred (and why it's safe to defer)

- **Swap commit/revert (Req 3), cascades, boosters, credit collection,
  star-rating payout, lives** — each needs `MatchFinder` and `Board` to exist
  first; building them now would mean designing the swap API against an
  unstable foundation. `tasks.md` sequences these as Tasks 3–8.
- **Unity `BoardController` MonoBehaviour** — intentionally last (Task 9),
  once the rules it wraps can't change underneath it.

## 5. Test strategy

No Unity Editor and no NuGet access exist in this environment (NuGet restore
is blocked by network policy — confirmed before writing any code), so this
spec uses a **dependency-free verification program** instead of xUnit/NUnit:
plain `Main()` assertions, compiled and run via `dotnet run`, that include the
exact same `.cs` files Unity will later compile (via `<Compile Include>` in
the verify project, not copies — one source of truth). This is a deliberate,
documented substitution for a conventional test framework, not a shortcut:
every assertion still really compiles and really executes.

## 6. Level catalog (Requirement 6, Task 12)

The GDD (§12.1) fixes the M1 content target at "30 levels" but doesn't
prescribe how they're grouped, difficulty numbers, or objective mix — those
are design choices, recorded here.

**Structure (GDD §8.2).** The GDD's island map assigns **Island 1 ("Coconut
Isle") to Levels 1–30** — so the whole M1 catalog is one island, with
`LevelNumber` running 1–30 globally. (Islands 2–3, Levels 31–70 / 71–120, are
later milestones and aren't authored here.) `LevelData.AllLevels` is the
single source of truth (all 30, in level order); `Island1Levels` (the whole
catalog) and `IslandLevels(n)` are views over it. `LevelCount` = 30.

**Objective rhythm.** Score → Collect → Score → Collect → ClearBoard,
repeating every 5 levels, so each block ends on a board-clear and alternates
the two metered objective types across the 30. (The objective *types*
themselves — Score/Collect/ClearBoard — are an implementation-era invention,
not GDD-specified; see the note below.)

**Difficulty levers, and how they ramp.** Nothing here is GDD-derived; the
goal is a monotone curve across Levels 1–30 that the verify suite guards
(`Task12: difficulty ramps across the 30 levels`):

- *Score targets* climb level-over-level, 500 → 4800 (500, 850, 1000, …, 4800).
- *Collect targets* likewise, 8 → 40.
- *Move limits* generally tighten toward Level 30 (down to 14) — less slack to
  hit a rising target.
- *Allowed tile types*: the earliest levels sometimes drop to 4–5 types
  (fewer types → more incidental matches → gentler); from the mid-teens on,
  all six are used throughout. More colors means each specific match is
  rarer, so the same target is harder to reach.

**Objective semantics are still open.** The GDD (reviewed from
`docs/gdd/`) frames each level around a single generic "level objective"
measured as a *percentage* (§7.3), with 1/2/3-star tiers paying 20/35/55
credits (§6.2). It does **not** define a points-per-tile scoring formula,
nor enumerate objective types, nor a board-clear goal — so how `Score` is
actually scored during play, what `Collect` counts, and what `ClearBoard`
requires are all still undecided (the board refills forever, so `ClearBoard`
can't mean an empty board). Task 12 only fixes the *catalog data*; making
these objectives actually playable is the follow-on work.

Credit-bag counts stay at the GDD §7.1 default (1–2 per level) across the
catalog; they're a reward knob, not a difficulty one, so they weren't used
to shape the curve.

**Not built here (deferred, not forgotten).** Per-level *star thresholds*
(`LevelStarThresholds`) still aren't part of `LevelData` — same as Task 10.
Star rating currently needs thresholds supplied at evaluation time; wiring a
per-level threshold set into the catalog is a small follow-up once a
level-select/results UI needs it. Likewise, no per-level ScriptableObject
`.asset` files are authored — the canonical catalog is the C# `AllLevels`
data; `LevelDataAsset` remains available for hand-authored Editor overrides.

### Design correction log

- Booster eligibility (§2): corrected from a speculative shape-based theory
  to the GDD's actual color+size rule, before Task 2 was implemented.
- Cascade refill (§3.4): corrected from a speculative matchless-refill rule
  (which would have suppressed real cascades) to plain random refill, before
  Task 4 was implemented.
- Level catalog structure (§6): Task 12 first grouped the 30 levels as 6
  islands × 5 (a guess, since the GDD text then in-repo didn't cover
  grouping). When the primary-source GDD was added to `docs/gdd/`, §8.2 turned
  out to place **Island 1 at Levels 1–30** (Islands 2–3 are Levels 31–70 /
  71–120, later milestones). Restructured the catalog to a single Island 1
  with `LevelNumber` 1–30, and updated the tests, before this branch merged.
- Manual activation, spent booster (§3.6): the original §3.6 write-up
  described `GetAffectedCellsAimed` and the composition flow but didn't say
  what happens to the *fired* booster tile. During Task 11's review pass an
  aimed SolarFlare was found to leave its spent booster on the board and
  (via chain expansion) re-fire it against its own color — a criterion-3
  violation. Added the "consume the fired booster" rule (drop the booster
  flag + add its cell to the cleared set) in `SwapEngine.
  TryManualActivationSwap`, with a dedicated regression test. Caught during
  Task 11, before marking it done.
