using System;
using UnityEngine;
using IslandQuest.Match3;

[CreateAssetMenu(menuName = "IslandQuest/LevelDataAsset", fileName = "LevelDataAsset")]
public sealed class LevelDataAsset : ScriptableObject
{
    public int island = 1;
    public int levelNumber = 1;
    public LevelObjectiveType objectiveType = LevelObjectiveType.Score;
    public int objectiveTarget = 500;
    public int moveLimit = 20;
    public TileType[] allowedTileTypes = new[]
    {
        TileType.Flower,
        TileType.Leaf,
        TileType.Wave,
        TileType.Sun,
        TileType.Mushroom,
        TileType.Coral,
    };
    public int minInitialCreditBags = 1;
    public int maxInitialCreditBags = 2;
    public Difficulty difficulty = Difficulty.Easy;

    public LevelData ToLevelData()
    {
        var objective = new LevelObjective(objectiveType, objectiveTarget);
        return new LevelData(island, levelNumber, objective, moveLimit, allowedTileTypes, minInitialCreditBags, maxInitialCreditBags, difficulty);
    }
}
