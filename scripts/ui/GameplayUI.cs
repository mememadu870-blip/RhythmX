using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Gameplay UI Controller
/// </summary>
public partial class GameplayUI : Control
{
    // Node references
    private Node2D _notesContainer;
    private Label _scoreValueLabel;
    private Label _comboValueLabel;
    private Label _judgmentLabel;
    private Label _songNameLabel;
    private ProgressBar _progressBar;
    private Button _pauseButton;
    private PanelContainer _pauseMenu;
    private Line2D _judgmentLine;
    
    private SongData _currentSong;
    private ChartData _currentChart;
    private GameManager.Difficulty _difficulty;
    
    private readonly List<NoteObject> _activeNotes = new();
    private readonly Queue<NoteData> _noteQueue = new();
    private double _currentTime;
    private float _noteSpeed = 400f;
    private float _fallDistance = 800f;
    
    // Note prefab
    private PackedScene _noteScene;
    
    public override void _Ready()
    {
        // Get node references
        _notesContainer = GetNode<Node2D>("TrackContainer/NotesContainer");
        _scoreValueLabel = GetNode<Label>("ScoreContainer/ScoreValue");
        _comboValueLabel = GetNode<Label>("ComboContainer/ComboValue");
        _judgmentLabel = GetNode<Label>("JudgmentLabel");
        _songNameLabel = GetNode<Label>("TopBar/SongName");
        _progressBar = GetNode<ProgressBar>("ProgressBar");
        _pauseButton = GetNode<Button>("PauseButton");
        _pauseMenu = GetNode<PanelContainer>("PauseMenu");
        
        // Connect signals
        if (_pauseButton != null)
            _pauseButton.Pressed += OnPausePressed;
        
        // Connect pause menu buttons
        var resumeButton = GetNode<Button>("PauseMenu/PauseVBox/ResumeButton");
        var restartButton = GetNode<Button>("PauseMenu/PauseVBox/RestartButton");
        var quitButton = GetNode<Button>("PauseMenu/PauseVBox/QuitButton");
        
        if (resumeButton != null)
            resumeButton.Pressed += OnResumePressed;
        if (restartButton != null)
            restartButton.Pressed += OnRestartPressed;
        if (quitButton != null)
            quitButton.Pressed += OnQuitPressed;
        
        // Create note scene
        CreateNoteScene();
        
        LoadCurrentSong();
        StartGame();
    }
    
    private void CreateNoteScene()
    {
        // Create a simple note scene programmatically
        var note = new NoteObject();
        _noteScene = new PackedScene();
        // In real implementation, would load from file
    }
    
