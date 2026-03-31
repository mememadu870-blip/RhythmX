using Godot;
using System;

namespace RhythmX;

/// <summary>
/// Main game manager - handles game state and transitions
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }
    
    public enum GameState
    {
        MainMenu,
        SongSelection,
        Playing,
        Paused,
        Results,
        ChartEditor,
        Settings,
        Achievements
    }
    
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }
    
    public static readonly float[] DifficultyDensityMultipliers = { 0.4f, 0.7f, 1.0f, 1.3f };
    public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Expert" };
    public static readonly Color[] DifficultyColors = 
    {
        new Color(0.5f, 1f, 0.5f),  // Easy - Green
        new Color(1f, 1f, 0.5f),    // Normal - Yellow
        new Color(1f, 0.5f, 0.5f),  // Hard - Red
        new Color(0.8f, 0.3f, 1f)   // Expert - Purple
    };
    
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public SongData CurrentSong { get; private set; }
    public ChartData CurrentChart { get; private set; }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;
    
    public float AudioOffset { get; private set; }
    public float NoteSpeed { get; private set; } = 1.0f;
    
    public override void _Ready()
    {
        Instance = this;
        
        // Load settings
        AudioOffset = (float)ConfigFile.GetValue("settings", "audio_offset", 0.0);
        NoteSpeed = (float)ConfigFile.GetValue("settings", "note_speed", 1.0);
    }
    
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        
        switch (newState)
        {
            case GameState.MainMenu:
                GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
                break;
            case GameState.SongSelection:
                GetTree().ChangeSceneToFile("res://scenes/SongSelection.tscn");
                break;
            case GameState.Playing:
                GetTree().ChangeSceneToFile("res://scenes/Gameplay.tscn");
                break;
            case GameState.Results:
                GetTree().ChangeSceneToFile("res://scenes/Results.tscn");
                break;
            case GameState.Settings:
                GetTree().ChangeSceneToFile("res://scenes/Settings.tscn");
                break;
            case GameState.Achievements:
                GetTree().ChangeSceneToFile("res://scenes/Achievements.tscn");
                break;
        }
    }
    
    public void StartGame(SongData song, ChartData chart, Difficulty difficulty)
    {
        CurrentSong = song;
        CurrentChart = chart;
        CurrentDifficulty = difficulty;
        ChangeState(GameState.Playing);
    }
    
    public void ReturnToMainMenu()
    {
        ChangeState(GameState.MainMenu);
    }
    
    public void SetAudioOffset(float offset)
    {
        AudioOffset = Mathf.Clamp(offset, -300f, 300f);
        ConfigFile.SetValue("settings", "audio_offset", AudioOffset);
        ConfigFile.Save("user://settings.cfg");
    }
    
    public void SetNoteSpeed(float speed)
    {
        NoteSpeed = Mathf.Clamp(speed, 0.5f, 3.0f);
        ConfigFile.SetValue("settings", "note_speed", NoteSpeed);
        ConfigFile.Save("user://settings.cfg");
    }
    
    private ConfigFile _configFile = new();
    private ConfigFile ConfigFile
    {
        get
        {
            if (!_configLoaded)
            {
                _configFile.Load("user://settings.cfg");
                _configLoaded = true;
            }
            return _configFile;
        }
    }
    private bool _configLoaded = false;
}