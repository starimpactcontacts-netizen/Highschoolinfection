# High School Infection

Asymmetrical multiplayer zombie-infection game set in a dark, rainy high school.
Humans hide and survive the night; zombies hunt and spread the infection by touch.
If even one human is alive when the timer runs out, the humans win. If everyone
gets infected, the zombies win.

> This is an early **playable prototype** — a blockout school, first-person
> movement, networked lobby, and the core infection/win-loss loop. Art,
> customization, voice chat, and the shop come later.

## How to run it

1. Install **Godot 4** (you already have the `.mono` build — that's fine).
2. Open Godot, click **Import**, and select this project's `project.godot`.
3. Press **F5** (or the ▶ Play button) to launch. You'll see the main menu.

## Testing multiplayer on one machine

You can run several copies of the game at once to fake a lobby:

1. In Godot, go to **Debug → Customize Run Instances…**
2. Enable it and set **Number of instances** to `3` (or more).
3. Press **F5**. Several game windows open.
4. In one window click **HOST GAME**. In the others, leave the IP as
   `127.0.0.1` and click **JOIN GAME**.
5. Everyone spawns in the school. The **host** sees a **START ROUND** button —
   click it to pick 2 random zombies and start the 5-minute timer.

To play with friends over the internet you'll need the host to port-forward
UDP **24565** (or use a tool like ZeroTier/Radmin VPN). Proper hosting comes later.

## Controls

| Action | Key |
| --- | --- |
| Move | W A S D |
| Look | Mouse |
| Jump | Space |
| Hide in / leave locker | E |
| Release / recapture mouse | Esc |

## Lockers

Humans can duck into a red locker (walk up, press **E**) to break line of sight
for up to **15 seconds** — then you're kicked out automatically, with a short
cooldown before you can hide again. Only one person per locker, and you can't
be infected while hidden. Use it to survive a chase, not to camp.

## Character customization (UI prototype)

From the title screen, hit **CUSTOMIZE CHARACTER**. This opens a full
customization screen with a **live preview doll** (drawn entirely in code — no
art assets yet) that updates instantly as you pick:

- Hair style + hair color
- Eye color
- Skin tone
- Uniform color
- Accessory (ribbon, glasses, headset, mask)

Every item carries a **rarity tier** (Common / Rare / Epic / Legendary) with a
matching swatch border — the same data model the cosmetics shop and monetization
will hang off later. Selections live in the `Cosmetics` autoload, so they persist
across screens and will drive the in-game character look. Falling sakura petals
on the menu/customization screens are procedural too (`scripts/petals.gd`).

## How the code is laid out

| File | What it does |
| --- | --- |
| `scripts/net.gd` | Autoload. Sets up input, hosts/joins, switches scenes. |
| `scripts/cosmetics.gd` | Autoload. Cosmetics catalog, rarities, and the local player's chosen look. |
| `scenes/main_menu.tscn` + `scripts/main_menu.gd` | Title screen (host / join / customize). |
| `scenes/customization.tscn` + `scripts/customization.gd` | Character customization UI. |
| `scripts/char_preview.gd` | Code-drawn live preview doll. |
| `scripts/petals.gd` | Procedural falling sakura petals overlay. |
| `scenes/game.tscn` + `scripts/game.gd` | The match: level, spawning, zombie assignment, timer, win/loss. Server-authoritative. |
| `scenes/player.tscn` + `scripts/player.gd` | One player. Same script for humans and zombies; infected state flips on touch. |

## Roadmap (post-prototype)

- Real high-school art + character models and animations
- Wire customization choices onto the in-game player mesh
- More cosmetic items + the actual shop UI
- Proximity voice + text chat (the mic-panic mechanic)
- Sabotage mechanics (lock doors, shove players, lockers)
- Dedicated servers + matchmaking
- Cosmetics shop + donations
