using Godot;

namespace RhythmX;

/// <summary>
/// Effect manager for visual feedback
/// </summary>
public partial class EffectManager : Node
{
    public static EffectManager Instance { get; private set; }
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    public void PlayHitEffect(int track, ScoreManager.Judgment judgment)
    {
        // Would spawn particle effect
        GD.Print($"Hit effect: Track {track}, Judgment: {judgment}");
    }
    
    public void PlayComboEffect(int combo)
    {
        if (combo >= 100)
        {
            GD.Print($"Big combo effect: {combo}!");
        }
    }
    
    public void PlayFullComboEffect()
    {
        GD.Print("FULL COMBO effect!");
    }
}