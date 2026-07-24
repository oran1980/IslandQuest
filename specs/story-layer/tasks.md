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

- [x] **M2-1. Green credit balance & economy** (Requirement 1)
  - `IslandQuest.Economy.CreditManager`: `Balance`, `Earn(int)`,
    `TrySpend(int) → bool` (never negative), `CanAfford(int)`, `AwardBonus(int)`,
    over an `ICreditStore` seam (in-memory `CreditStore` now; future
    `Core/SaveSystem` later — mirrors M1's `ILevelRecordStore`). Plain C#,
    verify-tested. See "How M2-1 was verified" below.

- [x] **M2-2. Story actions & the §4.3 cost table** (Requirement 2)
  - `StoryAction` (enum + cost/context lookup) transcribed verbatim from GDD
    §4.3 (campfire 30 / bridge 50 / cave 80 / passage 120 / animal 40 / chest
    60), each carrying its emotional-moment + life-hack context. A verify test
    pins the costs to the table. Data; verify-tested. See log below.

- [x] **M2-3. Mia & Leo dialogue** (Requirement 3)
  - `DialogueLine { Speaker, Text }`, `Speaker { Mia, Leo }`,
    `DialogueSequence` with a cursor (`Current`, `HasNext`, `Advance`,
    `SkipToEnd`). Plain C#, verify-tested. See log below.

- [x] **M2-4. Story scene model + all five Act 1 scenes** (Requirement 5 crit. 1, 4)
  - `StoryScene` (setting, **optional** gated `StoryAction`, `DialogueSequence`,
    `LifeHack` ref, optional bonus) + a `NightSetting` enum (§5.2) + a `LifeHack`
    enum (§3.4 Act 1: BowDrillFire, WaterFiltration, LeanToShelter,
    StarNavigation, FieldFirstAid). Author **all five** Act 1 scenes, each with
    Mia+Leo dialogue teaching its hack: campfire (**gated** — LightCampfire, 30,
    bow-drill), then water filtration, lean-to shelter, star navigation, and
    Leo's-cut first aid as **free teaching beats** (no gated action). Data +
    authored dialogue; verify-tested (campfire gating + all five present/ordered).
  - _Dialogue lines are authored content (drafted in Mia/Leo's §3.3/§3.5 voice
    since the GDD gives the hooks, not verbatim script) — reviewable._

- [x] **M2-5. Story sequencing + credit gate** (Requirement 5 crit. 2–3, 5)
  - `IslandQuest.Story.StoryManager`: Act 1 scenes + cursor, `CurrentScene`,
    `TryAdvanceScene()` — for a gated scene charges via `CreditManager.TrySpend`
    (advances + awards bonus on success; blocks + reports insufficient credits
    on failure); for a free teaching beat advances with no spend. Depends on
    M2-1/2/4. Plain C#, verify-tested — the gated "return to puzzle or buy" fork
    (GDD §4.2) and the ungated free-advance are both key cases.

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

## How M2-1 was verified (strict TDD: RED confirmed before any production code)

Baseline before starting: 95 passed, 0 failed. First M2 code, so the
`verify/` csproj was extended to also compile `../Assets/Scripts/Economy/*.cs`
and `../Assets/Scripts/Story/*.cs` (empty globs until files exist).

1. RED step: added 8 tests + `using IslandQuest.Economy;` referencing
   `CreditManager`, `ICreditStore`, `CreditStore` — none existed. Confirmed the
   compile failure first: `CS0234: The type or namespace name 'Economy' does not
   exist in the namespace 'IslandQuest'`.
2. GREEN step: added `Assets/Scripts/Economy/CreditManager.cs` — `CreditManager`
   over an `ICreditStore` (in-memory `CreditStore`): `Balance`, `Earn`,
   `AwardBonus`, `CanAfford`, `TrySpend` (deducts exactly, refuses + leaves
   untouched when unaffordable so it never goes negative), all amounts
   positive-guarded. Full suite: **103 passed, 0 failed**.
3. REVIEW: confirmed every mutation goes through the injected store (store-seam
   test writes through), the refused-spend path leaves the balance byte-exact,
   and `CanAfford` is non-mutating. No M1 regressions. Persistence seam mirrors
   M1's `ILevelRecordStore` so a future `Core/SaveSystem` drops in unchanged.

## How M2-2 was verified

Baseline: 103 passed. RED: 2 tests referencing `StoryAction` / `StoryActionType`
(+ `using IslandQuest.Story;`) — `CS0234: namespace 'Story' does not exist`.
GREEN: `Assets/Scripts/Story/StoryAction.cs` — the six §4.3 actions via
`StoryAction.For(type)`, each with its verbatim cost + emotional-moment context;
suite **105 passed, 0 failed**. REVIEW: costs pinned to the §4.3 table by test,
all six carry non-empty context, the campfire names the bow-drill lesson, an
unknown type throws. No regressions.

## How M2-3 was verified

Baseline: 105 passed. RED: 5 tests referencing `DialogueLine`, `Speaker`,
`DialogueSequence` — `CS0246: 'DialogueLine' could not be found`. GREEN:
`Assets/Scripts/Story/DialogueSequence.cs` — `DialogueLine` (speaker + non-empty
text) and a cursored `DialogueSequence` (`Current`, `HasNext`, `Advance`,
`SkipToEnd`); suite **110 passed, 0 failed**. REVIEW: starts on line 0, advances
one at a time, `HasNext` false at the last line, `Advance` past the end throws,
`SkipToEnd` lands on the last line, empty/null/blank inputs rejected.

## How M2-4 was verified

Baseline: 110 passed. RED: 4 tests referencing `StoryScene`, `NightSetting`,
`LifeHack` — `CS0246: 'StoryScene' could not be found`. GREEN:
`Assets/Scripts/Story/StoryScene.cs` — the scene model (setting, life hack,
dialogue, optional `StoryAction`, optional bonus) + `NightSetting` (§5.2) +
`LifeHack` (§3.4 Act 1) + the authored 5-scene `StoryScene.Act1` catalog; also
added `DialogueSequence.Lines` (a non-cursor-disturbing read view). Suite
**114 passed, 0 failed**. REVIEW: Act 1 has exactly 5 scenes in order; the
campfire is first + gated (LightCampfire, 30, BowDrillFire); the other four are
free teaching beats covering WaterFiltration/LeanToShelter/StarNavigation/
FieldFirstAid; every scene features both Mia and Leo (Leo asks the "why" per
§3.3); bonus defaults to 0 and negative bonus is rejected.
**Authored dialogue** (Mia/Leo lines) is drafted content in their §3.3/§3.5
voice, flagged for product-owner review.

## How M2-5 was verified

Baseline: 114 passed. RED: 6 tests referencing `StoryManager` / `SceneOutcome`
— `CS0246: 'StoryManager' could not be found`. GREEN:
`Assets/Scripts/Story/StoryManager.cs` — sequences an act's scenes over a
cursor with `CurrentScene`/`IsComplete`/`SceneNumber` and `TryAdvanceScene()`
(gated → `CreditManager.TrySpend`, awards bonus + advances on success, returns
`InsufficientCredits` without mutating on failure; free beat advances with no
spend). Suite **120 passed, 0 failed**. REVIEW: starts on the campfire; an
unaffordable gate leaves balance + scene byte-exact; an affordable gate charges
exactly and advances; a free beat advances without spending; a bonus scene
awards its credits; advancing through all scenes completes the act and a further
advance throws.
