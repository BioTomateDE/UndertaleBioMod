# Undertale BioMod
It's basically debug mode, but better.

> [!NOTE]
> This mod is still in early development and will probably have some issues.
> It also lacks some features I still want to implement.
> Please report bugs and feature requests in the Issues page!

## Peakness
* Easily customizable by editing the CSX script.
* No extra resources needed; the csx file contains everything.
* Compatible (kinda) with different Undertale versions and mods.
* As long as you don't press the extra hotkeys to enable modules, it will act like vanilla.

## Features
* Show various hitboxes
  * Frisk's hitbox ("bbox")
  * Frisk's hitbox if you move one step
  * The collision rectangle that is cast when pressing Z for interacting with objects
  * Default position (wrongwarp spot) of every room
  * Interactables (NPCs, signs, etc)
  * Solids (walls mostly)
  * Doors / Room Transitions
  * Event triggers (cutscene triggers, phonecalls, Alphys' texts, etc) (not complete!)
* Show miscellaneous info in the top-left
  * Current room (ID and code name)
  * Current value of `global.interact`
  * Plot value
  * Step counter until an encounter happens
  * RNG minimum, maximum and mean stepcount for this room
  * Whether you are phasing ("noclipping")
  * Whether your menu is disabled
  * The battle group of the current/next battle
  * (In battle) The turn count
  * (In battle) The chance of successfully fleeing
  * (In battle) Attack damage for every frame + RNG variance
* Room switcher
  * Type a room ID or name
  * It can autocomplete with Tab
  * Doesn't need the `room_` prefix
  * Has rainbow text
* Speedhack
  * Slow down your game by 0.3x (customizable)
  * Speed up your game by 5x (customizable)
* Noclip
* Hotkey that unfreezes you (i.e. in cutscenes) and re-enables your menu
* Hotkey to move to the previous/next room
* Hold a key to automatically mash away dialogue (like holding C in Deltarune)
* Press key to get a punch card into the first slot of your inventory 

## How to use
1. Download the `UndertaleBioMod.csx` file.
2. If you have't already, download [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool).
3. Open UndertaleModTool and load Undertale 1.01.
4. In the top, click on **Scripts** and then on **Run other script**.
5. Select the `UndertaleBioMod.csx` file.
6. It should say "Patched successfully". If not, read the error message. Report an Issue here if you can't fix it.
7. Press Ctrl+S to save the modified data file.
8. (optional) Make a backup of your Undertale data file.
9. Overwrite the `data.win`/`game.unx` file in Undertale's game asset folder.
10. Launch the game! Press F1 for help.

## License
This script is licenced under [`GPL-3.0-only`](https://www.gnu.org/licenses/gpl-3.0.en.html).
