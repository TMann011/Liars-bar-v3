using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace LiarsBarMod.Features.Cards
{
    public static class PlayerCardTracker
    {
        private static readonly Dictionary<int, List<int>> _lastPlayedMap = new Dictionary<int, List<int>>();
        private static readonly Dictionary<int, int> _lastPlayedCountMap = new Dictionary<int, int>();
        private static int _prevCardsOnTable = -1;
        private static int _prevRoundCard = -999;

        public static void Reset()
        {
            _lastPlayedMap.Clear();
            _lastPlayedCountMap.Clear();
            _prevCardsOnTable = -1;
            _prevRoundCard = -999;
        }

        public static void Update(Manager mgr, GameObject[] players)
        {
            if (mgr == null || !mgr.GameStarted)
            {
                if (_lastPlayedMap.Count > 0) Reset();
                return;
            }

            try
            {
                // 1. ChaosDeckGamePlayManager (Devil Mode / Chaos Deck)
                var chaosMgr = UnityEngine.Object.FindFirstObjectByType<ChaosDeckGamePlayManager>();
                if (chaosMgr != null && (mgr.useChaosDeckRules || chaosMgr.CardsOnTable > 0 || chaosMgr.RoundCard > 0 || UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>() == null))
                {
                    int currentTable = chaosMgr.CardsOnTable;
                    int currentRoundCard = chaosMgr.RoundCard;

                    if (currentTable == 0 && _prevCardsOnTable > 0)
                    {
                        Reset();
                    }
                    _prevCardsOnTable = currentTable;
                    _prevRoundCard = currentRoundCard;

                    var lastPlayer = chaosMgr.LastBetPlayer;
                    if (lastPlayer != null && chaosMgr.LastRound != null && chaosMgr.LastRound.Count > 0)
                    {
                        var ps = lastPlayer.GetComponent<PlayerStats>();
                        int slot = ps != null ? ps.Slot : -1;
                        if (slot >= 0)
                        {
                            var playedList = new List<int>();
                            for (int i = 0; i < chaosMgr.LastRound.Count; i++)
                            {
                                int val = chaosMgr.LastRound[i];
                                if (val != 0) playedList.Add((val == 5 || val == -1) ? -1 : val);
                            }
                            if (playedList.Count > 0)
                            {
                                _lastPlayedMap[slot] = playedList;
                                _lastPlayedCountMap[slot] = chaosMgr.LastRoundCount > 0 ? chaosMgr.LastRoundCount : playedList.Count;
                            }
                        }
                    }
                    return;
                }

                // 2. DeckGamePlayManager (Standard Cards)
                var deckMgr = UnityEngine.Object.FindFirstObjectByType<DeckGamePlayManager>();
                if (deckMgr != null)
                {
                    int currentTable = deckMgr.CardsOnTable;
                    int currentRoundCard = deckMgr.RoundCard;

                    // New round detected when table resets to 0
                    if (currentTable == 0 && _prevCardsOnTable > 0)
                    {
                        Reset();
                    }
                    _prevCardsOnTable = currentTable;
                    _prevRoundCard = currentRoundCard;

                    var lastPlayer = deckMgr.LastBetPlayer;
                    if (lastPlayer != null && deckMgr.LastRound != null && deckMgr.LastRound.Count > 0)
                    {
                        var ps = lastPlayer.GetComponent<PlayerStats>();
                        int slot = ps != null ? ps.Slot : -1;
                        if (slot >= 0)
                        {
                            var playedList = new List<int>();
                            for (int i = 0; i < deckMgr.LastRound.Count; i++)
                            {
                                int val = deckMgr.LastRound[i];
                                if (val != 0) playedList.Add((val == 5 || val == -1) ? -1 : val);
                            }
                            if (playedList.Count > 0)
                            {
                                _lastPlayedMap[slot] = playedList;
                                _lastPlayedCountMap[slot] = deckMgr.LastRoundCount > 0 ? deckMgr.LastRoundCount : playedList.Count;
                            }
                        }
                    }
                    return;
                }

                // 3. BlorfGamePlayManager
                var blorfMgr = UnityEngine.Object.FindFirstObjectByType<BlorfGamePlayManager>();
                if (blorfMgr != null)
                {
                    int currentTable = blorfMgr.CardsOnTable;
                    if (currentTable == 0 && _prevCardsOnTable > 0)
                    {
                        Reset();
                    }
                    _prevCardsOnTable = currentTable;

                    var lastPlayer = blorfMgr.LastBetPlayer;
                    if (lastPlayer != null && blorfMgr.LastRound != null && blorfMgr.LastRound.Count > 0)
                    {
                        var ps = lastPlayer.GetComponent<PlayerStats>();
                        int slot = ps != null ? ps.Slot : -1;
                        if (slot >= 0)
                        {
                            var playedList = new List<int>();
                            for (int i = 0; i < blorfMgr.LastRound.Count; i++)
                            {
                                int val = blorfMgr.LastRound[i];
                                if (val != 0) playedList.Add((val == 5 || val == -1) ? -1 : val);
                            }
                            if (playedList.Count > 0)
                            {
                                _lastPlayedMap[slot] = playedList;
                                _lastPlayedCountMap[slot] = playedList.Count;
                            }
                        }
                    }
                    return;
                }
            }
            catch { }
        }

        public static List<int> GetLastPlayedCards(int slot)
        {
            if (_lastPlayedMap.TryGetValue(slot, out var list) && list != null)
            {
                return list;
            }
            return new List<int>();
        }

        public static int GetLastPlayedCount(int slot)
        {
            if (_lastPlayedCountMap.TryGetValue(slot, out int count))
            {
                return count;
            }
            return 0;
        }
    }
}
