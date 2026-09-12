using System;
using System.Collections.Generic;

namespace IslandQuest.Story
{
    /// <summary>Night-world settings a story scene can play in (GDD §5.2).</summary>
    public enum NightSetting
    {
        ForestAtNight,
        Campfire,
        StandardCave,
        HiddenCave,
        JungleRiver,
        SecretRuins,
    }

    /// <summary>The survival life-hacks taught in Act 1 (Coconut Isle, GDD §3.4).
    /// M2 stores only the reference + the Layer-1 dialogue; the full tip-card /
    /// deep-dive content is M3 Education.</summary>
    public enum LifeHack
    {
        BowDrillFire,     // Campfire needed, no matches
        WaterFiltration,  // Dirty stream found
        LeanToShelter,    // Sudden rainstorm
        StarNavigation,   // Lost after dark
        FieldFirstAid,    // Leo gets a cut in the field
    }

    /// <summary>
    /// One night-story scene (Story Layer Requirement 5): a setting, the life
    /// hack it teaches (GDD §3.4), the Mia+Leo dialogue that delivers it (Layer
    /// 1, §3.5), an <b>optional</b> credit-gated <see cref="StoryAction"/> (null
    /// for a free teaching beat), and optional bonus credits (a treasure/reveal,
    /// GDD §4.2). Pure data; <see cref="StoryManager"/> sequences these.
    /// </summary>
    public sealed class StoryScene
    {
        public NightSetting Setting { get; }
        public LifeHack LifeHack { get; }
        public DialogueSequence Dialogue { get; }

        /// <summary>The credit-gated action, or null if this is a free teaching
        /// beat (Act 1 gates only the campfire — see design.md §3).</summary>
        public StoryAction? Action { get; }

        /// <summary>Bonus credits awarded when the scene resolves (0 = none).</summary>
        public int BonusCredits { get; }

        public bool IsGated => Action != null;

        public StoryScene(NightSetting setting, LifeHack lifeHack, DialogueSequence dialogue,
            StoryAction? action = null, int bonusCredits = 0)
        {
            Dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            if (bonusCredits < 0)
                throw new ArgumentOutOfRangeException(nameof(bonusCredits), "Bonus credits cannot be negative.");

            Setting = setting;
            LifeHack = lifeHack;
            Action = action;
            BonusCredits = bonusCredits;
        }

        /// <summary>Act 1 — Coconut Isle (GDD §3.4), the five story-trigger
        /// scenes in narrative order. Only the campfire is credit-gated; the rest
        /// are free teaching beats (see requirements.md Requirement 5). Dialogue
        /// is authored in Mia/Leo's §3.3/§3.5 voice — Mia teaches, Leo asks the
        /// "but why?" follow-up.</summary>
        public static IReadOnlyList<StoryScene> Act1 { get; } = BuildAct1();

        private static IReadOnlyList<StoryScene> BuildAct1()
        {
            return new List<StoryScene>
            {
                // 1 — Campfire needed, no matches → bow-drill (the gated showcase).
                // Gated scenes run longer than free beats: the player spent credits,
                // so the lesson goes deeper and Leo gets his full "but WHY" (§3.3).
                new StoryScene(NightSetting.Campfire, LifeHack.BowDrillFire,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "No matches, no lighter. Just us and a pile of sticks."),
                        new DialogueLine(Speaker.Leo, "That's less reassuring than you think it is."),
                        new DialogueLine(Speaker.Mia, "Friction and patience make fire. Dry wood is everything — if it bends instead of snapping, it's no good to us."),
                        new DialogueLine(Speaker.Leo, "So we're rubbing sticks together. Like actual cavemen."),
                        new DialogueLine(Speaker.Mia, "Cavemen had ten thousand years of practice. We've got about twenty minutes before it gets cold."),
                        new DialogueLine(Speaker.Leo, "But WHY does spinning a stick make fire? It's just... wood."),
                        new DialogueLine(Speaker.Mia, "Friction grinds the wood into dust and heats it past its ignition point. You're not making a flame — you're making one ember."),
                        new DialogueLine(Speaker.Mia, "Then you catch it in the tinder and breathe. Gently — an ember dies easy.")),
                    StoryAction.For(StoryActionType.LightCampfire)),

                // 2 — Dirty stream found → 3-layer filtration + boiling.
                new StoryScene(NightSetting.JungleRiver, LifeHack.WaterFiltration,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "It's moving, sure. So is everything living in it."),
                        new DialogueLine(Speaker.Leo, "...I was about to drink that."),
                        new DialogueLine(Speaker.Mia, "Gravel, sand, charcoal — three layers, then boil three minutes. The filter clears what you can see. The boiling kills what you can't."))),

                // 3 — Sudden rainstorm → lean-to shelter.
                new StoryScene(NightSetting.ForestAtNight, LifeHack.LeanToShelter,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "Rain's coming. Branches at forty-five degrees and we stay dry."),
                        new DialogueLine(Speaker.Leo, "How much leaf cover are we talking?"),
                        new DialogueLine(Speaker.Mia, "Thirty centimetres, minimum. The angle sheds the water, the thickness keeps it out. Any less and you'll feel every drop."))),

                // 4 — Lost after dark → North Star navigation.
                new StoryScene(NightSetting.ForestAtNight, LifeHack.StarNavigation,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "We're turned around. Good news — the sky's been a map far longer than maps have."),
                        new DialogueLine(Speaker.Leo, "Which one's the right star? They all look the same to me."),
                        new DialogueLine(Speaker.Mia, "The North Star. It sits over true north and never wanders. Find it, and you'll never walk in circles again."))),

                // 5 — Leo gets a cut → plantain antiseptic + pine-needle tea.
                new StoryScene(NightSetting.ForestAtNight, LifeHack.FieldFirstAid,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Leo, "Ow. It's not deep, it just really wants attention."),
                        new DialogueLine(Speaker.Mia, "Hold still. Crushed plantain leaf — nature's antiseptic. Pine-needle tea after, for the vitamin C."),
                        new DialogueLine(Speaker.Leo, "The weeds are medicine. What else have I been stepping on this whole time?"))),
            };
        }
    }
}
