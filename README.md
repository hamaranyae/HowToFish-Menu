# Trickshot Menu

An in-game mod menu for **How to Fish** that automates and powers up the 360° trickshot system, plus casino cheats for the roulette table and the weapon-skin slot machine.

Built for **BepInEx 6.0.0-be.788** (Unity Mono x64).

---

## Features

### CONTROL — automation & tuning
| Option | Effect |
| --- | --- |
| Auto Trickshot | automatically spins and fires at launched / free fish in range |
| Airborne Only | only targets untethered fish — never fires while you are reeling in or wound up close to the reel |
| Jump For Aerial | jumps before the shot for the **Aerial / Dogfight** bonus |
| One-Shot Helper | boosts damage so the fish dies in one round (**One Shot One Kill + Overkill**) |
| Force Last Bullet | loads exactly one round for the **Last Bullet** bonus |
| Aim Heads | headshot assist for the **Headshot** bonus |
| Spin Light FX / HUD | cosmetic light-sweep + vignette, and the top-left status card |
| Sliders | spin speed, spin degrees (style overkill), target lead, max range, combo window |
| Direction / Weapons | clockwise / counter-clockwise spin; which guns count — PISTOL / SNIPER / SHOTGUN / ALL |

### BONUS
Shows exactly which multipliers (Aerial, Headshot, Last Bullet, risk tiers) are lined up before you pull the trigger.

### STATS
Trickshots fired, kills, best combo, best multiplier, estimated earnings, last chain — with a session reset.

### ROULETTE — live predictor + cheats
- **Live landing predictor** — reads the exact slot under the ball while the wheel is still spinning.
- **Always Win Roulette** — the table always pays your bet color: BLACK/RED **x2**, GREEN **x35**.

### SLOT MACHINE — weapon skins
- **Free Spins** — auto-spins the skin machine while you stand beside it; no fish is deposited or destroyed.
- **Force Rarity** — every spin lands a marked skin: **RED** (rare-marked) or **GOLD** (legendary-marked) — and it is unlocked for you.

### SETTINGS
- Rebind **every** hotkey (click a key pill, then press the key — ESC cancels).
- Animated **MOD GUIDE** overlay explaining each function.

### LOG
Live mod log window with a CLEAR button.

---

## Install

1. **BepInEx 6.0.0-be.788** (Unity Mono). Either download it from the
   [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases)
   (`BepInEx_win_x64_6.0.0-be.788.zip`) or use the bundled runtime zip
   from the **dist/** folder. Extract it into the folder containing `How to Fish.exe`.
2. **The mod.** Drop `BepInEx\plugins\TrickshotMenu\TrickshotMenu.dll` into
   `BepInEx\plugins\TrickshotMenu\` (create the folder if needed).
3. Launch the game and press **F8** to open the menu.

Your game folder should look like:

```
How to Fish.exe
winhttp.dll
doorstop_config.ini
BepInEx\
  core\...
  plugins\
    TrickshotMenu\
      TrickshotMenu.dll
```

Settings are saved to `BepInEx\config\trickshot.howtofish.cfg` on first launch.

---

## Keybinds (default)

| Key | Action |
| --- | --- |
| `F8` | open / close the menu |
| `F9` | manual trickshot right now |
| `F7` | open / close the log window |
| `F6` | print a full state dump to the log |

All rebindable in **SETTINGS → KEYBINDS**.

---

## Notes

- Casino cheat options (**always win roulette**, **forced slot rarity**, **free spins**) are **host-side**: they work in single player and in your own co-op lobby, and everyone in the lobby sees the result.
- The roulette landing predictor works from the moment you stand by the table; bets lock before the spin, so there is no fair pre-bet read — that is exactly what *Always Win Roulette* is for.

---

## Building from source

- Target framework: `netstandard2.1`
- Requires references to the game's managed assemblies (`Assembly-CSharp.dll`) and the BepInEx core assemblies.

```powershell
dotnet build -c Release
```

Output goes to `BepInEx\plugins\TrickshotMenu\TrickshotMenu.dll` relative to the project.

---

## Disclaimer

Unofficial fan project. Not affiliated with the developers of How to Fish. Use at your own risk — modifies memory of the game process while it runs.