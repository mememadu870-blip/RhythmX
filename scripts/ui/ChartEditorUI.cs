using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace RhythmX;

/// <summary>
/// Chart Editor UI Controller
/// </summary>
public partial class ChartEditorUI : Control
{
    // Node references
    private Label _songNameLabel;
    private Label _bpmLabel;
    private Node2D _notesContainer;
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
    private Button _backButton;
    
    private ChartData _currentChart;
    private NoteType _currentTool = NoteType.Tap;
    private bool _isPlaying;
    private double _currentTime;
    
    private readonly List<NoteData> _notes = new();
    private readonly Stack<List<NoteData>> _undoStack = new();
    private readonly Stack<List<NoteData>> _redoStack = new();
    
    public override void _Ready()
    {
        _songNameLabel = GetNode<Label>("Header/SongName");
        _bpmLabel = GetNode<Label>("Header/BPMLabel");
        _notesContainer = GetNode<Node2D>("TrackContainer/NotesContainer");
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
        _backButton = GetNode<Button>("Header/BackButton");
        
        // Connect tool buttons
        if (_tapTool != null)
            _tapTool.Pressed += () => SelectTool(NoteType.Tap);
        if (_holdTool != null)
            _holdTool.Pressed += () => SelectTool(NoteType.Hold);
        if (_swipeTool != null)
            _swipeTool.Pressed += () => SelectTool(NoteType.Swipe);
        if (_eraseTool != null)
            _eraseTool.Pressed += () => SelectTool(NoteType.Tap); // Erase mode
        
        if (_undoButton != null)
            _undoButton.Pressed += OnUndoPressed;
        if (_redoButton != null)
            _redoButton.Pressed += OnRedoPressed;
        if (_clearButton != null)
            _clearButton.Pressed += OnClearPressed;
        if (_playButton != null)
            _playButton.Pressed += OnPlayPressed;
        if (_stopButton != null)
            _stopButton.Pressed += OnStopPressed;
        if (_saveButton != null)
            _saveButton.Pressed += OnSavePressed;
        if (_backButton != null)
            _backButton.Pressed += OnBackPressed;
        
        CreateNewChart();
    }
    
    private void CreateNewChart()
    {
        _currentChart = new ChartData
        {
            Id = System.Guid.NewGuid().ToString(),
            Difficulty = GameManager.Difficulty.Normal,
            TrackCount = 4,
            Bpm = 120
        };
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (_songNameLabel != null)
            _songNameLabel.Text = "New Chart";
        
        if (_bpmLabel != null && _currentChart != null)
            _bpmLabel.Text = $"BPM: {_currentChart.Bpm}";
    }
    
    private void SelectTool(NoteType tool)
    {
        _currentTool = tool;
        
        if (_tapTool != null)
            _tapTool.ButtonPressed = tool == NoteType.Tap;
        if (_holdTool != null)
            _holdTool.ButtonPressed = tool == NoteType.Hold;
        if (_swipeTool != null)
            _swipeTool.ButtonPressed = tool == NoteType.Swipe;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            // Check if clicking on track area
            var trackContainer = GetNode<Control>("TrackContainer");
            if (trackContainer != null)
            {
                var localPos = trackContainer.GetLocalMousePosition();
                if (trackContainer.GetRect().HasPoint(localPos))
                {
                    int track = (int)(localPos.X / 130);
                    if (track >= 0 && track < 4)
                    {
                        AddNote(_currentTime, track);
                    }
                }
            }
        }
    }
    
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
        
        _notes.Add(note);
        _notes.Sort((a, b) => a.Time.CompareTo(b.Time));
        
        RefreshNoteDisplay();
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
                Type = note.Type
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
            // Would create visual representation
        }
    }
    
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
        SaveUndoState();
        _notes.Clear();
        RefreshNoteDisplay();
    }
    
    private void OnPlayPressed()
    {
        _isPlaying = true;
        // Would play audio
    }
    
    private void OnStopPressed()
    {
        _isPlaying = false;
        _currentTime = 0;
    }
    
    private void OnSavePressed()
    {
        if (_currentChart == null) return;
        
        _currentChart.Notes = new List<NoteData>(_notes);
        
        // Save to file using System.Text.Json
        var json = JsonSerializer.Serialize(_currentChart);
        using var file = FileAccess.Open($"user://charts/{_currentChart.Id}.json", FileAccess.ModeFlags.Write);
        file?.StoreString(json);
        
        GD.Print("Chart saved!");
    }
    
    private void OnBackPressed()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }
}