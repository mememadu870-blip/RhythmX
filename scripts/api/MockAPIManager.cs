using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RhythmX;

/// <summary>
/// Mock implementation of API - returns fake data for development/testing
/// Implements IAPIManager interface for easy swapping with real API later
/// </summary>
public partial class MockAPIManager : Node, IAPIManager
{
    private bool _isOnline = true;
    private string _currentUserId = "mock_user_001";
    private string _currentToken = "mock_token_abc123";
    
    private Dictionary<string, SongData> _mockSongs;
    private Dictionary<string, List<LeaderboardEntry>> _mockLeaderboards;
    private List<AchievementDefinition> _mockAchievements;
    
    private Random _random = new Random();
    
    public override void _Ready()
    {
        InitializeMockData();
    }
    
    private void InitializeMockData()
    {
        _mockSongs = new Dictionary<string, SongData>();
        
        // Create 10 mock songs
        for (int i = 1; i <= 10; i++)
        {
            var song = CreateMockSong(i);
            _mockSongs[song.Id] = song;
        }
        
        // Create mock leaderboards for each song + difficulty
        _mockLeaderboards = new Dictionary<string, List<LeaderboardEntry>>();
        foreach (var songId in _mockSongs.Keys)
        {
            for (int diff = 0; diff <= 3; diff++)
            {
                var key = $"{songId}_{diff}";
                _mockLeaderboards[key] = CreateMockLeaderboard(songId, (GameManager.Difficulty)diff);
            }
        }
        
        // Create mock achievements
        _mockAchievements = CreateMockAchievements();
        
        GD.Print($"MockAPIManager initialized with {_mockSongs.Count} songs, {_mockAchievements.Count} achievements");
    }
    
    #region Mock Data Generation
    
    private SongData CreateMockSong(int index)
    {
        var song = new SongData
        {
            Id = $"song_{index:00}",
            Name = GetMockSongName(index),
            Artist = GetMockArtistName(index),
            Bpm = 120 + (index * 10),
            Duration = 180 + (index * 15),
            AudioPath = "", // Mock - no actual audio
            IsImported = false,
            IsFavorite = index <= 2,
            PlayCount = index <= 5 ? index * 10 : 0,
            HighScore = index <= 5 ? _random.Next(800000, 1000000) : 0
        };
        
        // Add charts for each difficulty
        song.Charts = new List<ChartData>();
        for (int d = 0; d < 4; d++)
        {
            var chart = new ChartData
            {
                Id = $"{song.Id}_chart_{d}",
                Difficulty = (GameManager.Difficulty)d,
                TrackCount = 4,
                Bpm = song.Bpm,
                Offset = 0.1
            };
            
            // Generate mock notes
            float density = GameManager.DifficultyDensityMultipliers[d];
            int noteCount = (int)(density * 100 * (song.Duration / 180));
            chart.Notes = GenerateMockNotes(noteCount, song.Duration, chart.TrackCount, (GameManager.Difficulty)d);
            
            song.Charts.Add(chart);
        }
        
        return song;
    }
    
    private List<NoteData> GenerateMockNotes(int count, double duration, int tracks, GameManager.Difficulty difficulty)
    {
        var notes = new List<NoteData>();
        double interval = duration / count;
        
        for (int i = 0; i < count; i++)
        {
            var note = new NoteData
            {
                Time = i * interval,
                Track = _random.Next(0, tracks),
                Type = NoteType.Tap
            };
            
            // Add hold notes for harder difficulties
            if (difficulty >= GameManager.Difficulty.Hard && _random.NextDouble() < 0.15)
            {
                note.Type = NoteType.Hold;
                note.EndTime = note.Time + _random.NextDouble() * 1.5 + 0.3;
            }
            
            // Add swipe notes for expert
            if (difficulty == GameManager.Difficulty.Expert && _random.NextDouble() < 0.1)
            {
                note.Type = NoteType.Swipe;
                note.SwipeDirection = (SwipeDirection)_random.Next(1, 5);
            }
            
            notes.Add(note);
        }
        
        // Sort by time
        notes.Sort((a, b) => a.Time.CompareTo(b.Time));
        return notes;
    }
    
