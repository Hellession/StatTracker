namespace StatTracker;

public class SavedGamePlayer
{
    public string PlayerRole
    {
        get => _playerRole;
        set
        {
            string additionals = "";
            if (IsVIP)
                additionals += "VIP ";
            if (IsTT)
                additionals += "TT ";
            if (IsRecruit)
                additionals += "Recruit ";
            if (string.IsNullOrEmpty(value))
                _playerRole = additionals + "Unknown";
            else
                _playerRole = additionals + value;
        }
    }

    private string _playerRole = "Unknown";

    /// <summary>
    /// The ToS-wide username, not the game nickname
    /// </summary>
    public string PlayerUsername { get; set; } = "";

    public GameOutcome PlayerOutcome { get; set; } = GameOutcome.Defeat;
    
    public bool IsVIP
    {
        get => _isVIP;
        set
        {
            if (value)
                PlayerRole = $"VIP {PlayerRole}";
            _isVIP = value;
        }
    }

    private bool _isVIP;

    public bool IsTT
    {
        get => _isTT;
        set
        {
            if (value)
                PlayerRole = $"TT {PlayerRole}";
            _isTT = value;
        }
    }
    private bool _isTT;
    
    public bool IsRecruit
    {
        get => _isRecruit;
        set
        {
            if (value)
                PlayerRole = $"Recruit {PlayerRole}";
            _isRecruit = value;
        }
    }
    private bool _isRecruit;
}