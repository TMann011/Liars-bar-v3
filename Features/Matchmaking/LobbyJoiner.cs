using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Il2Cpp;
using Il2CppSteamworks;
using MelonLoader;
using UnityEngine;

namespace LiarsBarMod.Features.Matchmaking
{
    public static class LobbyJoiner
    {
        public static int SelectedModeIndex = 0;
        public static string DirectLobbyIdInput = "";
        public static bool ShowInGameMatches = true;
        public static int FilterModeIndex = -1; // -1 = All Modes

        public static readonly string[] ModeNames = {
            "Standard Cards (Deck)",
            "Liar's Dice",
            "Devil Cards (Chaos Deck)",
            "Liar's Poker",
            "Velvet Room",
            "Texas Hold'em",
            "Liar's Spin",
            "Arena",
            "Russian Roulette",
            "Ranked Match"
        };

        /// <summary>
        /// Extracts a 64-bit Steam Lobby ID from raw text, invite links, or steam:// URLs.
        /// </summary>
        public static ulong ExtractLobbyId(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            string clean = input.Trim();

            // Check steam://joinlobby/<appid>/<lobbyid>/<steamid>
            var matchUrl = Regex.Match(clean, @"joinlobby/\d+/(\d+)");
            if (matchUrl.Success && ulong.TryParse(matchUrl.Groups[1].Value, out ulong urlId))
            {
                return urlId;
            }

            // Check standard 64-bit Steam ID regex (17-19 digits, commonly starting with 1097...)
            var matchDigits = Regex.Match(clean, @"\b(1097\d{12,15}|\d{17,19})\b");
            if (matchDigits.Success && ulong.TryParse(matchDigits.Value, out ulong digId))
            {
                return digId;
            }

            // Fallback plain parse
            if (ulong.TryParse(clean, out ulong rawId))
            {
                return rawId;
            }

            return 0;
        }

