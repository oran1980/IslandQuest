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
            var cells = new HashSet<(int Row, int Col)>();

            switch (tile.Booster)
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
                            if (board[r, c].Type == tile.Type)
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
