namespace IslandQuest.Match3
{
    /// <summary>
    /// The six nature/survival-themed tile colors that appear on the Match-3
    /// board. Maps 1:1 to GDD §7.2 "Tile Elements &amp; Boosters".
    /// </summary>
    public enum TileType
    {
        Flower,   // Pink   - edible wildflowers vs toxic look-alikes
        Leaf,     // Green  - medicinal plants and herbal remedies
        Wave,     // Blue   - water sourcing and purification
        Sun,      // Yellow - solar navigation and signaling
        Mushroom, // Orange - edible vs toxic fungi identification
        Coral     // Red    - marine survival and reef navigation
    }

    /// <summary>
    /// Booster created when 4+ same-type tiles are matched at once.
    /// Activation effects are implemented in Task 5; the enum exists now so
    /// <see cref="Tile"/>'s shape is stable from the start.
    /// </summary>
    public enum BoosterType
    {
        None,
        BloomBurst,  // Flower   - clears entire row
        LeafWheel,   // Leaf     - clears full column
        TidalClear,  // Wave     - removes 3x3 zone
        SolarFlare,  // Sun      - removes all tiles of one color
        SporeCloud,  // Mushroom - removes 5 random tiles
        DeepSurge    // Coral    - clears bottom two rows
    }
}
