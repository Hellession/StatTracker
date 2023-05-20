using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using PlayerIOClient;
using Server;
using UnityEngine;
using Random = System.Random;

namespace StatTracker
{
    [BepInDependency("com.tubaantics.bettertos", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.hellession.stattracker";
        
        internal static List<string> CurrentCSVFile { get; set; } = new List<string>();
        
        /// <summary>
        /// Should contain 'null' for every game that is already saved in 'Plugin.CurrentCSVFile'
        /// </summary>
        internal static List<SavedGame> CurrentSavedGames { get; } = new List<SavedGame>();
        
        internal static SavedGame OngoingGame { get; set; }

        internal static ManualLogSource Log { get; set; }

        internal static List<LobbyModifier> CurrentLobbyModifiers { get; } = new List<LobbyModifier>();

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            
            LoadFile();
            Harmony harmony = Harmony.CreateAndPatchAll(typeof(Plugin), PluginGUID);
            if (Chainloader.PluginInfos.ContainsKey("com.tubaantics.bettertos"))
            {
                Logger.LogInfo($"DETECTED BetterTOS! Patching BetterTOS methods...");
                BetterTOSPatches.PatchBetterTOS(harmony);
            }
            else
                Logger.LogInfo($"No BetterTOS found. Not patching BetterTOS.");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnDestroy()
        {
            Log.LogInfo($"OnDestroy() called. The player is leaving the game! Saving data if needed...");
            if (OngoingGame == null) return;
            if (OngoingGame.GameState != GameSaveState.GameEnded) OngoingGame.MyOutcome = GameOutcome.Disconnection;
            SaveFile();
        }

        //aids
        public static List<string> TopFormats { get; } = new()
        {"Game mode,My starting Role,My game outcome,Am I Traitor or VIP or Recruit,Target player number,Game State,Am I alive,My position,My name,My final role,Started at,Ended At,Player 1 name,Player 1 Role,Player 1 Outcome,Player 2 name,Player 2 Role,Player 2 Outcome,Player 3 name,Player 3 Role,Player 3 Outcome,Player 4 name,Player 4 Role,Player 4 Outcome,Player 5 name,Player 5 Role,Player 5 Outcome,Player 6 name,Player 6 Role,Player 6 Outcome,Player 7 name,Player 7 Role,Player 7 Outcome,Player 8 name,Player 8 Role,Player 8 Outcome,Player 9 name,Player 9 Role,Player 9 Outcome,Player 10 name,Player 10 Role,Player 10 Outcome,Player 11 name,Player 11 Role,Player 11 Outcome,Player 12 name,Player 12 Role,Player 12 Outcome,Player 13 name,Player 13 Role,Player 13 Outcome,Player 14 name,Player 14 Role,Player 14 Outcome,Player 15 name,Player 15 Role,Player 15 Outcome",
            "Game mode,My starting Role,My game outcome,Am I Traitor or VIP,Target player number,Game State,Am I alive,My position,My name,My final role,Started at,Ended At,Scroll 1,Scroll 2,Scroll 3,Data version,Player 1 name,Player 1 Role,Player 1 Outcome,Player 2 name,Player 2 Role,Player 2 Outcome,Player 3 name,Player 3 Role,Player 3 Outcome,Player 4 name,Player 4 Role,Player 4 Outcome,Player 5 name,Player 5 Role,Player 5 Outcome,Player 6 name,Player 6 Role,Player 6 Outcome,Player 7 name,Player 7 Role,Player 7 Outcome,Player 8 name,Player 8 Role,Player 8 Outcome,Player 9 name,Player 9 Role,Player 9 Outcome,Player 10 name,Player 10 Role,Player 10 Outcome,Player 11 name,Player 11 Role,Player 11 Outcome,Player 12 name,Player 12 Role,Player 12 Outcome,Player 13 name,Player 13 Role,Player 13 Outcome,Player 14 name,Player 14 Role,Player 14 Outcome,Player 15 name,Player 15 Role,Player 15 Outcome",
            "Game mode,My starting Role,My game outcome,\"Am I Traitor, VIP or Recruit\",Target player number,Game State,Am I alive,My position,My name,My final role,Started at,Ended At,Custom Role List,Custom Role List Hash,Scroll 1,Scroll 2,Scroll 3,Data version,Player 1 name,Player 1 Role,Player 1 Outcome,Player 2 name,Player 2 Role,Player 2 Outcome,Player 3 name,Player 3 Role,Player 3 Outcome,Player 4 name,Player 4 Role,Player 4 Outcome,Player 5 name,Player 5 Role,Player 5 Outcome,Player 6 name,Player 6 Role,Player 6 Outcome,Player 7 name,Player 7 Role,Player 7 Outcome,Player 8 name,Player 8 Role,Player 8 Outcome,Player 9 name,Player 9 Role,Player 9 Outcome,Player 10 name,Player 10 Role,Player 10 Outcome,Player 11 name,Player 11 Role,Player 11 Outcome,Player 12 name,Player 12 Role,Player 12 Outcome,Player 13 name,Player 13 Role,Player 13 Outcome,Player 14 name,Player 14 Role,Player 14 Outcome,Player 15 name,Player 15 Role,Player 15 Outcome"
        };

        private static string SaveFilePath
        {
            get
            {
                if (Application.platform is RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor)
                    return "Hellession/savedGames.csv";
                return Application.dataPath + "/Hellession/Data/savedGames.csv";
            }
        }

        private static void LoadFile()
        {
            CurrentCSVFile.Add(TopFormats[2]);
            string allTheCommas = "";
            for (int i = 0; i < 60; i++)
                allTheCommas += ",";
            CurrentCSVFile.Add(allTheCommas);
            if (File.Exists(SaveFilePath))
            {
                List<string> linesInThisFile =
                    File.ReadAllLines(SaveFilePath).ToList();
                int detectedVersion = 3;
                if (linesInThisFile.Count > 0 && linesInThisFile[0] == TopFormats[0])
                    detectedVersion = 1;
                else if (linesInThisFile.Count > 0 && linesInThisFile[0] == TopFormats[1])
                    detectedVersion = 2;
                for (int i = 2; i < linesInThisFile.Count; i++)
                {
                    CurrentSavedGames.Add(null);
                    switch (detectedVersion)
                    {
                        case 1:
                        {
                            var dataEntries = linesInThisFile[i].Split(',').ToList();
                            dataEntries.Insert(12, "1");
                            dataEntries.Insert(12, "Unknown");
                            dataEntries.Insert(12, "Unknown");
                            dataEntries.Insert(12, "Unknown");
                            CurrentCSVFile.Add(string.Join(",", dataEntries));
                        }
                            break;
                        case 2:
                        {
                            var dataEntries = linesInThisFile[i].Split(',').ToList();
                            dataEntries.Insert(12, "");
                            dataEntries.Insert(12, "");
                            CurrentCSVFile.Add(string.Join(",", dataEntries));
                        }
                            break;
                        case 3:
                            CurrentCSVFile.Add(linesInThisFile[i]);
                            break;
                    }
                }
            }
        }

        internal static string GetCurrentRoleList(GameState state)
        {
            var currentRoleList = GlobalServiceLocator.GameService.ActiveGameState.RoleList;
            if (currentRoleList.Count == 0 && GlobalServiceLocator.GameService.ActiveGameState.GameMode is
                {
                    RoleList: { }
                })
                currentRoleList = GlobalServiceLocator.GameService.ActiveGameState.GameMode.RoleList;
            //sort roles by their alphabetic names
            var stringRoles = (from g in currentRoleList select g.Name).ToList();
            stringRoles.AddRange(from g in CurrentLobbyModifiers select g.ToString());
            return string.Join(", ", stringRoles);
        }
        
        internal static string GetCurrentRoleListOrdered(GameState state)
        {
            var currentRoleList = GlobalServiceLocator.GameService.ActiveGameState.RoleList;
            if (currentRoleList.Count == 0 && GlobalServiceLocator.GameService.ActiveGameState.GameMode is
                {
                    RoleList: { }
                })
                currentRoleList = GlobalServiceLocator.GameService.ActiveGameState.GameMode.RoleList;
            //sort roles by their alphabetic names
            var stringRoles = (from g in currentRoleList orderby g.Name select g.Name).ToList();
            stringRoles.AddRange(from g in CurrentLobbyModifiers orderby g.ToString() select g.ToString());
            return string.Join(", ", stringRoles);
        }
        
        /// <summary>
        /// thanks stack overflow!
        /// </summary>
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        internal static void SaveFile()
        {
            var dir = Path.GetDirectoryName(SaveFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            List<string> newList = new List<string>
            {
                CurrentCSVFile[0],
                CurrentCSVFile[1]
            };
            for (int entry = 0; entry < CurrentSavedGames.Count; entry++)
            {
                if(CurrentSavedGames[entry] is null)
                    newList.Add(CurrentCSVFile[entry+2]);
                else
                    newList.Add(CurrentSavedGames[entry].GetCSVValue());
            }

            try
            {
                File.WriteAllText(SaveFilePath, string.Join("\n", newList));
            }
            catch (Exception e)
            {
                Log.LogWarning($"[Failed to save data to file!] {e}");
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerPickNames")]
        public static void PickNamesPatch(BaseMessage msg)
        {
            PickNamesMessage pickNamesMessage = (PickNamesMessage)msg;
            if (OngoingGame != null)
            {
                Log.LogWarning($"New game has started even though there is an ongoing game saved! Leaving the previous game where it was!");
            }
            
            var gameState = GlobalServiceLocator.GameService.ActiveGameState;
            OngoingGame = new SavedGame()
            {
                GameState = GameSaveState.PickingNames,
                MyStartingRole = "Unknown",
                StartedAt = DateTime.Now,
                GameMode = gameState.GameMode.Name,
                MyOutcome = GameOutcome.TBD
            };
            for(var i=0;i<pickNamesMessage.PlayerCount;i++)
                OngoingGame.Players.Add(new SavedGamePlayer());
            CurrentSavedGames.Add(OngoingGame);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerRoleAndPosition")]
        public static void RoleAndPositionPatch(BaseMessage msg)
        {
            RoleAndPositionMessage extraInfoMessage = (RoleAndPositionMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerRoleAndPosition.");
                return;
            }

            //set all the names of every player to their nickname for the time being
            var gameState = GlobalServiceLocator.GameService.ActiveGameState;
            OngoingGame.GameState = GameSaveState.RoleReveal;
            for (int playerIndex = 0;playerIndex<gameState.Players.Count;playerIndex++)
            {
                var gameStatePlayer = gameState.Players[playerIndex];
                var ongoingPlayer = OngoingGame.Players[playerIndex];
                ongoingPlayer.PlayerUsername = gameStatePlayer.Name;
                if (gameStatePlayer == gameState.Me)
                {
                    OngoingGame.MyStartingRole = gameState.Me.CurrentRole.Name;
                    OngoingGame.MyFinalRole = gameState.Me.CurrentRole.Name;
                    OngoingGame.MyName = gameState.Me.Name;
                    OngoingGame.MyPosition = playerIndex + 1;
                    ongoingPlayer.PlayerRole = gameState.Me.CurrentRole.Name;
                }
            }

            if (extraInfoMessage.TargetPosition != null)
                OngoingGame.TargetPlayer = extraInfoMessage.TargetPosition.Value + 1;
            SaveFile();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerUserLeftDuringSelection")]
        public static void UserLeftDuringPregamePatch(BaseMessage msg)
        {
            UserLeftDuringSelectionMessage extraInfoMessage = (UserLeftDuringSelectionMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerUserLeftDuringSelection.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
                return;
            OngoingGame.Players[extraInfoMessage.Position].PlayerOutcome = GameOutcome.Disconnection;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RoleRevealController), nameof(RoleRevealController.AddScroll))]
        public static void AddScrollPatch(CustomizationScrollData scrollData, RoleRevealController __instance, int ___scrollCount)
        {
            if (___scrollCount >= __instance.ScrollImage.Length) return;
            //this scroll will get added
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring AddScroll.");
                return;
            }
            if (OngoingGame.GameState is GameSaveState.GameEnded or GameSaveState.OngoingAlive or GameSaveState.OngoingDead)
                return;
            if (string.IsNullOrEmpty(OngoingGame.EquippedScroll1))
            {
                OngoingGame.EquippedScroll1 = scrollData.Name;
                return;
            }
            if (string.IsNullOrEmpty(OngoingGame.EquippedScroll2))
            {
                OngoingGame.EquippedScroll2 = scrollData.Name;
                return;
            }
            if (string.IsNullOrEmpty(OngoingGame.EquippedScroll3))
            {
                OngoingGame.EquippedScroll3 = scrollData.Name;
                return;
            }
            Log.LogWarning($"Received information that a scroll is being added, but all 3 scrolls are already not empty! Additional scroll: {scrollData.Name}");
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerUserDisconnected")]
        public static void UserLeftPatch(BaseMessage msg)
        {
            UserDisconnectedMessage extraInfoMessage = (UserDisconnectedMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerUserDisconnected.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
                return;
            OngoingGame.Players[extraInfoMessage.Position].PlayerOutcome = GameOutcome.Disconnection;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerWhoDiedAndHow")]
        public static void OnSomeoneDiedPatch(BaseMessage msg)
        {
            WhoDiedAndHowMessage extraInfoMessage = (WhoDiedAndHowMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerWhoDiedAndHow.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
            {
                Log.LogWarning($"Ongoing game has state GameEnded, but there was a player who died and the server wants to tell me...?");
                return;
            }

            if (OngoingGame.Players[extraInfoMessage.Position].PlayerRole != "Unknown")
                return;
            var gameRules = GlobalServiceLocator.GameService.ActiveGameRules;
            OngoingGame.Players[extraInfoMessage.Position].PlayerRole = gameRules.Roles[extraInfoMessage.Role].Name;
            SaveFile();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(IGameService), "RaiseOnGameOver")]
        public static void SomeoneWonPatch(int winningFaction, List<Player> winners)
        {
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring RaiseOnGameOver.");
                return;
            }
            
            OngoingGame.GameState = GameSaveState.GameEnded;
            foreach (var currPlr in winners)
            {
                int playerIndex = currPlr.Position;
                var savedGamePlayer = OngoingGame.Players[playerIndex];
                savedGamePlayer.PlayerOutcome = GameOutcome.Victory;
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerVIPPlayer")]
        public static void FoundVipPatch(BaseMessage msg)
        {
            VIPPlayerMessage vipMsg = (VIPPlayerMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerVIPPlayer.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
            {
                Log.LogWarning($"Ongoing game has state GameEnded, but OnServerVIPPlayer just fired.");
                return;
            }
            
            if (vipMsg.Position + 1 == OngoingGame.MyPosition)
                OngoingGame.AmTraitorOrVIP = true;
            OngoingGame.Players[vipMsg.Position].IsVIP = true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerTraitor")]
        public static void FoundTtPatch(BaseMessage msg)
        {
            TraitorMessage vipMsg = (TraitorMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerTraitor.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
            {
                Log.LogWarning($"Ongoing game has state GameEnded, but OnServerTraitor just fired.");
                return;
            }

            if (vipMsg.Position + 1 == OngoingGame.MyPosition)
                OngoingGame.AmTraitorOrVIP = true;
            OngoingGame.Players[vipMsg.Position].IsTT = true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(IApplicationService), "RaiseOnReturnToHome")]
        public static void ReturnHomePatch()
        {
            if (OngoingGame == null) return;
            if (OngoingGame.GameState != GameSaveState.GameEnded) OngoingGame.MyOutcome = GameOutcome.Disconnection;
            Log.LogDebug($"User returned to home screen but the ongoing game isn't null. Setting to null and saving...");
            OngoingGame = null;
            SaveFile();
            CurrentLobbyModifiers.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(IApplicationService), "RaiseOnDisconnected")]
        public static void DisconnectedPatch()
        {
            if (OngoingGame == null) return;
            if (OngoingGame.GameState != GameSaveState.GameEnded) OngoingGame.MyOutcome = GameOutcome.Disconnection;
            Log.LogDebug($"User disconnected but the ongoing game isn't null. Setting to null and saving...");
            OngoingGame = null;
            SaveFile();
            CurrentLobbyModifiers.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerStartFirstDay")]
        public static void FirstDayPatch(BaseMessage msg)
        {
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerStartFirstDay.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
            {
                Log.LogWarning($"Ongoing game has state GameEnded, but OnServerStartFirstDay just fired.");
                return;
            }

            OngoingGame.GameState = GameSaveState.OngoingAlive;
            if (!OngoingGame.GameMode.StartsWith("Custom")) return;
            OngoingGame.CustomRoleList = GetCurrentRoleList(GlobalServiceLocator.GameService.ActiveGameState);
            OngoingGame.CustomRoleListMD5 = CreateMD5(GetCurrentRoleListOrdered(GlobalServiceLocator.GameService.ActiveGameState));
            CurrentLobbyModifiers.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerUserDied")]
        public static void UserDiedPatch(BaseMessage msg)
        {
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerUserDied.");
                return;
            }

            if (OngoingGame.GameState == GameSaveState.GameEnded)
            {
                Log.LogWarning($"Ongoing game has state GameEnded, but OnServerUserDied just fired.");
                return;
            }

            OngoingGame.GameState = GameSaveState.OngoingDead;
            OngoingGame.AmAlive = false;
            SaveFile();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerAfterGameScreenData")]
        public static void OnServerGameEndedPatch(BaseMessage msg)
        {
            //AfterGameScreenDataMessage extraInfoMessage = (AfterGameScreenDataMessage)msg;
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerAfterGameScreenData.");
                return;
            }
            
            
            var gameRules = GlobalServiceLocator.GameService.ActiveGameRules;
            var activeEndGame = GlobalServiceLocator.GameService.ActiveEndGameData;

            OngoingGame.MyOutcome = (GameOutcome)activeEndGame.GameResult;
            OngoingGame.EndedAt = DateTime.Now;
            foreach (var currPlr in activeEndGame.PartyMemberList)
            {
                var savedGamePlayer = OngoingGame.Players[currPlr.Position];
                savedGamePlayer.PlayerRole = gameRules.Roles[currPlr.BeginningRoleId].Name;
                savedGamePlayer.PlayerUsername = currPlr.AccountName;
                if (currPlr.Position + 1 == OngoingGame.MyPosition)
                    OngoingGame.MyFinalRole = gameRules.Roles[currPlr.FinalRoleId].Name;
            }

            OngoingGame = null;
            SaveFile();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameService), "OnServerOtherMafia")]
        public static void OnMafiaInfoPatch(BaseMessage msg)
        {
            if (OngoingGame is null)
            {
                Log.LogWarning($"Ongoing game is null! Ignoring OnServerOtherMafia.");
                return;
            }
            OtherMafiaMessage extraInfoMessage = (OtherMafiaMessage)msg;
            
            //set all the names of every player to their nickname for the time being
            var gameRules = GlobalServiceLocator.GameService.ActiveGameRules;
            for (int i = 0;i<extraInfoMessage.Positions.Length;i++)
            {
                var playerIndex = extraInfoMessage.Positions[i];
                var ongoingPlayer = OngoingGame.Players[playerIndex];
                ongoingPlayer.PlayerRole = gameRules.Roles[extraInfoMessage.Roles[i]].Name;
            }
            SaveFile();
        }
    }
}
