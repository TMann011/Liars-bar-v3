using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using LiarsBarMod.Features.Cards;

namespace LiarsBarMod.Features.Poker
{
    public static class PokerController
    {
        // Card type cycle order for up/down arrows
        public static readonly int[] CardTypeCycle = { 1, 2, 3, 4, -1 }; // K, Q, A, J, Devil

        public enum ShotOutcomeOverride
        {
            Normal = 0,
            MustDie = 1,
            MustSurvive = 2
        }

        // Persistent per-player shot overrides (Key = PokerGamePlay InstanceID)
        private static readonly Dictionary<int, ShotOutcomeOverride> _shotOverrides = new Dictionary<int, ShotOutcomeOverride>();
        private static readonly Dictionary<int, bool> _lastTakingAim = new Dictionary<int, bool>();

        public static ShotOutcomeOverride GetShotOverride(PokerGamePlay pg)
        {
            if (pg == null) return ShotOutcomeOverride.Normal;
            int id = pg.GetInstanceID();
            return _shotOverrides.TryGetValue(id, out var val) ? val : ShotOutcomeOverride.Normal;
        }

        public static void SetShotOverride(PokerGamePlay pg, ShotOutcomeOverride outcome)
        {
            if (pg == null) return;
            int id = pg.GetInstanceID();
            _shotOverrides[id] = outcome;

            if (outcome == ShotOutcomeOverride.MustDie)
            {
                SetNextShotDeadly(pg);
            }
            else if (outcome == ShotOutcomeOverride.MustSurvive)
            {
                SetNextShotSafe(pg);
            }
        }

        public static void ClearShotOverride(PokerGamePlay pg)
        {
            if (pg == null) return;
            int id = pg.GetInstanceID();
            _shotOverrides.Remove(id);
        }

        public static List<int> GetCardsForPoker(PokerGamePlay pg)
        {
            var res = new List<int>();
            if (pg == null) return res;

            try
            {
                if (pg.CardTypes != null && pg.CardTypes.Count > 0)
                {
                    for (int i = 0; i < pg.CardTypes.Count; i++) res.Add(pg.CardTypes[i]);
                    return res;
                }

                if (pg.Cards != null && pg.Cards.Count > 0)
                {
                    for (int i = 0; i < pg.Cards.Count; i++)
                    {
                        var c = pg.Cards[i];
                        if (c != null)
                        {
                            var pc = c.GetComponent<PokerCard>();
                            if (pc != null) res.Add(pc.cardtype);
                        }
                    }
                }
            }
            catch { }

            return res;
        }

        /// <summary>
        /// Sets a single poker card slot. Updates local card object, all PokerCard components,
        /// and CardTypes SyncList, then broadcasts authoritative RandomCards/SetCardsCmd.
        /// </summary>
        public static void SetPokerCard(PokerGamePlay pg, int slot, int type)
        {
            if (pg == null || slot < 0 || slot > 1) return;

            try
            {
                // Update the CardTypes SyncList entry
                if (pg.CardTypes != null)
                {
                    while (pg.CardTypes.Count <= slot) pg.CardTypes.Add(type);
                    pg.CardTypes[slot] = type;
                }

                // Update the local PokerCard component + visual mesh
                if (pg.Cards != null && slot < pg.Cards.Count)
                {
                    var c = pg.Cards[slot];
                    if (c != null)
                    {
                        var pc = c.GetComponent<PokerCard>();
                        if (pc != null)
                        {
                            pc.cardtype = type;
                            try { pc.SetCard(); } catch { }
                        }
                    }
                }

                // Also update any PokerCard components in pg hierarchy or CardsParent
                var allPokerCards = pg.GetComponentsInChildren<PokerCard>(true);
                if (allPokerCards != null && slot < allPokerCards.Length && allPokerCards[slot] != null)
                {
                    allPokerCards[slot].cardtype = type;
                    try { allPokerCards[slot].SetCard(); } catch { }
                }
                if (pg.CardsParent != null)
                {
                    var parentCards = pg.CardsParent.GetComponentsInChildren<PokerCard>(true);
                    if (parentCards != null && slot < parentCards.Length && parentCards[slot] != null)
                    {
                        parentCards[slot].cardtype = type;
                        try { parentCards[slot].SetCard(); } catch { }
                    }
                }

                // Update the network-synced card value
                pg.CardValue = type;
                pg.NetworkCardValue = type;
                pg.currentcard = slot;

                // Sync to server and peers:
                if (pg.CardTypes != null && pg.CardTypes.Count >= 2)
                {
                    int c0 = pg.CardTypes[0];
                    int c1 = pg.CardTypes[1];
                    try { pg.RandomCards(c0, c1); } catch { }
                    try { pg.CmdShowCardsFirst(0, c0, c1); } catch { }
                }
                try { pg.SetCardsCmd(); } catch { }
                try { pg.SetCardsRpc(); } catch { }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"SetPokerCard error: {ex.Message}");
            }
        }

