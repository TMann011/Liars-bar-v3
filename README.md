# LiarsBarMod v3.0

[![Version](https://img.shields.io/badge/version-3.0.0-blue)](https://github.com/)
[![Target](https://img.shields.io/badge/game-Liar's%20Bar%20v0.962-orange)](https://store.steampowered.com/)
[![Framework](https://img.shields.io/badge/.NET-6.0-purple)](https://dotnet.microsoft.com/)
[![Loader](https://img.shields.io/badge/MelonLoader-0.7.3%2B-red)](https://melonwiki.xyz/)

Multi-feature mod for **Liar's Bar** (Steam) v0.962. A 14-tab cheat menu covering cards, dice, poker, roulette, players, stats, skins and more.

> **Warning:** Using this mod online can get your account flagged or banned. It modifies game state in ways the developers did not intend. Use at your own risk.

---

## Features

### Cards & Dice
- See all players' cards and dice in real-time
- Edit card values: single or bulk (King / Queen / Ace / Jack / Devil)
- Edit dice values: individual or all
- Set all cards King/Queen with hotkeys

### Revolver
- Modify bullet position (+/-1)
- Set safe or death chamber
- Modify revolver for any player

### Game Modes
- **Poker:** God mode, card value, win now
- **Texas:** God mode, unlimited bullets, force fold, god save
- **Spin:** Local slot manipulation
- **Roulette:** Never die, cover mode

### Players
- View/edit other players' cards, dice and revolvers
- Kill, force-drink, kick or disconnect players
- Host/Client indicators
- Anti-kick protection (runs automatically in lobby and in-game)
- Out-of-turn actions: force throw, call liar

### Self & Skins
- Name change
- 360 head toggle, WASD movement, emotes
- XP, level and wins editing
- Unlock all skins
- Save/revert cheat states

### UI
- 14-tab GUI: Info, Cards, Dice, Spin, Poker, Move, Stats, Skins, Players, Dev, Self, Global, Exploit, Support
- Resizable, draggable menu window
- Fully color-customizable: menu BG, text, tab active/inactive, content BG with RGBA sliders
- Settings tab always visible regardless of game state

## Keybinds

| Key | Action |
|-----|--------|
| INSERT | Toggle menu |
| L | Keybind help |
| P | Dump state to Desktop |
| `[` | Force-refresh game detection |
| F2 | Toggle 360 head |
| F3–F5 | Emotes 1–3 |
| F6 / F7 | Set all cards King / Queen |
| F8 / F9 | Bullet -1 / +1 |

## Installation

1. Install **MelonLoader v0.7.3+** on Liar's Bar.exe
2. Copy `LiarsBarMod.dll` into `[game folder]\Mods\`
3. Launch Liar's Bar
4. Press INSERT to open the mod menu

## Building from source

- Requires .NET 6.0 SDK
- Reference **MelonLoader 0.7.3+** and the game's `Assembly-CSharp.dll`
- Build with: `dotnet build LiarsBarMod.csproj`

## Notes & Known Limitations

- **Dice changes require host** (game design limitation)
- Cards/kills work from any client via Mirror Commands
- Network sync is best-effort — internal Cmd method names are resolved heuristically at runtime, not guaranteed on every game version
- Anti-kick runs automatically in lobby and in-game
- Supports 10 game modes with multi-layer detection (CharController → Managers.Players → scene scan)

## Disclaimer

This project is for **educational and research purposes only**. Viewing, modifying, or exploiting game state in multiplayer games may violate the game's Terms of Service. The authors assume no responsibility for any bans, penalties, or consequences resulting from use of this software.

---

**v3.0.0 (2026-08-08)** — full changelog in [DEVLOG.md](DEVLOG.md)