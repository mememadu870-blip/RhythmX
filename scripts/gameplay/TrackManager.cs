using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Manages track input and visual feedback
/// </summary>
public partial class TrackManager : Node
{
    public static TrackManager Instance { get; private set; }
    
    [Export] public int TrackCount { get; private set; } = 4;
    [Export] public float TrackWidth { get; private set; } = 720f;  // Total width for all tracks
    [Export] public float HitLineY { get; private set; } = 400f;    // Y position of hit line
    
    // Track colors
    private readonly Color[] _trackColors = {
        new Color(0.6f, 0.97f, 1f),   // Track 1 - Cyan
        new Color(1f, 0.35f, 0.89f),  // Track 2 - Magenta  
        new Color(0.6f, 0.97f, 1f),   // Track 3 - Cyan
        new Color(1f, 0.35f, 0.89f)   // Track 4 - Magenta
    };
    
    private TrackLane[] _lanes;
    
    // Effect references (will be set by GameplayUI)
    private GpuParticles2D _hitParticlePrefab;
    private GpuParticles2D _missParticlePrefab;
    
    public event Action<int, ScoreManager.Judgment> OnTrackHit;
    public event Action<int> OnTrackMiss;
    
    public override void _Ready()
    {
        Instance = this;
        InitializeTracks();
    }
    
    private void InitializeTracks()
    {
        _lanes = new TrackLane[TrackCount];
        
        float trackSpacing = TrackWidth / TrackCount;
        float startX = -TrackWidth / 2 + trackSpacing / 2;
        
        for (int i = 0; i < TrackCount; i++)
        {
            _lanes[i] = new TrackLane
            {
                Index = i,
                X = startX + i * trackSpacing,
                Color = GetTrackColor(i),
                IsPressed = false,
                IsHolding = false,
                PressTime = 0f
            };
        }
    }
    
    public Color GetTrackColor(int track)
    {
        if (track >= 0 && track < _trackColors.Length)
            return _trackColors[track];
        return Colors.White;
    }
    
    public float GetTrackX(int track)
    {
        if (track >= 0 && track < TrackCount)
            return _lanes[track].X;
        return 0f;
    }
    
    public void OnTrackPressed(int track)
    {
        if (track < 0 || track >= TrackCount) return;
        
        _lanes[track].IsPressed = true;
        _lanes[track].PressTime = (float)Time.GetTicksMsec() / 1000f;
        
        // Visual feedback - highlight track
        HighlightTrack(track, true);
        
        // Check for note hit (delegated to GameplayUI)
        // This is just visual feedback here
    }
    
    public void OnTrackReleased(int track)
    {
        if (track < 0 || track >= TrackCount) return;
        
        _lanes[track].IsPressed = false;
        
        // Handle hold note release
        if (_lanes[track].IsHolding && _lanes[track].HoldNote != null)
        {
            double currentTime = AudioManager.Instance?.CurrentTime ?? 0;
            var note = _lanes[track].HoldNote;
            
            if (currentTime < note.Data.EndTime)
            {
                // Early release - handle through GameplayUI
                GameplayUI.Instance?.HandleHoldRelease(track);
            }
            
            _lanes[track].IsHolding = false;
            _lanes[track].HoldNote = null;
        }
        
        HighlightTrack(track, false);
    }
    
    public void SetHoldingNote(int track, NoteObject note)
    {
        if (track < 0 || track >= TrackCount) return;
        _lanes[track].IsHolding = true;
        _lanes[track].HoldNote = note;
    }
    
    public void ClearHoldNote(int track)
    {
        if (track < 0 || track >= TrackCount) return;
        _lanes[track].IsHolding = false;
        _lanes[track].HoldNote = null;
    }
    
    public bool IsTrackHolding(int track)
    {
        if (track < 0 || track >= TrackCount) return false;
        return _lanes[track].IsHolding;
    }
    
    public void UpdateHoldNotes()
    {
        double currentTime = AudioManager.Instance?.CurrentTime ?? 0;
        
        foreach (var lane in _lanes)
        {
            if (lane.IsHolding && lane.HoldNote != null)
            {
                if (currentTime >= lane.HoldNote.Data.EndTime)
                {
                    lane.HoldNote.CompleteHold();
                    lane.IsHolding = false;
                    lane.HoldNote = null;
                }
            }
        }
    }
    
    public void ShowHitEffect(int track, ScoreManager.Judgment judgment)
    {
        if (track < 0 || track >= TrackCount) return;
        
        // Spawn visual effect at track position
        GameplayUI.Instance?.ShowHitEffect(track, judgment, _lanes[track].X);
        
        OnTrackHit?.Invoke(track, judgment);
    }
    
    public void ShowMissEffect(int track)
    {
        if (track < 0 || track >= TrackCount) return;
        
        GameplayUI.Instance?.ShowMissEffect(track, _lanes[track].X);
        
        OnTrackMiss?.Invoke(track);
    }
    
    private void HighlightTrack(int track, bool highlight)
    {
        // Visual feedback is handled by GameplayUI
        GameplayUI.Instance?.HighlightTrack(track, highlight, _lanes[track].Color);
    }
    
    public TrackLane GetLane(int track)
    {
        if (track >= 0 && track < TrackCount)
            return _lanes[track];
        return null;
    }
    
    public override void _Process(double delta)
    {
        UpdateHoldNotes();
    }
}

/// <summary>
/// Represents a single track lane
/// </summary>
public class TrackLane
{
    public int Index;
    public float X;
    public Color Color;
    public bool IsPressed;
    public bool IsHolding;
    public float PressTime;
    public NoteObject HoldNote;
}