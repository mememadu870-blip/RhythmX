using Godot;

namespace RhythmX;

/// <summary>
/// Main Menu UI Controller
/// </summary>
public partial class MainMenuUI : Control
{
    // Node references
    private Label _userNameLabel;
    private Label _userStatsLabel;
    private Label _achievementProgressLabel;
    
    public override void _Ready()
    {
        // Get node references
        _userNameLabel = GetNode<Label>("UserInfoContainer/UserInfo/UserName");
        _userStatsLabel = GetNode<Label>("UserInfoContainer/UserInfo/UserStats");
        _achievementProgressLabel = GetNode<Label>("AchievementProgress/AchievementLabel");
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update user info
        var playerData = PlayerData.Load();
        
        if (_userNameLabel != null)
        {
            _userNameLabel.Text = playerData.Nickname ?? "Guest";
        }
        
        if (_userStatsLabel != null)
        {
            _userStatsLabel.Text = $"Played: {playerData.TotalPlayCount} songs";
        }
        
        // Update achievement progress
        if (_achievementProgressLabel != null)
        {
            int unlocked = AchievementManager.Instance?.GetUnlockedCount() ?? 0;
            _achievementProgressLabel.Text = $"{unlocked} achievements unlocked";
        }
    }
    
    // These methods are called by scene signals - must be public for Godot signals
    public void OnPlayPressed()
    {
        GD.Print("OnPlayPressed called");
        GameManager.Instance?.ChangeState(GameManager.GameState.SongSelection);
    }
    
    public void OnImportPressed()
    {
        GD.Print("OnImportPressed called");
        ImportSongUI.Instance?.ShowImportDialog();
    }
    
    public void OnEditorPressed()
    {
        GD.Print("OnEditorPressed called");
        GameManager.Instance?.ChangeState(GameManager.GameState.ChartEditor);
    }
    
    public void OnAchievementsPressed()
    {
        GD.Print("OnAchievementsPressed called");
        GameManager.Instance?.ChangeState(GameManager.GameState.Achievements);
    }
    
    public void OnSettingsPressed()
    {
        GD.Print("OnSettingsPressed called");
        GameManager.Instance?.ChangeState(GameManager.GameState.Settings);
    }
}