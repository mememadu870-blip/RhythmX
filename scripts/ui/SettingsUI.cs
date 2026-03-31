using Godot;

namespace RhythmX;

/// <summary>
/// Settings UI Controller
/// </summary>
public partial class SettingsUI : Control
{
    private HSlider _audioOffsetSlider;
    private Label _offsetValueLabel;
    private HSlider _speedSlider;
    private Label _speedValueLabel;
    private HSlider _volumeSlider;
    private Label _volumeValueLabel;
    private CheckButton _fullscreenToggle;
    private CheckButton _showComboToggle;
    private CheckButton _showJudgmentToggle;
    private Label _accountInfoLabel;
    private Button _logoutButton;
    private Button _resetButton;
    private Button _backButton;
    
    public override void _Ready()
    {
        _audioOffsetSlider = GetNode<HSlider>("ScrollContainer/SettingsVBox/AudioSection/AudioOffsetSlider");
        _offsetValueLabel = GetNode<Label>("ScrollContainer/SettingsVBox/AudioSection/AudioOffsetContainer/OffsetValue");
        _speedSlider = GetNode<HSlider>("ScrollContainer/SettingsVBox/AudioSection/SpeedSlider");
        _speedValueLabel = GetNode<Label>("ScrollContainer/SettingsVBox/AudioSection/SpeedContainer/SpeedValue");
        _volumeSlider = GetNode<HSlider>("ScrollContainer/SettingsVBox/AudioSection/VolumeSlider");
        _volumeValueLabel = GetNode<Label>("ScrollContainer/SettingsVBox/AudioSection/VolumeContainer/VolumeValue");
        _fullscreenToggle = GetNode<CheckButton>("ScrollContainer/SettingsVBox/VisualSection/FullscreenToggle");
        _showComboToggle = GetNode<CheckButton>("ScrollContainer/SettingsVBox/VisualSection/ShowComboToggle");
        _showJudgmentToggle = GetNode<CheckButton>("ScrollContainer/SettingsVBox/VisualSection/ShowJudgmentToggle");
        _accountInfoLabel = GetNode<Label>("ScrollContainer/SettingsVBox/AccountSection/AccountInfo");
        _logoutButton = GetNode<Button>("ScrollContainer/SettingsVBox/AccountSection/LogoutButton");
        _resetButton = GetNode<Button>("ScrollContainer/SettingsVBox/ResetButton");
        _backButton = GetNode<Button>("Header/BackButton");
        
        if (_audioOffsetSlider != null)
            _audioOffsetSlider.ValueChanged += OnAudioOffsetChanged;
        if (_speedSlider != null)
            _speedSlider.ValueChanged += OnSpeedChanged;
        if (_volumeSlider != null)
            _volumeSlider.ValueChanged += OnVolumeChanged;
        if (_fullscreenToggle != null)
            _fullscreenToggle.Toggled += OnFullscreenToggled;
        if (_logoutButton != null)
            _logoutButton.Pressed += OnLogoutPressed;
        if (_resetButton != null)
            _resetButton.Pressed += OnResetPressed;
        if (_backButton != null)
            _backButton.Pressed += OnBackPressed;
        
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        var playerData = PlayerData.Load();
        
        if (_audioOffsetSlider != null)
            _audioOffsetSlider.Value = playerData.AudioOffset;
        if (_offsetValueLabel != null)
            _offsetValueLabel.Text = $"{playerData.AudioOffset:F0} ms";
        
        if (_speedSlider != null)
            _speedSlider.Value = playerData.NoteSpeed;
        if (_speedValueLabel != null)
            _speedValueLabel.Text = $"{playerData.NoteSpeed:F1}x";
        
        if (_fullscreenToggle != null)
            _fullscreenToggle.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
        
        UpdateAccountInfo();
    }
    
    private void UpdateAccountInfo()
    {
        if (_accountInfoLabel != null)
        {
            if (CloudManager.Instance != null && CloudManager.Instance.IsLoggedIn)
            {
                _accountInfoLabel.Text = $"Logged in as: {CloudManager.Instance.CurrentUserNickname}";
            }
            else
            {
                _accountInfoLabel.Text = "Not logged in";
            }
        }
    }
    
    private void OnAudioOffsetChanged(double value)
    {
        GameManager.Instance?.SetAudioOffset((float)value);
        if (_offsetValueLabel != null)
            _offsetValueLabel.Text = $"{value:F0} ms";
    }
    
    private void OnSpeedChanged(double value)
    {
        GameManager.Instance?.SetNoteSpeed((float)value);
        if (_speedValueLabel != null)
            _speedValueLabel.Text = $"{value:F1}x";
    }
    
    private void OnVolumeChanged(double value)
    {
        AudioManager.Instance?.SetMusicVolume((float)(value / 100.0));
        if (_volumeValueLabel != null)
            _volumeValueLabel.Text = $"{value:F0}%";
    }
    
    private void OnFullscreenToggled(bool isOn)
    {
        if (isOn)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
    }
    
    private void OnLogoutPressed()
    {
        CloudManager.Instance?.Logout();
        UpdateAccountInfo();
    }
    
    private void OnResetPressed()
    {
        if (_audioOffsetSlider != null)
            _audioOffsetSlider.Value = 0;
        if (_speedSlider != null)
            _speedSlider.Value = 1.0;
        if (_volumeSlider != null)
            _volumeSlider.Value = 100;
        
        GameManager.Instance?.SetAudioOffset(0);
        GameManager.Instance?.SetNoteSpeed(1.0f);
    }
    
    private void OnBackPressed()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }
}