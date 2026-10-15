# Liar's Bar Tactical Suite - Engineering Log & Architecture Specification

## Architecture Overview & Subsystem Index
- **Engine / Runtime**: Unity 2022.3.27f1 x64 IL2CPP
- **Modding Framework**: MelonLoader v0.7.3 Open-Beta (`net6.0`)
- **Networking Stack**: Unity Mirror (`Il2CppMirror.dll`) over Steam P2P Transport (`Telepathy` / `SimpleWebTransport`). **Note:** Photon Engine is NOT used anywhere in this title.
- **Target Assembly**: `Assembly-CSharp.dll` (via `MelonLoader\Il2CppAssemblies`)
- **Deployment Target**: `Liar's Bar\Mods\LiarsBarMod.dll`

---

## Technical Deep-Dive: Networking Architecture & "Server Stuff" (Photon vs Mirror)

### 1. Why "Photon" Cannot Be Used
- **Stack Incompatibility**: The user inquired about using "Photon" (PUN, Photon Fusion, Quantum) for server-side manipulation. Liar's Bar does not link or interface with Photon in any capacity. The game's network layer is built entirely on **Mirror Networking** (`NetworkBehaviour`, `NetworkIdentity`, `NetworkConnection`, `SyncList<T>`, `SyncVar`). Attempting to introduce Photon SDKs cannot interact with Liar's Bar game servers or peer lobbies.
- **Protocol Boundary**: Mirror communication relies on binary `NetworkWriter`/`NetworkReader` bitstreams exchanged between clients and the match host.

### 2. The Host vs Client Authority Model
- **Network Topology**: Liar's Bar operates on a **Peer-to-Peer with Player Host** topology (one player in the lobby acts as `isServer = true`, while others connect as `isClient = true`).
- **Hidden Game State (Fog of War)**:
  - *Dice*: At round start, the server rolls dice and transmits them exclusively to the owning client via `TargetSetDiceValues` (a private TargetRPC). Opposing players' clients receive **zero network packets** containing opponent dice values until the reveal/showdown phase (`RpcSetAllDice`). Opponent dice in client memory are either uninstantiated or default to 1. No client-side memory scanner can read what has not been transmitted over the wire.
  - *Dice Modification*: There is no client-to-server `[Command]` for "SetDiceValues" mid-round. Any local write to `DiceValues` or `dicerenders` as a client is cosmetic/visual (`[L]`). When showdown occurs, the host's canonical memory evaluates the round.
- **How Real Authority Is Achieved**:
  - **Host Your Own Lobby**: When hosting, your machine is the authoritative server (`isServer == true`). All opponent dice, cards, revolver states, and drink counts reside in your RAM in plaintext, and all `[H]` host commands execute with full authority.
  - **Host Migration Hijacking**: Mirror contains `ServerSnapDeadForMigration()`. If the host leaves or disconnects, Mirror migrates the server role. Exploiting the migration election allows promoting the local client to `isServer`.
  - **Client-Authorized Commands (`[Command]`)**: Actions with valid Mirror command endpoints (e.g. `PlaceBet`, `CallLier`, `ThrowCards`, `SwapcardCMD`, `SetCardsCmd`) execute over the network from client mode.

---

## Detailed Version Changelog (v3.0.0 -> v4.4.0)

### v4.4.0 (Current Release)
- **Live Authority Indicator**:
  - Dynamically detects and displays whether your local client holds server authority (`[HOST]` in gold vs `[CLIENT]` in green) directly in the menu title bar header.
  - Dynamically evaluates server role across all 10 game types (`DiceGamePlay`, `ChaosDeckGameplay`, `DeckGameplay`, `ChaosGamePlay`, `BlorfGamePlay`, `PokerGamePlay`, `TexasGamePlay`, `SpinGamePlay`, `RouletteGamePlay`).
- **Flexible Resizing Engine**:
  - Expanded base dimensions from `700x560` to `760x600` for zero text truncation across all 14 tabs.
  - Adjusted minimum window bounds from `640x500` down to `500x400`, allowing players on lower resolutions or windowed modes to shrink the UI as needed while expanding as far as necessary.
- **Enhanced Poker & Texas Hold'em Controls**:
  - Added Texas Hold'em quick presets: `Pocket Aces (AA)`, `Pocket Kings (KK)`, `Pocket Queens (QQ)`, `Big Slick (AK)`.
  - Added Texas bullet step buttons: `Bullet +1` and `Bullet -1` alongside `Reset Bullets [H]`.
  - Added Russian Poker command triggers: `Sync Cards CMD [C]` (`SetCardsCmd`) and `Swap Card CMD [C]` (`SwapcardCMD(0)`).
