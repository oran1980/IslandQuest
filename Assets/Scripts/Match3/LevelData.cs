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

        public static IReadOnlyList<LevelData> Island1Levels { get; } = BuildIsland1Levels();

        private static IReadOnlyList<LevelData> BuildIsland1Levels()
        {
            return new List<LevelData>
            {
                new LevelData(
                    island: 1,
                    levelNumber: 1,
                    objective: new LevelObjective(LevelObjectiveType.Score, target: 500),
                    moveLimit: 20,
                    allowedTileTypes: new[] { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom, TileType.Coral }),
                new LevelData(
                    island: 1,
                    levelNumber: 2,
                    objective: new LevelObjective(LevelObjectiveType.Collect, target: 8),
                    moveLimit: 18,
                    allowedTileTypes: new[] { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom }),
                new LevelData(
                    island: 1,
                    levelNumber: 3,
                    objective: new LevelObjective(LevelObjectiveType.Score, target: 850),
                    moveLimit: 16,
                    allowedTileTypes: new[] { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom }),
                new LevelData(
                    island: 1,
                    levelNumber: 4,
                    objective: new LevelObjective(LevelObjectiveType.Collect, target: 12),
                    moveLimit: 15,
                    allowedTileTypes: new[] { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun }),
                new LevelData(
                    island: 1,
                    levelNumber: 5,
                    objective: new LevelObjective(LevelObjectiveType.ClearBoard, target: 0),
                    moveLimit: 22,
                    allowedTileTypes: new[] { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom, TileType.Coral })
            };
        }
    }
}
