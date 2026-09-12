using System;
using System.Collections.Generic;
using System.Linq;
using IslandQuest.Match3;
using IslandQuest.Economy;
using IslandQuest.Story;

// Dependency-free verification harness for Task 1 (design.md §5 explains why:
// NuGet restore is blocked in this sandbox, so this stands in for xUnit/NUnit).
// Each Check_* method is one scenario from tasks.md's "How Task 1 was verified"
// list. Failures throw immediately with a descriptive message and a non-zero
// exit code; nothing here mutates shared state across checks.

int passed = 0;
int failed = 0;

void Run(string name, Action check)
{
    try
    {
        check();
        passed++;
        Console.WriteLine($"  PASS  {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"  FAIL  {name}");
        Console.WriteLine($"        {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

void AssertThrows<TException>(Action action, string message) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }
    throw new Exception($"expected {typeof(TException).Name} but none was thrown: {message}");
}

Console.WriteLine("IslandQuest.Match3 — Task 1 verification");
Console.WriteLine("=========================================");

Run("Board has correct dimensions and only allowed tile types", () =>
{
    var config = new BoardConfig(rows: 9, columns: 9, seed: 1);
    var board = BoardGenerator.Generate(config);

    Assert(board.Rows == 9, $"expected 9 rows, got {board.Rows}");
    Assert(board.Columns == 9, $"expected 9 columns, got {board.Columns}");

    var allowed = new HashSet<TileType>(config.AllowedTileTypes);
    for (int r = 0; r < board.Rows; r++)
        for (int c = 0; c < board.Columns; c++)
            Assert(allowed.Contains(board[r, c].Type), $"tile at ({r},{c}) has disallowed type {board[r, c].Type}");
});

Run("500 generated boards across sizes/seeds are all matchless", () =>
{
    var sizesToTry = new (int rows, int cols)[] { (9, 9), (3, 3), (4, 6) };
    int checkedCount = 0;

    foreach (var (rows, cols) in sizesToTry)
    {
        for (int seed = 0; seed < 500 / sizesToTry.Length; seed++)
        {
            var config = new BoardConfig(rows: rows, columns: cols, seed: seed);
            var board = BoardGenerator.Generate(config);
            Assert(!MatchFinder.HasAnyMatch(board),
                $"board ({rows}x{cols}, seed {seed}) was generated with a pre-existing match");
            checkedCount++;
        }
    }

    Assert(checkedCount >= 495, $"expected to check ~500 boards, only checked {checkedCount}");
});

Run("Same seed produces an identical board", () =>
{
    var config = new BoardConfig(seed: 42);
    var boardA = BoardGenerator.Generate(config);
    var boardB = BoardGenerator.Generate(config);

    for (int r = 0; r < boardA.Rows; r++)
        for (int c = 0; c < boardA.Columns; c++)
            Assert(boardA[r, c].Type == boardB[r, c].Type,
                $"mismatch at ({r},{c}): {boardA[r, c].Type} vs {boardB[r, c].Type} for the same seed");
});

Run("Different seeds produce different boards", () =>
{
    var boardA = BoardGenerator.Generate(new BoardConfig(seed: 1));
    var boardB = BoardGenerator.Generate(new BoardConfig(seed: 2));

    bool anyDifference = false;
    for (int r = 0; r < boardA.Rows && !anyDifference; r++)
        for (int c = 0; c < boardA.Columns && !anyDifference; c++)
            if (boardA[r, c].Type != boardB[r, c].Type)
                anyDifference = true;

    Assert(anyDifference, "two different seeds produced byte-identical boards — RNG may not be wired up");
});

Run("Every generated board has at least one legal move", () =>
{
    for (int seed = 0; seed < 50; seed++)
    {
        var board = BoardGenerator.Generate(new BoardConfig(seed: seed));
        bool hasLegalMove = false;

        for (int r = 0; r < board.Rows && !hasLegalMove; r++)
        {
            for (int c = 0; c < board.Columns && !hasLegalMove; c++)
            {
                if (c + 1 < board.Columns && CreatesMatchIfSwapped(board, r, c, r, c + 1)) hasLegalMove = true;
                if (r + 1 < board.Rows && CreatesMatchIfSwapped(board, r, c, r + 1, c)) hasLegalMove = true;
            }
        }

        Assert(hasLegalMove, $"board with seed {seed} has zero legal moves — player would be stuck");
    }
});

Run("Initial credit bag count stays within configured min/max", () =>
{
    var config = new BoardConfig(seed: 7, minInitialCreditBags: 1, maxInitialCreditBags: 2);

    for (int seed = 0; seed < 30; seed++)
    {
        var board = BoardGenerator.Generate(new BoardConfig(rows: 9, columns: 9, seed: seed,
            minInitialCreditBags: config.MinInitialCreditBags, maxInitialCreditBags: config.MaxInitialCreditBags));

        int bagCount = 0;
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
                if (board[r, c].HasCreditBag) bagCount++;

        Assert(bagCount >= config.MinInitialCreditBags && bagCount <= config.MaxInitialCreditBags,
            $"seed {seed}: bag count {bagCount} outside [{config.MinInitialCreditBags},{config.MaxInitialCreditBags}]");
    }
});

Run("MatchFinder: empty board (all distinct via diagonal pattern) has no matches", () =>
{
    // Hand-built 4x4 board, deliberately matchless: a 3-color diagonal-striped
    // pattern with no two same-type tiles ever adjacent in a row or column.
    var board = new Board(4, 4);
    TileType[] palette = { TileType.Flower, TileType.Leaf, TileType.Wave };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(palette[(r + c) % 3]);

    Assert(!MatchFinder.HasAnyMatch(board), "hand-built diagonal-striped board should have no matches");
    Assert(MatchFinder.FindMatchedCells(board).Count == 0, "matched-cells set should be empty");
});

Run("MatchFinder: detects an exact horizontal run of 3", () =>
{
    var board = new Board(3, 3);
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(TileType.Sun); // start everything matching, then break it up
    board[1, 0] = new Tile(TileType.Wave);
    board[2, 0] = new Tile(TileType.Wave);
    board[2, 2] = new Tile(TileType.Wave);
    // Row 0 = Sun,Sun,Sun (the run we want); row1 = Wave,Sun,Sun; row2 = Wave,Sun,Wave

    var matched = MatchFinder.FindMatchedCells(board);
    Assert(matched.Contains((0, 0)) && matched.Contains((0, 1)) && matched.Contains((0, 2)),
        "expected row 0 to be detected as a horizontal match");
});

Run("MatchFinder: detects an exact vertical run of 3", () =>
{
    var board = new Board(3, 3);
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(TileType.Coral);
    board[0, 1] = new Tile(TileType.Leaf);
    board[2, 1] = new Tile(TileType.Leaf);
    // Column 0 = Coral,Coral,Coral (the run we want)

    var matched = MatchFinder.FindMatchedCells(board);
    Assert(matched.Contains((0, 0)) && matched.Contains((1, 0)) && matched.Contains((2, 0)),
        "expected column 0 to be detected as a vertical match");
});

Run("MatchFinder: an L-shaped overlap counts each cell exactly once", () =>
{
    // Build a 3x3 board where (0,0) is the corner of both a horizontal run
    // (row 0) and a vertical run (column 0), all using the same TileType.
    var board = new Board(3, 3);
    var fill = new TileType[3, 3]
    {
        { TileType.Mushroom, TileType.Mushroom, TileType.Mushroom },
        { TileType.Mushroom, TileType.Sun,      TileType.Wave     },
        { TileType.Mushroom, TileType.Wave,     TileType.Sun      },
    };
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(fill[r, c]);

    var matched = MatchFinder.FindMatchedCells(board);
    // Column 0 (Mushroom x3) and row 0 (Mushroom x3) share cell (0,0).
    Assert(matched.Contains((0, 0)), "(0,0) should be in the matched set");
    Assert(matched.Contains((0, 1)) && matched.Contains((0, 2)), "rest of row 0 should match");
    Assert(matched.Contains((1, 0)) && matched.Contains((2, 0)), "rest of column 0 should match");
    Assert(matched.Count == 5, $"expected exactly 5 distinct matched cells (no double-counting), got {matched.Count}");
});

Run("Degenerate config (only 2 tile types) throws at construction", () =>
{
    bool threw = false;
    try
    {
        var _ = new BoardConfig(allowedTileTypes: new[] { TileType.Flower, TileType.Leaf });
    }
    catch (ArgumentException)
    {
        threw = true;
    }
    Assert(threw, "expected BoardConfig to reject fewer than 3 allowed tile types");
});

Console.WriteLine("--- Task 2: MatchGroup / MatchResolver / BoosterRules ---");

Run("Task2: straight run of 3 is one MatchGroup, not booster eligible", () =>
{
    // Only row 0 is a match (Sun,Sun,Sun); every column and every other row
    // is built to avoid any run of 3, including no incidental verticals.
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Sun,    TileType.Sun,    TileType.Sun,     TileType.Wave },
        { TileType.Wave,   TileType.Leaf,   TileType.Coral,   TileType.Sun  },
        { TileType.Leaf,   TileType.Wave,   TileType.Sun,     TileType.Coral },
        { TileType.Coral,  TileType.Sun,    TileType.Wave,    TileType.Leaf },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    var groups = MatchResolver.FindMatchGroups(board);
    Assert(groups.Count == 1, $"expected 1 group, got {groups.Count}");
    Assert(groups[0].Size == 3, $"expected size 3, got {groups[0].Size}");
    Assert(!groups[0].IsBoosterEligible, "a run of exactly 3 should not be booster eligible");
});

Run("Task2: straight run of 4 is one MatchGroup, booster eligible, correct booster", () =>
{
    // Only row 0 is a match (Leaf x4); columns and other rows avoid any run.
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Leaf,     TileType.Leaf,     TileType.Leaf,     TileType.Leaf },
        { TileType.Wave,     TileType.Sun,      TileType.Coral,    TileType.Mushroom },
        { TileType.Sun,      TileType.Coral,    TileType.Mushroom, TileType.Wave },
        { TileType.Coral,    TileType.Mushroom, TileType.Wave,     TileType.Sun },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    var groups = MatchResolver.FindMatchGroups(board);
    Assert(groups.Count == 1, $"expected 1 group, got {groups.Count}");
    Assert(groups[0].Size == 4, $"expected size 4, got {groups[0].Size}");
    Assert(groups[0].IsBoosterEligible, "a run of 4 should be booster eligible");
    Assert(groups[0].AwardedBooster == BoosterType.LeafWheel,
        $"expected LeafWheel for Leaf, got {groups[0].AwardedBooster}");
});

Run("Task2: L-shaped overlap merges into one group of size 5", () =>
{
    var board = new Board(3, 3);
    var fill = new TileType[3, 3]
    {
        { TileType.Mushroom, TileType.Mushroom, TileType.Mushroom },
        { TileType.Mushroom, TileType.Sun,      TileType.Wave     },
        { TileType.Mushroom, TileType.Wave,     TileType.Sun      },
    };
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(fill[r, c]);

    var groups = MatchResolver.FindMatchGroups(board);
    Assert(groups.Count == 1, $"expected the L-shape to merge into 1 group, got {groups.Count}");
    Assert(groups[0].Size == 5, $"expected size 5, got {groups[0].Size}");
    Assert(groups[0].IsBoosterEligible, "size 5 should be booster eligible");
    Assert(groups[0].AwardedBooster == BoosterType.SporeCloud,
        $"expected SporeCloud for Mushroom, got {groups[0].AwardedBooster}");
});

