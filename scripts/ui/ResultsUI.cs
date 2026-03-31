using Godot;

namespace RhythmX;

/// <summary>
/// Results UI Controller
/// </summary>
public partial class ResultsUI : Control
{
    // Node references
    private Label _gradeLabel;
    private Label _scoreValueLabel;
    private Label _maxComboValueLabel;
    private Label _perfectValueLabel;
    private Label _greatValueLabel;
    private Label _goodValueLabel;
    private Label _missValueLabel;
    private Label _accuracyValueLabel;
    private Label _fullComboBadge;
    private Label _allPerfectBadge;
    private Label _newRecordBadge;
    private Button _retryButton;
    private Button _nextButton;
    private Button _backButton;
    private Label _songNameLabel;
    private Label _artistLabel;
    
    private ScoreResult _result;
    private SongData _song;
    private GameManager.Difficulty _difficulty;
    
    public override void _Ready()
    {
        // Get node references
        _gradeLabel = GetNode<Label>("VBoxContainer/GradeContainer/GradeLabel");
        _scoreValueLabel = GetNode<Label>("VBoxContainer/ScoreContainer/ScoreValue");
        _maxComboValueLabel = GetNode<Label>("VBoxContainer/StatsContainer/MaxComboValue");
        _perfectValueLabel = GetNode<Label>("VBoxContainer/StatsContainer/PerfectValue");
        _greatValueLabel = GetNode<Label>("VBoxContainer/StatsContainer/GreatValue");
        _goodValueLabel = GetNode<Label>("VBoxContainer/StatsContainer/GoodValue");
        _missValueLabel = GetNode<Label>("VBoxContainer/StatsContainer/MissValue");
        _accuracyValueLabel = GetNode<Label>("VBoxContainer/StatsContainer/AccuracyValue");
        _fullComboBadge = GetNode<Label>("VBoxContainer/BadgesContainer/FullComboBadge");
        _allPerfectBadge = GetNode<Label>("VBoxContainer/BadgesContainer/AllPerfectBadge");
        _newRecordBadge = GetNode<Label>("VBoxContainer/GradeContainer/NewRecordBadge");
        _retryButton = GetNode<Button>("VBoxContainer/ButtonContainer/RetryButton");
        _nextButton = GetNode<Button>("VBoxContainer/ButtonContainer/NextButton");
        _backButton = GetNode<Button>("VBoxContainer/ButtonContainer/BackButton");
        _songNameLabel = GetNode<Label>("SongInfo/SongName");
        _artistLabel = GetNode<Label>("SongInfo/Artist");
        
        // Connect signals
        if (_retryButton != null)
            _retryButton.Pressed += OnRetryPressed;
        if (_nextButton != null)
            _nextButton.Pressed += OnNextPressed;
        if (_backButton != null)
            _backButton.Pressed += OnBackPressed;
        
        LoadResults();
        PlayResultAnimation();
    }
    
    private void LoadResults()
    {
        _result = ScoreManager.Instance?.GetResult();
        _song = GameManager.Instance?.CurrentSong;
        _difficulty = GameManager.Instance?.CurrentDifficulty ?? GameManager.Difficulty.Normal;
        
        // If no result, create mock
        if (_result == null)
        {
            _result = new ScoreResult
            {
                Score = 985420,
                MaxCombo = 256,
                PerfectCount = 180,
                GreatCount = 30,
                GoodCount = 10,
                MissCount = 5,
                Accuracy = 0.985f,
                Grade = "S",
                IsFullCombo = false,
                IsAllPerfect = false
            };
        }
    }
    
    private void PlayResultAnimation()
    {
        // Animate score counter
        AnimateScore();
        
        // Show grade after delay
        GetTree().CreateTimer(0.5).Timeout += ShowGrade;
        
        // Show stats after delay
        GetTree().CreateTimer(0.8).Timeout += ShowStats;
        
        // Show badges
        GetTree().CreateTimer(1.2).Timeout += ShowBadges;
    }
    
