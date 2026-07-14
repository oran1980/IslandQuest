# CLAUDE.md — IslandQuest Core Puzzle

This file is read automatically by Claude Code at the start of a session in
this directory. It exists so a new session has full context immediately,
without the person re-explaining the project.

## What this project is

Unity match-3 puzzle game core logic (`IslandQuest`), built from a GDD
(`specs/` has the derived spec docs; the original .docx GDD is not in this
folder — ask the user to re-upload it if you need to check something against
the primary source that isn't already captured in `requirements.md`).

## Mandatory process — read these before writing any code

1. **`specs/PROCESS.md`** — the RED → GREEN → REFACTOR → REVIEW cycle. This
   is not optional or a suggestion: write a failing test first (ideally a
   compile failure, referencing a type/method that doesn't exist yet),
   confirm it actually fails, then write minimal code to pass it, then
   review the result against the spec before marking anything done.
2. **`specs/core-puzzle/requirements.md`** — EARS-style acceptance criteria,
   traced to GDD sections. Every requirement should map to a test.
3. **`specs/core-puzzle/design.md`** — architecture decisions and a
   correction log (mistakes caught and fixed before they hit production
   code — keep adding to this log, don't just quietly fix things).
4. **`specs/core-puzzle/tasks.md`** — the ordered task list. Each task has a
   "How Task N was verified" section with the actual RED failure and GREEN
   fix, not just an end-state summary. Follow that format for new tasks.

## Current status

Tasks 1–5 done (board generation, match detection/grouping, swap
validation, cascade/gravity/combo loop, booster spawn & activation —
row/column/3×3-zone/one-color/5-random/bottom-two-rows clears per GDD §7.2,
mapping obtained from the user and now recorded in `requirements.md`'s
Requirement 5). **Next: Task 6 — credit bag collection & mid-level spawns.**
See `tasks.md` for the full remaining list (Tasks 6–10).

One open item from Task 4's review: `CascadeEngine.ClearGravityRefill`
destroys credit-bag info on cleared cells with no record kept. Task 6 will
need to read `MatchGroup.Cells` for `HasCreditBag` *before* calling
`ClearGravityRefill`, not after. Documented in `tasks.md` under Task 4.

## Key architecture facts

- `Assets/Scripts/Match3/*.cs` is plain C#, **zero `UnityEngine`
  references** — intentional, so it's independently testable and drops
  straight into a real Unity project unchanged. Don't introduce a
  UnityEngine dependency here; that only happens in Task 9's
  `BoardController` (a MonoBehaviour, deliberately last).
- Row convention: **row 0 is the top of the board**, increasing row index
  moves down (matches gravity direction and `BoardGenerator`'s fill order).
- No NuGet access in the original sandbox this was built in — `verify/`
  is a dependency-free hand-rolled assertion harness instead of xUnit,
  compiling the *real* `Assets/Scripts/Match3/*.cs` files via
  `<Compile Include>` (not copies). **If NuGet works in this environment**
  (it should, on a normal dev machine), feel free to migrate `verify/` to
  a real xUnit project — that's an improvement, not a requirement, and
  should go through the same RED/GREEN process (port tests one at a time,
  confirm still green, don't rewrite and hope).

## Running tests

```bash
cd verify
dotnet run
```

Expect `26 passed, 0 failed` as of Task 4. If NuGet access exists here and
`verify/NuGet.Config` (which clears all package sources) causes problems for
a future task that actually needs a package, that file is safe to remove —
it was only there to force an offline build in a network-restricted sandbox.

## Working style

- Keep `tasks.md`'s per-task verification log format: what was RED, what
  made it GREEN, what the review pass found (even minor things — "reviewed,
  no issues" should still list what was checked).
- Don't mark a task `[x]` until the full suite is green *and* a review pass
  is done.
- If something in `requirements.md`/`design.md` turns out to be wrong once
  you're implementing it, fix the doc and note it in design.md's correction
  log — don't just silently code around it.
