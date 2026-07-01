using System;
using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>Outcome of a full <see cref="CascadeEngine.ResolveCascade"/> call.</summary>
    public sealed class CascadeResult
    {
        public int Rounds { get; }
        public int BonusCredits { get; }
        public bool BonusBagDropped { get; }

        public CascadeResult(int rounds, int bonusCredits, bool bonusBagDropped)
        {
            Rounds = rounds;
            BonusCredits = bonusCredits;
            BonusBagDropped = bonusBagDropped;
        }
    }

    /// <summary>
    /// Implements Requirement 4a (design.md §3.4): repeatedly clears matched
    /// groups, applies gravity, refills, and re-scans until the board is
    /// stable, tracking cascade rounds for the GDD §6.2 combo bonus.
    /// </summary>
    public static class CascadeEngine
    {
        private const int DefaultMaxRounds = 50;

        /// <summary>
        /// GDD §6.2: "Combo x3 or more -> +10 bonus credits" and "mid-level
        /// bonus bag drops on board". Pure function — no board access — so
        /// the threshold rule is testable in total isolation from gravity/
        /// refill mechanics.
        /// </summary>
        public static (int BonusCredits, bool DropBonusBag) ComputeComboBonus(int cascadeRounds)
        {
            bool earned = cascadeRounds >= 3;
            return (earned ? 10 : 0, earned);
        }

        /// <summary>
        /// One mechanical pass: clears the given cells, compacts each
        /// affected column downward (survivors keep their relative order),
        /// and refills newly-emptied top cells with a uniform random pick
        /// from <paramref name="config"/>'s allowed types. Does not re-scan
        /// for matches — that's <see cref="ResolveCascade"/>'s job. Kept
        /// separate so gravity/refill mechanics are testable without needing
        /// a real match to trigger them.
        /// </summary>
        public static void ClearGravityRefill(Board board, IReadOnlyCollection<(int Row, int Col)> clearedCells, BoardConfig config, Random rng)
        {
            var clearedByColumn = new HashSet<int>();
            foreach (var cell in clearedCells)
                clearedByColumn.Add(cell.Col);

            var clearedSet = clearedCells as HashSet<(int Row, int Col)> ?? new HashSet<(int Row, int Col)>(clearedCells);

            foreach (var col in clearedByColumn)
            {
                var survivors = new List<Tile>();
                for (int r = 0; r < board.Rows; r++)
                {
                    if (!clearedSet.Contains((r, col)))
                        survivors.Add(board[r, col]);
                }

                int emptyCount = board.Rows - survivors.Count;

                // Survivors land in the bottom rows, preserving relative order.
                for (int i = 0; i < survivors.Count; i++)
                    board[emptyCount + i, col] = survivors[i];

                // Refill the newly-emptied top rows.
                for (int r = 0; r < emptyCount; r++)
                {
                    var newType = config.AllowedTileTypes[rng.Next(config.AllowedTileTypes.Length)];
                    board[r, col] = new Tile(newType);
                }
            }
        }

        /// <summary>
        /// Full cascade loop for one player action: re-scan, clear, drop,
        /// refill, repeat until a scan finds nothing. <paramref name="maxRounds"/>
        /// is a defensive cap (design.md §3.4) — exceeding it indicates a bug,
        /// not a legitimate gameplay scenario, so it throws rather than
        /// silently truncating the cascade.
        /// </summary>
        public static CascadeResult ResolveCascade(Board board, BoardConfig config, Random rng, int maxRounds = DefaultMaxRounds)
        {
            int rounds = 0;

            while (true)
            {
                var groups = MatchResolver.FindMatchGroups(board);
                if (groups.Count == 0)
                    break;

                if (rounds >= maxRounds)
                {
                    throw new InvalidOperationException(
                        $"CascadeEngine.ResolveCascade exceeded maxRounds ({maxRounds}) without stabilizing. " +
                        "This should not happen in real gameplay and likely indicates a bug in match " +
                        "clearing/refill rather than a legitimately long cascade.");
                }

                var clearedCells = new HashSet<(int Row, int Col)>();
                foreach (var group in groups)
                    foreach (var cell in group.Cells)
                        clearedCells.Add(cell);

                ClearGravityRefill(board, clearedCells, config, rng);
                rounds++;
            }

            var (bonusCredits, dropBag) = ComputeComboBonus(rounds);

            if (dropBag)
                DropOneBonusBag(board, rng);

            return new CascadeResult(rounds, bonusCredits, dropBag);
        }

        private static void DropOneBonusBag(Board board, Random rng)
        {
            var eligible = new List<(int Row, int Col)>();
            for (int r = 0; r < board.Rows; r++)
                for (int c = 0; c < board.Columns; c++)
                    if (!board[r, c].HasCreditBag)
                        eligible.Add((r, c));

            // If every cell already has a bag (degenerate/unrealistic config),
            // there's nowhere left to drop one — skip rather than throw, since
            // failing to award a bonus bag isn't worth crashing a cascade over.
            if (eligible.Count == 0)
                return;

            var (row, col) = eligible[rng.Next(eligible.Count)];
            board[row, col] = board[row, col].WithCreditBag(true);
        }
    }
}
