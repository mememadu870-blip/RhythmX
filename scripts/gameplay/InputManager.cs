using Godot;
using System;

namespace RhythmX;

/// <summary>
/// Handles keyboard and touch input for rhythm gameplay
/// </summary>
public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; }
    
    // Default key bindings (D, F, J, K for 4 tracks)
    private readonly string[] _trackActions = { "tap_1", "tap_2", "tap_3", "tap_4" };
    
    private bool[] _trackPressed;
    private bool[] _trackPreviouslyPressed;
    private int _trackCount = 4;
    
    public event Action<int> OnTrackPressed;
    public event Action<int> OnTrackReleased;
    
    public override void _Ready()
    {
        Instance = this;
        _trackPressed = new bool[_trackCount];
        _trackPreviouslyPressed = new bool[_trackCount];
    }
    
    public void SetTrackCount(int count)
    {
        _trackCount = count;
        _trackPressed = new bool[count];
        _trackPreviouslyPressed = new bool[count];
    }
    
    public override void _Input(InputEvent @event)
    {
        // Handle keyboard input
        if (@event is InputEventKey keyEvent)
        {
            for (int i = 0; i < _trackActions.Length && i < _trackCount; i++)
            {
                if (keyEvent.IsAction(_trackActions[i]))
                {
                    if (keyEvent.IsPressed() && !_trackPreviouslyPressed[i])
                    {
                        HandleTrackDown(i);
                    }
                    else if (!keyEvent.IsPressed() && _trackPreviouslyPressed[i])
                    {
                        HandleTrackUp(i);
                    }
                }
            }
            
            // Handle pause
            if (keyEvent.IsActionPressed("pause"))
            {
                // Pause handled by GameplayUI
            }
        }
        
        // Handle touch/mouse input
        if (@event is InputEventScreenTouch touchEvent)
        {
            int track = GetTrackFromPosition(touchEvent.Position);
            if (track >= 0 && track < _trackCount)
            {
                if (touchEvent.IsPressed())
                {
                    HandleTrackDown(track);
                }
                else
                {
                    HandleTrackUp(track);
                }
            }
        }
        
        if (@event is InputEventMouseButton mouseEvent)
        {
            int track = GetTrackFromPosition(mouseEvent.Position);
            if (track >= 0 && track < _trackCount)
            {
                if (mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    HandleTrackDown(track);
                }
                else if (!mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    HandleTrackUp(track);
                }
            }
        }
    }
    
    private void HandleTrackDown(int track)
    {
        if (track < 0 || track >= _trackCount) return;
        
        _trackPressed[track] = true;
        OnTrackPressed?.Invoke(track);
        
        // Notify TrackManager
        TrackManager.Instance?.OnTrackPressed(track);
        
        // Notify GameplayUI for note hit check
        GameplayUI.Instance?.HandleTrackInput(track);
    }
    
    private void HandleTrackUp(int track)
    {
        if (track < 0 || track >= _trackCount) return;
        
        _trackPressed[track] = false;
        OnTrackReleased?.Invoke(track);
        
        TrackManager.Instance?.OnTrackReleased(track);
    }
    
    public override void _Process(double delta)
    {
        // Update previous state for detecting changes
        for (int i = 0; i < _trackCount; i++)
        {
            _trackPreviouslyPressed[i] = _trackPressed[i];
            
            // Also check continuous input state
            if (Input.IsActionPressed(_trackActions[i]))
            {
                _trackPressed[i] = true;
            }
        }
    }
    
    private int GetTrackFromPosition(Vector2 screenPosition)
    {
        // Get the track container to calculate positions
        var gameplayUI = GameplayUI.Instance;
        if (gameplayUI == null) return -1;
        
        var trackContainer = gameplayUI.GetNode<Control>("TrackContainer");
        if (trackContainer == null) return -1;
        
        // Convert screen position to local position
        Vector2 localPos = trackContainer.GetGlobalRect().Position;
        Vector2 size = trackContainer.Size;
        
        float trackWidth = size.X / _trackCount;
        float relativeX = screenPosition.X - localPos.X;
        
        int track = (int)(relativeX / trackWidth);
        
        if (track >= 0 && track < _trackCount)
        {
            return track;
        }
        
        return -1;
    }
    
    public bool IsTrackPressed(int track)
    {
        if (track < 0 || track >= _trackPressed.Length) return false;
        return _trackPressed[track];
    }
    
    /// <summary>
    /// Detect swipe direction from start to end position
    /// </summary>
    public SwipeDirection DetectSwipe(Vector2 startPos, Vector2 endPos, float minDistance = 50f)
    {
        Vector2 delta = endPos - startPos;
        
        if (delta.Length() < minDistance)
            return SwipeDirection.None;
        
        float angle = Mathf.Atan2(delta.Y, delta.X) * Mathf.RadToDeg(1f);
        
        // Adjust for screen coordinates (Y is inverted)
        angle = -angle;
        
        if (angle >= -45f && angle < 45f)
            return SwipeDirection.Right;
        else if (angle >= 45f && angle < 135f)
            return SwipeDirection.Up;
        else if (angle >= -135f && angle < -45f)
            return SwipeDirection.Down;
        else
            return SwipeDirection.Left;
    }
    
    /// <summary>
    /// Get the current track state array
    /// </summary>
    public bool[] GetTrackStates()
    {
        return _trackPressed;
    }
}