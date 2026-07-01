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
    }
}