Run("Task2: two different-color matches produce two distinct groups", () =>
{
    var board = new Board(3, 6);
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 6; c++)
            board[r, c] = new Tile(TileType.Wave); // filler, will be overwritten below

    // Fill with a non-matching base pattern first.
    TileType[] palette = { TileType.Wave, TileType.Sun, TileType.Coral };
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 6; c++)
            board[r, c] = new Tile(palette[(r + c) % 3]);

    // Carve a horizontal run of Flower at row 0, cols 0-2.
    board[0, 0] = new Tile(TileType.Flower);
    board[0, 1] = new Tile(TileType.Flower);
    board[0, 2] = new Tile(TileType.Flower);
    // Carve an unrelated horizontal run of Flower at row 2, cols 3-5 (not touching the first).
    board[2, 3] = new Tile(TileType.Flower);
    board[2, 4] = new Tile(TileType.Flower);
    board[2, 5] = new Tile(TileType.Flower);

    var groups = MatchResolver.FindMatchGroups(board);
    var flowerGroups = new List<MatchGroup>();
    foreach (var g in groups) if (g.Type == TileType.Flower) flowerGroups.Add(g);

    Assert(flowerGroups.Count == 2, $"expected 2 separate Flower groups, got {flowerGroups.Count}");
    Assert(flowerGroups[0].Size == 3 && flowerGroups[1].Size == 3, "each Flower group should be size 3");
});

Run("Task2: BoosterRules covers all 6 tile types per GDD §7.2", () =>
{
    Assert(BoosterRules.ForTileType(TileType.Flower) == BoosterType.BloomBurst, "Flower -> BloomBurst");
    Assert(BoosterRules.ForTileType(TileType.Leaf) == BoosterType.LeafWheel, "Leaf -> LeafWheel");
    Assert(BoosterRules.ForTileType(TileType.Wave) == BoosterType.TidalClear, "Wave -> TidalClear");
    Assert(BoosterRules.ForTileType(TileType.Sun) == BoosterType.SolarFlare, "Sun -> SolarFlare");
    Assert(BoosterRules.ForTileType(TileType.Mushroom) == BoosterType.SporeCloud, "Mushroom -> SporeCloud");
    Assert(BoosterRules.ForTileType(TileType.Coral) == BoosterType.DeepSurge, "Coral -> DeepSurge");
});

Console.WriteLine("--- Task 3: SwapEngine ---");

Run("Task3: non-adjacent swap is rejected, board unchanged", () =>
{
    var board = BoardGenerator.Generate(new BoardConfig(seed: 100));
    var before = SnapshotTypes(board);

    var result = SwapEngine.TrySwap(board, 0, 0, 5, 5);

    Assert(!result.Success, "swap of non-adjacent tiles should fail");
    Assert(TypesEqual(before, SnapshotTypes(board)), "board should be unchanged after a rejected swap");
});

Run("Task3: out-of-bounds swap is rejected gracefully, no exception", () =>
{
    var board = BoardGenerator.Generate(new BoardConfig(seed: 101));
    var before = SnapshotTypes(board);

    var result = SwapEngine.TrySwap(board, 0, 0, -1, 0);

    Assert(!result.Success, "swap with an out-of-bounds coordinate should fail, not throw");
    Assert(TypesEqual(before, SnapshotTypes(board)), "board should be unchanged after a rejected out-of-bounds swap");
});

Run("Task3: adjacent swap with no resulting match is rejected and fully reverted", () =>
{
    // Hand-built board where swapping (0,0) and (0,1) creates no match.
    var board = new Board(3, 3);
    var fill = new TileType[3, 3]
    {
        { TileType.Flower, TileType.Leaf,  TileType.Wave },
        { TileType.Sun,     TileType.Coral, TileType.Mushroom },
        { TileType.Leaf,    TileType.Wave,  TileType.Sun },
    };
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(fill[r, c]);
    var before = SnapshotTypes(board);

    var result = SwapEngine.TrySwap(board, 0, 0, 0, 1);

    Assert(!result.Success, "swap that creates no match should be rejected");
    Assert(TypesEqual(before, SnapshotTypes(board)), "board should be byte-identical after revert");
});

Run("Task3: adjacent swap that creates a match is committed and reported", () =>
{
    // Hand-built board where swapping (0,2) and (1,2) creates a horizontal
    // match of Flower in row 0: Flower, Flower, [Flower swapped in].
    var board = new Board(3, 3);
    var fill = new TileType[3, 3]
    {
        { TileType.Flower, TileType.Flower, TileType.Wave   },
        { TileType.Sun,     TileType.Coral,  TileType.Flower },
        { TileType.Leaf,    TileType.Wave,   TileType.Sun    },
    };
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(fill[r, c]);

    var result = SwapEngine.TrySwap(board, 0, 2, 1, 2);

    Assert(result.Success, "swap that creates a match should succeed");
    Assert(board[0, 2].Type == TileType.Flower, "tile should have actually moved");
    Assert(board[1, 2].Type == TileType.Wave, "swapped-out tile should be in the other position");
    Assert(result.MatchGroups.Count == 1, $"expected 1 resulting match group, got {result.MatchGroups.Count}");
    Assert(result.MatchGroups[0].Size == 3, "resulting match group should be size 3");
});

Console.WriteLine("--- Task 4: CascadeEngine (gravity, refill, combo loop) ---");

Run("Task4: ComputeComboBonus — below 3 rounds gives no bonus", () =>
{
    var r0 = CascadeEngine.ComputeComboBonus(0);
    var r1 = CascadeEngine.ComputeComboBonus(1);
    var r2 = CascadeEngine.ComputeComboBonus(2);
    Assert(r0.BonusCredits == 0 && !r0.DropBonusBag, "0 rounds: no bonus");
    Assert(r1.BonusCredits == 0 && !r1.DropBonusBag, "1 round: no bonus");
    Assert(r2.BonusCredits == 0 && !r2.DropBonusBag, "2 rounds: no bonus");
});

Run("Task4: ComputeComboBonus — 3 or more rounds awards +10 and a bag", () =>
{
    var r3 = CascadeEngine.ComputeComboBonus(3);
    var r4 = CascadeEngine.ComputeComboBonus(4);
    var r10 = CascadeEngine.ComputeComboBonus(10);
    Assert(r3.BonusCredits == 10 && r3.DropBonusBag, "3 rounds: GDD §6.2 bonus");
    Assert(r4.BonusCredits == 10 && r4.DropBonusBag, "4 rounds: still the same flat bonus");
    Assert(r10.BonusCredits == 10 && r10.DropBonusBag, "10 rounds: still the same flat bonus (not scaling)");
});

Run("Task4: ClearGravityRefill compacts survivors downward in original order", () =>
{
    // 2-column board, row0=top..row4=bottom. Clear (2,0) only. Survivors in
    // col0, top-to-bottom skipping row2, are: row0, row1, row3, row4 — they
    // should compact to the bottom 4 rows (1,2,3,4) in that same order, and
    // row0 (now empty) should be refilled with a valid allowed type.
    var board = new Board(5, 2);
    board[0, 0] = new Tile(TileType.Flower);
    board[1, 0] = new Tile(TileType.Leaf);
    board[2, 0] = new Tile(TileType.Wave); // will be cleared
    board[3, 0] = new Tile(TileType.Sun);
    board[4, 0] = new Tile(TileType.Mushroom);
    for (int r = 0; r < 5; r++) board[r, 1] = new Tile(TileType.Coral); // untouched column

    var config = new BoardConfig(seed: 5); // default 9x9 — only AllowedTileTypes is used below; shape need not match the 2-column test board
    var rng = new Random(5);
    var cleared = new HashSet<(int Row, int Col)> { (2, 0) };

    CascadeEngine.ClearGravityRefill(board, cleared, config, rng);

    Assert(board[1, 0].Type == TileType.Flower, $"expected Flower at row1, got {board[1, 0].Type}");
    Assert(board[2, 0].Type == TileType.Leaf, $"expected Leaf at row2, got {board[2, 0].Type}");
    Assert(board[3, 0].Type == TileType.Sun, $"expected Sun at row3 (unmoved, below the gap), got {board[3, 0].Type}");
    Assert(board[4, 0].Type == TileType.Mushroom, $"expected Mushroom at row4 (unmoved), got {board[4, 0].Type}");
    Assert(Array.IndexOf(config.AllowedTileTypes, board[0, 0].Type) >= 0, "row0 should be refilled with a valid allowed type");
    // Untouched column must be completely unaffected.
    for (int r = 0; r < 5; r++)
        Assert(board[r, 1].Type == TileType.Coral, $"untouched column row{r} should be unaffected");
});

Run("Task4: ResolveCascade — simple single match terminates with no matches left", () =>
{
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Sun,    TileType.Sun,    TileType.Sun,     TileType.Wave },
        { TileType.Wave,   TileType.Leaf,   TileType.Coral,   TileType.Sun  },
        { TileType.Leaf,   TileType.Wave,   TileType.Sun,     TileType.Coral },
        { TileType.Coral,  TileType.Sun,    TileType.Wave,    TileType.Leaf },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    var config = new BoardConfig(rows: 4, columns: 4, seed: 11);
    var result = CascadeEngine.ResolveCascade(board, config, new Random(11));

    Assert(result.Rounds >= 1, "at least one round should have run, given an initial match");
    Assert(!MatchFinder.HasAnyMatch(board), "board must have zero matches once ResolveCascade returns");
});

Run("Task4: ResolveCascade — engineered chain reaction yields >= 2 deterministic rounds", () =>
{
    // Column 1 telescoping trick (other columns are inert filler using types
    // that never appear in column 1, so no horizontal interference, and
    // alternating so no vertical self-match): clearing the F,F,F run at
    // rows 2-4 leaves survivors L,L,L,C (rows 0,1,5,6) which compact to
    // rows 3,4,5 = L,L,L — a SECOND match guaranteed purely by compaction,
    // independent of any refill randomness. This proves the loop actually
    // re-scans and resolves chained matches, not just a single pass.
    var board = new Board(7, 3);
    var col1 = new TileType[] { TileType.Leaf, TileType.Leaf, TileType.Flower, TileType.Flower, TileType.Flower, TileType.Leaf, TileType.Coral };
    for (int r = 0; r < 7; r++)
    {
        board[r, 0] = new Tile(r % 2 == 0 ? TileType.Coral : TileType.Mushroom); // inert filler
        board[r, 1] = new Tile(col1[r]);
        board[r, 2] = new Tile(r % 2 == 0 ? TileType.Mushroom : TileType.Coral); // inert filler
    }

    var config = new BoardConfig(rows: 7, columns: 3, seed: 22);
    int bagsBefore = CountBags(board);
    var result = CascadeEngine.ResolveCascade(board, config, new Random(22));
    int bagsAfter = CountBags(board);

    Assert(result.Rounds >= 2, $"expected >= 2 rounds from the engineered compaction chain, got {result.Rounds}");
    Assert(!MatchFinder.HasAnyMatch(board), "board must have zero matches once ResolveCascade returns");

    // Invariant check (robust regardless of exact round count, which can
    // exceed 2 if refill happens to extend the chain further):
    if (result.Rounds >= 3)
    {
        Assert(result.BonusCredits == 10, $"{result.Rounds} rounds should award the GDD §6.2 bonus");
        Assert(result.BonusBagDropped, "result should report a bag was dropped for a 3+ round chain");
        Assert(bagsAfter == bagsBefore + 1, "a 3+ round chain should drop exactly one extra credit bag");
    }
    else
    {
        Assert(result.BonusCredits == 0, $"{result.Rounds} rounds should not award a bonus");
        Assert(!result.BonusBagDropped, "result should report no bag was dropped below the 3-round threshold");
        Assert(bagsAfter == bagsBefore, "no bonus bag should be dropped for fewer than 3 rounds");
    }
});

