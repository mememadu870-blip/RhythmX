using Godot;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Achievements UI Controller
/// </summary>
public partial class AchievementsUI : Control
{
    private Label _progressLabel;
    private ProgressBar _progressBar;
    private Label _hiddenLabel;
    private VBoxContainer _achievementList;
    private Button _allTab;
    private Button _regularTab;
    private Button _hiddenTab;
    private Button _backButton;
    
    private int _currentTab = 0; // 0 = All, 1 = Regular, 2 = Hidden
    
    public override void _Ready()
    {
        _progressLabel = GetNode<Label>("SummaryContainer/ProgressLabel");
        _progressBar = GetNode<ProgressBar>("SummaryContainer/ProgressBar");
        _hiddenLabel = GetNode<Label>("SummaryContainer/HiddenLabel");
        _achievementList = GetNode<VBoxContainer>("ScrollContainer/AchievementList");
        _allTab = GetNode<Button>("TabContainer/AllTab");
        _regularTab = GetNode<Button>("TabContainer/RegularTab");
        _hiddenTab = GetNode<Button>("TabContainer/HiddenTab");
        _backButton = GetNode<Button>("Header/BackButton");
        
        if (_allTab != null)
            _allTab.Pressed += () => SelectTab(0);
        if (_regularTab != null)
            _regularTab.Pressed += () => SelectTab(1);
        if (_hiddenTab != null)
            _hiddenTab.Pressed += () => SelectTab(2);
        if (_backButton != null)
            _backButton.Pressed += OnBackPressed;
        
        RefreshList();
    }
    
    private void RefreshList()
    {
        UpdateSummary();
        PopulateAchievements();
    }
    
    private void UpdateSummary()
    {
        if (AchievementManager.Instance == null) return;
        
        int progress = AchievementManager.Instance.GetTotalProgress();
        var records = AchievementManager.Instance.GetAllRecords();
        int unlocked = 0;
        int total = 0;
        int hiddenUnlocked = 0;
        
        foreach (var record in records)
        {
            var def = AchievementManager.Instance.Definitions[record.AchievementId];
            if (!def.IsHidden)
            {
                total++;
                if (record.Unlocked) unlocked++;
            }
            else if (record.Unlocked)
            {
                hiddenUnlocked++;
            }
        }
        
        if (_progressLabel != null)
            _progressLabel.Text = $"{unlocked} / {total} Unlocked";
        
        if (_progressBar != null)
            _progressBar.Value = progress;
        
        if (_hiddenLabel != null)
            _hiddenLabel.Text = $"Hidden: {hiddenUnlocked} / 5";
    }
    
    private void PopulateAchievements()
    {
        if (_achievementList == null || AchievementManager.Instance == null) return;
        
        // Clear existing items
        foreach (var child in _achievementList.GetChildren())
        {
            child.QueueFree();
        }
        
        List<AchievementRecord> records;
        
        switch (_currentTab)
        {
            case 1: // Regular
                records = new List<AchievementRecord>();
                foreach (var r in AchievementManager.Instance.GetAllRecords())
                {
                    if (AchievementManager.Instance.Definitions.TryGetValue(r.AchievementId, out var def))
                    {
                        if (!def.IsHidden) records.Add(r);
                    }
                }
                break;
            case 2: // Hidden (only show unlocked)
                records = new List<AchievementRecord>();
                foreach (var r in AchievementManager.Instance.GetAllRecords())
                {
                    if (r.Unlocked && AchievementManager.Instance.Definitions.TryGetValue(r.AchievementId, out var def))
                    {
                        if (def.IsHidden) records.Add(r);
                    }
                }
                break;
            default: // All visible
                records = AchievementManager.Instance.GetVisibleAchievements();
                break;
        }
        
        foreach (var record in records)
        {
            CreateAchievementItem(record);
        }
    }
    
    private void CreateAchievementItem(AchievementRecord record)
    {
        var container = new HBoxContainer();
        container.CustomMinimumSize = new Vector2(0, 60);
        
        if (AchievementManager.Instance.Definitions.TryGetValue(record.AchievementId, out var def))
        {
            var nameLabel = new Label
            {
                Text = def.IsHidden && !record.Unlocked ? "???" : def.Name,
                SizeFlagsHorizontal = SizeFlags.Expand
            };
            container.AddChild(nameLabel);
            
            var progressLabel = new Label
            {
                Text = record.Unlocked ? "✓" : $"{record.Progress}/{def.Target}"
            };
            container.AddChild(progressLabel);
        }
        
        _achievementList.AddChild(container);
    }
    
    private void SelectTab(int tab)
    {
        _currentTab = tab;
        
        // Update tab button states using Modulate for visual feedback
        if (_allTab != null)
            _allTab.Modulate = tab == 0 ? new Color(0.4f, 1f, 1f) : new Color(1f, 1f, 1f);
        if (_regularTab != null)
            _regularTab.Modulate = tab == 1 ? new Color(0.4f, 1f, 1f) : new Color(1f, 1f, 1f);
        if (_hiddenTab != null)
            _hiddenTab.Modulate = tab == 2 ? new Color(0.8f, 0.3f, 1f) : new Color(1f, 1f, 1f);
        
        PopulateAchievements();
    }
    
    private void OnBackPressed()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }
}