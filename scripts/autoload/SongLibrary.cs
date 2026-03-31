using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RhythmX;

/// <summary>
/// Manages song library - import, save, load songs
/// </summary>
public partial class SongLibrary : Node
{
    public static SongLibrary Instance { get; private set; }
    
    public List<SongData> Songs { get; private set; } = new();
    public List<SongData> ImportedSongs { get; private set; } = new();
    
    public event Action<SongData> OnSongImported;
    public event Action OnLibraryUpdated;
    
    private string _importedPath;
    
    public override void _Ready()
    {
        Instance = this;
        _importedPath = "user://imported_songs";
        
        DirAccess.MakeDirAbsolute(_importedPath);
        LoadLibrary();
    }
    
    private void LoadLibrary()
    {
        Songs.Clear();
        ImportedSongs.Clear();
        
        // Load mock songs for testing
        LoadMockSongs();
        
        // Load imported songs
        using var dir = DirAccess.Open(_importedPath);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
                {
                    var song = LoadSongFromJson($"{_importedPath}/{fileName}");
                    if (song != null)
                    {
                        ImportedSongs.Add(song);
                        Songs.Add(song);
                    }
                }
                fileName = dir.GetNext();
            }
        }
        
        OnLibraryUpdated?.Invoke();
    }
    
    private void LoadMockSongs()
    {
        var mockNames = new[] { "Neon Pulsar", "Digital Dreams", "Crystal Wave", "Thunder Strike", "Midnight Run",
            "Starlight Serenade", "Electric Soul", "Cosmic Journey", "Rainbow Road", "Final Frontier" };
        var mockArtists = new[] { "Synthwave Masters", "Chiptune Collective", "Electronic Dreams", "Bass Warriors", "Melody Makers",
            "Rhythm Rebels", "Sound Architects", "Beat Breakers", "Audio Alchemists", "Music Machines" };
        
        for (int i = 0; i < 10; i++)
        {
            var song = new SongData
            {
                Id = $"song_{i:00}",
                Name = mockNames[i],
                Artist = mockArtists[i],
                Bpm = 120 + i * 10,
                Duration = 180 + i * 15,
                IsImported = false,
                IsFavorite = i < 2,
                PlayCount = i < 5 ? i * 10 : 0,
                HighScore = i < 5 ? 800000 + i * 20000 : 0
            };
            
            // Generate mock charts for each difficulty
            for (int d = 0; d < 4; d++)
            {
                var chart = GenerateMockChart(song, (GameManager.Difficulty)d);
                song.Charts.Add(chart);
            }
            
            Songs.Add(song);
        }
    }
    
    private ChartData GenerateMockChart(SongData song, GameManager.Difficulty difficulty)
    {
        var chart = new ChartData
        {
            Id = $"{song.Id}_chart_{(int)difficulty}",
            Difficulty = difficulty,
            TrackCount = 4,
            Bpm = song.Bpm,
            Offset = 0
        };
        
        // Generate notes based on difficulty
        int noteCount = (int)(GameManager.DifficultyDensityMultipliers[(int)difficulty] * 100 * (song.Duration / 180));
        double interval = song.Duration / noteCount;
        
        var random = new Random(song.Id.GetHashCode());
        
        for (int i = 0; i < noteCount; i++)
        {
            var note = new NoteData
            {
                Time = i * interval,
                Track = random.Next(4),
                Type = NoteType.Tap
            };
            
            // Add some variety
            if (difficulty >= GameManager.Difficulty.Hard && random.NextDouble() < 0.15)
            {
                note.Type = NoteType.Hold;
                note.EndTime = note.Time + random.Next(1, 3) * 0.5;
            }
            else if (difficulty == GameManager.Difficulty.Expert && random.NextDouble() < 0.1)
            {
                note.Type = NoteType.Swipe;
                note.SwipeDirection = (SwipeDirection)random.Next(1, 5);
            }
            
            chart.Notes.Add(note);
        }
        
        return chart;
    }
    
    private SongData LoadSongFromJson(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return null;
        
        try
        {
            var json = file.GetAsText();
            // Parse JSON and create SongData
            return null; // TODO: Implement JSON parsing
        }
        catch
        {
            return null;
        }
    }
    
    public List<SongData> SearchSongs(string query)
    {
        if (string.IsNullOrEmpty(query)) return Songs;
        
        query = query.ToLower();
        return Songs.FindAll(s => 
            s.Name.ToLower().Contains(query) || 
            s.Artist.ToLower().Contains(query)
        );
    }
    
    public void AddSong(SongData song)
    {
        Songs.Add(song);
        if (song.IsImported)
        {
            ImportedSongs.Add(song);
        }
        OnLibraryUpdated?.Invoke();
    }
    
    public void ToggleFavorite(string songId)
    {
        var song = Songs.Find(s => s.Id == songId);
        if (song != null)
        {
            song.IsFavorite = !song.IsFavorite;
            
            var playerData = PlayerData.Load();
            if (song.IsFavorite)
            {
                if (!playerData.FavoriteSongs.Contains(songId))
                    playerData.FavoriteSongs.Add(songId);
            }
            else
            {
                playerData.FavoriteSongs.Remove(songId);
            }
            playerData.Save();
            
            OnLibraryUpdated?.Invoke();
        }
    }
}