Run("Task4: ResolveCascade throws if maxRounds is exceeded (defensive cap)", () =>
{
    var board = new Board(3, 3);
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile(TileType.Sun); // entire board is one giant match

    var config = new BoardConfig(rows: 3, columns: 3, seed: 33);
    bool threw = false;
    try
    {
        CascadeEngine.ResolveCascade(board, config, new Random(33), maxRounds: 0);
    }
    catch (InvalidOperationException)
    {
        threw = true;
    }
    Assert(threw, "expected ResolveCascade to throw when it can't finish within maxRounds");
});

Console.WriteLine("--- Task 13: Scoring rule (10/tile x combo) ---");

Run("Task13: ScoringRules.RoundScore is 10/tile times the 1-based combo round", () =>
{
    Assert(ScoringRules.PointsPerTile == 10, $"expected 10 points per tile, got {ScoringRules.PointsPerTile}");
    Assert(ScoringRules.RoundScore(5, 1) == 50, $"5 tiles in round 1 should score 50, got {ScoringRules.RoundScore(5, 1)}");
    Assert(ScoringRules.RoundScore(5, 2) == 100, $"5 tiles in round 2 (x2) should score 100, got {ScoringRules.RoundScore(5, 2)}");
    Assert(ScoringRules.RoundScore(3, 3) == 90, $"3 tiles in round 3 (x3) should score 90, got {ScoringRules.RoundScore(3, 3)}");
    Assert(ScoringRules.RoundScore(0, 4) == 0, "zero tiles scores zero");
});

Run("Task13: ResolveCascade reports TilesCleared and a combo-weighted Score", () =>
{
    // Engineered telescoping board (mirrors the Task 4 chain test): a single
    // Leaf 4-match at row 0 clears, and compaction forces a second-round match,
    // so we get >=2 rounds and can assert Score reflects the combo weighting.
    var config = new BoardConfig(rows: 4, columns: 4, seed: 44);
    var board = new Board(4, 4);
    var cycle = new[] { TileType.Wave, TileType.Sun, TileType.Coral };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(cycle[(r + c) % 3]);
    // Force a Leaf row-of-4 at row 0.
    for (int c = 0; c < 4; c++)
        board[0, c] = new Tile(TileType.Leaf);

    var result = CascadeEngine.ResolveCascade(board, config, new Random(44));
    Assert(result.Rounds >= 1, "expected at least one cascade round");
    // A 4-match spawns a booster and keeps its spawn cell (Task 5), so the
    // row-of-4 clears 3 tiles that round — assert on that floor, not 4.
    Assert(result.TilesCleared >= 3, $"expected at least 3 tiles cleared, got {result.TilesCleared}");
    Assert(result.Score >= ScoringRules.PointsPerTile * result.TilesCleared,
        $"combo-weighted score {result.Score} must be >= flat 10/tile baseline for {result.TilesCleared} tiles");
});

Run("Task13: ResolveCascadeFrom scores the provided first-round cells at round 1", () =>
{
    var config = new BoardConfig(rows: 5, columns: 5, seed: 5);
    var board = new Board(5, 5);
    var cycle = new[] { TileType.Flower, TileType.Leaf, TileType.Wave };
    for (int r = 0; r < 5; r++)
        for (int c = 0; c < 5; c++)
            board[r, c] = new Tile(cycle[(r + c) % 3]);

    var cleared = new HashSet<(int Row, int Col)> { (2, 0), (2, 1), (2, 2), (2, 3), (2, 4) };
    var result = CascadeEngine.ResolveCascadeFrom(board, cleared, config, new Random(5));
    // The 5 provided cells clear in round 1 (x1) => at least 5 * 10 = 50 points.
    Assert(result.TilesCleared >= 5, $"expected >= 5 tiles cleared, got {result.TilesCleared}");
    Assert(result.Score >= 50, $"expected >= 50 points from the first-round clear, got {result.Score}");
});

Console.WriteLine("--- Task 14: Difficulty tiers, reward scaling, objective revision ---");

Run("Task14: every level has a Difficulty; tiers are non-decreasing and all three appear", () =>
{
    bool seenEasy = false, seenHard = false, seenVeryHard = false;
    int prev = -1;
    foreach (var level in LevelData.AllLevels)
    {
        int d = (int)level.Difficulty;
        Assert(d >= prev, $"{level.Name}: difficulty {level.Difficulty} dropped below the previous level's tier");
        prev = d;
        seenEasy |= level.Difficulty == Difficulty.Easy;
        seenHard |= level.Difficulty == Difficulty.Hard;
        seenVeryHard |= level.Difficulty == Difficulty.VeryHard;
    }
    Assert(seenEasy && seenHard && seenVeryHard, "all three difficulty tiers should appear across the 30 levels");
});

Run("Task14: reward = star base x difficulty multiplier (Easy x1, Hard x1.5, VeryHard x2)", () =>
{
    var objective = new LevelObjective(LevelObjectiveType.Score, target: 100);
    var thresholds = new LevelStarThresholds(oneStar: 100, twoStar: 200, threeStar: 300);
    var progress = new LevelProgress(score: 250, collected: 0, remainingCount: 0); // 2-star

    var easy = LevelEvaluator.Evaluate(objective, thresholds, progress, Difficulty.Easy);
    var hard = LevelEvaluator.Evaluate(objective, thresholds, progress, Difficulty.Hard);
    var vhard = LevelEvaluator.Evaluate(objective, thresholds, progress, Difficulty.VeryHard);

    Assert(easy.Stars == 2 && easy.CreditPayout == 35, $"easy 2-star should pay 35, got {easy.CreditPayout}");
    Assert(hard.CreditPayout == 53, $"hard 2-star should pay round(35 x 1.5)=53, got {hard.CreditPayout}");
    Assert(vhard.CreditPayout == 70, $"very-hard 2-star should pay 35 x 2=70, got {vhard.CreditPayout}");
});

Run("Task14: Evaluate without a difficulty defaults to Easy (x1) — back-compat", () =>
{
    var objective = new LevelObjective(LevelObjectiveType.Score, target: 100);
    var thresholds = new LevelStarThresholds(100, 200, 300);
    var progress = new LevelProgress(score: 350, collected: 0, remainingCount: 0); // 3-star
    var r = LevelEvaluator.Evaluate(objective, thresholds, progress);
    Assert(r.Stars == 3 && r.CreditPayout == 55, $"default (Easy) 3-star should pay 55, got {r.CreditPayout}");
});

Run("Task14: CollectBags replaces ClearBoard — positive bag target, complete when none remain", () =>
{
    var obj = new LevelObjective(LevelObjectiveType.CollectBags, target: 3);
    Assert(obj.Type == LevelObjectiveType.CollectBags, "type should be CollectBags");
    Assert(!obj.IsComplete(new LevelProgress(100, 0, remainingCount: 2)), "2 bags left => not complete");
    Assert(obj.IsComplete(new LevelProgress(100, 0, remainingCount: 0)), "0 bags left => complete");

    bool threw = false;
    try { new LevelObjective(LevelObjectiveType.CollectBags, target: 0); }
    catch (ArgumentOutOfRangeException) { threw = true; }
    Assert(threw, "CollectBags with target 0 should throw (needs a bag count)");
});

Run("Task14: CollectBags levels seed exactly their target bag count", () =>
{
    foreach (var level in LevelData.AllLevels)
    {
        if (level.Objective.Type != LevelObjectiveType.CollectBags) continue;
        Assert(level.Objective.Target >= 1, $"{level.Name}: CollectBags needs a positive target");
        Assert(level.MinInitialCreditBags == level.Objective.Target && level.MaxInitialCreditBags == level.Objective.Target,
            $"{level.Name}: should seed exactly {level.Objective.Target} bags, got [{level.MinInitialCreditBags},{level.MaxInitialCreditBags}]");
    }
});

Console.WriteLine("--- Task 15: Level session (moves, progress, win/loss) ---");

// Small helper: a synthetic cascade outcome for one move.
CascadeResult Move(int tiles, int score, int bags = 0) => new CascadeResult(1, 0, false, bags, tiles, score);
var Palette = new[] { TileType.Flower, TileType.Leaf, TileType.Wave, TileType.Sun, TileType.Mushroom, TileType.Coral };

Run("Task15: a level is won when the objective completes, with stars & difficulty-scaled credits", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.Score, 100), 5, Palette, difficulty: Difficulty.Hard);
    var session = new LevelSession(level, new LevelStarThresholds(100, 200, 300));

    Assert(session.Outcome == LevelOutcome.InProgress, "a fresh session is in progress");
    Assert(session.MovesRemaining == 5, $"starts with the full move budget, got {session.MovesRemaining}");

    session.ApplyMove(Move(tiles: 20, score: 250)); // 2-star score, objective (>=100) met
    Assert(session.Outcome == LevelOutcome.Won, "objective met => won");

    var result = session.GetResult();
    Assert(result.IsComplete, "won result should be complete");
    Assert(result.Stars == 2, $"score 250 vs 100/200/300 => 2 stars, got {result.Stars}");
    Assert(result.CreditPayout == 53, $"Hard 2-star => round(35 x 1.5)=53, got {result.CreditPayout}");
});

Run("Task15: completing a level always earns at least 1 star even below the 2-star threshold", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.Collect, 5), 5, Palette);
    var session = new LevelSession(level, new LevelStarThresholds(1000, 2000, 3000));
    session.ApplyMove(Move(tiles: 6, score: 60)); // collected 6 >= 5 => complete; score below all thresholds
    Assert(session.Outcome == LevelOutcome.Won, "collect objective met => won");
    Assert(session.GetResult().Stars == 1, $"completion floors at 1 star, got {session.GetResult().Stars}");
});

Run("Task15: a level is lost when moves run out with the objective incomplete", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.Score, 1000), 2, Palette);
    var session = new LevelSession(level, new LevelStarThresholds(1000, 2000, 3000));

    session.ApplyMove(Move(tiles: 5, score: 50));
    Assert(session.Outcome == LevelOutcome.InProgress, "one move left, objective not met yet");
    session.ApplyMove(Move(tiles: 5, score: 50)); // moves exhausted, score 100 < 1000
    Assert(session.Outcome == LevelOutcome.Lost, "moves exhausted & objective incomplete => lost");

    var result = session.GetResult();
    Assert(!result.IsComplete && result.Stars == 0 && result.CreditPayout == 0, "a loss yields no stars and no credits");
});

Run("Task15: progress accumulates across moves and the move budget counts down", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.Collect, 30), 4, Palette);
    var session = new LevelSession(level, new LevelStarThresholds(50, 100, 150));

    session.ApplyMove(Move(tiles: 8, score: 80, bags: 1));
    session.ApplyMove(Move(tiles: 7, score: 70));
    Assert(session.TilesCleared == 15, $"tiles 8+7=15, got {session.TilesCleared}");
    Assert(session.Score == 150, $"score 80+70=150, got {session.Score}");
    Assert(session.BagsCollected == 1, $"bags=1, got {session.BagsCollected}");
    Assert(session.MovesRemaining == 2, $"4-2=2 moves left, got {session.MovesRemaining}");
    Assert(session.Outcome == LevelOutcome.InProgress, "15 tiles < 30 target and moves remain");
});

Run("Task15: a CollectBags level completes once all its seeded bags are collected", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.CollectBags, 3), 10, Palette,
        minInitialCreditBags: 3, maxInitialCreditBags: 3);
    var session = new LevelSession(level, new LevelStarThresholds(50, 100, 150));

    session.ApplyMove(Move(tiles: 10, score: 100, bags: 2));
    Assert(session.Outcome == LevelOutcome.InProgress, "1 bag still uncollected");
    session.ApplyMove(Move(tiles: 10, score: 100, bags: 1));
    Assert(session.Outcome == LevelOutcome.Won, "all 3 bags collected => won");
});