- **Hardened Multi-Vector Player Drink Pipeline**:
  - Restructured player `Drink` execution in `TacticalMenu.ForceDrinkPlayer`:
    1. Direct `NetworkdrinkCount++` on target's `DiceGamePlay`.
    2. Reflection-safe invocation of `DiceGamePlay.Drink()` if present.
    3. Server RPC invocation of `Manager.DrinkRpc` and `Manager.DrinkCmd`.
    4. Reflection-safe invocation of `CharController.Drink()`.
- **Mirror Networking Master Dissection**:
  - Extracted and verified Mirror's internal command routing in `NetworkServer.cs` and `RemoteCalls.cs`.
  - Documented the strict authority boundary where `requiresAuthority = true` commands are validated on the server side against `identity.connectionToClient == conn`.

### v4.3.0
- **Authority System Tagging**:
  - Explicitly tagged every UI button, tab, and routine with its network execution boundary:
    - `<color=#55FFAA>[C]</color>` **Client Supported**: Works as a regular connected client.
    - `<color=#FFDD55>[H]</color>` **Host Required**: Requires server authority (`isServer == true`).
    - `<color=#8899AA>[L]</color>` **Local Visual**: Client-side render modification only.
- **Expanded Poker Suite**:
  - Quick Hand Presets: `Pair of Kings (KK)`, `Pair of Aces (AA)`, `King & Ace (KA)`, `Royal Pair (QK)`.
  - Texas Hold'em: `Force Fold Others [H]`, `Unlimited Bullets (99) [C]`, bullet step controls, table cards readout.
- **Expanded Tactical Hotkey Matrix**:
  - `[Insert]` / `[F1]`: Toggle Menu Window
  - `[P]` / `[F2]`: Live Game State & Symbol Dump to Desktop
  - `[L]` / `[F3]`: Toggle Keybind Overlay Guide
  - `[F4]`: Quick Safe Revolver (Offset bullet from chamber)
  - `[F5]`: Quick Deadly Revolver (Align bullet to current chamber)
  - `[F6]`: Quick God Mode (0 bullets / Roulette never die)
  - `[F7]`: Kill All Others `[H]`
  - `[F8]`: Force End Game Win `[H]`

### v4.2.0 - Theme & Visual Overhaul (Inkwell Dark Glass)
- **Custom Texture Engine (`Theme.cs`)**:
  - Eliminated default Unity IMGUI grey bubbly bevel boxes.
  - Zero-allocation cached 1x1 solid/gradient texture renderer (`Theme.DrawPanel`, `Theme.DrawBorder`).
  - Obsidian Dark Glass color palette: Background `rgba(0.04, 0.05, 0.09, 0.96)`, 1.5px cyan border `#00FFB0`.
- **Tactical Visual Badges (`UiBuilder.cs`)**:
  - Color-coded Card chips: Gold `[ K ]`, Pink `[ Q ]`, Cyan `[ A ]`, Amber `[ J ]`, Crimson `[ DEVIL ]`.
  - Unicode Dice chips: `[ ⚀ 1 ]` through `[ ⚅ 6 ]`.
  - 6-Cylinder Revolver Radar: Visual cylinder radar (`[ ◉ ◯ ● ◯ ◯ ◯ ]`) showing current chamber and bullet alignment.
  - Tactical power toggles: `<color=#00FFAA>[● ON]</color>` vs `<color=#667788>[○ OFF]</color>`.
- **Window Management**: Header mouse drag, bottom-right corner resize grip (`//`), dynamic auto-expanding scroll view driven by `UiBuilder.TotalHeight` (zero clipping).
- **Telemetry Footer**: Real-time FPS counter, match mode badge, keybind hints.

### v4.1.0 - Poker & Method Unstripping Hardening
- **Poker Class Resolution**:
  - Discovered and resolved bug where Poker cards were accessed as `Card` instead of `PokerCard`.
  - Implemented `PokerController` handling `PokerCard.cardtype`, `PokerCard.SetCard()`, `PokerGamePlay.CardTypes`, and `fullbullets` SyncList.
- **Zero-Strip Defensive Execution**:
  - Isolated Unity stripped IL2CPP methods (`ApplyMeshForCardType`, `ApplyHandMaterial`, `ReleaseFixedVisuals`) inside individual try-catch blocks.
  - Prevents `MissingMethodException` from crashing the OnGUI frame or breaking tab navigation.

