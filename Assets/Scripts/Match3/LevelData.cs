using System;
using System.Collections.Generic;
using System.Linq;

namespace IslandQuest.Match3
{
    public sealed class LevelData
    {
        public int Island { get; }
        public int LevelNumber { get; }
        public LevelObjective Objective { get; }
        public int MoveLimit { get; }
        public TileType[] AllowedTileTypes { get; }
        public int MinInitialCreditBags { get; }
        public int MaxInitialCreditBags { get; }

        public string Name => $"Island {Island} Level {LevelNumber}";

        public LevelData(
            int island,
            int levelNumber,
            LevelObjective objective,
            int moveLimit,
            TileType[] allowedTileTypes,
            int minInitialCreditBags = 1,
            int maxInitialCreditBags = 2)
        {
            if (island < 1)
                throw new ArgumentOutOfRangeException(nameof(island), "Island must be at least 1.");
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber), "Level number must be at least 1.");
            if (objective is null)
                throw new ArgumentNullException(nameof(objective));
            if (moveLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(moveLimit), "Move limit must be at least 1.");

            var uniqueTypes = allowedTileTypes?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(allowedTileTypes));
            if (uniqueTypes.Length < 3)
                throw new ArgumentException("Level must allow at least 3 distinct tile types.", nameof(allowedTileTypes));

            if (minInitialCreditBags < 0)
                throw new ArgumentOutOfRangeException(nameof(minInitialCreditBags));
            if (maxInitialCreditBags < minInitialCreditBags)
                throw new ArgumentOutOfRangeException(nameof(maxInitialCreditBags), "Max must be >= min.");

            Island = island;
            LevelNumber = levelNumber;
            Objective = objective;
            MoveLimit = moveLimit;
            AllowedTileTypes = uniqueTypes;
            MinInitialCreditBags = minInitialCreditBags;
            MaxInitialCreditBags = maxInitialCreditBags;
        }

        public BoardConfig ToBoardConfig(int? seed = null)
        {
            return new BoardConfig(
                rows: 9,
                columns: 9,
                allowedTileTypes: AllowedTileTypes,
                seed: seed,
                minInitialCreditBags: MinInitialCreditBags,
                maxInitialCreditBags: MaxInitialCreditBags);
        }

        /// <summary>Number of islands in the M1 catalog (GDD §12.1: 30 levels).</summary>
        public const int IslandCount = 6;

        /// <summary>Levels per island; 6 × 5 = the 30-level M1 target.</summary>
        public const int LevelsPerIsland = 5;

        // Reusable allowed-type sets. Fewer types = more incidental matches
        // (easier); the full six leans harder, so later islands use All6.
        private static readonly TileType[] All6 =
            { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom, TileType.Coral };
        private static readonly TileType[] Five =
            { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom };
        private static readonly TileType[] Four =
            { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun };

        /// <summary>The full 30-level catalog (Requirement 6). Single source of
        /// truth; <see cref="Island1Levels"/> and <see cref="IslandLevels"/>
        /// derive from it. Difficulty ramps island-over-island — see design.md's
        /// level-catalog section for the numbers and rationale.</summary>
        public static IReadOnlyList<LevelData> AllLevels { get; } = BuildAllLevels();

        /// <summary>Island 1 (Levels 1–5). Kept for back-compat with Task 10;
        /// now just a view over <see cref="AllLevels"/>.</summary>
        public static IReadOnlyList<LevelData> Island1Levels { get; } = IslandLevels(1);

        /// <summary>The 5 levels of the given island (1..<see cref="IslandCount"/>).</summary>
        public static IReadOnlyList<LevelData> IslandLevels(int island)
        {
            if (island < 1 || island > IslandCount)
                throw new ArgumentOutOfRangeException(nameof(island), $"Island must be between 1 and {IslandCount}.");

            var result = new List<LevelData>(LevelsPerIsland);
            foreach (var level in AllLevels)
                if (level.Island == island)
                    result.Add(level);
            return result;
        }

        private static LevelData Lvl(int island, int level, LevelObjectiveType type, int target, int moves, TileType[] types)
            => new LevelData(island, level, new LevelObjective(type, target), moves, types);

        private static IReadOnlyList<LevelData> BuildAllLevels()
        {
            return new List<LevelData>
            {
                // Island 1 — introductory (unchanged from Task 10).
                Lvl(1, 1, LevelObjectiveType.Score, 500, 20, All6),
                Lvl(1, 2, LevelObjectiveType.Collect, 8, 18, Five),
                Lvl(1, 3, LevelObjectiveType.Score, 850, 16, Five),
                Lvl(1, 4, LevelObjectiveType.Collect, 12, 15, Four),
                Lvl(1, 5, LevelObjectiveType.ClearBoard, 0, 22, All6),

                // Island 2.
                Lvl(2, 1, LevelObjectiveType.Score, 1000, 20, All6),
                Lvl(2, 2, LevelObjectiveType.Collect, 14, 18, Five),
                Lvl(2, 3, LevelObjectiveType.Score, 1300, 17, All6),
                Lvl(2, 4, LevelObjectiveType.Collect, 16, 16, Five),
                Lvl(2, 5, LevelObjectiveType.ClearBoard, 0, 22, All6),

                // Island 3.
                Lvl(3, 1, LevelObjectiveType.Score, 1600, 19, All6),
                Lvl(3, 2, LevelObjectiveType.Collect, 18, 17, All6),
                Lvl(3, 3, LevelObjectiveType.Score, 2000, 16, All6),
                Lvl(3, 4, LevelObjectiveType.Collect, 20, 15, Five),
                Lvl(3, 5, LevelObjectiveType.ClearBoard, 0, 21, All6),

                // Island 4.
                Lvl(4, 1, LevelObjectiveType.Score, 2400, 18, All6),
                Lvl(4, 2, LevelObjectiveType.Collect, 22, 17, All6),
                Lvl(4, 3, LevelObjectiveType.Score, 2800, 16, All6),
                Lvl(4, 4, LevelObjectiveType.Collect, 26, 15, All6),
                Lvl(4, 5, LevelObjectiveType.ClearBoard, 0, 20, All6),

                // Island 5.
                Lvl(5, 1, LevelObjectiveType.Score, 3200, 18, All6),
                Lvl(5, 2, LevelObjectiveType.Collect, 28, 16, All6),
                Lvl(5, 3, LevelObjectiveType.Score, 3800, 15, All6),
                Lvl(5, 4, LevelObjectiveType.Collect, 32, 15, All6),
                Lvl(5, 5, LevelObjectiveType.ClearBoard, 0, 19, All6),

                // Island 6 — hardest.
                Lvl(6, 1, LevelObjectiveType.Score, 4200, 17, All6),
                Lvl(6, 2, LevelObjectiveType.Collect, 34, 16, All6),
                Lvl(6, 3, LevelObjectiveType.Score, 4800, 15, All6),
                Lvl(6, 4, LevelObjectiveType.Collect, 40, 14, All6),
                Lvl(6, 5, LevelObjectiveType.ClearBoard, 0, 18, All6),
            };
        }
    }
}
