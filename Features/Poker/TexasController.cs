using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace LiarsBarMod.Features.Poker
{
    public static class TexasController
    {
        // =========================================================================
        // REVERSE-ENGINEERED 52-CARD STANDARD DECK SPECIFICATION (GameAssembly.dll)
        // =========================================================================
        // CardNumber ranges from 1 to 52.
        // Mesh resolution in TexasCard.SetCard: index = CardNumber - 1 (0 to 51).
        //
        // Rank Mapping (GetCardValue / TexasCard.GetCardValue):
        //   Cards  1..4  -> 2
        //   Cards  5..8  -> 3
        //   Cards  9..12 -> 4
        //   Cards 13..16 -> 5
        //   Cards 17..20 -> 6
        //   Cards 21..24 -> 7
        //   Cards 25..28 -> 8
        //   Cards 29..32 -> 9
        //   Cards 33..36 -> 10
        //   Cards 37..40 -> 11 (Jack)
        //   Cards 41..44 -> 12 (Queen)
        //   Cards 45..48 -> 13 (King)
        //   Cards 49..52 -> 14 (Ace)
        //
        // Suit Mapping (ReturnCardType / CardNumber % 4):
        //   rem == 1 -> Type 0: Clubs (♣)
        //   rem == 2 -> Type 1: Diamonds (♦)
        //   rem == 3 -> Type 2: Hearts (♥)
        //   rem == 0 -> Type 3: Spades (♠)
        // =========================================================================

        public static readonly string[] RankNames = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };
        public static readonly string[] ShortRankNames = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        public static readonly string[] SuitNames = { "Clubs", "Diamonds", "Hearts", "Spades" };
        public static readonly string[] SuitSymbols = { "♣", "♦", "♥", "♠" };

        public static int GetCardValue(int cardId)
        {
            if (cardId < 1 || cardId > 52) return 0;
            return ((cardId - 1) / 4) + 2;
        }

        public static int GetCardSuit(int cardId)
        {
            if (cardId < 1 || cardId > 52) return -1;
            int rem = cardId % 4;
            return rem switch
            {
                1 => 0, // Clubs (♣)
                2 => 1, // Diamonds (♦)
                3 => 2, // Hearts (♥)
                0 => 3, // Spades (♠)
                _ => -1
            };
        }

        public static int GetCardId(int rank, int suit)
        {
            if (rank < 2 || rank > 14 || suit < 0 || suit > 3) return 1;
            int g = rank - 2;
            return suit switch
            {
                0 => g * 4 + 1, // Clubs
                1 => g * 4 + 2, // Diamonds
                2 => g * 4 + 3, // Hearts
                3 => g * 4 + 4, // Spades
                _ => 1
            };
        }

        public static string GetCardName(int cardId)
        {
            if (cardId < 1 || cardId > 52) return $"#{cardId}";
            int val = GetCardValue(cardId);
            int suit = GetCardSuit(cardId);
            if (val < 2 || val > 14 || suit < 0 || suit > 3) return $"#{cardId}";
            return $"{RankNames[val - 2]}{SuitSymbols[suit]}";
        }

        public static List<int> GetTexasCardsForPlayer(TexasGamePlay tg)
        {
            var res = new List<int>();
            if (tg == null) return res;

            try
            {
                // Primary: Internal CardTypes list (Network/RAM hand representation)
                if (tg.CardTypes != null && tg.CardTypes.Count > 0)
                {
                    for (int i = 0; i < tg.CardTypes.Count; i++)
                    {
                        int id = tg.CardTypes[i];
                        if (id >= 1 && id <= 52) res.Add(id);
                    }
                    if (res.Count > 0) return res;
                }

                // Fallback: 3D table card GameObjects (only accept valid dealt cards, ignore 0 placeholder)
                if (tg.Cards != null && tg.Cards.Count > 0)
                {
                    for (int i = 0; i < tg.Cards.Count; i++)
                    {
                        var c = tg.Cards[i];
                        if (c != null && c.CardNumber >= 1 && c.CardNumber <= 52)
                        {
                            res.Add(c.CardNumber);
                        }
                    }
                }
            }
            catch { }

            return res;
        }

        public static void ApplyTexasCardChange(TexasGamePlay tg, int slot, int newCardId)
        {
            if (tg == null || slot < 0 || slot > 1 || newCardId < 1 || newCardId > 52) return;

            try
            {
                // 1. Update internal CardTypes list (Network/RAM hand representation)
                if (tg.CardTypes != null)
                {
                    while (tg.CardTypes.Count <= slot) tg.CardTypes.Add(newCardId);
                    tg.CardTypes[slot] = newCardId;
                }

                int cardVal = GetCardValue(newCardId);

                // 2. Update physical 3D card GameObject & mesh via TexasCard component
                if (tg.Cards != null && slot < tg.Cards.Count)
                {
                    var tc = tg.Cards[slot];
                    if (tc != null)
                    {
                        tc.CardNumber = newCardId;
                        tc.CardValue = cardVal;
                        try { tc.SetCard(newCardId); } catch { }
                        try { tc.SetCardWithoutMesh(newCardId); } catch { }
                        try { tc.SetMesh(); } catch { }
                    }
                }

                // Also sweep all TexasCard components in tg hierarchy and CardsParent
                try
                {
                    var allTexasCards = tg.GetComponentsInChildren<TexasCard>(true);
                    if (allTexasCards != null && slot < allTexasCards.Length && allTexasCards[slot] != null)
                    {
                        allTexasCards[slot].CardNumber = newCardId;
                        allTexasCards[slot].CardValue = cardVal;
                        try { allTexasCards[slot].SetCard(newCardId); } catch { }
                        try { allTexasCards[slot].SetCardWithoutMesh(newCardId); } catch { }
                        try { allTexasCards[slot].SetMesh(); } catch { }
                    }
                }
                catch { }

                try
                {
                    if (tg.CardsParent != null)
                    {
                        var parentCards = tg.CardsParent.GetComponentsInChildren<TexasCard>(true);
                        if (parentCards != null && slot < parentCards.Length && parentCards[slot] != null)
                        {
                            parentCards[slot].CardNumber = newCardId;
                            parentCards[slot].CardValue = cardVal;
                            try { parentCards[slot].SetCard(newCardId); } catch { }
                            try { parentCards[slot].SetCardWithoutMesh(newCardId); } catch { }
                            try { parentCards[slot].SetMesh(); } catch { }
                        }
                    }
                }
                catch { }

                // 3. Mirror networking & sync: Broadcast hand reveal & sync state
                if (tg.CardTypes != null && tg.CardTypes.Count >= 2)
                {
                    int c0 = tg.CardTypes[0];
                    int c1 = tg.CardTypes[1];
                    try { tg.SetRevealCardsCMD(c0, c1); } catch { }
                    try { tg.SetRevealCards(c0, c1); } catch { }
                    try { tg.RandomCards(c0, c1); } catch { }
                }

                try { tg.SetCardsCmd(); } catch { }
                try { tg.SetSwitch(slot, newCardId); } catch { }
            }
            catch { }
        }

        public static void SetTexasHand(TexasGamePlay tg, int card0, int card1)
        {
            ApplyTexasCardChange(tg, 0, card0);
            ApplyTexasCardChange(tg, 1, card1);
        }

        // =========================================================================
        // FULL 52-CARD TEXAS HOLD'EM HAND EVALUATOR (7 CARDS / 5 CARDS)
        // =========================================================================

        public struct HandScore
        {
            public int Category; // 9=RoyalFlush, 8=StraightFlush, 7=FourOfAKind, 6=FullHouse, 5=Flush, 4=Straight, 3=ThreeOfAKind, 2=TwoPair, 1=OnePair, 0=HighCard
            public int PrimaryRank;
            public int SecondaryRank;
            public int Kickers;
            public string Description;

            public long Value => (long)Category * 10000000000L + (long)PrimaryRank * 100000000L + (long)SecondaryRank * 1000000L + (long)Kickers;
        }

        public static HandScore EvaluateHand(List<int> cardIds)
        {
            var res = new HandScore { Category = 0, Description = "High Card" };
            if (cardIds == null || cardIds.Count == 0) return res;

            var validCards = new List<int>();
            for (int i = 0; i < cardIds.Count; i++)
            {
                int id = cardIds[i];
                if (id >= 1 && id <= 52) validCards.Add(id);
            }
            if (validCards.Count == 0) return res;

            int[] rankCounts = new int[15]; // index 2..14
            var suitRanks = new List<int>[4];
            for (int s = 0; s < 4; s++) suitRanks[s] = new List<int>();

            for (int i = 0; i < validCards.Count; i++)
            {
                int id = validCards[i];
                int val = GetCardValue(id);
                int suit = GetCardSuit(id);
                if (val >= 2 && val <= 14)
                {
                    rankCounts[val]++;
                    if (suit >= 0 && suit <= 3 && !suitRanks[suit].Contains(val))
                    {
                        suitRanks[suit].Add(val);
                    }
                }
            }

            // 1. Royal Flush & Straight Flush check
            for (int s = 0; s < 4; s++)
            {
                if (suitRanks[s].Count >= 5)
                {
                    suitRanks[s].Sort();
                    suitRanks[s].Reverse(); // Descending

                    var sr = suitRanks[s];
                    // Check standard straight in suit
                    for (int top = 14; top >= 6; top--)
                    {
                        if (sr.Contains(top) && sr.Contains(top - 1) && sr.Contains(top - 2) && sr.Contains(top - 3) && sr.Contains(top - 4))
                        {
                            if (top == 14)
                            {
                                return new HandScore
                                {
                                    Category = 9,
                                    PrimaryRank = 14,
                                    Description = $"★ Royal Flush ({SuitNames[s]})"
                                };
                            }
                            return new HandScore
                            {
                                Category = 8,
                                PrimaryRank = top,
                                Description = $"Straight Flush ({ShortRankNames[top - 2]} High, {SuitNames[s]})"
                            };
                        }
                    }
                    // Wheel straight flush (A-2-3-4-5)
                    if (sr.Contains(14) && sr.Contains(2) && sr.Contains(3) && sr.Contains(4) && sr.Contains(5))
                    {
                        return new HandScore
                        {
                            Category = 8,
                            PrimaryRank = 5,
                            Description = $"Straight Flush (5 High Wheel, {SuitNames[s]})"
                        };
                    }
                }
            }

            // 2. Four of a Kind
            for (int r = 14; r >= 2; r--)
            {
                if (rankCounts[r] >= 4)
                {
                    int kicker = 0;
                    for (int k = 14; k >= 2; k--)
                    {
                        if (k != r && rankCounts[k] > 0) { kicker = k; break; }
                    }
                    return new HandScore
                    {
                        Category = 7,
                        PrimaryRank = r,
                        SecondaryRank = kicker,
                        Description = $"Four of a Kind ({RankNames[r - 2]}s)"
                    };
                }
            }

            // 3. Full House (Three of a Kind + Pair)
            int threeRank = -1;
            int pairRank = -1;
            for (int r = 14; r >= 2; r--)
            {
                if (rankCounts[r] >= 3 && threeRank == -1) threeRank = r;
            }
            if (threeRank != -1)
            {
                for (int r = 14; r >= 2; r--)
                {
                    if (r != threeRank && rankCounts[r] >= 2) { pairRank = r; break; }
                }
                if (pairRank != -1)
                {
                    return new HandScore
                    {
                        Category = 6,
                        PrimaryRank = threeRank,
                        SecondaryRank = pairRank,
                        Description = $"Full House ({RankNames[threeRank - 2]}s full of {RankNames[pairRank - 2]}s)"
                    };
                }
            }

            // 4. Flush (5 cards of the same suit)
            for (int s = 0; s < 4; s++)
            {
                if (suitRanks[s].Count >= 5)
                {
                    int topR = suitRanks[s][0];
                    return new HandScore
                    {
                        Category = 5,
                        PrimaryRank = topR,
                        Description = $"Flush ({SuitNames[s]}, {ShortRankNames[topR - 2]} High)"
                    };
                }
            }

            // 5. Straight (5 consecutive ranks)
            for (int top = 14; top >= 6; top--)
            {
                if (rankCounts[top] > 0 && rankCounts[top - 1] > 0 && rankCounts[top - 2] > 0 && rankCounts[top - 3] > 0 && rankCounts[top - 4] > 0)
                {
                    return new HandScore
                    {
                        Category = 4,
                        PrimaryRank = top,
                        Description = $"Straight ({ShortRankNames[top - 6]} to {ShortRankNames[top - 2]})"
                    };
                }
            }
            // Wheel straight (A-2-3-4-5)
            if (rankCounts[14] > 0 && rankCounts[2] > 0 && rankCounts[3] > 0 && rankCounts[4] > 0 && rankCounts[5] > 0)
            {
                return new HandScore
                {
                    Category = 4,
                    PrimaryRank = 5,
                    Description = "Straight (Ace to 5 Wheel)"
                };
            }

            // 6. Three of a Kind
            if (threeRank != -1)
            {
                int k1 = 0, k2 = 0;
                for (int k = 14; k >= 2; k--)
                {
                    if (k != threeRank && rankCounts[k] > 0)
                    {
                        if (k1 == 0) k1 = k;
                        else if (k2 == 0) { k2 = k; break; }
                    }
                }
                return new HandScore
                {
                    Category = 3,
                    PrimaryRank = threeRank,
                    SecondaryRank = k1,
                    Kickers = k2,
                    Description = $"Three of a Kind ({RankNames[threeRank - 2]}s)"
                };
            }

            // 7. Two Pair
            int firstPair = -1;
            int secondPair = -1;
            for (int r = 14; r >= 2; r--)
            {
                if (rankCounts[r] >= 2)
                {
                    if (firstPair == -1) firstPair = r;
                    else if (secondPair == -1) { secondPair = r; break; }
                }
            }
            if (firstPair != -1 && secondPair != -1)
            {
                int kicker = 0;
                for (int k = 14; k >= 2; k--)
                {
                    if (k != firstPair && k != secondPair && rankCounts[k] > 0) { kicker = k; break; }
                }
                return new HandScore
                {
                    Category = 2,
                    PrimaryRank = firstPair,
                    SecondaryRank = secondPair,
                    Kickers = kicker,
                    Description = $"Two Pair ({RankNames[firstPair - 2]}s & {RankNames[secondPair - 2]}s)"
                };
            }

            // 8. One Pair
            if (firstPair != -1)
            {
                int k1 = 0;
                for (int k = 14; k >= 2; k--)
                {
                    if (k != firstPair && rankCounts[k] > 0) { k1 = k; break; }
                }
                return new HandScore
                {
                    Category = 1,
                    PrimaryRank = firstPair,
                    SecondaryRank = k1,
                    Description = $"One Pair of {RankNames[firstPair - 2]}s"
                };
            }

            // 9. High Card
            for (int r = 14; r >= 2; r--)
            {
                if (rankCounts[r] > 0)
                {
                    return new HandScore
                    {
                        Category = 0,
                        PrimaryRank = r,
                        Description = $"High Card {ShortRankNames[r - 2]}"
                    };
                }
            }

            return res;
        }

        public static string EvaluateTexasBestRank(TexasGamePlay tg, TexasGamePlayManager tm)
        {
            if (tg == null) return "No Player";

            var pool = new List<int>();
            if (tm != null && tm.OpenTableCards != null)
            {
                for (int i = 0; i < tm.OpenTableCards.Count; i++)
                {
                    int c = tm.OpenTableCards[i];
                    if (c >= 1 && c <= 52) pool.Add(c);
                }
            }

            var hand = GetTexasCardsForPlayer(tg);
            for (int i = 0; i < hand.Count; i++) pool.Add(hand[i]);

            if (pool.Count == 0) return "<color=#667788>[Waiting for Deal]</color>";

            var score = EvaluateHand(pool);
            return score.Description;
        }

        public static string ApplyTexasBestPossibleHand(TexasGamePlay tg, TexasGamePlayManager tm)
        {
            if (tg == null) return "No Player";

            var tableCards = new List<int>();
            var used = new HashSet<int>();

            if (tm != null && tm.OpenTableCards != null)
            {
                for (int i = 0; i < tm.OpenTableCards.Count; i++)
                {
                    int c = tm.OpenTableCards[i];
                    if (c >= 1 && c <= 52)
                    {
                        tableCards.Add(c);
                        used.Add(c);
                    }
                }
            }

            // Pre-flop fallback: Pocket Aces (A♠=52, A♥=51)
            if (tableCards.Count == 0)
            {
                int c0 = GetCardId(14, 3); // A♠ (52)
                int c1 = GetCardId(14, 2); // A♥ (51)
                SetTexasHand(tg, c0, c1);
                return "Pocket Aces [A♠, A♥]";
            }

            // Community cards present (Flop / Turn / River):
            // Brute-force optimal 2 hole cards from remaining 52-card deck
            long bestVal = -1;
            int bestC0 = -1;
            int bestC1 = -1;
            string bestDesc = "";

            for (int c0 = 1; c0 <= 52; c0++)
            {
                if (used.Contains(c0)) continue;
                for (int c1 = c0 + 1; c1 <= 52; c1++)
                {
                    if (used.Contains(c1)) continue;

                    var candidate = new List<int>(tableCards) { c0, c1 };
                    var score = EvaluateHand(candidate);

                    if (score.Value > bestVal)
                    {
                        bestVal = score.Value;
                        bestC0 = c0;
                        bestC1 = c1;
                        bestDesc = score.Description;
                    }
                }
            }

            if (bestC0 != -1 && bestC1 != -1)
            {
                SetTexasHand(tg, bestC0, bestC1);
                return $"{bestDesc} [{GetCardName(bestC0)}, {GetCardName(bestC1)}]";
            }

            return "Optimal Hand Applied";
        }

        // =========================================================================
        // REVOLVER & RUSSIAN ROULETTE BULLET MANIPULATION (TexasGamePlay)
        // =========================================================================

        public static string GetTexasRadar(TexasGamePlay tg)
        {
            if (tg == null) return "[ ○ ○ ○ ○ ○ ○ ]";
            int bullets = Mathf.Clamp(tg.NetworkBullets, 0, 6);
            string radar = "[ ";
            for (int i = 0; i < 6; i++)
            {
                if (i < bullets)
                    radar += "<color=#FF4444>●</color> ";
                else
                    radar += "<color=#888888>○</color> ";
            }
            radar += "]";
            return radar;
        }

        public static void SetPlayerShotDeadly(TexasGamePlay tg)
        {
            if (tg == null) return;
            try
            {
                tg._revolverWillDieLocal = true;
                tg._revolverGodSaveLocal = false;
                tg._revolverOutcomeReady = true;
                try { tg.Network_revolverOutcomeReady = true; } catch { }
                int cur = Mathf.Max(1, tg.NetworkBullets);
                tg.NetworkBullets = cur;
                tg.Bullets = cur;
            }
            catch { }
        }

        public static void SetPlayerShotSafe(TexasGamePlay tg)
        {
            if (tg == null) return;
            try
            {
                tg._revolverWillDieLocal = false;
                tg._revolverGodSaveLocal = true;
                tg._revolverOutcomeReady = true;
                try { tg.Network_revolverOutcomeReady = true; } catch { }
                tg.NetworkBullets = 0;
                tg.Bullets = 0;
            }
            catch { }
        }

        public static void SetBullets(TexasGamePlay tg, int count)
        {
            if (tg == null) return;
            try
            {
                count = Mathf.Clamp(count, 0, 6);
                tg.NetworkBullets = count;
                tg.Bullets = count;
                if (count == 0)
                {
                    tg._revolverGodSaveLocal = true;
                    tg._revolverWillDieLocal = false;
                }
                else if (count >= 6)
                {
                    tg._revolverWillDieLocal = true;
                    tg._revolverGodSaveLocal = false;
                }
                else
                {
                    tg._revolverWillDieLocal = false;
                    tg._revolverGodSaveLocal = false;
                }
            }
            catch { }
        }

        public static void AddBullets(TexasGamePlay tg, int delta)
        {
            if (tg == null) return;
            SetBullets(tg, tg.NetworkBullets + delta);
        }

        public static void ResetBullets(TexasGamePlay tg)
        {
            if (tg == null) return;
            try
            {
                tg.BulletReset();
                tg.Bullets = 0;
                tg.NetworkBullets = 0;
                tg._revolverGodSaveLocal = true;
                tg._revolverWillDieLocal = false;
            }
            catch { }
        }
    }
}
