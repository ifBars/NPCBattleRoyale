# NPC Battle Royale – User Guide

This guide explains how to configure and run NPC tournaments using the in‑game UI.

## Opening the Config Panel

- Interact with the arena control panel and press "Config" to open.
- The panel can also be toggled by scripts via `ConfigPanel.Toggle()`.

## Layout Overview

The configuration panel contains three tabs:

- **Setup**: Choose arena, participant count per group, and which groups to include.
- **Settings**: Tournament format, arena rotation, advancement rules, flow timing.
- **Presets**: Load or save named configurations.

Use the Close button or the footer "Close" action to exit the panel.

## Setup Tab

- **Arena**: Select the battleground. Use the list of arenas provided by the manager.
- **Participants per Group**: Slider controlling how many NPCs enter each group stage match.
- **Select Groups**: Toggle which NPC groups participate. Two columns help quickly scan and select.

Tips:
- If no groups are selected, starting a tournament will warn and do nothing.

## Settings Tab

- **Shuffle Participants**: Randomizes participant order prior to grouping.
- **KO as Elimination**: Treats a knockout as an elimination.
- **Format**: Choose between:
  - Groups → Finals (group stages advance into final bracket)
  - Single Elimination (straight bracket, no groups)
- **Arena Rotation**: How arenas change between matches:
  - Fixed (same arena)
  - Alternate (switch in sequence)
  - Random (random choice per match)
- **Advance Per Group**: Number of top finishers moving from each group to finals.
- **Max FFA size (0 = no cap)**: Caps the number of NPCs in a single free‑for‑all heat. 0 means unlimited; higher values split large pools into multiple heats.
- **Heal winners between rounds**: Restores winner health before the next round.
- **Inter‑round delay (sec)**: Time to wait between matches.

Note: The Settings page is scrollable; use the mouse wheel to see all options.

## Presets Tab

- **Load Preset**: Select a stored configuration to apply immediately.
- **Save Current as Preset**: Saves your current configuration (arena, groups, participants, and settings) under the current or prompted name.

## Starting a Tournament

1. Configure settings on Setup and Settings tabs.
2. Click "Start Tournament" in the footer.
3. The system:
   - Sets the active arena
   - Builds the tournament flow based on your options
   - Begins running matches

## Arena Controls (World Objects)

Near each arena you will find a world control panel with interactable buttons:
- Start Round
- Aggro All
- Toggle Gates
- Stop/Reset
- Config (opens the configuration UI)

## Troubleshooting

- "Manager not ready": Ensure the `BattleRoyaleManager` has been initialized (it is created at runtime automatically). Try reloading the scene or game if the warning persists.
- No groups selected: Choose at least one group on the Setup tab.
- Participants not moving: The manager disables schedules and sets up combat; if NPCs still idle, ensure their behaviours aren’t overridden by other mods.

## Scripting Reference

- Open/close the UI:
  - `ConfigPanel.Show()` / `ConfigPanel.Hide()` / `ConfigPanel.Toggle()`
- Access the manager:
  - `BattleRoyaleManager.Instance`
  - `BattleRoyaleManager.ActiveArenaIndex` selects the arena index.
  - `BattleRoyaleManager.StartRound()` / `StopRound()` to control fights.

## Notes

- Some NPCs are ignored by default (e.g., story‑critical characters). See `BattleRoyaleManager.IgnoredNPCIDs`.
- The system disables schedules and enables combat during fights, then restores defaults after a round.
