namespace StatTracker;

public class LobbyModifier
{
    public LobbyModifierType Type { get; set; }
    
    public string Extra { get; set; }

    public override string ToString()
    {
        switch (Type)
        {
            case LobbyModifierType.Traitor:
                return "Mafia TT modifier";
            case LobbyModifierType.RoleBan:
                return $"Banned {Extra}";
            case LobbyModifierType.VIP:
                return $"VIP Modifier";
        }

        return "Unknown modifier";
    }
}

public enum LobbyModifierType
{
    RoleBan,
    Traitor,
    VIP
}