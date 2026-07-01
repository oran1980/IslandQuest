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

## Requirement 4b — Boosters, Credit Wallet, Objectives, Lives *(future tasks)*

Booster activation effects (GDD §7.2), credit bag collection into an actual
player balance, level objective/star-rating payout (GDD §6.2), and the
Lives/Hearts system (GDD §7.4) are specified in `tasks.md` as Tasks 5–8 and
will get their acceptance criteria added to this document when each task
starts.