### v4.0.0 - Modular Architecture & Desktop Dumper
- Refactored monolithic `Core.cs` into modular subsystems: `UI/`, `Features/Cards/`, `Features/Dice/`, `Features/Poker/`, `Features/Dev/`.
- Built `StateDumper.cs` (modeled after `DNKW` state dumper):
  - Dumps live session telemetry, manager states, and player rosters directly to `Desktop\liarsbar_dump.txt`.
  - Performs live IL2CPP reflection across 14 game classes, dumping all unstripped fields, properties, and methods with argument signatures.
- Implemented multi-vector Dice ESP inspecting `DiceValues`, physical cup `dicerenders[i].Face`, and child `Il2Cpp.Dice` components.

### v4.2.0 - Texas Hold'em Royal Deck Architecture & Auto-Best Hand Solver
- **Texas Hold'em Architecture Discovery**:
  - Uncovered the actual 3D card component for Texas Hold'em: `Il2Cpp.TexasCard` (with `CardNumber`, `SetCardWithoutMesh(int)`, and `SetMesh()`). Previous card modifications only targeted `Card` and `PokerCard`, leaving physical 3D cards unchanged.
  - Resolved `tg.Cards` typing: `tg.Cards` is `Il2CppSystem.Collections.Generic.List<Il2Cpp.TexasCard>` (not `List<GameObject>`).
  - Identified the game's 20-card Royal Hold'em deck structure:
    - 4 suits: 0 = ♠ Spades, 1 = ♥ Hearts, 2 = ♦ Diamonds, 3 = ♣ Clubs.
    - 5 ranks: 0 = 10, 1 = Jack, 2 = Queen, 3 = King, 4 = Ace.
    - Card ID mapping: `cardId = suit * 5 + rank`. (e.g., Hand `[5, 8]` from live dump = 10♥ and K♥).
- **Multi-Layer Synchronization Protocol (`TexasController.cs`)**:
  - Layer 1: Memory / RAM list `tg.CardTypes[slot] = newCardId`.
  - Layer 2: 3D Table Mesh & Material: `tc.CardNumber = newCardId; tc.SetCardWithoutMesh(newCardId); tc.SetMesh();`.
  - Layer 3: Mirror Network RPCs: `tg.SetRevealCardsCMD(c0, c1)` + `tg.SetRevealCards(c0, c1)` + `tg.SetCardsCmd()`.
  - Layer 4: Switch system: `tg.SetSwitch(slot, newCardId)`.
- **Dynamic "Auto Set Best Cards (Based on Pool)" Solver**:
  - Live inspection of `TexasGamePlayManager.OpenTableCards` (community cards on table).
  - Evaluates all available 2-card combinations against the board to determine mathematically optimal hand rank:
    - Royal Flush > Four of a Kind > Full House > Flush > Straight > Three of a Kind > Two Pair > One Pair > High Card.
  - Automatically identifies the highest-ranking hole cards and immediately patches the local hand, visual 3D meshes, and server reveal state.
  - Added live community card readout and calculated rank indicator in the Tactical GUI.
- **Symbol Dumper Enhancements (`StateDumper.cs`)**:
  - Added `TexasGamePlayManager.OpenTableCards` and `AllTableCards` dump to `liarsbar_dump.txt`.
  - Added `TexasCard` and `TexasGamePlayManager` to full IL2CPP reflection symbol dumping.

---

## Roadmap & Next Objectives

1. **Automated Host Migration Hijacking**:
   - Analyze `ServerSnapDeadForMigration()` and Mirror's host migration handshake.
   - Implement an automated routine to trigger migration and bias the server election so the local client becomes `isServer` in any public lobby.
2. **2D/3D ESP Bounding Boxes & Snaplines**:
   - Port `DNKW RenderHelper.cs` (using `Shader.Find("Hidden/Internal-Colored")` and `GL.QUADS`) to draw physical bounding boxes, cup highlights, and player snaplines in 3D world space.
3. **Raw NetworkWriter Packet Crafting**:
   - Hook Mirror's `NetworkConnection.Send()` to craft and inject raw commands, testing for unauthenticated RPC execution vulnerabilities in game server handlers.
4. **Automated Round Predictor**:
   - Combine alive dice counts with probability distribution algorithms to calculate exact statistical odds for "Call Liar" vs "Spot On" during the bidding phase.