        public static void CyclePokerCard(PokerGamePlay pg, int slot, int direction)
        {
            if (pg == null) return;
            var cards = GetCardsForPoker(pg);
            int current = slot < cards.Count ? cards[slot] : 1;
            int idx = Array.IndexOf(CardTypeCycle, current);
            if (idx < 0) idx = 0;
            int next = (idx + direction + CardTypeCycle.Length) % CardTypeCycle.Length;
            SetPokerCard(pg, slot, CardTypeCycle[next]);
        }

        public static void ApplyBestPokerHand(PokerGamePlay pg)
        {
            if (pg == null) return;
            SetPokerCard(pg, 0, 3); // Ace
            SetPokerCard(pg, 1, 3); // Ace
        }

        /// <summary>
        /// God mode: removes all live bullets from the cylinder so pulling trigger is always safe.
        /// </summary>
        public static void EnableGodMode(PokerGamePlay pg)
        {
            if (pg == null) return;
            SetShotOverride(pg, ShotOutcomeOverride.MustSurvive);
            SetNextShotSafe(pg);
        }

        /// <summary>
        /// Sets the death bullet to be the current one.
        /// </summary>
        public static void ForceDeadlyGun(PokerGamePlay pg)
        {
            if (pg == null) return;
            SetShotOverride(pg, ShotOutcomeOverride.MustDie);
            SetNextShotDeadly(pg);
        }

        public static void SetCurrentBullet(PokerGamePlay pg, int bullet)
        {
            if (pg == null) return;
            pg.CurrentBullet = bullet;
        }

        public static void SetDeathBullet(PokerGamePlay pg, int chamberIndex)
        {
            if (pg == null) return;
            try
            {
                if (pg.fullbullets != null)
                {
                    for (int i = 0; i < pg.fullbullets.Count; i++)
                        pg.fullbullets[i] = -1;

                    if (chamberIndex >= 0 && chamberIndex < pg.fullbullets.Count)
                        pg.fullbullets[chamberIndex] = chamberIndex;
                }
            }
            catch { }
        }

        public static void WinRound(PokerGamePlay pg)
        {
            if (pg == null) return;
            try
            {
                EnableGodMode(pg);
                SetPokerCard(pg, 0, 1); // King
                SetPokerCard(pg, 1, 1); // King
            }
            catch { }
        }

        public static List<int> GetBullets(PokerGamePlay pg)
        {
            var res = new List<int>();
            if (pg?.fullbullets == null) return res;
            try
            {
                for (int i = 0; i < pg.fullbullets.Count; i++)
                    res.Add(pg.fullbullets[i]);
            }
            catch { }
            return res;
        }

