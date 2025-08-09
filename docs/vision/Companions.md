\# Companions \& Cards (Waifu Layer)



\_Last updated: 2025-08-09\_



\## Purpose

Unify “waifu collecting” with combat cards + station managers without bloating the core loop.



\## Data Model (SO-first)

\- \*\*CompanionSO\*\*

&nbsp; - id, displayName, portrait (sprite), rarity (Common/Rare/Epic/Story)

&nbsp; - tags (Class: Knight/Mage/Rogue/Cleric; Temperament; Biome Affinity)

&nbsp; - stationRole (None/Farm/Kitchen/Alchemy/Blacksmith)

&nbsp; - startingCards: List<CardSO> (references)

&nbsp; - passiveBuffs: e.g., `+0.5 essence/sec`, `+10% dungeon reward`

\- \*\*CardSO\*\* (existing)

&nbsp; - add `icon`, `rarity`, `cost` (future: energy), `action` (Attack/Guard/Mend/SkillX)



\## Acquisition

\- Early MVP: fixed recruits via upgrade or dungeon milestone.

\- Later: drops from bosses/chests; pity counter per rarity.



\## Progression

\- Companion level (uses essence or secondary currency).

\- Card upgrades: +damage or alt effects; keep numbers small.



\## Station Managers (4 “Story Harem”)

\- Unique manager per station; leveling reduces minigame need, increases auto-output.

\- UI: assign dropdown; show delta to output.



\## Combat Mapping

\- Selected party (1–3 companions) defines \*\*hand pool\*\*.

\- MVP: static 3-card hand; Later: draw/discard from party deck.



\## Art Pipeline

\- Portrait spec: 512–1024px, bust, neutral BG.

\- Batch SD/ComfyUI prompts kept in `docs/art/prompts.md` (WIP).