    private string GetMockSongName(int index)
    {
        var names = new[]
        {
            "Neon Pulsar", "Digital Dreams", "Crystal Wave", "Thunder Strike",
            "Midnight Run", "Starlight Serenade", "Electric Soul", "Cosmic Journey",
            "Rainbow Road", "Final Frontier"
        };
        return names[Math.Min(index - 1, names.Length - 1)];
    }
    
    private string GetMockArtistName(int index)
    {
        var artists = new[]
        {
            "Synthwave Masters", "Chiptune Collective", "Electronic Dreams", "Bass Warriors",
            "Melody Makers", "Rhythm Rebels", "Sound Architects", "Beat Breakers",
            "Audio Alchemists", "Music Machines"
        };
        return artists[Math.Min(index - 1, artists.Length - 1)];
    }
    
    private List<LeaderboardEntry> CreateMockLeaderboard(string songId, GameManager.Difficulty difficulty)
    {
        var entries = new List<LeaderboardEntry>();
        var grades = new[] { "S+", "S", "A", "A", "B", "B", "C", "D" };
        
        for (int i = 1; i <= 20; i++)
        {
            entries.Add(new LeaderboardEntry
            {
                Rank = i,
                UserId = $"user_{i:00}",
                Nickname = GetMockPlayerName(i),
                Score = Math.Max(100000, 1000000 - (i * 40000) + _random.Next(-5000, 5000)),
                Grade = grades[Math.Min(i - 1, grades.Length - 1)],
                MaxCombo = Math.Max(10, 200 - (i * 8) + _random.Next(-5, 5)),
                PlayTime = DateTime.Now.AddDays(-_random.Next(1, 30))
            });
        }
        
        return entries;
    }
    
    private string GetMockPlayerName(int index)
    {
        var names = new[]
        {
            "ProPlayer", "RhythmKing", "BeatMaster", "NoteHunter",
            "ComboQueen", "SpeedDemon", "MusicLover", "ChartMaker",
            "SonicWave", "NightCore", "DayCore", "BassDrop",
            "MelodyChaser", "SoundSeeker", "TempoTamer", "FlowFinder",
            "GrooveGuardian", "PulsePounder", "SyncSurfer", "BeatBender"
        };
        return names[Math.Min(index - 1, names.Length - 1)];
    }
    
