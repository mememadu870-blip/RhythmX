using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Gameplay UI Controller - Main gameplay scene manager
/// Handles note spawning, hit detection, scoring, and UI updates
/// </summary>
public partial class GameplayUI : Control
{
    public static GameplayUI Instance { get; private set; }
    
    // Node references
    private Node2D _notesContainer;
    private Control _trackContainer;
    private Label _scoreValueLabel;
    private Label _comboValueLabel;
    private Label _judgmentLabel;
    private Label _songNameLabel;
    private Label _difficultyLabel;
    private ProgressBar _progressBar;
    private Button _pauseButton;
    private PanelContainer _pauseMenu;
    
    // Track lane visuals
    private Control[] _trackLanes;
    
    // Current song data
    private SongData _currentSong;
    private ChartData _currentChart;
    private GameManager.Difficulty _difficulty;
    
    // Note management
    private readonly List<NoteObject> _activeNotes = new();
    private readonly Queue<NoteData> _noteQueue = new();
    private double _currentTime;
    
    // Note settings
    private float _noteSpeed = 400f;
    private float _fallDistance = 800f;
    private float _hitLineY = 400f;
    private float _spawnY = -400f;
    private float _despawnY = 500f;
    
    // Timing windows (in seconds)
    private double _perfectWindow = 0.045;
    private double _greatWindow = 0.090;
    private double _goodWindow = 0.135;
    
    // Game state
    private bool _isPlaying;
    private bool _isPaused;
    private bool _gameStarted;
    
    // Hit sound
    private AudioStream _hitSound;
    
    public override void _Ready()
    {
        Instance = this;
        
        GetNodeReferences();
        SetupTrackLanes();
        ConnectSignals();
        LoadCurrentSong();
        CreateHitSound();
        
        // Start game after delay
        GetTree().CreateTimer(1.5).Timeout += BeginPlayback;
    }
    
    private void GetNodeReferences()
    {
        _notesContainer = GetNode<Node2D>("TrackContainer/NotesContainer");
        _trackContainer = GetNode<Control>("TrackContainer");
        _scoreValueLabel = GetNode<Label>("ScoreContainer/ScoreValue");
        _comboValueLabel = GetNode<Label>("ComboContainer/ComboValue");
        _judgmentLabel = GetNode<Label>("JudgmentLabel");
        _songNameLabel = GetNode<Label>("TopBar/SongName");
        _difficultyLabel = GetNode<Label>("TopBar/Difficulty");
        _progressBar = GetNode<ProgressBar>("ProgressBar");
        _pauseButton = GetNode<Button>("PauseButton");
        _pauseMenu = GetNode<PanelContainer>("PauseMenu");
        
        // Hide judgment label initially
        if (_judgmentLabel != null)
        {
            _judgmentLabel.Visible = false;
        }
        
        // Hide pause menu
        if (_pauseMenu != null)
        {
            _pauseMenu.Visible = false;
        }
    }
    
    private void SetupTrackLanes()
    {
        int trackCount = _currentChart?.TrackCount ?? 4;
        _trackLanes = new Control[trackCount];
        
        float trackWidth = _trackContainer?.Size.X ?? 720f;
        float laneWidth = trackWidth / trackCount;
        
        // Create lane visuals if not existing
        for (int i = 0; i < trackCount; i++)
        {
            string lanePath = $"TrackContainer/Lane{i}";
            var lane = GetNode<Control>(lanePath);
            
            if (lane == null)
            {
                // Create lane dynamically
                var colorRect = new ColorRect();
                colorRect.Name = $"Lane{i}";
                colorRect.Size = new Vector2(laneWidth, 800f);
                colorRect.Position = new Vector2(i * laneWidth, 0);
                colorRect.Color = new Color(TrackManager.Instance?.GetTrackColor(i) ?? Colors.Cyan, 0.1f);
                _trackContainer?.AddChild(colorRect);
                lane = colorRect;
            }
            
            _trackLanes[i] = lane;
        }
        
        // TrackManager uses TrackCount property
    }
    
    private void ConnectSignals()
    {
        // Pause button
        if (_pauseButton != null)
        {
            _pauseButton.Pressed += OnPausePressed;
        }
        
        // Pause menu buttons
        var resumeButton = GetNode<Button>("PauseMenu/PauseVBox/ResumeButton");
        var restartButton = GetNode<Button>("PauseMenu/PauseVBox/RestartButton");
        var quitButton = GetNode<Button>("PauseMenu/PauseVBox/QuitButton");
        
        if (resumeButton != null)
            resumeButton.Pressed += OnResumePressed;
        if (restartButton != null)
            restartButton.Pressed += OnRestartPressed;
        if (quitButton != null)
            quitButton.Pressed += OnQuitPressed;
        
        // Subscribe to score events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnJudgment += OnJudgment;
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
            ScoreManager.Instance.OnComboChanged += OnComboChanged;
        }
        
