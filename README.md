# IslandQuest — Core Puzzle (Match-3) — SDD Workspace

This is the start of the Unity implementation for IslandQuest, built using a
**spec-driven development (SDD)** workflow combined with **strict TDD**
(Red → Green → Review) from Task 4 onward: a failing test is written and
confirmed to fail *for the right reason* before any production code exists,
then the minimal implementation is added, then the implementation is
reviewed against the spec's acceptance criteria — iterating until a review
pass finds nothing — before a task is marked done.

## What's here

```
specs/core-puzzle/
  requirements.md   — what the system must do, traced to the GDD
  design.md          — how it's built, and why (architecture decisions,
                        plus a correction log of mistakes caught and fixed
                        before they reached production code)
  tasks.md            — ordered task list with verification notes per task

Assets/Scripts/Match3/
  TileType.cs         — the 6 nature-themed tile colors (GDD §7.2)
  Tile.cs              — single board-cell value type
  BoardConfig.cs       — generation settings, validated at construction
  Board.cs             — grid state container, swap primitive
  MatchFinder.cs       — match-3 detection (the rules' source of truth)
  BoardGenerator.cs    — builds a board that's matchless and solvable
  MatchGroup.cs         — one connected blob of matched same-type cells
  MatchResolver.cs      — clusters MatchFinder's flat cells into MatchGroups
  BoosterRules.cs        — color -> booster mapping (GDD §7.2, verbatim)
  SwapEngine.cs           — validates, commits, or reverts a player's swap
  CascadeEngine.cs        — gravity, refill, cascade loop, combo bonus

verify/
  Program.cs                          — dependency-free assertion suite
  IslandQuest.Core.Verify.csproj      — links the real Match3/*.cs files
  NuGet.Config                        — sources cleared (offline build)
```

## Why this structure

`Assets/Scripts/Match3/*.cs` contains **zero `UnityEngine` references** on
purpose. It's plain C# game rules — board generation, match detection,
cascades — kept separate from any MonoBehaviour/presentation code. That
means:

1. **You can drop this folder straight into a Unity 6.3 LTS project** at
   `Assets/Scripts/Match3/` and it will compile as-is, no changes needed.
2. **It's independently testable without opening Unity.** This sandbox has
   no Unity Editor and no NuGet access (confirmed — `nuget.org` returns 403
   under the current network policy), so `verify/` is a small hand-rolled
   assertion harness instead of xUnit/NUnit. It compiles and runs the *exact*
   same files Unity will use (via `<Compile Include>`, not a copy), so a
   passing run is a real guarantee, not a simulation.

## Running the verification suite

```bash
cd verify
dotnet run
```

Expected output: `67 passed, 0 failed`. See `specs/core-puzzle/tasks.md` →
"How Task N was verified" sections for what each check covers. (On a machine
with no standalone `dotnet` on PATH, use the SDK bundled with Unity — see
`CLAUDE.md` for the exact invocation.)

## Status — Milestone 1 (Core Puzzle) complete ✅

- [x] **Task 1** — Core data model, match detection, board generation.
- [x] **Task 2** — Match group classification (`MatchGroup`, `MatchResolver`,
      `BoosterRules`).
- [x] **Task 3** — Swap validation & commit/revert (`SwapEngine`).
- [x] **Task 4** — Gravity, refill, cascade loop, combo bonus
      (`CascadeEngine`). First task built with strict RED→GREEN→review TDD.
- [x] **Task 5** — Booster spawn & activation (`BoosterActivation`), GDD §7.2
      effects.
- [x] **Task 6** — Credit bag collection & mid-level bonus-bag drops.
- [x] **Task 7** — Level objectives, completion, star rating & payout.
- [x] **Task 8** — Lives (hearts) system with regeneration.
- [x] **Task 9** — Unity presentation layer (`BoardController`,
      `BoardTileView`). Playtested in Unity 6.3 LTS: grid render, drag-swap,
      cascade playback, and manual booster activation all confirmed on screen.
- [x] **Task 10** — `ScriptableObject` level data (Island 1, Levels 1–5).
- [x] **Task 11** — Manual booster activation via swap (Requirement 5c).

All 67 checks pass against the real compiled source (verified, not assumed),
and the Unity layer has been playtested in the Editor. Several design mistakes
(speculative booster-shape rule; speculative matchless-refill rule; a spent
manually-activated booster re-firing its own color) were caught and corrected
against the GDD / in review *before* or *as* they were implemented — see
design.md's correction log.

## Next step

Milestone 1's core engine is done. The remaining M1 content target from GDD
§12.1 is the full **30 levels** (only Island 1, Levels 1–5 exist so far).
Beyond that, GDD §12.1 lists later milestones — Story/Night mode, IAP, ads,
DifficultyAI near-miss tuning, and survival-tip delivery — each of which will
get its own spec. The next phase is chosen at the start of the next branch.
