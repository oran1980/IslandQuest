using System;

namespace IslandQuest.Economy
{
    /// <summary>Where the green-credit balance lives (GDD §11.2:
    /// "Credit balance, all transactions, local persistence"). Kept behind an
    /// interface so an in-memory store works now and a real persistence layer (a
    /// future <c>Core/SaveSystem</c>, out of scope) drops in later — the same
    /// seam M1 used for <c>ILevelRecordStore</c>.</summary>
    public interface ICreditStore
    {
        int Balance { get; set; }
    }

    /// <summary>In-memory <see cref="ICreditStore"/> — holds the balance for the
    /// current run only (resets on app restart). Intentional for M2; persistence
    /// is a <c>Core/SaveSystem</c> concern not built yet. Plain C#, no
    /// UnityEngine, so it stays verify-testable.</summary>
    public sealed class CreditStore : ICreditStore
    {
        public int Balance { get; set; }
    }

    /// <summary>
    /// The green-credit economy (Story Layer Requirement 1 / GDD §4): the single
    /// currency bridging the puzzle loop (earned per level, M1
    /// <c>LevelResult.CreditPayout</c>) and the story loop (spent on Mia's
    /// actions, GDD §4.3). Every balance mutation goes through here; nothing else
    /// touches a raw balance. Plain C# — the presentation layer reads/drives it.
    /// </summary>
    public sealed class CreditManager
    {
        private readonly ICreditStore _store;

        public CreditManager() : this(new CreditStore()) { }

        public CreditManager(ICreditStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Current spendable green-credit balance.</summary>
        public int Balance => _store.Balance;

        /// <summary>Add credits earned by completing a puzzle level (GDD §4.2).</summary>
        public void Earn(int amount)
        {
            RequirePositive(amount);
            _store.Balance += amount;
        }

        /// <summary>Add bonus credits from a treasure chest / story reveal
        /// (GDD §4.2). Same effect as <see cref="Earn"/>; named apart so call
        /// sites read as the distinct GDD source.</summary>
        public void AwardBonus(int amount)
        {
            RequirePositive(amount);
            _store.Balance += amount;
        }

        /// <summary>True if the balance can currently cover <paramref name="cost"/>
        /// — lets the story/UI gate an action before attempting it. Does not
        /// mutate the balance.</summary>
        public bool CanAfford(int cost)
        {
            RequirePositive(cost);
            return _store.Balance >= cost;
        }

        /// <summary>Spend on a story action (GDD §4.3). Deducts exactly
        /// <paramref name="cost"/> and returns true if affordable; otherwise
        /// leaves the balance untouched and returns false (it never goes
        /// negative — the "return to puzzle or buy credits" fork, GDD §4.2).</summary>
        public bool TrySpend(int cost)
        {
            RequirePositive(cost);
            if (_store.Balance < cost)
                return false;
            _store.Balance -= cost;
            return true;
        }

        private static void RequirePositive(int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount), "Credit amounts must be positive.");
        }
    }
}
