using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Detects horizontal and vertical runs of 3+ identically-typed tiles.
    /// This is the single source of truth every other rule (generation,
    /// future swap/cascade logic) calls into, rather than each re-implementing
    /// detection — see design.md §3.1.
    /// </summary>
    public static class MatchFinder
    {
        public static bool HasAnyMatch(Board board) => FindMatchedCells(board).Count > 0;

        /// <summary>
        /// Returns every cell that participates in a run of 3+. A cell that
        /// belongs to both a horizontal and a vertical run appears exactly
        /// once (HashSet semantics), satisfying Requirement 2.3.
        /// </summary>
        public static HashSet<(int Row, int Col)> FindMatchedCells(Board board)
        {
            var matched = new HashSet<(int Row, int Col)>();

            // Horizontal runs, row by row.
            for (int r = 0; r < board.Rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= board.Columns; c++)
                {
                    bool sameAsPrev = c < board.Columns && board[r, c].Type == board[r, c - 1].Type;
                    if (!sameAsPrev)
                    {
                        if (c - runStart >= 3)
                        {
                            for (int k = runStart; k < c; k++)
                                matched.Add((r, k));
                        }
                        runStart = c;
                    }
                }
            }

            // Vertical runs, column by column.
            for (int c = 0; c < board.Columns; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= board.Rows; r++)
                {
                    bool sameAsPrev = r < board.Rows && board[r, c].Type == board[r - 1, c].Type;
                    if (!sameAsPrev)
                    {
                        if (r - runStart >= 3)
                        {
                            for (int k = runStart; k < r; k++)
                                matched.Add((k, c));
                        }
                        runStart = r;
                    }
                }
            }

            return matched;
        }
    }
}
