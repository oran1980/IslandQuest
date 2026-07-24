# Requirements — Core Puzzle (Match-3 Board)

Source: IslandQuest GDD v2.0 — §6.2 (Credit Sources), §7 (Puzzle Mechanics),
§7.4 (Lives), §11 (Technical Architecture).

Scope: this spec covers **Milestone 1 — Core Puzzle** only (GDD §12.1: "Working
Match-3 board, 30 levels, lives system, green credit bag drops"). Story/Night
mode, IAP, ads, DifficultyAI near-miss tuning, and survival tip delivery are
out of scope here and will get their own specs once this foundation exists.

Each requirement below is written as a user story with EARS-style acceptance
criteria (WHEN/IF ... THE SYSTEM SHALL ...) so later tasks and tests can trace
back to a specific line.

---

## Requirement 1 — Board Initialization

**User story:** As a player starting a level, I want a 9×9 board filled with
the six nature-themed tile types and no tiles already lined up, so that the
puzzle starts fairly and every match I see is something I made.

**Acceptance criteria**
1. WHEN a level is generated THE SYSTEM SHALL create a grid of `BoardConfig.Rows`
   × `BoardConfig.Columns` cells (default 9×9, GDD §7.1).
2. WHEN populating the grid THE SYSTEM SHALL only use tile types present in
   `BoardConfig.AllowedTileTypes` (default: Flower, Leaf, Wave, Sun, Mushroom,
   Coral — GDD §7.2).
3. WHEN the board finishes generating THE SYSTEM SHALL contain zero existing
   runs of 3+ identical adjacent tiles, horizontally or vertically.
4. WHEN the board finishes generating THE SYSTEM SHALL guarantee at least one
   adjacent tile swap exists that produces a match, so the player is never
   handed a dead board.
5. IF a `Seed` is supplied in `BoardConfig` THEN THE SYSTEM SHALL produce an
   identical board every time for that seed (determinism for QA/tests/replays).
6. WHEN the board finishes generating THE SYSTEM SHALL place 1–2 green credit
   bags on random tiles (GDD §7.1: "approx. 1–2 per level"), configurable via
   `BoardConfig.MinInitialCreditBags` / `MaxInitialCreditBags`.

## Requirement 2 — Match Detection

**User story:** As the game engine, I need to reliably detect every run of 3+
same-type tiles on the board so that swaps, cascades, and generation can all
rely on one correct source of truth.

**Acceptance criteria**
1. WHEN scanning the board THE SYSTEM SHALL identify every maximal horizontal
   run of 3+ identical tile types.
2. WHEN scanning the board THE SYSTEM SHALL identify every maximal vertical
   run of 3+ identical tile types.
3. WHEN a tile belongs to both a horizontal and a vertical run (an "L" or "T"
   shape) THE SYSTEM SHALL include it exactly once in the result set, not
   duplicated.
4. WHEN no run of 3+ exists anywhere on the board THE SYSTEM SHALL report an
   empty result with no false positives.

## Requirement 2b — Match Grouping & Booster Eligibility

**User story:** As the game engine, I need to know which matched cells form
one connected blob (not just "this cell is matched, somehow"), and whether
that blob is big enough to earn a booster, so Task 5 has a clean signal to
act on.

**Acceptance criteria**
1. WHEN matched cells are 4-directionally adjacent and share the same tile
   type THE SYSTEM SHALL group them into a single `MatchGroup`.
2. WHEN two matched regions of the same tile type do not touch THE SYSTEM
   SHALL keep them as separate `MatchGroup`s.
3. WHEN a `MatchGroup` contains 4 or more cells THE SYSTEM SHALL mark it
   booster-eligible and resolve the specific `BoosterType` from the GDD §7.2
   color table (one booster per color; shape — straight run vs. an L/T merge
   of two runs — has no bearing on eligibility or which booster is awarded,
   per the GDD).
4. WHEN a `MatchGroup` contains exactly 3 cells THE SYSTEM SHALL mark it not
   booster-eligible.

## Requirement 3 — Tile Swap Validation

**User story:** As a player, I want to drag one tile onto an adjacent tile and
have it actually swap only if doing so creates a match, so invalid moves don't
waste my attention.

**Acceptance criteria**
1. IF either position is out of bounds THEN THE SYSTEM SHALL reject the swap
   without throwing and without modifying the board.
2. IF the two tiles are not adjacent (sharing an edge) THEN THE SYSTEM SHALL
   reject the swap without modifying the board.
3. WHEN a swap of two adjacent tiles would create at least one match THE
   SYSTEM SHALL commit the swap and report the resulting `MatchGroup`s.
4. IF a swap of two adjacent tiles would create no match THEN THE SYSTEM
   SHALL revert the board to its exact prior state.

## Requirement 4a — Gravity, Refill, and the Cascade/Combo Loop

**User story:** As a player, after my swap clears a match, I want the board
above to fall into place and refill automatically, and — if that sets off a
chain reaction — I want to be rewarded for it, so cascades feel alive and
combo skill is recognized.

**Acceptance criteria**
1. WHEN one or more `MatchGroup`s are cleared THE SYSTEM SHALL shift every
   remaining tile in each affected column downward to fill the gap, in
   relative order (no reordering of surviving tiles within a column).
2. WHEN gravity leaves empty cells at the top of a column THE SYSTEM SHALL
   fill them with new tiles drawn from `BoardConfig.AllowedTileTypes`.
3. WHEN a refill produces a new match (by coincidence or by design — see
   tasks.md Task 4's correction note) THE SYSTEM SHALL detect it on the next
   scan and resolve it the same way as any other match, repeating
   clear/gravity/refill until a scan finds zero matches.
4. WHEN a full resolution (one swap and everything it triggers) completes
   THE SYSTEM SHALL report the number of cascade rounds it took.
5. IF a resolution takes 3 or more cascade rounds THEN THE SYSTEM SHALL
   report 10 bonus credits earned and place exactly one additional credit
   bag on the board (GDD §6.2), without modifying any persistent player
   balance (out of scope here — see Task 7).
6. IF a resolution takes fewer than 3 cascade rounds THEN THE SYSTEM SHALL
   report 0 bonus credits and place no additional bag.
7. WHEN resolution finishes THE SYSTEM SHALL leave no empty cells on the
   board (every column fully refilled).

## Requirement 5 — Booster Spawn & Activation

**User story:** As a player, when I match 4 or more tiles of one color, I want
a booster tile to appear that can later clear a whole row, column, zone, or
color, so bigger matches feel more rewarding and give me a tool for tough
boards.

Source for the color -> effect table: GDD §7.2 "Tile Elements & Boosters"
(re-supplied by the user in-conversation since the .docx isn't in this repo;
transcribed verbatim below).

| Element  | Booster      | Effect                            |
|----------|--------------|------------------------------------|
| Flower   | BloomBurst   | Clears entire row                  |
| Leaf     | LeafWheel    | Clears full column                 |
| Wave     | TidalClear   | Removes 3x3 zone                   |
| Sun      | SolarFlare   | Removes all tiles of one color     |
| Mushroom | SporeCloud   | Removes 5 random tiles             |
| Coral    | DeepSurge    | Clears bottom two rows             |

**Acceptance criteria**
1. WHEN a `MatchGroup` is booster-eligible (`Size >= 4`) THE SYSTEM SHALL
   leave exactly one of its cells on the board as a booster tile (same
   `TileType`, `Booster` set to `MatchGroup.AwardedBooster`) instead of
   clearing it, and clear every other cell in the group normally in the same
   round.
2. WHEN a booster tile is included in the set of cells cleared during any
   cascade round (matched again later, whether by a normal match or as part
   of another booster-eligible group) THE SYSTEM SHALL, in that same round,
   also clear the cells its effect designates per the table above:
   `BloomBurst` -> the booster's entire row; `LeafWheel` -> the booster's
   entire column; `TidalClear` -> the 3x3 zone centered on the booster,
   clipped at board edges; `SolarFlare` -> every board cell whose `TileType`
   equals the booster's own type; `SporeCloud` -> 5 additional cells chosen
   at random from anywhere on the board; `DeepSurge` -> every cell in the
   bottom two rows of the board.
3. WHEN a booster's activation clears a cell that itself holds another
   booster tile THE SYSTEM SHALL activate that booster too, continuing until
   no newly-added cell in that round's cleared set is an unactivated booster
   (chain reaction, still counted as one cascade round).
4. IF a booster's designated cells exceed what the board actually has (e.g.
   `TidalClear` near a corner, `SolarFlare` when few tiles share that color)
   THEN THE SYSTEM SHALL clip/clear only cells that exist, without error.
5. Booster activation only expands which cells are cleared within the
   current cascade round; it does not by itself add an extra round to
   `CascadeResult.Rounds` — gravity/refill and any resulting new matches are
   still handled by the existing Requirement 4a loop.

**Interpretation notes** (not literal GDD text — flagged per this project's
convention of documenting non-obvious calls rather than silently guessing):
- Which cell in a booster-eligible group keeps the booster ("spawn cell")
  isn't specified by the GDD. This spec picks the topmost, then leftmost
  cell in the group — deterministic, independent of RNG or `HashSet`
  iteration order.
- "Activation" triggers when the booster tile is cleared again later (as
  part of any match), not immediately upon spawn — standard genre
  convention, and consistent with the GDD listing "spawn" and "activation
  effects" as two separate ideas.
- `SolarFlare`'s "one color" is read as the booster's own `TileType` (the
  color it formed from); nothing in the GDD suggests targeting a different
  color.

## Requirement 5c — Manual Booster Activation via Swap

**User story:** As a player, I want to deliberately swap a booster tile
instead of only having it activate by chance during a cascade, so boosters
become a tool I can use intentionally, not just a byproduct of luck.

Two distinct new swap outcomes, both bypassing `SwapEngine`'s normal
"only commit if it creates a match" rule for these specific tile
combinations. Scope decisions below were confirmed with the user directly
(not GDD-derived, since the GDD doesn't address manual activation at all)
and are recorded here rather than left implicit.

**Acceptance criteria**

1. WHEN a player swaps two adjacent tiles that are **both BloomBurst
   boosters** THE SYSTEM SHALL commit the swap and immediately fire
   BloomBurst's row-clear effect that same round, regardless of whether the
   swap would otherwise form a match.
2. IF a player swaps two adjacent booster tiles where **at least one is not
   BloomBurst** (e.g. LeafWheel+LeafWheel, BloomBurst+TidalClear) THEN THE
   SYSTEM SHALL fall through to normal `SwapEngine` rules — commit only if
   the swap happens to form an ordinary match, otherwise revert. Explicitly
   out of scope: no special combo effect for any booster pairing except two
   BloomBursts specifically. (Confirmed with user: other booster-pair combos
   were considered and deliberately excluded, not merely unspecified — may
   get their own requirement later if wanted.)
3. WHEN a player swaps a booster tile with an adjacent **non-booster**
   tile THE SYSTEM SHALL commit the swap and immediately fire that
   booster's own GDD §7.2 effect that same round, "aimed" through the
   non-booster tile's board position instead of the booster's own original
   position, regardless of whether the swap would otherwise form a match
   (this takes precedence over an incidental ordinary match — see the
   precedence note below):
   - `BloomBurst` -> clears the **target tile's row** (not the booster's own
     row)
   - `LeafWheel` -> clears the **target tile's column**
   - `TidalClear` -> 3x3 zone centered on the **target tile's position**,
     clipped at board edges (same clipping rule as Requirement 5's
     non-aimed version)
   - `SolarFlare` -> all tiles matching the **target tile's color** (not the
     booster's own color)
   - `SporeCloud` -> unchanged from its normal effect (5 random tiles,
     board-wide) — the target position has no bearing, since this effect
     was never position-anchored to begin with
   - `DeepSurge` -> unchanged from its normal effect (bottom two rows) —
     this effect is anchored to the board's edge, not any tile's position,
     so "aiming" doesn't apply
4. WHEN either of the above manual-activation swaps triggers a booster
   effect THE SYSTEM SHALL run the resulting cleared cells through the same
   gravity/refill/re-scan cascade loop as any other clear (Requirement 4a),
   including chaining into any further boosters the effect happens to
   sweep up.

**Precedence note:** criterion 3 explicitly fires regardless of whether the
swap would also have formed an ordinary match on its own — manual
activation is not a fallback checked only after an ordinary-match check
fails. An implementation that checks for an ordinary match first and only
falls back to manual activation when no match exists would violate this
criterion; the two BloomBurst-pair and booster+regular-tile checks must run
*before* (or independently of, with equal-or-higher priority than) the
ordinary match check in `SwapEngine`.

**Implementation note for whoever picks this up:** `BoosterActivation.
GetAffectedCells` (Requirement 5) always reads the booster's own position
and color — that behavior is correct and already verified, and should stay
untouched. This requirement needs a second, separate code path (e.g. a
`GetAffectedCellsAimed` overload taking an explicit target position/color)
rather than a signature change, so Requirement 5's existing tests keep
exercising the original, unmodified path.

## Requirement 5b — Credit Wallet, Objectives, Lives *(future tasks)*

Credit bag collection into an actual player balance, level
objective/star-rating payout (GDD §6.2), and the Lives/Hearts system (GDD
§7.4) are specified in `tasks.md` as Tasks 6–8 and will get their acceptance
criteria added to this document when each task starts.

## Requirement 6 — Full Level Catalog (30 levels)

**User story:** As a player, I want a full run of levels to progress through
with a sensible difficulty ramp, so the game has real content rather than a
handful of sample levels.

This is the remaining M1 content target from GDD §12.1 ("Working Match-3
board, **30 levels**, lives system, green credit bag drops"). Task 10
delivered a 5-level sample; this requirement completes the catalog to 30.

**Structure (per GDD §8.2 — corrected):** the GDD's island map puts **Island
1, "Coconut Isle", at Levels 1–30** (Island 2 "Ember Peak" = 31–70, Island 3
"Coral Abyss" = 71–120 — later milestones). So the entire M1 catalog is
**Island 1, Levels numbered 1–30 globally**. (An earlier draft of this
requirement grouped the 30 as 6 islands × 5 — that contradicted GDD §8.2 and
was corrected; see design.md's correction log.)

**Acceptance criteria**

1. THE SYSTEM SHALL expose exactly **30** `LevelData` entries, all in
   **Island 1**, with `LevelNumber` 1–30 in ascending order.
2. Every level SHALL satisfy the same validity invariants Task 10 already
   enforces per level: a positive move limit, at least 3 distinct allowed
   tile types, and `0 ≤ MinInitialCreditBags ≤ MaxInitialCreditBags`.
3. Each `(Island, LevelNumber)` pair SHALL be unique across the catalog.
4. The catalog SHALL ramp in difficulty across the 30 levels: reading levels
   in order, each `Score` target SHALL be ≥ the previous `Score` level's, and
   each `Collect` target ≥ the previous `Collect` level's (a monotone curve;
   move limits generally tighten toward Level 30). These specific numbers are
   a design choice, not GDD-derived, and are documented in design.md §6.
5. The original Task 10 sample SHALL remain the gentle on-ramp: Levels 1–5
   keep their objectives/targets, now as the start of the full island.

## Requirement 7 — Level Play: Scoring, Objectives, Difficulty & Rewards

**User story:** As a player, I want each level to have a clear goal, a fair
number of moves, and a reward that's bigger on harder levels, so finishing a
tough level feels worth it.

**Provenance:** the GDD frames a level around a single generic "objective"
measured as a percentage (§7.3) and pays a flat 1/2/3-star credit reward
(§6.2: 20/35/55). It does **not** define a scoring formula, objective types,
or difficulty-scaled rewards. The rules below were chosen with the product
owner (a Homescapes-style model) and are design decisions, not GDD-derived —
documented in design.md's level-play section.

**Scoring**

1. WHEN tiles are cleared THE SYSTEM SHALL award **10 points per cleared
   tile**, multiplied by the cascade **combo round** (round 1 = ×1, round 2 =
   ×2, …), so deeper cascades score more. A move's score is the sum over its
   cascade rounds.

**Objective types** (each level has exactly one)

2. `Score` — complete WHEN the level's accumulated score ≥ the target.
3. `Collect` — complete WHEN the cumulative count of tiles cleared ≥ the
   target.
4. `CollectBags` — complete WHEN all of the level's green credit bags have
   been collected. The level SHALL seed exactly the target number of bags at
   generation so the objective is always attainable (this replaces the former
   `ClearBoard`, which was unwinnable on an infinitely-refilling board).

**Move limit & loss**

5. A level SHALL end in **loss** IF the move limit is reached before the
   objective is complete, and in **win** as soon as the objective completes.

**Difficulty & reward**

6. Every level SHALL carry a difficulty tier: `Easy`, `Hard`, or `VeryHard`,
   generally rising across Levels 1–30.
7. On a win THE SYSTEM SHALL pay green credits = the star base (GDD §6.2:
   20/35/55 for 1/2/3 stars) × a difficulty multiplier (`Easy` ×1.0, `Hard`
   ×1.5, `VeryHard` ×2.0), rounded. (Multipliers are tunable balance values.)
8. Star count SHALL come from the level's star thresholds against the
   objective's performance value (existing `LevelStarThresholds` /
   `LevelEvaluator`); 1 star means the objective was met, 2–3 mean it was
   exceeded by the level's margins.

*Implementation is sequenced in tasks.md as Tasks 13 (scoring), 14 (difficulty
tiers + reward + objective-type revision), 15 (level session: moves, progress,
win/loss), and 16 (Unity level-select + results UI).*
