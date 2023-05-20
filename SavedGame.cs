using System;
using System.Collections.Generic;

namespace StatTracker;

public class SavedGame
{
    public string MyStartingRole { get; set; } = "";
    public string MyFinalRole { get; set; } = "";
    public string MyName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; } = null;
    public GameSaveState GameState { get; set; }
    public int? MyPosition { get; set; }
    public bool AmAlive { get; set; } = true;
    public GameOutcome MyOutcome { get; set; }
    public string GameMode { get; set; } = "";
    public bool AmTraitorOrVIP { get; set; }
    public int? TargetPlayer { get; set; }
    public List<SavedGamePlayer> Players { get; } = new List<SavedGamePlayer>();
    
    public string EquippedScroll1 { get; set; }
    
    public string EquippedScroll2 { get; set; }
    
    public string EquippedScroll3 { get; set; }

    public string CustomRoleList { get; set; } = "";

    public string CustomRoleListMD5 { get; set; } = "";

    public int Versioning { get; set; } = 3;

    public string GetCSVValue()
    {
        List<string> allPlayersEntries = new List<string>();
        for (int i = 0; i < 15; i++)
        {
            if (i < Players.Count)
            {
                var plr = Players[i];
                allPlayersEntries.Add(plr.PlayerUsername);
                allPlayersEntries.Add(plr.PlayerRole);
                allPlayersEntries.Add(plr.PlayerOutcome.ToString());
            }
            else
            {
                allPlayersEntries.Add("");
                allPlayersEntries.Add("");
                allPlayersEntries.Add("");
            }
        }

        return
            $"{GameMode},{MyStartingRole},{MyOutcome},{AmTraitorOrVIP},{TargetPlayer},{GameState},{AmAlive},{MyPosition},{MyName},{MyFinalRole},{StartedAt},{EndedAt},\"{CustomRoleList}\",{CustomRoleListMD5},{EquippedScroll1},{EquippedScroll2},{EquippedScroll3},{Versioning},{string.Join(",", allPlayersEntries)}";
    }
}