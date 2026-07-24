# Requirements — Story Layer (Milestone 2)

Source: IslandQuest GDD v2.1 — §3 (Mia & Leo), §4 (Dual Loop System), §4.3
(Credit Costs — Story Actions), §5 (Day/Night World Design), §8.1 (Overarching
Story), §11 (Technical Architecture: `Assets/Scripts/Story`, `Economy`).

Scope: this spec covers **Milestone 2 — Story Layer** only (GDD §12.1:
"Day/night mode, story scenes, Mia & Leo dialogue, campfire moment"). It builds
directly on the M1 core-puzzle foundation (see `specs/core-puzzle/`), which
already earns per-level credits (`LevelResult.CreditPayout`). Out of scope,
deferred to their own milestones:

- **Survival tip cards / journal** (Layer 2–3 of GDD §3.5) → **M3 Education**.
- **IAP, ads, Remote Config, analytics** (the "buy credits" / Rewarded-Ad
  paths) → **M4 Monetization**. This spec models the credit *balance* and
  *spend* that those systems will later top up, but not the purchase itself.
- **DOTween/Spine polish, full sound** → **M5**.

Following the M1 architecture rule (see `specs/core-puzzle/design.md` §5): the
story/economy **logic** is plain C# with zero `UnityEngine` references, so it's
independently verifiable in `verify/`; the Unity presentation layer (day/night
cutscene, dialogue UI, character display) is built last and Editor-playtested,
exactly like Task 9 / Task 16b.

Each requirement is a user story with EARS-style acceptance criteria
(WHEN/IF … THE SYSTEM SHALL …) so tasks and tests trace back to a line.

---

## Requirement 1 — Green Credit Balance & Economy

**User story:** As a player, I want the green credits I earn in puzzles to
carry into the story and be spent on Mia's actions, so the two loops feed each
other and running low creates a reason to play more (GDD §4.1–4.2).

**Provenance:** GDD §4 makes credits the single currency bridging both loops —
earned in the puzzle (M1, 20–60 per level by stars), spent in the story (§4.3).
`CreditManager` owns "Credit balance, all transactions, local persistence"
(§11.2).

**Acceptance criteria**
1. WHEN the player completes a level THE SYSTEM SHALL add that level's credit
   payout (M1 `LevelResult.CreditPayout`) to the running balance.
2. WHEN a story action is paid for THE SYSTEM SHALL deduct its credit cost from
   the balance (costs per Requirement 2 / GDD §4.3).
3. IF the balance is less than an action's cost THE SYSTEM SHALL refuse the
   spend and leave the balance unchanged (the balance SHALL never go negative).
4. THE SYSTEM SHALL expose whether the player can currently afford a given cost,
   so the story/UI can gate an action before attempting it.
5. WHEN a treasure chest / story bonus is awarded THE SYSTEM SHALL add those
   bonus credits to the balance (GDD §4.2).
6. Credit persistence SHALL sit behind an interface seam (a future
   `Core/SaveSystem`, out of scope) — mirroring M1's `ILevelRecordStore`
   approach — so an in-memory store works now and a persistent one drops in
   later.

## Requirement 2 — Story Actions & Their Costs

**User story:** As a player, I want Mia's key story moments (campfire, bridge,
cave…) to cost credits I earned, so progress feels earned and each action has
weight (GDD §4.3).

**Acceptance criteria**
1. THE SYSTEM SHALL define the story actions and costs verbatim from GDD §4.3:
   Light a campfire = 30, Cross a rope bridge = 50, Enter a hidden cave = 80,
   Unlock secret passage = 120, Rescue a trapped animal = 40, Open a treasure
   chest = 60.
2. Each story action SHALL carry its associated emotional-moment / life-hack
   context (GDD §4.3 column 3) so the scene can present it.
3. WHEN an action is performed THE SYSTEM SHALL charge exactly its listed cost
   via Requirement 1's spend path (no action bypasses the balance check).