    private void AnimateScore()
    {
        if (_scoreValueLabel == null) return;
        
        int targetScore = _result.Score;
        int currentScore = 0;
        float duration = 1.5f;
        float elapsed = 0f;
        
        var tween = CreateTween();
        tween.TweenMethod(new Callable(this, nameof(SetScoreDisplay)), 0, targetScore, duration);
    }
    
    private void SetScoreDisplay(int score)
    {
        if (_scoreValueLabel != null)
            _scoreValueLabel.Text = score.ToString("N0");
    }
    
    private void ShowGrade()
    {
        if (_gradeLabel == null) return;
        
        _gradeLabel.Text = _result.Grade;
        
        // Set grade color
        _gradeLabel.Modulate = _result.Grade switch
        {
            "S+" => new Color(1f, 0.85f, 0f),
            "S" => new Color(1f, 0.85f, 0f),
            "A" => new Color(0.3f, 1f, 0.5f),
            "B" => new Color(0.5f, 0.8f, 1f),
            "C" => new Color(0.8f, 0.6f, 0.4f),
            _ => new Color(0.6f, 0.4f, 0.4f)
        };
    }
    
    private void ShowStats()
    {
        if (_maxComboValueLabel != null)
            _maxComboValueLabel.Text = _result.MaxCombo.ToString();
        
        if (_perfectValueLabel != null)
            _perfectValueLabel.Text = _result.PerfectCount.ToString();
        
        if (_greatValueLabel != null)
            _greatValueLabel.Text = _result.GreatCount.ToString();
        
        if (_goodValueLabel != null)
            _goodValueLabel.Text = _result.GoodCount.ToString();
        
        if (_missValueLabel != null)
            _missValueLabel.Text = _result.MissCount.ToString();
        
        if (_accuracyValueLabel != null)
            _accuracyValueLabel.Text = $"{_result.Accuracy * 100:F2}%";
        
        // Song info
        if (_song != null)
        {
            if (_songNameLabel != null)
                _songNameLabel.Text = _song.Name;
            if (_artistLabel != null)
                _artistLabel.Text = _song.Artist;
        }
    }
    
    private void ShowBadges()
    {
        if (_result.IsFullCombo && _fullComboBadge != null)
        {
            _fullComboBadge.Visible = true;
        }
        
        if (_result.IsAllPerfect && _allPerfectBadge != null)
        {
            _allPerfectBadge.Visible = true;
        }
        
        // Check for new record
        if (_song != null)
        {
            var playerData = PlayerData.Load();
            var record = playerData.GetRecord(_song.Id);
            if (record == null || _result.Score > record.HighScore)
            {
                if (_newRecordBadge != null)
                    _newRecordBadge.Visible = true;
            }
        }
    }
    
    private void OnRetryPressed()
    {
        var chart = _song?.GetChart(_difficulty);
        if (_song != null && chart != null)
        {
            GameManager.Instance?.StartGame(_song, chart, _difficulty);
        }
    }
    
    private void OnNextPressed()
    {
        // Go to next song in library
        var songs = SongLibrary.Instance?.Songs;
        if (songs == null || songs.Count == 0)
        {
            GameManager.Instance?.ReturnToMainMenu();
            return;
        }
        
        // Find current song index and get next
        int currentIndex = songs.FindIndex(s => s.Id == _song?.Id);
        int nextIndex = (currentIndex + 1) % songs.Count;
        var nextSong = songs[nextIndex];
        
        var chart = nextSong?.GetChart(_difficulty);
        if (nextSong != null && chart != null)
        {
            GameManager.Instance?.StartGame(nextSong, chart, _difficulty);
        }
        else
        {
            GameManager.Instance?.ChangeState(GameManager.GameState.SongSelection);
        }
    }
    
    private void OnBackPressed()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }
}