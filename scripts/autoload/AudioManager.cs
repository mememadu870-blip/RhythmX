using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Handles all audio playback and timing
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }
    
    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _sfxPlayer;
    
    public double CurrentTime { get; private set; }
    public double SongDuration { get; private set; }
    public double Bpm { get; private set; }
    public bool IsPlaying { get; private set; }
    
    public event Action<double> OnBeat;
    public event Action OnSongEnd;
    
    private double _dspStartTime;
    private double _pauseTime;
    private double _startOffset;
    
    public override void _Ready()
    {
        Instance = this;
        
        _musicPlayer = new AudioStreamPlayer();
        _sfxPlayer = new AudioStreamPlayer();
        
        AddChild(_musicPlayer);
        AddChild(_sfxPlayer);
    }
    
    public void LoadSong(AudioStream stream, double bpm, double offset = 0)
    {
        _musicPlayer.Stream = stream;
        Bpm = bpm;
        SongDuration = stream.GetLength();
        _startOffset = offset;
        CurrentTime = 0;
    }
    
    public void LoadSongFromPath(string path, double bpm, double offset = 0)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return;
        
        // Load audio based on extension
        AudioStream stream = null;
        var ext = path.GetExtension().ToLower();
        
        if (ext == "ogg")
        {
            stream = AudioStreamOggVorbis.LoadFromFile(path);
        }
        else if (ext == "mp3")
        {
            stream = new AudioStreamMP3();
            // Would need to load file data
        }
        else if (ext == "wav")
        {
            stream = new AudioStreamWav();
            // Would need to load file data
        }
        
        if (stream != null)
        {
            LoadSong(stream, bpm, offset);
        }
    }
    
    public void Play()
    {
        if (_musicPlayer.Stream == null) return;
        
        _musicPlayer.Play();
        _dspStartTime = Time.GetTicksMsec() / 1000.0;
        IsPlaying = true;
    }
    
    public void PlayFrom(double time)
    {
        if (_musicPlayer.Stream == null) return;
        
        _musicPlayer.Play();
        _musicPlayer.Seek((float)time);
        _dspStartTime = Time.GetTicksMsec() / 1000.0 - time;
        IsPlaying = true;
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
        _musicPlayer.StreamPaused = false;
        _dspStartTime = Time.GetTicksMsec() / 1000.0 - _pauseTime;
        IsPlaying = true;
    }
    
    public void Stop()
    {
        _musicPlayer.Stop();
        IsPlaying = false;
        CurrentTime = 0;
    }
    
    public void Seek(double time)
    {
        if (_musicPlayer.Stream == null) return;
        
        time = Math.Clamp(time, 0, SongDuration);
        _musicPlayer.Seek((float)time);
        
        if (IsPlaying)
        {
            _dspStartTime = Time.GetTicksMsec() / 1000.0 - time;
        }
        else
        {
            _pauseTime = time;
        }
    }
    
    public override void _Process(double delta)
    {
        if (!IsPlaying || _musicPlayer.Stream == null) return;
        
        // Use DSP time for accurate timing
        double dspTime = Time.GetTicksMsec() / 1000.0;
        CurrentTime = dspTime - _dspStartTime + _startOffset;
        
        // Check for song end
        if (CurrentTime >= SongDuration)
        {
            IsPlaying = false;
            OnSongEnd?.Invoke();
        }
        
        // Beat detection
        double beatDuration = 60.0 / Bpm;
        int currentBeat = (int)(CurrentTime / beatDuration);
        OnBeat?.Invoke(currentBeat);
    }
    
    public void PlaySfx(AudioStream stream)
    {
        _sfxPlayer.Stream = stream;
        _sfxPlayer.Play();
    }
    
    public void SetVolume(float volume)
    {
        _musicPlayer.VolumeDb = Mathf.LinearToDb(volume);
    }
}