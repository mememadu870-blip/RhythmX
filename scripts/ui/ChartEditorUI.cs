using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RhythmX;

/// <summary>
/// Chart Editor UI Controller - Create and edit rhythm charts
/// </summary>
public partial class ChartEditorUI : Control
{
    // Node references
    private Label _songNameLabel;
    private Label _bpmLabel;
    private Node2D _notesContainer;
    private Control _trackContainer;
    private VSlider _timelineSlider;
    private Label _timeLabel;
    private Button _tapTool;
    private Button _holdTool;
    private Button _swipeTool;
    private Button _eraseTool;
    private Button _undoButton;
    private Button _redoButton;
    private Button _clearButton;
    private Button _playButton;
    private Button _stopButton;
    private Button _saveButton;
    private Button _exportButton;
    private Button _backButton;
    
    // Chart data
    private ChartData _currentChart;
    private string _audioPath;
    private NoteType _currentTool = NoteType.Tap;
    private bool _eraseMode;
    private bool _isPlaying;
    private double _currentTime;
    private double _duration = 180;
    
    // Note storage
    private readonly List<NoteData> _notes = new();
    private readonly Stack<List<NoteData>> _undoStack = new();
    private readonly Stack<List<NoteData>> _redoStack = new();
    
    // Grid settings
    private float _pixelsPerSecond = 100f;
    private float _trackWidth = 130f;
    
    public override void _Ready()
    {
        GetNodeReferences();
        ConnectSignals();
        CreateNewChart();
    }
    
    private void GetNodeReferences()
    {
        _songNameLabel = GetNode<Label>("Header/SongName");
        _bpmLabel = GetNode<Label>("Header/BPMLabel");
        _notesContainer = GetNode<Node2D>("TrackContainer/NotesContainer");
        _trackContainer = GetNode<Control>("TrackContainer");
        _timelineSlider = GetNode<VSlider>("TimelineContainer/TimelineSlider");
        _timeLabel = GetNode<Label>("TimelineContainer/TimeLabel");
        _tapTool = GetNode<Button>("ToolContainer/ToolVBox/TapTool");
        _holdTool = GetNode<Button>("ToolContainer/ToolVBox/HoldTool");
        _swipeTool = GetNode<Button>("ToolContainer/ToolVBox/SwipeTool");
        _eraseTool = GetNode<Button>("ToolContainer/ToolVBox/EraseTool");
        _undoButton = GetNode<Button>("ToolContainer/ToolVBox/UndoButton");
        _redoButton = GetNode<Button>("ToolContainer/ToolVBox/RedoButton");
        _clearButton = GetNode<Button>("ToolContainer/ToolVBox/ClearButton");
        _playButton = GetNode<Button>("BottomBar/PlayButton");
        _stopButton = GetNode<Button>("BottomBar/StopButton");
        _saveButton = GetNode<Button>("BottomBar/SaveButton");
        _exportButton = GetNode<Button>("BottomBar/ExportButton");
        _backButton = GetNode<Button>("Header/BackButton");
    }
    
    private void ConnectSignals()
    {
        // Tool buttons
        if (_tapTool != null)
            _tapTool.Pressed += () => SelectTool(NoteType.Tap, false);
        if (_holdTool != null)
            _holdTool.Pressed += () => SelectTool(NoteType.Hold, false);
        if (_swipeTool != null)
            _swipeTool.Pressed += () => SelectTool(NoteType.Swipe, false);
        if (_eraseTool != null)
            _eraseTool.Pressed += () => SelectTool(NoteType.Tap, true);
        
        // Action buttons
        if (_undoButton != null)
            _undoButton.Pressed += OnUndoPressed;
        if (_redoButton != null)
            _redoButton.Pressed += OnRedoPressed;
        if (_clearButton != null)
            _clearButton.Pressed += OnClearPressed;
        if (GetNode<Button>("ToolContainer/ToolVBox/LoadAudioButton") is Button loadAudioBtn)
            loadAudioBtn.Pressed += OnLoadAudioPressed;
        if (_playButton != null)
            _playButton.Pressed += OnPlayPressed;
        if (_stopButton != null)
            _stopButton.Pressed += OnStopPressed;
        if (_saveButton != null)
            _saveButton.Pressed += OnSavePressed;
        if (_exportButton != null)
            _exportButton.Pressed += OnExportPressed;
        if (_backButton != null)
            _backButton.Pressed += OnBackPressed;
        
        // Timeline slider
        if (_timelineSlider != null)
            _timelineSlider.ValueChanged += OnTimelineChanged;
    }
    
