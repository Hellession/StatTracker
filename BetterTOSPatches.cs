using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using BetterTOS;
using HarmonyLib;
using PlayerIOClient;
using Server;
using UnityEngine;
using Random = System.Random;

namespace StatTracker;

public class BetterTOSPatches
{
    /// <summary>
    /// Moved to attempt to fight dependency issues
    /// </summary>
    internal static void PatchBetterTOS(Harmony harmony)
    {
        Type joinLobbyType0 = typeof(MyPatcher).GetNestedTypes(AccessTools.all).First(x =>
                {
                    return AccessTools.FirstMethod(x,
                        g =>
                        {
                            return g.Name.Contains("JoinLobby") && g.GetMethodBody().LocalVariables.Count == 0;
                        }) != null && (from g2 in AccessTools.GetMethodNames(x) where g2.Contains("JoinLobby") select g2).Count() == 3;
                });
                Type joinLobbyType1 = typeof(MyPatcher).GetNestedTypes(AccessTools.all).First(x =>
                {
                    return AccessTools.FirstMethod(x,
                        g =>
                        {
                            return g.Name.Contains("JoinLobby") && g.GetMethodBody().LocalVariables.Count > 1 &&
                                   g.GetMethodBody().LocalVariables[0].LocalType == joinLobbyType0 &&
                                   g.GetMethodBody().LocalVariables[1].LocalType == typeof(LobbyUser);
                        }) != null;
                });
                if (joinLobbyType1 != null)
                {
                    Plugin.Log.LogInfo($"Found {joinLobbyType1}! Patching...");
                    MethodBase method1 = AccessTools.FirstMethod(joinLobbyType1,
                        g => g.Name.Contains("JoinLobby") && g.GetMethodBody().LocalVariables.Count > 1 &&
                             g.GetMethodBody().LocalVariables[0].LocalType == joinLobbyType0 && g.GetMethodBody().LocalVariables[1].LocalType == typeof(LobbyUser));
                    if(method1 == null)
                        Plugin.Log.LogInfo("Method is null!");
                    else
                    {
                        harmony.Patch(method1, postfix: new HarmonyMethod(typeof(BetterTOSPatches), nameof(BetterTOSPatches.JoinLobbyBetterTos)));
                    }
                }
                
                Type uiStartType = typeof(MyPatcher).GetNestedTypes(AccessTools.all).First(x =>
                {
                    //Log.LogInfo($"Type: {x}");
                    return AccessTools.FirstMethod(x,
                        g =>
                        {
                            //Log.LogInfo($"Method name: {g.Name}. Local variables: {g.GetMethodBody().LocalVariables.Count} items");
                            //if(g.GetMethodBody().LocalVariables.Count > 0)
                            //    Log.LogInfo($"Variable at index 0 type: {g.GetMethodBody().LocalVariables[0].LocalType}");
                            return g.Name.Contains("GameUIStart_Patch") && g.GetMethodBody().LocalVariables.Count > 1 &&
                                   g.GetMethodBody().LocalVariables[0].LocalType == typeof(string[]) && 
                                   g.GetMethodBody().LocalVariables[1].LocalType == typeof(int);
                        }) != null;
                });
                if (uiStartType != null)
                {
                    Plugin.Log.LogInfo($"Found {uiStartType}! Patching...");
                    MethodBase method1 = AccessTools.FirstMethod(uiStartType,
                        g =>
                        {
                            return g.Name.Contains("GameUIStart_Patch") && g.GetMethodBody().LocalVariables.Count > 1 &&
                                   g.GetMethodBody().LocalVariables[0].LocalType == typeof(string[]) &&
                                   g.GetMethodBody().LocalVariables[1].LocalType == typeof(int);
                        });
                    if(method1 == null)
                        Plugin.Log.LogInfo("Method is null!");
                    else
                    {
                        harmony.Patch(method1, postfix: new HarmonyMethod(typeof(BetterTOSPatches), nameof(BetterTOSPatches.GameUIStartBetterTos)));
                    }
                }
                
                Type gameStartType0 = typeof(MyPatcher).GetNestedTypes(AccessTools.all).First(x =>
                {
                    //Log.LogInfo($"Type: {x}");
                    return AccessTools.FirstMethod(x,
                        g =>
                        {
                            //Log.LogInfo($"Method name: {g.Name}. Local variables: {g.GetMethodBody().LocalVariables.Count} items");
                            //if(g.GetMethodBody().LocalVariables.Count > 0)
                            //    Log.LogInfo($"Variable at index 0 type: {g.GetMethodBody().LocalVariables[0].LocalType}");
                            return g.Name.Contains("GameStart_Patch") && g.GetMethodBody().LocalVariables.Count > 1 &&
                                   g.GetMethodBody().LocalVariables[0].LocalType == typeof(string[]) && 
                                   g.GetMethodBody().LocalVariables[1].LocalType == typeof(Random);
                        }) != null;
                });
                if (gameStartType0 != null)
                {
                    Plugin.Log.LogInfo($"Found {gameStartType0}! Patching...");
                    MethodBase method1 = AccessTools.FirstMethod(gameStartType0,
                        g => g.Name.Contains("GameStart_Patch") && g.GetMethodBody().LocalVariables.Count > 1 &&
                             g.GetMethodBody().LocalVariables[0].LocalType == typeof(string[]) && 
                             g.GetMethodBody().LocalVariables[1].LocalType == typeof(Random));
                    if(method1 == null)
                        Plugin.Log.LogInfo("Method is null!");
                    else
                    {
                        harmony.Patch(method1, postfix: new HarmonyMethod(typeof(BetterTOSPatches), nameof(BetterTOSPatches.GameStartBetterTos)));
                    }
                }
    }
    
    
    
