using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>Outcome of a <see cref="SwapEngine.TrySwap"/> call.</summary>
    public sealed class SwapResult
    {
        public bool Success { get; }
        public IReadOnlyList<MatchGroup> MatchGroups { get; }

        private SwapResult(bool success, IReadOnlyList<MatchGroup> matchGroups)
        {
            Success = success;
            MatchGroups = matchGroups;
        }

        public static readonly SwapResult Rejected = new SwapResult(false, System.Array.Empty<MatchGroup>());

        public static SwapResult Committed(IReadOnlyList<MatchGroup> matchGroups) => new SwapResult(true, matchGroups);
    }

    /// <summary>Outcome of a <see cref="SwapEngine.TryManualActivationSwap"/> call.</summary>
    public sealed class ManualSwapResult
    {
        /// <summary>True if this swap matched one of Requirement 5c's
        /// manual-activation conditions, committed the swap, and produced a
        /// booster-effect cleared set. False means the caller should fall
        /// through to the ordinary <see cref="SwapEngine.TrySwap"/> path.</summary>
        public bool Triggered { get; }

        /// <summary>The cells the fired booster designates for clearing — feed
        /// directly into <see cref="CascadeEngine.ResolveCascadeFrom"/>. Empty
        /// when <see cref="Triggered"/> is false.</summary>
        public IReadOnlyCollection<(int Row, int Col)> ClearedCells { get; }

        private ManualSwapResult(bool triggered, IReadOnlyCollection<(int Row, int Col)> clearedCells)
        {
            Triggered = triggered;
            ClearedCells = clearedCells;
        }

        public static readonly ManualSwapResult NotTriggered =
            new ManualSwapResult(false, System.Array.Empty<(int Row, int Col)>());

        public static ManualSwapResult Fired(IReadOnlyCollection<(int Row, int Col)> clearedCells) =>
            new ManualSwapResult(true, clearedCells);
    }

    /// <summary>
    /// Implements Requirement 3: validate, tentatively swap, check for a
    /// match, then commit or revert. Out-of-bounds and non-adjacent swaps are
    /// rejected before ever touching the board, so a rejected swap never
    /// needs a revert step at all (vs. swap-then-undo, which would also work
    /// but means a malformed call briefly mutates state for no reason).
    /// </summary>
    public static class SwapEngine
    {
        public static SwapResult TrySwap(Board board, int r1, int c1, int r2, int c2)
        {
            if (!board.InBounds(r1, c1) || !board.InBounds(r2, c2))
                return SwapResult.Rejected;

            if (!board.AreAdjacent(r1, c1, r2, c2))
                return SwapResult.Rejected;

            board.SwapTiles(r1, c1, r2, c2);
            var matchGroups = MatchResolver.FindMatchGroups(board);

            if (matchGroups.Count == 0)
            {
                board.SwapTiles(r1, c1, r2, c2); // revert
                return SwapResult.Rejected;
            }

            return SwapResult.Committed(matchGroups);
        }

        /// <summary>
        /// Implements Requirement 5c: manual booster activation via swap. Runs
        /// *before* the ordinary match check (design.md §3.6 composition note),
        /// so a caller uses it as: try this first; if it triggers, feed its
        /// cleared set into <see cref="CascadeEngine.ResolveCascadeFrom"/>;
        /// otherwise fall through to <see cref="TrySwap"/>. Two conditions
        /// trigger, both committing the swap and firing an effect regardless of
        /// whether an ordinary match would also form:
        /// <list type="number">
        /// <item>Both tiles are BloomBurst boosters — fires BloomBurst's
        /// row-clear anchored on the target (second) cell's row (the tie-break
        /// from design.md §3.6, since both tiles are identical).</item>
        /// <item>Exactly one tile is a booster and the other is a non-booster —
        /// fires the booster's own effect aimed through the non-booster tile's
        /// position/color.</item>
        /// </list>
        /// Every other combination (two non-boosters, or two boosters that
        /// aren't both BloomBurst) returns <see cref="ManualSwapResult.NotTriggered"/>
        /// without touching the board.
        /// </summary>
        public static ManualSwapResult TryManualActivationSwap(Board board, int r1, int c1, int r2, int c2, System.Random rng)
        {
            if (!board.InBounds(r1, c1) || !board.InBounds(r2, c2))
                return ManualSwapResult.NotTriggered;

            if (!board.AreAdjacent(r1, c1, r2, c2))
                return ManualSwapResult.NotTriggered;

            var a = board[r1, c1];
            var b = board[r2, c2];
            bool aIsBooster = a.Booster != BoosterType.None;
            bool bIsBooster = b.Booster != BoosterType.None;

            // Condition 1: two BloomBursts. Tie-break anchors on the target cell (r2,c2).
            if (a.Booster == BoosterType.BloomBurst && b.Booster == BoosterType.BloomBurst)
            {
                board.SwapTiles(r1, c1, r2, c2);
                var cleared = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.BloomBurst, r2, c2, b.Type, rng);
                Consume(board, cleared, r2, c2);
                return ManualSwapResult.Fired(cleared);
            }

            // Condition 2: exactly one booster + one non-booster.
            if (aIsBooster ^ bIsBooster)
            {
                BoosterType booster;
                int targetRow, targetCol;
                TileType targetColor;
                if (aIsBooster)
                {
                    booster = a.Booster;
                    targetRow = r2; targetCol = c2; targetColor = b.Type;
                }
                else
                {
                    booster = b.Booster;
                    targetRow = r1; targetCol = c1; targetColor = a.Type;
                }

                board.SwapTiles(r1, c1, r2, c2); // booster lands on (targetRow, targetCol)
                var cleared = BoosterActivation.GetAffectedCellsAimed(board, booster, targetRow, targetCol, targetColor, rng);
                Consume(board, cleared, targetRow, targetCol);
                return ManualSwapResult.Fired(cleared);
            }

            // Two non-boosters, or two boosters not both BloomBurst: fall through.
            return ManualSwapResult.NotTriggered;
        }

        /// <summary>Consumes the just-fired booster: drops its booster flag so
        /// the cascade's chain expansion (<see cref="CascadeEngine.ResolveCascadeFrom"/>)
        /// doesn't re-activate it with its *own* (non-aimed) effect — which for
        /// SolarFlare would clear the booster's own color instead of the aimed
        /// target color — and adds its cell to the cleared set so the spent
        /// booster tile always leaves the board, even when the aimed effect
        /// (e.g. SolarFlare on a different color) wouldn't otherwise clear that
        /// cell.</summary>
        private static void Consume(Board board, HashSet<(int Row, int Col)> cleared, int row, int col)
        {
            board[row, col] = board[row, col].WithBooster(BoosterType.None);
            cleared.Add((row, col));
        }
    }
}
