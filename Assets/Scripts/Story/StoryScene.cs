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
                new StoryScene(NightSetting.Campfire, LifeHack.BowDrillFire,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "No matches out here, Leo. But friction and patience make fire."),
                        new DialogueLine(Speaker.Leo, "Rubbing sticks together? Does that actually work?"),
                        new DialogueLine(Speaker.Mia, "Watch this — dry wood is everything. Spin the spindle fast, catch the ember in the tinder.")),
                    StoryAction.For(StoryActionType.LightCampfire)),

                // 2 — Dirty stream found → 3-layer filtration + boiling.
                new StoryScene(NightSetting.JungleRiver, LifeHack.WaterFiltration,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "That stream's moving, but 'moving' isn't 'safe', Leo."),
                        new DialogueLine(Speaker.Leo, "So we can't just drink it? It looks clean enough."),
                        new DialogueLine(Speaker.Mia, "Gravel, sand, charcoal — three layers. Then boil three minutes. Filtering clears the grit; boiling kills what you can't see."))),

                // 3 — Sudden rainstorm → lean-to shelter.
                new StoryScene(NightSetting.ForestAtNight, LifeHack.LeanToShelter,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "Rain's coming in. We build a lean-to — branches at forty-five degrees."),
                        new DialogueLine(Speaker.Leo, "How much cover do we actually need?"),
                        new DialogueLine(Speaker.Mia, "At least thirty centimetres of leaves. The angle sheds the water, the thickness keeps it out."))),

                // 4 — Lost after dark → North Star navigation.
                new StoryScene(NightSetting.ForestAtNight, LifeHack.StarNavigation,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Mia, "We're turned around. But the sky's a map if you know how to read it."),
                        new DialogueLine(Speaker.Leo, "Which star do we even follow?"),
                        new DialogueLine(Speaker.Mia, "The North Star sits over true north. Find it, and you'll never walk in circles again."))),

                // 5 — Leo gets a cut → plantain antiseptic + pine-needle tea.
                new StoryScene(NightSetting.ForestAtNight, LifeHack.FieldFirstAid,
                    new DialogueSequence(
                        new DialogueLine(Speaker.Leo, "Ow — it's not deep, but it stings."),
                        new DialogueLine(Speaker.Mia, "Hold still. Crushed plantain leaf — nature's antiseptic. Pine-needle tea later for the vitamin C."),
                        new DialogueLine(Speaker.Leo, "Leaves as medicine. Okay, that's actually amazing."))),
            };
        }
    }
}
