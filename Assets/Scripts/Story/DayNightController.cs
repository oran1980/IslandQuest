using System;
using IslandQuest.Economy;

namespace IslandQuest.Story
{
    /// <summary>The two worlds (GDD §5): Day is the puzzle world (earn credits),
    /// Night is the story world (spend them).</summary>
    public enum WorldMode
    {
        Day,
        Night,
    }

    /// <summary>A mode switch — what it was, what it became, and the credit
    /// balance at that instant. The Day → Night switch surfaces the balance for
    /// the GDD §5.3 hand-off beat ("You have 85 credits. What will Mia do
    /// next?").</summary>
    public readonly struct DayNightTransition
    {
        public WorldMode From { get; }
        public WorldMode To { get; }
        public int CreditBalance { get; }

        public DayNightTransition(WorldMode from, WorldMode to, int creditBalance)
        {
            From = from;
            To = to;
            CreditBalance = creditBalance;
        }
    }

    /// <summary>
    /// The day/night mode <b>state machine</b> (GDD §11.2: "Mode switching,
    /// lighting changes, transition cutscenes" — this is the state half; the
    /// 8-second sunset cutscene + lighting are the presentation layer, M2-7).
    /// Starts in Day; toggles to Night to enter the story and back to Day to
    /// play more (GDD §4.2, §5.3). Plain C#, verify-testable.
    /// </summary>
    public sealed class DayNightController
    {
        private readonly CreditManager _credits;

        public DayNightController(CreditManager credits)
        {
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            Mode = WorldMode.Day;   // the puzzle world is the entry mode (§5.1)
        }

        public WorldMode Mode { get; private set; }

        /// <summary>Enter the story world. Returns the transition carrying the
        /// current credit balance for the §5.3 hand-off. Throws if already Night.</summary>
        public DayNightTransition ToNight() => SwitchTo(WorldMode.Night);

        /// <summary>Return to the puzzle world. Throws if already Day.</summary>
        public DayNightTransition ToDay() => SwitchTo(WorldMode.Day);

        private DayNightTransition SwitchTo(WorldMode target)
        {
            if (Mode == target)
                throw new InvalidOperationException($"Already in {target} mode.");

            var from = Mode;
            Mode = target;
            return new DayNightTransition(from, target, _credits.Balance);
        }
    }
}
