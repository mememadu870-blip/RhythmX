using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RhythmX;

/// <summary>
/// Interface for all API operations - allows swapping between Mock and real implementation
/// </summary>
public interface IAPIManager
{
    // Authentication
    Task<AuthResult> SendOTP(string phoneNumber);
    Task<AuthResult> VerifyOTP(string phoneNumber, string code);
    Task<AuthResult> GetCurrentUser();
    Task<bool> Logout();
    
    // Songs & Charts
    Task<List<SongData>> GetSongList(int page = 0, int limit = 20);
    Task<SongData> GetSongDetail(string songId);
    Task<List<ChartData>> GetCommunityCharts(string songId);
    Task<UploadResult> UploadChart(ChartData chart, string songId);
    
    // Player Data Sync
    Task<PlayerData> SyncPlayerData(PlayerData localData);
    Task<bool> UploadPlayRecord(PlayRecordRequest record);
    Task<List<LeaderboardEntry>> GetLeaderboard(string songId, GameManager.Difficulty difficulty);
    
    // Achievements
    Task<List<AchievementDefinition>> GetAchievementDefinitions();
    Task<bool> SyncAchievements(List<AchievementRecord> achievements);
    
    // Health Check
    Task<bool> IsOnline();
    Task<APIStatus> GetAPIStatus();
}

#region Data Types

[Serializable]
public class AuthResult
{
    public bool Success;
    public string Message;
    public string UserId;
    public string Token;
    public string Nickname;
    public int ErrorCode;
}

[Serializable]
public class UploadResult
{
    public bool Success;
    public string Message;
    public string ChartId;
    public string DownloadUrl;
}

[Serializable]
public class PlayRecordRequest
{
    public string SongId;
    public string ChartId;
    public GameManager.Difficulty Difficulty;
    public int Score;
    public int MaxCombo;
    public string Grade;
    public int PerfectCount;
    public int GreatCount;
    public int GoodCount;
    public int MissCount;
    public double Accuracy;
    public bool IsFullCombo;
    public bool IsAllPerfect;
    public DateTime PlayTime;
}

[Serializable]
public class LeaderboardEntry
{
    public int Rank;
    public string UserId;
    public string Nickname;
    public int Score;
    public string Grade;
    public int MaxCombo;
    public DateTime PlayTime;
}

[Serializable]
public class AchievementDefinition
{
    public string Id;
    public string Name;
    public string Description;
    public int Target;
    public bool IsHidden;
    public string IconPath;
    public string Reward;
}

[Serializable]
public class APIStatus
{
    public bool IsOnline;
    public string Version;
    public DateTime ServerTime;
    public int MaintenanceMode;
    public string Message;
}

#endregion