Run("Task15: applying a move after the level is over is rejected", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.Score, 10), 3, Palette);
    var session = new LevelSession(level, new LevelStarThresholds(10, 20, 30));
    session.ApplyMove(Move(tiles: 5, score: 50)); // wins immediately
    bool threw = false;
    try { session.ApplyMove(Move(tiles: 5, score: 50)); }
    catch (InvalidOperationException) { threw = true; }
    Assert(threw, "ApplyMove should throw once the level is Won or Lost");
});

// --- Task 16a: per-level star thresholds (Requirement 7 crit. 8) ---
// The level-select/results UI (Task 16) needs each catalog level to carry its
// own star thresholds instead of the caller inventing them. These grade on
// score for every objective type (design.md §7.4).

Run("Task16: every catalog level carries non-null, monotonic star thresholds", () =>
{
    foreach (var level in LevelData.AllLevels)
    {
        var t = level.StarThresholds;
        Assert(t != null, $"level {level.LevelNumber} is missing StarThresholds");
        Assert(t.OneStar >= 1, $"level {level.LevelNumber}: 1-star threshold must be positive");
        Assert(t.TwoStar > t.OneStar, $"level {level.LevelNumber}: 2-star must exceed 1-star (meaningful reach)");
        Assert(t.ThreeStar > t.TwoStar, $"level {level.LevelNumber}: 3-star must exceed 2-star (meaningful reach)");
    }
});

Run("Task16: a Score level's 1-star threshold equals its score target (bare win = 1 star)", () =>
{
    foreach (var level in LevelData.AllLevels)
        if (level.Objective.Type == LevelObjectiveType.Score)
            Assert(level.StarThresholds.OneStar == level.Objective.Target,
                $"level {level.LevelNumber}: Score 1-star should equal the score target {level.Objective.Target}, got {level.StarThresholds.OneStar}");
});

Run("Task16: a Collect level's 1-star threshold equals target x 10 (min score to clear the objective)", () =>
{
    foreach (var level in LevelData.AllLevels)
        if (level.Objective.Type == LevelObjectiveType.Collect)
            Assert(level.StarThresholds.OneStar == level.Objective.Target * 10,
                $"level {level.LevelNumber}: Collect 1-star should equal target*10 = {level.Objective.Target * 10}, got {level.StarThresholds.OneStar}");
});

Run("Task16: a LevelSession can be built from a LevelData alone, using its own thresholds", () =>
{
    var level = new LevelData(1, 1, new LevelObjective(LevelObjectiveType.Score, 100), 5, Palette,
        difficulty: Difficulty.Hard);
    var session = new LevelSession(level);
    // Score of 200 hits the catalog-derived 2-star reach (100 * 1.4 = 140, 100 * 1.9 = 190).
    session.ApplyMove(Move(tiles: 20, score: 200));
    Assert(session.Outcome == LevelOutcome.Won, "objective (score >= 100) should be met");
    var result = session.GetResult();
    Assert(result.Stars == 3, $"score 200 should clear the 3-star reach (190), got {result.Stars} stars");
});

// --- Task 16b: in-memory level record store (best-star records) ---
// The level-select UI shows each level's best-star record. Persistence
// (Core/SaveSystem) is out of scope (Task 8), so this is a run-lifetime store
// behind an ILevelRecordStore seam a real SaveSystem can implement later.

Run("Task16: a fresh record store reports zero best-stars and zero total", () =>
{
    ILevelRecordStore store = new LevelRecordStore();
    Assert(store.GetBestStars(1) == 0, "an unplayed level should report 0 best stars");
    Assert(store.GetBestStars(30) == 0, "an unplayed level should report 0 best stars");
    Assert(store.TotalStars == 0, "a fresh store should have 0 total stars");
});

Run("Task16: recording stars stores them and reports the improvement", () =>
{
    var store = new LevelRecordStore();
    Assert(store.Record(3, 2) == true, "first record for a level is an improvement");
    Assert(store.GetBestStars(3) == 2, "best stars should be the recorded value");
    Assert(store.TotalStars == 2, "total stars should reflect the single record");
});

Run("Task16: a record only improves the best; a worse or equal replay never lowers it", () =>
{
    var store = new LevelRecordStore();
    store.Record(5, 3);
    Assert(store.Record(5, 1) == false, "a worse replay must not count as an improvement");
    Assert(store.GetBestStars(5) == 3, "best stars must not drop on a worse replay");
    Assert(store.Record(5, 3) == false, "an equal replay is not an improvement");
    Assert(store.GetBestStars(5) == 3, "best stars unchanged on an equal replay");
});

Run("Task16: TotalStars sums the best across distinct levels", () =>
{
    var store = new LevelRecordStore();
    store.Record(1, 1);
    store.Record(2, 3);
    store.Record(2, 2);   // worse replay, ignored
    store.Record(3, 2);
    Assert(store.TotalStars == 1 + 3 + 2, $"expected 6 total stars, got {store.TotalStars}");
});

Run("Task16: the record store rejects out-of-range level numbers and star counts", () =>
{
    var store = new LevelRecordStore();
    AssertThrows<ArgumentOutOfRangeException>(() => store.Record(0, 1), "level number 0 is invalid");
    AssertThrows<ArgumentOutOfRangeException>(() => store.Record(1, 4), "4 stars is out of range");
    AssertThrows<ArgumentOutOfRangeException>(() => store.Record(1, -1), "negative stars are invalid");
    AssertThrows<ArgumentOutOfRangeException>(() => store.GetBestStars(0), "level number 0 is invalid");
});

Console.WriteLine("--- Task 5: Booster spawn & activation ---");

Run("Task5: 4+ match spawns a booster tile on the topmost-leftmost cell, clears the rest", () =>
{
    // Same fixture as Task2's "straight run of 4" test: row 0 is Leaf x4, rest matchless.
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Leaf,     TileType.Leaf,     TileType.Leaf,     TileType.Leaf },
        { TileType.Wave,     TileType.Sun,      TileType.Coral,    TileType.Mushroom },
        { TileType.Sun,      TileType.Coral,    TileType.Mushroom, TileType.Wave },
        { TileType.Coral,    TileType.Mushroom, TileType.Wave,     TileType.Sun },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    var groups = MatchResolver.FindMatchGroups(board);
    var cleared = CascadeEngine.DetermineClearedCells(board, groups, new Random(1));

    Assert(!cleared.Contains((0, 0)), "topmost-leftmost cell of the group should be kept as the booster tile, not cleared");
    Assert(cleared.Contains((0, 1)) && cleared.Contains((0, 2)) && cleared.Contains((0, 3)), "the other 3 cells of the group should clear normally");
    Assert(board[0, 0].Type == TileType.Leaf, "booster tile should keep its original TileType");
    Assert(board[0, 0].Booster == BoosterType.LeafWheel, $"expected LeafWheel booster spawned, got {board[0, 0].Booster}");
});

Run("Task5: exactly-3 match does not spawn a booster", () =>
{
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Sun,    TileType.Sun,    TileType.Sun,     TileType.Wave },
        { TileType.Wave,   TileType.Leaf,   TileType.Coral,   TileType.Sun  },
        { TileType.Leaf,   TileType.Wave,   TileType.Sun,     TileType.Coral },
        { TileType.Coral,  TileType.Sun,    TileType.Wave,    TileType.Leaf },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    var groups = MatchResolver.FindMatchGroups(board);
    var cleared = CascadeEngine.DetermineClearedCells(board, groups, new Random(1));

    Assert(cleared.Contains((0, 0)) && cleared.Contains((0, 1)) && cleared.Contains((0, 2)), "all 3 cells of a non-eligible match should clear");
    Assert(board[0, 0].Booster == BoosterType.None && board[0, 1].Booster == BoosterType.None && board[0, 2].Booster == BoosterType.None,
        "no booster should spawn from an exactly-3 match");
});

Run("Task5: BoosterActivation.GetAffectedCells — BloomBurst clears the entire row", () =>
{
    var board = new Board(4, 5);
    board[2, 2] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    var affected = BoosterActivation.GetAffectedCells(board, 2, 2, new Random(1));
    Assert(affected.Count == 5, $"expected all 5 cells in row 2, got {affected.Count}");
    for (int c = 0; c < 5; c++)
        Assert(affected.Contains((2, c)), $"row 2 col {c} should be included");
});

Run("Task5: BoosterActivation.GetAffectedCells — LeafWheel clears the entire column", () =>
{
    var board = new Board(5, 4);
    board[2, 2] = new Tile(TileType.Leaf, BoosterType.LeafWheel);
    var affected = BoosterActivation.GetAffectedCells(board, 2, 2, new Random(1));
    Assert(affected.Count == 5, $"expected all 5 cells in column 2, got {affected.Count}");
    for (int r = 0; r < 5; r++)
        Assert(affected.Contains((r, 2)), $"col 2 row {r} should be included");
});

Run("Task5: BoosterActivation.GetAffectedCells — TidalClear clips a 3x3 zone at the corner", () =>
{
    var board = new Board(5, 5);
    board[0, 0] = new Tile(TileType.Wave, BoosterType.TidalClear);
    var affected = BoosterActivation.GetAffectedCells(board, 0, 0, new Random(1));
    Assert(affected.Count == 4, $"corner 3x3 zone clipped to the board should be 2x2=4 cells, got {affected.Count}");
    Assert(affected.Contains((0, 0)) && affected.Contains((0, 1)) && affected.Contains((1, 0)) && affected.Contains((1, 1)),
        "expected exactly the 2x2 corner");
});

Run("Task5: BoosterActivation.GetAffectedCells — SolarFlare clears every tile of the booster's own color", () =>
{
    var board = new Board(3, 3);
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            board[r, c] = new Tile((r + c) % 2 == 0 ? TileType.Sun : TileType.Wave);
    board[1, 1] = new Tile(TileType.Sun, BoosterType.SolarFlare);

    var affected = BoosterActivation.GetAffectedCells(board, 1, 1, new Random(1));
    int expectedSunCount = 0;
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            if (board[r, c].Type == TileType.Sun) expectedSunCount++;

    Assert(affected.Count == expectedSunCount, $"expected {expectedSunCount} Sun cells, got {affected.Count}");
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            if (board[r, c].Type == TileType.Sun)
                Assert(affected.Contains((r, c)), $"Sun cell ({r},{c}) should be included");
});

Run("Task5: BoosterActivation.GetAffectedCells — SporeCloud clears 5 distinct random cells", () =>
{
    var board = new Board(9, 9);
    board[4, 4] = new Tile(TileType.Mushroom, BoosterType.SporeCloud);
    var affected = BoosterActivation.GetAffectedCells(board, 4, 4, new Random(1));
    Assert(affected.Count == 5, $"expected exactly 5 cells, got {affected.Count}");
    foreach (var cell in affected)
        Assert(board.InBounds(cell.Row, cell.Col), $"cell {cell} should be within board bounds");
});

Run("Task5: BoosterActivation.GetAffectedCells — DeepSurge clears the bottom two rows", () =>
{
    var board = new Board(6, 4);
    board[3, 1] = new Tile(TileType.Coral, BoosterType.DeepSurge);
    var affected = BoosterActivation.GetAffectedCells(board, 3, 1, new Random(1));
    Assert(affected.Count == 8, $"expected 2 rows x 4 cols = 8 cells, got {affected.Count}");
    for (int c = 0; c < 4; c++)
    {
        Assert(affected.Contains((4, c)), $"row 4 col {c} should be included");
        Assert(affected.Contains((5, c)), $"row 5 col {c} should be included");
    }
});

