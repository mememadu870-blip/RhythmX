using Godot;

namespace RhythmX;

/// <summary>
/// Main Menu UI Controller
/// </summary>
public partial class MainMenuUI : Control
{
    // Node references
    private Button _playButton;
    private Button _importButton;
    private Button _editorButton;
    private Button _achievementsButton;
    private Button _settingsButton;
    private Label _userNameLabel;
    private Label _userStatsLabel;
    private Label _achievementProgressLabel;
    
    public override void _Ready()
    {
        // Get button references
        _playButton = GetNode<Button>("CenterContainer/ButtonContainer/PlayButton");
        _importButton = GetNode<Button>("CenterContainer/ButtonContainer/ImportButton");
        _editorButton = GetNode<Button>("CenterContainer/ButtonContainer/EditorButton");
        _achievementsButton = GetNode<Button>("CenterContainer/ButtonContainer/AchievementsButton");
        _settingsButton = GetNode<Button>("CenterContainer/ButtonContainer/SettingsButton");
        _userNameLabel = GetNode<Label>("UserInfoContainer/UserInfo/UserName");
        _userStatsLabel = GetNode<Label>("UserInfoContainer/UserInfo/UserStats");
        _achievementProgressLabel = GetNode<Label>("AchievementProgress/AchievementLabel");
        
        // Connect signals
        if (_playButton != null)
            _playButton.Pressed += OnPlayPressed;
        if (_importButton != null)
            _importButton.Pressed += OnImportPressed;
        if (_editorButton != null)
            _editorButton.Pressed += OnEditorPressed;
        if (_achievementsButton != null)
            _achievementsButton.Pressed += OnAchievementsPressed;
        if (_settingsButton != null)
            _settingsButton.Pressed += OnSettingsPressed;
        
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
        if (_achievementProgressLabel != null && AchievementManager.Instance != null)
        {
            int unlocked = AchievementManager.Instance.GetUnlockedCount();
            _achievementProgressLabel.Text = $"{unlocked} achievements unlocked";
        }
    }
    
    private void OnPlayPressed()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.SongSelection);
    }
    
    private void OnImportPressed()
    {
        ImportSongUI.Instance?.ShowImportDialog();
    }
    
    private void OnEditorPressed()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.ChartEditor);
    }
    
    private void OnAchievementsPressed()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Achievements);
    }
    
    private void OnSettingsPressed()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Settings);
    }
}