    internal static void JoinLobbyBetterTos(object sender, Message m)
    {
        try
        {
            if (m.Type != "systemMsg") return;
            var myStr = m.GetString(0U);
            if (myStr.Contains(" has been banned."))
                Plugin.CurrentLobbyModifiers.Add(new LobbyModifier()
                {
                    Type = LobbyModifierType.RoleBan,
                    Extra = myStr.Replace(" has been banned.", "").Replace("<b>", "")
                });
            else if (myStr.Contains(" has been unbanned."))
                foreach (var kv in Plugin.CurrentLobbyModifiers)
                {
                    if (kv.Type != LobbyModifierType.RoleBan ||
                        kv.Extra != myStr.Replace(" has been unbanned.", "").Replace("<b>", "")) continue;
                    Plugin.CurrentLobbyModifiers.Remove(kv);
                    return;
                }
            else if (myStr.Contains("Modifier Mafia TT enabled."))
                Plugin.CurrentLobbyModifiers.Add(new LobbyModifier
                {
                    Type = LobbyModifierType.Traitor
                });
            else if (myStr.Contains("Modifier Mafia TT disabled."))
            {
                foreach (var kv in Plugin.CurrentLobbyModifiers)
                {
                    if (kv.Type != LobbyModifierType.Traitor) continue;
                    Plugin.CurrentLobbyModifiers.Remove(kv);
                    return;
                }

                Plugin.Log.LogWarning(
                    $"[BetterTOS] Disabled Mafia TT modifier despite the modifier not existing in the first place?");
            }
            else if (myStr.Contains("Modifier VIP enabled."))
                Plugin.CurrentLobbyModifiers.Add(new LobbyModifier
                {
                    Type = LobbyModifierType.VIP
                });
            else if (myStr.Contains("Modifier VIP disabled."))
            {
                foreach (var kv in Plugin.CurrentLobbyModifiers)
                {
                    if (kv.Type != LobbyModifierType.VIP) continue;
                    Plugin.CurrentLobbyModifiers.Remove(kv);
                    return;
                }

                Plugin.Log.LogWarning(
                    $"[BetterTOS] Disabled VIP modifier despite the modifier not existing in the first place?");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[JoinLobbyBetterTos] {e}");
        }
        /*
            switch (myStr)
            {
                case "Modifier Mafia TT enabled.":
                    Plugin.CurrentLobbyModifiers.Add(new LobbyModifier()
                    {
                        Type = LobbyModifierType.Traitor
                    });
                    break;
                case "Modifier Mafia TT disabled.":
                    foreach(var kv in Plugin.CurrentLobbyModifiers)
                        if (kv.Type == LobbyModifierType.Traitor)
                        {
                            Plugin.CurrentLobbyModifiers.Remove(kv);
                            return;
                        }
                    Plugin.Log.LogWarning($"[BetterTOS] Disabled Mafia TT modifier despite the modifier not existing in the first place?");
                    break;
                case "Modifier VIP enabled.":
                    Plugin.CurrentLobbyModifiers.Add(new LobbyModifier()
                    {
                        Type = LobbyModifierType.VIP
                    });
                    break;
                case "Modifier VIP disabled.":
                    foreach(var kv in Plugin.CurrentLobbyModifiers)
                        if (kv.Type == LobbyModifierType.VIP)
                        {
                            Plugin.CurrentLobbyModifiers.Remove(kv);
                            return;
                        }
                    Plugin.Log.LogWarning($"[BetterTOS] Disabled VIP modifier despite the modifier not existing in the first place?");
                    break;
                default:
                    if(myStr.Contains(" has been banned."))
                        Plugin.CurrentLobbyModifiers.Add(new LobbyModifier()
                        {
                            Type = LobbyModifierType.RoleBan,
                            Extra = myStr.Replace(" has been banned.", "")
                        });
                    else if(myStr.Contains(" has been unbanned."))
                        foreach(var kv in Plugin.CurrentLobbyModifiers)
                            if (kv.Type == LobbyModifierType.RoleBan && kv.Extra == myStr.Replace(" has been unbanned.", ""))
                            {
                                Plugin.CurrentLobbyModifiers.Remove(kv);
                                return;
                            }
                    break;
            }*/
    }
    
    internal static void GameUIStartBetterTos(object sender, Message m)
    {
        try
        {
            switch (m.Type)
            {
                case "fixedTarget":
                case "roleChange":
                case "UserLeft":
                case "dead":
                case "RevealRole":
                case "PlayerLynched":
                case "GameOver":
                case "EndGameData":
                case "VIPPosition":
                case "YouAreTT":
                case "firstday":
                case "TeamJackal":
                case "dgvsvvs":
                case "IsAJackal":
                case "facmemb":
                    if (Plugin.OngoingGame is null)
                    {
                        Plugin.Log.LogWarning($"[BetterTOS] Ongoing game is null! Ignoring {m.Type}.");
                        return;
                    }

                    break;
            }

            switch (m.Type)
            {
                case "UserLeft":
                case "dead":
                case "RevealRole":
                case "PlayerLynched":
                case "GameOver":
                case "traitorPosition":
                case "YouAreTT":
                case "firstday":
                case "TeamJackal":
                case "dgvsvvs":
                case "IsAJackal":
                    if (Plugin.OngoingGame.GameState == GameSaveState.GameEnded)
                    {
                        Plugin.Log.LogWarning(
                            $"[BetterTOS] Ongoing game state is GameEnded, but I received message {m.Type}. Ignoring...");
                        return;
                    }

                    break;
            }

            switch (m.Type)
            {
                case "fixedTarget":
                    Plugin.OngoingGame.TargetPlayer =
                        GlobalServiceLocator.GameService.ActiveGameState.Me.FixedTarget.Position + 1;
                    break;
                case "roleChange":
                    Plugin.OngoingGame.Players[MyPatcher.myPosition - 1].PlayerRole = MyPatcher.myRole.Name;
                    Plugin.OngoingGame.MyFinalRole = MyPatcher.myRole.Name;
                    break;
                case "UserLeft":
                    Plugin.OngoingGame.Players[m.GetInt(0U)].PlayerOutcome = GameOutcome.Disconnection;
                    break;
                case "dead":
                    Plugin.OngoingGame.GameState = GameSaveState.OngoingDead;
                    Plugin.OngoingGame.AmAlive = false;
                    Plugin.SaveFile();
                    break;
                case "RevealRole":
                    if (Plugin.OngoingGame.Players[m.GetInt(0U) - 1].PlayerRole != "Unknown")
                        return;
                    Plugin.OngoingGame.Players[m.GetInt(0U) - 1].PlayerRole = GlobalServiceLocator.GameService
                        .ActiveGameState.Players[m.GetInt(0U) - 1].CurrentRole.Name;
                    break;
                case "PlayerLynched":
                    Plugin.OngoingGame.Players[m.GetInt(0U) - 1].PlayerRole = GlobalServiceLocator.GameService
                        .ActiveGameState.Players[m.GetInt(0U) - 1].CurrentRole.Name;
                    break;
                case "GameOver":
                    Plugin.OngoingGame.GameState = GameSaveState.GameEnded;
                    if (m.GetString(1U) != "none")
                    {
                        var array2 = m.GetString(1U).Split('$');
                        foreach (var t in array2)
                        {
                            var playerIndex = int.Parse(t) - 1;
                            var savedGamePlayer = Plugin.OngoingGame.Players[playerIndex];
                            savedGamePlayer.PlayerOutcome = GameOutcome.Victory;
                        }
                    }

                    break;
                case "EndGameData":
                    var activeEndGame = GlobalServiceLocator.GameService.ActiveEndGameData;

                    Plugin.OngoingGame.MyOutcome = (GameOutcome)activeEndGame.GameResult;
                    Plugin.OngoingGame.EndedAt = DateTime.Now;
                    foreach (var currPlr in activeEndGame.PartyMemberList)
                    {
                        var savedGamePlayer = Plugin.OngoingGame.Players[currPlr.Position];
                        savedGamePlayer.PlayerRole = MyPatcher.TheRules.Roles[currPlr.BeginningRoleId].Name;
                        savedGamePlayer.PlayerUsername = currPlr.AccountName;
                        if (currPlr.Position == Plugin.OngoingGame.MyPosition - 1)
                            Plugin.OngoingGame.MyFinalRole = MyPatcher.TheRules.Roles[currPlr.FinalRoleId].Name;
                    }

                    Plugin.OngoingGame = null;
                    Plugin.SaveFile();
                    break;
                case "VIPPosition":
                    if(m.GetInt(0U) + 1 == Plugin.OngoingGame.MyPosition)
                        Plugin.OngoingGame.AmTraitorOrVIP = true;
                    Plugin.OngoingGame.Players[m.GetInt(0U)].IsVIP = true;
                    break;
                case "traitorPosition":
                    if(m.GetInt(0U) + 1 == Plugin.OngoingGame.MyPosition)
                        Plugin.OngoingGame.AmTraitorOrVIP = true;
                    Plugin.OngoingGame.Players[m.GetInt(0U)].IsTT = true;
                    break;
                case "YouAreTT":
                    Plugin.OngoingGame.Players[GlobalServiceLocator.GameService.ActiveGameState.Me.Position].IsTT =
                        true;
                    Plugin.OngoingGame.AmTraitorOrVIP = true;
                    break;
                case "firstday":
                    Plugin.OngoingGame.GameState = GameSaveState.OngoingAlive;
                    if (!Plugin.OngoingGame.GameMode.StartsWith("Custom")) return;
                    Plugin.OngoingGame.CustomRoleList =
                        Plugin.GetCurrentRoleList(GlobalServiceLocator.GameService.ActiveGameState);
                    Plugin.OngoingGame.CustomRoleListMD5 = Plugin.CreateMD5(Plugin.GetCurrentRoleListOrdered(GlobalServiceLocator.GameService.ActiveGameState));
                    Plugin.CurrentLobbyModifiers.Clear();
                    break;
                case "TeamJackal":
                    Plugin.OngoingGame.AmTraitorOrVIP = true;
                    Plugin.OngoingGame.Players[m.GetInt(0U)].IsRecruit = true;
                    if (!Plugin.OngoingGame.MyPosition.HasValue)
                    {
                        Plugin.Log.LogWarning(
                            $"[BetterTOS] Joined the Jackal team, but my player's position is unknown!");
                        return;
                    }

                    Plugin.OngoingGame.Players[Plugin.OngoingGame.MyPosition.Value].IsRecruit = true;
                    break;
                case "dgvsvvs":
                    Plugin.OngoingGame.Players[m.GetInt(0U)].IsRecruit = true;
                    Plugin.OngoingGame.Players[m.GetInt(1U)].IsRecruit = true;
                    break;
                case "IsAJackal":
                    Plugin.OngoingGame.Players[m.GetInt(0U)].IsRecruit = true;
                    break;
                case "facmemb":
                    string[] array5 = m.GetString(0U).Split('&');
                    for (int n = 0; n < array5.Length; n++)
                    {
                        int num2 = int.Parse(array5[n].Split('$')[0]);
                        Plugin.OngoingGame.Players[num2 - 1].PlayerRole =
                            GlobalServiceLocator.GameService.ActiveGameState.Players[num2 - 1].CurrentRole.Name;
                    }

                    break;
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[GameUIStartBetterTos] {e}");
        }
    }
    
    internal static void GameStartBetterTos(object sender, Message m)
    {
        try
        {
            switch (m.Type)
            {
                case "gameData":
                {
                    var gameState = GlobalServiceLocator.GameService.ActiveGameState;
                    if (Plugin.OngoingGame is null)
                    {
                        Plugin.Log.LogInfo(
                            $"[BetterTOS] New game has started! Creating object from {m.Type}!");
                        Plugin.OngoingGame = new SavedGame()
                        {
                            GameState = GameSaveState.PickingNames,
                            MyStartingRole = "Unknown",
                            StartedAt = DateTime.Now,
                            MyOutcome = GameOutcome.TBD
                        };
                        Plugin.CurrentSavedGames.Add(Plugin.OngoingGame);
                    }

                    Plugin.OngoingGame.GameMode = gameState.GameMode.Name;
                    for (int playerIndex = 0; playerIndex < gameState.Players.Count; playerIndex++)
                    {
                        Plugin.OngoingGame.Players.Add(new SavedGamePlayer());
                        var gameStatePlayer = gameState.Players[playerIndex];
                        var ongoingPlayer = Plugin.OngoingGame.Players[playerIndex];
                        ongoingPlayer.PlayerUsername = gameStatePlayer.Name;
                        if (Plugin.OngoingGame.MyPosition.HasValue && playerIndex == Plugin.OngoingGame.MyPosition)
                            ongoingPlayer.PlayerRole = Plugin.OngoingGame.MyStartingRole;
                    }
                }
                    break;
                case "roleData":
                {
                    if (Plugin.OngoingGame is null)
                    {
                        Plugin.Log.LogInfo(
                            $"[BetterTOS] New game has started! Creating object from {m.Type}!");
                        Plugin.OngoingGame = new SavedGame()
                        {
                            GameState = GameSaveState.PickingNames,
                            StartedAt = DateTime.Now,
                            MyOutcome = GameOutcome.TBD
                        };

                        Plugin.CurrentSavedGames.Add(Plugin.OngoingGame);
                    }

                    //set all the names of every player to their nickname for the time being
                    Plugin.OngoingGame.GameState = GameSaveState.RoleReveal;
                    Plugin.OngoingGame.MyPosition = MyPatcher.myPosition;
                    Plugin.OngoingGame.MyName = MyPatcher.myName;
                    Plugin.OngoingGame.MyStartingRole = MyPatcher.myRole.Name;
                    Plugin.OngoingGame.MyFinalRole = MyPatcher.myRole.Name;
                    if(Plugin.OngoingGame.Players.Count > 0)
                        Plugin.OngoingGame.Players[MyPatcher.myPosition - 1].PlayerRole = MyPatcher.myRole.Name;

                    Plugin.SaveFile();
                }
                    break;
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[GameStartBetterTos] {e}");
        }
    }
}