Run("Task5: chain reaction — clearing a booster tile activates it and any booster it in turn clears", () =>
{
    // (0,0) holds a BloomBurst booster (row clear). (0,3), also in row 0,
    // holds a LeafWheel booster. Clearing (0,0) should activate BloomBurst
    // (clearing the rest of row 0, including (0,3)'s LeafWheel), which
    // should in turn activate and clear all of column 3.
    var board = new Board(4, 4);
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(TileType.Sun); // filler, irrelevant to the chain itself
    board[0, 0] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    board[0, 3] = new Tile(TileType.Leaf, BoosterType.LeafWheel);

    var triggerGroup = new MatchGroup(TileType.Flower, new HashSet<(int Row, int Col)> { (0, 0) });
    var cleared = CascadeEngine.DetermineClearedCells(board, new List<MatchGroup> { triggerGroup }, new Random(1));

    for (int c = 0; c < 4; c++)
        Assert(cleared.Contains((0, c)), $"BloomBurst should clear row 0 col {c}");
    for (int r = 0; r < 4; r++)
        Assert(cleared.Contains((r, 3)), $"chained LeafWheel should clear column 3 row {r}");
});

Run("Task5: ResolveCascade — a spawned booster tile survives its own round instead of being cleared", () =>
{
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Leaf,     TileType.Leaf,     TileType.Leaf,     TileType.Leaf },
        { TileType.Wave,     TileType.Sun,      TileType.Coral,    TileType.Mushroom },
        { TileType.Sun,      TileType.Coral,    TileType.Mushroom, TileType.Wave },
        { TileType.Coral,    TileType.Mushroom, TileType.Wave,     TileType.Sun },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    var config = new BoardConfig(rows: 4, columns: 4, seed: 44);
    CascadeEngine.ResolveCascade(board, config, new Random(44));

    int boosterCount = 0;
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            if (board[r, c].Booster == BoosterType.LeafWheel) boosterCount++;

    Assert(boosterCount == 1, $"expected exactly 1 surviving LeafWheel booster tile after resolving, got {boosterCount}");
});

Run("Task6: collecting a credit bag from a cleared match is reported in CascadeResult", () =>
{
    var board = new Board(4, 4);
    var fill = new TileType[4, 4]
    {
        { TileType.Flower, TileType.Flower, TileType.Flower, TileType.Flower },
        { TileType.Wave,   TileType.Sun,    TileType.Coral,   TileType.Mushroom },
        { TileType.Sun,    TileType.Coral,   TileType.Mushroom,TileType.Wave },
        { TileType.Coral,  TileType.Mushroom,TileType.Wave,   TileType.Sun },
    };
    for (int r = 0; r < 4; r++)
        for (int c = 0; c < 4; c++)
            board[r, c] = new Tile(fill[r, c]);

    board[0, 1] = board[0, 1].WithCreditBag(true);
    var config = new BoardConfig(rows: 4, columns: 4, seed: 50);
    var result = CascadeEngine.ResolveCascade(board, config, new Random(50));

    Assert(result.CreditBagsCollected == 1,
        $"expected 1 collected bag from the cleared match, got {result.CreditBagsCollected}");
});

Run("Task6: a bonus bag drop from a 3+ round chain is added to the board without losing collected bags", () =>
{
    var board = new Board(7, 3);
    var col1 = new TileType[] { TileType.Leaf, TileType.Leaf, TileType.Flower, TileType.Flower, TileType.Flower, TileType.Leaf, TileType.Coral };
    for (int r = 0; r < 7; r++)
    {
        board[r, 0] = new Tile(r % 2 == 0 ? TileType.Coral : TileType.Mushroom);
        board[r, 1] = new Tile(col1[r]);
        board[r, 2] = new Tile(r % 2 == 0 ? TileType.Mushroom : TileType.Coral);
    }

    board[2, 1] = board[2, 1].WithCreditBag(true);
    board[3, 1] = board[3, 1].WithCreditBag(true);

    var config = new BoardConfig(rows: 7, columns: 3, seed: 60);
    int bagsBefore = CountBags(board);
    var result = CascadeEngine.ResolveCascade(board, config, new Random(60));
    int bagsAfter = CountBags(board);

    Assert(result.Rounds >= 2, "expected the engineered board to produce a cascade");
    Assert(result.CreditBagsCollected >= 1, "expected at least one bag to be collected during the cascade");
    Assert(bagsAfter == bagsBefore - result.CreditBagsCollected + (result.BonusBagDropped ? 1 : 0),
        "board bag count after cascade should equal initial bags minus collected plus any bonus bag dropped");
});

Console.WriteLine("--- Task 7: Level objective, completion, and star rating ---");

Run("Task7: score objective completes when score threshold is reached", () =>
{
    var objective = new LevelObjective(LevelObjectiveType.Score, target: 50);
    var thresholds = new LevelStarThresholds(oneStar: 20, twoStar: 35, threeStar: 55);
    var progress = new LevelProgress(score: 52, collected: 0, remainingCount: 5);

    var result = LevelEvaluator.Evaluate(objective, thresholds, progress);

    Assert(result.IsComplete, "score objective should be complete when score meets the target");
    Assert(result.Stars == 2, $"expected 2 stars for score 52 with thresholds 20/35/55, got {result.Stars}");
    Assert(result.CreditPayout == 35, $"expected 35 credit payout for 2 stars, got {result.CreditPayout}");
});

Run("Task7: collect objective completes when collected items meet the target", () =>
{
    var objective = new LevelObjective(LevelObjectiveType.Collect, target: 10);
    var thresholds = new LevelStarThresholds(oneStar: 10, twoStar: 20, threeStar: 30);
    var progress = new LevelProgress(score: 0, collected: 10, remainingCount: 2);

    var result = LevelEvaluator.Evaluate(objective, thresholds, progress);

    Assert(result.IsComplete, "collect objective should be complete when collected count reaches target");
    Assert(result.Stars == 1, $"expected 1 star for collected 10 with thresholds 10/20/30, got {result.Stars}");
    Assert(result.CreditPayout == 20, $"expected 20 credit payout for 1 star, got {result.CreditPayout}");
});

Run("Task7: collect-bags objective completes only when remaining bag count is zero", () =>
{
    var objective = new LevelObjective(LevelObjectiveType.CollectBags, target: 3);
    var thresholds = new LevelStarThresholds(oneStar: 10, twoStar: 20, threeStar: 30);
    var progress = new LevelProgress(score: 75, collected: 0, remainingCount: 0);

    var result = LevelEvaluator.Evaluate(objective, thresholds, progress);

    Assert(result.IsComplete, "collect-bags objective should be complete when remaining bag count is zero");
    Assert(result.Stars == 3, $"expected 3 stars for score 75 with thresholds 10/20/30, got {result.Stars}");
    Assert(result.CreditPayout == 55, $"expected 55 credit payout for 3 stars (Easy default), got {result.CreditPayout}");
});

Run("Task7: incomplete objective returns zero stars and zero payout", () =>
{
    var objective = new LevelObjective(LevelObjectiveType.Score, target: 100);
    var thresholds = new LevelStarThresholds(oneStar: 20, twoStar: 40, threeStar: 60);
    var progress = new LevelProgress(score: 50, collected: 0, remainingCount: 1);

    var result = LevelEvaluator.Evaluate(objective, thresholds, progress);

    Assert(!result.IsComplete, "objective should not be complete when progress is below the target");
    Assert(result.Stars == 0, $"expected 0 stars for incomplete objective, got {result.Stars}");
    Assert(result.CreditPayout == 0, $"expected 0 credit payout for incomplete objective, got {result.CreditPayout}");
});

Console.WriteLine("--- Task 8: Lives (Hearts) system ---");

Run("Task8: constructor enforces positive max and current bounds", () =>
{
    Assert(new HeartSystem(5).MaxHearts == 5, "default max hearts should be 5");
    Assert(new HeartSystem(0, maxHearts: 3).CurrentHearts == 0, "current hearts can be zero");
});

Run("Task8: losing a heart decrements and schedules regeneration", () =>
{
    var now = DateTimeOffset.UtcNow;
    var hearts = new HeartSystem(3, maxHearts: 5, regenInterval: TimeSpan.FromMinutes(30));
    var next = hearts.LoseHeart(now);

    Assert(next.CurrentHearts == 2, "losing a heart should decrement current hearts");
    Assert(next.NextHeartAt.HasValue, "losing a heart below max should schedule a next heart time");
    Assert(next.GetTimeUntilNextHeart(now) == TimeSpan.FromMinutes(30), "next heart time should be exactly one interval away immediately after losing a heart");
});

Run("Task8: losing a heart at zero stays at zero and schedules regeneration", () =>
{
    var now = DateTimeOffset.UtcNow;
    var hearts = new HeartSystem(0, maxHearts: 5, regenInterval: TimeSpan.FromMinutes(30));
    var next = hearts.LoseHeart(now);

    Assert(next.CurrentHearts == 0, "current hearts should remain at zero when losing a heart with none left");
    Assert(next.NextHeartAt.HasValue, "regeneration should still be scheduled when hearts reach zero");
});

Run("Task8: regeneration before next heart does nothing", () =>
{
    var now = DateTimeOffset.UtcNow;
    var hearts = new HeartSystem(3, maxHearts: 5, regenInterval: TimeSpan.FromMinutes(30), nextHeartAt: now + TimeSpan.FromMinutes(15));
    var next = hearts.Regenerate(now + TimeSpan.FromMinutes(10));

    Assert(next.CurrentHearts == 3, "regeneration should not increase hearts before the interval elapses");
    Assert(next.NextHeartAt == hearts.NextHeartAt, "next heart time should remain unchanged before regeneration occurs");
});

Run("Task8: regeneration restores one heart after exact interval", () =>
{
    var now = DateTimeOffset.UtcNow;
    var hearts = new HeartSystem(2, maxHearts: 5, regenInterval: TimeSpan.FromMinutes(30), nextHeartAt: now + TimeSpan.FromMinutes(30));
    var next = hearts.Regenerate(now + TimeSpan.FromMinutes(30));

    Assert(next.CurrentHearts == 3, "one heart should restore after a single interval");
    Assert(next.NextHeartAt.HasValue, "next heart time should still be scheduled after restoring below max");
});

Run("Task8: regeneration caps at max hearts and clears next heart time", () =>
{
    var now = DateTimeOffset.UtcNow;
    var hearts = new HeartSystem(4, maxHearts: 5, regenInterval: TimeSpan.FromMinutes(30), nextHeartAt: now + TimeSpan.FromMinutes(30));
    var next = hearts.Regenerate(now + TimeSpan.FromMinutes(90));

    Assert(next.CurrentHearts == 5, "regeneration should cap hearts at max");
    Assert(next.NextHeartAt == null, "next heart time should be cleared once max hearts are reached");
});

Run("Task8: time until next heart returns zero when due", () =>
{
    var now = DateTimeOffset.UtcNow;
    var hearts = new HeartSystem(2, maxHearts: 5, regenInterval: TimeSpan.FromMinutes(30), nextHeartAt: now);
    Assert(hearts.GetTimeUntilNextHeart(now) == TimeSpan.Zero, "time until next heart should be zero when now is at or past next heart time");
});

Console.WriteLine("--- Task 10: ScriptableObject level data for Island 1, Levels 1-5 ---");

