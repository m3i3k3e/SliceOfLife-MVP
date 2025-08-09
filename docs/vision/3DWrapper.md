\# 3D Wrapper — World-Space Vertical Slice



\_Last updated: 2025-08-09\_



\## Goal (Vertical Slice)

Deliver a playable loop using world-space interactables that call existing APIs (no new backend):

\*\*Altar (Gather) → Bed (Sleep) → Door (Enter Dungeon) → Battle → Reward → Back to Hub\*\*



\## Scene Map (Basement Hub)

\- \*\*Player Start\*\* (spawn)  

\- \*\*Altar of Essence\*\* → `EssenceManager.TryClickHarvest()` per interact press  

\- \*\*Bed\*\* → `HUD.Sleep()` or `GameManager.Sleep()`  

\- \*\*Dungeon Door\*\* → `LoadScene("Battle")`  

\- \*\*Upgrade Board\*\* (optional) → opens `UpgradesPanel`



\## Interaction Rules

\- \*\*Use key\*\*: `E` on focus (raycast 3m) or mouse click on hotspot.

\- Tooltips/UI: world-space label + on-screen prompt.



\## Camera \& Input

\- 3rd-person or fixed isometric? \*\*MVP\*\*: fixed orbit camera with mouse rotate + WASD.

\- Disable camera in Battle scene (separate camera rig inside `BattleRoot`).



\## Prefabs (new)

\- `InteractableBase` (collider + prompt + OnInteract UnityEvent)

\- `AltarInteractable` → calls `TryClickHarvest()`

\- `BedInteractable` → calls `Sleep()`

\- `DungeonDoorInteractable` → calls `LoadScene("Battle")`



\## Art \& Performance (MVP)

\- Greybox meshes; single URP lit material set; baked lighting off.

\- Budget: 50k tris scene, 1 directional light, no post.



\## Acceptance (done when)

\- \[ ] Interactables trigger existing APIs reliably.

\- \[ ] Tooltip \& cooldown feedback visible.

\- \[ ] Returning from Battle restores hub state (no leaks).

\- \[ ] Snapshot updated; CODEX APIs unchanged.



\## Stretch (after slice)

\- Footstep SFX, light dust VFX, simple NPC idle.



