using System;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Color -> booster mapping, taken verbatim from GDD §7.2. Eligibility
    /// is purely a function of match *size* (>= 4, see <see cref="MatchGroup"/>)
    /// and *color* — the GDD does not distinguish a straight run from an L/T
    /// merge of two runs; there is exactly one booster per color.
    /// </summary>
    public static class BoosterRules
    {
        public static BoosterType ForTileType(TileType type) => type switch
        {
            TileType.Flower => BoosterType.BloomBurst,
            TileType.Leaf => BoosterType.LeafWheel,
            TileType.Wave => BoosterType.TidalClear,
            TileType.Sun => BoosterType.SolarFlare,
            TileType.Mushroom => BoosterType.SporeCloud,
            TileType.Coral => BoosterType.DeepSurge,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown tile type — add it to GDD §7.2's table and this mapping together.")
        };
    }
}
