using Godot;
using System;
using System.Threading.Tasks;

namespace RhythmX;

/// <summary>
/// Import Song UI - File dialog and import flow
/// </summary>
public partial class ImportSongUI : Control
{
    public static ImportSongUI Instance { get; private set; }
    
    private FileDialog _fileDialog;
    private Button _importButton;
    private Button _cancelButton;
    private Label _statusLabel;
    private ProgressBar _progressBar;
    private bool _isImporting;
    
    public override void _Ready()
    {
        Instance = this;
        
        CreateUI();
        Hide();
    }
    
    private void CreateUI()
    {
        // Create file dialog
        _fileDialog = new FileDialog();
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        _fileDialog.Filters = new[] { "*.wav", "*.ogg" };
        _fileDialog.Title = "Select Audio File";
        _fileDialog.FileSelected += OnFileSelected;
        AddChild(_fileDialog);
        
        // Main container
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(400, 200);
        panel.AnchorRight = 1f;
        panel.AnchorBottom = 1f;
        
        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        
        // Status label
        _statusLabel = new Label
        {
            Text = "Select an audio file to import",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(_statusLabel);
        
        // Progress bar
        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            CustomMinimumSize = new Vector2(300, 20),
            Visible = false
        };
        vbox.AddChild(_progressBar);
        
        // Button container
        var buttonBox = new HBoxContainer();
        buttonBox.Alignment = BoxContainer.AlignmentMode.Center;
        
        _importButton = new Button { Text = "Import" };
        _importButton.Pressed += OnImportPressed;
        buttonBox.AddChild(_importButton);
        
        _cancelButton = new Button { Text = "Cancel" };
        _cancelButton.Pressed += OnCancelPressed;
        buttonBox.AddChild(_cancelButton);
        
        vbox.AddChild(buttonBox);
        panel.AddChild(vbox);
        AddChild(panel);
    }
    
    public void ShowImportDialog()
    {
        Show();
        _fileDialog.Show();
    }
    
    private void OnFileSelected(string path)
    {
        _statusLabel.Text = $"Selected: {path.GetFile()}";
        _selectedPath = path;
    }
    
    private string _selectedPath = "";
    
    private async void OnImportPressed()
    {
        if (_isImporting) return;
        
        if (string.IsNullOrEmpty(_selectedPath))
        {
            _statusLabel.Text = "Please select a file first!";
            return;
        }
        
        _isImporting = true;
        _importButton.Disabled = true;
        _progressBar.Visible = true;
        _progressBar.Value = 0;
        _statusLabel.Text = "Importing...";
        
        try
        {
            await ImportSongAsync(_selectedPath);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            GD.PrintErr($"Import failed: {ex}");
        }
        
        _isImporting = false;
        _importButton.Disabled = false;
    }
    
    private async Task ImportSongAsync(string audioPath)
    {
        // Step 1: Analyze BPM
        _statusLabel.Text = "Analyzing BPM...";
        _progressBar.Value = 25;
        
        double bpm = await AudioAnalysis.Instance.AnalyzeBpmFromFileAsync(audioPath);
        GD.Print($"Detected BPM: {bpm}");
        
        // Step 2: Generate charts
        _statusLabel.Text = "Generating charts...";
        _progressBar.Value = 50;
        
        var charts = ChartGenerator.Instance.GenerateAllCharts(
            AudioStreamWav.LoadFromFile(audioPath), 
            bpm
        );
        
        // Step 3: Create song data
        _statusLabel.Text = "Saving song...";
        _progressBar.Value = 75;
        
        string fileName = audioPath.GetFile();
        string songName = fileName.GetBaseName();
        
        var song = new SongData
        {
            Id = $"imported_{DateTime.Now.Ticks}",
            Name = songName,
            Artist = "Unknown Artist",
            Bpm = bpm,
            Duration = GetAudioDuration(audioPath),
            AudioPath = audioPath,
            IsImported = true,
            Charts = charts
        };
        
        // Step 4: Add to library
        SongLibrary.Instance?.AddSong(song);
        
        // Save to player data
        var playerData = PlayerData.Load();
        playerData.ImportedSongs.Add(song.Id);
        playerData.Save();
        
        // Check achievement
        AchievementManager.Instance?.OnSongImport();
        
        _progressBar.Value = 100;
        _statusLabel.Text = $"Imported: {songName} (BPM: {bpm:F0})";
        
        await Task.Delay(1500);
        
        Hide();
    }
    
    private double GetAudioDuration(string path)
    {
        string extension = path.GetExtension().ToLower();
        
        if (extension == "wav")
        {
            var stream = AudioStreamWav.LoadFromFile(path);
            return stream.GetLength();
        }
        else if (extension == "ogg")
        {
            var stream = AudioStreamOggVorbis.LoadFromFile(path);
            return stream.GetLength();
        }
        
        return 180; // Default 3 minutes
    }
    
    private void OnCancelPressed()
    {
        _selectedPath = "";
        Hide();
    }
}