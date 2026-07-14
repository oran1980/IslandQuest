using System;

namespace IslandQuest.Match3
{
    public sealed class HeartSystem
    {
        public int CurrentHearts { get; }
        public int MaxHearts { get; }
        public TimeSpan RegenInterval { get; }
        public DateTimeOffset? NextHeartAt { get; }

        public HeartSystem(int currentHearts, int maxHearts = 5, TimeSpan? regenInterval = null, DateTimeOffset? nextHeartAt = null)
        {
            if (maxHearts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHearts), "Max hearts must be at least 1.");
            if (currentHearts < 0)
                throw new ArgumentOutOfRangeException(nameof(currentHearts), "Current hearts cannot be negative.");
            if (currentHearts > maxHearts)
                throw new ArgumentOutOfRangeException(nameof(currentHearts), "Current hearts cannot exceed max hearts.");

            CurrentHearts = currentHearts;
            MaxHearts = maxHearts;
            RegenInterval = regenInterval ?? TimeSpan.FromMinutes(30);
            NextHeartAt = currentHearts >= maxHearts ? null : nextHeartAt;
        }

        public bool HasHearts => CurrentHearts > 0;

        public HeartSystem LoseHeart(DateTimeOffset now)
        {
            int newHearts = CurrentHearts > 0 ? CurrentHearts - 1 : 0;
            DateTimeOffset? next = NextHeartAt;

            if (newHearts < MaxHearts && next is null)
                next = now + RegenInterval;

            return new HeartSystem(newHearts, MaxHearts, RegenInterval, next);
        }

        public HeartSystem Regenerate(DateTimeOffset now)
        {
            if (NextHeartAt is null || now < NextHeartAt.Value)
                return this;

            var delta = now - NextHeartAt.Value;
            long intervals = 1 + (long)(delta.Ticks / RegenInterval.Ticks);
            int restored = (int)Math.Min(MaxHearts - CurrentHearts, intervals);
            int newHearts = CurrentHearts + restored;

            if (newHearts >= MaxHearts)
                return new HeartSystem(MaxHearts, MaxHearts, RegenInterval, null);

            var next = NextHeartAt.Value + TimeSpan.FromTicks(restoreIntervalsTicks(intervals, RegenInterval));
            return new HeartSystem(newHearts, MaxHearts, RegenInterval, next);

            static long restoreIntervalsTicks(long intervals, TimeSpan interval) => intervals * interval.Ticks;
        }

        public TimeSpan? GetTimeUntilNextHeart(DateTimeOffset now)
        {
            if (NextHeartAt is null)
                return null;

            var remaining = NextHeartAt.Value - now;
            return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }
}
