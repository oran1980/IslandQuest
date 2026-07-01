# Development Process — TDD + Review Cycle

Applies from Task 4 onward. (Tasks 1–3 were verified thoroughly but tests
were written alongside/after implementation, not strictly RED-first — noted
here for transparency rather than silently rewriting history. Retrofit on
request.)

Every task now follows this loop, and a task is not marked `[x]` in
`tasks.md` until all four steps are clean:

## 1. RED
Write the test(s) for the task's acceptance criteria *before* the
production code exists. The test must fail — ideally by failing to compile
(referencing a type/method that doesn't exist yet), which is the strongest
possible proof the test isn't accidentally passing for the wrong reason.
Run it and paste the actual failure — not a description of what the failure
would be.

## 2. GREEN
Write the minimum production code needed to make the failing test(s) pass.
Re-run the full suite (not just the new test) — a regression in Tasks 1–3
caused by Task 4's code is still a failure. Nothing is "done" until the
whole suite is green together.

## 3. REFACTOR
If implementing the minimal version exposed duplication or an awkward
shape (e.g. reusable logic that should be extracted into its own file),
clean it up now, with the test suite as the safety net proving behavior
didn't change.

## 4. REVIEW
Re-read every changed/new file with fresh eyes against the requirement it
claims to satisfy. Look specifically for: edge cases the tests didn't
cover, claims in design.md that are stronger than what the code actually
guarantees, resource/performance issues, and inconsistent naming or
duplicated logic. Fix anything found, then go back to step 2 (re-run the
suite) and repeat step 4 until a pass finds nothing. Document what was
found and fixed in `tasks.md` next to that task, even for small things —
"reviewed, no issues" is only credible if there's a visible trail of what
was actually checked.
