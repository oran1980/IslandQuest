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
        public int CreditBagsCollected { get; }

        public CascadeResult(int rounds, int bonusCredits, bool bonusBagDropped, int creditBagsCollected)
        {
            Rounds = rounds;
            BonusCredits = bonusCredits;
            BonusBagDropped = bonusBagDropped;
            CreditBagsCollected = creditBagsCollected;
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
            int creditBagsCollected = 0;

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

                var clearedCells = DetermineClearedCells(board, groups, rng);
                creditBagsCollected += CountClearedCreditBags(board, clearedCells);

                ClearGravityRefill(board, clearedCells, config, rng);
                rounds++;
            }

            var (bonusCredits, dropBag) = ComputeComboBonus(rounds);

            if (dropBag)
                DropOneBonusBag(board, rng);

            return new CascadeResult(rounds, bonusCredits, dropBag, creditBagsCollected);
        }

        /// <summary>
        /// Requirement 5c criterion 4 / design.md §3.6: entry point for a
        /// cascade whose first round's cleared cells are already known (e.g. a
        /// manual booster activation, which is a direct effect lookup rather
        /// than a <see cref="MatchGroup"/>). Runs that provided set through the
        /// same booster-chain expansion, credit-bag counting, and gravity/
        /// refill as an ordinary round, then continues the identical
        /// re-scan/clear/drop/refill loop as <see cref="ResolveCascade"/> — so
        /// manual activations reuse the exact cascade machinery, not a parallel
        /// copy. An empty <paramref name="initialClearedCells"/> degrades
        /// cleanly to <see cref="ResolveCascade"/>'s behavior.
        /// </summary>
        public static CascadeResult ResolveCascadeFrom(Board board, IReadOnlyCollection<(int Row, int Col)> initialClearedCells, BoardConfig config, Random rng, int maxRounds = DefaultMaxRounds)
        {
            int rounds = 0;
            int creditBagsCollected = 0;

            var seedCells = new HashSet<(int Row, int Col)>(initialClearedCells);
            if (seedCells.Count > 0)
            {
                ExpandBoosterChain(board, seedCells, rng);
                creditBagsCollected += CountClearedCreditBags(board, seedCells);
                ClearGravityRefill(board, seedCells, config, rng);
                rounds++;
            }

            while (true)
            {
                var groups = MatchResolver.FindMatchGroups(board);
                if (groups.Count == 0)
                    break;

                if (rounds >= maxRounds)
                {
                    throw new InvalidOperationException(
                        $"CascadeEngine.ResolveCascadeFrom exceeded maxRounds ({maxRounds}) without stabilizing. " +
                        "This should not happen in real gameplay and likely indicates a bug in match " +
                        "clearing/refill rather than a legitimately long cascade.");
                }

                var clearedCells = DetermineClearedCells(board, groups, rng);
                creditBagsCollected += CountClearedCreditBags(board, clearedCells);

                ClearGravityRefill(board, clearedCells, config, rng);
                rounds++;
            }

            var (bonusCredits, dropBag) = ComputeComboBonus(rounds);

            if (dropBag)
                DropOneBonusBag(board, rng);

            return new CascadeResult(rounds, bonusCredits, dropBag, creditBagsCollected);
        }

        /// <summary>
        /// Implements Requirement 5: for each match group, either spawns a
        /// booster tile (booster-eligible groups keep one cell — the
        /// topmost, then leftmost, per requirements.md's interpretation
        /// note — instead of clearing it) or clears every cell normally.
        /// Then repeatedly expands the cleared set for any already-cleared
        /// cell that is itself a booster tile, via
        /// <see cref="BoosterActivation.GetAffectedCells"/>, until a pass
        /// finds no new booster to activate (chain reactions, criterion 3).
        /// Mutates <paramref name="board"/> only to write newly-spawned
        /// boosters onto their surviving cell; does not clear/refill —
        /// that's still <see cref="ClearGravityRefill"/>'s job.
        /// </summary>
        public static HashSet<(int Row, int Col)> DetermineClearedCells(Board board, IReadOnlyList<MatchGroup> groups, Random rng)
        {
            var clearedCells = new HashSet<(int Row, int Col)>();

            foreach (var group in groups)
            {
                if (group.IsBoosterEligible)
                {
                    var spawnCell = ChooseBoosterSpawnCell(group);
                    board[spawnCell.Row, spawnCell.Col] = board[spawnCell.Row, spawnCell.Col].WithBooster(group.AwardedBooster);

                    foreach (var cell in group.Cells)
                        if (cell != spawnCell)
                            clearedCells.Add(cell);
                }
                else
                {
                    foreach (var cell in group.Cells)
                        clearedCells.Add(cell);
                }
            }

            ExpandBoosterChain(board, clearedCells, rng);

            return clearedCells;
        }

        /// <summary>
        /// Fixed-point chain reaction (Requirement 5 criterion 3 / 5c criterion
        /// 4): repeatedly scans <paramref name="clearedCells"/> for any cell
        /// that is itself a still-present booster tile and unions in its
        /// <see cref="BoosterActivation.GetAffectedCells"/> effect, until a full
        /// pass adds nothing new. Shared by <see cref="DetermineClearedCells"/>
        /// (match-driven clears) and <see cref="ResolveCascadeFrom"/> (manual
        /// activations) so both chain identically, however deep.
        /// </summary>
        private static void ExpandBoosterChain(Board board, HashSet<(int Row, int Col)> clearedCells, Random rng)
        {
            var processed = new HashSet<(int Row, int Col)>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var cell in new List<(int Row, int Col)>(clearedCells))
                {
                    if (!processed.Add(cell))
                        continue;

                    if (board[cell.Row, cell.Col].Booster == BoosterType.None)
                        continue;

                    foreach (var affected in BoosterActivation.GetAffectedCells(board, cell.Row, cell.Col, rng))
                        if (clearedCells.Add(affected))
                            changed = true;
                }
            }
        }

        /// <summary>Topmost, then leftmost cell of the group — see requirements.md
        /// Requirement 5's interpretation note for why this arbitrary-but-deterministic
        /// tiebreak was chosen (the GDD doesn't specify a spawn position).</summary>
        private static (int Row, int Col) ChooseBoosterSpawnCell(MatchGroup group)
        {
            (int Row, int Col) best = default;
            bool first = true;

            foreach (var cell in group.Cells)
            {
                if (first || cell.Row < best.Row || (cell.Row == best.Row && cell.Col < best.Col))
                {
                    best = cell;
                    first = false;
                }
            }

            return best;
        }

        private static int CountClearedCreditBags(Board board, IEnumerable<(int Row, int Col)> clearedCells)
        {
            int collected = 0;
            foreach (var cell in clearedCells)
            {
                if (board[cell.Row, cell.Col].HasCreditBag)
                    collected++;
            }
            return collected;
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