        /// <summary>
        /// Arms this player so pulling trigger is 100% FATAL. Fills all chambers and stores override.
        /// </summary>
        public static void SetNextShotDeadly(PokerGamePlay pg)
        {
            if (pg == null) return;
            try
            {
                int id = pg.GetInstanceID();
                _shotOverrides[id] = ShotOutcomeOverride.MustDie;

                if (pg.fullbullets != null)
                {
                    int count = pg.fullbullets.Count > 0 ? pg.fullbullets.Count : 6;
                    while (pg.fullbullets.Count < count) pg.fullbullets.Add(-1);

                    for (int i = 0; i < pg.fullbullets.Count; i++)
                    {
                        pg.fullbullets[i] = i;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"SetNextShotDeadly error: {ex.Message}");
            }
        }

        /// <summary>
        /// Arms this player so pulling trigger is 100% SAFE. Empties all 6 chambers, broadcasts authoritative empty bullet commands.
        /// </summary>
        public static void SetNextShotSafe(PokerGamePlay pg)
        {
            if (pg == null) return;
            try
            {
                int id = pg.GetInstanceID();
                _shotOverrides[id] = ShotOutcomeOverride.MustSurvive;

                if (pg.fullbullets != null)
                {
                    int count = pg.fullbullets.Count > 0 ? pg.fullbullets.Count : 6;
                    while (pg.fullbullets.Count < count) pg.fullbullets.Add(-1);

                    for (int i = 0; i < pg.fullbullets.Count; i++)
                    {
                        pg.fullbullets[i] = -1;
                    }
                }

                int cur = pg.CurrentBullet;
                if (cur < 0 || (pg.fullbullets != null && cur >= pg.fullbullets.Count))
                {
                    cur = 0;
                    pg.CurrentBullet = 0;
                }

                // Authoritatively broadcast empty bullet for all 6 chambers so ANY trigger pull is 100% blank
                for (int c = 0; c < 6; c++)
                {
                    try { pg.MakeEmptyBullet(c); } catch { }
                    try { pg.MakeEmptyBulletRPC(c); } catch { }
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"SetNextShotSafe error: {ex.Message}");
            }
        }

        /// <summary>
        /// Immediately and authoritatively executes death on a PokerGamePlay target.
        /// </summary>
        public static void ExecuteForceKill(PokerGamePlay pg)
        {
            if (pg == null) return;
            try
            {
                try { pg.CommandBeDead(); } catch { }
                try { pg.HitTargetCmd(); } catch { }
                try { pg.PlayDeadSfx(); } catch { }
                try { pg.SetDeadAnim(null, pg.gameObject); } catch { }

                var poc = CardController.GetLocalPlayerObjectController();
                if (poc == null) poc = pg.GetComponent<PlayerObjectController>();
                if (poc != null)
                {
                    try { poc.SetDeadDeck(pg.gameObject); } catch { }
                    try { poc.SetDeadSpin(pg.gameObject); } catch { }
                    try { poc.SetHealthData(0, true, pg.gameObject); } catch { }
                }

                var ps = pg.GetComponent<PlayerStats>();
                if (ps != null)
                {
                    try { ps.NetworkDead = true; } catch { }
                    try { ps.isShotHeadMoment = true; ps.NetworkisShotHeadMoment = true; } catch { }
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"ExecuteForceKill error: {ex.Message}");
            }
        }

        /// <summary>
        /// Per-frame polling routine hooked in Core.cs OnUpdate().
        /// Detects when a player pulls the trigger and executes the armed kill/survive override.
        /// </summary>
        public static void UpdateFrame(GameObject[] players, GameObject me)
        {
            if (players == null || players.Length == 0) return;

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null) continue;

                var pg = p.GetComponent<PokerGamePlay>();
                if (pg == null) continue;

                int id = pg.GetInstanceID();
                bool aimNow = pg.TakingAim || pg.NetworkTakingAim;
                bool hadAim = _lastTakingAim.TryGetValue(id, out bool wasAim) && wasAim;
                _lastTakingAim[id] = aimNow;

                ShotOutcomeOverride outcome = GetShotOverride(pg);

                // Continuous enforcement for MustSurvive
                if (outcome == ShotOutcomeOverride.MustSurvive)
                {
                    if (pg.fullbullets != null)
                    {
                        for (int b = 0; b < pg.fullbullets.Count; b++)
                        {
                            if (pg.fullbullets[b] != -1) pg.fullbullets[b] = -1;
                        }
                    }

                    // If aim is active, keep sending empty bullet commands for all chambers
                    if (aimNow)
                    {
                        for (int c = 0; c < 6; c++)
                        {
                            try { pg.MakeEmptyBullet(c); } catch { }
                        }
                    }
                }
                else if (outcome == ShotOutcomeOverride.MustDie)
                {
                    if (pg.fullbullets != null)
                    {
                        for (int b = 0; b < pg.fullbullets.Count; b++)
                        {
                            if (pg.fullbullets[b] == -1) pg.fullbullets[b] = b;
                        }
                    }
                }

                // Trigger pull edge: TakingAim transitioned from TRUE -> FALSE, OR local click during aim
                bool isLocal = (p == me);
                bool triggerClicked = isLocal && aimNow && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));
                bool aimReleased = hadAim && !aimNow;

                if (aimReleased || triggerClicked)
                {
                    if (outcome == ShotOutcomeOverride.MustDie)
                    {
                        MelonLoader.MelonLogger.Msg($"[+] PokerController: Trigger pull detected with MustDie armed on {p.name}. Executing lethal packet.");
                        ExecuteForceKill(pg);
                        try { pg.StartCoroutine(pg.WaitforFire()); } catch { }
                        _shotOverrides[id] = ShotOutcomeOverride.Normal; // Disarm after fatal trigger
                    }
                    else if (outcome == ShotOutcomeOverride.MustSurvive)
                    {
                        MelonLoader.MelonLogger.Msg($"[+] PokerController: Trigger pull detected with MustSurvive armed on {p.name}. Enforcing blank chambers.");
                        for (int c = 0; c < 6; c++)
                        {
                            try { pg.MakeEmptyBullet(c); } catch { }
                            try { pg.MakeEmptyBulletRPC(c); } catch { }
                        }
                        if (pg.fullbullets != null)
                        {
                            for (int b = 0; b < pg.fullbullets.Count; b++) pg.fullbullets[b] = -1;
                        }
                        try { pg.StartCoroutine(pg.WaitforEmpty()); } catch { }
                        // Disarm after successful survive
                        _shotOverrides[id] = ShotOutcomeOverride.Normal;
                    }
                }
            }
        }

        public static void RotateChamber(PokerGamePlay pg, int delta)
        {
            if (pg == null) return;
            try
            {
                int count = (pg.fullbullets != null && pg.fullbullets.Count > 0) ? pg.fullbullets.Count : 6;
                int cur = pg.CurrentBullet;
                if (cur < 0 || cur >= count) cur = 0;
                pg.CurrentBullet = (cur + delta + count) % count;
            }
            catch { }
        }

        public static void ToggleChamberBullet(PokerGamePlay pg, int chamberIndex)
        {
            if (pg == null || pg.fullbullets == null || chamberIndex < 0) return;
            try
            {
                while (pg.fullbullets.Count <= chamberIndex) pg.fullbullets.Add(-1);
                if (pg.fullbullets[chamberIndex] != -1)
                    pg.fullbullets[chamberIndex] = -1;
                else
                    pg.fullbullets[chamberIndex] = chamberIndex;
            }
            catch { }
        }

        public static void SetAllChambersSafe(PokerGamePlay pg)
        {
            if (pg == null || pg.fullbullets == null) return;
            try
            {
                SetShotOverride(pg, ShotOutcomeOverride.MustSurvive);
                for (int i = 0; i < pg.fullbullets.Count; i++)
                    pg.fullbullets[i] = -1;
                if (pg.CurrentBullet < 0 || pg.CurrentBullet >= pg.fullbullets.Count)
                    pg.CurrentBullet = 0;
                try { pg.MakeEmptyBullet(pg.CurrentBullet); } catch { }
                try { pg.MakeEmptyBulletRPC(pg.CurrentBullet); } catch { }
            }
            catch { }
        }

        public static void SetAllChambersDeadly(PokerGamePlay pg)
        {
            if (pg == null || pg.fullbullets == null) return;
            try
            {
                SetShotOverride(pg, ShotOutcomeOverride.MustDie);
                for (int i = 0; i < pg.fullbullets.Count; i++)
                    pg.fullbullets[i] = i;
                if (pg.CurrentBullet < 0 || pg.CurrentBullet >= pg.fullbullets.Count)
                    pg.CurrentBullet = 0;
            }
            catch { }
        }

        public static string GetPokerCylinderRadar(PokerGamePlay pg)
        {
            if (pg == null) return "[ ○ ○ ○ ○ ○ ○ ]";
            string s = "[ ";
            int cur = pg.CurrentBullet;
            int total = (pg.fullbullets != null && pg.fullbullets.Count >= 6) ? pg.fullbullets.Count : 6;
            for (int i = 0; i < total; i++)
            {
                bool isCur = (i == cur);
                bool isBul = (pg.fullbullets != null && i < pg.fullbullets.Count && pg.fullbullets[i] != -1);
                if (isCur && isBul) s += "<color=#FF3333>●</color> ";
                else if (isCur) s += "<color=#00FFB0>◉</color> ";
                else if (isBul) s += "<color=#FF8800>●</color> ";
                else s += "<color=#556677>○</color> ";
            }
            s += "]";
            return s;
        }
    }
}
