# IslandQuest — From Task 9 to Money in Your Bank Account

A practical, ordered playbook. Facts below were verified against Apple's and
Google's own documentation as of mid-2026 — this is a fast-moving area, so
where a number could plausibly have shifted by the time you read this,
there's a link to check.

**Not legal, tax, or financial advice.** Business structure (sole
proprietor vs. LLC vs. company), tax residency, and VAT/sales-tax
obligations vary a lot by country — talk to an accountant before you pick
one, especially once real money starts moving.

---

## Part 1 — What to install

| Tool | Why | Notes |
|---|---|---|
| **Unity Hub + Unity 6.3 LTS** | The actual game engine | The GDD says "2023 LTS" — that version line doesn't exist anymore; Unity skipped straight to Unity 6. **Unity 6.3 LTS** (2-year support, until Dec 2027) is the current equivalent recommendation. Download via Unity Hub, not a bare installer, so you can manage versions. |
| **Android Build Support module** | Lets Unity build `.aab` files for Google Play | Install from inside Unity Hub when installing the Editor — tick "Android Build Support" + its SDK/NDK/OpenJDK sub-items. No Mac needed. |
| **iOS Build Support module** | Lets Unity build the Xcode project for iOS | Only useful if you have a Mac (see Part 1a below). Tick it regardless — costs nothing to have installed. |
| **Git** | Version control | You already have this if you're using Claude Code. |
| **A code editor** | Editing C# outside Unity's built-in editor | VS Code or JetBrains Rider both work well with Unity + Claude Code. |

Unity itself is now free of any runtime/revenue fee — Unity canceled the
controversial "Runtime Fee" policy, so there's no per-install cost from
Unity regardless of how well the game does.

### 1a — The Mac problem (iOS only)

Building and submitting an iOS app **requires Xcode, which only runs on
macOS.** Android has no such requirement — you can build and submit from
Windows or Linux with zero extra hardware.

If you don't own a Mac, your options, roughly cheapest/simplest first:
1. **Unity Cloud Build** (~$9/month) — builds iOS from your GitHub repo on
   Unity's own Mac hardware, no Mac needed on your end.
2. **A cloud-Mac rental service** (e.g. MacinCloud) — pay hourly/monthly for
   remote access to real Mac hardware.
3. **Borrow or buy a Mac** — even an old Mac mini is enough just for builds.

Either way, you still need the $99/year Apple Developer Program (Part 3) —
there's no way around that part regardless of build method.

---

## Part 2 — Testing the game as it's built

- **In-Editor testing** (Play Mode) works today, no installs needed beyond
  Unity itself — this is what Task 9's `BoardController` will make possible
  once it exists.
- **On-device Android testing**: enable Developer Options + USB debugging
  on an Android phone, build an `.apk`/`.aab` from Unity, install directly
  (`adb install`) or via Unity's "Build and Run." No store account needed
  for this step.
- **On-device iOS testing**: needs the Apple Developer Program (Part 3)
  enrolled first, plus a provisioning profile — then you can install via
  Xcode directly, or via **TestFlight** once you're set up in App Store
  Connect (better for testing with other people, not just yourself).

---

## Part 3 — Accounts you need, and what they cost

| Account | Cost | Required for |
|---|---|---|
| **Apple Developer Program** | $99/year | Any iOS distribution at all — App Store *and* TestFlight, even free apps. Requires 2-factor auth on your Apple ID. |
| **Google Play Console** | $25 one-time | Android distribution. Non-refundable, never renews. |

