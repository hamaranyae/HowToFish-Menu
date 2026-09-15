<p align="center">
  <img src="assets/banner.png" alt="Trickshot Menu" width="100%">
</p>

# Trickshot Menu

A mod for **How to Fish** that plays the trickshot minigame for you and adds a pile of casino cheats on top.

Made for **BepInEx 6** (the Unity Mono build). Press **F8** in game to open it.

## The main idea

When a fish goes flying off your line, the mod spins your camera a full 360°, snaps the aim onto the fish and fires — which stacks the whole multiplier chain (Aerial/Dogfight, Headshot, Last Bullet, One Shot One Kill, Overkill, the risk tiers) without you having to nail the timing yourself. Everything is toggleable:

- **Auto Trickshot** — spin and shoot on its own
- **Airborne Only** — leaves hooked fish alone, and it never shoots while you're reeling in or wound up near the reel
- **Jump For Aerial** — jumps just before the shot
- **One-Shot Helper** — big damage, the fish dies in one round
- **Force Last Bullet** — loads a single round so the shot always counts as "last bullet"
- **Aim Heads** — aims at the head for the headshot bonus
- sliders for spin speed, spin degrees, target lead, max range, and combo window
- a weapon filter so only certain guns trigger it (pistol / sniper / shotgun / all)

The **BONUS** tab shows which multipliers are lined up before you pull the trigger, and the **STATS** tab keeps count of trickshots, kills, your best combo and the session's worth (with a reset button).

## Casino stuff

The **ROULETTE** tab is the fun part:

- **Live predictor** — shows you the exact color the ball is sitting on while the wheel is still spinning
- **Always Win Roulette** — the table pays your bet color every time (black/red ×2, green ×35)

And for the weapon skin machine next to the table:

- **Free Spins** — it just rolls the machine for you while you stand there, no fish required
- **Force Rarity** — every spin lands a rare (red) or legendary (gold) skin, and it actually unlocks it for you

One honest caveat: all the casino options are host-side. They work in single player and in your own co-op lobby, but guests only see the happily-ever-after result.

## Install

You need **BepInEx 6.0.0-be.788** first.

1. If you don't have BepInEx yet, grab the runtime zip from the [Releases](https://github.com/hamaranyae/TrickshotMenu/releases) page (or from the [official BepInEx repo](https://github.com/BepInEx/BepInEx/releases)) and extract it into your game folder — the one that contains `How to Fish.exe`.
2. Extract the mod zip into that same folder so it ends up here:

```
BepInEx\plugins\TrickshotMenu\TrickshotMenu.dll
```

3. Launch the game, press **F8**.

The config file is auto-created at `BepInEx\config\trickshot.howtofish.cfg` on first launch if you ever want to hand-edit something.

## Keys

- **F8** — menu
- **F9** — manual trickshot (fires on the tracked fish immediately)
- **F7** — log window
- **F6** — full state dump to the log

All four are rebindable in the **SETTINGS** tab, which also has a guide button that walks through the whole menu.

## Building

```
dotnet build -c Release
```

Targets `netstandard2.1`. You'll need the game's `Assembly-CSharp.dll` and the BepInEx core assemblies referenced, same as any other BepInEx plugin.

## License

MIT. Unofficial fan project — not affiliated with the How to Fish devs. If something breaks, that's on me, not them.