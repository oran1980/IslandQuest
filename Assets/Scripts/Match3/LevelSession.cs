using System;

namespace IslandQuest.Match3
{
    /// <summary>Where a level stands: still playable, or finished win/lose.</summary>
    public enum LevelOutcome
    {
        InProgress,
        Won,
        Lost
    }

    /// <summary>
    /// Implements Requirement 7 crit. 5 (design.md §7.4): the runtime "session"
    /// that turns a sequence of player moves into a win or loss. Each move's
    /// <see cref="CascadeResult"/> is folded into running score / tiles / bags;
    /// moves are counted against the level's limit. The level is <b>Won</b> the
    /// moment the objective completes, or <b>Lost</b> when the move budget is
    /// exhausted with the objective still incomplete. Pure C#, no UnityEngine —
    /// the presentation layer (Task 16) drives it and reads back its state.
    /// </summary>
    public sealed class LevelSession
    {
        private readonly LevelData _level;
        private readonly LevelStarThresholds _thresholds;

        private int _movesUsed;
        private int _score;
        private int _tilesCleared;
        private int _bagsCollected;

        public LevelSession(LevelData level, LevelStarThresholds thresholds)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        public int MovesUsed => _movesUsed;
        public int MovesRemaining => _level.MoveLimit - _movesUsed;
        public int Score => _score;
        public int TilesCleared => _tilesCleared;
        public int BagsCollected => _bagsCollected;

        /// <summary>Progress snapshot in the shape <see cref="LevelObjective"/>
        /// expects. For a <c>CollectBags</c> level, <see cref="LevelProgress.RemainingCount"/>
        /// is the number of the level's seeded bags not yet collected.</summary>
        public LevelProgress Progress => new LevelProgress(_score, _tilesCleared, RemainingBags());

        public bool IsObjectiveComplete => _level.Objective.IsComplete(Progress);

        /// <summary>Won the instant the objective completes; Lost once the move
        /// budget is spent without completing; otherwise still InProgress.</summary>
        public LevelOutcome Outcome
        {
            get
            {
                if (IsObjectiveComplete)
                    return LevelOutcome.Won;
                if (MovesRemaining <= 0)
                    return LevelOutcome.Lost;
                return LevelOutcome.InProgress;
            }
        }

        /// <summary>Record the result of one player move (a swap and its full
        /// cascade). Counts as one move against the budget. Throws if the level
        /// is already over.</summary>
        public void ApplyMove(CascadeResult cascade)
        {
            if (cascade is null) throw new ArgumentNullException(nameof(cascade));
            if (Outcome != LevelOutcome.InProgress)
                throw new InvalidOperationException("Cannot apply a move: the level is already over.");

            _movesUsed++;
            _score += cascade.Score;
            _tilesCleared += cascade.TilesCleared;
            _bagsCollected += cascade.CreditBagsCollected;
        }

        /// <summary>The final grade — completion, stars, and difficulty-scaled
        /// credit payout. Meaningful once <see cref="Outcome"/> is Won or Lost;
        /// on a loss it reports incomplete with zero stars/credits.</summary>
        public LevelResult GetResult() =>
            LevelEvaluator.Evaluate(_level.Objective, _thresholds, Progress, _level.Difficulty);

        private int RemainingBags()
        {
            if (_level.Objective.Type != LevelObjectiveType.CollectBags)
                return 0;
            return Math.Max(0, _level.Objective.Target - _bagsCollected);
        }
    }
}
