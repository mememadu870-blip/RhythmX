using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RhythmX;

/// <summary>
/// Song and chart data structures
/// </summary>
public class SongData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Artist { get; set; }
    public double Bpm { get; set; } = 120;
    public double Duration { get; set; }
    public string AudioPath { get; set; }
    public bool IsImported { get; set; }
    public bool IsFavorite { get; set; }
    public int PlayCount { get; set; }
    public int HighScore { get; set; }
    public List<ChartData> Charts { get; set; } = new();
    
    public ChartData GetChart(GameManager.Difficulty difficulty)
    {
        return Charts.Find(c => c.Difficulty == difficulty);
    }
    
    public bool HasChart(GameManager.Difficulty difficulty)
    {
        return Charts.Exists(c => c.Difficulty == difficulty);
    }
}

public class ChartData
{
    public string Id { get; set; }
    public GameManager.Difficulty Difficulty { get; set; }
    public int TrackCount { get; set; } = 4;
    public double Bpm { get; set; }
    public double Offset { get; set; }
    public List<NoteData> Notes { get; set; } = new();
    
    public int TotalNotes => Notes.Count;
}

public class NoteData
{
    public double Time { get; set; }
    public double EndTime { get; set; }
    public int Track { get; set; }
    public NoteType Type { get; set; }
    public SwipeDirection SwipeDirection { get; set; }
    
    public double Duration => EndTime - Time;
}

public enum NoteType
{
    Tap,
    Hold,
    Swipe
}

public enum SwipeDirection
{
    None,
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// Player save data
/// </summary>
public class PlayerData
{
    public string PlayerId { get; set; } = Guid.NewGuid().ToString();
    public string Nickname { get; set; } = "Player";
    public int TotalPlayCount { get; set; }
    public double TotalPlayTime { get; set; }
    public int MaxCombo { get; set; }
    public DateTime FirstPlayDate { get; set; } = DateTime.Now;
    public DateTime LastPlayDate { get; set; }
    
    public List<SongRecord> SongRecords { get; set; } = new();
    public List<string> FavoriteSongs { get; set; } = new();
    public List<string> ImportedSongs { get; set; } = new();
    public List<string> CreatedCharts { get; set; } = new();
    public List<AchievementRecord> Achievements { get; set; } = new();
    
    public float AudioOffset { get; set; }
    public float NoteSpeed { get; set; } = 1.0f;
    
    private static string SavePath => "user://playerdata.json";
    
    public static PlayerData Load()
    {
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            var json = file.GetAsText();
            try
            {
                return JsonSerializer.Deserialize<PlayerData>(json) ?? CreateNew();
            }
            catch
            {
                return CreateNew();
            }
        }
        return CreateNew();
    }
    
    public void Save()
    {
        var json = JsonSerializer.Serialize(this);
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }
    
    private static PlayerData CreateNew()
    {
        return new PlayerData
        {
            PlayerId = Guid.NewGuid().ToString(),
            FirstPlayDate = DateTime.Now,
            LastPlayDate = DateTime.Now
        };
    }
    
    public SongRecord GetRecord(string songId)
    {
        return SongRecords.Find(r => r.SongId == songId);
    }
    
    public void UpdateRecord(string songId, int score, int maxCombo, string grade, GameManager.Difficulty difficulty)
    {
        var record = GetRecord(songId);
        if (record == null)
        {
            record = new SongRecord { SongId = songId };
            SongRecords.Add(record);
        }
        
        record.PlayCount++;
        record.LastPlayed = DateTime.Now;
        
        if (score > record.HighScore)
        {
            record.HighScore = score;
            record.HighScoreGrade = grade;
            record.HighScoreDifficulty = difficulty;
        }
        
        if (maxCombo > record.MaxCombo)
        {
            record.MaxCombo = maxCombo;
        }
        
        Save();
    }
}

public class SongRecord
{
    public string SongId { get; set; }
    public int PlayCount { get; set; }
    public int HighScore { get; set; }
    public string HighScoreGrade { get; set; }
    public GameManager.Difficulty HighScoreDifficulty { get; set; }
    public int MaxCombo { get; set; }
    public DateTime LastPlayed { get; set; }
    public bool FullCombo { get; set; }
    public bool AllPerfect { get; set; }
}

public class AchievementRecord
{
    public string AchievementId { get; set; }
    public bool Unlocked { get; set; }
    public DateTime UnlockTime { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
}