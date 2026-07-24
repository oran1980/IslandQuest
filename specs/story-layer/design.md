# Design — Story Layer (Milestone 2)

Architecture decisions for M2, and a correction log (mistakes caught and fixed
before they reach code — keep appending, don't silently fix). Traces to
`requirements.md` and GDD §3–5, §8.1, §11.

## 0. Guiding constraint (inherited from M1)

`Assets/Scripts/Story/*` and `Assets/Scripts/Economy/*` **logic** stays plain
C# with **zero `UnityEngine` references**, exactly like `Assets/Scripts/Match3`
(see `specs/core-puzzle/design.md` §5 and CLAUDE.md). This keeps the credit
economy, dialogue flow, scene sequencing, and day/night state machine
verifiable in `verify/` without the Editor. The Unity presentation (day/night
cutscene, dialogue box, character sprites, scene backdrops) is built last as
MonoBehaviours and Editor-playtested — the Task 9 / Task 16b pattern.

The GDD's `Assets/Scripts` folders (§11.1) are the target namespaces:
`IslandQuest.Story` (StoryManager, DialogueSystem, DayNightController) and
`IslandQuest.Economy` (CreditManager). Presentation MonoBehaviours live under
`Assets/Scripts/Story` / `UI` like M1's `BoardController`.

## 1. The credit economy is the spine (Requirement 1–2)

GDD §4's core insight: one currency bridges both loops. So `CreditManager` is
built **first** — everything else (story gates, day/night hand-off) depends on
it, and it's the most testable piece.

- `CreditManager`: an integer `Balance`, `Earn(int)`, `TrySpend(int) → bool`
  (refuses and returns false if unaffordable; never goes negative), `CanAfford(
  int)`, and `AwardBonus(int)`. All mutations go through it — no other class
  touches a raw balance.
- Persistence behind `ICreditStore` (in-memory `CreditStore` now), mirroring
  M1's `ILevelRecordStore`/`LevelRecordStore` seam. A future `Core/SaveSystem`
  implements it; nothing in the story layer changes when it does.
- The bridge to M1: level completion feeds `LevelResult.CreditPayout` into
  `CreditManager.Earn` (the Task 16 `GameFlowController` is the wiring point —
  the M1↔M2 seam, done in the presentation task, not the engine).

`StoryAction` (Requirement 2): a small value type / enum with the GDD §4.3 cost
table and its emotional-moment/life-hack context. Costs are GDD-verbatim (not
tunable design values like M1's star thresholds) — they're transcribed, and a
verify test pins them to the table so an edit can't silently drift.

## 2. Dialogue is data + a cursor (Requirement 3)

`DialogueLine { Speaker (Mia|Leo), Text }`; `DialogueSequence` is an ordered
`DialogueLine[]` with a cursor: `Current`, `HasNext`, `Advance()`, `SkipToEnd()`.
Pure data + iteration — trivially testable, no UnityEngine. The presentation
dialogue box just renders `Current` and calls `Advance()` on tap. Leo's role
(the "but WHY does that work?" follow-up, §3.3) is encoded as authored lines,
not special logic.

## 3. Story scenes & sequencing (Requirement 5)

`StoryScene` bundles: `Setting` (a GDD §5.2 night-location enum), the gated
`StoryAction` (+ its cost), a `DialogueSequence`, the `LifeHack` it teaches (an
id/enum referencing §3.4 — the full tip-card content is M3, so M2 stores only
the reference + the Layer-1 lines), and an optional bonus-credit award.

`StoryManager` (§11.2 "scene sequencing, credit gate checks, dialogue flow"):
holds Act 1's ordered scenes + a cursor, exposes `CurrentScene`, and
`TryPerformAction()` which asks `CreditManager.TrySpend(scene.Action.Cost)` —
on success charges + advances (+ awards any bonus), on failure reports
insufficient credits without mutating anything (GDD §4.2's "return to puzzle or
buy" fork). `StoryManager` depends on `CreditManager` + the scene data only;
it's plain C# and fully verify-testable.

**Campfire scene (the M2 showcase, Requirement 5 crit. 4):** Setting = Campfire,
Action = LightCampfire (30), LifeHack = BowDrillFire, DialogueSequence = the
Mia/Leo bow-drill exchange (authored from §3.4/§3.5, e.g. Mia: "Watch this Leo —
dry wood is everything."). Authored as data so it drops into the sequencer like
any scene.

## 4. Day/Night as a state machine (Requirement 4)

`DayNightController` (§11.2 "mode switching, lighting changes, transition
cutscenes"): the **state** half is plain C# — `Mode { Day, Night }`, starts
Day, `ToNight()` / `ToDay()`, and a transition payload carrying the credit
balance for the §5.3 hand-off beat ("You have 85 credits…"). The **visual**
half (8-second sunset cutscene, lighting, board→scene fade) is the presentation
MonoBehaviour, which observes the state. Splitting it this way lets the mode
logic and the credit-at-transition read be tested without the cutscene.

## 5. Presentation layer (built last, Editor-playtested)

MonoBehaviours, following Task 16b's procedural-first approach unless/until art
assets arrive:
- Day↔Night transition screen (the §5.3 cutscene beat + credit read).
- Story-scene view: setting backdrop, character (Mia/Leo) display, a dialogue
  box advancing on tap, and the credit-gated action button (disabled/"need N
  more credits" when unaffordable).
- Wiring `GameFlowController` (M1) so a level win routes `CreditPayout` into
  `CreditManager`, and a "go to story / night" entry point switches modes.

The Mia/Leo character art + rigged facial animation (Spine, §11.3, optional) is
an **art-asset** dependency — the same drop-in seam left in Task 16b's "Mia"
placeholder. Procedural placeholders stand in until art exists; matching a
Homescapes look is deferred (M5 polish), consistent with the M1 results-screen
decision.

## 6. Scope boundaries (what M2 does NOT build)

- **Tip cards / journal / deep-dive** (§3.5 Layer 2–3, §8.3) → M3.
- **Buying credits / Rewarded-Ad top-ups** (§4.2 "purchase credits", §10) → M4.
  M2 models the balance those will feed, and surfaces the "insufficient
  credits" fork, but not the store.
- **Acts 2–3 scenes** (§3.4) → later content; M2 authors **Act 1**, with the
  campfire as the mandatory vertical slice.

## Design correction log

- **Island naming (Jungle vs Coconut).** GDD §3.4 calls Act 1 "Jungle Island";
  §8.2 (canonical map) calls Island 1 "Coconut Isle" (tropical jungle, Levels
  1–30) — the name M1's `LevelData` already uses. Same place; this spec
  standardizes on **Coconut Isle** and treats "Jungle Island" as a §3.4
  labelling slip, rather than introducing a second island name into code.