    private List<AchievementDefinition> CreateMockAchievements()
    {
        return new List<AchievementDefinition>
        {
            // Regular achievements
            new AchievementDefinition { Id = "first_clear", Name = "First Steps", Description = "Clear your first song", Target = 1, IsHidden = false },
            new AchievementDefinition { Id = "first_import", Name = "Music Collector", Description = "Import your first local song", Target = 1, IsHidden = false },
            new AchievementDefinition { Id = "combo_50", Name = "Combo Starter", Description = "Achieve 50 combo", Target = 50, IsHidden = false },
            new AchievementDefinition { Id = "combo_100", Name = "Combo Master", Description = "Achieve 100 combo", Target = 100, IsHidden = false },
            new AchievementDefinition { Id = "combo_500", Name = "Combo Legend", Description = "Achieve 500 combo", Target = 500, IsHidden = false },
            new AchievementDefinition { Id = "combo_1000", Name = "Combo King", Description = "Achieve 1000 combo", Target = 1000, IsHidden = false },
            new AchievementDefinition { Id = "collection_10", Name = "Song Library", Description = "Collect 10 songs", Target = 10, IsHidden = false },
            new AchievementDefinition { Id = "collection_50", Name = "Music Archive", Description = "Collect 50 songs", Target = 50, IsHidden = false },
            new AchievementDefinition { Id = "first_s_rank", Name = "S-Rank Achiever", Description = "Get an S rank on any song", Target = 1, IsHidden = false },
            new AchievementDefinition { Id = "first_full_combo", Name = "Full Combo!", Description = "Achieve full combo on any song", Target = 1, IsHidden = false },
            new AchievementDefinition { Id = "first_all_perfect", Name = "Perfect Player", Description = "Get all perfect on any song", Target = 1, IsHidden = false },
            new AchievementDefinition { Id = "chart_create_1", Name = "Chart Creator", Description = "Create your first chart", Target = 1, IsHidden = false },
            new AchievementDefinition { Id = "chart_create_10", Name = "Chart Architect", Description = "Create 10 charts", Target = 10, IsHidden = false },
            new AchievementDefinition { Id = "play_100", Name = "Dedicated Player", Description = "Play 100 songs", Target = 100, IsHidden = false },
            new AchievementDefinition { Id = "play_500", Name = "Rhythm Enthusiast", Description = "Play 500 songs", Target = 500, IsHidden = false },
            
            // Hidden achievements
            new AchievementDefinition { Id = "hidden_1", Name = "???", Description = "Unknown achievement", Target = 100, IsHidden = true, Reward = "Hidden Track: Code of Sound" },
            new AchievementDefinition { Id = "hidden_2", Name = "???", Description = "Unknown achievement", Target = 20, IsHidden = true, Reward = "Hidden Track: Silent Challenge" },
            new AchievementDefinition { Id = "hidden_3", Name = "???", Description = "Unknown achievement", Target = 1, IsHidden = true, Reward = "Hidden Track: Reverse World" },
            new AchievementDefinition { Id = "hidden_4", Name = "???", Description = "Unknown achievement", Target = 100, IsHidden = true, Reward = "Hidden Track: Chaos Maze" },
            new AchievementDefinition { Id = "hidden_5", Name = "???", Description = "Unknown achievement", Target = 100, IsHidden = true, Reward = "Hidden Track: Developer Mode" }
        };
    }
    
    #endregion
    
    #region IAPIManager Implementation
    
    // === Authentication ===
    
    public async Task<AuthResult> SendOTP(string phoneNumber)
    {
        await SimulateNetworkDelay();
        
        return new AuthResult
        {
            Success = true,
            Message = "OTP sent successfully (Mock)",
            ErrorCode = 0
        };
    }
    
    public async Task<AuthResult> VerifyOTP(string phoneNumber, string code)
    {
        await SimulateNetworkDelay();
        
        // Mock: accept any 6-digit code
        if (code.Length == 6)
        {
            _currentUserId = "mock_user_" + phoneNumber.GetHashCode();
            _currentToken = "mock_token_" + Guid.NewGuid().ToString().Substring(0, 8);
            
            // Store auth
            APIConfig.SetUserId(_currentUserId);
            APIConfig.SetToken(_currentToken);
            
            return new AuthResult
            {
                Success = true,
                Message = "Login successful (Mock)",
                UserId = _currentUserId,
                Token = _currentToken,
                Nickname = "MockPlayer",
                ErrorCode = 0
            };
        }
        
        return new AuthResult
        {
            Success = false,
            Message = "Invalid OTP (Mock)",
            ErrorCode = 401
        };
    }
    
    public async Task<AuthResult> GetCurrentUser()
    {
        await SimulateNetworkDelay();
        
        bool hasAuth = !string.IsNullOrEmpty(APIConfig.GetToken());
        
        return new AuthResult
        {
            Success = hasAuth,
            Message = hasAuth ? "User retrieved (Mock)" : "Not authenticated (Mock)",
            UserId = hasAuth ? APIConfig.GetUserId() : null,
            Token = hasAuth ? APIConfig.GetToken() : null,
            Nickname = hasAuth ? "MockPlayer" : null,
            ErrorCode = hasAuth ? 0 : 401
        };
    }
    
    public async Task<bool> Logout()
    {
        await SimulateNetworkDelay();
        
        _currentUserId = null;
        _currentToken = null;
        APIConfig.ClearAuth();
        
        return true;
    }
    
    // === Songs & Charts ===
    
