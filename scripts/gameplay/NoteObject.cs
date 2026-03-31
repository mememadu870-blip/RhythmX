using Godot;
using System;

namespace RhythmX;

/// <summary>
/// Visual representation of a rhythm game note
/// Handles note rendering, movement, and hit detection
/// </summary>
public partial class NoteObject : Node2D
{
    // Note data
    public NoteData Data { get; set; }
    
    // Visual settings
    public float TrackWidth { get; set; } = 180f;
    public Color NoteColor { get; set; } = new Color(0.4f, 1f, 1f);
    public float NoteSpeed { get; private set; } = 400f;
    
    // State
    public bool WasHit { get; private set; }
    public bool WasMissed { get; private set; }
    public bool IsHolding { get; private set; }
    
    // Visual components
    private Sprite2D _noteSprite;
    private Sprite2D _holdTail;
    
    // Reference positions
    private float _spawnY = -400f;
    private float _hitLineY = 400f;
    private float _despawnY = 500f;
    
    // Hold note tracking
    private double _holdStartTime;
    private double _holdEndTime;
    
    public override void _Ready()
    {
        CreateVisuals();
    }
    
    private void CreateVisuals()
    {
        // Create note sprite
        _noteSprite = new Sprite2D();
        
        // Create a simple colored rectangle texture
        UpdateVisuals();
        
        AddChild(_noteSprite);
        
        // Create hold tail if needed
        if (Data?.Type == NoteType.Hold)
        {
            CreateHoldTail();
        }
    }
    
    private void UpdateVisuals()
    {
        if (Data == null) return;
        
        // Create texture based on note type
        Vector2 size = GetNoteSize();
        
        var image = Image.CreateEmpty((int)size.X, (int)size.Y, false, Image.Format.Rgba8);
        image.Fill(NoteColor);
        
        var texture = ImageTexture.CreateFromImage(image);
        _noteSprite.Texture = texture;
        
        // Position offset
        _noteSprite.Offset = new Vector2(0, size.Y / 2);
    }
    
    private Vector2 GetNoteSize()
    {
        if (Data == null) return new Vector2(TrackWidth - 20, 30);
        
        return Data.Type switch
        {
            NoteType.Hold => new Vector2(TrackWidth - 20, 40),
            NoteType.Swipe => new Vector2(TrackWidth - 20, 50),
            _ => new Vector2(TrackWidth - 20, 30)
        };
    }
    
    private void CreateHoldTail()
    {
        if (Data == null || Data.Type != NoteType.Hold) return;
        
        _holdTail = new Sprite2D();
        
        float holdDuration = (float)Data.Duration;
        float holdLength = holdDuration * NoteSpeed;
        
        var image = Image.CreateEmpty((int)(TrackWidth - 20), (int)holdLength, false, Image.Format.Rgba8);
        Color tailColor = new Color(NoteColor.R, NoteColor.G, NoteColor.B, 0.5f);
        image.Fill(tailColor);
        
        var texture = ImageTexture.CreateFromImage(image);
        _holdTail.Texture = texture;
        _holdTail.Offset = new Vector2(0, holdLength / 2);
        _holdTail.Position = new Vector2(0, 20);
        
        AddChild(_holdTail);
        
        _holdStartTime = Data.Time;
        _holdEndTime = Data.EndTime;
    }
    
    public void Initialize(NoteData data, float hitLineY, float despawnY)
    {
        Data = data;
        _hitLineY = hitLineY;
        _despawnY = despawnY;
        
        // Set color based on note type
        NoteColor = data.Type switch
        {
            NoteType.Hold => new Color(1f, 0.8f, 0.2f),   // Gold for hold
            NoteType.Swipe => new Color(0.8f, 0.4f, 1f),  // Purple for swipe
            _ => new Color(0.4f, 1f, 1f)                   // Cyan for tap
        };
        
        WasHit = false;
        WasMissed = false;
        IsHolding = false;
        
        UpdateVisuals();
        
        if (data.Type == NoteType.Hold)
        {
            CreateHoldTail();
        }
    }
    
