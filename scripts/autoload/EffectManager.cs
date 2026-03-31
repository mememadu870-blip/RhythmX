using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Manages visual effects for gameplay - particles, trails, etc.
/// Singleton autoload for global effect access.
/// </summary>
public partial class EffectManager : Node
{
    public static EffectManager Instance { get; private set; }
    
    // Effect settings
    [Export] public float LaneGlowDuration { get; private set; } = 0.2f;
    [Export] public float BackgroundIntensityBase { get; private set; } = 0.3f;
    [Export] public float BackgroundIntensityMax { get; private set; } = 1f;
    
    // Judgment colors
    public readonly Color PerfectColor = new Color(0.6f, 1f, 1f);     // Cyan
    public readonly Color GreatColor = new Color(1f, 0.9f, 0.3f);     // Yellow
    public readonly Color GoodColor = new Color(0.5f, 0.8f, 0.5f);    // Green
    public readonly Color MissColor = new Color(1f, 0.3f, 0.3f);      // Red
    
    // Active effects tracking
    private List<GpuParticles2D> _activeParticles = new();
    private float _currentBackgroundIntensity;
    
    // Scene references for spawning effects
    private PackedScene _perfectEffectScene;
    private PackedScene _greatEffectScene;
    private PackedScene _goodEffectScene;
    private PackedScene _missEffectScene;
    private PackedScene _comboEffectScene;
    private PackedScene _bigComboEffectScene;
    
    public event Action<int> OnBeatPulse;
    
    public override void _Ready()
    {
        Instance = this;
        LoadEffectScenes();
        _currentBackgroundIntensity = BackgroundIntensityBase;
    }
    
    private void LoadEffectScenes()
    {
        // Load effect scenes from resources
        // These can be created in Godot editor and saved as .tscn files
        _perfectEffectScene = GD.Load<PackedScene>("res://resources/effects/PerfectEffect.tscn");
        _greatEffectScene = GD.Load<PackedScene>("res://resources/effects/GreatEffect.tscn");
        _goodEffectScene = GD.Load<PackedScene>("res://resources/effects/GoodEffect.tscn");
        _missEffectScene = GD.Load<PackedScene>("res://resources/effects/MissEffect.tscn");
        _comboEffectScene = GD.Load<PackedScene>("res://resources/effects/ComboEffect.tscn");
        _bigComboEffectScene = GD.Load<PackedScene>("res://resources/effects/BigComboEffect.tscn");
        
        // If scenes don't exist, we'll create simple effects dynamically
    }
    
    #region Hit Effects
    
    public void PlayHitEffect(ScoreManager.Judgment judgment, Vector2 position, Node parent)
    {
        PackedScene effectScene = GetEffectScene(judgment);
        
        if (effectScene != null)
        {
            var effect = effectScene.Instantiate<GpuParticles2D>();
            effect.Position = position;
            parent.AddChild(effect);
            effect.Emitting = true;
            
            // Auto-remove after effect ends
            GetTree().CreateTimer(effect.Lifetime).Timeout += () =>
            {
                effect.QueueFree();
            };
            
            _activeParticles.Add(effect);
        }
        else
        {
            // Create simple dynamic effect
            CreateDynamicHitEffect(judgment, position, parent);
        }
    }
    
    private PackedScene GetEffectScene(ScoreManager.Judgment judgment)
    {
        return judgment switch
        {
            ScoreManager.Judgment.Perfect => _perfectEffectScene,
            ScoreManager.Judgment.Great => _greatEffectScene,
            ScoreManager.Judgment.Good => _goodEffectScene,
            ScoreManager.Judgment.Miss => _missEffectScene,
            _ => _perfectEffectScene
        };
    }
    
    private void CreateDynamicHitEffect(ScoreManager.Judgment judgment, Vector2 position, Node parent)
    {
        // Create a simple particle effect dynamically
        var particles = new GpuParticles2D();
        particles.Position = position;
        particles.Amount = 20;
        particles.Lifetime = 0.3f;
        particles.Explosiveness = 0.8f;
        particles.OneShot = true;
        particles.Emitting = true;
        
        // Set color based on judgment
        Color color = GetJudgmentColor(judgment);
        
        // Create process material
        var material = new ParticleProcessMaterial();
        material.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Point;
        material.Direction = new Vector3(0, -1, 0);
        material.Spread = 45f;
        material.InitialVelocityMin = 100f;
        material.InitialVelocityMax = 200f;
        material.ScaleMin = 2f;
        material.ScaleMax = 4f;
        material.Color = color;
        
        particles.ProcessMaterial = material;
        
        parent.AddChild(particles);
        
        GetTree().CreateTimer(0.5f).Timeout += () =>
        {
            particles.QueueFree();
        };
    }
    
    public Color GetJudgmentColor(ScoreManager.Judgment judgment)
    {
        return judgment switch
        {
            ScoreManager.Judgment.Perfect => PerfectColor,
            ScoreManager.Judgment.Great => GreatColor,
            ScoreManager.Judgment.Good => GoodColor,
            ScoreManager.Judgment.Miss => MissColor,
            _ => Colors.White
        };
    }
    
    #endregion
    
    #region Combo Effects
    
