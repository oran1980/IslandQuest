namespace IslandQuest.Match3
{
    /// <summary>
    /// A single board cell. Deliberately a readonly struct (value type): the
    /// swap-then-validate-then-revert flow that lands in Task 3 needs cheap,
    /// alias-free copies of board state, and structs give that for free.
    /// </summary>
    public readonly struct Tile
    {
        public TileType Type { get; }
        public BoosterType Booster { get; }
        public bool HasCreditBag { get; }

        public Tile(TileType type, BoosterType booster = BoosterType.None, bool hasCreditBag = false)
        {
            Type = type;
            Booster = booster;
            HasCreditBag = hasCreditBag;
        }

        public Tile WithCreditBag(bool hasBag) => new Tile(Type, Booster, hasBag);

        public Tile WithBooster(BoosterType booster) => new Tile(Type, booster, HasCreditBag);

        public override string ToString() => HasCreditBag ? $"{Type}*" : Type.ToString();
    }
}