## Requirement 3 — Mia & Leo Dialogue

**User story:** As a player, I want Mia and Leo to talk during story scenes —
Mia teaching, Leo asking the questions I'm thinking — so survival knowledge
feels earned, never lectured (GDD §3.3, §3.5 Layer 1).

**Acceptance criteria**
1. A dialogue sequence SHALL be an ordered list of lines, each with a speaker
   (Mia or Leo) and text (GDD §3.5 Layer 1: "2–3 lines of natural dialogue").
2. THE SYSTEM SHALL advance through lines one at a time and expose the current
   line and whether more remain.
3. THE SYSTEM SHALL support skipping to the end of a sequence (Layer 1 is
   skippable per §3.5).
4. A dialogue sequence SHALL be attachable to a story scene so the scene's
   life-hack "story moment" is delivered in-context (GDD §3.4 triggers).

## Requirement 4 — Day / Night Mode

**User story:** As a player, I want a clear day (puzzle) vs night (story) mode
with a cinematic hand-off between them, so the shift in tone and gameplay reads
instantly (GDD §5).

**Acceptance criteria**
1. THE SYSTEM SHALL model two modes — **Day** (puzzle world) and **Night**
   (story world) — with Day as the entry mode (GDD §5.1–5.2).
2. THE SYSTEM SHALL transition Day → Night when the player leaves the puzzle to
   enter the story, and Night → Day when they return to play more (GDD §4.2,
   §5.3).
3. WHEN transitioning Day → Night THE SYSTEM SHALL surface the current credit
   balance for the hand-off beat (GDD §5.3: "You have 85 credits. What will Mia
   do next?").
4. The mode-switch **logic/state** SHALL be plain C# (testable); the lighting
   change and 8-second cutscene (GDD §5.3) are presentation, layered on top.

## Requirement 5 — Story Scene Sequencing & the Campfire Moment

**User story:** As a player, at night I want to guide Mia & Leo through a story
scene — spend credits on an action, watch it play out with dialogue and a
survival lesson — with the **campfire** as the first such moment (GDD §4.3,
§5.2, §3.4 Act 1).

**Provenance:** `StoryManager` owns "Scene sequencing, credit gate checks,
dialogue flow" (§11.2). Act 1 (Coconut Isle, Levels 1–30) opens with the
campfire trigger "Campfire needed, no matches" → bow-drill fire-starting
(§3.4).

**Acceptance criteria**
1. A story scene SHALL bundle: a setting (GDD §5.2 location), a credit-gated
   action + cost (Requirement 2), a dialogue sequence (Requirement 3), and the
   survival life-hack it teaches (GDD §3.4).
2. THE SYSTEM SHALL sequence Act 1's scenes in narrative order and expose the
   current scene.
3. IF the player can afford the current scene's action THE SYSTEM SHALL allow
   performing it — charging the cost (Requirement 1) and advancing the scene;
   OTHERWISE it SHALL block the action and report insufficient credits (the
   primary "return to puzzle or buy" moment, GDD §4.2).
4. THE **campfire scene** SHALL be authored: setting = Campfire (§5.2), action =
   Light a campfire (cost 30), a Mia+Leo dialogue delivering the bow-drill
   fire-starting story moment (§3.4, §3.5 Layer 1).
5. WHEN a scene with a treasure/bonus resolves THE SYSTEM SHALL award its bonus
   credits (Requirement 1 crit. 5 / GDD §4.2).

---

## Naming note (carried to design.md correction log)

GDD §3.4 labels Act 1 "**Jungle Island**" while §8.2 (the canonical island map)
names Island 1 "**Coconut Isle**" (tropical jungle biome, Levels 1–30 — the
name M1's `LevelData` already uses). These are the same place; this spec uses
**Coconut Isle** and treats "Jungle Island" as a §3.4 labelling slip.