Google has added real friction for **new personal accounts** (created after
Nov 13, 2023 — which yours will be): before you can publish to production,
you must run a **closed test with at least 12 opted-in testers for 14
consecutive days**, plus verify you personally have access to an Android
device via the Play Console mobile app. Budget 2–4 weeks for this alone —
start it as early as possible, since it's the long pole in the whole
process, not something to leave until the app is "done." (You'll need
people willing to install and actually use the test build — friends,
Reddit/Discord communities, or a paid testing service if you're stuck.)

Apple has no equivalent testing-gate requirement, but Apple's own app
*review* after submission typically takes 24–48 hours (can vary).

---

## Part 4 — Store listing requirements (both platforms)

Both stores will ask for, roughly:
- App icon, screenshots (multiple device sizes), a feature graphic (Google)
- Short + full description
- **Content rating questionnaire** (Apple: age rating; Google: IARC
  questionnaire) — for a match-3 game with lives/IAP/ads this is usually
  straightforward, but answer honestly, especially about ads and IAP.
- **Data safety / App Privacy disclosures** — what data you collect (even
  "none" needs to be declared) and why. If you add any analytics or ad SDK,
  this section gets more involved — fill it in *after* you know which SDKs
  are actually in the build.
- **Ads declaration** (Google specifically asks "does your app contain
  ads?" — must match reality or risk suspension)

---

## Part 5 — Wiring up monetization (per the GDD's IAP/ads/credit design)

Two separate revenue mechanisms, both need setup **inside** each store's
console, not just in Unity:

1. **In-app purchases** (credit packs, life refills, etc. per the GDD) —
   implement with **Unity IAP** (Unity's cross-platform package that talks
   to both Apple's and Google's billing under one API), then create
   matching **product IDs** in both App Store Connect and Play Console — the
   IDs must match exactly what your code references, or purchases will fail
   silently in testing.
2. **Ads** — pick a mediation SDK (AdMob is the most common starting point;
   Unity Ads/LevelPlay is another option) and create an account with that
   ad network separately from your store accounts. This is its own signup,
   its own dashboard, and its own payout mechanism (Google AdSense-style, on
   a different schedule than the app store payouts below) — worth knowing
   that ad revenue and IAP revenue arrive through **two completely separate
   payment pipelines**, not one combined "app store payout."

Both of these are realistically **Task 6 (credit bag collection/wallet)
and Task 7 (level completion economy) territory** in the current plan
before they're meaningful — no rush to wire up real billing until the
in-game economy itself is implemented and tested.

---

## Part 6 — How the money actually gets to your bank account

This is the part with the most misconceptions, so it's worth being precise.

### Apple

1. In App Store Connect, accept the **Paid Apps Agreement** (this is
   required even for a free app with IAP).
2. Fill in **banking information** and **tax information** (a W-9 if you're
   a US person/entity, a W-8BEN or similar if not) under
   Agreements/Tax/Banking.
3. Apple collects and remits sales tax/VAT globally on your behalf
   automatically — you don't handle that part.
4. **Commission**: standard rate is **30%**. If your total App Store
   proceeds are under **$1M/year**, enroll in the **App Store Small
   Business Program** (a few clicks in App Store Connect) to drop that to
   **15%**. New developers auto-qualify — there's essentially no reason not
   to enroll immediately once you're set up.
5. **Payout timing**: Apple pays via direct deposit/EFT to your bank,
   within **45 days after the end of the Apple fiscal month** the sale
   happened in (Apple's fiscal months don't line up with calendar months).
   You also need to clear a small **minimum payment threshold per
   country/currency** — if you don't hit it, that balance just rolls into
   next month rather than being lost.

### Google Play

1. In Play Console, create a **Payments Profile** (Settings → Payments
   profile), which requires a verified bank account in the same country as
   the profile.
2. **Commission**: **15%** on the first **$1M/year** in earnings, **30%**
   above that. (Google has also been rolling out a split "service fee +
   billing fee" structure for subscriptions in some regions from mid-2026 —
   worth double-checking the current terms in Play Console once you're
   actually earning subscription revenue, since this is genuinely still
   shifting.)
3. **Payout timing**: monthly, starting **around the 15th**, for the *prior
   calendar month's* sales — e.g. August sales pay out around September
   15th. EFT typically lands in 2–3 business days; wire transfer 5–7 days
   (and needs a **$100 minimum balance** to trigger a wire payout
   specifically).

### The one thing both have in common

**No banking/tax setup = no payout, even if the app is live and "earning."**
Both stores will show you accruing proceeds in their dashboards well before
the paperwork is finished — that number isn't wrong, it's just sitting
there waiting for you to finish the Payments Profile / Tax & Banking forms.
Do this step **early**, ideally right after creating each developer account,
long before the app is ready to ship — it's pure admin with no dependency
on the game being finished, so there's no reason to leave it for launch day.

---

## Part 7 — The whole thing as one ordered checklist

Roughly in the order it makes sense to tackle them (testing-gate items
first, since they're the slowest):

1. [ ] Install Unity Hub + Unity 6.3 LTS (+ Android Build Support, + iOS
       Build Support if you have Mac access)
2. [ ] Create Apple Developer Program account, pay $99, enable 2FA
3. [ ] Create Google Play Console account, pay $25, complete identity
       verification
4. [ ] **Immediately** fill in Apple's Tax & Banking info + accept Paid
       Apps Agreement, and enroll in the Small Business Program
5. [ ] **Immediately** create your Google Payments Profile + verified bank
       account
6. [ ] Recruit 12+ testers for Google's closed-testing requirement — start
       this well before the app is feature-complete, it's a 14-day clock
7. [ ] Finish Tasks 5–10 of the core puzzle (boosters, credit wallet, level
       objectives, lives, the `BoardController`, level data)
8. [ ] Wire up Unity IAP + create matching product IDs in both consoles
       (only once the in-game economy — Tasks 6–7 — actually exists)
9. [ ] Set up an ad mediation SDK account (AdMob or similar) if the game
       will carry ads
10. [ ] Build store listings: icon, screenshots, descriptions, content
        rating questionnaires, data-safety/privacy disclosures
11. [ ] Submit Android to closed testing → wait out the 14 days → apply for
        production access
12. [ ] Submit iOS build via TestFlight for your own testing, then submit
        for App Store review
13. [ ] Once both are live: monitor each console's payments dashboard —
        first real payout typically lands 4–8 weeks after first sales,
        given the 45-day Apple cycle and the 15th-of-next-month Google cycle