    public async Task<List<SongData>> GetSongList(int page = 0, int limit = 20)
    {
        await SimulateNetworkDelay();
        
        var songs = new List<SongData>();
        
        foreach (var song in _mockSongs.Values)
        {
            songs.Add(song);
        }
        
        // Pagination
        int start = page * limit;
        int end = Math.Min(start + limit, songs.Count);
        
        if (start >= songs.Count)
            return new List<SongData>();
        
        return songs.GetRange(start, end - start);
    }
    
    public async Task<SongData> GetSongDetail(string songId)
    {
        await SimulateNetworkDelay();
        
        if (_mockSongs.TryGetValue(songId, out var song))
        {
            return song;
        }
        
        return null;
    }
    
    public async Task<List<ChartData>> GetCommunityCharts(string songId)
    {
        await SimulateNetworkDelay();
        
        // Return mock community charts
        var charts = new List<ChartData>();
        for (int i = 0; i < 3; i++)
        {
            charts.Add(new ChartData
            {
                Id = $"{songId}_community_{i}",
                Difficulty = (GameManager.Difficulty)_random.Next(0, 4),
                TrackCount = 4,
                Bpm = 120 + _random.Next(0, 60)
            });
        }
        
        return charts;
    }
    
    public async Task<UploadResult> UploadChart(ChartData chart, string songId)
    {
        await SimulateNetworkDelay();
        
        return new UploadResult
        {
            Success = true,
            Message = "Chart uploaded successfully (Mock)",
            ChartId = chart.Id + "_uploaded",
            DownloadUrl = "https://mock.storage.rhythmx.app/" + chart.Id
        };
    }
    
    // === Player Data Sync ===
    
    public async Task<PlayerData> SyncPlayerData(PlayerData localData)
    {
        await SimulateNetworkDelay();
        
        // Mock: merge with server data
        if (!string.IsNullOrEmpty(APIConfig.GetUserId()))
        {
            localData.PlayerId = APIConfig.GetUserId();
        }
        localData.LastPlayDate = DateTime.Now;
        
        APIConfig.SetLastSyncTime(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        
        return localData;
    }
    
    public async Task<bool> UploadPlayRecord(PlayRecordRequest record)
    {
        await SimulateNetworkDelay();
        return true;
    }
    
    public async Task<List<LeaderboardEntry>> GetLeaderboard(string songId, GameManager.Difficulty difficulty)
    {
        await SimulateNetworkDelay();
        
        var key = $"{songId}_{(int)difficulty}";
        if (_mockLeaderboards.TryGetValue(key, out var entries))
        {
            return entries;
        }
        
        return new List<LeaderboardEntry>();
    }
    
    // === Achievements ===
    
    public async Task<List<AchievementDefinition>> GetAchievementDefinitions()
    {
        await SimulateNetworkDelay();
        return _mockAchievements;
    }
    
    public async Task<bool> SyncAchievements(List<AchievementRecord> achievements)
    {
        await SimulateNetworkDelay();
        return true;
    }
    
    // === Health Check ===
    
    public async Task<bool> IsOnline()
    {
        await Task.Delay(100);
        return _isOnline;
    }
    
    public async Task<APIStatus> GetAPIStatus()
    {
        await Task.Delay(100);
        
        return new APIStatus
        {
            IsOnline = _isOnline,
            Version = "1.0.0-mock",
            ServerTime = DateTime.UtcNow,
            MaintenanceMode = 0,
            Message = "Mock API is running"
        };
    }
    
    #endregion
    
    #region Utility
    
    private async Task SimulateNetworkDelay()
    {
        // Random delay between 100-500ms to simulate network latency
        await Task.Delay(_random.Next(100, 500));
    }
    
    /// <summary>
    /// For testing: toggle offline mode
    /// </summary>
    public void SetOnlineStatus(bool isOnline)
    {
        _isOnline = isOnline;
        GD.Print($"MockAPI online status set to: {isOnline}");
    }
    
    /// <summary>
    /// For testing: clear all mock data and regenerate
    /// </summary>
    public void ResetMockData()
    {
        InitializeMockData();
    }
    
    #endregion
}