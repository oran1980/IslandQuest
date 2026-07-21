namespace IslandQuest.Match3
{
    /// <summary>
    /// Implements Requirement 7's scoring rule: each cleared tile is worth
    /// <see cref="PointsPerTile"/>, multiplied by the 1-based cascade combo
    /// round it was cleared in (round 1 = ×1, round 2 = ×2, …), so deeper
    /// cascades score more. Pure and engine-agnostic so the rule is unit-
    /// testable in isolation from the cascade machinery. Not GDD-derived —
    /// the GDD has no per-tile scoring; see design.md's level-play section.
    /// </summary>
    public static class ScoringRules
    {
        public const int PointsPerTile = 10;

        /// <summary>Points for clearing <paramref name="tilesCleared"/> tiles in
        /// cascade round <paramref name="comboRound"/> (1-based).</summary>
        public static int RoundScore(int tilesCleared, int comboRound)
        {
            if (tilesCleared <= 0 || comboRound <= 0)
                return 0;
            return tilesCleared * PointsPerTile * comboRound;
        }
    }
}
