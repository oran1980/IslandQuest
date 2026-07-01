using System;
using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Builds a fresh <see cref="Board"/> per <see cref="BoardConfig"/>.
    /// See design.md §3.2 for why this is constructive (provably matchless by
    /// construction) rather than "randomize then scan-and-patch".
    /// </summary>
    public static class BoardGenerator
    {
        private const int MaxSolvabilityAttempts = 25;

        public static Board Generate(BoardConfig config)
        {
            var rng = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();

            for (int attempt = 0; attempt < MaxSolvabilityAttempts; attempt++)
            {
                var board = BuildMatchlessBoard(config, rng);
                if (HasLegalMove(board))
                {
                    PlaceInitialCreditBags(board, config, rng);
                    return board;
                }
            }

            throw new InvalidOperationException(
                $"Could not generate a board with at least one legal move after {MaxSolvabilityAttempts} " +
                "attempts. This should be effectively impossible for a 9x9 board with 6 tile types — check " +
                "BoardConfig for a degenerate (very small / very few tile types) configuration.");
        }

        /// <summary>
        /// Row-major constructive fill. At each cell, removes from the
        /// candidate pool any type that would extend an existing 2-run to the
        /// left or above into a run of 3. Because <see cref="BoardConfig"/>
        /// guarantees >= 3 allowed types, at most 2 are ever removed, so a
        /// legal choice always exists — the finished board is matchless by
        /// construction, not by luck.
        /// </summary>
        private static Board BuildMatchlessBoard(BoardConfig config, Random rng)
        {
            var board = new Board(config.Rows, config.Columns);
            var candidateBuffer = new List<TileType>(config.AllowedTileTypes.Length);

            for (int r = 0; r < config.Rows; r++)
            {
                for (int c = 0; c < config.Columns; c++)
                {
                    candidateBuffer.Clear();
                    candidateBuffer.AddRange(config.AllowedTileTypes);

                    if (c >= 2 && board[r, c - 1].Type == board[r, c - 2].Type)
                        candidateBuffer.Remove(board[r, c - 1].Type);

                    if (r >= 2 && board[r - 1, c].Type == board[r - 2, c].Type)
                        candidateBuffer.Remove(board[r - 1, c].Type);

                    var chosen = candidateBuffer[rng.Next(candidateBuffer.Count)];
                    board[r, c] = new Tile(chosen);
                }
            }

            return board;
        }

        private static bool HasLegalMove(Board board)
        {
            for (int r = 0; r < board.Rows; r++)
            {
                for (int c = 0; c < board.Columns; c++)
                {
                    if (c + 1 < board.Columns && SwapCreatesMatch(board, r, c, r, c + 1))
                        return true;
                    if (r + 1 < board.Rows && SwapCreatesMatch(board, r, c, r + 1, c))
                        return true;
                }
            }
            return false;
        }

        private static bool SwapCreatesMatch(Board board, int r1, int c1, int r2, int c2)
        {
            board.SwapTiles(r1, c1, r2, c2);
            bool createsMatch = MatchFinder.HasAnyMatch(board);
            board.SwapTiles(r1, c1, r2, c2); // always swap back, regardless of outcome
            return createsMatch;
        }

        private static void PlaceInitialCreditBags(Board board, BoardConfig config, Random rng)
        {
            int bagCount = rng.Next(config.MinInitialCreditBags, config.MaxInitialCreditBags + 1);
            var placed = new HashSet<(int Row, int Col)>();

            while (placed.Count < bagCount)
            {
                int r = rng.Next(board.Rows);
                int c = rng.Next(board.Columns);
                if (placed.Add((r, c)))
                    board[r, c] = board[r, c].WithCreditBag(true);
            }
        }
    }
}