    private void CreateNewChart()
    {
        _currentChart = new ChartData
        {
            Id = Guid.NewGuid().ToString(),
            Difficulty = GameManager.Difficulty.Normal,
            TrackCount = 4,
            Bpm = 120
        };
        
        _duration = 180;
        UpdateUI();
        DrawGridLines();
    }
    
    private void UpdateUI()
    {
        if (_songNameLabel != null)
            _songNameLabel.Text = string.IsNullOrEmpty(_audioPath) ? "New Chart" : System.IO.Path.GetFileNameWithoutExtension(_audioPath);
        
        if (_bpmLabel != null && _currentChart != null)
            _bpmLabel.Text = $"BPM: {_currentChart.Bpm:F0}";
        
        UpdateTimeLabel();
    }
    
    private void UpdateTimeLabel()
    {
        if (_timeLabel != null)
        {
            var time = TimeSpan.FromSeconds(_currentTime);
            _timeLabel.Text = $"{time.Minutes}:{time.Seconds:D2}";
        }
    }
    
    #region Tool Selection
    
    private void SelectTool(NoteType tool, bool eraseMode)
    {
        _currentTool = tool;
        _eraseMode = eraseMode;
        
        // Update button visuals
        if (_tapTool != null)
            _tapTool.Modulate = (!eraseMode && tool == NoteType.Tap) ? new Color(0.4f, 1f, 1f) : new Color(1f, 1f, 1f);
        if (_holdTool != null)
            _holdTool.Modulate = (!eraseMode && tool == NoteType.Hold) ? new Color(0.4f, 1f, 1f) : new Color(1f, 1f, 1f);
        if (_swipeTool != null)
            _swipeTool.Modulate = (!eraseMode && tool == NoteType.Swipe) ? new Color(0.4f, 1f, 1f) : new Color(1f, 1f, 1f);
        if (_eraseTool != null)
            _eraseTool.Modulate = eraseMode ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 1f, 1f);
    }
    
    #endregion
    
    #region Input Handling
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (_trackContainer == null) return;
            
            var localPos = _trackContainer.GetLocalMousePosition();
            if (_trackContainer.GetRect().HasPoint(localPos))
            {
                int track = (int)(localPos.X / _trackWidth);
                if (track >= 0 && track < 4)
                {
                    if (_eraseMode)
                    {
                        EraseNoteAt(_currentTime, track);
                    }
                    else
                    {
                        AddNote(_currentTime, track);
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Note Management
    
    private void AddNote(double time, int track)
    {
        SaveUndoState();
        
        var note = new NoteData
        {
            Time = time,
            Track = track,
            Type = _currentTool
        };
        
        if (_currentTool == NoteType.Hold)
        {
            note.EndTime = time + 0.5;
        }
        else if (_currentTool == NoteType.Swipe)
        {
            note.SwipeDirection = (SwipeDirection)new Random().Next(1, 5);
        }
        
        _notes.Add(note);
        _notes.Sort((a, b) => a.Time.CompareTo(b.Time));
        
        RefreshNoteDisplay();
        GD.Print($"Added {_currentTool} note at {time:F2}s on track {track}");
    }
    
    private void EraseNoteAt(double time, int track)
    {
        double tolerance = 0.1;
        
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            var note = _notes[i];
            if (note.Track == track && Math.Abs(note.Time - time) < tolerance)
            {
                SaveUndoState();
                _notes.RemoveAt(i);
                RefreshNoteDisplay();
                GD.Print($"Erased note at {time:F2}s on track {track}");
                return;
            }
        }
    }
    
    private void SaveUndoState()
    {
        var state = new List<NoteData>();
        foreach (var note in _notes)
        {
            state.Add(new NoteData
            {
                Time = note.Time,
                EndTime = note.EndTime,
                Track = note.Track,
                Type = note.Type,
                SwipeDirection = note.SwipeDirection
            });
        }
        _undoStack.Push(state);
        _redoStack.Clear();
    }
    
    private void RefreshNoteDisplay()
    {
        if (_notesContainer == null) return;
        
        // Clear existing notes
        foreach (var child in _notesContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // Draw notes
        foreach (var note in _notes)
        {
            DrawNote(note);
        }
    }
    
    private void DrawNote(NoteData note)
    {
        // Create note visual
        var noteRect = new ColorRect();
        
        float x = note.Track * _trackWidth + 10;
        float y = (float)note.Time * _pixelsPerSecond;
        float width = _trackWidth - 20;
        float height = 20;
        
        if (note.Type == NoteType.Hold)
        {
            height = (float)(note.Duration * _pixelsPerSecond);
        }
        
        noteRect.Position = new Vector2(x, y);
        noteRect.Size = new Vector2(width, height);
        
        // Color based on type
        noteRect.Color = note.Type switch
        {
            NoteType.Hold => new Color(1f, 0.8f, 0.2f),
            NoteType.Swipe => new Color(0.8f, 0.4f, 1f),
            _ => new Color(0.4f, 1f, 1f)
        };
        
        _notesContainer.AddChild(noteRect);
    }
    
    #endregion
    
    #region Grid Drawing
    
    private void DrawGridLines()
    {
        var gridNode = GetNode<Node2D>("TrackContainer/GridLines");
        if (gridNode == null) return;
        
        // Clear existing lines
        foreach (var child in gridNode.GetChildren())
        {
            child.QueueFree();
        }
        
        // Draw beat lines
        if (_currentChart == null) return;
        
        double beatInterval = 60.0 / _currentChart.Bpm;
        int beatCount = (int)(_duration / beatInterval);
        
        for (int i = 0; i <= beatCount; i++)
        {
            double time = i * beatInterval;
            float y = (float)time * _pixelsPerSecond;
            
            var line = new Line2D();
            line.AddPoint(new Vector2(0, y));
            line.AddPoint(new Vector2(520, y));
            line.DefaultColor = new Color(0.3f, 0.3f, 0.3f, i % 4 == 0 ? 0.5f : 0.2f);
            line.Width = i % 4 == 0 ? 2f : 1f;
            
            gridNode.AddChild(line);
        }
    }
    
    #endregion
    
    #region Actions
    
    private void OnUndoPressed()
    {
        if (_undoStack.Count == 0) return;
        
        var state = _undoStack.Pop();
        _redoStack.Push(new List<NoteData>(_notes));
        _notes.Clear();
        _notes.AddRange(state);
        RefreshNoteDisplay();
    }
    
    private void OnRedoPressed()
    {
        if (_redoStack.Count == 0) return;
        
        var state = _redoStack.Pop();
        _undoStack.Push(new List<NoteData>(_notes));
        _notes.Clear();
        _notes.AddRange(state);
        RefreshNoteDisplay();
    }
    
    private void OnClearPressed()
    {
        if (_notes.Count == 0) return;
        
        SaveUndoState();
        _notes.Clear();
        RefreshNoteDisplay();
    }
    
    private void OnLoadAudioPressed()
    {
        var dialog = new FileDialog();
        dialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        dialog.Access = FileDialog.AccessEnum.Filesystem;
        dialog.Filters = new[] { "*.wav", "*.ogg" };
        dialog.Title = "Load Audio File";
        
        AddChild(dialog);
        dialog.FileSelected += (path) =>
        {
            LoadAudioFile(path);
            dialog.QueueFree();
        };
        dialog.Canceled += () => dialog.QueueFree();
        
        dialog.Show();
    }
    
    private void OnPlayPressed()
    {
        _isPlaying = true;
        // Play audio if loaded
        if (!string.IsNullOrEmpty(_audioPath))
        {
            AudioManager.Instance?.LoadSongFromPath(_audioPath, _currentChart?.Bpm ?? 120, 0);
            AudioManager.Instance?.Seek(_currentTime);
            AudioManager.Instance?.Play();
        }
    }
    
    private void OnStopPressed()
    {
        _isPlaying = false;
        _currentTime = 0;
        AudioManager.Instance?.Stop();
        UpdateTimeLabel();
        RefreshNoteDisplay();
    }
    
    private void OnSavePressed()
    {
        if (_currentChart == null) return;
        
        _currentChart.Notes = new List<NoteData>(_notes);
        
        // Save to user directory
        DirAccess.MakeDirAbsolute("user://charts");
        string path = $"user://charts/{_currentChart.Id}.json";
        
        var json = JsonSerializer.Serialize(_currentChart, new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print($"Chart saved to: {path}");
        }
        
        // Add to player data
        var playerData = PlayerData.Load();
        if (!playerData.CreatedCharts.Contains(_currentChart.Id))
        {
            playerData.CreatedCharts.Add(_currentChart.Id);
            playerData.Save();
            
            AchievementManager.Instance?.OnChartCreated();
        }
    }
    
    private void OnExportPressed()
    {
        // Export to shareable format
        if (_currentChart == null || _notes.Count == 0)
        {
            GD.Print("No notes to export!");
            return;
        }
        
        string exportPath = $"user://exports/{_currentChart.Id}_export.json";
        DirAccess.MakeDirAbsolute("user://exports");
        
        var exportData = new
        {
            chartId = _currentChart.Id,
            bpm = _currentChart.Bpm,
            difficulty = _currentChart.Difficulty.ToString(),
            notes = _notes.ConvertAll(n => new
            {
                time = n.Time,
                endTime = n.EndTime,
                track = n.Track,
                type = n.Type.ToString(),
                swipeDirection = n.SwipeDirection.ToString()
            })
        };
        
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(exportPath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print($"Chart exported to: {exportPath}");
        }
    }
    
    private void OnBackPressed()
    {
        AudioManager.Instance?.Stop();
        GameManager.Instance?.ReturnToMainMenu();
    }
    
    private void OnTimelineChanged(double value)
    {
        _currentTime = value / 100.0 * _duration;
        UpdateTimeLabel();
        UpdateNotesPosition();
    }
    
    private void UpdateNotesPosition()
    {
        if (_notesContainer == null) return;
        _notesContainer.Position = new Vector2(0, -(float)_currentTime * _pixelsPerSecond);
    }
    
    #endregion
    
    #region Process
    
    public override void _Process(double delta)
    {
        if (!_isPlaying) return;
        
        _currentTime += delta;
        
        if (_currentTime >= _duration)
        {
            _currentTime = _duration;
            _isPlaying = false;
        }
        
        UpdateTimeLabel();
        UpdateNotesPosition();
        
        if (_timelineSlider != null && !_timelineSlider.HasFocus())
        {
            _timelineSlider.Value = _currentTime / _duration * 100;
        }
    }
    
    #endregion
    
    #region Public API
    
    public void LoadAudioFile(string path)
    {
        _audioPath = path;
        
        // Analyze BPM
        _ = LoadAudioAsync(path);
    }
    
    private async System.Threading.Tasks.Task LoadAudioAsync(string path)
    {
        double bpm = await AudioAnalysis.Instance.AnalyzeBpmFromFileAsync(path);
        _currentChart.Bpm = bpm;
        
        // Get duration
        string ext = path.GetExtension().ToLower();
        if (ext == "wav")
        {
            var stream = AudioStreamWav.LoadFromFile(path);
            _duration = stream.GetLength();
        }
        else if (ext == "ogg")
        {
            var stream = AudioStreamOggVorbis.LoadFromFile(path);
            _duration = stream.GetLength();
        }
        
        UpdateUI();
        DrawGridLines();
        GD.Print($"Loaded audio: BPM={bpm:F0}, Duration={_duration:F1}s");
    }
    
    public void LoadChart(ChartData chart)
    {
        _currentChart = chart;
        _notes.Clear();
        _notes.AddRange(chart.Notes);
        RefreshNoteDisplay();
        UpdateUI();
    }
    
    #endregion
}