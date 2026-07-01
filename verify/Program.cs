using System;
using System.Collections.Generic;
using System.Linq;
using IslandQuest.Match3;

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

Console.WriteLine("=========================================");
Console.WriteLine($"{passed} passed, {failed} failed");

if (failed > 0)
    Environment.Exit(1);

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