Run("Task10: Island 1 Levels 1-5 (the original sample) are present and valid", () =>
{
    // Island 1 now spans all 30 levels (GDD §8.2); this test still guards the
    // original Task 10 sample — the first five levels of the island.
    var island1 = LevelData.Island1Levels;
    Assert(island1.Count >= 5, $"expected at least 5 Island 1 levels, got {island1.Count}");

    for (int index = 0; index < 5; index++)
    {
        var level = island1[index];
        Assert(level.Island == 1, $"level {index + 1}: expected island 1, got {level.Island}");
        Assert(level.LevelNumber == index + 1, $"level {index + 1}: expected level number {index + 1}, got {level.LevelNumber}");
        Assert(level.MoveLimit > 0, $"level {index + 1}: move limit must be positive");
        Assert(level.AllowedTileTypes.Length >= 3, $"level {index + 1}: must allow at least 3 tile types");
        Assert(level.MinInitialCreditBags >= 0, $"level {index + 1}: min initial credit bags must be non-negative");
        Assert(level.MaxInitialCreditBags >= level.MinInitialCreditBags, $"level {index + 1}: max initial credit bags must be >= min");
    }
});

Console.WriteLine("--- Task 12: Full level catalog — Island 1, Levels 1-30 (GDD §8.2) ---");

Run("Task12: catalog is 30 levels, all Island 1, numbered 1-30 in order", () =>
{
    Assert(LevelData.LevelCount == 30, $"expected LevelCount 30, got {LevelData.LevelCount}");
    Assert(LevelData.AllLevels.Count == 30, $"expected 30 levels total, got {LevelData.AllLevels.Count}");
    for (int i = 0; i < LevelData.AllLevels.Count; i++)
    {
        var level = LevelData.AllLevels[i];
        Assert(level.Island == 1, $"{level.Name}: GDD §8.2 puts all M1 levels in Island 1, got island {level.Island}");
        Assert(level.LevelNumber == i + 1, $"entry {i}: expected level number {i + 1}, got {level.LevelNumber}");
    }
});

Run("Task12: IslandLevels(1) returns all 30 and Island1Levels matches it", () =>
{
    var island1 = LevelData.IslandLevels(1);
    Assert(island1.Count == 30, $"Island 1 should hold all 30 M1 levels, got {island1.Count}");
    Assert(LevelData.Island1Levels.Count == 30, $"Island1Levels should be all 30, got {LevelData.Island1Levels.Count}");
    // No other island has authored content in M1.
    Assert(LevelData.IslandLevels(2).Count == 0, "Island 2 (Levels 31-70) is a later milestone — not authored in M1");
});

Run("Task12: all 30 levels satisfy the per-level validity invariants", () =>
{
    foreach (var level in LevelData.AllLevels)
    {
        Assert(level.MoveLimit > 0, $"{level.Name}: move limit must be positive");
        Assert(level.AllowedTileTypes.Length >= 3, $"{level.Name}: must allow at least 3 tile types");
        Assert(level.MinInitialCreditBags >= 0, $"{level.Name}: min initial credit bags must be non-negative");
        Assert(level.MaxInitialCreditBags >= level.MinInitialCreditBags, $"{level.Name}: max credit bags must be >= min");
    }
});

Run("Task12: each (Island, LevelNumber) pair is unique", () =>
{
    var seen = new HashSet<(int, int)>();
    foreach (var level in LevelData.AllLevels)
        Assert(seen.Add((level.Island, level.LevelNumber)), $"duplicate level identity {level.Name}");
    Assert(seen.Count == 30, $"expected 30 unique level identities, got {seen.Count}");
});

Run("Task12: difficulty ramps across the 30 levels (Score & Collect targets non-decreasing in order)", () =>
{
    int prevScore = 0;
    int prevCollect = 0;
    foreach (var level in LevelData.AllLevels)
    {
        if (level.Objective.Type == LevelObjectiveType.Score)
        {
            Assert(level.Objective.Target >= prevScore, $"{level.Name}: Score target {level.Objective.Target} dropped below the previous Score level's {prevScore}");
            prevScore = level.Objective.Target;
        }
        else if (level.Objective.Type == LevelObjectiveType.Collect)
        {
            Assert(level.Objective.Target >= prevCollect, $"{level.Name}: Collect target {level.Objective.Target} dropped below the previous Collect level's {prevCollect}");
            prevCollect = level.Objective.Target;
        }
    }
});

Console.WriteLine("--- Task 11: Manual booster activation via swap (Requirement 5c) ---");

// Layer 1 — BoosterActivation.GetAffectedCellsAimed (pure, isolated)

Run("Task11: Aimed BloomBurst clears the target's row, not the booster's own row", () =>
{
    var board = FillerBoard(5, 5);
    var cells = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.BloomBurst, targetRow: 3, targetCol: 2, targetColor: board[3, 2].Type, rng: new Random(1));

    Assert(cells.Count == 5, $"expected 5 cells for a 5-wide target row, got {cells.Count}");
    for (int c = 0; c < 5; c++)
        Assert(cells.Contains((3, c)), $"expected target row cell (3,{c}) to be affected");
    for (int c = 0; c < 5; c++)
        Assert(!cells.Contains((0, c)), $"booster's own row (0,{c}) must not be affected by an aimed BloomBurst");
});

Run("Task11: Aimed LeafWheel clears the target's column", () =>
{
    var board = FillerBoard(5, 5);
    var cells = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.LeafWheel, targetRow: 1, targetCol: 4, targetColor: board[1, 4].Type, rng: new Random(1));

    Assert(cells.Count == 5, $"expected 5 cells for a 5-tall target column, got {cells.Count}");
    for (int r = 0; r < 5; r++)
        Assert(cells.Contains((r, 4)), $"expected target column cell ({r},4) to be affected");
});

Run("Task11: Aimed TidalClear is a 3x3 around the target, clipped at a board corner", () =>
{
    var board = FillerBoard(5, 5);
    var cells = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.TidalClear, targetRow: 0, targetCol: 0, targetColor: board[0, 0].Type, rng: new Random(1));

    Assert(cells.Count == 4, $"expected 4 cells for a corner-clipped 3x3, got {cells.Count}");
    foreach (var expected in new[] { (0, 0), (0, 1), (1, 0), (1, 1) })
        Assert(cells.Contains(expected), $"expected corner-clipped cell {expected} to be affected");
});

Run("Task11: Aimed SolarFlare reads the target's color, not the booster's own color", () =>
{
    var board = FillerBoard(4, 4);
    // Explicit colors: Flower is the target color; Sun stands in for the booster's own color.
    board[0, 0] = new Tile(TileType.Flower);
    board[2, 3] = new Tile(TileType.Flower);
    board[1, 1] = new Tile(TileType.Sun);
    board[3, 3] = new Tile(TileType.Sun);

    var cells = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.SolarFlare, targetRow: 0, targetCol: 0, targetColor: TileType.Flower, rng: new Random(1));

    Assert(cells.Contains((0, 0)) && cells.Contains((2, 3)), "aimed SolarFlare should include every target-color (Flower) cell");
    Assert(!cells.Contains((1, 1)) && !cells.Contains((3, 3)), "aimed SolarFlare must not include booster-color (Sun) cells");
    foreach (var cell in cells)
        Assert(board[cell.Row, cell.Col].Type == TileType.Flower, $"cell {cell} is not the target color");
});

Run("Task11: Aimed SolarFlare gives identical results across calls (color read is stable)", () =>
{
    var board = FillerBoard(4, 4);
    board[0, 0] = new Tile(TileType.Flower);
    board[2, 3] = new Tile(TileType.Flower);

    var first = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.SolarFlare, 0, 0, TileType.Flower, new Random(1));
    var second = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.SolarFlare, 0, 0, TileType.Flower, new Random(1));
    Assert(SameCells(first, second), "aimed SolarFlare should be deterministic with no mutation between calls");
});

Run("Task11: Aimed SporeCloud ignores the target and clears 5 distinct in-bounds cells", () =>
{
    var board = FillerBoard(5, 5);
    var cells = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.SporeCloud, targetRow: 2, targetCol: 2, targetColor: board[2, 2].Type, rng: new Random(3));

    Assert(cells.Count == 5, $"expected exactly 5 SporeCloud cells, got {cells.Count}");
    foreach (var cell in cells)
        Assert(board.InBounds(cell.Row, cell.Col), $"SporeCloud produced out-of-bounds cell {cell}");
});

Run("Task11: Aimed DeepSurge ignores the target and clears both bottom rows", () =>
{
    var board = FillerBoard(5, 5);
    var cells = BoosterActivation.GetAffectedCellsAimed(board, BoosterType.DeepSurge, targetRow: 0, targetCol: 0, targetColor: board[0, 0].Type, rng: new Random(1));

    Assert(cells.Count == 10, $"expected 10 cells (2 bottom rows x 5 cols), got {cells.Count}");
    for (int c = 0; c < 5; c++)
    {
        Assert(cells.Contains((3, c)) && cells.Contains((4, c)), $"expected bottom-two-rows cell in column {c}");
    }
    Assert(!cells.Contains((0, 0)), "DeepSurge must not be influenced by the target position");
});

// Layer 2 — SwapEngine.TryManualActivationSwap (orchestration decision)

Run("Task11: two adjacent BloomBursts trigger, anchored on the target cell's row", () =>
{
    var board = FillerBoard(5, 5);
    board[1, 3] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    board[2, 3] = new Tile(TileType.Flower, BoosterType.BloomBurst);

    // Swap source (1,3) with target (2,3): tie-break should pick the target row (2), not source row (1).
    var res = SwapEngine.TryManualActivationSwap(board, 1, 3, 2, 3, new Random(1));

    Assert(res.Triggered, "two adjacent BloomBursts should trigger manual activation");
    for (int c = 0; c < 5; c++)
        Assert(res.ClearedCells.Contains((2, c)), $"expected target row cell (2,{c}) cleared");
    Assert(!res.ClearedCells.Contains((1, 0)), "source cell's row must not be the anchor (tie-break picks target row)");
});

Run("Task11: a mixed booster pair does not trigger (falls through)", () =>
{
    var board = FillerBoard(5, 5);
    board[2, 1] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    board[2, 2] = new Tile(TileType.Leaf, BoosterType.LeafWheel);
    var mixed = SwapEngine.TryManualActivationSwap(board, 2, 1, 2, 2, new Random(1));
    Assert(!mixed.Triggered, "BloomBurst + non-BloomBurst booster must not trigger the combo");

    board[3, 1] = new Tile(TileType.Leaf, BoosterType.LeafWheel);
    board[3, 2] = new Tile(TileType.Leaf, BoosterType.LeafWheel);
    var twoLeaf = SwapEngine.TryManualActivationSwap(board, 3, 1, 3, 2, new Random(1));
    Assert(!twoLeaf.Triggered, "two non-BloomBurst boosters must not trigger the combo");
});

Run("Task11: booster + regular tile triggers, aimed through the regular tile's position", () =>
{
    var board = FillerBoard(5, 5);
    board[2, 2] = new Tile(TileType.Leaf, BoosterType.LeafWheel);
    // (2,3) is a plain filler tile — the "target" the booster is aimed through.
    var res = SwapEngine.TryManualActivationSwap(board, 2, 2, 2, 3, new Random(1));

    Assert(res.Triggered, "booster + non-booster swap should trigger manual activation");
    Assert(res.ClearedCells.Count == 5, $"LeafWheel aimed at column 3 should clear 5 cells, got {res.ClearedCells.Count}");
    for (int r = 0; r < 5; r++)
        Assert(res.ClearedCells.Contains((r, 3)), $"expected aimed column cell ({r},3) cleared");
});

