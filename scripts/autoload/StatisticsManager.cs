using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RhythmX;

/// <summary>
/// Statistics tracking
/// </summary>
public partial class StatisticsManager : Node
{
    public static StatisticsManager Instance { get; private set; }
    
    private List<PlaySession> _recentPlays = new();
    private OverallStatistics _overallStats = new();
    
    public override void _Ready()
    {
        Instance = this;
        LoadStatistics();
    }
    
    private void LoadStatistics()
    {
        using var file = FileAccess.Open("user://statistics.json", FileAccess.ModeFlags.Read);
        if (file != null)
        {
            var json = file.GetAsText();
            try
            {
                var data = JsonSerializer.Deserialize<StatisticsData>(json);
                if (data != null)
                {
                    _recentPlays = data.RecentPlays ?? new List<PlaySession>();
                    _overallStats = data.Overall ?? new OverallStatistics();
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }
    }
    
    private void SaveStatistics()
    {
        var data = new StatisticsData
        {
            RecentPlays = _recentPlays,
            Overall = _overallStats
        };
        var json = JsonSerializer.Serialize(data);
        using var file = FileAccess.Open("user://statistics.json", FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }
    
    public void RecordPlay(SongData song, ScoreResult result, GameManager.Difficulty difficulty)
    {
        var session = new PlaySession
        {
            SongId = song.Id,
            SongName = song.Name,
            Difficulty = difficulty,
            Score = result.Score,
            Grade = result.Grade,
            MaxCombo = result.MaxCombo,
            Accuracy = result.Accuracy,
            PlayTime = DateTime.Now
        };
        
        _recentPlays.Insert(0, session);
        if (_recentPlays.Count > 50)
            _recentPlays.RemoveAt(_recentPlays.Count - 1);
        
        _overallStats.TotalPlays++;
        _overallStats.TotalNotes += result.PerfectCount + result.GreatCount + result.GoodCount + result.MissCount;
        
        if (result.MaxCombo > _overallStats.MaxCombo)
            _overallStats.MaxCombo = result.MaxCombo;
        
        SaveStatistics();
    }
    
    public List<PlaySession> GetRecentPlays(int count = 10)
    {
        return _recentPlays.GetRange(0, Math.Min(count, _recentPlays.Count));
    }
    
    public OverallStatistics GetOverallStatistics() => _overallStats;
}

public class PlaySession
{
    public string SongId { get; set; }
    public string SongName { get; set; }
    public GameManager.Difficulty Difficulty { get; set; }
    public int Score { get; set; }
    public string Grade { get; set; }
    public int MaxCombo { get; set; }
    public float Accuracy { get; set; }
    public DateTime PlayTime { get; set; }
}

public class OverallStatistics
{
    public int TotalPlays { get; set; }
    public int TotalNotes { get; set; }
    public int MaxCombo { get; set; }
    public double TotalPlayTime { get; set; }
}

public class StatisticsData
{
    public List<PlaySession> RecentPlays { get; set; }
    public OverallStatistics Overall { get; set; }
}