    private void LoadCurrentSong()
    {
        _currentSong = GameManager.Instance?.CurrentSong;
        _currentChart = GameManager.Instance?.CurrentChart;
        _difficulty = GameManager.Instance?.CurrentDifficulty ?? GameManager.Difficulty.Normal;
        
        if (_currentSong == null || _currentChart == null)
        {
            GD.PrintErr("No song or chart loaded!");
            return;
        }
        
        // Queue notes
        foreach (var note in _currentChart.Notes)
        {
            _noteQueue.Enqueue(note);
        }
        
        // Update UI
        if (_songNameLabel != null)
            _songNameLabel.Text = _currentSong.Name;
        
        // Initialize score manager
        ScoreManager.Instance?.Initialize(_currentChart.TotalNotes);
        
        // Subscribe to events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnJudgment += OnJudgment;
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
            ScoreManager.Instance.OnComboChanged += OnComboChanged;
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSongEnd += OnSongEnd;
        }
    }
    
    private void StartGame()
    {
        // Start audio after delay
        GetTree().CreateTimer(1.5).Timeout += () =>
        {
            AudioManager.Instance?.Play();
        };
    }
    
    public override void _Process(double delta)
    {
        if (AudioManager.Instance == null || !AudioManager.Instance.IsPlaying)
            return;
        
        _currentTime = AudioManager.Instance.CurrentTime;
        
        // Spawn notes
        SpawnNotes();
        
        // Update active notes
        UpdateNotes((float)delta);
        
        // Update progress bar
        if (_progressBar != null && AudioManager.Instance != null)
        {
            double duration = AudioManager.Instance.SongDuration;
            if (duration > 0)
            {
                _progressBar.Value = (_currentTime / duration) * 100;
            }
        }
    }
    
    private void SpawnNotes()
    {
        double spawnTime = _currentTime + (_fallDistance / _noteSpeed);
        
        while (_noteQueue.Count > 0)
        {
            var note = _noteQueue.Peek();
            if (note.Time <= spawnTime)
            {
                _noteQueue.Dequeue();
                SpawnNote(note);
            }
            else
            {
                break;
            }
        }
    }
    
    private void SpawnNote(NoteData noteData)
    {
        // Create note visual
        var note = new NoteObject
        {
            Data = noteData,
            TrackWidth = 180f
        };
        
        // Position note
        float x = noteData.Track * 180f + 90f; // Center of track
        float y = -(float)(noteData.Time - _currentTime) * _noteSpeed;
        
        note.Position = new Vector2(x, y);
        
        // Set color based on type
        var color = noteData.Type switch
        {
            NoteType.Hold => new Color(1f, 0.8f, 0.2f),
            NoteType.Swipe => new Color(0.8f, 0.4f, 1f),
            _ => new Color(0.4f, 1f, 1f)
        };
        note.Color = color;
        
        _notesContainer?.AddChild(note);
        _activeNotes.Add(note);
    }
    
    private void UpdateNotes(float delta)
    {
        var notesToRemove = new List<NoteObject>();
        
        foreach (var note in _activeNotes)
        {
            // Update position
            float y = -(float)(note.Data.Time - _currentTime) * _noteSpeed;
            note.Position = new Vector2(note.Position.X, y);
            
            // Check if missed
            if (note.Data.Time < _currentTime - 0.15)
            {
                ScoreManager.Instance?.ProcessMiss();
                notesToRemove.Add(note);
            }
        }
        
        // Remove missed notes
        foreach (var note in notesToRemove)
        {
            _activeNotes.Remove(note);
            note.QueueFree();
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        
        // Handle track inputs
        for (int i = 0; i < 4; i++)
        {
            if (Input.IsActionJustPressed($"tap_{i + 1}"))
            {
                HandleTrackInput(i);
            }
        }
        
        // Handle pause
        if (Input.IsActionJustPressed("pause"))
        {
            if (_pauseMenu != null && _pauseMenu.Visible)
                OnResumePressed();
            else
                OnPausePressed();
        }
    }
    
    private void HandleTrackInput(int track)
    {
        // Find closest note on this track
        NoteObject closest = null;
        double minDiff = double.MaxValue;
        
        foreach (var note in _activeNotes)
        {
            if (note.Data.Track == track && note.Data.Time > _currentTime - 0.2)
            {
                double diff = Math.Abs(note.Data.Time - _currentTime);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = note;
                }
            }
        }
        
        if (closest != null && minDiff < 0.15)
        {
            var judgment = ScoreManager.Instance?.JudgeHit(_currentTime, closest.Data.Time) 
                ?? ScoreManager.Judgment.Miss;
            
            // Play hit sound
            // AudioManager.Instance?.PlaySfx(hitSound);
            
            // Remove note
            _activeNotes.Remove(closest);
            closest.QueueFree();
        }
    }
    
    private void OnJudgment(ScoreManager.Judgment judgment, int combo)
    {
        if (_judgmentLabel != null)
        {
            _judgmentLabel.Text = judgment.ToString().ToUpper();
            
            // Set color
            _judgmentLabel.Modulate = judgment switch
            {
                ScoreManager.Judgment.Perfect => new Color(0.6f, 1f, 1f),
                ScoreManager.Judgment.Great => new Color(1f, 0.9f, 0.3f),
                ScoreManager.Judgment.Good => new Color(0.5f, 0.8f, 0.5f),
                ScoreManager.Judgment.Miss => new Color(1f, 0.3f, 0.3f),
                _ => new Color(1f, 1f, 1f)
            };
            
            // Fade out after delay
            GetTree().CreateTimer(0.5).Timeout += () =>
            {
                _judgmentLabel.Text = "";
            };
        }
    }
    
    private void OnScoreChanged(int score)
    {
        if (_scoreValueLabel != null)
            _scoreValueLabel.Text = score.ToString("N0");
    }
    
    private void OnComboChanged(int combo)
    {
        if (_comboValueLabel != null)
            _comboValueLabel.Text = combo > 0 ? combo.ToString() : "";
    }
    
    private void OnSongEnd()
    {
        // Save results
        var result = ScoreManager.Instance?.GetResult();
        if (result != null && _currentSong != null)
        {
            var playerData = PlayerData.Load();
            playerData.UpdateRecord(_currentSong.Id, result.Score, result.MaxCombo, result.Grade, _difficulty);
            playerData.TotalPlayCount++;
            playerData.Save();
            
            // Check achievements
            AchievementManager.Instance?.OnSongClear(_currentSong.Id, result, _difficulty);
        }
        
        // Go to results
        GameManager.Instance?.ChangeState(GameManager.GameState.Results);
    }
    
    private void OnPausePressed()
    {
        AudioManager.Instance?.Pause();
        if (_pauseMenu != null)
            _pauseMenu.Visible = true;
        GetTree().Paused = true;
    }
    
    private void OnResumePressed()
    {
        GetTree().Paused = false;
        if (_pauseMenu != null)
            _pauseMenu.Visible = false;
        AudioManager.Instance?.Resume();
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
    }
}

/// <summary>
/// Visual representation of a note
/// </summary>
public partial class NoteObject : Node2D
{
    public NoteData Data { get; set; }
    public float TrackWidth { get; set; } = 180f;
    public Color Color { get; set; } = new Color(0.4f, 1f, 1f);
    
    public override void _Ready()
    {
        // Draw note shape
    }
    
    public override void _Draw()
    {
        var size = Data?.Type switch
        {
            NoteType.Hold => new Vector2(TrackWidth - 20, 100),
            NoteType.Swipe => new Vector2(TrackWidth - 20, 50),
            _ => new Vector2(TrackWidth - 20, 30)
        };
        
        DrawRect(new Rect2(-size.X / 2, -size.Y / 2, size.X, size.Y), Color);
        
        // Draw hold tail
        if (Data?.Type == NoteType.Hold)
        {
            float holdLength = (float)(Data.Duration * 400);
            DrawRect(new Rect2(-size.X / 2, size.Y / 2, size.X, holdLength), 
                new Color(Color.R, Color.G, Color.B, 0.5f));
        }
    }
}