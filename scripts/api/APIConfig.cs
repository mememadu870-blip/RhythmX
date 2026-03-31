using Godot;
using System;

namespace RhythmX;

/// <summary>
/// API configuration - endpoints and settings
/// </summary>
public static class APIConfig
{
    // Real API endpoints (for future implementation)
    public static readonly string BaseUrl = "https://api.rhythmx.app/v1";
    public static readonly string AuthEndpoint = "/auth";
    public static readonly string SongsEndpoint = "/songs";
    public static readonly string ChartsEndpoint = "/charts";
    public static readonly string SyncEndpoint = "/sync";
    public static readonly string LeaderboardEndpoint = "/leaderboard";
    public static readonly string AchievementsEndpoint = "/achievements";
    
    // Settings
    public static readonly int RequestTimeoutSeconds = 30;
    public static readonly int MaxRetryCount = 3;
    public static readonly int RetryDelayMs = 1000;
    
    // Storage keys
    public static readonly string TokenKey = "rhythmx_token";
    public static readonly string UserIdKey = "rhythmx_user_id";
    public static readonly string LastSyncKey = "rhythmx_last_sync";
    
    // Get stored token
    public static string GetToken()
    {
        var config = new ConfigFile();
        if (config.Load("user://api_config.cfg") == Error.Ok)
        {
            return (string)config.GetValue("auth", "token", "");
        }
        return "";
    }
    
    public static void SetToken(string token)
    {
        var config = new ConfigFile();
        config.Load("user://api_config.cfg");
        config.SetValue("auth", "token", token);
        config.Save("user://api_config.cfg");
    }
    
    public static string GetUserId()
    {
        var config = new ConfigFile();
        if (config.Load("user://api_config.cfg") == Error.Ok)
        {
            return (string)config.GetValue("auth", "user_id", "");
        }
        return "";
    }
    
    public static void SetUserId(string userId)
    {
        var config = new ConfigFile();
        config.Load("user://api_config.cfg");
        config.SetValue("auth", "user_id", userId);
        config.Save("user://api_config.cfg");
    }
    
    public static void ClearAuth()
    {
        var config = new ConfigFile();
        config.Load("user://api_config.cfg");
        config.SetValue("auth", "token", "");
        config.SetValue("auth", "user_id", "");
        config.Save("user://api_config.cfg");
    }
    
    public static long GetLastSyncTime()
    {
        var config = new ConfigFile();
        if (config.Load("user://api_config.cfg") == Error.Ok)
        {
            return (long)config.GetValue("sync", "last_sync", 0L);
        }
        return 0;
    }
    
    public static void SetLastSyncTime(long timestamp)
    {
        var config = new ConfigFile();
        config.Load("user://api_config.cfg");
        config.SetValue("sync", "last_sync", timestamp);
        config.Save("user://api_config.cfg");
    }
    
    public static bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(GetToken()) && !string.IsNullOrEmpty(GetUserId());
    }
}