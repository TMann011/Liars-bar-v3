using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using Il2CppSteamworks;
using LiarsBarMod.Features.Poker;

namespace LiarsBarMod.Features.Cards
{
    public static class CardController
    {
        public static Il2CppSystem.Collections.Generic.List<GameObject> GetCardsForPlayer(GameObject p)
        {
            if (p == null) return null;
            try
            {
                var bg = p.GetComponent<BlorfGamePlay>();
                if (bg != null && bg.Cards != null) return bg.Cards;

                var bmm = p.GetComponent<BlorfGamePlayMatchMaking>();
                if (bmm != null && bmm.Cards != null) return bmm.Cards;

                var cd = p.GetComponent<ChaosDeckGameplay>();
                if (cd != null && cd.Cards != null) return cd.Cards;

                var dk = p.GetComponent<DeckGameplay>();
                if (dk != null && dk.Cards != null) return dk.Cards;

                var cg = p.GetComponent<ChaosGamePlay>();
                if (cg != null && cg.Cards != null) return cg.Cards;

                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null && pg.Cards != null) return pg.Cards;

                var tg = p.GetComponent<TexasGamePlay>();
                if (tg != null && tg.Cards != null)
                {
                    var list = new Il2CppSystem.Collections.Generic.List<GameObject>();
                    for (int i = 0; i < tg.Cards.Count; i++)
                    {
                        var tc = tg.Cards[i];
                        if (tc != null) list.Add(tc.gameObject);
                    }
                    return list;
                }
            }
            catch { }
            return null;
        }

        public static List<int> GetCardTypesForPlayer(GameObject p)
        {
            var list = new List<int>();
            if (p == null) return list;

            try
            {
                // 1. DeckGameplay (Standard Cards)
                var dk = p.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    if (!dk.HaveCards) return list;

                    // Method A: Check active card indexes returned by engine
                    try
                    {
                        var activeIdxs = dk.GetActiveCardIndexes();
                        if (activeIdxs != null && activeIdxs.Count > 0)
                        {
                            for (int i = 0; i < activeIdxs.Count; i++)
                            {
                                int idx = activeIdxs[i];
                                if (dk.cardTypes != null && idx >= 0 && idx < dk.cardTypes.Count)
                                {
                                    int val = dk.cardTypes[idx];
                                    if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                                }
                            }
                            if (list.Count > 0) return list;
                        }
                        else if (activeIdxs != null && activeIdxs.Count == 0 && dk.cardTypes != null && dk.cardTypes.Count > 0)
                        {
                            return list;
                        }
                    }
                    catch { }

                    // Method B: Check visual Cards GameObjects active state in hierarchy
                    if (dk.Cards != null && dk.Cards.Count > 0)
                    {
                        for (int i = 0; i < dk.Cards.Count; i++)
                        {
                            var cObj = dk.Cards[i];
                            if (cObj != null && cObj.activeInHierarchy)
                            {
                                int val = 0;
                                if (dk.cardTypes != null && i < dk.cardTypes.Count) val = dk.cardTypes[i];
                                if (val == 0) val = GetCardType(cObj);
                                if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                            }
                        }
                        return list;
                    }

                    // Method C: Fallback to cardTypes if Cards collection unpopulated
                    if (dk.cardTypes != null && dk.cardTypes.Count > 0)
                    {
                        for (int i = 0; i < dk.cardTypes.Count; i++)
                        {
                            int val = dk.cardTypes[i];
                            if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                        }
                        return list;
                    }
                }

                // 2. ChaosDeckGameplay (Devil Cards)
                var cd = p.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    if (!cd.HaveCards) return list;

                    if (cd.Cards != null && cd.Cards.Count > 0)
                    {
                        for (int i = 0; i < cd.Cards.Count; i++)
                        {
                            var cObj = cd.Cards[i];
                            if (cObj != null && cObj.activeInHierarchy)
                            {
                                int val = 0;
                                if (cd.CardTypes != null && i < cd.CardTypes.Count) val = cd.CardTypes[i];
                                if (val == 0) val = GetCardType(cObj);
                                if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                            }
                        }
                        return list;
                    }

                    if (cd.CardTypes != null && cd.CardTypes.Count > 0)
                    {
                        for (int i = 0; i < cd.CardTypes.Count; i++)
                        {
                            int val = cd.CardTypes[i];
                            if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                        }
                        return list;
                    }
                }

