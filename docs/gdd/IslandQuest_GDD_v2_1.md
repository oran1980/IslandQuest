# IslandQuest — Game Design Document (v2.1)

> Plain-Markdown export of `IslandQuest_GDD_v2_1.docx` (the binary original
> lives beside this file). Generated for easy reading/grep/diff; if the two
> ever disagree, the `.docx` is authoritative.

IslandQuest
Survival Match-3 Adventure
Learn to Survive. Play to Explore.
Game Design Document v2.0  |  2026
CONFIDENTIAL — Internal Use Only

# 1. Game Overview & Vision

## 1.1 Concept Statement
IslandQuest is a mobile Match-3 puzzle game fused with a story-driven survival adventure. Players solve puzzles to collect glowing green credits, which fuel the story — two characters exploring a mysterious island chain through day and night, overcoming real wilderness challenges.
What sets IslandQuest apart: the protagonist Mia is a trained wilderness survival expert. As the story unfolds, she teaches the player real, practical survival life hacks — fire-starting, water purification, shelter building, star navigation. Players leave every session knowing something genuinely useful.
Design principle: Every session should make the player feel they gained something real — not just points.

## 1.2 At a Glance

| Parameter | Value |
| --- | --- |
| Genre | Match-3 Puzzle + Survival Adventure (Edutainment) |
| Platforms | iOS 15+ and Android 8+ |
| Target Audience | Ages 18–45, nature & outdoor enthusiasts, casual gamers |
| Gender Split (est.) | 55% female / 45% male — broader than typical Match-3 |
| Session Length | 8–15 minutes |
| Business Model | Free-to-Play + IAP + Rewarded Ads |
| Engine | Unity 2023 LTS |
| MVP Dev Time | 4–6 months |
| Launch Budget | $175–$300 (tools + store accounts) |

# 2. Core Concept — What Makes This Unique

## 2.1 The Edutainment Advantage
IslandQuest sits at the intersection of three categories that rarely overlap on mobile:
- Match-3 Puzzle — proven mass-market mechanics (Candy Crush, Homescapes)
- Narrative Adventure — story-driven meta layer with characters players bond with
- Survival Education — real wilderness knowledge delivered through gameplay
This combination creates a unique retention driver: players return not just to progress, but to learn the next survival tip. Word-of-mouth spreads organically when players share genuinely useful knowledge discovered through a game.

## 2.2 Competitive Differentiation

| Feature | IslandQuest | Homescapes | Candy Crush |
| --- | --- | --- | --- |
| Core puzzle | Match-3 nature elements | Match-3 | Match-3 |
| Story layer | Survival adventure, day & night | Home renovation | None |
| Educational value | Real survival life hacks | None | None |
| Credit color | Glowing green #39e75f (unique) | Light blue | Gold coins |
| Character depth | Mia — expert survivalist | Austin — butler | None |
| Night gameplay | Campfire, caves, darkness | None | None |
| Viral potential | "I learned to start fire!" | Low | Low |

# 3. Mia — Character Design & Survival Curriculum

## 3.1 Character Profile
Full name: Mia Calloway
Age: 27
Background: Marine biology PhD dropout who discovered a passion for wilderness survival. Trained across 4 continents. Runs a YouTube channel 'Wild & Smart' with 2M followers.
Personality: Warm, witty, quietly confident. Never panics. Finds beauty in difficult situations. Explains complex survival skills in simple, memorable language.
Physical design: Athletic build, sun-tanned skin, practical clothing — cargo pants, worn boots, bandana. Always carries a handmade survival kit. Animated expressions that react to player wins and losses.

## 3.2 Character Inspiration — A Fusion of Three Icons

| Inspiration | Trait borrowed | How Mia expresses it |
| --- | --- | --- |
| Bear Grylls | Boldness, improvisation, going first | Jumps into caves without hesitation, finds solutions under pressure, thrives in chaos |
| Cody Lundin | Primitive skills, deep nature knowledge | Prefers natural tools over modern ones, explains the science behind every technique |
| Joe Teti | Tactical calm, physical endurance | Never loses composure, plans every move carefully, makes jokes while exhausted |

## 3.3 Leo — Mia's Companion
Role: Mia's childhood friend, amateur photographer. Comic relief and emotional anchor of the story.
Personality: Curious, slightly clumsy, enthusiastic. Asks the questions the player is thinking. His learning journey mirrors the player's own.
Function: When Mia teaches a life hack, Leo asks the follow-up ('But WHY does that work?') — making explanations feel natural, never like a lecture.

