# Game System Examples

These scripts are examples from a game project I worked on. Each one represents a snippet of a larger system — some are standalone, while others integrate with additional subsystems. All of these are scripts I either wrote entirely or heavily modified and expanded.

---

## PlayerCustomization

This script manages the player’s visual appearance.

**Responsibilities include:**
- Determining armor style and color  
- Managing cosmetic attachments  
- Handling the equipped weapon and any associated mods  

The goal of this system was to centralize all player appearance data, ensuring consistent synchronization between the UI and the in-game visuals. It also handled network syncing so that other players could correctly see each player’s configuration during multiplayer sessions.

---

## RogueLike_RoomGeneration

This system handles room generation using **ScriptableObjects** that define biome types and corresponding room layouts.

**How it works:**
1. The global **RogueLike Manager** loads the current biome.  
2. The **Room Generator** selects potential room templates for that biome.  
3. A seeded randomization system assembles the final layout, ensuring replayability while maintaining logical structure.

This approach allowed for dynamic yet coherent level generation across different biomes and story moments.

---

## GameStateManager

A streamlined system designed to manage overall game states.

Instead of using multiple Unity scenes, this setup runs the entire game within a single scene and swaps between active prefabs as needed.  
This significantly reduced load times keeping players in the action and minimizing downtime.
