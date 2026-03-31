using Godot;
using System;
using System.Collections.Generic;

namespace RhythmX;

/// <summary>
/// Handles scoring, combos, and grade calculation
/// </summary>
public partial class ScoreManager : Node
{
    public static ScoreManager Instance { get; private set; }
    
    // Timing windows (ms)
    private const float PerfectWindow = 45f;
    private const float GreatWindow = 90f;
    private const float GoodWindow = 135f;
    
    // Scoring
    private const int PerfectScore = 300;
    private const int GreatScore = 200;
    private const int GoodScore = 100;
    
    // Combo multipliers
    private static readonly int[] ComboThresholds = { 10, 25, 50, 100, 200, 500 };
    private static readonly float[] ComboMultipliers = { 1.0f, 1.1f, 1.2f, 1.3f, 1.5f, 2.0f };
    
    public enum Judgment
    {
        Perfect,
        Great,
        Good,
        Miss
    }
    
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int PerfectCount { get; private set; }
    public int GreatCount { get; private set; }
    public int GoodCount { get; private set; }
    public int MissCount { get; private set; }
    public int TotalNotes { get; private set; }
    public float Accuracy { get; private set; }
    
    public string Grade => CalculateGrade();
    public bool IsFullCombo => MissCount == 0 && TotalNotes > 0;
    public bool IsAllPerfect => PerfectCount == TotalNotes && TotalNotes > 0;
    
    public event Action<Judgment, int> OnJudgment;
    public event Action<int> OnComboChanged;
    public event Action<int> OnScoreChanged;
    
    private readonly List<JudgmentRecord> _judgments = new();
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    public void Initialize(int totalNotes)
    {
        TotalNotes = totalNotes;
        ResetScore();
    }
    
    public void ResetScore()
    {
        Score = 0;
        Combo = 0;
        MaxCombo = 0;
        PerfectCount = 0;
        GreatCount = 0;
        GoodCount = 0;
        MissCount = 0;
        Accuracy = 1f;
        _judgments.Clear();
    }
    
    public Judgment JudgeHit(double hitTime, double targetTime)
    {
        double offsetMs = Math.Abs(hitTime - targetTime) * 1000;
        
        Judgment judgment;
        if (offsetMs <= PerfectWindow)
            judgment = Judgment.Perfect;
        else if (offsetMs <= GreatWindow)
            judgment = Judgment.Great;
        else if (offsetMs <= GoodWindow)
            judgment = Judgment.Good;
        else
            judgment = Judgment.Miss;
        
        ProcessJudgment(judgment, hitTime - targetTime);
        return judgment;
    }
    
    public void ProcessMiss()
    {
        ProcessJudgment(Judgment.Miss, 0);
    }
    
    private void ProcessJudgment(Judgment judgment, double offset)
    {
        _judgments.Add(new JudgmentRecord
        {
            JudgmentType = judgment,
            Offset = offset,
            Time = AudioManager.Instance?.CurrentTime ?? 0
        });
        
        switch (judgment)
        {
            case Judgment.Perfect:
                PerfectCount++;
                Combo++;
                AddScore(PerfectScore);
                break;
            case Judgment.Great:
                GreatCount++;
                Combo++;
                AddScore(GreatScore);
                break;
            case Judgment.Good:
                GoodCount++;
                Combo++;
                AddScore(GoodScore);
                break;
            case Judgment.Miss:
                MissCount++;
                Combo = 0;
                break;
        }
        
        if (Combo > MaxCombo)
            MaxCombo = Combo;
        
        UpdateAccuracy();
        
        OnJudgment?.Invoke(judgment, Combo);
        OnComboChanged?.Invoke(Combo);
        OnScoreChanged?.Invoke(Score);
    }
    
    private void AddScore(int baseScore)
    {
        float multiplier = GetComboMultiplier();
        Score += (int)(baseScore * multiplier);
    }
    
    private float GetComboMultiplier()
    {
        for (int i = ComboThresholds.Length - 1; i >= 0; i--)
        {
            if (Combo >= ComboThresholds[i])
                return ComboMultipliers[i];
        }
        return 1.0f;
    }
    
    private void UpdateAccuracy()
    {
        if (TotalNotes == 0)
        {
            Accuracy = 1f;
            return;
        }
        
        float totalWeight = PerfectCount * 1f + GreatCount * 0.7f + GoodCount * 0.4f;
        float maxWeight = PerfectCount + GreatCount + GoodCount + MissCount;
        Accuracy = totalWeight / maxWeight;
    }
    
    private string CalculateGrade()
    {
        if (TotalNotes == 0) return "D";
        
        if (IsAllPerfect) return "S+";
        if (IsFullCombo && Accuracy >= 0.98f) return "S+";
        if (Accuracy >= 0.95f) return "S";
        if (Accuracy >= 0.90f) return "A";
        if (Accuracy >= 0.80f) return "B";
        if (Accuracy >= 0.70f) return "C";
        return "D";
    }
    
    public ScoreResult GetResult()
    {
        return new ScoreResult
        {
            Score = Score,
            MaxCombo = MaxCombo,
            PerfectCount = PerfectCount,
            GreatCount = GreatCount,
            GoodCount = GoodCount,
            MissCount = MissCount,
            TotalNotes = TotalNotes,
            Accuracy = Accuracy,
            Grade = Grade,
            IsFullCombo = IsFullCombo,
            IsAllPerfect = IsAllPerfect
        };
    }
}

public struct JudgmentRecord
{
    public ScoreManager.Judgment JudgmentType;
    public double Offset;
    public double Time;
}

public class ScoreResult
{
    public int Score { get; set; }
    public int MaxCombo { get; set; }
    public int PerfectCount { get; set; }
    public int GreatCount { get; set; }
    public int GoodCount { get; set; }
    public int MissCount { get; set; }
    public int TotalNotes { get; set; }
    public float Accuracy { get; set; }
    public string Grade { get; set; }
    public bool IsFullCombo { get; set; }
    public bool IsAllPerfect { get; set; }
}