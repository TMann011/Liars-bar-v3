using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using LiarsBarMod.Features.Cards;
using LiarsBarMod.Features.Dice;
using LiarsBarMod.Features.Poker;
using LiarsBarMod.Features.Dev;
using LiarsBarMod.Features.Matchmaking;
using Il2CppSteamworks;

namespace LiarsBarMod.UI
{
    public static class TacticalMenu
    {
        public static bool MenuOpen = true;
        public static bool ShowEsp = true;
        public static bool ShowKeys = false;
        public static bool AntiKick = true;
        public static bool BypassAuth = false;
        public static bool GodSaveToggle = false;
        public static bool ShowTopLeftCards = false;
        public static bool BypassCardLimit = false;
        public static bool AlwaysMyTurn = false;
        public static bool DeathSpinImmunity = true;
        public static float _lastRgbBroadcastTime = 0f;

        public static bool RgbTagEnabled = false;
        public static bool RgbNameEnabled = false;
        public static float RgbSpeed = 1.0f;
        public static string SelectedTag = "None";
        public static string CustomTagInput = "";
        public static string BasePlayerName = "";
        public static string NameColorHex = "#FFFFFF";
        public static bool CustomColorEnabled = false;

        public static Vector2 MenuPos = new Vector2(30f, 30f);
        public static Vector2 MenuSize = new Vector2(760f, 600f);

        public static bool IsHostSession(GameObject me)
        {
            if (me == null) return false;
            try
            {
                var dg = me.GetComponent<DiceGamePlay>(); if (dg != null && dg.isServer) return true;
                var cd = me.GetComponent<ChaosDeckGameplay>(); if (cd != null && cd.isServer) return true;
                var dk = me.GetComponent<DeckGameplay>(); if (dk != null && dk.isServer) return true;
                var cg = me.GetComponent<ChaosGamePlay>(); if (cg != null && cg.isServer) return true;
                var bg = me.GetComponent<BlorfGamePlay>(); if (bg != null && bg.isServer) return true;
                var bm = me.GetComponent<BlorfGamePlayMatchMaking>(); if (bm != null && bm.isServer) return true;
                var pg = me.GetComponent<PokerGamePlay>(); if (pg != null && pg.isServer) return true;
                var tg = me.GetComponent<TexasGamePlay>(); if (tg != null && tg.isServer) return true;
                var sg = me.GetComponent<SpinGamePlay>(); if (sg != null && sg.isServer) return true;
                var rg = me.GetComponent<RouletteGamePlay>(); if (rg != null && rg.isServer) return true;
            }
            catch { }
            return false;
        }

        private static int _currentTab = 0;
        public static readonly string[] Tabs = {
            "Info", "Cards", "Dice", "Poker", "Spin", "Roul", "Players", "Join",
            "Move", "Stats", "Skins", "Dev", "Self", "Global", "Keys", "Settings"
        };

        private static bool _isDragging = false;
        private static Vector2 _dragOffset;
        private static bool _isResizing = false;

        private static Vector2 _scrollPos = Vector2.zero;
        private static readonly float[] _tabHeights = new float[16];

        // Colors
        public static Color MenuBg = Theme.WindowBg;
        public static Color ContentBg = Theme.ContentBg;
        public static Color MenuText = Color.white;

        private static bool _h360Active = false;
        private static float _mxs, _mys, _mnxs, _mnys;
        private static string _dumpStatus = "";

        private static float _fps = 60f;
        private static float _fpsTimer = 0f;

        public static void Draw(string gameTypeStr, Manager mgr, GameObject me, GameObject[] players)
        {
            if (!MenuOpen) return;

            // Compute FPS
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer > 0.5f)
            {
                _fps = 1.0f / Mathf.Max(0.0001f, Time.smoothDeltaTime);
                _fpsTimer = 0f;
            }

            HandleInputEvents();

            float w = Mathf.Max(MenuSize.x, 500f);
            float h = Mathf.Max(MenuSize.y, 400f);

            // 1. Draw Main Window Glass Frame
            Rect winRect = new Rect(MenuPos.x, MenuPos.y, w, h);
            Theme.DrawPanel(winRect, MenuBg, Theme.BorderCol, 1.5f);

            // 2. Title Bar Header
            Rect headerRect = new Rect(MenuPos.x, MenuPos.y, w, 32f);
            Theme.DrawPanel(headerRect, Theme.HeaderBg, new Color(0.00f, 1.00f, 0.70f, 0.50f), 1f);

            // Title Text & Status Indicator
            bool inMatch = (gameTypeStr != "None" && mgr != null && mgr.GameStarted);
            bool isHost = IsHostSession(me);
            string authBadge = isHost ? "<color=#FFDD55>[HOST]</color>" : "<color=#55FFAA>[CLIENT]</color>";
            string statusPill = inMatch ? $"<color=#00FFB0>● LIVE [{gameTypeStr.ToUpper()}]</color> {authBadge}" : $"<color=#8899AA>○ LOBBY</color> {authBadge}";
            GUI.Label(new Rect(MenuPos.x + 12f, MenuPos.y + 6f, 480f, 22f),
                $"<b><color=#00FFB0>Inkwell's</color> <color=#FFFFFF>Liar's Bar Menu</color></b>  {statusPill}");

            // Header Action Buttons
            DrawHeaderButton(new Rect(MenuPos.x + w - 175f, MenuPos.y + 4f, 52f, 24f), "<color=#55FFAA>DUMP</color>", () => {
                _dumpStatus = StateDumper.DumpStateToDesktop(gameTypeStr, mgr, me, players);
            });

            DrawHeaderButton(new Rect(MenuPos.x + w - 118f, MenuPos.y + 4f, 52f, 24f), "<color=#FFDD55>MIGRATE</color>", () => {
                ForceHostMigration(players, me);
            });

            DrawHeaderButton(new Rect(MenuPos.x + w - 60f, MenuPos.y + 4f, 48f, 24f), "<color=#FF4444>[ X ]</color>", () => {
                MenuOpen = false;
            });

            // 3. Tab Navigation Grid (2 rows of 8 tabs)
            int tabsPerRow = 8;
            float tabH = 26f;
            float tabW = (w - 20f) / tabsPerRow;

            for (int i = 0; i < Tabs.Length; i++)
            {
                int row = i / tabsPerRow;
                int col = i % tabsPerRow;
                float tx = MenuPos.x + 10f + (col * tabW);
                float ty = MenuPos.y + 36f + (row * (tabH + 3f));

                Rect tabRect = new Rect(tx, ty, tabW - 3f, tabH);
                bool isActive = (i == _currentTab);
                bool hover = tabRect.Contains(Event.current.mousePosition);

                Color bg = isActive ? Theme.TabActiveBg : (hover ? Theme.ButtonHoverBg : Theme.TabInactiveBg);
                Color border = isActive ? Theme.CyanAccent : Theme.BorderMuted;
                Theme.DrawPanel(tabRect, bg, border, isActive ? 1.5f : 1f);

                string label = isActive ? $"<b><color=#00FFB0>[ {Tabs[i]} ]</color></b>" : $"<color=#8FA0B8>{Tabs[i]}</color>";
                if (GUI.Button(tabRect, label, GUI.skin.label))
                {
                    _currentTab = i;
                    _scrollPos = Vector2.zero;
                }
            }

            // 4. Content Panel Area
            float contentTop = MenuPos.y + 98f;
            float contentH = h - 126f;
            Rect contentBox = new Rect(MenuPos.x + 10f, contentTop, w - 20f, contentH);

            Theme.DrawPanel(contentBox, ContentBg, Theme.BorderMuted, 1f);

            // Dynamic ScrollView
            float scrollContentH = Mathf.Max(contentH, _tabHeights[_currentTab]);
            Rect viewRect = new Rect(0, 0, contentBox.width - 18f, scrollContentH);

            _scrollPos = GUI.BeginScrollView(contentBox, _scrollPos, viewRect);

            UiBuilder ui = new UiBuilder(new Rect(8f, 8f, contentBox.width - 26f, 6000f));

            try
            {
                switch (_currentTab)
                {
                    case 0: DrawInfoTab(ui, gameTypeStr, mgr, me, players); break;
                    case 1: DrawCardsTab(ui, gameTypeStr, me, players); break;
                    case 2: DrawDiceTab(ui, gameTypeStr, me, players); break;
                    case 3: DrawPokerTab(ui, gameTypeStr, me, players); break;
                    case 4: DrawSpinTab(ui, gameTypeStr, me, players); break;
                    case 5: DrawRoulTab(ui, gameTypeStr, me, players); break;
                    case 6: DrawPlayersTab(ui, gameTypeStr, me, players); break;
                    case 7: DrawJoinTab(ui); break;
                    case 8: DrawMoveTab(ui, me); break;
                    case 9: DrawStatsTab(ui); break;
                    case 10: DrawSkinsTab(ui, me, players); break;
                    case 11: DrawDevTab(ui, gameTypeStr, mgr, me, players); break;
                    case 12: DrawSelfTab(ui, me); break;
                    case 13: DrawGlobalTab(ui); break;
                    case 14: DrawKeysTab(ui); break;
                    case 15: DrawSettingsTab(ui); break;
                }
            }
            catch (Exception ex)
            {
                ui.Label($"<color=#FF3333>Tab execution error: {ex.Message}</color>");
            }

            _tabHeights[_currentTab] = ui.TotalHeight + 24f;
            GUI.EndScrollView();

            // 5. Footer Telemetry Bar
            Rect footerRect = new Rect(MenuPos.x + 10f, MenuPos.y + h - 24f, w - 20f, 20f);
            string footerText = $"<color=#00FFB0>●</color> <color=#8899AA>[INSERT]</color> Menu  |  <color=#8899AA>[L]</color> Keys  |  <color=#8899AA>[P]</color> Dump  |  Mode: <color=#FFFFFF>{gameTypeStr}</color>  |  FPS: <color=#00FFB0>{_fps:F0}</color>  |  <color=#55FFAA>Inkwell's Liar's Bar Menu v4.4</color>";
            if (!string.IsNullOrEmpty(_dumpStatus)) footerText = $"<color=#00FFB0>Dumped -> {_dumpStatus}</color>";
            GUI.Label(footerRect, footerText);

            // Resize Grip
            Rect gripRect = new Rect(MenuPos.x + w - 18f, MenuPos.y + h - 18f, 16f, 16f);
            Theme.DrawPanel(gripRect, new Color(0.12f, 0.16f, 0.28f, 0.90f), Theme.BorderMuted);
            GUI.Label(new Rect(gripRect.x + 2f, gripRect.y - 1f, 14f, 14f), "<color=#00FFB0>//</color>");
        }

        private static void DrawHeaderButton(Rect rect, string text, Action onClick)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Color bg = hover ? Theme.ButtonHoverBg : new Color(0.09f, 0.12f, 0.20f, 0.85f);
            Color border = hover ? Theme.CyanAccent : Theme.BorderMuted;
            Theme.DrawPanel(rect, bg, border);

