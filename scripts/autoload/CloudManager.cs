using Godot;

namespace RhythmX;

/// <summary>
/// Cloud sync manager (mock implementation)
/// </summary>
public partial class CloudManager : Node
{
    public static CloudManager Instance { get; private set; }
    
    public bool IsOnline { get; private set; } = true;
    public bool IsLoggedIn { get; private set; }
    public string CurrentUserId { get; private set; }
    public string CurrentUserNickname { get; private set; }
    
    public override void _Ready()
    {
        Instance = this;
        LoadAuthState();
    }
    
    private void LoadAuthState()
    {
        CurrentUserId = ConfigFile.GetValue("auth", "user_id", "").AsString();
        string token = ConfigFile.GetValue("auth", "token", "").AsString();
        IsLoggedIn = !string.IsNullOrEmpty(CurrentUserId) && !string.IsNullOrEmpty(token);
    }
    
    public async void Login(string phone, string otp)
    {
        // Mock login
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        
        CurrentUserId = "mock_user_" + phone.GetHashCode();
        CurrentUserNickname = "Player";
        IsLoggedIn = true;
        
        ConfigFile.SetValue("auth", "user_id", CurrentUserId);
        ConfigFile.SetValue("auth", "token", "mock_token");
        ConfigFile.Save("user://settings.cfg");
    }
    
    public void Logout()
    {
        CurrentUserId = null;
        CurrentUserNickname = null;
        IsLoggedIn = false;
        
        ConfigFile.SetValue("auth", "user_id", "");
        ConfigFile.SetValue("auth", "token", "");
        ConfigFile.Save("user://settings.cfg");
    }
    
    private ConfigFile _configFile = new();
    private ConfigFile ConfigFile
    {
        get
        {
            if (!_configLoaded)
            {
                _configFile.Load("user://settings.cfg");
                _configLoaded = true;
            }
            return _configFile;
        }
    }
    private bool _configLoaded = false;
}