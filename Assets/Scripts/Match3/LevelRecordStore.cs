using System;
using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>A record of how well each level has been beaten — the best star
    /// count earned per level. The level-select UI (Task 16) reads it to show a
    /// star record next to each level. Kept behind an interface so a real
    /// persistence layer (a future <c>Core/SaveSystem</c>, deliberately out of
    /// scope per Task 8) can drop in without the UI changing.</summary>
    public interface ILevelRecordStore
    {
        /// <summary>Best stars (0–3) earned on the given level; 0 if unplayed.</summary>
        int GetBestStars(int levelNumber);

        /// <summary>Record a level result. Keeps only the best: returns true if
        /// this beat the previous best (or is the first record), false if an
        /// equal or worse replay left the record unchanged.</summary>
        bool Record(int levelNumber, int stars);

        /// <summary>Sum of best stars across every recorded level.</summary>
        int TotalStars { get; }
    }

    /// <summary>In-memory <see cref="ILevelRecordStore"/> — holds records for the
    /// current run only (they reset when the app restarts). This is intentional
    /// for M1: persistence is a <c>Core/SaveSystem</c> concern that isn't built
    /// yet (Task 8). Plain C#, no UnityEngine, so it stays verify-testable.</summary>
    public sealed class LevelRecordStore : ILevelRecordStore
    {
        private const int MaxStars = 3;

        private readonly Dictionary<int, int> _bestStars = new();

        public int GetBestStars(int levelNumber)
        {
            ValidateLevel(levelNumber);
            return _bestStars.TryGetValue(levelNumber, out int stars) ? stars : 0;
        }

        public bool Record(int levelNumber, int stars)
        {
            ValidateLevel(levelNumber);
            if (stars < 0 || stars > MaxStars)
                throw new ArgumentOutOfRangeException(nameof(stars), $"Stars must be between 0 and {MaxStars}.");

            if (_bestStars.TryGetValue(levelNumber, out int previous) && stars <= previous)
                return false;

            _bestStars[levelNumber] = stars;
            return true;
        }

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (int stars in _bestStars.Values)
                    total += stars;
                return total;
            }
        }

        private static void ValidateLevel(int levelNumber)
        {
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber), "Level number must be at least 1.");
        }
    }
}
