using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace LiarsBarMod.Features.Dice
{
    public static class DiceController
    {
        public static List<int> GetDiceForPlayer(GameObject p)
        {
            var res = new List<int>();
            if (p == null) return res;

            try
            {
                var dg = p.GetComponent<DiceGamePlay>();
                if (dg != null)
                {
                    // 1. Try DiceValues SyncList (present when revealed or when you are host)
                    if (dg.DiceValues != null && dg.DiceValues.Count > 0)
                    {
                        for (int i = 0; i < dg.DiceValues.Count; i++)
                        {
                            int v = dg.DiceValues[i];
                            if (v >= 1 && v <= 6) res.Add(v);
                        }
                    }

                    // 2. If empty or partial, try dicerenders list
                    if (res.Count == 0 && dg.dicerenders != null && dg.dicerenders.Count > 0)
                    {
                        for (int i = 0; i < dg.dicerenders.Count; i++)
                        {
                            var d = dg.dicerenders[i];
                            if (d != null && d.Face >= 1 && d.Face <= 6)
                            {
                                res.Add(d.Face);
                            }
                        }
                    }

                    // 3. If still empty, search all child Dice components under player
                    if (res.Count == 0)
                    {
                        var childDice = p.GetComponentsInChildren<Il2Cpp.Dice>(true);
                        if (childDice != null && childDice.Length > 0)
                        {
                            foreach (var d in childDice)
                            {
                                if (d != null && d.Face >= 1 && d.Face <= 6)
                                {
                                    res.Add(d.Face);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return res;
        }

        public static void SetSingleDice(DiceGamePlay dg, int index, int value)
        {
            if (dg == null || value < 1 || value > 6) return;

            try
            {
                // Update SyncList
                if (dg.DiceValues != null && index >= 0 && index < dg.DiceValues.Count)
                {
                    dg.DiceValues[index] = value;
                }

                // Update physical 3D/2D dice renderers under the cup
                if (dg.dicerenders != null && index >= 0 && index < dg.dicerenders.Count)
                {
                    var d = dg.dicerenders[index];
                    if (d != null)
                    {
                        d.Face = value;
                        try
                        {
                            if (d.DiceIcons != null && d.DiceIcons.Count >= value)
                            {
                                var sprite = d.DiceIcons[value - 1];
                                if (d.dice != null) d.dice.sprite = sprite;
                                if (d.dice2 != null) d.dice2.sprite = sprite;
                            }
                        }
                        catch { }
                    }
                }

                SyncAllDiceNetwork(dg);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"SetSingleDice error: {ex.Message}");
            }
        }

        public static void SetAllDice(DiceGamePlay dg, int value)
        {
            if (dg == null || value < 1 || value > 6) return;

            try
            {
                int count = dg.DiceValues != null ? dg.DiceValues.Count : 5;
                if (dg.DiceValues != null)
                {
                    for (int i = 0; i < dg.DiceValues.Count; i++)
                    {
                        dg.DiceValues[i] = value;
                    }
                }

                if (dg.dicerenders != null)
                {
                    for (int i = 0; i < dg.dicerenders.Count; i++)
                    {
                        var d = dg.dicerenders[i];
                        if (d != null)
                        {
                            d.Face = value;
                            try
                            {
                                if (d.DiceIcons != null && d.DiceIcons.Count >= value)
                                {
                                    var sprite = d.DiceIcons[value - 1];
                                    if (d.dice != null) d.dice.sprite = sprite;
                                    if (d.dice2 != null) d.dice2.sprite = sprite;
                                }
                            }
                            catch { }
                        }
                    }
                }

                SyncAllDiceNetwork(dg);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"SetAllDice error: {ex.Message}");
            }
        }

        public static void SyncAllDiceNetwork(DiceGamePlay dg)
        {
            if (dg == null) return;

            try
            {
                int count = dg.DiceValues != null ? dg.DiceValues.Count : (dg.dicerenders != null ? dg.dicerenders.Count : 0);
                if (count <= 0) return;

                int[] vals = new int[count];
                for (int i = 0; i < count; i++)
                {
                    if (dg.DiceValues != null && i < dg.DiceValues.Count) vals[i] = dg.DiceValues[i];
                    else if (dg.dicerenders != null && i < dg.dicerenders.Count && dg.dicerenders[i] != null) vals[i] = dg.dicerenders[i].Face;
                    else vals[i] = 1;
                }

                // If Server / Host, broadcast to all clients
                if (dg.isServer)
                {
                    try { dg.RpcSetAllDice(dg.name, vals); } catch { }
                    try
                    {
                        var conn = dg.connectionToClient;
                        if (conn != null) dg.TargetSetDiceValues(conn, vals);
                    }
                    catch { }
                }

                // Set Dirty flags
                try { dg.SetDirty(); } catch { }
            }
            catch { }
        }
    }
}
