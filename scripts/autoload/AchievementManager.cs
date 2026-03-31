using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Achievement system manager
/// </summary>
public partial class AchievementManager : Node
{
    public static AchievementManager Instance { get; private set; }
    
    private readonly Dictionary<string, AchievementDefinition> _definitions = new();
    private readonly Dictionary<string, AchievementRecord> _records = new();
    private readonly List<AchievementDefinition> _hiddenAchievements = new();
    
    public Dictionary<string, AchievementDefinition> Definitions => _definitions;
    
    public event Action<AchievementRecord> OnAchievementUnlock;
    public event Action OnAchievementsLoaded;
    
    public override void _Ready()
    {
        Instance = this;
        InitializeAchievements();
        LoadRecords();
    }
    
    private void InitializeAchievements()
    {
        // Regular achievements
        AddAchievement("first_clear", "First Steps", "Clear your first song", 1, false);
        AddAchievement("first_import", "Music Collector", "Import your first local song", 1, false);
        AddAchievement("combo_50", "Combo Starter", "Achieve 50 combo", 50, false);
        AddAchievement("combo_100", "Combo Master", "Achieve 100 combo", 100, false);
        AddAchievement("combo_500", "Combo Legend", "Achieve 500 combo", 500, false);
        AddAchievement("combo_1000", "Combo King", "Achieve 1000 combo", 1000, false);
        AddAchievement("collection_10", "Song Library", "Collect 10 songs", 10, false);
        AddAchievement("collection_50", "Music Archive", "Collect 50 songs", 50, false);
        AddAchievement("first_s_rank", "S-Rank Achiever", "Get an S rank on any song", 1, false);
        AddAchievement("first_full_combo", "Full Combo!", "Achieve full combo on any song", 1, false);
        AddAchievement("first_all_perfect", "Perfect Player", "Get all perfect on any song", 1, false);
        AddAchievement("chart_create_1", "Chart Creator", "Create your first chart", 1, false);
        AddAchievement("chart_create_10", "Chart Architect", "Create 10 charts", 10, false);
        
        // Hidden achievements
        AddAchievement("hidden_code_sound", "???", "Unknown achievement", 100, true, "Hidden Track: Code of Sound");
        AddAchievement("hidden_silent", "???", "Unknown achievement", 20, true, "Hidden Track: Silent Challenge");
        AddAchievement("hidden_reverse", "???", "Unknown achievement", 1, true, "Hidden Track: Reverse World");
        AddAchievement("hidden_chaos", "???", "Unknown achievement", 100, true, "Hidden Track: Chaos Maze");
        AddAchievement("hidden_developer", "???", "Unknown achievement", 100, true, "Hidden Track: Developer Mode");
    }
    
    private void AddAchievement(string id, string name, string description, int target, bool isHidden, string reward = null)
    {
        var def = new AchievementDefinition
        {
            Id = id,
            Name = name,
            Description = description,
            Target = target,
            IsHidden = isHidden,
            Reward = reward
        };
        
        _definitions[id] = def;
        
        if (isHidden)
            _hiddenAchievements.Add(def);
    }
    
    private void LoadRecords()
    {
        var playerData = PlayerData.Load();
        
        foreach (var record in playerData.Achievements)
        {
            _records[record.AchievementId] = record;
        }
        
        // Initialize missing achievements
        foreach (var defId in _definitions.Keys)
        {
            if (!_records.ContainsKey(defId))
            {
                _records[defId] = new AchievementRecord
                {
                    AchievementId = defId,
                    Unlocked = false,
                    Progress = 0,
                    Target = _definitions[defId].Target
                };
            }
        }
        
        OnAchievementsLoaded?.Invoke();
    }
    
    public void UnlockAchievement(string achievementId)
    {
        if (!_definitions.TryGetValue(achievementId, out var definition)) return;
        if (!_records.TryGetValue(achievementId, out var record)) return;
        if (record.Unlocked) return;
        
        record.Unlocked = true;
        record.UnlockTime = DateTime.Now;
        record.Progress = definition.Target;
        
        SaveRecords();
        OnAchievementUnlock?.Invoke(record);
        
        GD.Print($"Achievement unlocked: {definition.Name}");
    }
    
    public void UpdateProgress(string achievementId, int progress)
    {
        if (!_definitions.TryGetValue(achievementId, out var definition)) return;
        if (!_records.TryGetValue(achievementId, out var record)) return;
        if (record.Unlocked) return;
        
        record.Progress = Math.Max(record.Progress, progress);
        
        if (record.Progress >= definition.Target)
        {
            UnlockAchievement(achievementId);
        }
        else
        {
            SaveRecords();
        }
    }
    
    public AchievementRecord GetRecord(string achievementId)
    {
        return _records.TryGetValue(achievementId, out var record) ? record : null;
    }
    
    public List<AchievementRecord> GetAllRecords()
    {
        return new List<AchievementRecord>(_records.Values);
    }
    
    public List<AchievementRecord> GetVisibleAchievements()
    {
        var visible = new List<AchievementRecord>();
        foreach (var record in _records.Values)
        {
            if (_definitions.TryGetValue(record.AchievementId, out var def))
            {
                if (!def.IsHidden || record.Unlocked)
                    visible.Add(record);
            }
        }
        return visible;
    }
    
    public int GetTotalProgress()
    {
        int total = 0;
        int unlocked = 0;
        
        foreach (var def in _definitions.Values)
        {
            if (!def.IsHidden)
            {
                total++;
                if (_records.TryGetValue(def.Id, out var record) && record.Unlocked)
                    unlocked++;
            }
        }
        
        return total > 0 ? unlocked * 100 / total : 0;
    }
    
    public int GetUnlockedCount()
    {
        int count = 0;
        foreach (var record in _records.Values)
        {
            if (record.Unlocked) count++;
        }
        return count;
    }
    
    private void SaveRecords()
    {
        var playerData = PlayerData.Load();
        playerData.Achievements = new List<AchievementRecord>(_records.Values);
        playerData.Save();
    }
    
    public void OnSongClear(string songId, ScoreResult result, GameManager.Difficulty difficulty)
    {
        // First clear
        if (GetUnlockedCount() == 0)
            UnlockAchievement("first_clear");
        
        // Combo achievements
        if (result.MaxCombo >= 50) UpdateProgress("combo_50", result.MaxCombo);
        if (result.MaxCombo >= 100) UpdateProgress("combo_100", result.MaxCombo);
        if (result.MaxCombo >= 500) UpdateProgress("combo_500", result.MaxCombo);
        if (result.MaxCombo >= 1000) UpdateProgress("combo_1000", result.MaxCombo);
        
        if (result.IsFullCombo)
            UnlockAchievement("first_full_combo");
        
        if (result.IsAllPerfect)
            UnlockAchievement("first_all_perfect");
        
        if (result.Grade == "S" || result.Grade == "S+")
            UnlockAchievement("first_s_rank");
    }
    
    public void OnSongImport()
    {
        var playerData = PlayerData.Load();
        
        if (playerData.ImportedSongs.Count == 1)
            UnlockAchievement("first_import");
        
        UpdateProgress("collection_10", playerData.ImportedSongs.Count);
        UpdateProgress("collection_50", playerData.ImportedSongs.Count);
    }
    
    public void OnChartCreated()
    {
        var playerData = PlayerData.Load();
        
        if (playerData.CreatedCharts.Count == 1)
            UnlockAchievement("chart_create_1");
        
        UpdateProgress("chart_create_10", playerData.CreatedCharts.Count);
    }
}