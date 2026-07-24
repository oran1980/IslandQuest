# Game Design Document (primary source)

- **`IslandQuest_GDD_v2_1.docx`** — the authoritative IslandQuest GDD, v2.1
  (binary; open in Word/Pages/LibreOffice).
- **`IslandQuest_GDD_v2_1.md`** — a plain-Markdown export of the same
  document, for easy reading, `grep`, and git diffs. Regenerate it from the
  `.docx` if the source changes. **If the two ever disagree, the `.docx` is
  authoritative.**

This is the primary source the `specs/` folder is derived from. When a spec
detail is ambiguous or seems wrong, check it here first — the `.md` is the
quickest way in.

## Notes / known gaps (things the GDD does *not* specify)

The GDD frames each level around a single generic **"level objective"** whose
completion is measured as a percentage (§7.3), with 1/2/3-star performance
tiers paying 20/35/55 credits (§6.2). It does **not** define:

- a **scoring formula** (points per cleared tile, combo multipliers) — there
  is no per-tile points system in the GDD at all;
- an enumeration of **objective types** — the `Score` / `Collect` /
  `ClearBoard` split in the code was introduced during implementation
  (Task 7), not taken from the GDD;
- what "clearing the board" means, or any core blocker-tile objective (the
  only "blocking tile" mention is §7.3's DifficultyAI near-miss mechanic,
  which is out of the M1 scope).

Island / level structure per GDD §8.2: **Island 1 (Coconut Isle) = Levels
1–30**; Island 2 (Ember Peak) = 31–70; Island 3 (Coral Abyss) = 71–120. M1's
"30 levels" is therefore all of Island 1.