## 3.4 Survival Curriculum — Life Hacks by Story Act
Every survival tip is triggered by a real in-story situation. Mia never lectures out of context — the knowledge feels earned, not inserted.

### Act 1 — Jungle Island (Levels 1–30)

| Story trigger | Survival life hack taught |
| --- | --- |
| Campfire needed, no matches | Bow-drill fire starting — friction technique, tinder selection, wood moisture test |
| Dirty stream found | 3-layer water filtration (gravel, sand, charcoal) + mandatory boiling for 3 minutes |
| Sudden rainstorm | Lean-to shelter: branches at 45°, minimum 30cm leaf coverage to stay waterproof |
| Lost after dark | North Star navigation + analog watch solar compass (shadow tip method) |
| Leo gets a cut in the field | Plantain leaf antiseptic, pine needle vitamin C tea for wound healing |

### Act 2 — Volcanic Island (Levels 31–70)

| Story trigger | Survival life hack taught |
| --- | --- |
| No food for a day | Safe edible insects identification — caloric value vs disgust psychology |
| Wide river must be crossed | Current reading, V-formation group crossing technique, walking pole use |
| Volcanic ash cloud approaches | Improvised breathing mask from clothing layers with wet inner filter |
| Night in the open with no gear | Maximizing body heat — earth insulation, leaf piles, emergency group warmth |
| Ankle twisted on lava rock | Natural splinting with sticks, compression wrap from torn fabric, elevation rule |

### Act 3 — Hidden Cave Island (Levels 71–120)

| Story trigger | Survival life hack taught |
| --- | --- |
| Total darkness inside cave | Eye dark-adaptation takes 20 minutes — torch from pine resin and a sturdy stick |
| Mia & Leo become disoriented | Breadcrumb navigation system, sound echo mapping in tunnels |
| Underwater passage discovered | Breath control and safe free-diving basics — equalization and surface technique |
| Ancient trap is triggered | Pressure plate, weight trigger, and tripwire awareness in unknown terrain |
| Final treasure chamber reached | The Rule of Threes: 3 min without air, 3 hrs in cold, 3 days without water, 3 weeks without food |

## 3.5 Life Hack Delivery Format
Each survival tip is delivered in three layers — players absorb at whatever depth they choose:
- Layer 1 — Story moment: 2–3 lines of natural dialogue during the scene (e.g. 'Watch this Leo — dry wood is everything')
- Layer 2 — Tip card: A swipeable illustrated card appears after the scene (skippable, but 78% of players engage based on comparable edutainment titles)
- Layer 3 — Deep dive: Optional 'Learn More' button opens a full illustrated survival guide — this is also a Rewarded Ad trigger

# 4. Dual Loop System — Puzzle + Story

## 4.1 How the Two Loops Interlock
IslandQuest runs two parallel loops that feed each other. Neither works without the other — this is the core design insight:

| Loop | What the player does | What they get | What it costs |
| --- | --- | --- | --- |
| Puzzle Loop (Day) | Solve Match-3 levels | Glowing green credits | Lives (refill over time or buy) |
| Story Loop (Night) | Guide Mia & Leo through wilderness | Survival knowledge + narrative | Glowing green credits |

The key tension: credits are earned in the puzzle and spent in the story. Running out mid-story is the primary IAP trigger — the player is emotionally invested and wants to keep going RIGHT NOW.

## 4.2 The Credit Flow
- Solve puzzle level → earn 20–60 green credits based on star rating
- Watch story scene → spend credits on Mia's actions (campfire, bridge, cave)
- Story reveals a treasure chest → bonus credits awarded
- Credits run out mid-story → return to puzzle, or purchase credits
- Push notification: 'Mia found a hidden passage — she needs your help!'

## 4.3 Credit Costs — Story Actions

| Story action | Credit cost | Emotional moment |
| --- | --- | --- |
| Light a campfire | 30 | Warmth, safety — Mia teaches the bow-drill technique |
| Cross a rope bridge | 50 | Tension — Leo is scared, survival tip on rope physics |
| Enter a hidden cave | 80 | Mystery — darkness, torch-making lesson begins |
| Unlock secret passage | 120 | Major story reveal — always feels worth it |
| Rescue a trapped animal | 40 | Emotional hook — Mia explains animal behavior, Leo names it |
| Open a treasure chest | 60 | Random bonus credits + collectible item |

# 5. Day / Night World Design

