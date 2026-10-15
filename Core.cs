using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using LiarsBarMod.UI;
using LiarsBarMod.Features.Cards;
using LiarsBarMod.Features.Dice;
using LiarsBarMod.Features.Poker;
using LiarsBarMod.Features.Dev;
using Il2CppSteamworks;
using Il2CppTMPro;

[assembly: MelonInfo(typeof(LiarsBarMod.Core), "LiarsBarMod", "4.4.0", "Inkwell")]
[assembly: MelonGame("Curve Animation", "Liar's Bar")]

namespace LiarsBarMod
{
    public class Core : MelonMod
    {
        public enum GameType { None, Default, MatchMaking, Chaos, ChaosDeck, Deck, Dice, Poker, Texas, Spin, Roulette }

        private GameType _gameType = GameType.None;
        private Manager _mgr;
        private GameObject _me;
        private GameObject[] _players;

        public static Vector2 CardsHudPos = new Vector2(15f, 15f);
        private static bool _isDraggingCardsHud = false;
        private static Vector2 _cardsHudDragOffset;

        private float _lastRefreshTime = 0f;
        private bool _wasMenuOpen = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("  Inkwell's Liar's Bar Menu v4.4");
            MelonLogger.Msg("  Press [Insert] for Menu, [P] to Dump State");
            MelonLogger.Msg("=================================================");
        }