    public void PlayComboEffect(int combo, Vector2 position, Node parent)
    {
        if (combo >= 100)
        {
            if (_bigComboEffectScene != null)
            {
                SpawnEffect(_bigComboEffectScene, position, parent);
            }
            else
            {
                CreateComboFlashEffect(position, parent, Colors.Gold);
            }
        }
        else if (combo >= 50)
        {
            if (_comboEffectScene != null)
            {
                SpawnEffect(_comboEffectScene, position, parent);
            }
            else
            {
                CreateComboFlashEffect(position, parent, Colors.Yellow);
            }
        }
        
        // Increase background intensity with combo
        float comboIntensity = Mathf.Lerp(BackgroundIntensityBase, BackgroundIntensityMax, combo / 100f);
        SetBackgroundIntensity(comboIntensity);
    }
    
    public void PlayFullComboEffect(Vector2 position, Node parent)
    {
        // Special effect for full combo
        CreateComboFlashEffect(position, parent, PerfectColor, true);
    }
    
    private void CreateComboFlashEffect(Vector2 position, Node parent, Color color, bool isFullCombo = false)
    {
        var particles = new GpuParticles2D();
        particles.Position = position;
        particles.Amount = isFullCombo ? 50 : 30;
        particles.Lifetime = isFullCombo ? 1f : 0.5f;
        particles.Explosiveness = 0.9f;
        particles.OneShot = true;
        particles.Emitting = true;
        
        var material = new ParticleProcessMaterial();
        material.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        material.EmissionSphereRadius = 50f;
        material.Direction = new Vector3(0, 0, 0);
        material.Spread = 180f;
        material.InitialVelocityMin = 50f;
        material.InitialVelocityMax = 150f;
        material.ScaleMin = 3f;
        material.ScaleMax = isFullCombo ? 8f : 5f;
        material.Color = color;
        
        particles.ProcessMaterial = material;
        
        parent.AddChild(particles);
        
        GetTree().CreateTimer(particles.Lifetime + 0.2f).Timeout += () =>
        {
            particles.QueueFree();
        };
    }
    
    #endregion
    
    #region Track Effects
    
    public void PlayTrackFlash(int track, Vector2 position, Node parent)
    {
        // Quick flash effect when track is pressed
        var flash = new Sprite2D();
        
        // Create a simple white rectangle texture
        var image = Image.CreateEmpty(100, 10, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        var texture = ImageTexture.CreateFromImage(image);
        
        flash.Texture = texture;
        flash.Position = position;
        flash.Modulate = new Color(1f, 1f, 1f, 0.5f);
        
        parent.AddChild(flash);
        
        // Fade out animation
        float duration = 0.1f;
        float elapsed = 0f;
        
        var tween = parent.CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0f, duration);
        tween.TweenCallback(Callable.From(() => flash.QueueFree()));
    }
    
    #endregion
    
    #region Lane Glow
    
    public void FlashLane(CanvasItem laneNode, ScoreManager.Judgment judgment)
    {
        if (laneNode == null) return;
        
        Color flashColor = GetJudgmentColor(judgment);
        
        var tween = laneNode.CreateTween();
        tween.TweenProperty(laneNode, "modulate", flashColor, LaneGlowDuration / 2);
        tween.TweenProperty(laneNode, "modulate", Colors.White, LaneGlowDuration / 2);
    }
    
    public void GlowLane(CanvasItem laneNode, Color color, float intensity = 0.3f)
    {
        if (laneNode == null) return;
        
        Color glowColor = new Color(color.R, color.G, color.B, intensity);
        laneNode.Modulate = glowColor;
    }
    
    public void ResetLaneGlow(CanvasItem laneNode)
    {
        if (laneNode == null) return;
        laneNode.Modulate = Colors.White;
    }
    
    #endregion
    
    #region Background Effects
    
    public void SetBackgroundIntensity(float intensity)
    {
        _currentBackgroundIntensity = intensity;
        
        // This can be connected to background visual nodes
        // For now, it's just tracking the value
    }
    
    public void PulseBackground()
    {
        // Create a pulse effect
        float startIntensity = _currentBackgroundIntensity;
        float peakIntensity = Mathf.Min(_currentBackgroundIntensity * 1.5f, BackgroundIntensityMax);
        
        // Tween intensity up and down
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(SetBackgroundIntensity), startIntensity, peakIntensity, 0.15f);
        tween.TweenMethod(Callable.From<float>(SetBackgroundIntensity), peakIntensity, startIntensity, 0.15f);
        
        OnBeatPulse?.Invoke((int)(Time.GetTicksMsec() / 1000));
    }
    
    #endregion
    
    #region Beat Sync
    
    public void OnBeat(int beat)
    {
        // Pulse background on every beat
        PulseBackground();
    }
    
    #endregion
    
    #region Utility
    
    private void SpawnEffect(PackedScene scene, Vector2 position, Node parent)
    {
        if (scene == null) return;
        
        var effect = scene.Instantiate<Node2D>();
        effect.Position = position;
        parent.AddChild(effect);
        
        // If it's a particle system, start it
        if (effect is GpuParticles2D particles)
        {
            particles.Emitting = true;
            GetTree().CreateTimer(particles.Lifetime).Timeout += () =>
            {
                particles.QueueFree();
            };
        }
    }
    
    public void ClearAllEffects()
    {
        foreach (var particle in _activeParticles)
        {
            if (particle != null && IsInstanceValid(particle))
            {
                particle.QueueFree();
            }
        }
        _activeParticles.Clear();
        
        SetBackgroundIntensity(BackgroundIntensityBase);
    }
    
    #endregion
}