Run("Task11: booster + regular triggers even when the swap would also form an ordinary match", () =>
{
    var board = FillerBoard(5, 5);
    // Set up so swapping the booster out of (0,2) drops a Wave there, forming Wave-Wave-Wave across row 0.
    board[0, 0] = new Tile(TileType.Wave);
    board[0, 1] = new Tile(TileType.Wave);
    board[0, 2] = new Tile(TileType.Leaf, BoosterType.LeafWheel);
    board[0, 3] = new Tile(TileType.Wave);
    Assert(!MatchFinder.HasAnyMatch(board), "precondition: board must have no pre-existing match");

    var res = SwapEngine.TryManualActivationSwap(board, 0, 2, 0, 3, new Random(1));

    Assert(res.Triggered, "manual activation must fire regardless of an incidental ordinary match (precedence)");
    // Proves the aimed (column-clear) path fired, not the ordinary row-match path.
    for (int r = 0; r < 5; r++)
        Assert(res.ClearedCells.Contains((r, 3)), $"expected aimed column cell ({r},3) cleared");
});

Run("Task11: two regular tiles never trigger manual activation", () =>
{
    var board = FillerBoard(5, 5);
    var res = SwapEngine.TryManualActivationSwap(board, 2, 2, 2, 3, new Random(1));
    Assert(!res.Triggered, "two non-booster tiles must never trigger manual activation");
});

Run("Task11: non-adjacent or out-of-bounds booster pairs do not trigger", () =>
{
    var board = FillerBoard(5, 5);
    board[0, 0] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    board[0, 3] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    var nonAdjacent = SwapEngine.TryManualActivationSwap(board, 0, 0, 0, 3, new Random(1));
    Assert(!nonAdjacent.Triggered, "non-adjacent booster pair must not trigger (adjacency guard)");

    var outOfBounds = SwapEngine.TryManualActivationSwap(board, 0, 0, 0, 99, new Random(1));
    Assert(!outOfBounds.Triggered, "out-of-bounds coordinates must not trigger (bounds guard)");
});

Run("Task11: an aimed SolarFlare is consumed and does not chain into its own color", () =>
{
    // Booster is Sun/SolarFlare; target is a Flower tile. Aimed effect should
    // clear Flower tiles (target color) plus the spent booster's own cell, but
    // must NOT sweep up Sun tiles via chain re-activation.
    var board = FillerBoard(5, 5);
    board[2, 2] = new Tile(TileType.Sun, BoosterType.SolarFlare);
    board[2, 3] = new Tile(TileType.Flower); // target (regular)
    board[0, 0] = new Tile(TileType.Flower);
    board[4, 4] = new Tile(TileType.Sun);    // a booster-color tile that must survive

    var res = SwapEngine.TryManualActivationSwap(board, 2, 2, 2, 3, new Random(1));
    Assert(res.Triggered, "SolarFlare booster + regular tile should trigger");
    Assert(res.ClearedCells.Contains((2, 3)), "the spent booster's landing cell must be cleared");
    Assert(res.ClearedCells.Contains((0, 0)), "a target-color (Flower) cell should be cleared");
    Assert(!res.ClearedCells.Contains((4, 4)), "a booster-color (Sun) cell must not be swept up");
});

// Layer 3 — end-to-end cascade integration

Run("Task11: manual-activation result feeds the full cascade loop (Rounds >= 2)", () =>
{
    var board = FillerBoard(5, 5);
    // Column 0 telescopes into a Wave-Wave-Wave vertical match once row 2 clears,
    // independent of refill randomness (same technique as Task 4's telescoping test).
    board[0, 0] = new Tile(TileType.Wave);
    board[1, 0] = new Tile(TileType.Wave);
    board[3, 0] = new Tile(TileType.Wave);
    board[4, 0] = new Tile(TileType.Sun);
    board[2, 0] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    board[2, 1] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    Assert(!MatchFinder.HasAnyMatch(board), "precondition: engineered board must start matchless");

    var config = new BoardConfig(rows: 5, columns: 5, seed: 7);
    var rngLocal = new Random(7);
    var swap = SwapEngine.TryManualActivationSwap(board, 2, 0, 2, 1, rngLocal);
    Assert(swap.Triggered, "precondition: BloomBurst pair should trigger");

    var result = CascadeEngine.ResolveCascadeFrom(board, swap.ClearedCells, config, rngLocal);
    Assert(result.Rounds >= 2, $"expected the telescoped match to produce a 2nd cascade round, got {result.Rounds}");
});

Run("Task11: credit bags on manually-cleared cells are counted", () =>
{
    var board = FillerBoard(5, 5);
    board[3, 2] = board[3, 2].WithCreditBag(true);

    var cleared = new HashSet<(int Row, int Col)> { (3, 0), (3, 1), (3, 2), (3, 3), (3, 4) };
    var config = new BoardConfig(rows: 5, columns: 5, seed: 5);
    var result = CascadeEngine.ResolveCascadeFrom(board, cleared, config, new Random(5));

    Assert(result.CreditBagsCollected >= 1, $"expected the bagged cleared cell to be counted, got {result.CreditBagsCollected}");
});

Run("Task11: original GetAffectedCells path is unchanged (regression guard)", () =>
{
    var board = FillerBoard(5, 5);
    board[2, 2] = new Tile(TileType.Flower, BoosterType.BloomBurst);
    var bloom = BoosterActivation.GetAffectedCells(board, 2, 2, new Random(1));
    Assert(bloom.Count == 5, "original BloomBurst should still clear its own full row");
    for (int c = 0; c < 5; c++)
        Assert(bloom.Contains((2, c)), $"original BloomBurst should include (2,{c})");

    var board2 = FillerBoard(4, 4);
    board2[1, 1] = new Tile(TileType.Sun, BoosterType.SolarFlare);
    board2[3, 3] = new Tile(TileType.Sun);
    var flare = BoosterActivation.GetAffectedCells(board2, 1, 1, new Random(1));
    foreach (var cell in flare)
        Assert(board2[cell.Row, cell.Col].Type == TileType.Sun, $"original SolarFlare should read its own color; {cell} is not Sun");
    Assert(flare.Contains((1, 1)) && flare.Contains((3, 3)), "original SolarFlare should include all Sun cells");
});

// --- M2-1: green credit balance & economy (Story Layer Requirement 1) ---
// The currency bridging the puzzle (earn) and story (spend) loops. Plain C#,
// zero UnityEngine; persistence behind an ICreditStore seam like M1's records.

Console.WriteLine("--- M2-1: Credit economy ---");

Run("M2-1: a fresh credit manager starts at zero", () =>
{
    var credits = new CreditManager();
    Assert(credits.Balance == 0, $"expected 0 starting balance, got {credits.Balance}");
});

Run("M2-1: earning credits adds to the balance", () =>
{
    var credits = new CreditManager();
    credits.Earn(20);
    credits.Earn(35);
    Assert(credits.Balance == 55, $"expected 55 after earning 20+35, got {credits.Balance}");
});

Run("M2-1: an affordable spend succeeds and deducts exactly the cost", () =>
{
    var credits = new CreditManager();
    credits.Earn(50);
    Assert(credits.TrySpend(30) == true, "spending 30 of 50 should succeed");
    Assert(credits.Balance == 20, $"expected 20 remaining, got {credits.Balance}");
});

Run("M2-1: an unaffordable spend is refused and leaves the balance untouched (never negative)", () =>
{
    var credits = new CreditManager();
    credits.Earn(20);
    Assert(credits.TrySpend(30) == false, "spending 30 of 20 should be refused");
    Assert(credits.Balance == 20, $"balance must be unchanged after a refused spend, got {credits.Balance}");
});

Run("M2-1: CanAfford reports affordability without mutating the balance", () =>
{
    var credits = new CreditManager();
    credits.Earn(30);
    Assert(credits.CanAfford(30) == true, "30 of 30 should be affordable");
    Assert(credits.CanAfford(31) == false, "31 of 30 should not be affordable");
    Assert(credits.Balance == 30, "CanAfford must not change the balance");
});

Run("M2-1: bonus credits (treasure/story) are added to the balance", () =>
{
    var credits = new CreditManager();
    credits.Earn(10);
    credits.AwardBonus(60);
    Assert(credits.Balance == 70, $"expected 70 after 10 + 60 bonus, got {credits.Balance}");
});

Run("M2-1: rejects non-positive earn/spend/bonus amounts", () =>
{
    var credits = new CreditManager();
    AssertThrows<ArgumentOutOfRangeException>(() => credits.Earn(0), "earn 0 is invalid");
    AssertThrows<ArgumentOutOfRangeException>(() => credits.Earn(-5), "earn negative is invalid");
    AssertThrows<ArgumentOutOfRangeException>(() => credits.TrySpend(0), "spend 0 is invalid");
    AssertThrows<ArgumentOutOfRangeException>(() => credits.AwardBonus(-1), "negative bonus is invalid");
});

Run("M2-1: balance is read through the injected store (SaveSystem seam)", () =>
{
    ICreditStore store = new CreditStore();
    store.Balance = 40;
    var credits = new CreditManager(store);
    Assert(credits.Balance == 40, "manager should reflect the store's balance");
    credits.Earn(10);
    Assert(store.Balance == 50, "earning should write through to the store");
});

// --- M2-2: story actions & the GDD §4.3 cost table (Story Layer Requirement 2) ---

Console.WriteLine("--- M2-2: Story actions & costs ---");

Run("M2-2: every story action's cost matches the GDD §4.3 table verbatim", () =>
{
    (StoryActionType type, int cost)[] expected =
    {
        (StoryActionType.LightCampfire, 30),
        (StoryActionType.CrossRopeBridge, 50),
        (StoryActionType.EnterHiddenCave, 80),
        (StoryActionType.UnlockSecretPassage, 120),
        (StoryActionType.RescueTrappedAnimal, 40),
        (StoryActionType.OpenTreasureChest, 60),
    };
    foreach (var (type, cost) in expected)
    {
        var action = StoryAction.For(type);
        Assert(action.Cost == cost, $"{type} should cost {cost} (§4.3), got {action.Cost}");
        Assert(action.Type == type, $"StoryAction.For({type}) should carry that type");
    }
});

Run("M2-2: every story action carries its §4.3 emotional-moment context", () =>
{
    foreach (StoryActionType type in Enum.GetValues(typeof(StoryActionType)))
    {
        var action = StoryAction.For(type);
        Assert(!string.IsNullOrWhiteSpace(action.EmotionalMoment),
            $"{type} should carry a non-empty emotional-moment context (§4.3 col 3)");
    }
    // Spot-check the campfire's context names the bow-drill lesson (§4.3).
    Assert(StoryAction.For(StoryActionType.LightCampfire).EmotionalMoment.Contains("bow-drill"),
        "the campfire action's context should reference the bow-drill technique");
});

// --- M2-3: Mia & Leo dialogue (Story Layer Requirement 3) ---

Console.WriteLine("--- M2-3: Dialogue ---");

Run("M2-3: a dialogue line carries a speaker and non-empty text", () =>
{
    var line = new DialogueLine(Speaker.Mia, "Watch this Leo — dry wood is everything.");
    Assert(line.Speaker == Speaker.Mia, "line should carry its speaker");
    Assert(line.Text.Contains("dry wood"), "line should carry its text");
    AssertThrows<ArgumentException>(() => new DialogueLine(Speaker.Leo, "  "), "blank text is invalid");
});

Run("M2-3: a sequence starts on its first line and advances one line at a time", () =>
{
    var seq = new DialogueSequence(
        new DialogueLine(Speaker.Mia, "First."),
        new DialogueLine(Speaker.Leo, "Second."),
        new DialogueLine(Speaker.Mia, "Third."));
    Assert(seq.Current.Text == "First.", "should start on the first line");
    Assert(seq.HasNext, "more lines should remain");
    seq.Advance();
    Assert(seq.Current.Text == "Second." && seq.Current.Speaker == Speaker.Leo, "advance should move to the next line");
    seq.Advance();
    Assert(seq.Current.Text == "Third.", "advance should reach the last line");
    Assert(!seq.HasNext, "no lines should remain at the end");
});

