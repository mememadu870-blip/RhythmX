using Godot;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Song Selection UI Controller
/// </summary>
public partial class SongSelectionUI : Control
{
    // Node references
    private VBoxContainer _songListContainer;
    private Label _songNameLabel;
    private Label _artistLabel;
    private Label _bpmValueLabel;
    private Label _durationValueLabel;
    private Label _highScoreValueLabel;
    private Label _noteCountLabel;
    private Button[] _difficultyButtons = new Button[4];
    private Button _playButton;
    private Button _backButton;
    
    private List<SongData> _songs;
    private SongData _selectedSong;
    private GameManager.Difficulty _selectedDifficulty = GameManager.Difficulty.Normal;
    
    public override void _Ready()
    {
        // Get node references
        _songListContainer = GetNode<VBoxContainer>("MainContainer/SongListContainer/SongList/SongListVBox");
        _songNameLabel = GetNode<Label>("MainContainer/DetailContainer/DetailVBox/SongInfo/SongName");
        _artistLabel = GetNode<Label>("MainContainer/DetailContainer/DetailVBox/SongInfo/Artist");
        _bpmValueLabel = GetNode<Label>("MainContainer/DetailContainer/DetailVBox/SongInfo/StatsGrid/BPMValue");
        _durationValueLabel = GetNode<Label>("MainContainer/DetailContainer/DetailVBox/SongInfo/StatsGrid/DurationValue");
        _highScoreValueLabel = GetNode<Label>("MainContainer/DetailContainer/DetailVBox/SongInfo/StatsGrid/HighScoreValue");
        _noteCountLabel = GetNode<Label>("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/NoteCountLabel");
        _playButton = GetNode<Button>("BottomBar/PlayButton");
        _backButton = GetNode<Button>("Header/BackButton");
        
        // Get difficulty buttons
        _difficultyButtons[0] = GetNode<Button>("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/EasyButton");
        _difficultyButtons[1] = GetNode<Button>("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/NormalButton");
        _difficultyButtons[2] = GetNode<Button>("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/HardButton");
        _difficultyButtons[3] = GetNode<Button>("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/ExpertButton");
        
        // Connect signals
        if (_backButton != null)
            _backButton.Pressed += OnBackPressed;
        
        if (_playButton != null)
            _playButton.Pressed += OnPlayPressed;
        
        for (int i = 0; i < _difficultyButtons.Length; i++)
        {
            int diff = i;
            if (_difficultyButtons[i] != null)
                _difficultyButtons[i].Pressed += () => OnDifficultySelected(diff);
        }
        
        LoadSongs();
    }
    
    private void LoadSongs()
    {
        _songs = SongLibrary.Instance?.Songs ?? new List<SongData>();
        
        // Populate song list
        if (_songListContainer != null)
        {
            foreach (var child in _songListContainer.GetChildren())
            {
                child.QueueFree();
            }
            
            foreach (var song in _songs)
            {
                var button = new Button
                {
                    Text = $"{song.Name}\n{song.Artist}",
                    CustomMinimumSize = new Vector2(280, 60)
                };
                button.Pressed += () => SelectSong(song);
                _songListContainer.AddChild(button);
            }
        }
        
        // Select first song
        if (_songs.Count > 0)
        {
            SelectSong(_songs[0]);
        }
    }
    
    private void SelectSong(SongData song)
    {
        _selectedSong = song;
        UpdateSongDetails();
        UpdateDifficultyButtons();
    }
    
    private void UpdateSongDetails()
    {
        if (_selectedSong == null) return;
        
        if (_songNameLabel != null)
            _songNameLabel.Text = _selectedSong.Name;
        
        if (_artistLabel != null)
            _artistLabel.Text = _selectedSong.Artist;
        
        if (_bpmValueLabel != null)
            _bpmValueLabel.Text = _selectedSong.Bpm.ToString("F0");
        
        if (_durationValueLabel != null)
        {
            var duration = System.TimeSpan.FromSeconds(_selectedSong.Duration);
            _durationValueLabel.Text = duration.ToString(@"mm\:ss");
        }
        
        if (_highScoreValueLabel != null)
        {
            _highScoreValueLabel.Text = _selectedSong.HighScore > 0 
                ? _selectedSong.HighScore.ToString("N0") 
                : "---";
        }
    }
    
    private void UpdateDifficultyButtons()
    {
        if (_selectedSong == null) return;
        
        for (int i = 0; i < 4; i++)
        {
            var diff = (GameManager.Difficulty)i;
            bool hasChart = _selectedSong.HasChart(diff);
            
            if (_difficultyButtons[i] != null)
            {
                _difficultyButtons[i].Disabled = !hasChart;
            }
        }
        
        // Select default difficulty
        if (_selectedSong.HasChart(GameManager.Difficulty.Normal))
        {
            OnDifficultySelected((int)GameManager.Difficulty.Normal);
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                if (_selectedSong.HasChart((GameManager.Difficulty)i))
                {
                    OnDifficultySelected(i);
                    break;
                }
            }
        }
    }
    
    private void OnDifficultySelected(int difficulty)
    {
        _selectedDifficulty = (GameManager.Difficulty)difficulty;
        
        // Update button colors
        for (int i = 0; i < _difficultyButtons.Length; i++)
        {
            if (_difficultyButtons[i] != null)
            {
                bool isSelected = i == difficulty;
                var color = isSelected ? GameManager.DifficultyColors[i] : new Color(0.6f, 0.6f, 0.6f);
                // Update button appearance
            }
        }
        
        // Update note count
        if (_noteCountLabel != null && _selectedSong != null)
        {
            var chart = _selectedSong.GetChart(_selectedDifficulty);
            _noteCountLabel.Text = chart != null ? $"Notes: {chart.TotalNotes}" : "Notes: N/A";
        }
    }
    
    private void OnPlayPressed()
    {
        if (_selectedSong == null) return;
        
        var chart = _selectedSong.GetChart(_selectedDifficulty);
        if (chart == null) return;
        
        GameManager.Instance?.StartGame(_selectedSong, chart, _selectedDifficulty);
    }
    
    private void OnBackPressed()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }
}