## 5.1 Day Mode — The Puzzle World
Day scenes are bright, colorful, and energetic. The Match-3 board sits against a lush jungle or tropical backdrop. This is where the player earns credits.

| Element | Day appearance | Behavior |
| --- | --- | --- |
| Board background | Sunlit jungle clearing | Animated leaves, birds flying past |
| Green credit bags | Glowing neon green (#39e75f), sparkling particles | Fall onto board randomly; combo chains drop more |
| Tile elements | Bright saturated colors — flower, leaf, wave, sun, mushroom, coral | Standard match-3 behavior |
| UI tone | Warm yellows and greens | Energetic, celebratory particle bursts |
| Music | Upbeat tropical acoustic guitar | Tempo increases with combo count |

## 5.2 Night Mode — The Story World
Night scenes are atmospheric, mysterious, and emotionally rich. Darkness is a core element — the environment reacts to Mia's actions. This is where credits are spent and survival knowledge is gained.

| Location | Visual style | Survival element taught |
| --- | --- | --- |
| Forest at night | Deep blue-black, firefly particles, moonlight shafts through canopy | Navigation by stars, identifying animals by sound |
| Campfire scene | Warm amber glow radiating outward, animated smoke rising | Fire-starting methods, keeping fire alive in wind and rain |
| Standard cave | Near-total darkness, torch illuminates small radius | Eye dark-adaptation (20 min), echo navigation, torch-making |
| Hidden cave | Bioluminescent crystals, ancient carvings on walls | Underground survival, reading geological danger signs |
| Jungle river | Moonlit water, ripple reflections, low mist | Water sourcing, current reading, safe crossing technique |
| Secret ruins | Overgrown stone, scattered treasure, mysterious symbols | Ancient navigation tools, orienteering without instruments |

## 5.3 Day-to-Night Transition
The transition between modes is a cinematic moment that signals a shift in tone and gameplay:
- Sun sets behind the island — 8-second animated cutscene
- Mia speaks: 'The jungle at night is a completely different world. Stay close, Leo.'
- Board dissolves into story scene with a soft fade
- Credit balance shown: 'You have 85 credits. What will Mia do next?'

# 6. Green Credit Economy

## 6.1 Visual Identity — Why Glowing Green
The glowing green credit bag (#39e75f) is a deliberate brand decision:
- Visually distinct — impossible to confuse with any top-10 Match-3 game
- Thematically coherent — green = nature, jungle, life, growth
- Memorable — neon green pops dramatically against dark night scenes
- Ownable — becomes the unmistakable signature of the IslandQuest brand

## 6.2 Credit Sources

| Source | Amount | Trigger |
| --- | --- | --- |
| Level complete — 1 star | 20 credits | Basic completion |
| Level complete — 2 stars | 35 credits | Good performance |
| Level complete — 3 stars | 55 credits | Excellent performance |
| Combo x3 or more | +10 bonus credits | Mid-level bonus bag drops on board |
| Daily login | 30 credits (Day 1) up to 250 (Day 30+) | Streak-based scaling reward |
| Treasure chest (story) | 40–80 credits | Random, surprise reward |
| Watch Rewarded Ad | 25 credits | Player-initiated, always voluntary |
| Refer a friend | 150 credits | One-time per successful install |
| IAP — Starter Pack | 200 credits | $0.99 |
| IAP — Credit Pack M | 600 credits | $2.99 |
| IAP — Credit Pack L | 1,500 credits | $6.99 |

## 6.3 Credit Sinks

| Sink | Cost | Frequency |
| --- | --- | --- |
| Story action (average) | 30–120 credits | Every story scene |
| Continue after level fail (5 extra moves) | 50 credits | Near-miss moment — high conversion |
| Extra lives (×5 hearts) | 60 credits | When hearts depleted |
| Booster before level | 30 credits each | Player's choice |
| Unlock bonus chapter early | 300 credits | Impatient players |

# 7. Puzzle Mechanics (Match-3)

## 7.1 Board Configuration
- Grid size: 9×9
- 6 tile types, all nature and survival themed
- Green credit bags fall randomly — approx. 1–2 per level; more during combo chains

## 7.2 Tile Elements & Boosters

| Element | Color | Booster on 4+ match | Survival theme |
| --- | --- | --- | --- |
| Flower | Pink | Bloom Burst — clears entire row | Edible wildflowers vs toxic look-alikes |
| Leaf | Green | Leaf Wheel — clears full column | Medicinal plants and herbal remedies |
| Wave | Blue | Tidal Clear — removes 3×3 zone | Water sourcing and purification |
| Sun | Yellow | Solar Flare — removes all tiles of one color | Solar navigation and signaling |
| Mushroom | Orange | Spore Cloud — removes 5 random tiles | Edible vs toxic fungi identification |
| Coral | Red | Deep Surge — clears bottom two rows | Marine survival and reef navigation |

## 7.3 Near-Miss System
The most important monetization mechanic in the game. The Difficulty AI ensures levels end with 1–3 moves remaining approximately 30% of the time:
- Player reaches 85–95% of level objective on their final move
- AI subtly introduces one blocking tile to prevent completion
- Prompt appears: 'So close! Continue with 5 extra moves?' — 50 credits or $0.99
- Near-miss conversion rate: 12–18% (vs 3–5% for standard IAP prompts)

## 7.4 Lives System

| Parameter | Value |
| --- | --- |
| Max lives | 5 hearts |
| Refill rate | 1 heart per 30 minutes (full refill = 2.5 hours) |
| Buy lives | $0.99 for 5 immediate hearts |
| Free lives sources | Rewarded Ad = 1 life \| Daily login = 3 lives \| Friend gift = 1 per day |
| Push notification | 'Your hearts are full — Mia is waiting for you!' |

# 8. Story World — Islands & Narrative

## 8.1 Overarching Story
Mia discovers an ancient hand-drawn map in her grandmother's attic — a chart of uncharted islands rumored to hide the 'Emerald Archive': a cache of ancient survival knowledge compiled by a lost civilization. She calls Leo and they set off. Each island is a chapter. Each chapter teaches survival skills specific to that environment.

## 8.2 Island Map — MVP (3 islands) and Full Vision (12 islands)

| Island | Biome | Levels | Story arc | Key survival skills |
| --- | --- | --- | --- | --- |
| 1 — Coconut Isle | Tropical jungle | 1–30 | Find the first map fragment; rescue a stranded sailor | Fire, water filtration, shelter, star navigation |
| 2 — Ember Peak | Volcanic highland | 31–70 | Climb the active volcano for the second fragment; survive eruption | Heat survival, gas protection, food foraging |
| 3 — Coral Abyss | Coastal caves and reef | 71–120 | Dive for the hidden archive; face total darkness underground | Cave survival, free-diving, the Rule of Threes |
| 4–12 | Locked — future updates | — | Arctic tundra, Sahara desert, Amazon rainforest, Deep ocean... | Each biome introduces a complete new skill set |

## 8.3 Collectibles
- Survival Journal: each life hack unlocked adds an illustrated page (viewable from the main menu at any time)
- Animal Friends: rescued animals become animated companions in Mia's camp
- Ancient Artifacts: decorative items from treasure chests, displayed in a virtual expedition tent
- Leo's Photo Album: auto-generated story cards from key moments — shareable to WhatsApp and Instagram

# 9. Retention Systems

## 9.1 Daily Engagement

| Mechanism | Reward | Goal |
| --- | --- | --- |
| Daily login | 30–250 credits (streak-scaled) | Build daily habit |
| 7-day streak bonus | Rare booster + 200 credits | Weekly commitment |
| Daily quest ×3 | 100 credits per quest | Multiple sessions per day |
| Morning bonus (8–10am) | ×2 credits on first level | Drive peak-hour usage |
| 'Mia is waiting' push | 4 hours after last session | Win-back lapsed players |

## 9.2 The Knowledge Retention Loop (Unique to IslandQuest)
Players return because they want to learn the next survival skill. No other Match-3 has this driver:
- End of session preview: 'Next time: Mia teaches you how to find water in the desert without tools'
- Teaser push notification: 'Did you know you can navigate by stars using only your hand?'
- 'Survival Tip of the Day' push notification — standalone value, drives opens even without gameplay intent

## 9.3 Time-Limited Events

| Event | Duration | Mechanic | Reward |
| --- | --- | --- | --- |
| Weekend Challenge | 48 hours | Special 5-level survival scenario | Exclusive island skin |
| Seasonal Event | 14 days | Biome-specific content (winter storm, monsoon, drought) | Unique outfit for Mia |
| Flash Survival | 2 hours | 3 extreme-difficulty levels | ×3 credits + rare artifact |
| Mystery Island | 24 hours per month | Hidden island with unknown content | Lore unlock + journal page |

## 9.4 Social Features
- Weekly leaderboard — top 20 friends by credits earned; top 5 win exclusive cosmetic item
- Send lives to friends — 1 heart per day per friend, received as push notification
- Share Leo's photos — auto-generates branded story card for social media
- Expedition teams — groups of 5 players unlock a shared island chapter together
- 'I learned this from a game' card — survival tip with IslandQuest branding, designed for sharing

# 10. Monetization Model

## 10.1 In-App Purchases

| Product | Price | Contents | Primary buyer |
| --- | --- | --- | --- |
| Starter Pack (one-time offer) | $0.99 | 200 credits + 3 boosters + 5 lives | New players, day 1–3 |
| Lives Pack | $0.99 | 5 immediate hearts | Impulse buy, mid-session |
| Credit Pack S | $1.99 | 300 credits | Story-blocked players |
| Credit Pack M | $2.99 | 600 credits + 1 booster | Regular spenders |
| Credit Pack L | $6.99 | 1,500 credits + 3 boosters | Whales |
| Booster Bundle | $1.99 | ×10 of each booster type | Puzzle-focused players |
| No Ads Forever | $3.99 | Removes all interstitial ads permanently | ~15% of active players buy this |
| Season Pass | $4.99/month | Daily credits, exclusive content, ad-free | Highly engaged regulars |

## 10.2 Advertising Strategy

| Ad type | Placement | Player gets | Est. eCPM |
| --- | --- | --- | --- |
| Rewarded Video | Voluntary — player taps 'Watch Ad' | 25 credits / 1 life / booster | $12–25 |
| Rewarded Video | After level fail — optional continue offer | 5 extra moves to finish level | $15–30 |
| Rewarded Video | 'Learn More' button on survival tip card | Full illustrated survival guide | $10–20 |
| Interstitial | After level WIN only — 1 in every 4 wins | Nothing — 30 seconds max | $8–15 |
| Banner | Home / map screen only — never during gameplay | Nothing | $0.5–2 |

Golden rule: interstitial ads appear ONLY after a win, never after a loss. Players in a positive state have 2x higher CTR and are far less likely to uninstall after seeing an ad.

## 10.3 Revenue Projections (Month 3)

| Scenario | DAU | ARPDAU | Monthly revenue |
| --- | --- | --- | --- |
| Conservative | 1,000 | $0.04 | $1,200 |
| Realistic | 5,000 | $0.06 | $9,000 |
| Optimistic | 20,000 | $0.08 | $48,000 |

# 11. Technical Architecture (Unity)

## 11.1 Project Structure
- Assets/Scripts/Core — GameManager, LevelManager, SceneController, SaveSystem
- Assets/Scripts/Match3 — BoardController, TileLogic, MatchFinder, BoardSolver, DifficultyAI
- Assets/Scripts/Story — StoryManager, DialogueSystem, CharacterAnimator, DayNightController
- Assets/Scripts/Economy — CreditManager, IAPManager, AdManager, LivesManager
- Assets/Scripts/Education — SurvivalTipManager, JournalController, TipCardUI
- Assets/Scripts/UI — HUDController, ShopUI, PopupManager, TransitionController
- Assets/ScriptableObjects — LevelData, TileConfig, StoryScene, SurvivalTip, ShopItem

## 11.2 Key Classes

| Class | Responsibility | Key dependencies |
| --- | --- | --- |
| GameManager | Singleton, global game state, scene flow control | — |
| BoardController | Board creation, drag input handling, tile swaps | MatchFinder, DifficultyAI |
| MatchFinder | Match detection, combo chains, cascade logic | BoardController |
| DifficultyAI | Near-miss logic, dynamic difficulty adjustment per player | LevelData, CreditManager |
| DayNightController | Mode switching, lighting changes, transition cutscenes | StoryManager |
| StoryManager | Scene sequencing, credit gate checks, dialogue flow | CreditManager, DialogueSystem |
| SurvivalTipManager | Tip trigger logic, card display, journal page unlock | StoryManager |
| CreditManager | Credit balance, all transactions, local persistence | IAPManager, AdManager |
| IAPManager | Unity IAP integration, receipt validation server call | CreditManager |
| AdManager | AdMob SDK — Rewarded, Interstitial, Banner placement logic | GameManager |
| LivesManager | Heart count, 30-min timer, push notification scheduling | GameManager |

## 11.3 Required SDKs & Plugins

| SDK / Plugin | Purpose | Cost |
| --- | --- | --- |
| Google AdMob Unity SDK | Banner, Interstitial, Rewarded ads | Free |
| Unity IAP | Cross-platform in-app purchases (iOS + Android) | Free |
| Firebase Analytics | Event tracking, funnels, crash reporting (Crashlytics) | Free |
| Firebase Remote Config | Change A/B test parameters without app update | Free |
| Firebase Cloud Messaging | Push notifications on iOS and Android | Free |
| DOTween Pro | UI animations, tile movement, scene transitions | $15 one-time |
| Match3 Kit (Asset Store) | Pre-built Match-3 foundation — saves 6–8 weeks | $50–80 |
| Spine (optional) | Rigged 2D animation for Mia and Leo characters | $60 one-time |

# 12. Development Roadmap & Budget

## 12.1 Milestones

| Milestone | Duration | Deliverables | Budget |
| --- | --- | --- | --- |
| M1 — Core Puzzle | Weeks 1–6 | Working Match-3 board, 30 levels, lives system, green credit bag drops | $0 |
| M2 — Story Layer | Weeks 7–10 | Day/night mode, story scenes, Mia & Leo dialogue, campfire moment | $80 |
| M3 — Education Layer | Weeks 11–12 | Survival tip cards, journal system, 15 life hacks fully integrated | $0 |
| M4 — Monetization | Weeks 13–14 | Unity IAP, AdMob, Remote Config, Firebase Analytics fully live | $80 |
| M5 — Polish | Weeks 15–16 | DOTween animations, full sound design, Spine (optional), UI pass | $115 |
| M6 — Soft Launch | Weeks 17–18 | Canada / Australia release, A/B tests running, crash monitoring | $50 |
| M7 — Global Launch | Week 19+ | Full release, content updates, island 2 begins development | $100+ |

## 12.2 Budget Breakdown

| Item | Cost |
| --- | --- |
| Apple Developer Account | $99/year |
| Google Play Account | $25 one-time |
| Match3 Kit (Asset Store) | $50–80 |
| DOTween Pro | $15 |
| Sound packs (Freesound.org + 1 paid) | $0–30 |
| Spine character animation (optional) | $60 |
| Soft launch paid installs (Canada/AU) | $50–100 |
| Total estimated | $299–409 |

# 13. A/B Testing Plan
Run via Firebase Remote Config during soft launch in Canada and Australia before global release:

| Test | Variant A | Variant B | Decision metric |
| --- | --- | --- | --- |
| Campfire credit cost | 30 credits | 40 credits | Story completion rate |
| Near-miss AI threshold | 85% of objective | 90% of objective | IAP conversion rate |
| Maximum lives | 5 hearts | 3 hearts | Session frequency + IAP rate |
| Interstitial frequency | 1 in every 4 wins | 1 in every 3 wins | D7 retention |
| Starter pack price | $0.99 | $1.49 | Revenue per user |
| Survival tip delivery | Tip card auto-appears | Tip card is opt-in button | Tip read-through rate |
| Push notification tone | Urgent ('Mia needs you!') | Warm ('Mia found something...') | Open rate |

# 14. KPIs & Success Metrics

## 14.1 Retention Targets

| Metric | Month 1 | Month 3 | Month 6 |
| --- | --- | --- | --- |
| D1 Retention | 40%+ | 45%+ | 50%+ |
| D7 Retention | 20%+ | 25%+ | 30%+ |
| D30 Retention | 8%+ | 12%+ | 15%+ |
| Avg session length | 8 min | 10 min | 12 min |
| Sessions per DAU per day | 1.8 | 2.2 | 2.5 |

## 14.2 Monetization Targets

| Metric | Month 1 | Month 3 | Month 6 |
| --- | --- | --- | --- |
| ARPDAU | $0.03 | $0.05 | $0.08 |
| IAP conversion rate | 2% | 3% | 4% |
| Rewarded Ad watch rate | 35% | 45% | 50% |
| App Store / Play Store rating | 4.0+ | 4.2+ | 4.4+ |

## 14.3 Education Metrics — Unique to IslandQuest

| Metric | Target | Why it matters |
| --- | --- | --- |
| Tip card read-through rate | 75%+ | Validates educational hook as core retention driver |
| Journal completion rate per island | 60%+ | Players engaged with full knowledge layer, not just puzzle |
| 'Learn More' tap rate | 25%+ | Rewarded Ad trigger working effectively |
| Organic reviews mentioning 'learned' | 20%+ of all reviews | Confirms unique value proposition in market |
| Survival tip share rate (social) | 10%+ of tip readers | Primary viral organic growth driver |

IslandQuest GDD v2.0  |  Created with Claude  |  2026