        public override void OnLateUpdate()
        {
            if (TacticalMenu.MenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void OnUpdate()
        {
            // Cursor Lock & Visibility Controller (Allows moving mouse freely in menu)
            if (TacticalMenu.MenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _wasMenuOpen = true;
            }
            else if (_wasMenuOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _wasMenuOpen = false;
            }

            // Hotkeys
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.F1))
            {
                TacticalMenu.MenuOpen = !TacticalMenu.MenuOpen;
                if (TacticalMenu.MenuOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.F2))
            {
                StateDumper.DumpStateToDesktop(_gameType.ToString(), _mgr, _me, _players);
            }

            if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.F3))
            {
                TacticalMenu.ShowKeys = !TacticalMenu.ShowKeys;
            }

            if (Input.GetKeyDown(KeyCode.F4) && _me != null)
            {
                CardController.SetRevolverSafe(_me);
            }

            if (Input.GetKeyDown(KeyCode.F5) && _me != null)
            {
                CardController.SetRevolverDeath(_me);
            }

            if (Input.GetKeyDown(KeyCode.F6) && _me != null)
            {
                var pg = _me.GetComponent<PokerGamePlay>();
                if (pg != null) PokerController.EnableGodMode(pg);
                var tg = _me.GetComponent<TexasGamePlay>();
                if (tg != null) { tg.NetworkBullets = 0; try { tg.BulletReset(); } catch { } }
                var rg = _me.GetComponent<RouletteGamePlay>();
                if (rg != null) rg.canfire = false;
            }

            if (Input.GetKeyDown(KeyCode.F7) && _players != null)
            {
                foreach (var p in _players)
                {
                    if (p != _me)
                    {
                        try { var dk = p.GetComponent<DeckGameplay>(); if (dk != null) { dk.CmdKillPlayer(); continue; } } catch { }
                        try { var cd = p.GetComponent<ChaosDeckGameplay>(); if (cd != null) { cd.CommandBeDead(); continue; } } catch { }
                        try { var bg = p.GetComponent<BlorfGamePlay>(); if (bg != null) { bg.CommandBeDead(); continue; } } catch { }
                        try { var pg = p.GetComponent<PokerGamePlay>(); if (pg != null) { pg.CommandBeDead(); continue; } } catch { }
                        try { var tg = p.GetComponent<TexasGamePlay>(); if (tg != null) { tg.CommandBeDead(); continue; } } catch { }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F8) && _mgr != null && _me != null)
            {
                try { _mgr.ForceEndGameWithWinner(_me.GetComponent<PlayerStats>()); } catch { }
            }

            if (Input.GetKeyDown(KeyCode.F9) && _me != null)
            {
                CardController.CallLiarAnytime(_me);
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                TacticalMenu.ForceHostMigration(_players, _me);
            }

            // Periodic session state refresh (every 0.4 seconds)
            if (Time.time - _lastRefreshTime > 0.4f)
            {
                _lastRefreshTime = Time.time;
                RefreshSessionState();
            }

            // Anti-Kick Background Protection
            if (TacticalMenu.AntiKick)
            {
                RunAntiKick();
            }

            // God Save Toggle — enforce every frame
            if (TacticalMenu.GodSaveToggle && _me != null)
            {
                try
                {
                    var tg = _me.GetComponent<TexasGamePlay>();
                    if (tg != null)
                    {
                        tg._revolverGodSaveLocal = true;
                        tg._revolverWillDieLocal = false;
                    }
                    var pg = _me.GetComponent<PokerGamePlay>();
                    if (pg != null)
                    {
                        PokerController.SetShotOverride(pg, PokerController.ShotOutcomeOverride.MustSurvive);
                        PokerController.SetNextShotSafe(pg);
                    }
                }
                catch { }
            }

            // Russian Poker Shot Outcome Enforcement (Die on Shot / Survive Shot)
            if (_gameType == GameType.Poker || (_me != null && _me.GetComponent<PokerGamePlay>() != null))
            {
                try
                {
                    PokerController.UpdateFrame(_players, _me);
                }
                catch { }
            }

            // Dynamic RGB Name Tag Sync (Broadcasts dynamic RGB tag/color to all players)
            UpdateDynamicRgbNameTag();

            // Hand & Played Card Dynamic Tracking
            PlayerCardTracker.Update(_mgr, _players);

            // Card Placing Limit Bypass (Allows selecting & throwing up to 5 cards) - STRICTLY guarded to active match only
            if (_me != null && _mgr != null && _mgr.GameStarted && _gameType != GameType.None)
            {
                if (TacticalMenu.BypassCardLimit)
                {
                    CardController.EnforceBypassCardLimit(_me);
                }
                else
                {
                    CardController.RestoreDefaultCardLimit(_me);
                }
            }

            // Liar's Spin: Death Spin Immunity (Guarantees local player survives death spin)
            if (TacticalMenu.DeathSpinImmunity && _gameType == GameType.Spin && _me != null)
            {
                try
                {
                    var s = _me.GetComponent<SpinGamePlay>();
                    if (s != null && s.isDeadSpinMoment)
                    {
                        s.isDeadSpinMoment = false;
                        var spinMgr = UnityEngine.Object.FindFirstObjectByType<LiarsSpinGameplayManager>();
                        if (spinMgr != null)
                        {
                            spinMgr.isDeadSpin = false;
                            try { spinMgr.NetworkisDeadSpin = false; } catch { }
                        }
                        try { s.RpcPlaySaveAnim(); } catch { }
                    }
                }
                catch { }
            }

            // Always Have Turn Enforcement (Play cards without waiting)
            if (TacticalMenu.AlwaysMyTurn && _me != null)
            {
                EnforceAlwaysMyTurn();
            }
        }

        private void EnforceAlwaysMyTurn()
        {
            if (_me == null) return;
            try
            {
                var ps = _me.GetComponent<PlayerStats>();
                if (ps != null)
                {
                    if (!ps.NetworkHaveTurn) ps.NetworkHaveTurn = true;
                    if (_mgr != null && _mgr.NetworkActivePlayerSlot != ps.Slot)
                    {
                        _mgr.NetworkActivePlayerSlot = ps.Slot;
                    }
                }

                var dk = _me.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    dk.lookBlockedUntilTime = 0f;
                    dk.liarLookHoldActive = false;
                }

                var cd = _me.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    cd.lookBlockedUntilTime = 0f;
                    cd.liarLookHoldActive = false;
                }
            }
            catch { }
        }

        private void UpdateDynamicRgbNameTag()
        {
            try
            {
                string baseName = TacticalMenu.BasePlayerName;
                if (string.IsNullOrEmpty(baseName))
                {
                    try { baseName = SteamFriends.GetPersonaName(); } catch { }
                    if (string.IsNullOrEmpty(baseName)) baseName = "Inkwell";
                }

                string tag = TacticalMenu.SelectedTag;
                bool rgbTag = TacticalMenu.RgbTagEnabled;
                bool rgbName = TacticalMenu.RgbNameEnabled;
                float speed = TacticalMenu.RgbSpeed;
                string staticHex = TacticalMenu.NameColorHex;
                if (string.IsNullOrEmpty(staticHex)) staticHex = "#FFFFFF";

                bool customCol = TacticalMenu.CustomColorEnabled;
                bool hasTag = (tag != "None" && !string.IsNullOrEmpty(tag));
                bool hasColor = customCol || rgbTag || rgbName || (!string.IsNullOrEmpty(staticHex) && !string.Equals(staticHex, "#FFFFFF", StringComparison.OrdinalIgnoreCase));

                string hex = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB((Time.time * speed) % 1.0f, 1.0f, 1.0f));

                string tagCol = rgbTag ? $"#{hex}" : staticHex;
                string nameCol = rgbName ? $"#{hex}" : staticHex;

                string displayTag = hasTag ? (hasColor ? $"<color={tagCol}>{tag}</color> " : $"{tag} ") : "";
                string displayName = hasColor ? $"<color={nameCol}>{baseName}</color>" : baseName;
                string fullRichName = $"{displayTag}{displayName}";

                // Clean string for table gameplay and network chat sync (avoids raw <color> markup appearing on 3D table text)
                string cleanSyncName = hasTag ? $"{tag} {baseName}" : baseName;

                Color tableColor = Color.white;
                if (hasColor)
                {
                    if (rgbName || rgbTag)
                        ColorUtility.TryParseHtmlString($"#{hex}", out tableColor);
                    else
                        ColorUtility.TryParseHtmlString(nameCol, out tableColor);
                }

                // 1. In Match / Table Gameplay: Enforce richText = true so formatted color tags render without displaying raw markup
                string activeDisplayName = hasColor ? fullRichName : cleanSyncName;
                if (_me != null)
                {
                    var ps = _me.GetComponent<PlayerStats>();
                    if (ps != null)
                    {
                        if (ps.NameText != null)
                        {
                            ps.NameText.richText = true;
                            ps.NameText.parseCtrlCharacters = true;
                            ps.NameText.color = tableColor;
                            ps.normalColor = tableColor;
                            ps._nameAnimStartColor = tableColor;
                            ps._nameAnimEndColor = tableColor;
                            ps.NameText.text = activeDisplayName;
                            try { ps.NameText.ForceMeshUpdate(true, true); } catch { }
                        }
                        if (_mgr != null && ps.Slot >= 0)
                        {
                            try
                            {
                                if (_mgr.NameTexts != null && ps.Slot < _mgr.NameTexts.Count)
                                {
                                    var wt = _mgr.NameTexts[ps.Slot];
                                    if (wt != null)
                                    {
                                        if (wt.m_TextComponent != null)
                                        {
                                            wt.m_TextComponent.richText = true;
                                            wt.m_TextComponent.parseCtrlCharacters = true;
                                            wt.m_TextComponent.color = tableColor;
                                            wt.m_TextComponent.text = activeDisplayName;
                                            try { wt.m_TextComponent.ForceMeshUpdate(true, true); } catch { }
                                        }
                                        try { wt.SetText(activeDisplayName); } catch { }
                                    }
                                }
                                _mgr.SetText(ps.Slot, activeDisplayName);
                            }
                            catch { }
                        }
                    }
                }

                // Ensure all match players have richText enabled on their 3D nametags
                if (_players != null)
                {
                    foreach (var pl in _players)
                    {
                        if (pl == null || pl == _me) continue;
                        try
                        {
                            var oppPs = pl.GetComponent<PlayerStats>();
                            if (oppPs != null && oppPs.NameText != null && !oppPs.NameText.richText)
                            {
                                oppPs.NameText.richText = true;
                                oppPs.NameText.parseCtrlCharacters = true;
                                try { oppPs.NameText.ForceMeshUpdate(true, true); } catch { }
                            }
                        }
                        catch { }
                    }
                }

                // 2. In Match Notification / Message UI Components & Global Scene Text Sanitizer
                SanitizeAndEnableRichText();

                // 3. In Lobby: PlayerObjectController & LobbySlot 3D Nametags
                PlayerObjectController localPoc = null;
                var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                if (allPocs != null)
                {
                    ulong mySteamId = 0;
                    try { mySteamId = SteamUser.GetSteamID().m_SteamID; } catch { }

                    for (int i = 0; i < allPocs.Length; i++)
                    {
                        var poc = allPocs[i];
                        if (poc == null) continue;
                        bool isLocal = poc.isLocalPlayer || poc.isOwned || poc.authority || (mySteamId != 0 && poc.NetworkPlayerSteamID == mySteamId);
                        if (isLocal)
                        {
                            if (localPoc == null) localPoc = poc;
                            if (poc.NameText != null)
                            {
                                poc.NameText.richText = true;
                                poc.NameText.color = Color.white;
                                poc.NameText.text = fullRichName;
                            }
                        }
                    }
                }

                int localSlotIdx = (localPoc != null) ? localPoc.InGameSlot : -1;

                // Priority A: Direct lookup via LobbyController.SpawnSlots (Only modify our slot; restore other slots if corrupted)
                var lc = LobbyController.Instance ?? UnityEngine.Object.FindObjectOfType<LobbyController>();
                if (lc != null)
                {
                    if (localSlotIdx < 0 && lc.LocalPlayerController != null)
                        localSlotIdx = lc.LocalPlayerController.InGameSlot;

                    if (lc.SpawnSlots != null)
                    {
                        for (int i = 0; i < lc.SpawnSlots.Count; i++)
                        {
                            var slot = lc.SpawnSlots[i];
                            if (slot == null || slot.NameText == null) continue;

                            if (i == localSlotIdx)
                            {
                                // Local player slot: Apply our rich colored nametag to UI component only
                                slot.NameText.richText = true;
                                slot.NameText.color = Color.white;
                                slot.NameText.text = fullRichName;
                            }
                            else
                            {
                                // Other player slot: If corrupted by our previous name, revert to true NetworkPlayerName
                                if (!string.IsNullOrEmpty(slot.NetworkPlayerName) && slot.NameText.text != null && slot.NameText.text.Contains(baseName))
                                {
                                    slot.NameText.text = slot.NetworkPlayerName;
                                }
                            }
                        }
                    }
                }

                // Priority B: Direct lookup via VelvetLobbyController.SpawnSlots
                var vlc = VelvetLobbyController.Instance ?? UnityEngine.Object.FindObjectOfType<VelvetLobbyController>();
                if (vlc != null)
                {
                    if (localSlotIdx < 0 && vlc.LocalPlayerController != null)
                        localSlotIdx = vlc.LocalPlayerController.InGameSlot;

                    if (vlc.SpawnSlots != null)
                    {
                        for (int i = 0; i < vlc.SpawnSlots.Count; i++)
                        {
                            var slot = vlc.SpawnSlots[i];
                            if (slot == null || slot.NameText == null) continue;

                            if (i == localSlotIdx)
                            {
                                // Local player slot: Apply our rich colored nametag to UI component only
                                slot.NameText.richText = true;
                                slot.NameText.color = Color.white;
                                slot.NameText.text = fullRichName;
                            }
                            else
                            {
                                // Other player slot: If corrupted by our previous name, revert to true NetworkPlayerName
                                if (!string.IsNullOrEmpty(slot.NetworkPlayerName) && slot.NameText.text != null && slot.NameText.text.Contains(baseName))
                                {
                                    slot.NameText.text = slot.NetworkPlayerName;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static float _lastTmpSanitizeTime = 0f;

        private static void SanitizeAndEnableRichText()
        {
            if (Time.time - _lastTmpSanitizeTime < 0.25f) return;
            _lastTmpSanitizeTime = Time.time;

            try
            {
                var all3D = UnityEngine.Object.FindObjectsOfType<TextMeshPro>();
                if (all3D != null)
                {
                    for (int i = 0; i < all3D.Length; i++)
                    {
                        var t = all3D[i];
                        if (t == null) continue;
                        FixTextComponent(t);
                    }
                }
            }
            catch { }

            try
            {
                var allUI = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                if (allUI != null)
                {
                    for (int i = 0; i < allUI.Length; i++)
                    {
                        var t = allUI[i];
                        if (t == null) continue;
                        FixTextComponent(t);
                    }
                }
            }
            catch { }

            try
            {
                var allLegacy = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
                if (allLegacy != null)
                {
                    for (int i = 0; i < allLegacy.Length; i++)
                    {
                        var t = allLegacy[i];
                        if (t == null) continue;
                        if (!t.supportRichText) t.supportRichText = true;
                    }
                }
            }
            catch { }
        }

        private static void FixTextComponent(TMP_Text t)
        {
            if (t == null) return;
            bool modified = false;

            string txt = t.text;
            if (!string.IsNullOrEmpty(txt))
            {
                // 1. If text was HTML escaped by game's chat or network serializer (&lt;color=#FFD...&gt;)
                if (txt.Contains("&lt;") || txt.Contains("&gt;"))
                {
                    txt = txt.Replace("&lt;", "<").Replace("&gt;", ">");
                    t.text = txt;
                    modified = true;
                }

                // 2. Chat input fields / preview prompts: strictly strip any raw markup so tags never leak into chat
                string goName = (t.gameObject != null) ? t.gameObject.name : "";
                if (goName.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    goName.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (txt.Contains("<color=") || txt.Contains("</color>"))
                    {
                        txt = System.Text.RegularExpressions.Regex.Replace(txt, "<.*?>", string.Empty);
                        t.text = txt;
                        modified = true;
                    }
                }
                else
                {
                    // 3. 3D nametags, turn banners, announcer text: ensure richText is active and tags are valid
                    if (!t.richText) { t.richText = true; modified = true; }
                    if (!t.parseCtrlCharacters) { t.parseCtrlCharacters = true; modified = true; }

                    if (txt.Contains("<color=") && !txt.Contains("</color>"))
                    {
                        int start = txt.IndexOf("<color=");
                        int closeBracket = txt.IndexOf('>', start);
                        if (closeBracket == -1)
                        {
                            t.text = txt.Substring(0, start) + txt.Substring(start).Replace("<color=", "");
                            modified = true;
                        }
                        else
                        {
                            t.text = txt + "</color>";
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                try { t.ForceMeshUpdate(true, true); } catch { }
            }
        }

        private void RefreshSessionState()
        {
            try
            {
                _gameType = DetectGameType();
                _mgr = Manager.Instance;

                // Safe session discovery without destroying any Mirror network identities

                _players = DiscoverAllPlayers(_mgr);
                _me = DiscoverLocalPlayer(_mgr, _players);
            }
            catch { }
        }

        private void RunAntiKick()
        {
            try
            {
                if (_me != null)
                {
                    var poc = _me.GetComponent<PlayerObjectController>();
                    if (poc != null)
                    {
                        if (poc.Kicked) poc.Kicked = false;
                        if (poc.NetworkKicked) poc.NetworkKicked = false;
                    }
                }

                var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                if (allPocs != null)
                {
                    for (int i = 0; i < allPocs.Length; i++)
                    {
                        var poc = allPocs[i];
                        if (poc != null && poc.Kicked)
                        {
                            poc.Kicked = false;
                            poc.NetworkKicked = false;
                        }
                    }
                }
            }
            catch { }
        }

        public override void OnGUI()
        {
            if (TacticalMenu.MenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 1. Draw Tactical Menu
            TacticalMenu.Draw(_gameType.ToString(), _mgr, _me, _players);

            // 2. Draw Keybinds HUD if toggled
            if (TacticalMenu.ShowKeys)
            {
                DrawKeybindOverlay();
            }

            // 3. Draw Quick In-Game ESP HUD (Disabled duplicate text overlay, green HUD preserved)
            // if (TacticalMenu.ShowEsp && !TacticalMenu.MenuOpen && _players != null && _players.Length > 0) DrawQuickEspHud();

            // 4. Draw Top-Left Standard Cards & Spin Tactical HUD Overlay
            if (TacticalMenu.ShowTopLeftCards && _players != null && _players.Length > 0)
            {
                if (_gameType == GameType.Spin)
                    DrawTopLeftSpinOverlay();
                else
                    DrawTopLeftCardsOverlay();
            }
        }

        private void DrawTopLeftCardsOverlay()
        {
            if (_players == null || _players.Length == 0) return;
            if (_mgr == null || !_mgr.GameStarted || _gameType == GameType.None) return;

            float rowH = 26f;
            float headerH = 28f;
            float totalH = headerH + (_players.Length * rowH) + 8f;
            float panelW = 620f;

            // Handle HUD Mouse Dragging
            Event e = Event.current;
            if (e != null)
            {
                Rect dragHeaderRect = new Rect(CardsHudPos.x, CardsHudPos.y, panelW, headerH);
                if (e.type == EventType.MouseDown && dragHeaderRect.Contains(e.mousePosition))
                {
                    _isDraggingCardsHud = true;
                    _cardsHudDragOffset = e.mousePosition - CardsHudPos;
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _isDraggingCardsHud = false;
                }
                else if (e.type == EventType.MouseDrag && _isDraggingCardsHud)
                {
                    CardsHudPos = e.mousePosition - _cardsHudDragOffset;
                    CardsHudPos.x = Mathf.Clamp(CardsHudPos.x, 0f, Mathf.Max(0f, Screen.width - panelW));
                    CardsHudPos.y = Mathf.Clamp(CardsHudPos.y, 0f, Mathf.Max(0f, Screen.height - totalH));
                    e.Use();
                }
            }

            Rect hudRect = new Rect(CardsHudPos.x, CardsHudPos.y, panelW, totalH);
            Theme.DrawPanel(hudRect, new Color(0.03f, 0.05f, 0.09f, 0.94f), new Color(0.00f, 0.90f, 0.65f, 0.50f), 1.5f);

            string modeName = _gameType == GameType.ChaosDeck ? "CHAOS DECK HUD" : (_gameType == GameType.Dice ? "DICE HUD" : (_gameType == GameType.Texas ? "TEXAS HOLD'EM HUD" : (_gameType == GameType.Poker ? "POKER HUD" : "CARDS HUD")));

            string tableTargetInfo = "";
            try
            {
                var cdm = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
                var dm = UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>();
                if (_gameType == GameType.ChaosDeck && cdm != null)
                {
                    tableTargetInfo = $" | Table: <color=#FFDD55>{cdm.CardsOnTable}</color> | Target: {UiBuilder.GetCardBadge(cdm.RoundCard)}";
                }
                else if (dm != null && (_gameType == GameType.Deck || _gameType == GameType.Default || _gameType == GameType.MatchMaking))
                {
                    tableTargetInfo = $" | Table: <color=#FFDD55>{dm.CardsOnTable}</color> | Target: {UiBuilder.GetCardBadge(dm.RoundCard)}";
                }
            }
            catch { }

            GUI.Label(new Rect(hudRect.x + 10f, hudRect.y + 4f, hudRect.width - 20f, 20f),
                $"<b><color=#00FFB0>🎴 {modeName}</color>{tableTargetInfo} <color=#55FFAA>[DRAGGABLE]</color></b>");

            float curY = hudRect.y + headerH;

            for (int i = 0; i < _players.Length; i++)
            {
                var p = _players[i];
                if (p == null) continue;

                var ps = p.GetComponent<PlayerStats>();
                if (ps == null) continue;

                bool isMe = p == _me;
                string nameStr = isMe ? TacticalMenu.GetFormattedPlayerName(TacticalMenu.BasePlayerName ?? ps.NetworkPlayerName) : ps.NetworkPlayerName;
                if (string.IsNullOrEmpty(nameStr)) nameStr = $"Player #{ps.Slot}";

                string stateTag = ps.NetworkDead ? "<color=#FF3333>[DEAD]</color> " : (isMe ? "<color=#00FFB0>[YOU]</color> " : "");

                string cardsDisplay = "";
                string lastPlayedStr = "";

                if (_gameType == GameType.Dice)
                {
                    var diceList = DiceController.GetDiceForPlayer(p);
                    if (diceList.Count > 0)
                    {
                        var dBadges = new List<string>();
                        foreach (var d in diceList) dBadges.Add(UiBuilder.GetDiceBadge(d));
                        cardsDisplay = string.Join(" ", dBadges);
                    }
                    else cardsDisplay = "<color=#8899AA>[Hidden Dice]</color>";
                }
                else
                {
                    var cardTypes = CardController.GetCardTypesForPlayer(p);
                    if (cardTypes.Count > 0)
                    {
                        var badges = new List<string>();
                        foreach (var t in cardTypes)
                        {
                            if (_gameType == GameType.Texas)
                                badges.Add(UiBuilder.GetTexasCardBadge(t));
                            else
                                badges.Add(UiBuilder.GetCardBadge(t));
                        }
                        cardsDisplay = string.Join(" ", badges);

                        if (_gameType == GameType.Texas)
                        {
                            var tg = p.GetComponent<TexasGamePlay>();
                            if (tg != null)
                            {
                                var tm = UnityEngine.Object.FindFirstObjectByType<TexasGamePlayManager>();
                                string rank = TexasController.EvaluateTexasBestRank(tg, tm);
                                if (!string.IsNullOrEmpty(rank)) cardsDisplay += $" <color=#00FFB0>[{rank}]</color>";
                            }
                        }
                    }
                    else
                    {
                        cardsDisplay = "<color=#8899AA>[Empty Hand]</color>";
                    }

                    // Dynamic Last Played Cards Tracking
                    var lastPlayedList = PlayerCardTracker.GetLastPlayedCards(ps.Slot);
                    if (lastPlayedList != null && lastPlayedList.Count > 0)
                    {
                        var lastBadges = new List<string>();
                        foreach (var lp in lastPlayedList)
                        {
                            if (_gameType == GameType.Texas)
                                lastBadges.Add(UiBuilder.GetTexasCardBadge(lp));
                            else
                                lastBadges.Add(UiBuilder.GetCardBadge(lp));
                        }
                        lastPlayedStr = $" | <color=#FFCC00>Last Played:</color> {string.Join(" ", lastBadges)}";
                    }
                }

                int ch = CardController.GetCurrentChamber(p);
                int bul = CardController.GetRevolverBullet(p);
                string revStr = "";
                if (bul > 0)
                {
                    int nextChamber = (ch % 6) + 1;
                    bool isFatalNext = (nextChamber == bul);
                    bool isChambered = (ch == bul);
                    string statCol = isFatalNext ? "<color=#FF3333>[FATAL NEXT]</color>" : (isChambered ? "<color=#FF6666>[CHAMBERED]</color>" : "<color=#55FFAA>[SAFE]</color>");
                    revStr = $"  {statCol} <color=#FFDD55>Cyl:{ch}/6 B:{bul}</color>";
                }

                string rowTxt = $"{stateTag}<b>{nameStr}</b>: {cardsDisplay}{lastPlayedStr}{revStr}";
                GUI.Label(new Rect(hudRect.x + 10f, curY, hudRect.width - 15f, rowH), rowTxt);
                curY += rowH;
            }
        }

        private void DrawTopLeftSpinOverlay()
        {
            if (_players == null || _players.Length == 0) return;
            if (_mgr == null || !_mgr.GameStarted) return;

            float rowH = 26f;
            float headerH = 28f;
            float totalH = headerH + (_players.Length * rowH) + 8f;
            float panelW = 680f;

            // Handle HUD Mouse Dragging
            Event e = Event.current;
            if (e != null)
            {
                Rect dragHeaderRect = new Rect(CardsHudPos.x, CardsHudPos.y, panelW, headerH);
                if (e.type == EventType.MouseDown && dragHeaderRect.Contains(e.mousePosition))
                {
                    _isDraggingCardsHud = true;
                    _cardsHudDragOffset = e.mousePosition - CardsHudPos;
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _isDraggingCardsHud = false;
                }
                else if (e.type == EventType.MouseDrag && _isDraggingCardsHud)
                {
                    CardsHudPos = e.mousePosition - _cardsHudDragOffset;
                    CardsHudPos.x = Mathf.Clamp(CardsHudPos.x, 0f, Mathf.Max(0f, Screen.width - panelW));
                    CardsHudPos.y = Mathf.Clamp(CardsHudPos.y, 0f, Mathf.Max(0f, Screen.height - totalH));
                    e.Use();
                }
            }

            Rect hudRect = new Rect(CardsHudPos.x, CardsHudPos.y, panelW, totalH);
            Theme.DrawPanel(hudRect, new Color(0.03f, 0.05f, 0.09f, 0.94f), new Color(0.00f, 0.90f, 0.65f, 0.50f), 1.5f);

            var spinMgr = UnityEngine.Object.FindFirstObjectByType<LiarsSpinGameplayManager>();
            string targetCritBadge = "[None]";
            string deadSpinBadge = "[ROUND SAFE]";
            if (spinMgr != null)
            {
                targetCritBadge = TacticalMenu.GetSpinSymbolBadge((int)spinMgr.currentRoundCriterion);
                deadSpinBadge = spinMgr.isDeadSpin ? "<color=#FF3333>[DEAD SPIN ACTIVE]</color>" : "<color=#55FFAA>[ROUND SAFE]</color>";
            }

            GUI.Label(new Rect(hudRect.x + 10f, hudRect.y + 4f, hudRect.width - 20f, 20f),
                $"<b><color=#00FFB0>🎰 LIAR'S SPIN TACTICAL HUD</color></b> | Target: {targetCritBadge} | Status: {deadSpinBadge}");

            float curY = hudRect.y + headerH;

            for (int i = 0; i < _players.Length; i++)
            {
                var p = _players[i];
                if (p == null) continue;

                var ps = p.GetComponent<PlayerStats>();
                if (ps == null) continue;

                var pSpin = p.GetComponent<SpinGamePlay>();
                bool isMe = (p == _me);
                string nameStr = ps.NetworkPlayerName;
                if (string.IsNullOrEmpty(nameStr)) nameStr = isMe ? "You" : $"Player #{ps.Slot}";

                string stateTag = isMe ? "<color=#00FFB0>[YOU]</color> " : "";
                if (ps.NetworkDead) stateTag += "<color=#888888>[DEAD]</color> ";
                else if (pSpin != null && pSpin.isDeadSpinMoment) stateTag += "<color=#FF3333>[DEAD SPIN]</color> ";
                else stateTag += "<color=#55FFAA>[ALIVE]</color> ";

                string reelStr = "";
                string matchStr = "";
                string betStr = "";
                if (pSpin != null)
                {
                    reelStr = $"{TacticalMenu.GetSpinSymbolGlyph(pSpin._localSlot1)} {TacticalMenu.GetSpinSymbolGlyph(pSpin._localSlot2)} {TacticalMenu.GetSpinSymbolGlyph(pSpin._localSlot3)} {TacticalMenu.GetSpinSymbolGlyph(pSpin._localSlot4)}";
                    int matches = pSpin.CorrectSlotCount();
                    matchStr = $" | Matches: <color=#00FFB0><b>{matches}/4</b></color>";
                    betStr = $" | Bet: <b>{pSpin.BetCount}</b>";
                    if (pSpin.IsJackpot()) matchStr += " <color=#FFD700>[JACKPOT]</color>";
                    if (pSpin.IsAllRed()) matchStr += " <color=#FF3344>[ALL RED]</color>";
                }

                string rowTxt = $"{stateTag}<b>{nameStr}</b> (Slot {ps.Slot}): [ {reelStr} ]{matchStr}{betStr}";
                GUI.Label(new Rect(hudRect.x + 10f, curY, hudRect.width - 15f, rowH), rowTxt);
                curY += rowH;
            }
        }

        private void DrawKeybindOverlay()
        {
            float w = 275f;
            float h = 195f;
            Rect r = new Rect(20f, Screen.height - h - 30f, w, h);

            GUI.backgroundColor = new Color(0.04f, 0.05f, 0.10f, 0.94f);
            GUI.Box(r, "<b><color=#00FFB0>[KEYBIND TACTICAL GUIDE]</color></b>");

            string txt = "<color=#FFFFFF>[INSERT / F1]</color>  Toggle Menu\n" +
                         "<color=#FFFFFF>[P / F2]</color>       Dump State to Desktop\n" +
                         "<color=#FFFFFF>[L / F3]</color>       Toggle Keybind Overlay\n" +
                         "<color=#55FFAA>[F4]</color>          Quick Safe Revolver (Self)\n" +
                         "<color=#FF5555>[F5]</color>          Quick Deadly Revolver (Self)\n" +
                         "<color=#55FFAA>[F6]</color>          God Mode (0 Bullets/Cover)\n" +
                         "<color=#FFDD55>[F7]</color>          Kill All Others [H]\n" +
                         "<color=#FFDD55>[F8]</color>          Force End Game Win [H]\n" +
                         "<color=#FF3344>[F9]</color>          Call Liar Anytime (Global)\n" +
                         "<color=#55FFAA>[F10]</color>         Force Host Migration [C]\n" +
                         "<color=#888888>Inkwell's Liar's Bar Menu v4.4</color>";

            GUI.Label(new Rect(r.x + 10f, r.y + 24f, r.width - 20f, r.height - 28f), txt);
        }

        private void DrawQuickEspHud()
        {
            float startY = 150f;
            for (int i = 0; i < _players.Length; i++)
            {
                var p = _players[i];
                if (p == null) continue;

                var ps = p.GetComponent<PlayerStats>();
                if (ps == null) continue;

                bool isMe = p == _me;
                string extra = "";

                if (_gameType == GameType.Dice)
                {
                    var diceList = DiceController.GetDiceForPlayer(p);
                    if (diceList.Count > 0)
                        extra = $" [Dice: {string.Join("-", diceList)}]";
                }
                else if (_gameType == GameType.Poker)
                {
                    var pg = p.GetComponent<PokerGamePlay>();
                    if (pg != null)
                    {
                        var cards = PokerController.GetCardsForPoker(pg);
                        var cardNames = new List<string>();
                        foreach (var c in cards) cardNames.Add(CardController.GetCardName(c));
                        extra = $" [Cards: {string.Join(",", cardNames)}] [B:{pg.fullbullets.Count}]";
                    }
                }
                else if (_gameType == GameType.Texas)
                {
                    var tg = p.GetComponent<TexasGamePlay>();
                    if (tg != null)
                    {
                        var tCards = TexasController.GetTexasCardsForPlayer(tg);
                        if (tCards.Count > 0)
                        {
                            var cardNames = new List<string>();
                            foreach (var c in tCards) cardNames.Add(TexasController.GetCardName(c));
                            var tm = UnityEngine.Object.FindFirstObjectByType<TexasGamePlayManager>();
                            string rank = TexasController.EvaluateTexasBestRank(tg, tm);
                            extra = $" [Cards: {string.Join(",", cardNames)}] [{rank}] [B:{tg.NetworkBullets}]";
                        }
                        else
                        {
                            extra = $" [Cards: Hidden (Server Fog)] [B:{tg.NetworkBullets}]";
                        }
                    }
                }
                else
                {
                    var cardTypes = CardController.GetCardTypesForPlayer(p);
                    if (cardTypes.Count > 0)
                    {
                        var cardNames = new List<string>();
                        foreach (var t in cardTypes) cardNames.Add(CardController.GetCardName(t));
                        extra = $" [Cards: {string.Join(",", cardNames)}]";
                    }
                    int b = CardController.GetRevolverBullet(p);
                    int ch = CardController.GetCurrentChamber(p);
                    if (b > 0 || ch > 0) extra += $" (Ch:{ch}/6 B:{b})";
                }

                string label = $"{(isMe ? "[YOU] " : "")}{ps.NetworkPlayerName}{(ps.NetworkDead ? " [DEAD]" : "")}{extra}";
                GUI.color = ps.NetworkDead ? Color.gray : (isMe ? Color.cyan : Color.white);
                GUI.Label(new Rect(20f, startY, 600f, 22f), label);
                startY += 20f;
            }
            GUI.color = Color.white;
        }

        // ================= GAME MODE DETECTION =================

        private static GameType DetectGameType()
        {
            try
            {
                var cc = UnityEngine.Object.FindFirstObjectByType<CharController>();
                if (cc != null)
                {
                    var t = cc.GetIl2CppType();
                    if (t != null)
                    {
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<BlorfGamePlayMatchMaking>()) return GameType.MatchMaking;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<BlorfGamePlay>()) return GameType.Default;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<ChaosGamePlay>()) return GameType.Chaos;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<ChaosDeckGameplay>()) return GameType.ChaosDeck;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<DeckGameplay>()) return GameType.Deck;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<DiceGamePlay>()) return GameType.Dice;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<PokerGamePlay>()) return GameType.Poker;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<TexasGamePlay>()) return GameType.Texas;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<SpinGamePlay>()) return GameType.Spin;
                        if (t == Il2CppInterop.Runtime.Il2CppType.Of<RouletteGamePlay>()) return GameType.Roulette;
                    }
                }
            }
            catch { }

            try
            {
                var m = Manager.Instance;
                if (m != null)
                {
                    if (m.useChaosDeckRules || m.ChaosDeckGame != null || UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>() != null) return GameType.ChaosDeck;
                    if (m.TexasGame != null) return GameType.Texas;
                    if (m.PokerGame != null) return GameType.Poker;
                    if (m.DeckGamePlayManager != null) return GameType.Deck;
                    if (m.ChaosGame != null) return GameType.Chaos;
                    if (m.SpinGame != null) return GameType.Spin;
                    if (UnityEngine.Object.FindFirstObjectByType<DiceGamePlayManager>() != null) return GameType.Dice;

                    if (m.Players != null && m.Players.Count > 0)
                    {
                        var p0 = m.Players[0];
                        if (p0 != null && p0.gameObject != null)
                        {
                            var go = p0.gameObject;
                            if (go.GetComponent<ChaosDeckGameplay>() != null) return GameType.ChaosDeck;
                            if (go.GetComponent<BlorfGamePlayMatchMaking>() != null) return GameType.MatchMaking;
                            if (go.GetComponent<BlorfGamePlay>() != null) return GameType.Default;
                            if (go.GetComponent<ChaosGamePlay>() != null) return m.ChaosDeckGame != null ? GameType.ChaosDeck : GameType.Chaos;
                            if (go.GetComponent<DeckGameplay>() != null) return GameType.Deck;
                            if (go.GetComponent<DiceGamePlay>() != null) return GameType.Dice;
                            if (go.GetComponent<PokerGamePlay>() != null) return GameType.Poker;
                            if (go.GetComponent<TexasGamePlay>() != null) return GameType.Texas;
                            if (go.GetComponent<SpinGamePlay>() != null) return GameType.Spin;
                            if (go.GetComponent<RouletteGamePlay>() != null) return GameType.Roulette;
                        }
                    }
                }
            }
            catch { }

            return GameType.None;
        }

        private static GameObject[] DiscoverAllPlayers(Manager m)
        {
            var list = new List<GameObject>();
            var seenIds = new HashSet<int>();

            // Vector 1: Manager.Players list (if non-empty)
            if (m != null && m.Players != null && m.Players.Count > 0)
            {
                for (int i = 0; i < m.Players.Count; i++)
                {
                    var p = m.Players[i];
                    if (p != null && p.gameObject != null)
                    {
                        int id = p.gameObject.GetInstanceID();
                        if (!seenIds.Contains(id))
                        {
                            seenIds.Add(id);
                            list.Add(p.gameObject);
                        }
                    }
                }
                if (list.Count > 0) return list.ToArray();
            }

            // Vector 2: Scan all PlayerStats components in scene (Present in 100% of matches & modes)
            try
            {
                var allStats = UnityEngine.Object.FindObjectsOfType<PlayerStats>();
                if (allStats != null && allStats.Length > 0)
                {
                    var sortedList = new List<PlayerStats>();
                    for (int i = 0; i < allStats.Length; i++)
                    {
                        if (allStats[i] != null && allStats[i].gameObject != null) sortedList.Add(allStats[i]);
                    }
                    sortedList.Sort((a, b) => a.Slot.CompareTo(b.Slot));

                    for (int i = 0; i < sortedList.Count; i++)
                    {
                        var s = sortedList[i];
                        int id = s.gameObject.GetInstanceID();
                        if (!seenIds.Contains(id))
                        {
                            seenIds.Add(id);
                            list.Add(s.gameObject);
                        }
                    }
                    if (list.Count > 0) return list.ToArray();
                }
            }
            catch { }

            // Vector 3: Scan all CharController components in scene
            try
            {
                var allCC = UnityEngine.Object.FindObjectsOfType<CharController>();
                if (allCC != null && allCC.Length > 0)
                {
                    for (int i = 0; i < allCC.Length; i++)
                    {
                        var cc = allCC[i];
                        if (cc != null && cc.gameObject != null)
                        {
                            int id = cc.gameObject.GetInstanceID();
                            if (!seenIds.Contains(id))
                            {
                                seenIds.Add(id);
                                list.Add(cc.gameObject);
                            }
                        }
                    }
                    if (list.Count > 0) return list.ToArray();
                }
            }
            catch { }

            // Vector 4: Scan all PlayerObjectController components in scene
            try
            {
                var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                if (allPocs != null && allPocs.Length > 0)
                {
                    for (int i = 0; i < allPocs.Length; i++)
                    {
                        var poc = allPocs[i];
                        if (poc != null && poc.gameObject != null)
                        {
                            int id = poc.gameObject.GetInstanceID();
                            if (!seenIds.Contains(id))
                            {
                                seenIds.Add(id);
                                list.Add(poc.gameObject);
                            }
                        }
                    }
                    if (list.Count > 0) return list.ToArray();
                }
            }
            catch { }

            return list.ToArray();
        }

        private static GameObject DiscoverLocalPlayer(Manager m, GameObject[] players)
        {
            // Vector 1: Manager.GetLocalPlayer()
            if (m != null)
            {
                try
                {
                    var lp = m.GetLocalPlayer();
                    if (lp != null && lp.gameObject != null) return lp.gameObject;
                }
                catch { }
            }

            // Vector 2: Inspect discovered players for isLocalPlayer
            if (players != null && players.Length > 0)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p == null) continue;

                    var ps = p.GetComponent<PlayerStats>();
                    if (ps != null && ps.isLocalPlayer) return p;

                    var cc = p.GetComponent<CharController>();
                    if (cc != null && cc.isLocalPlayer) return p;

                    var poc = p.GetComponent<PlayerObjectController>();
                    if (poc != null && poc.isLocalPlayer) return p;
                }
            }

            // Vector 3: Global scene search for isLocalPlayer
            try
            {
                var allStats = UnityEngine.Object.FindObjectsOfType<PlayerStats>();
                if (allStats != null)
                {
                    for (int i = 0; i < allStats.Length; i++)
                    {
                        var s = allStats[i];
                        if (s != null && s.isLocalPlayer && s.gameObject != null) return s.gameObject;
                    }
                }

                var allCC = UnityEngine.Object.FindObjectsOfType<CharController>();
                if (allCC != null)
                {
                    for (int i = 0; i < allCC.Length; i++)
                    {
                        var cc = allCC[i];
                        if (cc != null && cc.isLocalPlayer && cc.gameObject != null) return cc.gameObject;
                    }
                }

                var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                if (allPocs != null)
                {
                    for (int i = 0; i < allPocs.Length; i++)
                    {
                        var poc = allPocs[i];
                        if (poc != null && poc.isLocalPlayer && poc.gameObject != null) return poc.gameObject;
                    }
                }
            }
            catch { }

            return null;
        }

        private static GameObject[] GetPlayers(Manager m)
        {
            return DiscoverAllPlayers(m);
        }
    }
}
