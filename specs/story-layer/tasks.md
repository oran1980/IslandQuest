# Tasks — Story Layer (Milestone 2)

Vertical, independently-verifiable slices for M2 (GDD §12.1: "Day/night mode,
story scenes, Mia & Leo dialogue, campfire moment"). Numbered fresh for this
spec (M2-1, M2-2, …) to avoid colliding with core-puzzle Tasks 1–16.

**Process:** every task follows `specs/PROCESS.md` (RED → GREEN → REFACTOR →
REVIEW), same as M1 from Task 4 on. Engine tasks are verify-tested (plain C#,
zero `UnityEngine`); presentation tasks are Editor-playtested in Unity 6.3 LTS
(like Task 9 / Task 16b). A task isn't `[x]` until the full `verify/` suite is
green *and* a review pass is done — keep the per-task "How M2-N was verified"
log format (actual RED failure + GREEN fix + what review checked).

Engine-first ordering: the credit economy is the spine everything else gates
on, so it comes first; the Unity presentation + M1↔M2 wiring comes last.

---

- [ ] **M2-1. Green credit balance & economy** (Requirement 1)
  - `IslandQuest.Economy.CreditManager`: `Balance`, `Earn(int)`,
    `TrySpend(int) → bool` (never negative), `CanAfford(int)`, `AwardBonus(int)`,
    over an `ICreditStore` seam (in-memory `CreditStore` now; future
    `Core/SaveSystem` later — mirrors M1's `ILevelRecordStore`). Plain C#,
    verify-tested.

- [ ] **M2-2. Story actions & the §4.3 cost table** (Requirement 2)
  - `StoryAction` (enum + cost/context lookup) transcribed verbatim from GDD
    §4.3 (campfire 30 / bridge 50 / cave 80 / passage 120 / animal 40 / chest
    60), each carrying its emotional-moment + life-hack context. A verify test
    pins the costs to the table. Data; verify-tested.

- [ ] **M2-3. Mia & Leo dialogue** (Requirement 3)
  - `DialogueLine { Speaker, Text }`, `Speaker { Mia, Leo }`,
    `DialogueSequence` with a cursor (`Current`, `HasNext`, `Advance`,
    `SkipToEnd`). Plain C#, verify-tested.

- [ ] **M2-4. Story scene model + Act 1 authoring incl. campfire**
    (Requirement 5 crit. 1, 4)
  - `StoryScene` (setting, gated `StoryAction`, `DialogueSequence`, `LifeHack`
    ref, optional bonus) + a `NightSetting` enum (§5.2) + a `LifeHack` enum
    (§3.4 Act 1). Author the **campfire scene** (and the rest of Act 1's
    triggers as data). Data; verify-tested (campfire has action=LightCampfire,
    cost 30, bow-drill hack, Mia+Leo lines).

- [ ] **M2-5. Story sequencing + credit gate** (Requirement 5 crit. 2–3, 5)
  - `IslandQuest.Story.StoryManager`: Act 1 scenes + cursor, `CurrentScene`,
    `TryPerformAction()` (charges via `CreditManager.TrySpend`, advances +
    awards bonus on success; blocks + reports insufficient credits on failure).
    Depends on M2-1/2/4. Plain C#, verify-tested — the "return to puzzle or buy"
    fork (GDD §4.2) is the key case.

- [ ] **M2-6. Day/Night mode state machine** (Requirement 4 crit. 1–3)
  - `IslandQuest.Story.DayNightController` (state half): `Mode { Day, Night }`,
    starts Day, `ToNight()`/`ToDay()`, and a transition payload exposing the
    credit balance for the §5.3 hand-off. Plain C#, verify-tested; the cutscene
    visual is M2-7.

- [ ] **M2-7. Unity presentation — day/night + story scene UI**
    (Requirement 4 crit. 4, Requirement 5 presentation)
  - MonoBehaviours + scene(s): Day→Night transition beat (credit read), a
    story-scene view (setting backdrop, Mia/Leo, tap-through dialogue box, the
    credit-gated action button with an "insufficient credits" state), and the
    **campfire scene** playable end-to-end. Wire `GameFlowController` (M1) so a
    level win feeds `LevelResult.CreditPayout` into `CreditManager` and a "go to
    night/story" entry switches modes. Procedural-first (Task 16b style);
    Mia/Leo art is a drop-in seam. Editor-playtested.

---

## Open sequencing question (confirm before M2-7)

M2-1 through M2-6 are pure engine and can proceed straight away under TDD. M2-7
is the big Unity slice and, like Task 16b, needs the Editor for its playtest and
carries the art-asset dependency for Mia/Leo. Recommended: build M2-1…M2-6
first (all verify-green), then scope M2-7's scene/wiring with a playtest pass.

## How M2-N was verified

_(Appended per task as they're completed, following the M1 log format —
actual RED failure, GREEN fix, REVIEW findings.)_
