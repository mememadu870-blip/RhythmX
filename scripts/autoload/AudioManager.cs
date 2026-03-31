using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Handles all audio playback, synchronization, and timing
/// Singleton autoload for global audio access.
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }
    
    // Audio streams
    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _sfxPlayer;
    private AudioStreamPlayer _hitPlayer;
    
    // Timing state
    public double CurrentTime { get; private set; }
    public double SongDuration { get; private set; }
    public double BPM { get; private set; } = 120;
    public double BeatProgress { get; private set; }
    public int CurrentBeat { get; private set; }
    public bool IsPlaying { get; private set; }
    public double TimeSinceStart { get; private set; }
    
    private AudioStream _currentStream;
    private double _startTime;
    private double _pauseTime;
    private double _startOffset;
    
    // Hit sound pool
    private List<AudioStreamPlayer> _hitSoundPool = new();
    private int _hitSoundPoolSize = 5;
    private int _currentHitPlayerIndex = 0;
    
    // Settings
    public float MusicVolume { get; private set; } = 1.0f;
    public float SfxVolume { get; private set; } = 1.0f;
    public float HitVolume { get; private set; } = 1.0f;
    
    // Events
    public event Action<double> OnBeat;
    public event Action OnSongEnd;
    public event Action<double> OnTimeUpdate;
    
    public override void _Ready()
    {
        Instance = this;
        
        // Create audio players
        _musicPlayer = new AudioStreamPlayer();
        _sfxPlayer = new AudioStreamPlayer();
        _hitPlayer = new AudioStreamPlayer();
        
        AddChild(_musicPlayer);
        AddChild(_sfxPlayer);
        AddChild(_hitPlayer);
        
        // Create hit sound pool for overlapping sounds
        for (int i = 0; i < _hitSoundPoolSize; i++)
        {
            var player = new AudioStreamPlayer();
            player.VolumeDb = 0f;
            AddChild(player);
            _hitSoundPool.Add(player);
        }
        
        // Load volumes from settings
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") == Error.Ok)
        {
            MusicVolume = (float)config.GetValue("audio", "music_volume", 1.0);
            SfxVolume = (float)config.GetValue("audio", "sfx_volume", 1.0);
            HitVolume = (float)config.GetValue("audio", "hit_volume", 1.0);
        }
        
        ApplyVolumes();
    }
    
    private void ApplyVolumes()
    {
        _musicPlayer.VolumeDb = Mathf.LinearToDb(MusicVolume);
        _sfxPlayer.VolumeDb = Mathf.LinearToDb(SfxVolume);
        _hitPlayer.VolumeDb = Mathf.LinearToDb(HitVolume);
        
        foreach (var player in _hitSoundPool)
        {
            player.VolumeDb = Mathf.LinearToDb(HitVolume);
        }
    }
    
    #region Song Loading
    
    public void LoadSong(AudioStream stream, double bpm, double startOffset = 0)
    {
        _currentStream = stream;
        _musicPlayer.Stream = stream;
        BPM = bpm;
        _startOffset = startOffset;
        
        // Get duration from stream
        if (stream is AudioStreamWav wav)
        {
            SongDuration = wav.GetLength();
        }
        else if (stream is AudioStreamOggVorbis ogg)
        {
            SongDuration = ogg.GetLength();
        }
        else
        {
            // Fallback - will update during playback
            SongDuration = 0;
        }
        
        CurrentTime = 0;
        CurrentBeat = 0;
        TimeSinceStart = 0;
    }
    
    public void LoadSongFromPath(string path, double bpm, double startOffset = 0)
    {
        // Load audio file from path
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"Audio file not found: {path}");
            return;
        }
        
        AudioStream stream = null;
        
        string extension = path.GetExtension().ToLower();
        
        switch (extension)
        {
            case "wav":
                stream = AudioStreamWav.LoadFromFile(path);
                break;
            case "ogg":
                stream = AudioStreamOggVorbis.LoadFromFile(path);
                break;
            default:
                GD.PrintErr($"Unsupported audio format: {extension}");
                return;
        }
        
        if (stream != null)
        {
            LoadSong(stream, bpm, startOffset);
        }
    }
    
    #endregion
    
    #region Playback Control
    
    public void Play()
    {
        if (_currentStream == null) return;
        
        _musicPlayer.Play();
        _startTime = Time.GetTicksMsec() / 1000.0;
        IsPlaying = true;
    }
    
    public void PlayFrom(double time)
    {
        if (_currentStream == null) return;
        
        _musicPlayer.Seek((float)time);
        _musicPlayer.Play();
        _startTime = Time.GetTicksMsec() / 1000.0 - time;
        IsPlaying = true;
        CurrentTime = time;
    }
    
    public void Pause()
    {
        if (!IsPlaying) return;
        
        _pauseTime = CurrentTime;
        _musicPlayer.StreamPaused = true;
        IsPlaying = false;
    }
    
    public void Resume()
    {
        if (!IsPlaying) return;
        
        _musicPlayer.StreamPaused = false;
        _startTime = Time.GetTicksMsec() / 1000.0 - _pauseTime;
        IsPlaying = true;
    }
    
    public void Stop()
    {
        _musicPlayer.Stop();
        IsPlaying = false;
        CurrentTime = 0;
        CurrentBeat = 0;
        TimeSinceStart = 0;
    }
    
    public void Seek(double time)
    {
        if (_currentStream == null) return;
        
        time = Math.Max(0, Math.Min(time, SongDuration));
        _musicPlayer.Seek((float)time);
        
        if (IsPlaying)
        {
            _startTime = Time.GetTicksMsec() / 1000.0 - time;
        }
        else
        {
            _pauseTime = time;
        }
        
        CurrentTime = time;
    }
    
    #endregion
    
    #region Time Processing
    
    public override void _Process(double delta)
    {
        if (!IsPlaying || _currentStream == null) return;
        
        // Update current time
        double realTime = Time.GetTicksMsec() / 1000.0;
        CurrentTime = realTime - _startTime + _startOffset;
        TimeSinceStart = CurrentTime;
        
        // Calculate beat progress
        double beatDuration = 60.0 / BPM;
        BeatProgress = (CurrentTime % beatDuration) / beatDuration;
        
        int newBeat = (int)(CurrentTime / beatDuration);
        
        if (newBeat != CurrentBeat)
        {
            CurrentBeat = newBeat;
            OnBeat?.Invoke(CurrentBeat);
            
            // Notify EffectManager for beat sync
            EffectManager.Instance?.OnBeat(CurrentBeat);
        }
        
        // Fire time update event
        OnTimeUpdate?.Invoke(CurrentTime);
        
        // Check for song end
        if (CurrentTime >= SongDuration && SongDuration > 0)
        {
            IsPlaying = false;
            OnSongEnd?.Invoke();
        }
    }
    
    public double GetTimeToBeat(int beat)
    {
        double beatTime = beat * (60.0 / BPM);
        return beatTime - CurrentTime;
    }
    
    public double GetNextBeatTime()
    {
        double beatDuration = 60.0 / BPM;
        return (CurrentBeat + 1) * beatDuration;
    }
    
    #endregion
    
    #region Sound Effects
    
    public void PlayHitSound(AudioStream clip)
    {
        if (clip == null) return;
        
        // Use pooled player for overlapping hit sounds
        var player = _hitSoundPool[_currentHitPlayerIndex];
        player.Stream = clip;
        player.Play();
        
        _currentHitPlayerIndex = (_currentHitPlayerIndex + 1) % _hitSoundPoolSize;
    }
    
    public void PlaySfx(AudioStream clip)
    {
        if (clip == null) return;
        _sfxPlayer.Stream = clip;
        _sfxPlayer.Play();
    }
    
    public void PlaySfxFromPath(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"SFX file not found: {path}");
            return;
        }
        
        AudioStream stream = null;
        string extension = path.GetExtension().ToLower();
        
        switch (extension)
        {
            case "wav":
                stream = AudioStreamWav.LoadFromFile(path);
                break;
            case "ogg":
                stream = AudioStreamOggVorbis.LoadFromFile(path);
                break;
        }
        
        if (stream != null)
        {
            PlaySfx(stream);
        }
    }
    
    #endregion
    
    #region Volume Control
    
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp(volume, 0f, 1f);
        _musicPlayer.VolumeDb = Mathf.LinearToDb(MusicVolume);
        SaveSettings();
    }
    
    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp(volume, 0f, 1f);
        _sfxPlayer.VolumeDb = Mathf.LinearToDb(SfxVolume);
        SaveSettings();
    }
    
    public void SetHitVolume(float volume)
    {
        HitVolume = Mathf.Clamp(volume, 0f, 1f);
        foreach (var player in _hitSoundPool)
        {
            player.VolumeDb = Mathf.LinearToDb(HitVolume);
        }
        SaveSettings();
    }
    
    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("audio", "music_volume", MusicVolume);
        config.SetValue("audio", "sfx_volume", SfxVolume);
        config.SetValue("audio", "hit_volume", HitVolume);
        config.Save("user://settings.cfg");
    }
    
    #endregion
    
    #region BPM Analysis
    
    public double CalculateBeatTime(int beat)
    {
        return beat * (60.0 / BPM);
    }
    
    public int GetBeatFromTime(double time)
    {
        return (int)(time * BPM / 60.0);
    }
    
    public double GetTimeInBeat(double time)
    {
        double beatDuration = 60.0 / BPM;
        return time % beatDuration;
    }
    
    #endregion
}