Run("M2-3: advancing past the last line is rejected", () =>
{
    var seq = new DialogueSequence(new DialogueLine(Speaker.Mia, "Only line."));
    Assert(!seq.HasNext, "a single-line sequence has no next");
    AssertThrows<InvalidOperationException>(() => seq.Advance(), "advancing past the end should throw");
});

Run("M2-3: SkipToEnd jumps straight to the last line", () =>
{
    var seq = new DialogueSequence(
        new DialogueLine(Speaker.Mia, "A"),
        new DialogueLine(Speaker.Leo, "B"),
        new DialogueLine(Speaker.Mia, "C"));
    seq.SkipToEnd();
    Assert(seq.Current.Text == "C", "SkipToEnd should land on the last line");
    Assert(!seq.HasNext, "no lines remain after SkipToEnd");
});

Run("M2-3: an empty dialogue sequence is rejected", () =>
{
    AssertThrows<ArgumentException>(() => new DialogueSequence(), "a sequence needs at least one line");
});

// --- M2-4: story scene model + all five Act 1 scenes (Story Layer Requirement 5 crit. 1, 4) ---

Console.WriteLine("--- M2-4: Story scenes & Act 1 ---");

Run("M2-4: a scene bundles setting, life hack, dialogue, optional action & bonus", () =>
{
    var seq = new DialogueSequence(new DialogueLine(Speaker.Mia, "..."));
    var gated = new StoryScene(NightSetting.Campfire, LifeHack.BowDrillFire, seq,
        StoryAction.For(StoryActionType.LightCampfire));
    Assert(gated.IsGated, "a scene with an action is gated");
    Assert(gated.Action!.Cost == 30, "the campfire gate should cost 30");
    Assert(gated.Setting == NightSetting.Campfire && gated.LifeHack == LifeHack.BowDrillFire, "scene carries setting + hack");

    var free = new StoryScene(NightSetting.JungleRiver, LifeHack.WaterFiltration, seq);
    Assert(!free.IsGated && free.Action == null, "a scene with no action is a free teaching beat");
    Assert(free.BonusCredits == 0, "bonus defaults to zero");
});

Run("M2-4: Act 1 has exactly five scenes, campfire first and gated at 30", () =>
{
    var act1 = StoryScene.Act1;
    Assert(act1.Count == 5, $"Act 1 should have 5 scenes, got {act1.Count}");
    var campfire = act1[0];
    Assert(campfire.Setting == NightSetting.Campfire, "the first Act 1 scene is the campfire");
    Assert(campfire.LifeHack == LifeHack.BowDrillFire, "the campfire teaches bow-drill fire");
    Assert(campfire.IsGated && campfire.Action!.Type == StoryActionType.LightCampfire && campfire.Action.Cost == 30,
        "the campfire is gated by Light-a-campfire (30)");
});

Run("M2-4: the other four Act 1 scenes are free teaching beats covering §3.4's hacks", () =>
{
    var act1 = StoryScene.Act1;
    var expectedHacks = new HashSet<LifeHack>
    {
        LifeHack.WaterFiltration, LifeHack.LeanToShelter, LifeHack.StarNavigation, LifeHack.FieldFirstAid
    };
    for (int i = 1; i < act1.Count; i++)
    {
        Assert(!act1[i].IsGated, $"Act 1 scene {i} should be a free teaching beat (no credit gate)");
        Assert(expectedHacks.Remove(act1[i].LifeHack), $"unexpected/duplicate hack at scene {i}: {act1[i].LifeHack}");
    }
    Assert(expectedHacks.Count == 0, "all four non-campfire §3.4 hacks should be covered");
});

Run("M2-4: every Act 1 scene has a Mia+Leo dialogue (Leo asks the follow-up, §3.3)", () =>
{
    foreach (var scene in StoryScene.Act1)
    {
        Assert(scene.Dialogue.LineCount >= 2, "each scene should have at least two dialogue lines (§3.5)");
        bool hasMia = false, hasLeo = false;
        // Read the lines view without disturbing the sequence's playback cursor.
        foreach (var line in scene.Dialogue.Lines)
        {
            hasMia |= line.Speaker == Speaker.Mia;
            hasLeo |= line.Speaker == Speaker.Leo;
        }
        Assert(hasMia && hasLeo, $"scene {scene.Setting} should feature both Mia and Leo");
    }
});

// --- M2-5: story sequencing + credit gate (Story Layer Requirement 5 crit. 2-3, 5) ---

Console.WriteLine("--- M2-5: Story sequencing & gate ---");

Run("M2-5: a new story manager starts on Act 1's first scene (the campfire)", () =>
{
    var mgr = new StoryManager(new CreditManager());
    Assert(!mgr.IsComplete, "a fresh act is not complete");
    Assert(mgr.CurrentScene!.Setting == NightSetting.Campfire, "should start on the campfire scene");
});

Run("M2-5: a gated scene the player can't afford is blocked, leaving state untouched", () =>
{
    var credits = new CreditManager();
    credits.Earn(20);   // campfire costs 30
    var mgr = new StoryManager(credits);
    Assert(mgr.TryAdvanceScene() == SceneOutcome.InsufficientCredits, "20 < 30 should block the campfire");
    Assert(credits.Balance == 20, "a blocked action must not spend");
    Assert(mgr.CurrentScene!.Setting == NightSetting.Campfire, "a blocked action must not advance the scene");
});

Run("M2-5: an affordable gated scene charges its cost and advances", () =>
{
    var credits = new CreditManager();
    credits.Earn(30);
    var mgr = new StoryManager(credits);
    Assert(mgr.TryAdvanceScene() == SceneOutcome.Advanced, "30 >= 30 should light the campfire");
    Assert(credits.Balance == 0, "the campfire should charge exactly 30");
    Assert(mgr.CurrentScene!.LifeHack == LifeHack.WaterFiltration, "should advance to Act 1 scene 2");
});

Run("M2-5: a free teaching beat advances with no spend", () =>
{
    var credits = new CreditManager();
    credits.Earn(5);
    var seq = new DialogueSequence(new DialogueLine(Speaker.Mia, "A"), new DialogueLine(Speaker.Leo, "B"));
    var free = new StoryScene(NightSetting.JungleRiver, LifeHack.WaterFiltration, seq);
    var mgr = new StoryManager(credits, new List<StoryScene> { free });
    Assert(mgr.TryAdvanceScene() == SceneOutcome.Advanced, "a free beat always advances");
    Assert(credits.Balance == 5, "a free beat must not spend");
    Assert(mgr.IsComplete, "advancing the only scene completes the act");
});

Run("M2-5: resolving a scene awards its bonus credits", () =>
{
    var credits = new CreditManager();
    var seq = new DialogueSequence(new DialogueLine(Speaker.Mia, "Treasure!"));
    var bonusScene = new StoryScene(NightSetting.SecretRuins, LifeHack.StarNavigation, seq, bonusCredits: 60);
    var mgr = new StoryManager(credits, new List<StoryScene> { bonusScene });
    mgr.TryAdvanceScene();
    Assert(credits.Balance == 60, $"resolving a bonus scene should award 60, got {credits.Balance}");
});

Run("M2-5: advancing through every scene completes the act; advancing again throws", () =>
{
    var credits = new CreditManager();
    credits.Earn(30);   // enough for the one gated (campfire) scene
    var mgr = new StoryManager(credits);
    int guard = 0;
    while (!mgr.IsComplete && guard++ < 10)
        Assert(mgr.TryAdvanceScene() == SceneOutcome.Advanced, "each Act 1 scene should advance");
    Assert(mgr.IsComplete && mgr.CurrentScene == null, "the act should be complete with no current scene");
    AssertThrows<InvalidOperationException>(() => mgr.TryAdvanceScene(), "advancing a completed act should throw");
});

// --- M2-6: day/night mode state machine (Story Layer Requirement 4 crit. 1-3) ---

Console.WriteLine("--- M2-6: Day/Night mode ---");

Run("M2-6: the world starts in Day mode (the puzzle world)", () =>
{
    var dn = new DayNightController(new CreditManager());
    Assert(dn.Mode == WorldMode.Day, "the entry mode is Day (GDD §5.1)");
});

Run("M2-6: going to night switches mode and reports the credit balance for the §5.3 hand-off", () =>
{
    var credits = new CreditManager();
    credits.Earn(85);
    var dn = new DayNightController(credits);
    var transition = dn.ToNight();
    Assert(dn.Mode == WorldMode.Night, "should now be Night");
    Assert(transition.From == WorldMode.Day && transition.To == WorldMode.Night, "transition should record Day → Night");
    Assert(transition.CreditBalance == 85, $"the hand-off should surface the 85-credit balance (§5.3), got {transition.CreditBalance}");
});

Run("M2-6: returning to day switches back to the puzzle world", () =>
{
    var dn = new DayNightController(new CreditManager());
    dn.ToNight();
    var transition = dn.ToDay();
    Assert(dn.Mode == WorldMode.Day, "should be back in Day");
    Assert(transition.From == WorldMode.Night && transition.To == WorldMode.Day, "transition should record Night → Day");
});

Run("M2-6: switching to the mode you're already in is rejected", () =>
{
    var dn = new DayNightController(new CreditManager());
    AssertThrows<InvalidOperationException>(() => dn.ToDay(), "already Day → ToDay should throw");
    dn.ToNight();
    AssertThrows<InvalidOperationException>(() => dn.ToNight(), "already Night → ToNight should throw");
});

Console.WriteLine("=========================================");
Console.WriteLine($"{passed} passed, {failed} failed");

if (failed > 0)
    Environment.Exit(1);

// --- local helpers for Task 11 ---
// Matchless filler: (r+c)%3 cycles Flower/Leaf/Wave so no two 4-adjacent cells
// share a type, guaranteeing HasAnyMatch(board) == false before any override.
static Board FillerBoard(int rows, int cols)
{
    var board = new Board(rows, cols);
    var cycle = new[] { TileType.Flower, TileType.Leaf, TileType.Wave };
    for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            board[r, c] = new Tile(cycle[(r + c) % 3]);
    return board;
}

static bool SameCells(IEnumerable<(int Row, int Col)> a, IEnumerable<(int Row, int Col)> b)
{
    var sa = new HashSet<(int Row, int Col)>(a);
    var sb = new HashSet<(int Row, int Col)>(b);
    return sa.SetEquals(sb);
}

// --- local helpers for Task 3 snapshot comparisons ---
static TileType[,] SnapshotTypes(Board board)
{
    var snapshot = new TileType[board.Rows, board.Columns];
    for (int r = 0; r < board.Rows; r++)
        for (int c = 0; c < board.Columns; c++)
            snapshot[r, c] = board[r, c].Type;
    return snapshot;
}

static bool TypesEqual(TileType[,] a, TileType[,] b)
{
    if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
        return false;
    for (int r = 0; r < a.GetLength(0); r++)
        for (int c = 0; c < a.GetLength(1); c++)
            if (a[r, c] != b[r, c])
                return false;
    return true;
}

static int CountBags(Board board)
{
    int count = 0;
    for (int r = 0; r < board.Rows; r++)
        for (int c = 0; c < board.Columns; c++)
            if (board[r, c].HasCreditBag) count++;
    return count;
}

// --- local helper, mirrors BoardGenerator's private HasLegalMove logic so the
// harness can verify it independently without reaching into private members ---
static bool CreatesMatchIfSwapped(Board board, int r1, int c1, int r2, int c2)
{
    var temp = board[r1, c1];
    board[r1, c1] = board[r2, c2];
    board[r2, c2] = temp;

    bool createsMatch = MatchFinder.HasAnyMatch(board);

    var swapBack = board[r1, c1];
    board[r1, c1] = board[r2, c2];
    board[r2, c2] = swapBack;

    return createsMatch;
}
