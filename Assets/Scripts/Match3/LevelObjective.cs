using System;

namespace IslandQuest.Match3
{
    public enum LevelObjectiveType
    {
        Score,       // reach a points target
        Collect,     // clear a target count of tiles
        CollectBags  // collect all of the level's credit bags (Requirement 7)
    }

    /// <summary>Per-level difficulty tier (Requirement 7). Scales the win
    /// reward; see <see cref="LevelEvaluator"/>. Order matters — used as an
    /// ascending rank when checking the catalog's difficulty ramp.</summary>
    public enum Difficulty
    {
        Easy,
        Hard,
        VeryHard
    }

    public sealed class LevelObjective
    {
        public LevelObjectiveType Type { get; }
        public int Target { get; }

        public LevelObjective(LevelObjectiveType type, int target)
        {
            // All three objective types now carry a positive target: a score to
            // reach, a tile count to clear, or a bag count to collect.
            if (target < 1)
                throw new ArgumentOutOfRangeException(nameof(target),
                    "Objectives require a positive target (score / tile count / bag count).");

            Type = type;
            Target = target;
        }

        public bool IsComplete(LevelProgress progress)
        {
            if (progress is null)
                throw new ArgumentNullException(nameof(progress));

            return Type switch
            {
                LevelObjectiveType.Score => progress.Score >= Target,
                LevelObjectiveType.Collect => progress.Collected >= Target,
                // RemainingCount = uncollected objective bags; done at zero.
                LevelObjectiveType.CollectBags => progress.RemainingCount == 0,
                _ => throw new InvalidOperationException("Unknown objective type."),
            };
        }

        public int PerformanceValue(LevelProgress progress)
        {
            if (progress is null)
                throw new ArgumentNullException(nameof(progress));

            return Type switch
            {
                LevelObjectiveType.Score => progress.Score,
                LevelObjectiveType.Collect => progress.Collected,
                // Stars on a bag-collection level reflect how well you scored.
                LevelObjectiveType.CollectBags => progress.Score,
                _ => throw new InvalidOperationException("Unknown objective type."),
            };
        }
    }

    public sealed class LevelStarThresholds
    {
        public int OneStar { get; }
        public int TwoStar { get; }
        public int ThreeStar { get; }

        public LevelStarThresholds(int oneStar, int twoStar, int threeStar)
        {
            if (oneStar < 1)
                throw new ArgumentOutOfRangeException(nameof(oneStar), "One-star threshold must be positive.");
            if (twoStar < oneStar)
                throw new ArgumentOutOfRangeException(nameof(twoStar), "Two-star threshold must be greater than or equal to one-star threshold.");
            if (threeStar < twoStar)
                throw new ArgumentOutOfRangeException(nameof(threeStar), "Three-star threshold must be greater than or equal to two-star threshold.");

            OneStar = oneStar;
            TwoStar = twoStar;
            ThreeStar = threeStar;
        }

        public int GetStars(int performance)
        {
            if (performance >= ThreeStar)
                return 3;
            if (performance >= TwoStar)
                return 2;
            if (performance >= OneStar)
                return 1;
            return 0;
        }
    }

    public sealed class LevelProgress
    {
        public int Score { get; }
        public int Collected { get; }
        public int RemainingCount { get; }

        public LevelProgress(int score, int collected, int remainingCount)
        {
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
            if (collected < 0) throw new ArgumentOutOfRangeException(nameof(collected));
            if (remainingCount < 0) throw new ArgumentOutOfRangeException(nameof(remainingCount));

            Score = score;
            Collected = collected;
            RemainingCount = remainingCount;
        }
    }

    public sealed class LevelResult
    {
        public bool IsComplete { get; }
        public int Stars { get; }
        public int CreditPayout { get; }

        public LevelResult(bool isComplete, int stars, int creditPayout)
        {
            IsComplete = isComplete;
            Stars = stars;
            CreditPayout = creditPayout;
        }
    }

    public static class LevelEvaluator
    {
        public static LevelResult Evaluate(LevelObjective objective, LevelStarThresholds thresholds, LevelProgress progress, Difficulty difficulty = Difficulty.Easy)
        {
            if (objective is null) throw new ArgumentNullException(nameof(objective));
            if (thresholds is null) throw new ArgumentNullException(nameof(thresholds));
            if (progress is null) throw new ArgumentNullException(nameof(progress));

            bool completed = objective.IsComplete(progress);
            int stars = completed ? thresholds.GetStars(objective.PerformanceValue(progress)) : 0;
            int payout = ComputePayout(stars, difficulty);
            return new LevelResult(completed, stars, payout);
        }

        /// <summary>GDD §6.2 flat star reward (20/35/55) scaled by the level's
        /// difficulty (Requirement 7): Easy ×1, Hard ×1.5, VeryHard ×2. The
        /// difficulty scaling is a documented extension of the GDD's flat
        /// payout — harder levels are worth more.</summary>
        private static int ComputePayout(int stars, Difficulty difficulty)
        {
            int baseCredits = stars switch
            {
                1 => 20,
                2 => 35,
                3 => 55,
                _ => 0,
            };
            return (int)Math.Round(baseCredits * DifficultyMultiplier(difficulty), MidpointRounding.AwayFromZero);
        }

        private static double DifficultyMultiplier(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => 1.0,
            Difficulty.Hard => 1.5,
            Difficulty.VeryHard => 2.0,
            _ => 1.0,
        };
    }
}
