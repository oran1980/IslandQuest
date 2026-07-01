using System;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Mutable grid of <see cref="Tile"/>s. Holds state only — no rules live
    /// here; see <see cref="MatchFinder"/> and <see cref="BoardGenerator"/>.
    /// </summary>
    public sealed class Board
    {
        private readonly Tile[,] _grid;

        public int Rows { get; }
        public int Columns { get; }

        public Board(int rows, int columns)
        {
            if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));
            if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));

            Rows = rows;
            Columns = columns;
            _grid = new Tile[rows, columns];
        }

        public Tile this[int row, int col]
        {
            get => _grid[row, col];
            set => _grid[row, col] = value;
        }

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < Columns;

        public bool AreAdjacent(int r1, int c1, int r2, int c2)
        {
            int rowDelta = Math.Abs(r1 - r2);
            int colDelta = Math.Abs(c1 - c2);
            return (rowDelta == 1 && colDelta == 0) || (rowDelta == 0 && colDelta == 1);
        }

        /// <summary>Swaps two cells in place. Pure mechanics, no validation —
        /// callers (BoardGenerator's solvability probe, SwapEngine) decide
        /// whether a swap is legal before calling this.</summary>
        public void SwapTiles(int r1, int c1, int r2, int c2)
        {
            var temp = this[r1, c1];
            this[r1, c1] = this[r2, c2];
            this[r2, c2] = temp;
        }

        /// <summary>Deep copy. Used so callers can snapshot state before a
        /// tentative operation (e.g. the future swap-then-revert flow).</summary>
        public Board Clone()
        {
            var copy = new Board(Rows, Columns);
            Array.Copy(_grid, copy._grid, _grid.Length);
            return copy;
        }
    }
}