        public static string ResolveLobbyDataMode(CSteamID id)
        {
            try
            {
                string mode = SteamMatchmaking.GetLobbyData(id, "LobbyType");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "lobbytype");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "GameMode");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "gameMode");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "gamemode");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "Mode");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "DeckMode");
                if (string.IsNullOrEmpty(mode)) mode = SteamMatchmaking.GetLobbyData(id, "gamemodetext");
                return FormatModeName(mode);
            }
            catch
            {
                return "Standard Cards (Deck)";
            }
        }

        public static string FormatModeName(string rawMode)
        {
            if (string.IsNullOrEmpty(rawMode)) return "Standard Cards (Deck)";
            string trimmed = rawMode.Trim();
            if (int.TryParse(trimmed, out int mIdx))
            {
                if (mIdx >= 0 && mIdx < ModeNames.Length) return ModeNames[mIdx];
                return $"Mode #{mIdx}";
            }
            string lower = trimmed.ToLowerInvariant();
            if (lower.Contains("ranked")) return "Ranked Match";
            if (lower.Contains("devil") || lower.Contains("chaos")) return "Devil Cards (Chaos Deck)";
            if (lower.Contains("dice")) return "Liar's Dice";
            if (lower.Contains("poker")) return "Liar's Poker";
            if (lower.Contains("velvet")) return "Velvet Room";
            if (lower.Contains("texas")) return "Texas Hold'em";
            if (lower.Contains("spin")) return "Liar's Spin";
            if (lower.Contains("arena")) return "Arena";
            if (lower.Contains("roulette")) return "Russian Roulette";
            if (lower.Contains("deck") || lower.Contains("card") || lower.Contains("standard")) return "Standard Cards (Deck)";
            return trimmed;
        }

        public struct LobbyInfo
        {
            public CSteamID Id;
            public ulong RawId;
            public string Name;
            public string Mode;
            public int Members;
            public int MaxMembers;
            public bool InProgress;
        }

        public struct FriendSession
        {
            public string FriendName;
            public CSteamID FriendId;
            public CSteamID LobbyId;
            public string Mode;
            public bool HasLobby;
        }

        public static readonly List<LobbyInfo> CachedLobbies = new List<LobbyInfo>();
        public static readonly List<FriendSession> CachedFriends = new List<FriendSession>();
        public static string StatusMessage = "Ready. Select a game mode, enter Lobby ID, or queue Ranked.";
        private static float _lastAutoPoll = 0f;

        public static void PollUpdate()
        {
            if (Time.time - _lastAutoPoll > 1.5f)
            {
                _lastAutoPoll = Time.time;
                UpdateLobbyCache();
            }
        }

        public static string GetModeForLobby(CSteamID lobbyId)
        {
            try
            {
                for (int i = 0; i < CachedLobbies.Count; i++)
                {
                    if (CachedLobbies[i].Id.m_SteamID == lobbyId.m_SteamID && !string.IsNullOrEmpty(CachedLobbies[i].Mode))
                        return CachedLobbies[i].Mode;
                }
                for (int i = 0; i < CachedFriends.Count; i++)
                {
                    if (CachedFriends[i].LobbyId.m_SteamID == lobbyId.m_SteamID && !string.IsNullOrEmpty(CachedFriends[i].Mode))
                        return CachedFriends[i].Mode;
                }
                return ResolveLobbyDataMode(lobbyId);
            }
            catch { }
            return "Liar's Bar";
        }

        private static Callback<LobbyEnter_t> _onNativeLobbyEnteredCallback;
        private static Callback<LobbyDataUpdate_t> _onNativeLobbyDataUpdatedCallback;

        public static void EnsureCallbacks()
        {
            try
            {
                if (_onNativeLobbyEnteredCallback == null)
                    _onNativeLobbyEnteredCallback = Callback<LobbyEnter_t>.Create(new Action<LobbyEnter_t>(OnNativeLobbyEntered));
                if (_onNativeLobbyDataUpdatedCallback == null)
                    _onNativeLobbyDataUpdatedCallback = Callback<LobbyDataUpdate_t>.Create(new Action<LobbyDataUpdate_t>(OnNativeLobbyDataUpdated));
            }
            catch { }
        }

        private static void OnNativeLobbyEntered(LobbyEnter_t callback)
        {
            MelonLogger.Msg($"[+] LobbyJoiner: Native LobbyEnter_t received for {callback.m_ulSteamIDLobby}, response={callback.m_EChatRoomEnterResponse}");
            var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
            var nm = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>() ?? (CustomNetworkManager.singleton != null ? CustomNetworkManager.singleton.TryCast<CustomNetworkManager>() : null);

            if (sl != null)
            {
                try { sl.OnLobbyEntered(callback); } catch { }
            }

            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.RequestLobbyData(lobbyId);
            TryConnectHostAddress(lobbyId, nm);
        }

        private static void OnNativeLobbyDataUpdated(LobbyDataUpdate_t callback)
        {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            var nm = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>() ?? (CustomNetworkManager.singleton != null ? CustomNetworkManager.singleton.TryCast<CustomNetworkManager>() : null);
            TryConnectHostAddress(lobbyId, nm);
        }

        private static void TryConnectHostAddress(CSteamID lobbyId, CustomNetworkManager nm)
        {
            if (nm == null || nm.isNetworkActive) return;
            try
            {
                string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, "HostAddress");
                if (string.IsNullOrEmpty(hostAddress)) hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, "hostaddress");
                if (!string.IsNullOrEmpty(hostAddress))
                {
                    MelonLogger.Msg($"[+] LobbyJoiner: Direct fallback starting CustomNetworkManager client to {hostAddress}");
                    nm.networkAddress = hostAddress;
                    nm.StartClient();
                    StatusMessage = $"Connected to Host ({hostAddress})";
                }
            }
            catch { }
        }

        /// <summary>
        /// Connects to any public, private, or friends-only Steam lobby with clean state transitions.
        /// </summary>
        public static void JoinLobby(CSteamID lobbyId)
        {
            try
            {
                if (lobbyId.m_SteamID == 0) return;
                EnsureCallbacks();
                string modeName = GetModeForLobby(lobbyId);
                StatusMessage = $"Connecting to {modeName} ({lobbyId.m_SteamID})...";
                MelonLogger.Msg($"[+] LobbyJoiner: Connecting to {modeName} (Steam Lobby {lobbyId.m_SteamID})");

                // Request lobby data from Steam servers early so HostAddress is populated for private/hidden lobbies
                SteamMatchmaking.RequestLobbyData(lobbyId);

                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                var nm = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>() ?? (CustomNetworkManager.singleton != null ? CustomNetworkManager.singleton.TryCast<CustomNetworkManager>() : null);

                // 1. Leave any existing lobby or match cleanly
                try
                {
                    if (sl != null && sl.CurrentLobbyID != 0 && sl.CurrentLobbyID != lobbyId.m_SteamID)
                    {
                        SteamMatchmaking.LeaveLobby(new CSteamID(sl.CurrentLobbyID));
                        sl.CurrentLobbyID = 0;
                    }
                    if (nm != null && nm.isNetworkActive)
                    {
                        try { nm.StopClient(); } catch { }
                        try { nm.StopHost(); } catch { }
                    }
                }
                catch { }

                // 2. Clear join locks and configure SteamLobby state
                if (sl != null)
                {
                    sl.JoinLocked = false;
                    sl.CurrentLobbyID = lobbyId.m_SteamID;
                    if (sl.manager == null && nm != null) sl.manager = nm;
                    if (sl.LobbyText != null) sl.LobbyText.text = lobbyId.m_SteamID.ToString();
                    if (sl.lobbyname != null) sl.lobbyname.text = lobbyId.m_SteamID.ToString();
                }

                // 3. Update in-game connecting screen
                var lm = LobbyListManager.instance ?? UnityEngine.Object.FindObjectOfType<LobbyListManager>();
                if (lm != null)
                {
                    if (lm.ConnectingScreen != null) lm.ConnectingScreen.SetActive(true);
                    if (lm.ConnectingScreenInfo != null) lm.ConnectingScreenInfo.text = $"Connecting to {modeName}...";
                    if (lm.LobbyIdInput != null) lm.LobbyIdInput.text = lobbyId.m_SteamID.ToString();
                }

                // 4. Dispatch Join: Call native SteamMatchmaking.JoinLobby first to trigger Steam connection, then notify SteamLobby
                MelonLogger.Msg($"[+] LobbyJoiner: Dispatching SteamMatchmaking.JoinLobby({lobbyId.m_SteamID})");
                SteamMatchmaking.JoinLobby(lobbyId);

                if (sl != null)
                {
                    try
                    {
                        sl.JoinLocked = false;
                        sl.JoinLobby(lobbyId);
                    }
                    catch (Exception ex)
                    {
                        // Internal engine method threw NullReferenceException (missing UI or Callback), native SteamMatchmaking.JoinLobby already handled connection
                        MelonLogger.Warning($"[-] sl.JoinLobby internal warning: {ex.Message}. Native JoinLobby active.");
                    }
                }

                StatusMessage = $"Connection request sent for Lobby {lobbyId.m_SteamID}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Join error: {ex.Message}";
                MelonLogger.Error($"[-] JoinLobby error: {ex.Message}");
            }
        }

        /// <summary>
        /// Convenience method to parse a lobby ID string or link and join.
        /// </summary>
        public static void JoinLobbyByIdString(string input)
        {
            ulong id = ExtractLobbyId(input);
            if (id == 0)
            {
                StatusMessage = "<color=#FF5555>Invalid Lobby ID or Link. Please verify.</color>";
                return;
            }
            JoinLobby(new CSteamID(id));
        }

        /// <summary>
        /// Enters the official Ranked Matchmaking Queue.
        /// </summary>
        public static void JoinRankedQueue()
        {
            try
            {
                var lm = LobbyListManager.instance ?? UnityEngine.Object.FindObjectOfType<LobbyListManager>();
                if (lm != null)
                {
                    StatusMessage = "Entering Official Ranked Queue...";
                    MelonLogger.Msg("[+] LobbyJoiner: Entering Official Ranked Queue via LobbyListManager.RankedGame()");
                    if (lm.ConnectingScreen != null) lm.ConnectingScreen.SetActive(true);
                    if (lm.ConnectingScreenInfo != null) lm.ConnectingScreenInfo.text = "Searching for Ranked Match...";
                    lm.RankedGame();
                    return;
                }

                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                if (sl != null)
                {
                    sl.JoinLocked = false;
                    sl.StartMatchMaking();
                    StatusMessage = "Ranked Matchmaking started.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ranked Queue error: {ex.Message}";
                MelonLogger.Error($"[-] JoinRankedQueue error: {ex.Message}");
            }
        }

        public static void QuickJoinMode(int modeIndex)
        {
            try
            {
                if (modeIndex == 9) // Ranked Match
                {
                    JoinRankedQueue();
                    return;
                }

                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                var nm = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>() ?? (CustomNetworkManager.singleton != null ? CustomNetworkManager.singleton.TryCast<CustomNetworkManager>() : null);

                // Leave existing lobby if currently in one
                try
                {
                    if (sl != null && sl.CurrentLobbyID != 0)
                    {
                        SteamMatchmaking.LeaveLobby(new CSteamID(sl.CurrentLobbyID));
                    }
                    if (nm != null)
                    {
                        try { nm.StopClient(); } catch { }
                        try { nm.StopHost(); } catch { }
                    }
                }
                catch { }

                if (sl != null)
                {
                    sl.JoinLocked = false;
                    sl.ShowInGameLobbies = ShowInGameMatches;
                    if (sl.lobbytype != null) sl.lobbytype.value = modeIndex;
                }

                if (nm != null)
                {
                    try { nm.Mode = (CustomNetworkManager.GameMode)modeIndex; } catch { }
                    try { nm.DeckMode = (modeIndex == 2 ? 1 : 0); } catch { }
                    try { nm.isVelvetRoom = (modeIndex == 4); } catch { }
                }

                if (sl != null)
                {
                    StatusMessage = $"Matchmaking queued for: {ModeNames[modeIndex]}";
                    MelonLogger.Msg($"[+] LobbyJoiner: Starting Matchmaking for {ModeNames[modeIndex]}");
                    sl.StartMatchMaking();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Matchmaking error: {ex.Message}";
            }
        }

        public static void CancelMatchmaking()
        {
            try
            {
                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                if (sl != null)
                {
                    sl.StopMatchMaking();
                    StatusMessage = "Matchmaking canceled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Cancel error: {ex.Message}";
            }
        }

        public static void HostLobbyForMode(int modeIndex)
        {
            try
            {
                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                var nm = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>() ?? (CustomNetworkManager.singleton != null ? CustomNetworkManager.singleton.TryCast<CustomNetworkManager>() : null);

                if (sl != null && sl.lobbytype != null)
                {
                    sl.lobbytype.value = modeIndex;
                }

                if (nm != null)
                {
                    try { nm.Mode = (CustomNetworkManager.GameMode)modeIndex; } catch { }
                    try { nm.DeckMode = (modeIndex == 2 ? 1 : 0); } catch { }
                    try { nm.isVelvetRoom = (modeIndex == 4); } catch { }
                }

                if (sl != null)
                {
                    StatusMessage = $"Creating host lobby for {ModeNames[modeIndex]}...";
                    sl.HostLobby();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Host error: {ex.Message}";
            }
        }

        public static void RefreshLobbies()
        {
            try
            {
                StatusMessage = "Querying Steam matchmaking network...";
                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                if (sl != null)
                {
                    sl.ShowInGameLobbies = ShowInGameMatches;
                    sl.GetLobbiesList();
                }

                SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
                SteamMatchmaking.AddRequestLobbyListResultCountFilter(64);
                SteamMatchmaking.RequestLobbyList();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Refresh error: {ex.Message}";
            }
        }

        public static void UpdateLobbyCache()
        {
            try
            {
                var sl = SteamLobby.Instance ?? UnityEngine.Object.FindObjectOfType<SteamLobby>();
                if (sl != null && sl.lobbiesIDs != null && sl.lobbiesIDs.Count > 0)
                {
                    CachedLobbies.Clear();
                    for (int i = 0; i < sl.lobbiesIDs.Count; i++)
                    {
                        var id = sl.lobbiesIDs[i];
                        if (id.m_SteamID == 0) continue;

                        string name = SteamMatchmaking.GetLobbyData(id, "name");
                        if (string.IsNullOrEmpty(name)) name = $"Lobby #{id.m_SteamID}";

                        string mode = ResolveLobbyDataMode(id);

                        int members = SteamMatchmaking.GetNumLobbyMembers(id);
                        int max = SteamMatchmaking.GetLobbyMemberLimit(id);
                        if (max <= 0) max = 4;

                        string inGameStr = SteamMatchmaking.GetLobbyData(id, "inGame");
                        bool inProgress = (inGameStr == "1" || inGameStr.Equals("true", StringComparison.OrdinalIgnoreCase));

                        CachedLobbies.Add(new LobbyInfo
                        {
                            Id = id,
                            RawId = id.m_SteamID,
                            Name = name,
                            Mode = mode,
                            Members = members,
                            MaxMembers = max,
                            InProgress = inProgress
                        });
                    }
                    StatusMessage = $"Discovered {CachedLobbies.Count} live lobbies.";
                }
            }
            catch { }
        }

        public static void RefreshFriends()
        {
            try
            {
                CachedFriends.Clear();
                int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                for (int i = 0; i < count; i++)
                {
                    var fId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                    var gameInfo = new FriendGameInfo_t();
                    if (SteamFriends.GetFriendGamePlayed(fId, out gameInfo))
                    {
                        string friendName = SteamFriends.GetFriendPersonaName(fId);
                        bool hasLobby = (gameInfo.m_steamIDLobby.m_SteamID != 0);
                        string friendMode = "";
                        if (hasLobby)
                        {
                            friendMode = ResolveLobbyDataMode(gameInfo.m_steamIDLobby);
                        }
                        CachedFriends.Add(new FriendSession
                        {
                            FriendName = friendName,
                            FriendId = fId,
                            LobbyId = gameInfo.m_steamIDLobby,
                            Mode = friendMode,
                            HasLobby = hasLobby
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[-] RefreshFriends error: {ex.Message}");
            }
        }
    }
}