    public void SetSpeed(float speed)
    {
        NoteSpeed = speed;
        
        // Update hold tail length
        if (_holdTail != null && Data != null)
        {
            float holdLength = (float)Data.Duration * speed;
            
            var image = Image.CreateEmpty((int)(TrackWidth - 20), (int)holdLength, false, Image.Format.Rgba8);
            Color tailColor = new Color(NoteColor.R, NoteColor.G, NoteColor.B, 0.5f);
            image.Fill(tailColor);
            
            var texture = ImageTexture.CreateFromImage(image);
            _holdTail.Texture = texture;
            _holdTail.Offset = new Vector2(0, holdLength / 2);
        }
    }
    
    public void SetSpawnPosition(float spawnY)
    {
        _spawnY = spawnY;
    }
    
    public void UpdatePosition(double currentTime)
    {
        if (Data == null) return;
        
        // Calculate Y position based on time difference
        double timeDiff = Data.Time - currentTime;
        float y = _hitLineY - (float)(timeDiff * NoteSpeed);
        
        Position = new Vector2(Position.X, y);
        
        // Check if past despawn point
        if (y > _despawnY && !WasHit && !WasMissed)
        {
            // Will be marked as missed by NoteController
        }
    }
    
    public void MarkHit()
    {
        WasHit = true;
        
        // Hit animation
        PlayHitAnimation();
    }
    
    public void MarkMissed()
    {
        WasMissed = true;
        
        // Miss animation
        PlayMissAnimation();
    }
    
    public void StartHold()
    {
        IsHolding = true;
        
        // Change color to indicate holding
        NoteColor = new Color(1f, 1f, 0.5f);  // Bright yellow while holding
        UpdateVisuals();
    }
    
    public void CompleteHold()
    {
        IsHolding = false;
        WasHit = true;
        
        PlayHitAnimation();
    }
    
    public void ReleaseHold()
    {
        IsHolding = false;
        
        // If released early, might be a miss
        if (!WasHit)
        {
            MarkMissed();
        }
    }
    
    private void PlayHitAnimation()
    {
        // Scale up and fade out
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1.5f, 1.5f), 0.1f);
        tween.TweenProperty(this, "modulate:a", 0f, 0.2f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
    
    private void PlayMissAnimation()
    {
        // Fade out quickly
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", MissColor, 0.1f);
        tween.TweenProperty(this, "modulate:a", 0f, 0.3f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
    
    private static Color MissColor = new Color(1f, 0.3f, 0.3f);
    
    public double GetTimeOffset(double currentTime)
    {
        if (Data == null) return 0;
        return currentTime - Data.Time;
    }
    
    public bool IsWithinHitWindow(double currentTime, double windowMs = 135)
    {
        if (Data == null) return false;
        double timeDiff = Math.Abs(currentTime - Data.Time);
        return timeDiff <= windowMs / 1000.0;
    }
    
    public ScoreManager.Judgment GetJudgment(double currentTime)
    {
        if (Data == null) return ScoreManager.Judgment.Miss;
        
        double offsetMs = Math.Abs(currentTime - Data.Time) * 1000;
        
        if (offsetMs <= 45)
            return ScoreManager.Judgment.Perfect;
        else if (offsetMs <= 90)
            return ScoreManager.Judgment.Great;
        else if (offsetMs <= 135)
            return ScoreManager.Judgment.Good;
        else
            return ScoreManager.Judgment.Miss;
    }
    
    public override void _Draw()
    {
        // Custom drawing for more complex note visuals
        if (Data == null) return;
        
        Vector2 size = GetNoteSize();
        
        // Draw note body
        DrawRect(new Rect2(-size.X / 2, 0, size.X, size.Y), NoteColor);
        
        // Draw hold tail
        if (Data.Type == NoteType.Hold && Data.Duration > 0)
        {
            float holdLength = (float)Data.Duration * NoteSpeed;
            Color tailColor = new Color(NoteColor.R, NoteColor.G, NoteColor.B, 0.5f);
            DrawRect(new Rect2(-size.X / 2, size.Y, size.X, holdLength), tailColor);
        }
    }
}