                // 3. BlorfGamePlay / BlorfMatchMaking
                var bg = p.GetComponent<BlorfGamePlay>();
                if (bg != null)
                {
                    if (bg.Cards != null && bg.Cards.Count > 0)
                    {
                        for (int i = 0; i < bg.Cards.Count; i++)
                        {
                            var cObj = bg.Cards[i];
                            if (cObj != null && cObj.activeInHierarchy)
                            {
                                int val = 0;
                                if (bg.CardTypes != null && i < bg.CardTypes.Count) val = bg.CardTypes[i];
                                if (val == 0) val = GetCardType(cObj);
                                if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                            }
                        }
                        return list;
                    }

                    if (bg.CardTypes != null && bg.CardTypes.Count > 0)
                    {
                        for (int i = 0; i < bg.CardTypes.Count; i++)
                        {
                            int val = bg.CardTypes[i];
                            if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                        }
                        return list;
                    }
                }

                var bmm = p.GetComponent<BlorfGamePlayMatchMaking>();
                if (bmm != null)
                {
                    if (bmm.Cards != null && bmm.Cards.Count > 0)
                    {
                        for (int i = 0; i < bmm.Cards.Count; i++)
                        {
                            var cObj = bmm.Cards[i];
                            if (cObj != null && cObj.activeInHierarchy)
                            {
                                int val = 0;
                                if (bmm.CardTypes != null && i < bmm.CardTypes.Count) val = bmm.CardTypes[i];
                                if (val == 0) val = GetCardType(cObj);
                                if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                            }
                        }
                        return list;
                    }

                    if (bmm.CardTypes != null && bmm.CardTypes.Count > 0)
                    {
                        for (int i = 0; i < bmm.CardTypes.Count; i++)
                        {
                            int val = bmm.CardTypes[i];
                            if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                        }
                        return list;
                    }
                }

                // 4. ChaosGamePlay
                var cg = p.GetComponent<ChaosGamePlay>();
                if (cg != null)
                {
                    if (cg.Cards != null && cg.Cards.Count > 0)
                    {
                        for (int i = 0; i < cg.Cards.Count; i++)
                        {
                            var cObj = cg.Cards[i];
                            if (cObj != null && cObj.activeInHierarchy)
                            {
                                int val = 0;
                                if (cg.CardTypes != null && i < cg.CardTypes.Count) val = cg.CardTypes[i];
                                if (val == 0) val = GetCardType(cObj);
                                if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                            }
                        }
                        return list;
                    }

                    if (cg.CardTypes != null && cg.CardTypes.Count > 0)
                    {
                        for (int i = 0; i < cg.CardTypes.Count; i++)
                        {
                            int val = cg.CardTypes[i];
                            if (val != 0) list.Add((val == 5 || val == -1) ? -1 : val);
                        }
                        return list;
                    }
                }

                // 5. PokerGamePlay
                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    if (pg.Cards != null && pg.Cards.Count > 0)
                    {
                        for (int i = 0; i < pg.Cards.Count; i++)
                        {
                            var cObj = pg.Cards[i];
                            if (cObj != null && cObj.activeInHierarchy)
                            {
                                int val = 0;
                                if (pg.CardTypes != null && i < pg.CardTypes.Count) val = pg.CardTypes[i];
                                if (val == 0) val = GetCardType(cObj);
                                if (val != 0) list.Add(val);
                            }
                        }
                        return list;
                    }

