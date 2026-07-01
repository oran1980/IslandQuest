using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>
    /// One connected blob of same-type matched cells (built by
    /// <see cref="MatchResolver.FindMatchGroups"/>). A straight run-of-3, a
    /// straight run-of-5, and an L-shaped merge of a row-run and a
    /// column-run are all just "a MatchGroup" here — see design.md §3.1b for
    /// why shape isn't tracked as a separate concept.
    /// </summary>
    public sealed class MatchGroup
    {
        public TileType Type { get; }
        public IReadOnlyCollection<(int Row, int Col)> Cells { get; }

        public int Size => Cells.Count;

        /// <summary>GDD §7.2: boosters spawn on a match of 4 or more.</summary>
        public bool IsBoosterEligible => Size >= 4;

        public BoosterType AwardedBooster => IsBoosterEligible ? BoosterRules.ForTileType(Type) : BoosterType.None;

        public MatchGroup(TileType type, IReadOnlyCollection<(int Row, int Col)> cells)
        {
            Type = type;
            Cells = cells;
        }
    }
}
