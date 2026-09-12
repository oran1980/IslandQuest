using System;
using System.Collections.Generic;
using IslandQuest.Economy;

namespace IslandQuest.Story
{
    /// <summary>Result of trying to advance past the current story scene.</summary>
    public enum SceneOutcome
    {
        /// <summary>The scene resolved (gated cost charged if any, bonus awarded)
        /// and the act moved to the next scene (or completed).</summary>
        Advanced,

        /// <summary>The current scene is credit-gated and the player can't afford
        /// it — nothing changed (GDD §4.2's "return to puzzle or buy" fork).</summary>
        InsufficientCredits,
    }

    /// <summary>
    /// Sequences a story act's scenes and enforces the credit gate (GDD §11.2:
    /// "Scene sequencing, credit gate checks, dialogue flow"). Defaults to Act 1
    /// (<see cref="StoryScene.Act1"/>). Depends only on <see cref="CreditManager"/>
    /// + scene data — plain C#, verify-testable. The presentation layer reads
    /// <see cref="CurrentScene"/> to render and calls <see cref="TryAdvanceScene"/>
    /// when the player performs the scene's action / continues.
    /// </summary>
    public sealed class StoryManager
    {
        private readonly CreditManager _credits;
        private readonly IReadOnlyList<StoryScene> _scenes;
        private int _index;

        public StoryManager(CreditManager credits) : this(credits, StoryScene.Act1) { }

        public StoryManager(CreditManager credits, IReadOnlyList<StoryScene> scenes)
        {
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            if (scenes is null || scenes.Count == 0)
                throw new ArgumentException("A story act needs at least one scene.", nameof(scenes));
            _scenes = scenes;
            _index = 0;
        }

        /// <summary>The scene the player is on, or null once the act is complete.</summary>
        public StoryScene? CurrentScene => IsComplete ? null : _scenes[_index];

        /// <summary>True once every scene has been advanced past.</summary>
        public bool IsComplete => _index >= _scenes.Count;

        /// <summary>1-based position for display ("Scene 2 of 5").</summary>
        public int SceneNumber => _index + 1;

        public int SceneCount => _scenes.Count;

        /// <summary>Perform the current scene's action and advance. For a gated
        /// scene this charges its cost via <see cref="CreditManager.TrySpend"/> —
        /// on failure nothing changes and <see cref="SceneOutcome.InsufficientCredits"/>
        /// is returned. On success (or for a free teaching beat) any bonus credits
        /// are awarded and the act moves to the next scene. Throws if the act is
        /// already complete.</summary>
        public SceneOutcome TryAdvanceScene()
        {
            if (IsComplete)
                throw new InvalidOperationException("The story act is already complete.");

            var scene = _scenes[_index];
            if (scene.IsGated && !_credits.TrySpend(scene.Action!.Cost))
                return SceneOutcome.InsufficientCredits;

            if (scene.BonusCredits > 0)
                _credits.AwardBonus(scene.BonusCredits);

            _index++;
            return SceneOutcome.Advanced;
        }
    }
}
