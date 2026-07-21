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

        /// <summary>Total levels in the M1 catalog — GDD §12.1 ("30 levels")
        /// and §8.2 (Island 1, "Coconut Isle", spans Levels 1–30).</summary>
        public const int LevelCount = 30;

        /// <summary>The island this catalog covers. Per GDD §8.2 the whole M1
        /// catalog is Island 1 (Levels 1–30); Islands 2–3 (Levels 31–70,
        /// 71–120) are later milestones and aren't authored here.</summary>
        public const int Island1 = 1;

        // Reusable allowed-type sets. Fewer types = more incidental matches
        // (easier); the full six leans harder, so later levels use All6.
        private static readonly TileType[] All6 =
            { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom, TileType.Coral };
        private static readonly TileType[] Five =
            { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom };
        private static readonly TileType[] Four =
            { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun };

        /// <summary>The full 30-level catalog (Requirement 6): Island 1,
        /// Levels 1–30, in ascending order. Single source of truth;
        /// <see cref="Island1Levels"/> and <see cref="IslandLevels"/> derive
        /// from it. Difficulty ramps level-over-level — see design.md §6 for
        /// the numbers and rationale.</summary>
        public static IReadOnlyList<LevelData> AllLevels { get; } = BuildAllLevels();

        /// <summary>Island 1's levels — for the M1 catalog this is the entire
        /// catalog (all 30). A view over <see cref="AllLevels"/>.</summary>
        public static IReadOnlyList<LevelData> Island1Levels { get; } = IslandLevels(Island1);

        /// <summary>The levels belonging to the given island. Only Island 1 is
        /// authored in M1; any other island returns an empty list.</summary>
        public static IReadOnlyList<LevelData> IslandLevels(int island)
        {
            if (island < 1)
                throw new ArgumentOutOfRangeException(nameof(island), "Island must be at least 1.");

            var result = new List<LevelData>();
            foreach (var level in AllLevels)
                if (level.Island == island)
                    result.Add(level);
            return result;
        }

        private static LevelData Lvl(int level, LevelObjectiveType type, int target, int moves, TileType[] types)
            => new LevelData(Island1, level, new LevelObjective(type, target), moves, types);

        // Island 1 — Coconut Isle, Levels 1–30 (GDD §8.2). One long difficulty
        // ramp; the Score/Collect/ClearBoard rhythm repeats every 5 levels for
        // variety. The Levels 1–5 block is the original Task 10 sample, now the
        // gentle on-ramp of the full island.
        private static IReadOnlyList<LevelData> BuildAllLevels()
        {
            return new List<LevelData>
            {
                Lvl(1, LevelObjectiveType.Score, 500, 20, All6),
                Lvl(2, LevelObjectiveType.Collect, 8, 18, Five),
                Lvl(3, LevelObjectiveType.Score, 850, 16, Five),
                Lvl(4, LevelObjectiveType.Collect, 12, 15, Four),
                Lvl(5, LevelObjectiveType.ClearBoard, 0, 22, All6),

                Lvl(6, LevelObjectiveType.Score, 1000, 20, All6),
                Lvl(7, LevelObjectiveType.Collect, 14, 18, Five),
                Lvl(8, LevelObjectiveType.Score, 1300, 17, All6),
                Lvl(9, LevelObjectiveType.Collect, 16, 16, Five),
                Lvl(10, LevelObjectiveType.ClearBoard, 0, 22, All6),

                Lvl(11, LevelObjectiveType.Score, 1600, 19, All6),
                Lvl(12, LevelObjectiveType.Collect, 18, 17, All6),
                Lvl(13, LevelObjectiveType.Score, 2000, 16, All6),
                Lvl(14, LevelObjectiveType.Collect, 20, 15, Five),
                Lvl(15, LevelObjectiveType.ClearBoard, 0, 21, All6),

                Lvl(16, LevelObjectiveType.Score, 2400, 18, All6),
                Lvl(17, LevelObjectiveType.Collect, 22, 17, All6),
                Lvl(18, LevelObjectiveType.Score, 2800, 16, All6),
                Lvl(19, LevelObjectiveType.Collect, 26, 15, All6),
                Lvl(20, LevelObjectiveType.ClearBoard, 0, 20, All6),

                Lvl(21, LevelObjectiveType.Score, 3200, 18, All6),
                Lvl(22, LevelObjectiveType.Collect, 28, 16, All6),
                Lvl(23, LevelObjectiveType.Score, 3800, 15, All6),
                Lvl(24, LevelObjectiveType.Collect, 32, 15, All6),
                Lvl(25, LevelObjectiveType.ClearBoard, 0, 19, All6),

                Lvl(26, LevelObjectiveType.Score, 4200, 17, All6),
                Lvl(27, LevelObjectiveType.Collect, 34, 16, All6),
                Lvl(28, LevelObjectiveType.Score, 4800, 15, All6),
                Lvl(29, LevelObjectiveType.Collect, 40, 14, All6),
                Lvl(30, LevelObjectiveType.ClearBoard, 0, 18, All6),
            };
        }
    }
}
