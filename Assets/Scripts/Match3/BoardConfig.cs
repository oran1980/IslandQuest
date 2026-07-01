using System;
using System.Linq;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Immutable settings for one board's generation. Defaults match GDD §7.1
    /// (9x9 grid, all 6 tile types, ~1-2 starting credit bags).
    /// </summary>
    public sealed class BoardConfig
    {
        public int Rows { get; }
        public int Columns { get; }
        public TileType[] AllowedTileTypes { get; }
        public int? Seed { get; }
        public int MinInitialCreditBags { get; }
        public int MaxInitialCreditBags { get; }

        public BoardConfig(
            int rows = 9,
            int columns = 9,
            TileType[]? allowedTileTypes = null,
            int? seed = null,
            int minInitialCreditBags = 1,
            int maxInitialCreditBags = 2)
        {
            if (rows < 3)
                throw new ArgumentOutOfRangeException(nameof(rows), "Board needs at least 3 rows for a run-of-3 to be possible.");
            if (columns < 3)
                throw new ArgumentOutOfRangeException(nameof(columns), "Board needs at least 3 columns for a run-of-3 to be possible.");

            var types = (allowedTileTypes ?? (TileType[])Enum.GetValues(typeof(TileType))).Distinct().ToArray();
            if (types.Length < 3)
                throw new ArgumentException(
                    "At least 3 distinct tile types are required: with only 2 types, the constructive " +
                    "matchless-fill algorithm (design.md §3.2) can be forced into a corner with zero legal " +
                    "choices for a cell.", nameof(allowedTileTypes));

            if (minInitialCreditBags < 0)
                throw new ArgumentOutOfRangeException(nameof(minInitialCreditBags));
            if (maxInitialCreditBags < minInitialCreditBags)
                throw new ArgumentOutOfRangeException(nameof(maxInitialCreditBags), "Max must be >= min.");
            if (maxInitialCreditBags > rows * columns)
                throw new ArgumentOutOfRangeException(nameof(maxInitialCreditBags), "Cannot place more credit bags than cells on the board.");

            Rows = rows;
            Columns = columns;
            AllowedTileTypes = types;
            Seed = seed;
            MinInitialCreditBags = minInitialCreditBags;
            MaxInitialCreditBags = maxInitialCreditBags;
        }
    }
}