            if (GUI.Button(rect, text, GUI.skin.label))
            {
                onClick?.Invoke();
            }
        }

        private static void HandleInputEvents()
        {
            Event e = Event.current;
            if (e == null) return;

            float w = Mathf.Max(MenuSize.x, 500f);
            float h = Mathf.Max(MenuSize.y, 400f);

            Rect headerRect = new Rect(MenuPos.x, MenuPos.y, w - 180f, 32f);
            Rect resizeRect = new Rect(MenuPos.x + w - 22f, MenuPos.y + h - 22f, 22f, 22f);

            if (e.type == EventType.MouseDown)
            {
                if (headerRect.Contains(e.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = e.mousePosition - MenuPos;
                    e.Use();
                }
                else if (resizeRect.Contains(e.mousePosition))
                {
                    _isResizing = true;
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                _isDragging = false;
                _isResizing = false;
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (_isDragging)
                {
                    MenuPos = e.mousePosition - _dragOffset;
                    e.Use();
                }
                else if (_isResizing)
                {
                    MenuSize = new Vector2(
                        Mathf.Max(500f, e.mousePosition.x - MenuPos.x),
                        Mathf.Max(400f, e.mousePosition.y - MenuPos.y)
                    );
                    e.Use();
                }
            }
        }

        // ================= TAB IMPLEMENTATIONS =================

        private static void DrawInfoTab(UiBuilder ui, string gt, Manager mgr, GameObject me, GameObject[] players)
        {
            ui.Header("MATCH & SESSION TELEMETRY");
            ui.Label("<color=#55FFAA>[C]</color> Client Supported   |   <color=#FFDD55>[H]</color> Host Required   |   <color=#8899AA>[L]</color> Local Visual");
            ui.Label($"<b>Mode:</b> <color=#00FFB0>{gt}</color>  |  <b>Players Connected:</b> <color=#55FFAA>{(players != null ? players.Length : 0)}</color>");
            if (mgr != null)
            {
                ui.Label($"<b>Session Active:</b> {mgr.GameStarted}  |  <b>Active Turn Slot:</b> #{mgr.NetworkActivePlayerSlot}");
            }

            var deckMgr = UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>();
            var chaosDeckMgr = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
            var blorfMgr = UnityEngine.Object.FindFirstObjectByType<BlorfGamePlayManager>();
            var diceMgr = UnityEngine.Object.FindFirstObjectByType<DiceGamePlayManager>();

            if (deckMgr != null || chaosDeckMgr != null || blorfMgr != null)
            {
                int cot = deckMgr != null ? deckMgr.CardsOnTable : (chaosDeckMgr != null ? chaosDeckMgr.CardsOnTable : blorfMgr.CardsOnTable);
                int rc = deckMgr != null ? deckMgr.RoundCard : (chaosDeckMgr != null ? chaosDeckMgr.RoundCard : blorfMgr.RoundCard);
                ui.Label($"<b>Table Cards:</b> <color=#FFD700>{cot}</color>  |  <b>Round Card:</b> {UiBuilder.GetCardBadge(rc)}");
            }

            if (diceMgr != null)
            {
                ui.Label($"<b>Dice Mode:</b> {diceMgr.NetworkDiceMode}  |  <b>Last Bid:</b> {diceMgr.NetworkLastCount}x {UiBuilder.GetDiceBadge(diceMgr.NetworkLastDice)} by {diceMgr.NetworkLastDiceName}");
            }

            ui.Space(4);
            ui.Header("LIVE ACTIVE PLAYERS ROSTER");

            if (players == null || players.Length == 0)
            {
                ui.Label("<color=#FF8800>No active players detected. Join or host a match to begin.</color>");
                return;
            }

            int aliveDiceTotals = 0;
            int[] diceFaceCounts = new int[7];

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null) continue;

                var ps = p.GetComponent<PlayerStats>();
                if (ps == null) continue;

                bool isMe = p == me;
                string deadTag = ps.NetworkDead ? " <color=#FF3333>[DEAD]</color>" : " <color=#33FF77>[ALIVE]</color>";
                string youTag = isMe ? " <color=#00FFB0>[YOU]</color>" : "";

                ui.SubHeader($"Slot #{ps.Slot} - {ps.NetworkPlayerName}{youTag}{deadTag}");

                // Cards & Hand ESP (Deck / Poker / Texas)
                if (gt == "Poker")
                {
                    var pg = p.GetComponent<PokerGamePlay>();
                    if (pg != null)
                    {
                        var pCards = PokerController.GetCardsForPoker(pg);
                        var cardBadges = new List<string>();
                        foreach (var cType in pCards) cardBadges.Add(UiBuilder.GetCardBadge(cType));
                        ui.Label($"     Poker Cards ({cardBadges.Count}): {(cardBadges.Count > 0 ? string.Join(" ", cardBadges) : "<color=#8899AA>[No Cards]</color>")}  Bullets: {pg.fullbullets?.Count ?? 0}");
                    }
                }
                else if (gt == "Texas")
                {
                    var tg = p.GetComponent<TexasGamePlay>();
                    if (tg != null)
                    {
                        var tCards = TexasController.GetTexasCardsForPlayer(tg);
                        if (tCards.Count > 0)
                        {
                            var tBadges = new List<string>();
                            foreach (var cId in tCards) tBadges.Add(UiBuilder.GetTexasCardBadge(cId));
                            var tm = UnityEngine.Object.FindFirstObjectByType<TexasGamePlayManager>();
                            string rank = TexasController.EvaluateTexasBestRank(tg, tm);
                            ui.Label($"     Texas Hand: {string.Join(" ", tBadges)}  Rank: <color=#00FFB0>{rank}</color>  Bullets: {tg.NetworkBullets}");
                        }
                        else
                        {
                            ui.Label($"     <color=#667788>Texas Hand: [Hidden in Server RAM until Showdown / Flop]</color>  Bullets: {tg.NetworkBullets}");
                        }
                    }
                }
                else
                {
                    var cardTypes = CardController.GetCardTypesForPlayer(p);
                    if (cardTypes.Count > 0)
                    {
                        var cardBadges = new List<string>();
                        foreach (var t in cardTypes) cardBadges.Add(UiBuilder.GetCardBadge(t));
                        ui.Label($"     Cards ({cardBadges.Count}): {string.Join(" ", cardBadges)}");
                    }
                }

                // Dice ESP
                var diceList = DiceController.GetDiceForPlayer(p);
                if (diceList.Count > 0)
                {
                    var diceBadges = new List<string>();
                    for (int d = 0; d < diceList.Count; d++)
                    {
                        int val = diceList[d];
                        diceBadges.Add(UiBuilder.GetDiceBadge(val));
                        if (!ps.NetworkDead && val >= 1 && val <= 6)
                        {
                            diceFaceCounts[val]++;
                            aliveDiceTotals++;
                        }
                    }
                    ui.Label($"     Dice ({diceList.Count}): {string.Join(" ", diceBadges)}");
                }
                else if (gt == "Dice")
                {
                    ui.Label("     <color=#667788>Dice: [Hidden in Server Memory until Showdown / Lift Cup]</color>");
                }

                // Revolver ESP & Radar
                int revBul = CardController.GetRevolverBullet(p);
                int revCur = CardController.GetCurrentChamber(p);
                if (revBul > 0 || revCur > 0)
                {
                    bool isDeadly = (revBul == revCur);
                    string status = isDeadly ? "<color=#FF3333>NEXT PULL DEATH</color>" : "<color=#55FFAA>SAFE</color>";
                    string radar = UiBuilder.GetRevolverRadar(revCur, revBul);
                    ui.Label($"     Revolver: {radar}  Chamber: {revCur}/6 | Bullet: {revBul} | {status}");
                }
            }

            if (aliveDiceTotals > 0)
            {
                ui.Space(4);
                ui.Header("ALIVE DICE TOTALS (SUMMARY)");
                ui.Label($"{UiBuilder.GetDiceBadge(1)}: {diceFaceCounts[1]}   {UiBuilder.GetDiceBadge(2)}: {diceFaceCounts[2]}   {UiBuilder.GetDiceBadge(3)}: {diceFaceCounts[3]}   {UiBuilder.GetDiceBadge(4)}: {diceFaceCounts[4]}   {UiBuilder.GetDiceBadge(5)}: {diceFaceCounts[5]}   {UiBuilder.GetDiceBadge(6)}: {diceFaceCounts[6]}  |  Total: <b>{aliveDiceTotals}</b>");
            }
        }

        private static void DrawCardsTab(UiBuilder ui, string gt, GameObject me, GameObject[] players)
        {
            ui.Header("TURN & CARD LIMIT CONTROLS <color=#55FFAA>[C]</color>");
            ui.Toggle(ref BypassCardLimit, "Bypass Card Limit (Select & play up to all 5 cards per turn)");
            ui.Toggle(ref AlwaysMyTurn, "Always My Turn (Play cards without waiting for your turn)");
            ui.BeginRow(26f);
            if (ui.RowButton("<color=#00FFB0>Throw Entire Hand Now</color>", 200f) && me != null)
            {
                CardController.ThrowAllHandCards(me);
            }
            if (ui.RowButton("<color=#FF3344>⚡ CALL LIAR ANYTIME [F9]</color>", 220f) && me != null)
            {
                CardController.CallLiarAnytime(me);
            }
            ui.EndRow();

            ui.Space(6);
            ui.Header("INDIVIDUAL HAND CARDS (LOCAL PLAYER) <color=#55FFAA>[C]</color>");

            if (me == null)
            {
                ui.Label("<color=#FF8800>Join a match to edit cards.</color>");
                return;
            }

            var cards = CardController.GetCardsForPlayer(me);
            if (cards != null && cards.Count > 0)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var cObj = cards[i];
                    if (cObj == null || !cObj.activeInHierarchy) continue;

                    int curType = CardController.GetCardType(cObj);
                    string cBadge = UiBuilder.GetCardBadge(curType);

                    ui.BeginRow(24f);
                    ui.RowLabel($"Card #{i + 1} {cBadge}:", 115f);
                    if (ui.RowButton("<color=#FFD700>K</color>", 36f)) CardController.SetOneCardSafe(me, cObj, 1);
                    if (ui.RowButton("<color=#FF66CC>Q</color>", 36f)) CardController.SetOneCardSafe(me, cObj, 2);
                    if (ui.RowButton("<color=#00E5FF>A</color>", 36f)) CardController.SetOneCardSafe(me, cObj, 3);
                    if (ui.RowButton("<color=#FFB300>J</color>", 36f)) CardController.SetOneCardSafe(me, cObj, 4);
                    if (ui.RowButton("<color=#FF3344>Devil</color>", 55f)) CardController.SetOneCardSafe(me, cObj, -1);
                    ui.EndRow();
                }
            }
            else
            {
                ui.Label("<color=#888888>No active cards in hand.</color>");
            }

            ui.Space(6);
            ui.Header("BULK HAND MODIFICATIONS <color=#55FFAA>[C]</color>");
            ui.BeginRow(26f);
            if (ui.RowButton("All -> <color=#FFD700>King</color>", 140f)) CardController.SetAllCardsForPlayer(me, 1);
            if (ui.RowButton("All -> <color=#FF66CC>Queen</color>", 140f)) CardController.SetAllCardsForPlayer(me, 2);
            if (ui.RowButton("All -> <color=#00E5FF>Ace</color>", 140f)) CardController.SetAllCardsForPlayer(me, 3);
            if (ui.RowButton("All -> <color=#FFB300>Jack</color>", 140f)) CardController.SetAllCardsForPlayer(me, 4);
            ui.EndRow();

            ui.BeginRow(26f);
            if (ui.RowButton("All -> <color=#FF3344>Devil (-1)</color>", 160f)) CardController.SetAllCardsForPlayer(me, -1);
            if (ui.RowButton("<color=#FFDD55>[H]</color> Clear Table (InstaWin)", 190f))
            {
                var dm = UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>();
                if (dm != null) dm.CardsOnTable = 0;
                var cdm = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
                if (cdm != null) cdm.CardsOnTable = 0;
                var bm = UnityEngine.Object.FindFirstObjectByType<BlorfGamePlayManager>();
                if (bm != null) bm.CardsOnTable = 0;
            }
            ui.EndRow();

            ui.Space(6);
            ui.Header("ROUND CARD RIGGING <color=#FFDD55>[H]</color>");
            var deckMgr = UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>();
            var chaosDeckMgr = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
            var blorfMgr = UnityEngine.Object.FindFirstObjectByType<BlorfGamePlayManager>();

            int rc = deckMgr != null ? deckMgr.RoundCard : (chaosDeckMgr != null ? chaosDeckMgr.RoundCard : (blorfMgr != null ? blorfMgr.RoundCard : 0));
            ui.Label($"Current Table Round Card: {UiBuilder.GetCardBadge(rc)}");

            ui.BeginRow(24f);
            if (ui.RowButton("RC: <color=#FFD700>King</color>", 85f)) SetRoundCardGlobal(1);
            if (ui.RowButton("RC: <color=#FF66CC>Queen</color>", 85f)) SetRoundCardGlobal(2);
            if (ui.RowButton("RC: <color=#00E5FF>Ace</color>", 85f)) SetRoundCardGlobal(3);
            if (ui.RowButton("RC: <color=#FFB300>Jack</color>", 85f)) SetRoundCardGlobal(4);
            if (ui.RowButton("RC: <color=#FF3344>Devil</color>", 85f)) SetRoundCardGlobal(-1);
            ui.EndRow();

            ui.Space(6);
            ui.Header("REVOLVER CHAMBER & BULLET RIGGING");
            int myChamber = CardController.GetCurrentChamber(me);
            int myBullet = CardController.GetRevolverBullet(me);
            int nextChamber = (myChamber % 6) + 1;
            bool isFatalNext = (nextChamber == myBullet);
            bool isChambered = (myChamber == myBullet);
            string myStatus = isFatalNext ? "<color=#FF3333>FATAL NEXT</color>" : (isChambered ? "<color=#FF6666>CHAMBERED</color>" : "<color=#55FFAA>SAFE</color>");
            ui.Label($"Local Cylinder: {UiBuilder.GetRevolverRadar(myChamber, myBullet)}  Chamber: <b>{myChamber}/6</b> | Bullet: <b>{myBullet}</b>  [{myStatus}]");

            ui.BeginRow(26f);
            if (ui.RowButton("<color=#55FFAA>[C]</color> Set Safe (Offset Bullet)", 210f)) CardController.SetRevolverSafe(me);
            if (ui.RowButton("<color=#FF4444>[C]</color> Set Deadly (Bullet In Chamber)", 225f)) CardController.SetRevolverDeath(me);
            if (ui.RowButton("Bullet +1", 85f)) CardController.SetRevolver(me, (myBullet % 6) + 1);
            if (ui.RowButton("Bullet -1", 85f)) CardController.SetRevolver(me, myBullet <= 1 ? 6 : myBullet - 1);
            ui.EndRow();

            ui.BeginRow(24f);
            ui.RowLabel("Chamber Pos:", 95f);
            if (ui.RowButton("Ch +1", 50f)) CardController.SetCurrentChamber(me, (myChamber + 1) % 6);
            if (ui.RowButton("Ch -1", 50f)) CardController.SetCurrentChamber(me, (myChamber + 5) % 6);
            ui.RowLabel("Bullet Pos:", 75f);
            for (int cIdx = 0; cIdx < 6; cIdx++)
            {
                int targetBullet = cIdx + 1;
                string col = (targetBullet == myBullet) ? "#FF4444" : (targetBullet == myChamber ? "#00FFB0" : "#8899AA");
                if (ui.RowButton($"<color={col}>[{targetBullet}]</color>", 32f))
                {
                    CardController.SetRevolver(me, targetBullet);
                }
            }
            ui.EndRow();

            ui.BeginRow(26f);
            if (ui.RowButton("<color=#FFDD55>[H]</color> <color=#FF5555>Rig Opponents to Death (Next Pull)</color>", 240f))
            {
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p != me) CardController.SetRevolverDeath(p);
                    }
                }
            }
            if (ui.RowButton("<color=#55FFAA>Rig Opponents to Safe</color>", 180f))
            {
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p != me) CardController.SetRevolverSafe(p);
                    }
                }
            }
            ui.EndRow();

            // Chaos Deck Specific Tactical Aim & Devil Actions
            if (me != null)
            {
                var cd = me.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    ui.Space(6);
                    ui.Header("CHAOS DECK & DEVIL TACTICAL ACTIONS <color=#FF3344>[CHAOS]</color>");
                    ui.BeginRow(26f);
                    if (ui.RowButton("<color=#FF3344>Devil Scream SFX</color>", 160f))
                    {
                        try { cd.playcmdsesdevil(); } catch { }
                    }
                    if (ui.RowButton("<color=#00FFB0>Reveal All Reset Cards</color>", 180f))
                    {
                        var cdm = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
                        if (cdm != null) try { cdm.OpenAllResetCards(); } catch { }
                    }
                    ui.EndRow();

                    if (players != null && players.Length > 0)
                    {
                        ui.Label("<color=#FFCC00>Direct Shot Override (Hit Target Player with Revolver):</color>");
                        ui.BeginRow(24f);
                        for (int pIdx = 0; pIdx < players.Length; pIdx++)
                        {
                            var targetPlayer = players[pIdx];
                            if (targetPlayer == null || targetPlayer == me) continue;
                            var ps = targetPlayer.GetComponent<PlayerStats>();
                            int slot = ps != null ? ps.Slot : pIdx;
                            string pName = ps != null ? ps.NetworkPlayerName : $"Player #{slot}";
                            if (ui.RowButton($"<color=#FF4444>Shoot #{slot} ({pName})</color>", 150f))
                            {
                                try
                                {
                                    int aimDir = cd.GetAimDirectionForSlot(slot);
                                    cd.HitTargetCmd(aimDir);
                                    cd.RequestFire(aimDir);
                                    cd.CmdResolveFire(aimDir);
                                }
                                catch { }
                            }
                        }
                        ui.EndRow();
                    }
                }
            }
        }

        private static void SetRoundCardGlobal(int val)
        {
            var cdm = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
            if (cdm != null)
            {
                cdm.RoundCard = val;
                try { cdm.ChangeRoundCardMesh(val); } catch { }
                try { cdm.UserCode_ChangeRoundCardMesh__Int32(val); } catch { }
            }

            var dm = UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>();
            if (dm != null)
            {
                dm.RoundCard = val;
                try { dm.ChangeRoundCardMesh(val); } catch { }
                try { dm.UserCode_ChangeRoundCardMesh__Int32(val); } catch { }
            }

            var bm = UnityEngine.Object.FindFirstObjectByType<BlorfGamePlayManager>();
            if (bm != null) bm.RoundCard = val;
        }

        private static void DrawDiceTab(UiBuilder ui, string gt, GameObject me, GameObject[] players)
        {
            ui.Header("DICE GAME OVERRIDES & ACTIONS");
            ui.Label("<color=#FFDD55>[!] Authority Notice:</color> <color=#8899AA>In Liar's Bar, opponent dice are held in server memory until showdown. Host [H] has real-time values; Client [C] dice edits are local visual [L] unless hosting.</color>");

            if (me == null)
            {
                ui.Label("<color=#FF8800>Join a Dice match to use controls.</color>");
                return;
            }

            var dg = me.GetComponent<DiceGamePlay>();
            if (dg == null)
            {
                ui.Label("<color=#888888>Not in Dice game mode.</color>");
                return;
            }

            ui.Label($"Drink Count: <b><color=#00FFB0>{dg.NetworkdrinkCount}</color></b> | Looking: <b>{dg.NetworkLooking}</b>");
            ui.BeginRow(26f);
            if (ui.RowButton("<color=#FFDD55>[H]</color> Drink +1", 100f)) dg.NetworkdrinkCount++;
            if (ui.RowButton("<color=#FFDD55>[H]</color> Drink = 0", 100f)) dg.NetworkdrinkCount = 0;
            if (ui.RowButton("<color=#55FFAA>[C]</color> Call Liar", 100f)) { try { dg.CallLier(); } catch { } }
            if (ui.RowButton("<color=#55FFAA>[C]</color> Spot On", 100f)) { try { dg.CallSpotOn(); } catch { } }
            if (ui.RowButton("<color=#55FFAA>[C]</color> Shake Cup", 105f)) { try { dg.Shake(); } catch { } }
            ui.EndRow();

            ui.Space(6);
            ui.Header("INDIVIDUAL DIE OVERRIDES (CUP & NETWORK) <color=#8899AA>[L]</color>/<color=#FFDD55>[H]</color>");

            var myDice = DiceController.GetDiceForPlayer(me);
            int diceCount = myDice.Count > 0 ? myDice.Count : 5;

            for (int i = 0; i < diceCount; i++)
            {
                int curVal = (i < myDice.Count) ? myDice[i] : 1;
                ui.BeginRow(24f);
                ui.RowLabel($"Die #{i + 1} {UiBuilder.GetDiceBadge(curVal)}:", 110f);
                for (int v = 1; v <= 6; v++)
                {
                    int targetVal = v;
                    int dieIndex = i;
                    if (ui.RowButton(targetVal.ToString(), 34f))
                    {
                        DiceController.SetSingleDice(dg, dieIndex, targetVal);
                    }
                }
                ui.EndRow();
            }

            ui.Space(6);
            ui.Header("BULK ALL DICE PRESETS <color=#8899AA>[L]</color>/<color=#FFDD55>[H]</color>");
            ui.BeginRow(26f);
            for (int v = 1; v <= 6; v++)
            {
                int targetVal = v;
                if (ui.RowButton($"All -> {targetVal}", 95f))
                {
                    DiceController.SetAllDice(dg, targetVal);
                }
            }
            ui.EndRow();
        }

        private static void DrawPokerTab(UiBuilder ui, string gt, GameObject me, GameObject[] players)
        {
            ui.Header("POKER & TEXAS HOLD'EM SUITE");

            if (me == null)
            {
                ui.Label("<color=#FF8800>Join a Poker / Texas match to activate.</color>");
                return;
            }

            var pg = me.GetComponent<PokerGamePlay>();
            var tg = me.GetComponent<TexasGamePlay>();

            if (pg != null)
            {
                ui.SubHeader("Russian Poker Hand & Bullet Controls");
                var bullets = PokerController.GetBullets(pg);
                int bulletCount = bullets.Count;
                string bulletMap = bulletCount > 0 ? string.Join(",", bullets) : "none";
                string pRadar = PokerController.GetPokerCylinderRadar(pg);
                var overrideState = PokerController.GetShotOverride(pg);
                string pOverrideBadge = overrideState switch
                {
                    PokerController.ShotOutcomeOverride.MustDie => "<color=#FF3333><b>[ARMED: DIE ON SHOT]</b></color>",
                    PokerController.ShotOutcomeOverride.MustSurvive => "<color=#55FFAA><b>[ARMED: SURVIVE SHOT]</b></color>",
                    _ => "<color=#8899AA>[DEFAULT RNG]</color>"
                };
                ui.Label($"Gun: {pRadar}  Chambers: <b><color=#FFD700>{bulletCount}</color></b> | Current: <b>{pg.CurrentBullet}</b> | Live: <b>{bulletMap}</b> | CardVal: <b>{pg.NetworkCardValue}</b>  {pOverrideBadge}");

                ui.BeginRow(26f);
                if (ui.RowButton("<color=#FF4444><b>[★ Die on Shot]</b></color>", 125f)) PokerController.SetShotOverride(pg, PokerController.ShotOutcomeOverride.MustDie);
                if (ui.RowButton("<color=#55FFAA><b>[★ Survive Shot]</b></color>", 130f)) PokerController.SetShotOverride(pg, PokerController.ShotOutcomeOverride.MustSurvive);
                if (ui.RowButton("Reset RNG", 75f)) PokerController.ClearShotOverride(pg);
                if (ui.RowButton("<color=#FF2222>Kill Now</color>", 75f)) PokerController.ExecuteForceKill(pg);
                if (ui.RowButton("<color=#55FFAA>God Mode</color>", 80f)) PokerController.EnableGodMode(pg);
                if (ui.RowButton("<color=#FFD700>Insta Win</color>", 80f)) PokerController.WinRound(pg);
                ui.EndRow();

                // Bullet manipulation
                ui.Space(2);
                ui.BeginRow(24f);
                ui.RowLabel("Current Bullet:", 95f);
                if (ui.RowButton("-", 24f)) { if (pg.CurrentBullet > 0) pg.CurrentBullet--; }
                if (ui.RowButton("+", 24f)) pg.CurrentBullet++;
                ui.Space(12f);
                ui.RowLabel("Set Death @:", 80f);
                for (int b = 0; b < bulletCount && b < 8; b++)
                {
                    int bb = b;
                    string bLabel = bullets[b] >= 0 ? $"<color=#FF3333>[{b}]</color>" : $"{b}";
                    if (ui.RowButton(bLabel, 28f)) PokerController.SetDeathBullet(pg, bb);
                }
                ui.EndRow();

                ui.Space(4);
                ui.SubHeader("Quick Hand Presets <color=#55FFAA>[C]</color>");
                ui.BeginRow(24f);
                if (ui.RowButton("<color=#00FFB0>* Auto Best Cards (AA)</color>", 160f)) PokerController.ApplyBestPokerHand(pg);
                if (ui.RowButton("Pair of Kings (KK)", 140f)) { PokerController.SetPokerCard(pg, 0, 1); PokerController.SetPokerCard(pg, 1, 1); }
                if (ui.RowButton("Pair of Aces (AA)", 140f)) { PokerController.SetPokerCard(pg, 0, 3); PokerController.SetPokerCard(pg, 1, 3); }
                if (ui.RowButton("King & Ace (KA)", 130f)) { PokerController.SetPokerCard(pg, 0, 1); PokerController.SetPokerCard(pg, 1, 3); }
                if (ui.RowButton("Royal Pair (QK)", 130f)) { PokerController.SetPokerCard(pg, 0, 2); PokerController.SetPokerCard(pg, 1, 1); }
                ui.EndRow();

                ui.Space(4);
                ui.Label("Individual Hand Cards:");
                var pCards = PokerController.GetCardsForPoker(pg);
                int count = pCards.Count > 0 ? pCards.Count : 2;

                for (int slot = 0; slot < count; slot++)
                {
                    int cType = slot < pCards.Count ? pCards[slot] : 1;
                    ui.BeginRow(24f);
                    ui.RowLabel($"Card #{slot + 1} {UiBuilder.GetCardBadge(cType)}:", 110f);
                    int s = slot;
                    if (ui.RowButton("<color=#00FF88>+</color>", 24f)) PokerController.CyclePokerCard(pg, s, 1);
                    if (ui.RowButton("<color=#FF6666>-</color>", 24f)) PokerController.CyclePokerCard(pg, s, -1);
                    ui.Space(4f);
                    if (ui.RowButton("<color=#FFD700>K</color>", 30f)) PokerController.SetPokerCard(pg, s, 1);
                    if (ui.RowButton("<color=#FF66CC>Q</color>", 30f)) PokerController.SetPokerCard(pg, s, 2);
                    if (ui.RowButton("<color=#00E5FF>A</color>", 30f)) PokerController.SetPokerCard(pg, s, 3);
                    if (ui.RowButton("<color=#FFB300>J</color>", 30f)) PokerController.SetPokerCard(pg, s, 4);
                    if (ui.RowButton("<color=#FF3333>D</color>", 30f)) PokerController.SetPokerCard(pg, s, -1);
                    ui.EndRow();
                }

                ui.Space(4);
                ui.BeginRow(24f);
                if (ui.RowButton("<color=#55FFAA>[C]</color> Sync Cards CMD", 130f)) { try { pg.SetCardsCmd(); } catch { } }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Swap Card CMD", 130f)) { try { pg.SwapcardCMD(0); } catch { } }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Call Liar", 90f)) { try { pg.LiarCmd(); } catch { } }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Versus", 85f)) { try { pg.VersusCmd(); } catch { } }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Challenge", 95f)) { try { pg.ChallangeCmd(); } catch { } }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Pass", 80f)) { try { pg.PassCmd(); } catch { } }
                if (ui.RowButton("<color=#FFDD55>[H]</color> Force Pass Others", 150f))
                {
                    if (players != null)
                    {
                        foreach (var pl in players)
                        {
                            if (pl == null || pl == me) continue;
                            ForcePassPlayer(pl, me);
                        }
                    }
                }
                ui.EndRow();
            }
            else if (tg != null)
            {
                ui.SubHeader("Texas Hold'em Hand & Bullets");
                ui.Label($"Bullets: <b>{tg.NetworkBullets}</b> | Folded: <b>{tg.NetworkFolded}</b> | GodSave: <b><color={(GodSaveToggle ? "#55FF55>ON" : "#FF5555>OFF")}</color></b>");

                ui.BeginRow(26f);
                if (ui.RowButton("<color=#55FFAA>[C]</color> God Mode (0 Bullets)", 170f)) { tg.NetworkBullets = 0; tg.Bullets = 0; }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Bullets = 99", 130f)) { tg.NetworkBullets = 99; tg.Bullets = 99; }
                if (ui.RowButton("<color=#55FFAA>[C]</color> +1", 45f)) { tg.NetworkBullets++; tg.Bullets++; }
                if (ui.RowButton("<color=#55FFAA>[C]</color> -1", 45f)) { if (tg.NetworkBullets > 0) { tg.NetworkBullets--; tg.Bullets--; } }
                if (ui.RowButton("<color=#FFDD55>[H]</color> Force Fold Others", 150f))
                {
                    if (players != null)
                    {
                        foreach (var pl in players)
                        {
                            if (pl == null || pl == me) continue;
                            ForceFoldPlayer(pl, me);
                        }
                    }
                }
                ui.EndRow();

                ui.BeginRow(24f);
                if (ui.RowButton("<color=#55FFAA>[C]</color> All In", 90f)) { try { tg.ImInCmd(); } catch { } }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Fold", 90f)) { try { tg.ImOutCMD(); } catch { } }
                if (ui.RowButton("<color=#FFDD55>[H]</color> Next Round", 110f)) { try { tg.NextRound(); } catch { } }
                // God Save Toggle
                string gsColor = GodSaveToggle ? "#55FF55" : "#AAAAAA";
                string gsLabel = GodSaveToggle ? "God Save [ON]" : "God Save [OFF]";
                if (ui.RowButton($"<color={gsColor}>{gsLabel}</color>", 120f)) GodSaveToggle = !GodSaveToggle;
                if (ui.RowButton("<color=#FFDD55>[H]</color> Reset Bullets", 115f)) { TexasController.ResetBullets(tg); }
                ui.EndRow();

                // Apply god save continuously when toggled
                if (GodSaveToggle)
                {
                    try { tg._revolverGodSaveLocal = true; } catch { }
                    try { tg._revolverWillDieLocal = false; } catch { }
                }

                ui.BeginRow(24f);
                string tRadarSelf = TexasController.GetTexasRadar(tg);
                string selfStatus = tg._revolverWillDieLocal ? "<color=#FF3333>FATAL</color>" : (tg._revolverGodSaveLocal ? "<color=#55FFAA>SURVIVE</color>" : $"<color=#FFDD55>{tg.NetworkBullets}/6</color>");
                ui.RowLabel($"   Revolver: {tRadarSelf} Bullets: <b>{tg.NetworkBullets}/6</b> [{selfStatus}]", 250f);
                if (ui.RowButton("<color=#FF4444>[C] Die on Shot</color>", 120f)) TexasController.SetPlayerShotDeadly(tg);
                if (ui.RowButton("<color=#55FFAA>[C] Survive Shot</color>", 130f)) TexasController.SetPlayerShotSafe(tg);
                if (ui.RowButton("Bul +1", 50f)) TexasController.AddBullets(tg, 1);
                if (ui.RowButton("Bul -1", 50f)) TexasController.AddBullets(tg, -1);
                ui.EndRow();

                ui.Space(4);
                // Texas Table Cards Readout
                var tm = UnityEngine.Object.FindFirstObjectByType<TexasGamePlayManager>();
                var tableBadges = new List<string>();
                if (tm != null && tm.OpenTableCards != null && tm.OpenTableCards.Count > 0)
                {
                    for (int i = 0; i < tm.OpenTableCards.Count; i++) tableBadges.Add(UiBuilder.GetTexasCardBadge(tm.OpenTableCards[i]));
                }
                string tableStr = tableBadges.Count > 0 ? string.Join(" ", tableBadges) : "<color=#8899AA>[Waiting for Deal / Flop]</color>";
                ui.Label($"<b>Table Community Cards:</b> {tableStr}");

                // Current Best Hand analysis
                string currentBestRank = TexasController.EvaluateTexasBestRank(tg, tm);
                ui.Label($"<b>Your Calculated Rank:</b> <color=#00FFB0>{currentBestRank}</color>");

                ui.BeginRow(26f);
                if (ui.RowButton("<color=#00FFB0>★ Auto Set Best Cards (Based on Pool)</color>", 270f))
                {
                    TexasController.ApplyTexasBestPossibleHand(tg, tm);
                }
                if (ui.RowButton("<color=#55FFAA>[C]</color> Refresh Mesh", 115f))
                {
                    try
                    {
                        var cards = TexasController.GetTexasCardsForPlayer(tg);
                        if (cards.Count >= 2) TexasController.SetTexasHand(tg, cards[0], cards[1]);
                    }
                    catch { }
                }
                if (ui.RowButton("<color=#FFDD55>[H]</color> Sync Cards CMD", 130f)) { try { tg.SetCardsCmd(); } catch { } }
                ui.EndRow();

                ui.Space(4);
                ui.SubHeader("Texas Quick Hand Presets <color=#55FFAA>[C]</color>");
                ui.BeginRow(24f);
                if (ui.RowButton("Pocket Aces (AA)", 130f)) TexasController.SetTexasHand(tg, 52, 51);
                if (ui.RowButton("Pocket Kings (KK)", 130f)) TexasController.SetTexasHand(tg, 48, 47);
                if (ui.RowButton("Pocket Queens (QQ)", 135f)) TexasController.SetTexasHand(tg, 44, 43);
                if (ui.RowButton("Pocket Jacks (JJ)", 130f)) TexasController.SetTexasHand(tg, 40, 39);
                if (ui.RowButton("Pocket 10s (TT)", 125f)) TexasController.SetTexasHand(tg, 36, 35);
                ui.EndRow();

                ui.BeginRow(24f);
                if (ui.RowButton("AK Suited ♠", 110f)) TexasController.SetTexasHand(tg, 52, 48);
                if (ui.RowButton("KQ Suited ♠", 110f)) TexasController.SetTexasHand(tg, 48, 44);
                if (ui.RowButton("QJ Suited ♠", 110f)) TexasController.SetTexasHand(tg, 44, 40);
                if (ui.RowButton("A♥ K♦ (Offsuit)", 125f)) TexasController.SetTexasHand(tg, 51, 46);
                if (ui.RowButton("<color=#FF6666>7-2 Offsuit (Bluff)</color>", 145f)) TexasController.SetTexasHand(tg, 24, 2);
                ui.EndRow();

                ui.Space(4);
                ui.Label("Individual Hand Cards (52-Card Standard Deck):");
                var myCards = TexasController.GetTexasCardsForPlayer(tg);
                int card0 = myCards.Count > 0 ? myCards[0] : 52;
                int card1 = myCards.Count > 1 ? myCards[1] : 51;

                // Card 1 Controls
                int r0 = TexasController.GetCardValue(card0); if (r0 < 2 || r0 > 14) r0 = 14;
                int s0 = TexasController.GetCardSuit(card0); if (s0 < 0 || s0 > 3) s0 = 3;
                ui.BeginRow(24f);
                ui.RowLabel($"Card 1 {UiBuilder.GetTexasCardBadge(card0)}:", 115f);
                if (ui.RowButton("-", 22f)) { int nr = r0 > 2 ? r0 - 1 : 14; TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(nr, s0)); }
                if (ui.RowButton("+", 22f)) { int nr = r0 < 14 ? r0 + 1 : 2; TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(nr, s0)); }
                ui.Space(4f);
                if (ui.RowButton("2", 24f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(2, s0));
                if (ui.RowButton("7", 24f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(7, s0));
                if (ui.RowButton("8", 24f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(8, s0));
                if (ui.RowButton("9", 24f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(9, s0));
                if (ui.RowButton("10", 28f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(10, s0));
                if (ui.RowButton("J", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(11, s0));
                if (ui.RowButton("Q", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(12, s0));
                if (ui.RowButton("K", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(13, s0));
                if (ui.RowButton("A", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(14, s0));
                ui.Space(6f);
                if (ui.RowButton("♠", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(r0, 3));
                if (ui.RowButton("<color=#FF5555>♥</color>", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(r0, 2));
                if (ui.RowButton("<color=#44AAFF>♦</color>", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(r0, 1));
                if (ui.RowButton("<color=#55FF55>♣</color>", 26f)) TexasController.ApplyTexasCardChange(tg, 0, TexasController.GetCardId(r0, 0));
                ui.EndRow();

                // Card 2 Controls
                int r1 = TexasController.GetCardValue(card1); if (r1 < 2 || r1 > 14) r1 = 14;
                int s1 = TexasController.GetCardSuit(card1); if (s1 < 0 || s1 > 3) s1 = 3;
                ui.BeginRow(24f);
                ui.RowLabel($"Card 2 {UiBuilder.GetTexasCardBadge(card1)}:", 115f);
                if (ui.RowButton("-", 22f)) { int nr = r1 > 2 ? r1 - 1 : 14; TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(nr, s1)); }
                if (ui.RowButton("+", 22f)) { int nr = r1 < 14 ? r1 + 1 : 2; TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(nr, s1)); }
                ui.Space(4f);
                if (ui.RowButton("2", 24f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(2, s1));
                if (ui.RowButton("7", 24f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(7, s1));
                if (ui.RowButton("8", 24f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(8, s1));
                if (ui.RowButton("9", 24f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(9, s1));
                if (ui.RowButton("10", 28f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(10, s1));
                if (ui.RowButton("J", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(11, s1));
                if (ui.RowButton("Q", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(12, s1));
                if (ui.RowButton("K", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(13, s1));
                if (ui.RowButton("A", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(14, s1));
                ui.Space(6f);
                if (ui.RowButton("♠", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(r1, 3));
                if (ui.RowButton("<color=#FF5555>♥</color>", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(r1, 2));
                if (ui.RowButton("<color=#44AAFF>♦</color>", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(r1, 1));
                if (ui.RowButton("<color=#55FF55>♣</color>", 26f)) TexasController.ApplyTexasCardChange(tg, 1, TexasController.GetCardId(r1, 0));
                ui.EndRow();
            }
            else
            {
                ui.Label("<color=#888888>Not in Poker or Texas Hold'em game mode.</color>");
            }
        }

        public static string GetSpinSymbolBadge(int criterion)
        {
            return (criterion % 4) switch
            {
                0 => "<color=#55FFAA>[ ♠ Spades ]</color>",
                1 => "<color=#FF5555>[ ♥ Hearts ]</color>",
                2 => "<color=#44AAFF>[ ♦ Diamonds ]</color>",
                3 => "<color=#FFAA00>[ ♣ Clubs ]</color>",
                _ => $"[ #{criterion} ]"
            };
        }

        public static string GetSpinSymbolGlyph(int criterion)
        {
            return (criterion % 4) switch
            {
                0 => "<color=#55FFAA>♠</color>",
                1 => "<color=#FF5555>♥</color>",
                2 => "<color=#44AAFF>♦</color>",
                3 => "<color=#FFAA00>♣</color>",
                _ => $"#{criterion}"
            };
        }

        public static void SetReelValue(SpinGamePlay s, int reelIndex, int criterionVal)
        {
            if (s == null) return;
            try
            {
                var crit = (LiarsSpinGameplayManager.TableCriterion)(criterionVal % 4);
                switch (reelIndex)
                {
                    case 1: s._localSlot1 = (int)crit; if (s.slotMachine != null) s.slotMachine._slot1 = crit; break;
                    case 2: s._localSlot2 = (int)crit; if (s.slotMachine != null) s.slotMachine._slot2 = crit; break;
                    case 3: s._localSlot3 = (int)crit; if (s.slotMachine != null) s.slotMachine._slot3 = crit; break;
                    case 4: s._localSlot4 = (int)crit; if (s.slotMachine != null) s.slotMachine._slot4 = crit; break;
                }
                if (s.slotMachine != null)
                {
                    s.slotMachine.UpdateSlots(s.slotMachine._slot1, s.slotMachine._slot2, s.slotMachine._slot3, s.slotMachine._slot4);
                    s.slotMachine.Spin();
                }
                s.RefreshUiSlotMachine();
            }
            catch { }
        }

        public static void SetAllReels(SpinGamePlay s, int criterionVal)
        {
            if (s == null) return;
            try
            {
                var crit = (LiarsSpinGameplayManager.TableCriterion)(criterionVal % 4);
                s._localSlot1 = (int)crit;
                s._localSlot2 = (int)crit;
                s._localSlot3 = (int)crit;
                s._localSlot4 = (int)crit;
                if (s.slotMachine != null)
                {
                    s.slotMachine._slot1 = crit;
                    s.slotMachine._slot2 = crit;
                    s.slotMachine._slot3 = crit;
                    s.slotMachine._slot4 = crit;
                    s.slotMachine.UpdateSlots(crit, crit, crit, crit);
                    s.slotMachine.Spin();
                }
                s.RefreshUiSlotMachine();
            }
            catch { }
        }

        private static void DrawSpinTab(UiBuilder ui, string gt, GameObject me, GameObject[] players)
        {
            ui.Header("LIAR'S SPIN - TACTICAL CONTROL CENTER <color=#55FFAA>[ALL SYSTEMS ACTIVE]</color>");

            var spinMgr = UnityEngine.Object.FindFirstObjectByType<LiarsSpinGameplayManager>();
            var s = me != null ? me.GetComponent<SpinGamePlay>() : null;

            if (spinMgr == null && s == null)
            {
                ui.Label("<color=#FF8800>Join a Liar's Spin match to activate real-time telemetry and controls.</color>");
                return;
            }

            // SECTION 1: LIVE GAMEPLAY & ROUND TELEMETRY
            ui.SubHeader("1. LIVE ROUND TELEMETRY & DEATH SPIN OUTCOME");
            if (spinMgr != null)
            {
                string critBadge = GetSpinSymbolBadge((int)spinMgr.currentRoundCriterion);
                string deadSpinBadge = spinMgr.isDeadSpin ? "<color=#FF3333>[DEAD SPIN ACTIVE - LETHAL]</color>" : "<color=#55FFAA>[ROUND SAFE - SURVIVE]</color>";

                string turnOwnerName = "Unknown";
                try
                {
                    var turnOwner = spinMgr.GetTurnOwner();
                    if (turnOwner != null) turnOwnerName = turnOwner.NetworkPlayerName;
                }
                catch { }

                string lastBetPlayerName = "None";
                try
                {
                    if (spinMgr.LastBetPlayer != null)
                    {
                        var ps = spinMgr.LastBetPlayer.GetComponent<PlayerStats>();
                        if (ps != null) lastBetPlayerName = ps.NetworkPlayerName;
                    }
                }
                catch { }

                ui.Label($"Target Criterion: <b>{critBadge}</b>  |  Death Spin Status: <b>{deadSpinBadge}</b>");
                ui.Label($"Current Bid: <b><color=#FFD700>{spinMgr.LastShowsCount}x {spinMgr.LastShowsName}</color></b> by <b>{lastBetPlayerName}</b>  |  Turn: <b><color=#00FFB0>{turnOwnerName}</color></b>");

                ui.BeginRow(26f);
                if (ui.RowButton("<color=#55FFAA>[C] Force Round Safe (Nobody Dies)</color>", 260f))
                {
                    spinMgr.isDeadSpin = false;
                    try { spinMgr.NetworkisDeadSpin = false; } catch { }
                }
                if (ui.RowButton("<color=#FF3333>[H] Force Round Lethal (Death Spin Kills)</color>", 280f))
                {
                    spinMgr.isDeadSpin = true;
                    try { spinMgr.NetworkisDeadSpin = true; } catch { }
                }
                ui.EndRow();
            }
            else
            {
                ui.Label("<color=#8899AA>Liar's Spin Gameplay Manager initializing...</color>");
            }

            ui.Space(6);

            // SECTION 2: ALL PLAYERS' SECRET REELS ESP RADAR & CONTROLS
            ui.SubHeader("2. ALL PLAYERS' SECRET REELS ESP RADAR & CONTROLS");
            if (players != null && players.Length > 0)
            {
                for (int pi = 0; pi < players.Length; pi++)
                {
                    var p = players[pi];
                    if (p == null) continue;
                    var ps = p.GetComponent<PlayerStats>();
                    var pSpin = p.GetComponent<SpinGamePlay>();
                    if (pSpin == null) continue;

                    bool isLocal = (p == me);
                    string pName = ps != null ? ps.NetworkPlayerName : p.name;
                    string whoTag = isLocal ? "<color=#00FFB0>[YOU]</color> " : "";

                    string locStr = $"[ {GetSpinSymbolGlyph(pSpin._localSlot1)} {GetSpinSymbolGlyph(pSpin._localSlot2)} {GetSpinSymbolGlyph(pSpin._localSlot3)} {GetSpinSymbolGlyph(pSpin._localSlot4)} ]";
                    int matches = pSpin.CorrectSlotCount();
                    bool isJackpot = pSpin.IsJackpot();
                    bool isRed = pSpin.IsAllRed();

                    string flags = "";
                    if (isJackpot) flags += " <color=#FFD700>[JACKPOT!]</color>";
                    if (isRed) flags += " <color=#FF3344>[ALL RED]</color>";
                    if (pSpin.isDeadSpinMoment) flags += " <color=#FF3333>[DEAD SPIN MOMENT]</color>";

                    ui.BeginRow(24f);
                    ui.RowLabel($"{whoTag}<b>{pName}</b> (Slot {ps.Slot}): {locStr}  Bet: <b>{pSpin.BetCount}</b>  Matches: <b>{matches}/4</b>{flags}", 380f);
                    if (!isLocal)
                    {
                        if (ui.RowButton("<color=#FF3333>[H] Kill</color>", 65f))
                        {
                            try { pSpin.KillCmd(); pSpin.DieAnim(); } catch { }
                        }
                        if (ui.RowButton("<color=#FF5555>[H] Lethal</color>", 75f))
                        {
                            try
                            {
                                if (spinMgr != null) { spinMgr.isDeadSpin = true; spinMgr.NetworkisDeadSpin = true; }
                                pSpin.isDeadSpinMoment = true;
                                pSpin.RPC_DeadSpin();
                            }
                            catch { }
                        }
                        if (ui.RowButton("<color=#55FFAA>[C] Safe</color>", 65f))
                        {
                            try
                            {
                                if (spinMgr != null) { spinMgr.isDeadSpin = false; spinMgr.NetworkisDeadSpin = false; }
                                pSpin.isDeadSpinMoment = false;
                                pSpin.RpcPlaySaveAnim();
                            }
                            catch { }
                        }
                        if (ui.RowButton("Call Liar", 70f))
                        {
                            try { if (s != null) s.CallLiar(); else if (spinMgr != null) spinMgr.CallLiar(p); } catch { }
                        }
                    }
                    ui.EndRow();
                }
            }
            else
            {
                ui.Label("<color=#888888>No other players detected.</color>");
            }

            ui.Space(6);

            // SECTION 3: LOCAL PLAYER REELS RIGGING & DEATH SPIN IMMUNITY
            if (s != null)
            {
                ui.SubHeader("3. LOCAL REELS RIGGING & DEATH SPIN SURVIVAL");

                ui.Toggle(ref DeathSpinImmunity, "Death Spin Immunity (Automatically survive death spin & clear lethal moment)");

                ui.BeginRow(24f);
                if (ui.RowButton("<color=#55FFAA>[C] Trigger Safe Death Spin (Play Save Anim)</color>", 270f))
                {
                    try
                    {
                        if (spinMgr != null) { spinMgr.isDeadSpin = false; spinMgr.NetworkisDeadSpin = false; }
                        s.isDeadSpinMoment = false;
                        s.RpcPlaySaveAnim();
                    }
                    catch { }
                }
                if (ui.RowButton("<color=#FF4444>[C] Trigger Lethal Death Spin (Play Die Anim)</color>", 280f))
                {
                    try
                    {
                        if (spinMgr != null) { spinMgr.isDeadSpin = true; spinMgr.NetworkisDeadSpin = true; }
                        s.isDeadSpinMoment = true;
                        s.DieAnim();
                    }
                    catch { }
                }
                ui.EndRow();

                ui.Space(4);
                string myReelsStr = $"[ {GetSpinSymbolGlyph(s._localSlot1)} {GetSpinSymbolGlyph(s._localSlot2)} {GetSpinSymbolGlyph(s._localSlot3)} {GetSpinSymbolGlyph(s._localSlot4)} ]";
                ui.Label($"Local Machine: <b>{myReelsStr}</b>  |  Matches Target Criterion: <color=#00FFB0><b>{s.CorrectSlotCount()}/4</b></color>");

                // Reel 1
                ui.BeginRow(24f);
                ui.RowLabel($"Reel 1 {GetSpinSymbolBadge(s._localSlot1)}:", 140f);
                if (ui.RowButton("<color=#55FFAA>♠ Spades</color>", 80f)) SetReelValue(s, 1, 0);
                if (ui.RowButton("<color=#FF5555>♥ Hearts</color>", 80f)) SetReelValue(s, 1, 1);
                if (ui.RowButton("<color=#44AAFF>♦ Diamonds</color>", 90f)) SetReelValue(s, 1, 2);
                if (ui.RowButton("<color=#FFAA00>♣ Clubs</color>", 75f)) SetReelValue(s, 1, 3);
                ui.EndRow();

                // Reel 2
                ui.BeginRow(24f);
                ui.RowLabel($"Reel 2 {GetSpinSymbolBadge(s._localSlot2)}:", 140f);
                if (ui.RowButton("<color=#55FFAA>♠ Spades</color>", 80f)) SetReelValue(s, 2, 0);
                if (ui.RowButton("<color=#FF5555>♥ Hearts</color>", 80f)) SetReelValue(s, 2, 1);
                if (ui.RowButton("<color=#44AAFF>♦ Diamonds</color>", 90f)) SetReelValue(s, 2, 2);
                if (ui.RowButton("<color=#FFAA00>♣ Clubs</color>", 75f)) SetReelValue(s, 2, 3);
                ui.EndRow();

                // Reel 3
                ui.BeginRow(24f);
                ui.RowLabel($"Reel 3 {GetSpinSymbolBadge(s._localSlot3)}:", 140f);
                if (ui.RowButton("<color=#55FFAA>♠ Spades</color>", 80f)) SetReelValue(s, 3, 0);
                if (ui.RowButton("<color=#FF5555>♥ Hearts</color>", 80f)) SetReelValue(s, 3, 1);
                if (ui.RowButton("<color=#44AAFF>♦ Diamonds</color>", 90f)) SetReelValue(s, 3, 2);
                if (ui.RowButton("<color=#FFAA00>♣ Clubs</color>", 75f)) SetReelValue(s, 3, 3);
                ui.EndRow();

                // Reel 4
                ui.BeginRow(24f);
                ui.RowLabel($"Reel 4 {GetSpinSymbolBadge(s._localSlot4)}:", 140f);
                if (ui.RowButton("<color=#55FFAA>♠ Spades</color>", 80f)) SetReelValue(s, 4, 0);
                if (ui.RowButton("<color=#FF5555>♥ Hearts</color>", 80f)) SetReelValue(s, 4, 1);
                if (ui.RowButton("<color=#44AAFF>♦ Diamonds</color>", 90f)) SetReelValue(s, 4, 2);
                if (ui.RowButton("<color=#FFAA00>♣ Clubs</color>", 75f)) SetReelValue(s, 4, 3);
                ui.EndRow();

                // Bulk presets
                ui.BeginRow(26f);
                if (ui.RowButton("<color=#FFD700><b>JACKPOT: All ♥ Hearts (1111)</b></color>", 240f))
                {
                    SetAllReels(s, 1);
                }
                if (spinMgr != null)
                {
                    if (ui.RowButton("<color=#00FFB0><b>Match Target Criterion</b></color>", 180f))
                    {
                        SetAllReels(s, (int)spinMgr.currentRoundCriterion);
                    }
                }
                ui.EndRow();

                ui.BeginRow(26f);
                if (ui.RowButton("All <color=#55FFAA>♠ Spades (0)</color>", 125f)) SetAllReels(s, 0);
                if (ui.RowButton("All <color=#FF5555>♥ Hearts (1)</color>", 125f)) SetAllReels(s, 1);
                if (ui.RowButton("All <color=#44AAFF>♦ Diamonds (2)</color>", 135f)) SetAllReels(s, 2);
                if (ui.RowButton("All <color=#FFAA00>♣ Clubs (3)</color>", 120f)) SetAllReels(s, 3);
                ui.EndRow();

                ui.Space(6);

                // SECTION 4: ACTIONS & SPECIAL SPINS
                ui.SubHeader("4. ACTIONS, BID MANIPULATION & SPECIAL SPINS");
                ui.BeginRow(26f);
                if (ui.RowButton("<color=#55FFAA><b>Bid +1</b></color>", 75f)) { try { s.RiseCmd(s.BetCount + 1); } catch { } }
                if (ui.RowButton("Bid = 1", 65f)) { try { s.RiseCmd(1); } catch { } }
                if (ui.RowButton("Bid = 2", 65f)) { try { s.RiseCmd(2); } catch { } }
                if (ui.RowButton("Bid = 3", 65f)) { try { s.RiseCmd(3); } catch { } }
                if (ui.RowButton("Bid = 4", 65f)) { try { s.RiseCmd(4); } catch { } }
                if (ui.RowButton("<color=#FF3344><b>CALL LIAR</b></color>", 95f)) { try { s.CallLiar(); } catch { } }
                if (ui.RowButton("<color=#FFD700><b>CLAIM JACKPOT</b></color>", 125f)) { try { s.CmdAnswerJackpot(); s.AnswerJackpot(1); } catch { } }
                ui.EndRow();

                ui.BeginRow(26f);
                if (ui.RowButton("Double Spin", 100f)) { try { s.Cmd_RequestDoubleSpin(); } catch { } }
                if (ui.RowButton("Secret Spin", 100f)) { try { s.Cmd_RequestSecretSpin(); } catch { } }
                if (ui.RowButton("Play Reel Anim", 110f)) { try { s.CmdRequestPlaySlotAnim(); } catch { } }
                if (ui.RowButton("Stop Reel Anim", 110f)) { try { s.CmdRequestStopSlotAnim(); } catch { } }
                if (ui.RowButton("Reveal All", 90f)) { try { s.RevealAll(); } catch { } }
                ui.EndRow();
            }

            ui.Space(6);

            // SECTION 5: HOST TABLE MANAGEMENT [H]
            if (spinMgr != null)
            {
                ui.SubHeader("5. HOST TABLE & ROUND CONTROL <color=#FFDD55>[H]</color>");
                ui.BeginRow(24f);
                ui.RowLabel("Force Table Criterion:", 150f);
                if (ui.RowButton("<color=#55FFAA>♠ Spades (0)</color>", 115f)) { try { spinMgr.SetCurrentTable(0); } catch { } }
                if (ui.RowButton("<color=#FF5555>♥ Hearts (1)</color>", 115f)) { try { spinMgr.SetCurrentTable(1); } catch { } }
                if (ui.RowButton("<color=#44AAFF>♦ Diamonds (2)</color>", 125f)) { try { spinMgr.SetCurrentTable(2); } catch { } }
                if (ui.RowButton("<color=#FFAA00>♣ Clubs (3)</color>", 110f)) { try { spinMgr.SetCurrentTable(3); } catch { } }
                ui.EndRow();

                ui.BeginRow(26f);
                if (ui.RowButton("<color=#FFDD55>[H]</color> Reset Round", 120f)) { try { spinMgr.ResetRound(); } catch { } }
                if (ui.RowButton("<color=#FFDD55>[H]</color> Kill Turn Loser", 140f)) { try { spinMgr.KillLastPlayer(); } catch { } }
                if (ui.RowButton("<color=#FFDD55>[H]</color> Dead Spin Anim", 130f)) { try { spinMgr.DeadSpinAnimOpen(); } catch { } }
                ui.EndRow();
            }
        }

        private static void DrawRoulTab(UiBuilder ui, string gt, GameObject me, GameObject[] players)
        {
            ui.Header("LIAR'S ROULETTE CONTROLS");

            if (me == null)
            {
                ui.Label("<color=#FF8800>Join a Roulette match to activate.</color>");
                return;
            }

            var r = me.GetComponent<RouletteGamePlay>();
            if (r == null)
            {
                ui.Label("<color=#888888>Not in Roulette mode.</color>");
                return;
            }

            ui.Label($"Target: {r.target} | CanFire: {r.canfire} | Covering: {r.Covering}");
            ui.BeginRow(26f);
            if (ui.RowButton("<color=#55FFAA>God Mode (Never Die)</color>", 180f)) r.canfire = false;
            if (ui.RowButton("Toggle Cover Mode", 170f)) r.Covering = !r.Covering;
            ui.EndRow();
        }

        private static void DrawPlayersTab(UiBuilder ui, string gt, GameObject me, GameObject[] players)
        {
            ui.Header("LOBBY & MATCH PLAYER CONTROLS");

            if (players == null || players.Length == 0)
            {
                ui.Label("<color=#888888>No players detected in session.</color>");
                return;
            }

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null) continue;

                var ps = p.GetComponent<PlayerStats>();
                if (ps == null) continue;

                var poc = p.GetComponent<PlayerObjectController>();
                bool isMe = p == me;

                // Player Info & ESP line
                string espText = "";
                if (gt == "Poker")
                {
                    var pg = p.GetComponent<PokerGamePlay>();
                    if (pg != null)
                    {
                        var pCards = PokerController.GetCardsForPoker(pg);
                        var cardBadges = new List<string>();
                        foreach (var cType in pCards) cardBadges.Add(UiBuilder.GetCardBadge(cType));
                        espText = $" [Cards: {(cardBadges.Count > 0 ? string.Join(" ", cardBadges) : "none")}]";
                    }
                }
                else if (gt == "Texas")
                {
                    var tg = p.GetComponent<TexasGamePlay>();
                    if (tg != null)
                    {
                        var tCards = TexasController.GetTexasCardsForPlayer(tg);
                        if (tCards.Count > 0)
                        {
                            var tBadges = new List<string>();
                            foreach (var cId in tCards) tBadges.Add(UiBuilder.GetTexasCardBadge(cId));
                            espText = $" [Hand: {string.Join(" ", tBadges)}]";
                        }
                        else
                        {
                            espText = " [Hand: <color=#667788>Hidden (Server Fog)</color>]";
                        }
                    }
                }
                else if (gt == "Dice")
                {
                    var diceList = DiceController.GetDiceForPlayer(p);
                    if (diceList.Count > 0)
                    {
                        var diceBadges = new List<string>();
                        foreach (var dVal in diceList) diceBadges.Add(UiBuilder.GetDiceBadge(dVal));
                        espText = $" [Dice: {string.Join(" ", diceBadges)}]";
                    }
                }
                else
                {
                    var cardTypes = CardController.GetCardTypesForPlayer(p);
                    if (cardTypes.Count > 0)
                    {
                        var cardBadges = new List<string>();
                        foreach (var t in cardTypes) cardBadges.Add(UiBuilder.GetCardBadge(t));
                        espText = $" [Cards: {string.Join(" ", cardBadges)}]";
                    }
                }

                string pName = isMe ? GetFormattedPlayerName(BasePlayerName ?? ps.NetworkPlayerName) : ps.NetworkPlayerName;
                ui.BeginRow(26f);
                ui.RowLabel($"<b>{(isMe ? "<color=#00FFB0>[YOU]</color> " : "      ")}{pName}</b> {(ps.NetworkDead ? "<color=#FF3333>[DEAD]</color>" : "")}{espText}", 280f);

                if (poc != null && ui.RowButton("Steam", 55f))
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://steamcommunity.com/profiles/{poc.PlayerSteamID}") { UseShellExecute = true }); } catch { }
                }

                if (!isMe)
                {
                    if (ui.RowButton("<color=#FFDD55>[H]</color> Kill", 60f)) SyncKillPlayer(p, me);
                    if (ui.RowButton("<color=#FFDD55>[H]</color> Safe Rev", 75f)) CardController.SetRevolverSafe(p);
                    if (ui.RowButton("<color=#FFDD55>[H]</color> Dead Rev", 80f)) CardController.SetRevolverDeath(p);
                    if (ui.RowButton("<color=#FFDD55>[H]</color> Drink", 65f)) ForceDrinkPlayer(p);
                    if (gt == "Texas")
                    {
                        if (ui.RowButton("<color=#55FFAA>[C]</color> Force Fold", 95f))
                        {
                            ForceFoldPlayer(p, me);
                        }
                    }
                    else if (gt == "Poker")
                    {
                        if (ui.RowButton("<color=#55FFAA>[C]</color> Force Pass", 95f))
                        {
                            ForcePassPlayer(p, me);
                        }
                        if (ui.RowButton("<color=#FFD700>Liar</color>", 50f))
                        {
                            ForceLiarPlayer(p);
                        }
                    }
                    else
                    {
                        if (ui.RowButton("All -> K", 65f)) CardController.SetAllCardsForPlayer(p, 1);
                        if (ui.RowButton("All -> A", 65f)) CardController.SetAllCardsForPlayer(p, 3);
                    }
                    if (poc != null && ui.RowButton("<color=#FFDD55>[H]</color> Kick", 60f)) { try { poc.NetworkKicked = true; poc.Kicked = true; } catch { } }
                }
                else
                {
                    if (ui.RowButton("<color=#FF4444>[Kill Self]</color>", 75f)) SyncKillPlayer(p, me);
                    if (ui.RowButton("<color=#55FFAA>[Safe Rev]</color>", 75f)) CardController.SetRevolverSafe(p);
                    if (ui.RowButton("<color=#FF4444>[Dead Rev]</color>", 75f)) CardController.SetRevolverDeath(p);
                }
                ui.EndRow();

                // Revolver Cylinder Radar & Interactive Chamber/Bullet Controls
                if (gt == "Poker" || p.GetComponent<PokerGamePlay>() != null)
                {
                    var pp = p.GetComponent<PokerGamePlay>();
                    if (pp != null)
                    {
                        int pCurChamber = pp.CurrentBullet;
                        var pBullets = PokerController.GetBullets(pp);
                        var overrideState = PokerController.GetShotOverride(pp);

                        string pOverrideBadge = overrideState switch
                        {
                            PokerController.ShotOutcomeOverride.MustDie => "<color=#FF3333><b>[ARMED: DIE ON SHOT]</b></color>",
                            PokerController.ShotOutcomeOverride.MustSurvive => "<color=#55FFAA><b>[ARMED: SURVIVE SHOT]</b></color>",
                            _ => "<color=#8899AA>[DEFAULT RNG]</color>"
                        };

                        bool isNextFatal = (overrideState == PokerController.ShotOutcomeOverride.MustDie) ||
                                           (overrideState != PokerController.ShotOutcomeOverride.MustSurvive && pCurChamber >= 0 && pCurChamber < pBullets.Count && pBullets[pCurChamber] != -1);
                        string pStatus = isNextFatal ? "<color=#FF3333>FATAL SHOT</color>" : "<color=#55FFAA>SURVIVE SHOT</color>";
                        string pRadar = PokerController.GetPokerCylinderRadar(pp);

                        ui.BeginRow(24f);
                        ui.RowLabel($"   Poker Gun: {pRadar}  Ch: <b>{(pCurChamber >= 0 && pCurChamber < 6 ? (pCurChamber + 1).ToString() : pCurChamber.ToString())}/6</b> [{pStatus}] {pOverrideBadge}", 400f);
                        ui.EndRow();

                        ui.BeginRow(24f);
                        if (ui.RowButton("<color=#FF4444><b>★ Die on Shot</b></color>", 110f)) PokerController.SetShotOverride(pp, PokerController.ShotOutcomeOverride.MustDie);
                        if (ui.RowButton("<color=#55FFAA><b>★ Survive Shot</b></color>", 115f)) PokerController.SetShotOverride(pp, PokerController.ShotOutcomeOverride.MustSurvive);
                        if (ui.RowButton("Reset RNG", 75f)) PokerController.ClearShotOverride(pp);
                        if (ui.RowButton("<color=#FF2222>Kill Now</color>", 75f)) PokerController.ExecuteForceKill(pp);
                        if (ui.RowButton("Ch +1", 48f)) PokerController.RotateChamber(pp, 1);
                        if (ui.RowButton("Ch -1", 48f)) PokerController.RotateChamber(pp, -1);
                        ui.EndRow();

                        ui.BeginRow(24f);
                        ui.RowLabel("   Chambers:", 80f);
                        for (int cIdx = 0; cIdx < 6; cIdx++)
                        {
                            bool isCur = (cIdx == pCurChamber);
                            bool isDeadly = (cIdx < pBullets.Count && pBullets[cIdx] != -1);
                            string col = isDeadly ? "#FF4444" : "#55FFAA";
                            string prefix = isCur ? ">" : "";
                            string suffix = isCur ? "<" : "";
                            int targetChamber = cIdx;
                            if (ui.RowButton($"<color={col}>{prefix}[{cIdx + 1}]{suffix}</color>", 42f))
                            {
                                PokerController.ToggleChamberBullet(pp, targetChamber);
                            }
                        }
                        if (ui.RowButton("All Safe", 65f)) PokerController.SetAllChambersSafe(pp);
                        if (ui.RowButton("All Die", 65f)) PokerController.SetAllChambersDeadly(pp);
                        ui.EndRow();
                    }
                }
                else if (gt == "Texas" || p.GetComponent<TexasGamePlay>() != null)
                {
                    var tp = p.GetComponent<TexasGamePlay>();
                    if (tp != null)
                    {
                        int curBul = tp.NetworkBullets;
                        bool willDie = tp._revolverWillDieLocal;
                        bool godSave = tp._revolverGodSaveLocal;
                        string pStatus;
                        if (willDie) pStatus = "<color=#FF3333>FATAL SHOT</color>";
                        else if (godSave) pStatus = "<color=#55FFAA>SURVIVE SHOT</color>";
                        else if (curBul >= 6) pStatus = "<color=#FF3333>FULL (FATAL)</color>";
                        else if (curBul <= 0) pStatus = "<color=#55FFAA>EMPTY (SAFE)</color>";
                        else pStatus = $"<color=#FFDD55>{curBul}/6 CHANCE</color>";

                        string tRadar = TexasController.GetTexasRadar(tp);

                        ui.BeginRow(24f);
                        ui.RowLabel($"   Texas Gun: {tRadar}  Bullets: <b>{curBul}/6</b> [{pStatus}]", 270f);
                        if (ui.RowButton("<color=#FF4444><b>Die on Shot</b></color>", 95f)) TexasController.SetPlayerShotDeadly(tp);
                        if (ui.RowButton("<color=#55FFAA><b>Survive Shot</b></color>", 95f)) TexasController.SetPlayerShotSafe(tp);
                        if (ui.RowButton("Bul +1", 50f)) TexasController.AddBullets(tp, 1);
                        if (ui.RowButton("Bul -1", 50f)) TexasController.AddBullets(tp, -1);
                        ui.EndRow();

                        ui.BeginRow(24f);
                        ui.RowLabel("   Load Bullets:", 95f);
                        for (int bIdx = 0; bIdx <= 6; bIdx++)
                        {
                            bool isCur = (bIdx == curBul);
                            string col = (bIdx == 0) ? "#55FFAA" : (bIdx == 6 ? "#FF4444" : "#FFCC44");
                            string prefix = isCur ? ">" : "";
                            string suffix = isCur ? "<" : "";
                            int targetB = bIdx;
                            if (ui.RowButton($"<color={col}>{prefix}[{bIdx}]{suffix}</color>", 38f))
                            {
                                TexasController.SetBullets(tp, targetB);
                            }
                        }
                        if (ui.RowButton("All Safe", 65f)) TexasController.SetPlayerShotSafe(tp);
                        if (ui.RowButton("All Die", 65f)) TexasController.SetPlayerShotDeadly(tp);
                        ui.EndRow();
                    }
                }
                else
                {
                    int pCur = CardController.GetCurrentChamber(p);
                    int pBul = CardController.GetRevolverBullet(p);
                    int nextChamber = (pCur % 6) + 1;
                    bool isFatalNext = (nextChamber == pBul);
                    bool isChambered = (pCur == pBul);
                    string pStatus = isFatalNext ? "<color=#FF3333>FATAL NEXT</color>" : (isChambered ? "<color=#FF6666>CHAMBERED</color>" : "<color=#55FFAA>SAFE</color>");
                    string pRadar = UiBuilder.GetRevolverRadar(pCur, pBul);

                    ui.BeginRow(24f);
                    ui.RowLabel($"   Cylinder: {pRadar}  Chamber: <b>{pCur}/6</b> | Bullet: <b>{pBul}</b>  [{pStatus}]", 270f);
                    if (ui.RowButton("<color=#55FFAA>Set Safe</color>", 75f)) CardController.SetRevolverSafe(p);
                    if (ui.RowButton("<color=#FF4444>Set Deadly</color>", 85f)) CardController.SetRevolverDeath(p);
                    if (ui.RowButton("Bullet +1", 70f)) CardController.SetRevolver(p, (pBul % 6) + 1);
                    if (ui.RowButton("Bullet -1", 70f)) CardController.SetRevolver(p, pBul <= 1 ? 6 : pBul - 1);
                    ui.EndRow();

                    ui.BeginRow(24f);
                    ui.RowLabel("   Chamber Pos:", 100f);
                    if (ui.RowButton("Ch +1", 50f)) CardController.SetCurrentChamber(p, (pCur + 1) % 6);
                    if (ui.RowButton("Ch -1", 50f)) CardController.SetCurrentChamber(p, (pCur + 5) % 6);
                    ui.RowLabel("Bullet Pos:", 75f);
                    for (int cIdx = 0; cIdx < 6; cIdx++)
                    {
                        int targetBullet = cIdx + 1;
                        string col = (targetBullet == pBul) ? "#FF4444" : (targetBullet == pCur ? "#00FFB0" : "#8899AA");
                        if (ui.RowButton($"<color={col}>[{targetBullet}]</color>", 32f))
                        {
                            CardController.SetRevolver(p, targetBullet);
                        }
                    }
                    ui.EndRow();
                }

                // Hand Cards Manipulation for Opponents
                if (!isMe && gt != "Dice")
                {
                    var pCardTypes = CardController.GetCardTypesForPlayer(p);
                    var pBadges = new List<string>();
                    if (gt == "Texas")
                    {
                        for (int ci = 0; ci < pCardTypes.Count; ci++) pBadges.Add(UiBuilder.GetTexasCardBadge(pCardTypes[ci]));
                    }
                    else
                    {
                        for (int ci = 0; ci < pCardTypes.Count; ci++) pBadges.Add(UiBuilder.GetCardBadge(pCardTypes[ci]));
                    }
                    string pCardsStr = pBadges.Count > 0 ? string.Join(" ", pBadges) : "<color=#8899AA>[Hidden / Not Dealt]</color>";

                    ui.BeginRow(24f);
                    ui.RowLabel($"   Hand Cards: {pCardsStr}", 220f);
                    if (gt == "Texas")
                    {
                        var tp = p.GetComponent<TexasGamePlay>();
                        if (ui.RowButton("Pocket AA", 75f)) { TexasController.SetTexasHand(tp, 52, 51); }
                        if (ui.RowButton("Pocket KK", 75f)) { TexasController.SetTexasHand(tp, 48, 47); }
                        if (ui.RowButton("<color=#FF6666>7-2 Bluff</color>", 75f)) { TexasController.SetTexasHand(tp, 24, 2); }
                        if (ui.RowButton("Fold", 45f)) { ForceFoldPlayer(p, me); }
                    }
                    else if (gt == "Poker")
                    {
                        var pp = p.GetComponent<PokerGamePlay>();
                        if (ui.RowButton("Pair AA", 60f)) { CardController.SetAllCardsForPlayer(p, 3); }
                        if (ui.RowButton("Pair KK", 60f)) { CardController.SetAllCardsForPlayer(p, 1); }
                        if (ui.RowButton("Pair QQ", 60f)) { CardController.SetAllCardsForPlayer(p, 2); }
                        if (ui.RowButton("Both D", 55f)) { CardController.SetAllCardsForPlayer(p, -1); }
                        if (ui.RowButton("Pass", 45f)) { ForcePassPlayer(p, me); }
                    }
                    else
                    {
                        if (ui.RowButton("All -> K", 60f)) CardController.SetAllCardsForPlayer(p, 1);
                        if (ui.RowButton("All -> Q", 60f)) CardController.SetAllCardsForPlayer(p, 2);
                        if (ui.RowButton("All -> A", 60f)) CardController.SetAllCardsForPlayer(p, 3);
                        if (ui.RowButton("All -> J", 60f)) CardController.SetAllCardsForPlayer(p, 4);
                        if (ui.RowButton("All -> D", 60f)) CardController.SetAllCardsForPlayer(p, -1);
                    }
                    ui.EndRow();

                    // Individual Card Slot Replacers for Opponent
                    if (gt == "Texas")
                    {
                        var tp = p.GetComponent<TexasGamePlay>();
                        if (tp != null)
                        {
                            int tc0 = pCardTypes.Count > 0 ? pCardTypes[0] : 52;
                            int tc1 = pCardTypes.Count > 1 ? pCardTypes[1] : 51;

                            ui.BeginRow(22f);
                            ui.RowLabel($"   C1 {UiBuilder.GetTexasCardBadge(tc0)}:", 80f);
                            if (ui.RowButton("A♠", 30f)) TexasController.ApplyTexasCardChange(tp, 0, 52);
                            if (ui.RowButton("K♠", 30f)) TexasController.ApplyTexasCardChange(tp, 0, 48);
                            if (ui.RowButton("Q♠", 30f)) TexasController.ApplyTexasCardChange(tp, 0, 44);
                            if (ui.RowButton("J♠", 30f)) TexasController.ApplyTexasCardChange(tp, 0, 40);
                            if (ui.RowButton("10♠", 32f)) TexasController.ApplyTexasCardChange(tp, 0, 36);
                            if (ui.RowButton("7♦", 28f)) TexasController.ApplyTexasCardChange(tp, 0, 22);
                            if (ui.RowButton("2♣", 28f)) TexasController.ApplyTexasCardChange(tp, 0, 1);
                            ui.Space(8f);
                            ui.RowLabel($"C2 {UiBuilder.GetTexasCardBadge(tc1)}:", 80f);
                            if (ui.RowButton("A♥", 30f)) TexasController.ApplyTexasCardChange(tp, 1, 51);
                            if (ui.RowButton("K♥", 30f)) TexasController.ApplyTexasCardChange(tp, 1, 47);
                            if (ui.RowButton("Q♥", 30f)) TexasController.ApplyTexasCardChange(tp, 1, 43);
                            if (ui.RowButton("J♥", 30f)) TexasController.ApplyTexasCardChange(tp, 1, 39);
                            if (ui.RowButton("10♥", 32f)) TexasController.ApplyTexasCardChange(tp, 1, 35);
                            if (ui.RowButton("7♠", 28f)) TexasController.ApplyTexasCardChange(tp, 1, 24);
                            if (ui.RowButton("2♦", 28f)) TexasController.ApplyTexasCardChange(tp, 1, 2);
                            ui.EndRow();
                        }
                    }
                    else if (gt == "Poker")
                    {
                        var pp = p.GetComponent<PokerGamePlay>();
                        if (pp != null)
                        {
                            ui.BeginRow(22f);
                            ui.RowLabel("   Card #1:", 75f);
                            if (ui.RowButton("K", 26f)) PokerController.SetPokerCard(pp, 0, 1);
                            if (ui.RowButton("Q", 26f)) PokerController.SetPokerCard(pp, 0, 2);
                            if (ui.RowButton("A", 26f)) PokerController.SetPokerCard(pp, 0, 3);
                            if (ui.RowButton("J", 26f)) PokerController.SetPokerCard(pp, 0, 4);
                            if (ui.RowButton("D", 26f)) PokerController.SetPokerCard(pp, 0, -1);
                            ui.Space(12f);
                            ui.RowLabel("Card #2:", 70f);
                            if (ui.RowButton("K", 26f)) PokerController.SetPokerCard(pp, 1, 1);
                            if (ui.RowButton("Q", 26f)) PokerController.SetPokerCard(pp, 1, 2);
                            if (ui.RowButton("A", 26f)) PokerController.SetPokerCard(pp, 1, 3);
                            if (ui.RowButton("J", 26f)) PokerController.SetPokerCard(pp, 1, 4);
                            if (ui.RowButton("D", 26f)) PokerController.SetPokerCard(pp, 1, -1);
                            ui.EndRow();
                        }
                    }
                    else
                    {
                        int numSlots = pCardTypes.Count > 0 ? Math.Min(pCardTypes.Count, 5) : 5;
                        ui.BeginRow(22f);
                        ui.RowLabel("   Slot Replacers:", 115f);
                        for (int sIdx = 0; sIdx < numSlots; sIdx++)
                        {
                            int targetSlot = sIdx;
                            if (ui.RowButton($"#{targetSlot + 1}:K", 44f)) CardController.SetPlayerCardSlot(p, targetSlot, 1);
                            if (ui.RowButton($"#{targetSlot + 1}:Q", 44f)) CardController.SetPlayerCardSlot(p, targetSlot, 2);
                            if (ui.RowButton($"#{targetSlot + 1}:A", 44f)) CardController.SetPlayerCardSlot(p, targetSlot, 3);
                            if (ui.RowButton($"#{targetSlot + 1}:D", 44f)) CardController.SetPlayerCardSlot(p, targetSlot, -1);
                        }
                        ui.EndRow();
                    }
                }
            }
        }

        private static void DrawMoveTab(UiBuilder ui, GameObject me)
        {
            ui.Header("MOVEMENT & HEAD MECHANICS <color=#55FFAA>[C]</color>");

            if (me == null)
            {
                ui.Label("<color=#FF8800>Join a match to use movement controls.</color>");
                return;
            }

            var cc = me.GetComponent<CharController>();
            if (cc != null)
            {
                if (ui.Button($"360 Degree Head Rotation: {(_h360Active ? "<color=#00FFAA>ON</color>" : "<color=#FF4444>OFF</color>")}", -1f, 26f))
                {
                    Toggle360Head(cc);
                }
            }

            ui.Space(4);
            ui.Header("EMOTES <color=#55FFAA>[C]</color>");
            ui.BeginRow(24f);
            for (int e = 1; e <= 5; e++)
            {
                int emoteIndex = e;
                if (ui.RowButton($"Emote {emoteIndex}", 85f) && cc != null)
                {
                    PlayEmote(cc, emoteIndex);
                }
            }
            ui.EndRow();

            ui.Space(4);
            ui.Header("POSITION NUDGE <color=#55FFAA>[C]</color>");
            ui.BeginRow(26f);
            if (ui.RowButton("Forward (0.5m)", 125f)) me.transform.position += me.transform.forward * 0.5f;
            if (ui.RowButton("Backward (0.5m)", 125f)) me.transform.position -= me.transform.forward * 0.5f;
            if (ui.RowButton("Left (0.5m)", 105f)) me.transform.position -= me.transform.right * 0.5f;
            if (ui.RowButton("Right (0.5m)", 105f)) me.transform.position += me.transform.right * 0.5f;
            ui.EndRow();
        }

        private static void Toggle360Head(CharController c)
        {
            if (c == null) return;
            try
            {
                if (_h360Active)
                {
                    c.MaxX = _mxs; c.MaxY = _mys; c.MinX = _mnxs; c.MinY = _mnys;
                    _h360Active = false;
                }
                else
                {
                    _mxs = c.MaxX; _mys = c.MaxY; _mnxs = c.MinX; _mnys = c.MinY;
                    c.MaxX = 9999f; c.MaxY = 9999f; c.MinX = -9999f; c.MinY = -9999f;
                    _h360Active = true;
                }
            }
            catch { }
        }

        private static void PlayEmote(CharController c, int n)
        {
            if (c == null) return;
            try
            {
                if (n == 1) c.PlayEmote1Sfx(1);
                else if (n == 2) c.PlayEmote2Sfx();
                else if (n == 3) c.PlayEmote3Sfx();
                else if (n == 4) c.PlayEmoteDeadSfx();
                else if (n == 5) c.PlayRandomVoice();
            }
            catch { }
        }

        private static void DrawStatsTab(UiBuilder ui)
        {
            ui.Header("PROFILE STATS & MMR MANIPULATION <color=#55FFAA>[C]</color>");

            var db = DatabaseManager.instance;
            if (db == null)
            {
                ui.Label("<color=#FF8800>DatabaseManager not loaded.</color>");
                return;
            }

            try
            {
                ui.Label($"Level: <b><color=#00FFB0>{db.Level}</color></b>  |  XP: <b><color=#55FFAA>{db.currentXP} / {db.needs}</color></b>");
                ui.Label($"MMR: Deck={db.DeckMMR5} | Spin={db.SpinMMR5} | Dice={db.DiceMMR5} | Poker={db.PokerMMR5}");

                ui.BeginRow(26f);
                if (ui.RowButton("Fill XP to Max", 140f)) db.currentXP = db.needs;
                if (ui.RowButton("Level +1", 95f)) db.Level = (DatabaseManager.Levels)((int)db.Level + 1);
                if (ui.RowButton("Level = 99", 95f)) db.Level = (DatabaseManager.Levels)99;
                ui.EndRow();

                ui.Space(4);
                ui.BeginRow(26f);
                if (ui.RowButton("Deck Wins +1", 125f)) db.DeckWins++;
                if (ui.RowButton("Spin Wins +1", 125f)) db.SpinWin++;
                if (ui.RowButton("Dice Wins +1", 125f)) db.DiceWins++;
                if (ui.RowButton("Poker Wins +1", 125f)) db.PokerWins++;
                ui.EndRow();
            }
            catch (Exception ex)
            {
                ui.Label($"<color=#FF3333>Stats reflection error: {ex.Message}</color>");
            }
        }

        private static void DrawSkinsTab(UiBuilder ui, GameObject me, GameObject[] players)
        {
            ui.Header("COSMETICS & SKINS UNLOCKER <color=#55FFAA>[C]</color>");

            if (ui.Button("<color=#FFD700>Unlock All Characters & Cosmetics</color>", -1f, 28f))
            {
                try
                {
                    var sm = UnityEngine.Object.FindFirstObjectByType<SkinManager>();
                    if (sm != null)
                    {
                        sm.MaxCharacter = 8;
                        for (int ch = 0; ch < 9; ch++) { sm.CurrentCharacter = ch; sm.CurrentSkin = 0; }
                    }
                }
                catch { }
            }

            if (me != null && ui.Button("Set Local In-Match Level to 99", -1f, 26f))
            {
                var cc = me.GetComponent<CharController>();
                if (cc != null) cc.Networklevel = 99;
            }

            ui.Space(6);
            ui.Header("PLAYER SKINS IN MATCH");
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p == null) continue;
                    var ps = p.GetComponent<PlayerStats>();
                    var poc = p.GetComponent<PlayerObjectController>();
                    if (ps != null && poc != null)
                    {
                        ui.Label($"  Player '{ps.NetworkPlayerName}': Skin Index={poc.PlayerSkin}");
                    }
                }
            }
        }

        private static void DrawDevTab(UiBuilder ui, string gt, Manager mgr, GameObject me, GameObject[] players)
        {
            ui.Header("DEVELOPER & REVERSE ENGINEERING SUITE");

            if (ui.Button("<color=#00FFB0>[C] Dump Game State & Symbols to Desktop [P / F2]</color>", -1f, 28f))
            {
                _dumpStatus = StateDumper.DumpStateToDesktop(gt, mgr, me, players);
            }

            ui.BeginRow(26f);
            if (ui.RowButton("<color=#FFDD55>[H]</color> <color=#00FFB0>Force End Game (Win)</color>", 210f) && mgr != null && me != null)
            {
                try { mgr.ForceEndGameWithWinner(me.GetComponent<PlayerStats>()); } catch { }
            }
            if (ui.RowButton("<color=#55FFAA>[C]</color> <color=#FF5555>Kill Self</color>", 120f) && me != null)
            {
                SyncKillPlayer(me, me);
            }
            if (ui.RowButton("<color=#FFDD55>[H]</color> <color=#FF3333>Kill All Others</color>", 160f) && players != null)
            {
                foreach (var p in players) if (p != me) SyncKillPlayer(p, me);
            }
            ui.EndRow();

            ui.BeginRow(26f);
            if (ui.RowButton("<color=#55FFAA>[C]</color> Force Host Migration", 190f))
            {
                ForceHostMigration(players, me);
            }
            if (ui.RowButton("<color=#FFDD55>[H]</color> Force Turn To Me", 180f) && mgr != null && me != null)
            {
                var ps = me.GetComponent<PlayerStats>();
                if (ps != null)
                {
                    mgr.NetworkActivePlayerSlot = ps.Slot;
                    if (players != null) foreach (var p in players) { var pss = p.GetComponent<PlayerStats>(); if (pss != null) pss.NetworkHaveTurn = (p == me); }
                }
            }
            ui.EndRow();
        }

        private static void DrawSelfTab(UiBuilder ui, GameObject me)
        {
            ui.Header("IDENTITY & RGB NAME TAG SPOOFER <color=#55FFAA>[NETWORKED FOR ALL]</color>");

            // Reset Button
            ui.BeginRow(28f);
            if (ui.RowButton("<color=#FF6666><b>[RESET ALL: Default Steam Name / Clear Tags & Colors]</b></color>", 440f))
            {
                ResetToDefaultIdentity(me);
            }
            ui.EndRow();
            ui.Space(4);

            // Initialize BasePlayerName if empty
            if (string.IsNullOrEmpty(BasePlayerName))
            {
                string raw = "";
                try { raw = SteamFriends.GetPersonaName(); } catch { }
                if (string.IsNullOrEmpty(raw) && me != null)
                {
                    var ps = me.GetComponent<PlayerStats>();
                    if (ps != null && !string.IsNullOrEmpty(ps.NetworkPlayerName))
                    {
                        raw = ps.NetworkPlayerName;
                    }
                }
                if (string.IsNullOrEmpty(raw)) raw = PlayerPrefs.GetString("PlayerName", "Inkwell");

                // Clean existing color markup and known tags
                raw = System.Text.RegularExpressions.Regex.Replace(raw, "<.*?>", string.Empty).Trim();
                string[] knownTags = { "[DEV]", "[PRO]", "[VIP]", "[ADMIN]", "[STAFF]", "[MOD]", "[GOD]" };
                foreach (var kt in knownTags)
                {
                    if (raw.StartsWith(kt, StringComparison.OrdinalIgnoreCase))
                    {
                        raw = raw.Substring(kt.Length).Trim();
                        break;
                    }
                }
                BasePlayerName = string.IsNullOrEmpty(raw) ? "Inkwell" : raw;
            }

            ui.BeginRow(26f);
            ui.RowLabel("Base Name:", 90f);
            BasePlayerName = GUI.TextField(new Rect(ui.CurrentX, ui.CurrentY, 180f, 24f), BasePlayerName ?? "");
            ui.Space(185f);
            if (ui.RowButton("Apply Base", 85f) && !string.IsNullOrEmpty(BasePlayerName))
            {
                SyncSelectedTag(me);
            }
            ui.EndRow();

            ui.Space(6);
            ui.Header("SECRET CLAN / DEV TAG SELECTION");
            ui.BeginRow(26f);
            if (ui.RowButton(SelectedTag == "[DEV]" ? "<color=#00FFB0>[DEV]</color>" : "[DEV]", 60f)) { SelectedTag = "[DEV]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "[PRO]" ? "<color=#00FFB0>[PRO]</color>" : "[PRO]", 60f)) { SelectedTag = "[PRO]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "[VIP]" ? "<color=#00FFB0>[VIP]</color>" : "[VIP]", 60f)) { SelectedTag = "[VIP]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "[ADMIN]" ? "<color=#00FFB0>[ADMIN]</color>" : "[ADMIN]", 70f)) { SelectedTag = "[ADMIN]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "[STAFF]" ? "<color=#00FFB0>[STAFF]</color>" : "[STAFF]", 70f)) { SelectedTag = "[STAFF]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "[MOD]" ? "<color=#00FFB0>[MOD]</color>" : "[MOD]", 60f)) { SelectedTag = "[MOD]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "[GOD]" ? "<color=#00FFB0>[GOD]</color>" : "[GOD]", 60f)) { SelectedTag = "[GOD]"; SyncSelectedTag(me); }
            if (ui.RowButton(SelectedTag == "None" ? "<color=#00FFB0>None</color>" : "None", 55f)) { SelectedTag = "None"; SyncSelectedTag(me); }
            ui.EndRow();

            ui.Space(4);
            ui.BeginRow(26f);
            ui.RowLabel("Custom Tag:", 95f);
            CustomTagInput = GUI.TextField(new Rect(ui.CurrentX, ui.CurrentY, 140f, 24f), CustomTagInput ?? "");
            ui.Space(145f);
            if (ui.RowButton("Use Custom", 95f) && !string.IsNullOrEmpty(CustomTagInput))
            {
                SelectedTag = CustomTagInput.StartsWith("[") ? CustomTagInput : $"[{CustomTagInput}]";
                SyncSelectedTag(me);
            }
            ui.EndRow();

            ui.Space(6);
            ui.Header("DYNAMIC RGB RAINBOW & COLOR CONTROLS");
            if (ui.Toggle(ref RgbTagEnabled, "Dynamic RGB Rainbow Tag (Visible for all players)")) { if (RgbTagEnabled) CustomColorEnabled = true; }
            if (ui.Toggle(ref RgbNameEnabled, "Dynamic RGB Rainbow Name (Visible for all players)")) { if (RgbNameEnabled) CustomColorEnabled = true; }
            ui.Slider("RGB Animation Cycle Speed", ref RgbSpeed, 0.2f, 5.0f);

            ui.Space(4);
            ui.SubHeader("Static Color Hex (used when RGB is OFF):");
            ui.BeginRow(26f);
            if (ui.RowButton("<color=#00FFB0>Cyan</color>", 65f)) { NameColorHex = "#00FFB0"; CustomColorEnabled = true; SyncSelectedTag(me); }
            if (ui.RowButton("<color=#FFD700>Gold</color>", 60f)) { NameColorHex = "#FFD700"; CustomColorEnabled = true; SyncSelectedTag(me); }
            if (ui.RowButton("<color=#FF3355>Crimson</color>", 75f)) { NameColorHex = "#FF3355"; CustomColorEnabled = true; SyncSelectedTag(me); }
            if (ui.RowButton("<color=#AA00FF>Purple</color>", 65f)) { NameColorHex = "#AA00FF"; CustomColorEnabled = true; SyncSelectedTag(me); }
            if (ui.RowButton("<color=#33FF77>Neon</color>", 60f)) { NameColorHex = "#33FF77"; CustomColorEnabled = true; SyncSelectedTag(me); }
            if (ui.RowButton("<color=#FF66CC>Pink</color>", 55f)) { NameColorHex = "#FF66CC"; CustomColorEnabled = true; SyncSelectedTag(me); }
            if (ui.RowButton("<color=#FFFFFF>White</color>", 60f)) { NameColorHex = "#FFFFFF"; CustomColorEnabled = false; SyncSelectedTag(me); }
            ui.EndRow();

            // Periodic throttled broadcast for RGB mode (updates every 2 seconds over network safely)
            if ((RgbTagEnabled || RgbNameEnabled) && Time.time - _lastRgbBroadcastTime > 2.0f)
            {
                _lastRgbBroadcastTime = Time.time;
                SyncSelectedTag(me);
            }

            ui.Space(6);
            ui.Header("NETWORKED PREVIEW & TELEMETRY");
            string sampleHex = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB((Time.time * RgbSpeed) % 1f, 1f, 1f));
            bool hasTag = (SelectedTag != "None" && !string.IsNullOrEmpty(SelectedTag));
            bool hasCol = CustomColorEnabled || RgbTagEnabled || RgbNameEnabled || (!string.IsNullOrEmpty(NameColorHex) && !string.Equals(NameColorHex, "#FFFFFF", StringComparison.OrdinalIgnoreCase));
            string previewTag = hasTag ? (hasCol ? (RgbTagEnabled ? $"<color=#{sampleHex}>{SelectedTag}</color> " : $"<color={NameColorHex}>{SelectedTag}</color> ") : $"{SelectedTag} ") : "";
            string previewName = hasCol ? (RgbNameEnabled ? $"<color=#{sampleHex}>{BasePlayerName}</color>" : $"<color={NameColorHex}>{BasePlayerName}</color>") : BasePlayerName;

            ui.Label($"Networked Name String: <b>{previewTag}{previewName}</b>");
            if (me != null)
            {
                var ps = me.GetComponent<PlayerStats>();
                var dg = me.GetComponent<DiceGamePlay>();
                bool isHost = (dg != null && dg.isServer);
                ui.Label($"Live SyncVar: <b>{(ps != null ? ps.NetworkPlayerName : "me")}</b>");
                ui.Label($"Session Authority: <b>{(isHost ? "<color=#55FFAA>[HOST / SERVER]</color>" : "<color=#FFDD55>[CLIENT]</color>")}</b>");
            }
            else
            {
                ui.Label("<color=#888888>Not currently connected to a match session.</color>");
            }

            ui.Space(4);
            ui.BeginRow(28f);
            if (ui.RowButton("<color=#00FFB0><b>[BROADCAST NAME & TAG TO NETWORK]</b></color>", 290f))
            {
                SyncSelectedTag(me);
            }
            ui.EndRow();
        }

        public static void ResetToDefaultIdentity(GameObject me)
        {
            try
            {
                SelectedTag = "None";
                CustomTagInput = "";
                RgbTagEnabled = false;
                RgbNameEnabled = false;
                NameColorHex = "#FFFFFF";
                CustomColorEnabled = false;

                string steamName = "";
                try { steamName = SteamFriends.GetPersonaName(); } catch { }
                if (string.IsNullOrEmpty(steamName) && me != null)
                {
                    var ps = me.GetComponent<PlayerStats>();
                    if (ps != null && !string.IsNullOrEmpty(ps.NetworkPlayerName))
                        steamName = ps.NetworkPlayerName;
                }
                if (string.IsNullOrEmpty(steamName)) steamName = "Player";

                steamName = System.Text.RegularExpressions.Regex.Replace(steamName, "<.*?>", string.Empty).Trim();
                string[] knownTags = { "[DEV]", "[PRO]", "[VIP]", "[ADMIN]", "[STAFF]", "[MOD]", "[GOD]" };
                foreach (var kt in knownTags)
                {
                    if (steamName.StartsWith(kt, StringComparison.OrdinalIgnoreCase))
                    {
                        steamName = steamName.Substring(kt.Length).Trim();
                        break;
                    }
                }
                BasePlayerName = steamName;

                PlayerPrefs.SetString("PlayerName", BasePlayerName);
                PlayerPrefs.Save();

                ApplyNameSpoof(BasePlayerName, me);
            }
            catch { }
        }

        public static void SyncSelectedTag(GameObject me)
        {
            try
            {
                string tag = SelectedTag;
                bool hasTag = (tag != "None" && !string.IsNullOrEmpty(tag));

                // Always clean BasePlayerName of any leftover HTML or duplicate tags
                string cleanBase = BasePlayerName ?? "";
                cleanBase = System.Text.RegularExpressions.Regex.Replace(cleanBase, "<.*?>", string.Empty).Trim();
                string[] knownTags = { "[DEV]", "[PRO]", "[VIP]", "[ADMIN]", "[STAFF]", "[MOD]", "[GOD]" };
                foreach (var kt in knownTags)
                {
                    if (cleanBase.StartsWith(kt, StringComparison.OrdinalIgnoreCase))
                    {
                        cleanBase = cleanBase.Substring(kt.Length).Trim();
                        break;
                    }
                }
                BasePlayerName = cleanBase;

                // Network name MUST remain 100% clean plain text (no <color> markup).
                // Color is applied client-side to TMP_Text so in-game chat and turn banners never display leaked HTML entities.
                string cleanSyncName = hasTag ? $"{tag} {cleanBase}" : cleanBase;
                ApplyNameSpoof(cleanSyncName, me);
            }
            catch { }
        }

        public static string GetFormattedPlayerName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) rawName = BasePlayerName ?? "Inkwell";
            rawName = System.Text.RegularExpressions.Regex.Replace(rawName, "<.*?>", string.Empty).Trim();
            string tag = SelectedTag;
            bool hasTag = (tag != "None" && !string.IsNullOrEmpty(tag));
            bool rgbTag = RgbTagEnabled;
            bool rgbName = RgbNameEnabled;
            float speed = RgbSpeed;
            string staticHex = NameColorHex;
            if (string.IsNullOrEmpty(staticHex)) staticHex = "#FFFFFF";
            bool hasColor = CustomColorEnabled || rgbTag || rgbName || (!string.IsNullOrEmpty(staticHex) && !string.Equals(staticHex, "#FFFFFF", StringComparison.OrdinalIgnoreCase));

            if (!hasColor && !hasTag) return rawName;

            string hex = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB((Time.time * speed) % 1.0f, 1.0f, 1.0f));
            string tagCol = rgbTag ? $"#{hex}" : staticHex;
            string nameCol = rgbName ? $"#{hex}" : staticHex;

            string displayTag = hasTag ? (hasColor ? $"<color={tagCol}>{tag}</color> " : $"{tag} ") : "";
            string displayName = hasColor ? $"<color={nameCol}>{rawName}</color>" : rawName;
            return $"{displayTag}{displayName}";
        }

        private static void ApplyNameSpoof(string name, GameObject me)
        {
            try
            {
                name = System.Text.RegularExpressions.Regex.Replace(name ?? "", "<.*?>", string.Empty).Trim();
                PlayerObjectController localPoc = null;
                if (me != null) localPoc = me.GetComponent<PlayerObjectController>();
                if (localPoc == null)
                {
                    var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                    if (allPocs != null)
                    {
                        ulong mySteamId = 0;
                        try { mySteamId = SteamUser.GetSteamID().m_SteamID; } catch { }
                        foreach (var p in allPocs)
                        {
                            if (p != null && (p.isLocalPlayer || p.isOwned || p.authority || (mySteamId != 0 && p.NetworkPlayerSteamID == mySteamId)))
                            {
                                localPoc = p;
                                break;
                            }
                        }
                    }
                }
                if (localPoc != null)
                {
                    try { localPoc.CmdSetPlayerName(name); } catch { }
                }
                PlayerPrefs.SetString("PlayerName", name);
                PlayerPrefs.Save();
            }
            catch { }
        }

        public static void ForceFoldPlayer(GameObject target, GameObject me)
        {
            if (target == null) return;
            try
            {
                var tp = target.GetComponent<TexasGamePlay>();
                if (tp != null)
                {
                    try { tp.NetworkFolded = true; } catch { }
                    try { tp.Folded = true; } catch { }
                    try { tp.Fold(true); } catch { }
                    try { tp.FoldAllIn(true); } catch { }
                    try { tp.FoldTargetRpc(null, true); } catch { }
                    try { tp.PlayFoldCoawrdSFXRPC(); } catch { }
                }

                var mgr = Manager.Instance;
                var tMgr = (mgr != null && mgr.TexasGame != null) ? mgr.TexasGame : UnityEngine.Object.FindObjectOfType<TexasGamePlayManager>();
                var ps = target.GetComponent<PlayerStats>();
                if (mgr != null && ps != null && mgr.NetworkActivePlayerSlot == ps.Slot)
                {
                    try { tMgr?.GiveTurnTexas(false); } catch { }
                    try { mgr.GiveTurnTexas(); } catch { }
                    try { mgr.GiveTurn(); } catch { }
                }
            }
            catch { }
        }

        public static void ForcePassPlayer(GameObject target, GameObject me)
        {
            if (target == null) return;
            try
            {
                var pp = target.GetComponent<PokerGamePlay>();
                if (pp != null)
                {
                    try { pp.HavePass = true; } catch { }
                    try { pp.NetworkHavePass = true; } catch { }
                    try { pp.Pass(); } catch { }
                }

                var mgr = Manager.Instance;
                var ps = target.GetComponent<PlayerStats>();
                if (mgr != null && ps != null && mgr.NetworkActivePlayerSlot == ps.Slot)
                {
                    try { mgr.GiveTurn(); } catch { }
                    try { mgr.GiveTurnToActiveSlot(); } catch { }
                }
            }
            catch { }
        }

        public static void ForceLiarPlayer(GameObject target)
        {
            if (target == null) return;
            try
            {
                var pp = target.GetComponent<PokerGamePlay>();
                if (pp != null)
                {
                    try { pp.LiarChallange(); } catch { }
                    try { pp.LiarCmd(); } catch { }
                    try { pp.PlayLiarCMD(); } catch { }
                }
            }
            catch { }
        }

        private static void DrawGlobalTab(UiBuilder ui)
        {
            ui.Header("GLOBAL ENGINE OVERRIDES");

            ui.Toggle(ref ShowTopLeftCards, "Top-Left Cards & Hand Overlay (Standard Cards Fix)");
            ui.Toggle(ref BypassCardLimit, "Bypass Card Placing Limit (Up to 5 cards)");
            ui.Toggle(ref AlwaysMyTurn, "Always My Turn (Bypass turn order)");
            ui.Toggle(ref ShowEsp, "ESP Overlay");
            ui.Toggle(ref AntiKick, "Anti-Kick Protection");
            ui.Toggle(ref BypassAuth, "Bypass Mirror Authority Checks");

            ui.Space(4);
            float speed = Time.timeScale;
            if (ui.Slider("Engine Game Speed", ref speed, 0.1f, 3.0f) != Time.timeScale)
            {
                Time.timeScale = speed;
            }
        }

        private static void DrawSettingsTab(UiBuilder ui)
        {
            ui.Header("TACTICAL GUI CONFIGURATION");

            MenuBg = ui.ColorSlider("Window Background", MenuBg);
            ContentBg = ui.ColorSlider("Content Panel", ContentBg);

            ui.Space(6);
            if (ui.Button("Reset GUI Position & Dimensions to Default", -1f, 26f))
            {
                MenuPos = new Vector2(30f, 30f);
                MenuSize = new Vector2(760f, 600f);
            }
            if (ui.Button("Reset Cards HUD Overlay Position (Top-Left)", -1f, 26f))
            {
                Core.CardsHudPos = new Vector2(15f, 15f);
            }
        }

        // ================= HELPER ROUTINES =================

        private static void SyncKillPlayer(GameObject target, GameObject me)
        {
            if (target == null) return;
            try
            {
                var ps = target.GetComponent<PlayerStats>();
                if (ps != null) ps.NetworkDead = true;
            }
            catch { }

            // Authoritative server-side kill commands via local player POC
            try
            {
                var localPoc = CardController.GetLocalPlayerObjectController();
                if (localPoc != null)
                {
                    try { localPoc.SetDeadDeck(target); } catch { }
                    try { localPoc.SetDeadSpin(target); } catch { }
                    try { localPoc.SetHealthData(0, true, target); } catch { }
                }
            }
            catch { }

            try { var dk = target.GetComponent<DeckGameplay>(); if (dk != null) { try { dk.ServerKillPlayer(); } catch { } try { dk.CmdKillPlayer(); } catch { } return; } } catch { }
            try { var cd = target.GetComponent<ChaosDeckGameplay>(); if (cd != null) { cd.CommandBeDead(); return; } } catch { }
            try { var bg = target.GetComponent<BlorfGamePlay>(); if (bg != null) { bg.CommandBeDead(); return; } } catch { }
            try { var pg = target.GetComponent<PokerGamePlay>(); if (pg != null) { pg.CommandBeDead(); try { pg.PlayDeadSfx(); } catch { } return; } } catch { }
            try { var tg = target.GetComponent<TexasGamePlay>(); if (tg != null) { tg.CommandBeDead(); tg.NetworkBullets = 6; try { tg.PlayDeadSfx(); } catch { } return; } } catch { }
            try { var cc = target.GetComponent<CharController>(); if (cc != null) { cc.RpcSnapDeadForMigration(); try { cc.PlayEmoteDeadRPC(); } catch { } return; } } catch { }
        }

        private static void ForceDrinkPlayer(GameObject target)
        {
            if (target == null) return;

            // Vector 1: Dice gameplay network drink count
            try
            {
                var dg = target.GetComponent<DiceGamePlay>();
                if (dg != null)
                {
                    dg.NetworkdrinkCount++;
                    try
                    {
                        var m = Il2CppInterop.Runtime.Il2CppType.Of<DiceGamePlay>().GetMethod("Drink", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                        if (m != null) m.Invoke(dg, null);
                    }
                    catch { }
                }
            }
            catch { }

            // Vector 2: Manager DrinkRpc invocation
            var mgr = Manager.Instance;
            if (mgr != null)
            {
                try
                {
                    var drMethod = Il2CppInterop.Runtime.Il2CppType.Of<Manager>().GetMethod("DrinkRpc", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    if (drMethod != null) drMethod.Invoke(mgr, new Il2CppSystem.Object[] { target });
                }
                catch { }

                try
                {
                    var drCmd = Il2CppInterop.Runtime.Il2CppType.Of<Manager>().GetMethod("DrinkCmd", Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    if (drCmd != null) drCmd.Invoke(mgr, new Il2CppSystem.Object[] { target });
                }
                catch { }
            }

            // Vector 3: Character controller drink trigger
            try
            {
                var cc = target.GetComponent<CharController>();
                if (cc != null)
                {
                    try
                    {
                        var m = Il2CppInterop.Runtime.Il2CppType.Of<CharController>().GetMethod("Drink", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
                        if (m != null) m.Invoke(cc, null);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void ForceHostMigration(GameObject[] players, GameObject me)
        {
            try
            {
                if (Manager.Instance == null || !Manager.Instance.GameStarted)
                {
                    MelonLogger.Warning("[-] ForceHostMigration: Cannot migrate host outside of active gameplay session.");
                    return;
                }

                ulong localSteamId = 0;
                PlayerObjectController localPoc = null;

                // 1. Identify local player controller & SteamID
                if (me != null)
                {
                    localPoc = me.GetComponent<PlayerObjectController>();
                    var ps = me.GetComponent<PlayerStats>();
                    if (ps != null && ps.Player_Id != 0) localSteamId = ps.Player_Id;
                }

                if (localPoc == null)
                {
                    var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                    if (allPocs != null)
                    {
                        foreach (var p in allPocs)
                        {
                            if (p != null && (p.isLocalPlayer || p.authority || p.isOwned))
                            {
                                localPoc = p;
                                break;
                            }
                        }
                        if (localPoc == null && allPocs.Length > 0) localPoc = allPocs[0];
                    }
                }

                if (localSteamId == 0 && localPoc != null && localPoc.NetworkPlayerSteamID != 0)
                {
                    localSteamId = localPoc.NetworkPlayerSteamID;
                }

                if (localSteamId == 0)
                {
                    try { localSteamId = SteamUser.GetSteamID().m_SteamID; } catch { }
                }

                // 2. Mark local player as New Host on PlayerObjectController
                if (localPoc != null)
                {
                    localPoc.isnewHost = true;
                    localPoc.NetworkisnewHost = true;
                    try { localPoc.SetMeAsHost(); } catch { }
                    try { localPoc.StartCoroutine(localPoc.waitforsethost()); } catch { }
                }

                var velvetPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectControllerVelvet>();
                if (velvetPocs != null)
                {
                    foreach (var vp in velvetPocs)
                    {
                        if (vp != null && (vp.isLocalPlayer || vp.authority || vp.isOwned))
                        {
                            vp.isnewHost = true;
                            vp.NetworkisnewHost = true;
                            try { vp.SetMeAsHost(); } catch { }
                        }
                    }
                }

                // 3. Mark HostMigration singleton
                var hm = HostMigration.Instance ?? UnityEngine.Object.FindObjectOfType<HostMigration>();
                if (hm != null)
                {
                    if (localSteamId != 0) hm.NextHostID = localSteamId.ToString();
                    hm.isNewHost = true;
                    hm.MigrationNedded = true;
                    hm.migrationstarted = true;
                    hm.migratedonce = true;
                    try { hm.PrepareDeckMigrationSnapshot(); } catch { }
                }

                // 4. Mark Manager and gameplay managers
                if (Manager.Instance != null)
                {
                    Manager.Instance.MigrationWaiting = true;
                    Manager.Instance.migrated = true;
                }

                var dgm = UnityEngine.Object.FindObjectOfType<DeckGamePlayManager>();
                if (dgm != null) try { dgm.PrepareForMigrationHardReset(); } catch { }

                var cdm = UnityEngine.Object.FindObjectOfType<ChaosDeckGamePlayManager>();
                if (cdm != null) try { cdm.PrepareForMigrationHardReset(); } catch { }

                // 5. Snap dead players for migration state
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p == null) continue;
                        try { var dk = p.GetComponent<DeckGameplay>(); if (dk != null) dk.ServerSnapDeadForMigration(); } catch { }
                        try { var cd = p.GetComponent<ChaosDeckGameplay>(); if (cd != null) cd.ServerSnapDeadForMigration(); } catch { }
                        try { var bg = p.GetComponent<BlorfGamePlay>(); if (bg != null) bg.ServerSnapDeadForMigration(); } catch { }
                    }
                }

                // 6. Eject previous host if detected
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p == null || p == me) continue;
                        var poc = p.GetComponent<PlayerObjectController>();
                        if (poc != null && (poc.isServer || IsHostSession(p)))
                        {
                            try
                            {
                                poc.NetworkKicked = true;
                                poc.Kicked = true;
                                poc.kickeddd = true;
                            }
                            catch { }
                        }
                    }
                }

                // 7. Fire native HostMigration execution
                if (hm != null)
                {
                    try { hm.Migrate(); } catch { }
                    try { hm.StartCoroutine(hm.WaitforStartHost()); } catch { }
                }

                // 8. CustomNetworkManager refresh & fallback
                var nm = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>();
                if (nm != null)
                {
                    try { nm.StartCoroutine(nm.RefreshNextHostAfterMigration()); } catch { }
                    if (!IsHostSession(me))
                    {
                        try { nm.OnClientDisconnect(); } catch { }
                    }
                }

                MelonLogger.Msg($"[+] ForceHostMigration: Local player authority seized (SteamID={localSteamId}).");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[-] ForceHostMigration error: {ex.Message}");
            }
        }

        private static string _directJoinInput = "";
        private static int _selectedJoinMode = 0;
        private static int _filterLobbyMode = -1; // -1 = All

        private static void DrawJoinTab(UiBuilder ui)
        {
            ui.Header("MATCHMAKING & LOBBY BROWSER <color=#55FFAA>[STEAM NETWORKED]</color>");
            ui.Label("<color=#8899AA>Direct join, quick play queue by game mode, live Steam lobby search, and friend match hopping.</color>");

            // Status message
            ui.Box($"Status: <b><color=#00FFB0>{LobbyJoiner.StatusMessage}</color></b>", 28f);

            // SECTION 1: MODE SELECTION & QUICK PLAY
            ui.Header("1. GAME MODE SELECTION & QUICK MATCH");
            ui.Box($"Selected Target Mode: <b><color=#00FFB0>{LobbyJoiner.ModeNames[_selectedJoinMode]}</color></b>", 28f);
            ui.Label("Select target game mode for matchmaking queue or hosting:");

            // Mode buttons (Row 1: modes 0-3)
            ui.BeginRow(28f);
            for (int m = 0; m < 4; m++)
            {
                bool isSel = (_selectedJoinMode == m);
                string col = isSel ? "#00FFB0" : "#FFFFFF";
                string prefix = isSel ? "▶ " : "";
                if (ui.RowButton($"<color={col}>{prefix}{LobbyJoiner.ModeNames[m]}</color>", 175f))
                {
                    _selectedJoinMode = m;
                    LobbyJoiner.SelectedModeIndex = m;
                }
            }
            ui.EndRow();

            // Mode buttons (Row 2: modes 4-7)
            ui.BeginRow(28f);
            for (int m = 4; m < LobbyJoiner.ModeNames.Length; m++)
            {
                bool isSel = (_selectedJoinMode == m);
                string col = isSel ? "#00FFB0" : "#FFFFFF";
                string prefix = isSel ? "▶ " : "";
                if (ui.RowButton($"<color={col}>{prefix}{LobbyJoiner.ModeNames[m]}</color>", 175f))
                {
                    _selectedJoinMode = m;
                    LobbyJoiner.SelectedModeIndex = m;
                }
            }
            ui.EndRow();

            ui.BeginRow(28f);
            if (ui.RowButton("<color=#FFD700><b>[★ QUEUE RANKED MATCH]</b></color>", 220f))
            {
                LobbyJoiner.JoinRankedQueue();
            }
            if (ui.RowButton("<color=#00FFB0><b>[▶ QUICK JOIN MODE]</b></color>", 180f))
            {
                LobbyJoiner.QuickJoinMode(_selectedJoinMode);
            }
            if (ui.RowButton("<color=#FF4444>[■ CANCEL QUEUE]</color>", 130f))
            {
                LobbyJoiner.CancelMatchmaking();
            }
            if (ui.RowButton("<color=#FFDD55>[+ HOST LOBBY]</color>", 140f))
            {
                LobbyJoiner.HostLobbyForMode(_selectedJoinMode);
            }
            ui.EndRow();

            var db = DatabaseManager.instance;
            if (db != null)
            {
                ui.Label($"<color=#AAAAAA>Official Competitive MMR:</color> Deck: <b><color=#00FFB0>{db.DeckMMR5}</color></b> | Spin: <b><color=#00FFB0>{db.SpinMMR5}</color></b> | Dice: <b><color=#00FFB0>{db.DiceMMR5}</color></b> | Poker: <b><color=#00FFB0>{db.PokerMMR5}</color></b>");
            }

            // Active games toggle
            bool prevShow = LobbyJoiner.ShowInGameMatches;
            LobbyJoiner.ShowInGameMatches = ui.Toggle("Bypass & Include In-Progress / Active Games <color=#55FFAA>[INKWELL EXPLOIT]</color>", LobbyJoiner.ShowInGameMatches);
            if (prevShow != LobbyJoiner.ShowInGameMatches)
            {
                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                if (sl != null) sl.ShowInGameLobbies = LobbyJoiner.ShowInGameMatches;
            }

            ui.Space(6f);

            // SECTION 2: DIRECT JOIN BY STEAM LOBBY ID
            ui.Header("2. DIRECT JOIN BY STEAM LOBBY ID / INVITE LINK");
            ui.BeginRow(28f);
            ui.RowLabel("Lobby ID / URL:", 110f);
            _directJoinInput = ui.RowTextField(_directJoinInput, 260f);
            if (ui.RowButton("<color=#55FFAA>PASTE</color>", 65f))
            {
                _directJoinInput = GUIUtility.systemCopyBuffer;
            }
            if (ui.RowButton("<color=#00FFB0><b>[CONNECT / JOIN]</b></color>", 150f))
            {
                LobbyJoiner.JoinLobbyByIdString(_directJoinInput);
            }
            ui.EndRow();

            ui.Space(6f);

            // SECTION 3: STEAM FRIENDS IN LIAR'S BAR
            ui.Header("3. STEAM FRIENDS PLAYING LIAR'S BAR");
            ui.BeginRow(26f);
            if (ui.RowButton("<color=#55FFAA>[↻ REFRESH FRIENDS]</color>", 180f))
            {
                LobbyJoiner.RefreshFriends();
            }
            ui.RowLabel($"<color=#8899AA>Detected Friends: {LobbyJoiner.CachedFriends.Count}</color>", 300f);
            ui.EndRow();

            if (LobbyJoiner.CachedFriends.Count == 0)
            {
                ui.Label("<color=#667788>No Steam friends currently detected playing Liar's Bar.</color>");
            }
            else
            {
                for (int i = 0; i < LobbyJoiner.CachedFriends.Count; i++)
                {
                    var f = LobbyJoiner.CachedFriends[i];
                    ui.BeginRow(24f);
                    ui.RowLabel($"<color=#FFFFFF><b>{f.FriendName}</b></color> <color=#8899AA>({f.FriendId.m_SteamID})</color>", 320f);
                    if (f.HasLobby)
                    {
                        string displayMode = !string.IsNullOrEmpty(f.Mode) ? f.Mode : "In Lobby";
                        ui.RowLabel($"<color=#00FFB0>Mode: {displayMode}</color>", 200f);
                        if (ui.RowButton("<color=#55FFAA>JOIN FRIEND</color>", 120f))
                        {
                            LobbyJoiner.JoinLobby(f.LobbyId);
                        }
                    }
                    else
                    {
                        ui.RowLabel("<color=#FFDD55>In Main Menu / Offline</color>", 200f);
                    }
                    ui.EndRow();
                }
            }

            ui.Space(6f);

            // SECTION 4: LIVE STEAM LOBBY BROWSER
            ui.Header("4. LIVE STEAM LOBBY BROWSER");
            ui.BeginRow(26f);
            if (ui.RowButton("<color=#00FFB0>[↻ QUERY STEAM LOBBIES]</color>", 200f))
            {
                LobbyJoiner.RefreshLobbies();
                LobbyJoiner.UpdateLobbyCache();
            }

            string filterLabel = (_filterLobbyMode == -1) ? "Filter: [ALL MODES]" : $"Filter: [{LobbyJoiner.ModeNames[_filterLobbyMode]}]";
            if (ui.RowButton($"<color=#FFDD55>{filterLabel}</color>", 220f))
            {
                _filterLobbyMode++;
                if (_filterLobbyMode >= LobbyJoiner.ModeNames.Length) _filterLobbyMode = -1;
            }
            ui.RowLabel($"<color=#8899AA>Found: {LobbyJoiner.CachedLobbies.Count} lobbies</color>", 200f);
            ui.EndRow();

            // Table Header
            ui.BeginRow(20f);
            ui.RowLabel("<color=#55FFAA><b>LOBBY NAME</b></color>", 260f);
            ui.RowLabel("<color=#55FFAA><b>GAME MODE</b></color>", 150f);
            ui.RowLabel("<color=#55FFAA><b>PLAYERS</b></color>", 80f);
            ui.RowLabel("<color=#55FFAA><b>STATUS</b></color>", 90f);
            ui.RowLabel("<color=#55FFAA><b>ACTION</b></color>", 90f);
            ui.EndRow();

            LobbyJoiner.PollUpdate();

            if (LobbyJoiner.CachedLobbies.Count == 0)
            {
                ui.Label("<color=#8899AA>No active lobbies returned yet. Click [QUERY STEAM LOBBIES] to scan worldwide.</color>");
            }
            else
            {
                int shown = 0;
                for (int i = 0; i < LobbyJoiner.CachedLobbies.Count; i++)
                {
                    var lob = LobbyJoiner.CachedLobbies[i];
                    string formattedMode = LobbyJoiner.FormatModeName(lob.Mode);
                    if (_filterLobbyMode != -1)
                    {
                        string target = LobbyJoiner.ModeNames[_filterLobbyMode].ToLower().Split(' ')[0];
                        if (!formattedMode.ToLower().Contains(target) && !lob.Mode.ToLower().Contains(target)) continue;
                    }

                    shown++;
                    ui.BeginRow(24f);
                    ui.RowLabel($"<color=#FFFFFF>{lob.Name}</color>", 260f);
                    ui.RowLabel($"<color=#8FA0B8>{formattedMode}</color>", 150f);
                    ui.RowLabel($"<color={(lob.Members >= lob.MaxMembers ? "#FF5555" : "#00FFB0")}>{lob.Members}/{lob.MaxMembers}</color>", 80f);
                    ui.RowLabel(lob.InProgress ? "<color=#FFDD55>IN-GAME</color>" : "<color=#55FFAA>LOBBY</color>", 90f);
                    if (ui.RowButton("<color=#00FFB0>JOIN</color>", 80f))
                    {
                        LobbyJoiner.JoinLobby(lob.Id);
                    }
                    ui.EndRow();
                }

                if (shown == 0)
                {
                    ui.Label($"<color=#8899AA>No lobbies match the current filter [{LobbyJoiner.ModeNames[_filterLobbyMode]}].</color>");
                }
            }
        }

        private static void DrawKeysTab(UiBuilder ui)
        {
            ui.Header("KEYBIND TACTICAL GUIDE & SHORTCUTS");
            ui.Label("<color=#8899AA>Quick reference for all runtime hotkeys available during lobby and in-game sessions:</color>");

            ui.Box(
                "<b><color=#FFFFFF>[INSERT / F1]</color></b>   Toggle Main Tactical Glass Menu\n" +
                "<b><color=#FFFFFF>[P / F2]</color></b>        Dump Live Game State & IL2CPP Symbols to Desktop\n" +
                "<b><color=#FFFFFF>[L / F3]</color></b>        Toggle Transparent Keybind Guide Overlay\n" +
                "<b><color=#55FFAA>[F4]</color></b>           Quick Safe Revolver (Loads 0 Lethal Chambers for Self)\n" +
                "<b><color=#FF5555>[F5]</color></b>           Quick Deadly Revolver (Forces Live Chamber on Trigger)\n" +
                "<b><color=#55FFAA>[F6]</color></b>           Toggle God Mode (Zero Bullets / Vignette Clear / Shield)\n" +
                "<b><color=#FFDD55>[F7]</color></b>           Kill All Opponents (Networked Elimination) [H]\n" +
                "<b><color=#FFDD55>[F8]</color></b>           Force End Game Win (Direct Winner Handover) [H]\n" +
                "<b><color=#FF3344>[F9]</color></b>           Call Liar Anytime (Bypasses Turn & Game Mode Locks)\n" +
                "<b><color=#55FFAA>[F10]</color></b>          Force Host Migration (Seizes Room Authority as Host) [C]",
                220f
            );
        }
    }
}
