using System;

namespace IslandQuest.Match3
{
    public enum LevelObjectiveType
    {
        Score,
        Collect,
        ClearBoard
    }

    public sealed class LevelObjective
    {
        public LevelObjectiveType Type { get; }
        public int Target { get; }

        public LevelObjective(LevelObjectiveType type, int target)
        {
            if (type == LevelObjectiveType.Score || type == LevelObjectiveType.Collect)
            {
                if (target < 1)
                    throw new ArgumentOutOfRangeException(nameof(target), "Score and Collect objectives require a positive target.");
            }
            else if (type == LevelObjectiveType.ClearBoard)
            {
                if (target != 0)
                    throw new ArgumentOutOfRangeException(nameof(target), "ClearBoard objectives do not use a target value.");
            }

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
                LevelObjectiveType.ClearBoard => progress.RemainingCount == 0,
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
                LevelObjectiveType.ClearBoard => progress.Score,
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
        public static LevelResult Evaluate(LevelObjective objective, LevelStarThresholds thresholds, LevelProgress progress)
        {
            if (objective is null) throw new ArgumentNullException(nameof(objective));
            if (thresholds is null) throw new ArgumentNullException(nameof(thresholds));
            if (progress is null) throw new ArgumentNullException(nameof(progress));

            bool completed = objective.IsComplete(progress);
            int stars = completed ? thresholds.GetStars(objective.PerformanceValue(progress)) : 0;
            int payout = ComputePayout(stars);
            return new LevelResult(completed, stars, payout);
        }

        private static int ComputePayout(int stars) => stars switch
        {
            1 => 20,
            2 => 35,
            3 => 55,
            _ => 0,
        };
    }
}
