using System;
using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Implements Requirement 5's GDD §7.2 activation-effect table: given a
    /// booster tile's position, returns the extra cells its effect clears.
    /// Pure with respect to the board (read-only) — see design.md §3.5.
    /// </summary>
    public static class BoosterActivation
    {
        private const int BottomRowCount = 2; // DeepSurge: "clears bottom two rows"
        private const int RandomTileCount = 5; // SporeCloud: "removes 5 random tiles"

        public static HashSet<(int Row, int Col)> GetAffectedCells(Board board, int row, int col, Random rng)
        {
            var tile = board[row, col];
            return ComputeEffect(board, tile.Booster, row, col, tile.Type, rng);
        }

        /// <summary>
        /// Requirement 5c: same GDD §7.2 effect table as
        /// <see cref="GetAffectedCells"/>, but "aimed" — the position-anchored
        /// effects (row/column/3x3) center on an explicit target position, and
        /// SolarFlare matches an explicit target color, both independent of
        /// where the booster tile itself sits or what color it is. The two
        /// board-anchored effects (SporeCloud, DeepSurge) ignore the target
        /// entirely, matching their non-aimed behavior. This is a separate,
        /// additive entry point so Requirement 5's original path stays
        /// untouched (design.md §3.6).
        /// </summary>
        public static HashSet<(int Row, int Col)> GetAffectedCellsAimed(Board board, BoosterType booster, int targetRow, int targetCol, TileType targetColor, Random rng)
        {
            return ComputeEffect(board, booster, targetRow, targetCol, targetColor, rng);
        }

        /// <summary>Shared effect table used by both the self-anchored
        /// (<see cref="GetAffectedCells"/>) and aimed
        /// (<see cref="GetAffectedCellsAimed"/>) entry points. <paramref name="row"/>/
        /// <paramref name="col"/> are the anchor position and <paramref name="color"/>
        /// the color SolarFlare matches — the only difference between the two
        /// callers is whether those come from the booster tile itself or from
        /// an explicit target.</summary>
        private static HashSet<(int Row, int Col)> ComputeEffect(Board board, BoosterType booster, int row, int col, TileType color, Random rng)
        {
            var cells = new HashSet<(int Row, int Col)>();

            switch (booster)
            {
                case BoosterType.BloomBurst:
                    for (int c = 0; c < board.Columns; c++)
                        cells.Add((row, c));
                    break;

                case BoosterType.LeafWheel:
                    for (int r = 0; r < board.Rows; r++)
                        cells.Add((r, col));
                    break;

                case BoosterType.TidalClear:
                    for (int r = Math.Max(0, row - 1); r <= Math.Min(board.Rows - 1, row + 1); r++)
                        for (int c = Math.Max(0, col - 1); c <= Math.Min(board.Columns - 1, col + 1); c++)
                            cells.Add((r, c));
                    break;

                case BoosterType.SolarFlare:
                    for (int r = 0; r < board.Rows; r++)
                        for (int c = 0; c < board.Columns; c++)
                            if (board[r, c].Type == color)
                                cells.Add((r, c));
                    break;

                case BoosterType.SporeCloud:
                    int target = Math.Min(RandomTileCount, board.Rows * board.Columns);
                    while (cells.Count < target)
                        cells.Add((rng.Next(board.Rows), rng.Next(board.Columns)));
                    break;

                case BoosterType.DeepSurge:
                    for (int r = Math.Max(0, board.Rows - BottomRowCount); r < board.Rows; r++)
                        for (int c = 0; c < board.Columns; c++)
                            cells.Add((r, c));
                    break;

                case BoosterType.None:
                    break;
            }

            return cells;
        }
    }
}
