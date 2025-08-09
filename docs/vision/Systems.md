\# Systems Design (Stations, Legacy, Economy)



\_Last updated: 2025-08-09\_



\## Economy (MVP Numbers)

\- Essence per click: 1 (cap 10/day)

\- Dungeon base reward: 20 × `RewardMultiplier`

\- Passive/sec: starts 0; first waifu gives +0.5/sec



\_Keep a tuning table here with current live values.\_



\## Stations (design stubs)



\### Farm

\- \*\*Minigame (MVP)\*\*: timing bar; hit “green” → yield Wheat xN.

\- \*\*Output\*\*: Wheat → feeds Kitchen; sells for small gold.

\- \*\*Automation\*\*: Manager Level reduces minigame frequency, adds passive/interval.



\### Kitchen

\- \*\*Minigame\*\*: ingredient order memory (2–4 steps).

\- \*\*Output\*\*: Meals → small essence boost on Sleep; gold sink/source toggle.

\- \*\*Automation\*\*: served/hour scales with level.



\### Alchemy

\- \*\*Minigame\*\*: recipe match (drag shapes); fail = weak potion.

\- \*\*Output\*\*: Potions → temporary battle buffs next day.

\- \*\*Automation\*\*: % auto-brew per day.



\### Blacksmith

\- \*\*Minigame\*\*: rhythm hammering (3–5 beats).

\- \*\*Output\*\*: Gear tokens → persistent small stat bumps.

\- \*\*Automation\*\*: queue length \& quality bonuses.



\## Legacy (Prestige)

\- \*\*Trigger\*\*: Player chooses “Fertilize” after FINAL station unlock milestone.

\- \*\*Reset\*\*: essence, upgrades, station levels.

\- \*\*Carryover\*\*: `LegacyBonus%`, starting floor, unlocked cosmetics.

\- \*\*Math (draft)\*\*: `NewLegacyBonus = Old + floor(total\_essence\_spent / 1,000) \* 2%` (TBD)



\## Save/Load Keys (draft)

\- Essence: current, passiveRate, clicksLeft, dayIndex

\- Upgrades: PurchasedIds\[]

\- Companions: owned ids, levels; managers assignment

\- Stations: level, progress bars, automation flags

\- Legacy: bonus%, prestigeCount



\## UX Notes

\- Always preview gains before committing: “Sleep: +12 essence (2 passive, 10 dungeon)”

\- Toasts: purchase, unlocks, battle win.



\## Tech Hooks (existing APIs to call)

\- Gather → `EssenceManager.TryClickHarvest()`

\- Sleep → `GameManager.Sleep()` / HUD wrapper

\- Enter Dungeon → `LoadScene("Battle")`

\- Reward → `EssenceManager.AddExternal(amount)`

\- Upgrades → `UpgradeManager.TryPurchase(so)`



