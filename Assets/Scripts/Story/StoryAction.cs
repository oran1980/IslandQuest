using System;

namespace IslandQuest.Story
{
    /// <summary>The credit-costed things Mia can do in a story scene
    /// (GDD §4.3). A scene may gate progress behind one of these, or be a free
    /// teaching beat (no action).</summary>
    public enum StoryActionType
    {
        LightCampfire,
        CrossRopeBridge,
        EnterHiddenCave,
        UnlockSecretPassage,
        RescueTrappedAnimal,
        OpenTreasureChest,
    }

    /// <summary>
    /// A story action and its green-credit cost + emotional-moment context,
    /// transcribed <b>verbatim</b> from GDD §4.3 (Credit Costs — Story Actions).
    /// These are GDD numbers, not tunable balance values — a verify test pins
    /// them to the table so an edit can't silently drift. The cost is charged
    /// through <c>CreditManager.TrySpend</c> (Requirement 2 crit. 3).
    /// </summary>
    public sealed class StoryAction
    {
        public StoryActionType Type { get; }
        public int Cost { get; }

        /// <summary>The GDD §4.3 "Emotional moment" column — what the action
        /// feels like + which life-hack lesson it opens.</summary>
        public string EmotionalMoment { get; }

        private StoryAction(StoryActionType type, int cost, string emotionalMoment)
        {
            Type = type;
            Cost = cost;
            EmotionalMoment = emotionalMoment;
        }

        /// <summary>The canonical action for a type, with its GDD §4.3 cost and
        /// context.</summary>
        public static StoryAction For(StoryActionType type) => type switch
        {
            StoryActionType.LightCampfire =>
                new StoryAction(type, 30, "Warmth, safety — Mia teaches the bow-drill technique"),
            StoryActionType.CrossRopeBridge =>
                new StoryAction(type, 50, "Tension — Leo is scared, survival tip on rope physics"),
            StoryActionType.EnterHiddenCave =>
                new StoryAction(type, 80, "Mystery — darkness, torch-making lesson begins"),
            StoryActionType.UnlockSecretPassage =>
                new StoryAction(type, 120, "Major story reveal — always feels worth it"),
            StoryActionType.RescueTrappedAnimal =>
                new StoryAction(type, 40, "Emotional hook — Mia explains animal behavior, Leo names it"),
            StoryActionType.OpenTreasureChest =>
                new StoryAction(type, 60, "Random bonus credits + collectible item"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown story action: {type}"),
        };
    }
}