        // Subscribe to audio events
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSongEnd += OnSongEnd;
        }
    }
    
    private void CreateHitSound()
    {
        // Create a simple hit sound programmatically
        // In production, this would be loaded from resources
        // _hitSound = GD.Load<AudioStream>("res://assets/sounds/hit.wav");
    }
    
    private void LoadCurrentSong()
    {
        _currentSong = GameManager.Instance?.CurrentSong;
        _currentChart = GameManager.Instance?.CurrentChart;
        _difficulty = GameManager.Instance?.CurrentDifficulty ?? GameManager.Difficulty.Normal;
        
        if (_currentSong == null || _currentChart == null)
        {
            GD.PrintErr("No song or chart loaded!");
            ReturnToMenu();
            return;
        }
        
        // Queue notes
        var sortedNotes = new List<NoteData>(_currentChart.Notes);
        sortedNotes.Sort((a, b) => a.Time.CompareTo(b.Time));
        
        _noteQueue.Clear();
        foreach (var note in sortedNotes)
        {
            _noteQueue.Enqueue(note);
        }
        
        // Update UI
        if (_songNameLabel != null)
            _songNameLabel.Text = _currentSong.Name;
        
        if (_difficultyLabel != null)
        {
            _difficultyLabel.Text = GameManager.DifficultyNames[(int)_difficulty];
            _difficultyLabel.Modulate = GameManager.DifficultyColors[(int)_difficulty];
        }
        
        // Initialize score manager
        ScoreManager.Instance?.Initialize(_currentChart.TotalNotes);
        
        // Load audio
        if (!string.IsNullOrEmpty(_currentSong.AudioPath))
        {
            AudioManager.Instance?.LoadSongFromPath(_currentSong.AudioPath, _currentChart.Bpm, _currentChart.Offset);
        }
        
        // Apply note speed from settings
        _noteSpeed = GameManager.Instance?.NoteSpeed * 400f ?? 400f;
    }
    
    private void BeginPlayback()
    {
        _gameStarted = true;
        _isPlaying = true;
        _isPaused = false;
        
        AudioManager.Instance?.Play();
    }
    
    public override void _Process(double delta)
    {
        if (!_gameStarted) return;
        
        if (!_isPlaying || _isPaused) return;
        
        _currentTime = AudioManager.Instance?.CurrentTime ?? 0;
        
        // Spawn new notes
        SpawnNotes();
        
        // Update active notes
        UpdateNotes(delta);
        
        // Check for missed notes
        CheckMissedNotes();
        
        // Update progress bar
        UpdateProgress();
        
        // Update hold notes in TrackManager
        TrackManager.Instance?.UpdateHoldNotes();
    }
    
    private void SpawnNotes()
    {
        double spawnTime = _currentTime + (_fallDistance / _noteSpeed);
        
        while (_noteQueue.Count > 0)
        {
            var noteData = _noteQueue.Peek();
            
            if (noteData.Time <= spawnTime)
            {
                _noteQueue.Dequeue();
                SpawnNote(noteData);
            }
            else
            {
                break;
            }
        }
    }
    
    private void SpawnNote(NoteData noteData)
    {
        var note = new NoteObject();
        note.Data = noteData;
        note.TrackWidth = _trackContainer?.Size.X / (_currentChart?.TrackCount ?? 4) ?? 180f;
        note.SetSpeed(_noteSpeed);
        note.Initialize(noteData, _hitLineY, _despawnY);
        note.SetSpawnPosition(_spawnY);
        
        // Calculate initial position
        float x = CalculateTrackX(noteData.Track);
        double timeDiff = noteData.Time - _currentTime;
        float y = _hitLineY - (float)(timeDiff * _noteSpeed);
        
        note.Position = new Vector2(x, y);
        
        _notesContainer?.AddChild(note);
        _activeNotes.Add(note);
    }
    
    private float CalculateTrackX(int track)
    {
        float trackWidth = _trackContainer?.Size.X ?? 720f;
        int trackCount = _currentChart?.TrackCount ?? 4;
        float laneWidth = trackWidth / trackCount;
        
        return track * laneWidth + laneWidth / 2;
    }
    
    private void UpdateNotes(double delta)
    {
        foreach (var note in _activeNotes)
        {
            if (note == null || note.WasHit || note.WasMissed) continue;
            
            note.UpdatePosition(_currentTime);
        }
    }
    
    private void CheckMissedNotes()
    {
        double missTime = _currentTime - _goodWindow;
        
        var missedNotes = new List<NoteObject>();
        
        foreach (var note in _activeNotes)
        {
            if (note == null || note.WasHit || note.WasMissed) continue;
            
            if (note.Data.Time < missTime)
            {
                missedNotes.Add(note);
            }
        }
        
        foreach (var note in missedNotes)
        {
            HandleMiss(note);
        }
    }
    
    private void UpdateProgress()
    {
        if (_progressBar == null) return;
        
        double duration = AudioManager.Instance?.SongDuration ?? 1;
        if (duration > 0)
        {
            _progressBar.Value = (_currentTime / duration) * 100;
        }
    }
    
    #region Input Handling
    
    public void HandleTrackInput(int track)
    {
        if (!_isPlaying || _isPaused) return;
        
        // Find closest hittable note on this track
        NoteObject closestNote = null;
        double minTimeDiff = double.MaxValue;
        
        foreach (var note in _activeNotes)
        {
            if (note == null || note.WasHit || note.WasMissed) continue;
            if (note.Data.Track != track) continue;
            
            double timeDiff = Math.Abs(note.Data.Time - _currentTime);
            
            if (timeDiff < minTimeDiff && timeDiff <= _goodWindow)
            {
                minTimeDiff = timeDiff;
                closestNote = note;
            }
        }
        
        if (closestNote != null)
        {
            HitNote(closestNote);
        }
    }
    
    public void HandleHoldRelease(int track)
    {
        // Handle early release of hold note
        var lane = TrackManager.Instance?.GetLane(track);
        if (lane?.HoldNote != null)
        {
            var note = lane.HoldNote;
            double currentTime = AudioManager.Instance?.CurrentTime ?? 0;
            
            // Check if held long enough
            double holdProgress = (currentTime - note.Data.Time) / note.Data.Duration;
            
            if (holdProgress < 0.8)
            {
                // Early release - mark as miss
                note.ReleaseHold();
                ScoreManager.Instance?.ProcessMiss();
                RemoveNote(note);
            }
        }
    }
    
    private void HitNote(NoteObject note)
    {
        if (note == null || note.WasHit) return;
        
        var judgment = ScoreManager.Instance?.JudgeHit(_currentTime, note.Data.Time)
            ?? ScoreManager.Judgment.Miss;
        
        // Play hit sound
        if (_hitSound != null)
        {
            AudioManager.Instance?.PlayHitSound(_hitSound);
        }
        
        // Handle hold notes
        if (note.Data.Type == NoteType.Hold)
        {
            note.StartHold();
            TrackManager.Instance?.SetHoldingNote(note.Data.Track, note);
        }
        else
        {
            note.MarkHit();
            RemoveNote(note);
        }
        
        // Show hit effect
        TrackManager.Instance?.ShowHitEffect(note.Data.Track, judgment);
    }
    
    private void HandleMiss(NoteObject note)
    {
        if (note == null || note.WasMissed) return;
        
        note.MarkMissed();
        ScoreManager.Instance?.ProcessMiss();
        
        // Show miss effect
        TrackManager.Instance?.ShowMissEffect(note.Data.Track);
        
        RemoveNote(note);
    }
    
    private void RemoveNote(NoteObject note)
    {
        _activeNotes.Remove(note);
        // Note will be freed by its animation
    }
    
    #endregion
    
    #region Visual Effects
    
    public void ShowHitEffect(int track, ScoreManager.Judgment judgment, float x)
    {
        Vector2 position = new Vector2(x, _hitLineY);
        EffectManager.Instance?.PlayHitEffect(judgment, position, _notesContainer);
        
        // Flash lane
        if (_trackLanes != null && track < _trackLanes.Length)
        {
            EffectManager.Instance?.FlashLane(_trackLanes[track], judgment);
        }
        
        // Combo effect
        int combo = ScoreManager.Instance?.Combo ?? 0;
        if (combo >= 50)
        {
            EffectManager.Instance?.PlayComboEffect(combo, position, _notesContainer);
        }
    }
    
    public void ShowMissEffect(int track, float x)
    {
        Vector2 position = new Vector2(x, _hitLineY);
        EffectManager.Instance?.PlayHitEffect(ScoreManager.Judgment.Miss, position, _notesContainer);
    }
    
    public void HighlightTrack(int track, bool highlight, Color color)
    {
        if (_trackLanes == null || track >= _trackLanes.Length) return;
        
        var lane = _trackLanes[track];
        if (lane == null) return;
        
        if (highlight)
        {
            EffectManager.Instance?.GlowLane(lane, color, 0.3f);
        }
        else
        {
            EffectManager.Instance?.ResetLaneGlow(lane);
        }
    }
    
    #endregion
    
    #region Score Events
    
    private void OnJudgment(ScoreManager.Judgment judgment, int combo)
    {
        if (_judgmentLabel == null) return;
        
        _judgmentLabel.Text = judgment.ToString().ToUpper();
        _judgmentLabel.Modulate = EffectManager.Instance?.GetJudgmentColor(judgment) ?? Colors.White;
        _judgmentLabel.Visible = true;
        
        // Scale animation
        var tween = _judgmentLabel.CreateTween();
        tween.TweenProperty(_judgmentLabel, "scale", new Vector2(1.3f, 1.3f), 0.05f);
        tween.TweenProperty(_judgmentLabel, "scale", new Vector2(1f, 1f), 0.15f);
        
        // Hide after delay
        GetTree().CreateTimer(0.5f).Timeout += () =>
        {
            _judgmentLabel.Visible = false;
        };
    }
    
    private void OnScoreChanged(int score)
    {
        if (_scoreValueLabel != null)
        {
            _scoreValueLabel.Text = score.ToString("N0");
        }
    }
    
    private void OnComboChanged(int combo)
    {
        if (_comboValueLabel == null) return;
        
        _comboValueLabel.Text = combo > 0 ? combo.ToString() : "";
        
        if (combo >= 100)
        {
            // Big combo animation
            var tween = _comboValueLabel.CreateTween();
            tween.TweenProperty(_comboValueLabel, "scale", new Vector2(1.5f, 1.5f), 0.1f);
            tween.TweenProperty(_comboValueLabel, "scale", new Vector2(1f, 1f), 0.2f);
        }
    }
    
    #endregion
    
    #region Pause Control
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.IsActionPressed("pause") || keyEvent.Keycode == Key.Escape)
            {
                if (_isPaused)
                    OnResumePressed();
                else
                    OnPausePressed();
            }
        }
    }
    
    private void OnPausePressed()
    {
        if (!_isPlaying) return;
        
        _isPaused = true;
        AudioManager.Instance?.Pause();
        
        if (_pauseMenu != null)
        {
            _pauseMenu.Visible = true;
        }
        
        GetTree().Paused = true;
    }
    
    private void OnResumePressed()
    {
        _isPaused = false;
        AudioManager.Instance?.Resume();
        
        if (_pauseMenu != null)
        {
            _pauseMenu.Visible = false;
        }
        
        GetTree().Paused = false;
    }
    
    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
    
    private void OnQuitPressed()
    {
        GetTree().Paused = false;
        GameManager.Instance?.ReturnToMainMenu();
    }
    
    #endregion
    
    #region Song End
    
    private void OnSongEnd()
    {
        _isPlaying = false;
        
        // Get results
        var result = ScoreManager.Instance?.GetResult();
        
        if (result != null && _currentSong != null)
        {
            // Save results
            var playerData = PlayerData.Load();
            playerData.UpdateRecord(_currentSong.Id, result.Score, result.MaxCombo, result.Grade, _difficulty);
            playerData.TotalPlayCount++;
            playerData.Save();
            
            // Check achievements
            CheckAchievements(result);
        }
        
        // Go to results scene
        GameManager.Instance?.ChangeState(GameManager.GameState.Results);
    }
    
    private void CheckAchievements(ScoreResult result)
    {
        if (AchievementManager.Instance == null) return;
        
        if (result.IsFullCombo)
        {
            AchievementManager.Instance.UnlockAchievement("first_full_combo");
        }
        
        if (result.IsAllPerfect)
        {
            AchievementManager.Instance.UnlockAchievement("first_all_perfect");
        }
        
        if (result.Grade == "S+" || result.Grade == "S")
        {
            AchievementManager.Instance.UnlockAchievement("first_s_rank");
        }
        
        if (result.MaxCombo >= 100)
        {
            AchievementManager.Instance.UnlockAchievement("combo_100");
        }
        if (result.MaxCombo >= 500)
        {
            AchievementManager.Instance.UnlockAchievement("combo_500");
        }
        if (result.MaxCombo >= 1000)
        {
            AchievementManager.Instance.UnlockAchievement("combo_1000");
        }
    }
    
    #endregion
    
    private void ReturnToMenu()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }
    
    public override void _ExitTree()
    {
        // Unsubscribe from events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnJudgment -= OnJudgment;
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
            ScoreManager.Instance.OnComboChanged -= OnComboChanged;
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSongEnd -= OnSongEnd;
        }
        
        // Clear notes
        foreach (var note in _activeNotes)
        {
            if (note != null && IsInstanceValid(note))
            {
                note.QueueFree();
            }
        }
        _activeNotes.Clear();
        _noteQueue.Clear();
    }
}