                    if (pg.CardTypes != null && pg.CardTypes.Count > 0)
                    {
                        for (int i = 0; i < pg.CardTypes.Count; i++)
                        {
                            int val = pg.CardTypes[i];
                            if (val != 0) list.Add(val);
                        }
                        return list;
                    }
                }

                // 6. TexasGamePlay
                var tg = p.GetComponent<TexasGamePlay>();
                if (tg != null)
                {
                    if (tg.CardTypes != null && tg.CardTypes.Count > 0)
                    {
                        for (int i = 0; i < tg.CardTypes.Count; i++)
                        {
                            int val = tg.CardTypes[i];
                            if (val != 0) list.Add(val);
                        }
                        if (list.Count > 0) return list;
                    }

                    try
                    {
                        var tCards = tg.GetComponentsInChildren<TexasCard>(true);
                        if (tCards != null && tCards.Length > 0)
                        {
                            for (int i = 0; i < tCards.Length; i++)
                            {
                                var tc = tCards[i];
                                if (tc != null && tc.CardNumber >= 1 && tc.CardNumber <= 52)
                                {
                                    list.Add(tc.CardNumber);
                                }
                            }
                            if (list.Count > 0) return list;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 7. Fallback: Visual GameObject Hand Cards Inspection
            try
            {
                var gCards = GetCardsForPlayer(p);
                if (gCards != null && gCards.Count > 0)
                {
                    for (int i = 0; i < gCards.Count; i++)
                    {
                        var cObj = gCards[i];
                        if (cObj != null && cObj.activeInHierarchy)
                        {
                            int ct = GetCardType(cObj);
                            if (ct != 0) list.Add(ct);
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public static void SyncPlayerCardTypesList(GameObject player, int slot, int newType)
        {
            if (player == null || slot < 0) return;
            try
            {
                var dk = player.GetComponent<DeckGameplay>();
                if (dk != null && dk.cardTypes != null && slot < dk.cardTypes.Count)
                {
                    dk.cardTypes[slot] = newType;
                }

                var cd = player.GetComponent<ChaosDeckGameplay>();
                if (cd != null && cd.CardTypes != null && slot < cd.CardTypes.Count)
                {
                    cd.CardTypes[slot] = newType;
                }

                var bg = player.GetComponent<BlorfGamePlay>();
                if (bg != null && bg.CardTypes != null && slot < bg.CardTypes.Count)
                {
                    bg.CardTypes[slot] = newType;
                }

                var bm = player.GetComponent<BlorfGamePlayMatchMaking>();
                if (bm != null && bm.CardTypes != null && slot < bm.CardTypes.Count)
                {
                    bm.CardTypes[slot] = newType;
                }

                var cg = player.GetComponent<ChaosGamePlay>();
                if (cg != null && cg.CardTypes != null && slot < cg.CardTypes.Count)
                {
                    cg.CardTypes[slot] = newType;
                }

                var pg = player.GetComponent<PokerGamePlay>();
                if (pg != null && pg.CardTypes != null && slot < pg.CardTypes.Count)
                {
                    pg.CardTypes[slot] = newType;
                    pg.CardValue = newType;
                    pg.NetworkCardValue = newType;
                    try { pg.SetCardsCmd(); } catch { }
                    try { pg.SetCardsRpc(); } catch { }
                }

                var tg = player.GetComponent<TexasGamePlay>();
                if (tg != null && tg.CardTypes != null && slot < tg.CardTypes.Count)
                {
                    tg.CardTypes[slot] = newType;
                }
            }
            catch { }
        }

        public static void SetOneCardSafe(GameObject player, GameObject cardObj, int type)
        {
            if (cardObj == null) return;

            // Find index in player hand
            int cardSlot = -1;
            if (player != null)
            {
                var playerCards = GetCardsForPlayer(player);
                if (playerCards != null)
                {
                    for (int i = 0; i < playerCards.Count; i++)
                    {
                        if (playerCards[i] == cardObj)
                        {
                            cardSlot = i;
                            break;
                        }
                    }
                }
            }

            // 1. Try standard Card component (Deck / ChaosDeck / Blorf)
            try
            {
                var c = cardObj.GetComponent<Card>();
                if (c != null)
                {
                    c.cardtype = type;
                    c.Devil = (type == -1);

                    try { c.SetCard(); } catch { }
                    try { c.ApplyMeshForCardType(type); } catch { }
                    try { c.ApplyHandMaterial(true); } catch { }
                    try { c.ReleaseFixedVisuals(); } catch { }

                    if (player != null && cardSlot >= 0)
                    {
                        SyncPlayerCardTypesList(player, cardSlot, type);
                    }
                    return;
                }
            }
            catch { }

            // 2. Try PokerCard component (Poker)
            try
            {
                var pc = cardObj.GetComponent<PokerCard>();
                if (pc != null)
                {
                    pc.cardtype = type;
                    try { pc.SetCard(); } catch { }

                    if (player != null && cardSlot >= 0)
                    {
                        SyncPlayerCardTypesList(player, cardSlot, type);
                    }
                    return;
                }
            }
            catch { }

            // 3. Try TexasCard component (Texas Hold'em)
            try
            {
                var tc = cardObj.GetComponent<TexasCard>();
                if (tc != null)
                {
                    tc.CardNumber = type;
                    try { tc.SetCardWithoutMesh(type); } catch { }
                    try { tc.SetMesh(); } catch { }

                    if (player != null)
                    {
                        var tg = player.GetComponent<TexasGamePlay>();
                        if (tg != null && cardSlot >= 0)
                        {
                            Features.Poker.TexasController.ApplyTexasCardChange(tg, cardSlot, type);
                        }
                    }
                    return;
                }
            }
            catch { }
        }

        public static void SetOneCardSafe(GameObject cardObj, int type)
        {
            SetOneCardSafe(null, cardObj, type);
        }

        public static void SetAllCardsForPlayer(GameObject p, int type)
        {
            if (p == null) return;
            try
            {
                // 1. DeckGameplay (Standard Cards)
                var dk = p.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    if (dk.cardTypes != null)
                    {
                        for (int i = 0; i < dk.cardTypes.Count; i++) dk.cardTypes[i] = type;
                    }
                    if (dk.Cards != null)
                    {
                        for (int i = 0; i < dk.Cards.Count; i++)
                        {
                            var cObj = dk.Cards[i];
                            if (cObj != null) SetOneCardSafe(p, cObj, type);
                        }
                    }
                    var allCards = p.GetComponentsInChildren<Card>(true);
                    if (allCards != null)
                    {
                        for (int i = 0; i < allCards.Length; i++)
                        {
                            if (allCards[i] != null) SetOneCardSafe(p, allCards[i].gameObject, type);
                        }
                    }
                    try
                    {
                        if (dk.cardTypes != null)
                        {
                            var arr = new int[dk.cardTypes.Count];
                            for (int i = 0; i < arr.Length; i++) arr[i] = dk.cardTypes[i];
                            dk.SetCardsRpc(arr);
                        }
                    }
                    catch { }
                    return;
                }

                // 2. ChaosDeckGameplay (Devil Cards)
                var cd = p.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    if (cd.CardTypes != null)
                    {
                        for (int i = 0; i < cd.CardTypes.Count; i++) cd.CardTypes[i] = type;
                    }
                    if (cd.Cards != null)
                    {
                        for (int i = 0; i < cd.Cards.Count; i++)
                        {
                            var cObj = cd.Cards[i];
                            if (cObj != null) SetOneCardSafe(p, cObj, type);
                        }
                    }
                    var allCards = p.GetComponentsInChildren<Card>(true);
                    if (allCards != null)
                    {
                        for (int i = 0; i < allCards.Length; i++)
                        {
                            if (allCards[i] != null) SetOneCardSafe(p, allCards[i].gameObject, type);
                        }
                    }
                    try { cd.SetCardsRpc(); } catch { }
                    return;
                }

                // 3. Blorf
                var bg = p.GetComponent<BlorfGamePlay>();
                if (bg != null)
                {
                    if (bg.CardTypes != null)
                    {
                        for (int i = 0; i < bg.CardTypes.Count; i++) bg.CardTypes[i] = type;
                    }
                    if (bg.Cards != null)
                    {
                        for (int i = 0; i < bg.Cards.Count; i++)
                        {
                            var cObj = bg.Cards[i];
                            if (cObj != null) SetOneCardSafe(p, cObj, type);
                        }
                    }
                    var allCards = p.GetComponentsInChildren<Card>(true);
                    if (allCards != null)
                    {
                        for (int i = 0; i < allCards.Length; i++)
                        {
                            if (allCards[i] != null) SetOneCardSafe(p, allCards[i].gameObject, type);
                        }
                    }
                    return;
                }

                // 4. Russian Poker
                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    PokerController.SetPokerCard(pg, 0, type);
                    PokerController.SetPokerCard(pg, 1, type);
                    return;
                }

                // 5. Texas Hold'em
                var tg = p.GetComponent<TexasGamePlay>();
                if (tg != null)
                {
                    TexasController.ApplyTexasCardChange(tg, 0, type);
                    TexasController.ApplyTexasCardChange(tg, 1, type);
                    return;
                }
            }
            catch { }
        }

        public static void SetPlayerCardSlot(GameObject p, int slot, int newType)
        {
            if (p == null || slot < 0) return;
            try
            {
                var dk = p.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    if (dk.cardTypes != null)
                    {
                        while (dk.cardTypes.Count <= slot) dk.cardTypes.Add(newType);
                        dk.cardTypes[slot] = newType;
                    }
                    if (dk.Cards != null && slot < dk.Cards.Count && dk.Cards[slot] != null)
                    {
                        SetOneCardSafe(p, dk.Cards[slot], newType);
                    }
                    var allCards = p.GetComponentsInChildren<Card>(true);
                    if (allCards != null && slot < allCards.Length && allCards[slot] != null)
                    {
                        SetOneCardSafe(p, allCards[slot].gameObject, newType);
                    }
                    try
                    {
                        if (dk.cardTypes != null)
                        {
                            var arr = new int[dk.cardTypes.Count];
                            for (int i = 0; i < arr.Length; i++) arr[i] = dk.cardTypes[i];
                            dk.SetCardsRpc(arr);
                        }
                    }
                    catch { }
                    return;
                }

                var cd = p.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    if (cd.CardTypes != null)
                    {
                        while (cd.CardTypes.Count <= slot) cd.CardTypes.Add(newType);
                        cd.CardTypes[slot] = newType;
                    }
                    if (cd.Cards != null && slot < cd.Cards.Count && cd.Cards[slot] != null)
                    {
                        SetOneCardSafe(p, cd.Cards[slot], newType);
                    }
                    var allCards = p.GetComponentsInChildren<Card>(true);
                    if (allCards != null && slot < allCards.Length && allCards[slot] != null)
                    {
                        SetOneCardSafe(p, allCards[slot].gameObject, newType);
                    }
                    try { cd.SetCardsRpc(); } catch { }
                    return;
                }

                var bg = p.GetComponent<BlorfGamePlay>();
                if (bg != null)
                {
                    if (bg.CardTypes != null)
                    {
                        while (bg.CardTypes.Count <= slot) bg.CardTypes.Add(newType);
                        bg.CardTypes[slot] = newType;
                    }
                    if (bg.Cards != null && slot < bg.Cards.Count && bg.Cards[slot] != null)
                    {
                        SetOneCardSafe(p, bg.Cards[slot], newType);
                    }
                    var allCards = p.GetComponentsInChildren<Card>(true);
                    if (allCards != null && slot < allCards.Length && allCards[slot] != null)
                    {
                        SetOneCardSafe(p, allCards[slot].gameObject, newType);
                    }
                    return;
                }

                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    PokerController.SetPokerCard(pg, slot, newType);
                    return;
                }

                var tg = p.GetComponent<TexasGamePlay>();
                if (tg != null)
                {
                    TexasController.ApplyTexasCardChange(tg, slot, newType);
                    return;
                }
            }
            catch { }
        }

        public static int GetCardType(GameObject cardObj)
        {
            if (cardObj == null) return 0;
            try
            {
                var c = cardObj.GetComponent<Card>();
                if (c != null)
                {
                    if (c.Devil || c.cardtype == -1 || c.cardtype == 5) return -1;
                    return c.cardtype;
                }
            }
            catch { }

            try
            {
                var pc = cardObj.GetComponent<PokerCard>();
                if (pc != null) return pc.cardtype;
            }
            catch { }

            try
            {
                var tc = cardObj.GetComponent<TexasCard>();
                if (tc != null) return tc.CardNumber;
            }
            catch { }

            return 0;
        }

        public static string GetCardName(int type)
        {
            return type == 1 ? "K" : type == 2 ? "Q" : type == 3 ? "A" : type == 4 ? "J" : (type == -1 || type == 5) ? "Devil" : type.ToString();
        }

        public static int GetRevolverBullet(GameObject p)
        {
            if (p == null) return 0;
            try
            {
                var bg = p.GetComponent<BlorfGamePlay>(); if (bg != null) return bg.Networkrevolverbulllet;
                var bmm = p.GetComponent<BlorfGamePlayMatchMaking>(); if (bmm != null) return bmm.Networkrevolverbulllet;
                var cg = p.GetComponent<ChaosGamePlay>(); if (cg != null) return cg.Networkrevolverbulllet;
                var cd = p.GetComponent<ChaosDeckGameplay>(); if (cd != null) return cd.Networkrevolverbulllet;
                var dk = p.GetComponent<DeckGameplay>(); if (dk != null) return dk.NetworkrevolverBullet;
                var tg = p.GetComponent<TexasGamePlay>(); if (tg != null) return tg.NetworkBullets;
                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null && pg.fullbullets != null && pg.fullbullets.Count > 0)
                {
                    for (int i = 0; i < pg.fullbullets.Count; i++)
                    {
                        if (pg.fullbullets[i] != -1) return i + 1;
                    }
                    return 0;
                }
            }
            catch { }
            return 0;
        }

        public static int GetCurrentChamber(GameObject p)
        {
            if (p == null) return 0;
            try
            {
                var bg = p.GetComponent<BlorfGamePlay>(); if (bg != null) return bg.Networkcurrentrevoler;
                var bmm = p.GetComponent<BlorfGamePlayMatchMaking>(); if (bmm != null) return bmm.Networkcurrentrevoler;
                var cg = p.GetComponent<ChaosGamePlay>(); if (cg != null) return cg.Networkcurrentrevoler;
                var cd = p.GetComponent<ChaosDeckGameplay>(); if (cd != null) return cd.Networkcurrentrevoler;
                var dk = p.GetComponent<DeckGameplay>(); if (dk != null) return dk.NetworkcurrentRevolver;
                var tg = p.GetComponent<TexasGamePlay>(); if (tg != null) return tg.NetworkBullets;
                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    return pg.CurrentBullet + 1;
                }
            }
            catch { }
            return 0;
        }

        public static PlayerObjectController GetLocalPlayerObjectController()
        {
            try
            {
                var allPocs = UnityEngine.Object.FindObjectsOfType<PlayerObjectController>();
                if (allPocs != null && allPocs.Length > 0)
                {
                    ulong mySteamId = 0;
                    try { mySteamId = SteamUser.GetSteamID().m_SteamID; } catch { }
                    foreach (var poc in allPocs)
                    {
                        if (poc != null && (poc.isLocalPlayer || poc.isOwned || poc.authority || (mySteamId != 0 && poc.NetworkPlayerSteamID == mySteamId)))
                        {
                            return poc;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public static void SetRevolver(GameObject p, int val)
        {
            if (p == null) return;
            try
            {
                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    LiarsBarMod.Features.Poker.PokerController.SetDeathBullet(pg, val - 1);
                    return;
                }

                var tg = p.GetComponent<TexasGamePlay>();
                if (tg != null)
                {
                    LiarsBarMod.Features.Poker.TexasController.SetBullets(tg, val);
                    return;
                }

                var bg = p.GetComponent<BlorfGamePlay>(); if (bg != null) { bg.Networkrevolverbulllet = val; bg.revolverbulllet = val; }
                var bmm = p.GetComponent<BlorfGamePlayMatchMaking>(); if (bmm != null) { bmm.Networkrevolverbulllet = val; bmm.revolverbulllet = val; }
                var cg = p.GetComponent<ChaosGamePlay>(); if (cg != null) { cg.Networkrevolverbulllet = val; cg.revolverbulllet = val; }
                var cd = p.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    cd.Networkrevolverbulllet = val;
                    cd.revolverbulllet = val;
                    try { cd.setmydata(cd.Networkcurrentrevoler, val); } catch { }
                    try { cd.ServerApplyMigrationRevolverState(cd.Networkcurrentrevoler, val); } catch { }
                }
                var dk = p.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    dk.NetworkrevolverBullet = val;
                    dk.revolverBullet = val;
                }

                // 1. Authoritative Network Broadcast via Local Player POC Command to Server
                try
                {
                    var localPoc = GetLocalPlayerObjectController();
                    if (localPoc != null)
                    {
                        localPoc.SetRevolverMigrationData(p, GetCurrentChamber(p), val);
                    }
                }
                catch { }

                // 2. Direct Target POC Command if client has authority over target
                try
                {
                    var targetPoc = p.GetComponent<PlayerObjectController>();
                    if (targetPoc != null && targetPoc.authority)
                    {
                        targetPoc.SetRevolverMigrationData(p, GetCurrentChamber(p), val);
                    }
                }
                catch { }
            }
            catch { }
        }

        public static void SetCurrentChamber(GameObject p, int val)
        {
            if (p == null) return;
            try
            {
                var pg = p.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    LiarsBarMod.Features.Poker.PokerController.SetCurrentBullet(pg, val - 1);
                    return;
                }

                var tg = p.GetComponent<TexasGamePlay>();
                if (tg != null)
                {
                    LiarsBarMod.Features.Poker.TexasController.SetBullets(tg, val);
                    return;
                }

                var bg = p.GetComponent<BlorfGamePlay>(); if (bg != null) { bg.Networkcurrentrevoler = val; bg.currentrevoler = val; }
                var bmm = p.GetComponent<BlorfGamePlayMatchMaking>(); if (bmm != null) { bmm.Networkcurrentrevoler = val; bmm.currentrevoler = val; }
                var cg = p.GetComponent<ChaosGamePlay>(); if (cg != null) { cg.Networkcurrentrevoler = val; cg.currentrevoler = val; }
                var cd = p.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    cd.Networkcurrentrevoler = val;
                    cd.currentrevoler = val;
                    try { cd.setmydata(val, cd.Networkrevolverbulllet); } catch { }
                    try { cd.ServerApplyMigrationRevolverState(val, cd.Networkrevolverbulllet); } catch { }
                }
                var dk = p.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    dk.NetworkcurrentRevolver = val;
                    dk.currentRevolver = val;
                }

                // 1. Authoritative Network Broadcast via Local Player POC Command to Server
                try
                {
                    var localPoc = GetLocalPlayerObjectController();
                    if (localPoc != null)
                    {
                        localPoc.SetRevolverMigrationData(p, val, GetRevolverBullet(p));
                    }
                }
                catch { }

                // 2. Direct Target POC Command if client has authority over target
                try
                {
                    var targetPoc = p.GetComponent<PlayerObjectController>();
                    if (targetPoc != null && targetPoc.authority)
                    {
                        targetPoc.SetRevolverMigrationData(p, val, GetRevolverBullet(p));
                    }
                }
                catch { }
            }
            catch { }
        }

        public static void SetRevolverDeath(GameObject p)
        {
            if (p == null) return;
            var pg = p.GetComponent<PokerGamePlay>();
            if (pg != null)
            {
                LiarsBarMod.Features.Poker.PokerController.SetNextShotDeadly(pg);
                return;
            }

            var tg = p.GetComponent<TexasGamePlay>();
            if (tg != null)
            {
                LiarsBarMod.Features.Poker.TexasController.SetPlayerShotDeadly(tg);
                return;
            }

            int ch = GetCurrentChamber(p);
            int nextChamber = (ch % 6) + 1;
            SetRevolver(p, nextChamber);
        }

        public static void SetRevolverSafe(GameObject p)
        {
            if (p == null) return;
            var pg = p.GetComponent<PokerGamePlay>();
            if (pg != null)
            {
                LiarsBarMod.Features.Poker.PokerController.SetNextShotSafe(pg);
                return;
            }

            var tg = p.GetComponent<TexasGamePlay>();
            if (tg != null)
            {
                LiarsBarMod.Features.Poker.TexasController.SetPlayerShotSafe(tg);
                return;
            }

            int ch = GetCurrentChamber(p);
            int nextChamber = (ch % 6) + 1;
            int safeChamber = ((nextChamber + 2) % 6) + 1;
            SetRevolver(p, safeChamber);
        }


        public static void EnforceBypassCardLimit(GameObject player)
        {
            if (player == null) return;
            try
            {
                var dk = player.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    // Selection bypass: allow selecting 4th and 5th card with Space
                    if (Input.GetKeyDown(KeyCode.Space) && dk.Cards != null && dk.currentCard >= 0 && dk.currentCard < dk.Cards.Count)
                    {
                        var cObj = dk.Cards[dk.currentCard];
                        if (cObj != null)
                        {
                            var c = cObj.GetComponent<Card>();
                            if (c != null && !c.Selected && dk.selectedCards != null && dk.selectedCards.Count >= 3)
                            {
                                c.Selected = true;
                                if (!dk.selectedCards.Contains(cObj))
                                    dk.selectedCards.Add(cObj);
                            }
                        }
                    }

                    // Crash prevention: If more than 3 cards selected, native ThrowCards() crashes due to 3-slot throwProps array.
                    // Intercept Enter/Return and send CmdThrowCards directly.
                    if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && dk.selectedCards != null && dk.selectedCards.Count > 3)
                    {
                        var throwList = new Il2CppSystem.Collections.Generic.List<int>();
                        for (int i = 0; i < dk.selectedCards.Count; i++)
                        {
                            var sc = dk.selectedCards[i];
                            if (sc != null)
                            {
                                var cardComp = sc.GetComponent<Card>();
                                int val = cardComp != null ? cardComp.cardtype : 1;
                                throwList.Add(val);
                                try { cardComp.Selected = false; } catch { }
                                try { sc.SetActive(false); } catch { }
                            }
                        }
                        dk.selectedCards.Clear();
                        try { dk.CmdThrowCards(throwList, true); } catch { }
                        try { dk.ServerThrowCards(throwList, true); } catch { }
                    }
                    return;
                }

                var cd = player.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    // Selection bypass: allow selecting 4th and 5th card with Space
                    if (Input.GetKeyDown(KeyCode.Space) && cd.Cards != null && cd.currentcard >= 0 && cd.currentcard < cd.Cards.Count)
                    {
                        var cObj = cd.Cards[cd.currentcard];
                        if (cObj != null)
                        {
                            var c = cObj.GetComponent<Card>();
                            if (c != null && !c.Selected && cd.selectedCards != null && cd.selectedCards.Count >= 3)
                            {
                                c.Selected = true;
                                if (!cd.selectedCards.Contains(cObj))
                                    cd.selectedCards.Add(cObj);
                            }
                        }
                    }

                    // Crash prevention for ChaosDeck
                    if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && cd.selectedCards != null && cd.selectedCards.Count > 3)
                    {
                        var throwList = new Il2CppSystem.Collections.Generic.List<int>();
                        for (int i = 0; i < cd.selectedCards.Count; i++)
                        {
                            var sc = cd.selectedCards[i];
                            if (sc != null)
                            {
                                var cardComp = sc.GetComponent<Card>();
                                int val = cardComp != null ? cardComp.cardtype : 1;
                                throwList.Add(val);
                                try { cardComp.Selected = false; } catch { }
                                try { sc.SetActive(false); } catch { }
                            }
                        }
                        cd.selectedCards.Clear();
                        try { cd.CmdThrowCards(throwList, true); } catch { }
                        try { cd.ServerThrowCards(throwList, true); } catch { }
                    }
                    return;
                }
            }
            catch { }
        }

        public static void RestoreDefaultCardLimit(GameObject player)
        {
            // Do not call static setters on DeckGameplay / ChaosDeckGameplay as they produce AccessViolationException in IL2CPP
        }

        public static void ThrowAllHandCards(GameObject player)
        {
            if (player == null) return;
            try
            {
                var dk = player.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    if (dk.cardTypes != null && dk.cardTypes.Count > 0)
                    {
                        var throwList = new Il2CppSystem.Collections.Generic.List<int>();
                        for (int i = 0; i < dk.cardTypes.Count; i++)
                        {
                            int val = dk.cardTypes[i];
                            if (val != 0) throwList.Add(val);
                        }
                        if (throwList.Count > 0)
                        {
                            dk.CmdThrowCards(throwList, true);
                            return;
                        }
                    }
                    dk.ThrowCards();
                    return;
                }

                var cd = player.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    if (cd.CardTypes != null && cd.CardTypes.Count > 0)
                    {
                        var throwList = new Il2CppSystem.Collections.Generic.List<int>();
                        for (int i = 0; i < cd.CardTypes.Count; i++)
                        {
                            int val = cd.CardTypes[i];
                            if (val != 0) throwList.Add(val);
                        }
                        if (throwList.Count > 0)
                        {
                            cd.CmdThrowCards(throwList, true);
                            return;
                        }
                    }
                    cd.ThrowCards();
                    return;
                }

                var bg = player.GetComponent<BlorfGamePlay>();
                if (bg != null)
                {
                    if (bg.CardTypes != null && bg.CardTypes.Count > 0)
                    {
                        var throwList = new Il2CppSystem.Collections.Generic.List<int>();
                        for (int i = 0; i < bg.CardTypes.Count; i++)
                        {
                            int val = bg.CardTypes[i];
                            if (val != 0) throwList.Add(val);
                        }
                        if (throwList.Count > 0)
                        {
                            bg.ThrowCardsCmd(throwList);
                            return;
                        }
                    }
                    bg.ThrowCards();
                    return;
                }
            }
            catch { }
        }

        public static void CallLiarAnytime(GameObject me)
        {
            if (me == null) return;
            try
            {
                var dk = me.GetComponent<DeckGameplay>();
                if (dk != null)
                {
                    try { dk.CmdCallLiar(); } catch { }
                    try { dk.RequestCallLiar(); } catch { }
                    return;
                }

                var cd = me.GetComponent<ChaosDeckGameplay>();
                if (cd != null)
                {
                    try { cd.CmdCallLiar(); } catch { }
                    try { cd.RequestCallLiar(); } catch { }
                    return;
                }

                var bg = me.GetComponent<BlorfGamePlay>();
                if (bg != null)
                {
                    try { bg.PlayLiarCMD(); } catch { }
                    try { bg.CallLiar(); } catch { }
                    return;
                }

                var dg = me.GetComponent<DiceGamePlay>();
                if (dg != null)
                {
                    try { dg.PlayLiarCMD(); } catch { }
                    try { dg.CallLier(); } catch { }
                    return;
                }

                var pg = me.GetComponent<PokerGamePlay>();
                if (pg != null)
                {
                    try { pg.LiarCmd(); } catch { }
                    try { pg.LiarChallange(); } catch { }
                    try { pg.PlayLiarCMD(); } catch { }
                    return;
                }

                var sg = me.GetComponent<SpinGamePlay>();
                if (sg != null)
                {
                    try { sg.PlayLiarCMD(); } catch { }
                    try { sg.CallLiar(); } catch { }
                    return;
                }
            }
            catch { }
        }
    }
}
