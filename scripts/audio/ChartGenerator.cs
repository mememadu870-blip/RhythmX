using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RhythmX;

/// <summary>
/// Automatic chart generation from audio analysis
/// Creates rhythm game charts based on detected onsets and energy
/// </summary>
public partial class ChartGenerator : Node
{
    public static ChartGenerator Instance { get; private set; }
    
    private const int FFT_SIZE = 2048;
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    /// <summary>
    /// Generate a chart asynchronously
    /// </summary>
    public async Task<ChartData> GenerateChartAsync(AudioStreamWav stream, GameManager.Difficulty difficulty, double bpm)
    {
        await Task.Delay(100);
        return GenerateChart(stream, bpm, difficulty);
    }
    
    /// <summary>
    /// Generate all difficulty charts at once
    /// </summary>
    public List<ChartData> GenerateAllCharts(AudioStreamWav stream, double bpm)
    {
        var charts = new List<ChartData>();
        
        foreach (GameManager.Difficulty difficulty in Enum.GetValues(typeof(GameManager.Difficulty)))
        {
            var chart = GenerateChart(stream, bpm, difficulty);
            charts.Add(chart);
        }
        
        return charts;
    }
    
    /// <summary>
    /// Generate a single difficulty chart
    /// </summary>
    public ChartData GenerateChart(AudioStreamWav stream, double bpm, GameManager.Difficulty difficulty)
    {
        var chart = new ChartData
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8),
            Difficulty = difficulty,
            TrackCount = 4,
            Bpm = bpm,
            Offset = 0
        };
        
        // Get audio data
        var data = stream.Data;
        if (data == null || data.Length == 0)
        {
            GD.PrintErr("No audio data for chart generation");
            chart.Notes = new List<NoteData>();
            return chart;
        }
        
        // Convert to float samples
        float[] samples = ConvertToFloatSamples(data, stream.Format);
        float[] monoSamples = ConvertToMono(samples, stream.Stereo ? 2 : 1);
        
        // Detect onsets
        var onsets = DetectOnsets(monoSamples, (int)stream.MixRate);
        
        // Calculate beat interval
        float beatInterval = 60f / (float)bpm;
        
        // Filter based on difficulty density
        float density = GameManager.DifficultyDensityMultipliers[(int)difficulty];
        var filteredOnsets = FilterOnsets(onsets, beatInterval, density);
        
        // Convert to notes
        chart.Notes = GenerateNotes(filteredOnsets, (int)stream.MixRate, difficulty);
        
        GD.Print($"Generated {chart.Notes.Count} notes for {difficulty} difficulty");
        
        return chart;
    }
    
    /// <summary>
    /// Generate chart from audio file path
    /// </summary>
    public async Task<ChartData> GenerateChartFromFileAsync(string path, GameManager.Difficulty difficulty, double bpm)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"Audio file not found: {path}");
            return CreateEmptyChart(difficulty, bpm);
        }
        
        if (path.GetExtension().ToLower() == "wav")
        {
            var stream = AudioStreamWav.LoadFromFile(path);
            return await GenerateChartAsync(stream, difficulty, bpm);
        }
        
        // For other formats, create basic chart
        GD.Print($"Chart generation for {path.GetExtension()} not fully supported");
        return CreateEmptyChart(difficulty, bpm);
    }
    
    private ChartData CreateEmptyChart(GameManager.Difficulty difficulty, double bpm)
    {
        return new ChartData
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8),
            Difficulty = difficulty,
            TrackCount = 4,
            Bpm = bpm,
            Offset = 0,
            Notes = new List<NoteData>()
        };
    }
    
    private float[] ConvertToFloatSamples(byte[] data, AudioStreamWav.FormatEnum format)
    {
        int bytesPerSample = format == AudioStreamWav.FormatEnum.Format16Bits ? 2 : 1;
        int sampleCount = data.Length / bytesPerSample;
        
        float[] samples = new float[sampleCount];
        
        for (int i = 0; i < sampleCount; i++)
        {
            if (format == AudioStreamWav.FormatEnum.Format16Bits)
            {
                short value = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                samples[i] = value / 32768f;
            }
            else
            {
                samples[i] = (data[i] - 128) / 128f;
            }
        }
        
        return samples;
    }
    
    private float[] ConvertToMono(float[] samples, int channels)
    {
        if (channels <= 1) return samples;
        
        int monoCount = samples.Length / channels;
        float[] mono = new float[monoCount];
        
        for (int i = 0; i < monoCount; i++)
        {
            float sum = 0;
            for (int c = 0; c < channels; c++)
            {
                sum += samples[i * channels + c];
            }
            mono[i] = sum / channels;
        }
        
        return mono;
    }
    
    #region Onset Detection
    
    private List<Onset> DetectOnsets(float[] samples, int sampleRate)
    {
        var onsets = new List<Onset>();
        
        int hopSize = FFT_SIZE / 4;
        float[] prevSpectrum = new float[FFT_SIZE / 2 + 1];
        
        // Spectral flux calculation
        List<float> fluxList = new List<float>();
        
        for (int i = 0; i < samples.Length - FFT_SIZE; i += hopSize)
        {
            float[] window = new float[FFT_SIZE];
            for (int j = 0; j < FFT_SIZE; j++)
            {
                window[j] = samples[i + j] * HannWindow(j, FFT_SIZE);
            }
            
            float[] spectrum = ComputeMagnitudeSpectrum(window);
            
            float flux = 0;
            for (int k = 0; k < spectrum.Length; k++)
            {
                float diff = spectrum[k] - prevSpectrum[k];
                if (diff > 0) flux += diff;
                prevSpectrum[k] = spectrum[k];
            }
            fluxList.Add(flux);
        }
        
        // Adaptive threshold
        float[] threshold = ComputeAdaptiveThreshold(fluxList.ToArray(), 10);
        
        // Peak picking
        for (int i = 1; i < fluxList.Count - 1; i++)
        {
            if (fluxList[i] > threshold[i] &&
                fluxList[i] > fluxList[i - 1] &&
                fluxList[i] > fluxList[i + 1])
            {
                float time = i * hopSize / (float)sampleRate;
                float energy = fluxList[i];
                int freqBand = GetDominantFrequencyBand(samples, i * hopSize, sampleRate);
                
                onsets.Add(new Onset
                {
                    Time = time,
                    Energy = energy,
                    FrequencyBand = freqBand
                });
            }
        }
        
        return onsets;
    }
    
    private List<Onset> FilterOnsets(List<Onset> onsets, float beatInterval, float density)
    {
        if (onsets.Count == 0) return onsets;
        
        // Sort by time
        onsets.Sort((a, b) => a.Time.CompareTo(b.Time));
        
        List<Onset> filtered = new List<Onset>();
        float minInterval = beatInterval * 0.25f / density;
        float lastTime = -minInterval;
        
        foreach (var onset in onsets)
        {
            if (onset.Time - lastTime >= minInterval)
            {
                filtered.Add(onset);
                lastTime = onset.Time;
            }
        }
        
        return filtered;
    }
    
    #endregion
    
    #region Note Generation
    
    private List<NoteData> GenerateNotes(List<Onset> onsets, int sampleRate, GameManager.Difficulty difficulty)
    {
        var notes = new List<NoteData>();
        int trackCount = 4;
        
        var random = new Random();
        
        foreach (var onset in onsets)
        {
            // Assign track based on frequency band
            int track = Math.Clamp(onset.FrequencyBand, 0, trackCount - 1);
            
            // Randomize slightly for variety (20% chance)
            if (random.NextDouble() < 0.2)
            {
                track = random.Next(0, trackCount);
            }
            
            // Determine note type based on energy and difficulty
            NoteType noteType = DetermineNoteType(onset.Energy, difficulty, random);
            
            var note = new NoteData
            {
                Time = onset.Time,
                Track = track,
                Type = noteType
            };
            
            if (noteType == NoteType.Hold)
            {
                // Generate hold duration based on energy
                double holdDuration = random.NextDouble() * 0.7 + 0.3; // 0.3 to 1.0
                note.EndTime = onset.Time + holdDuration;
            }
            else if (noteType == NoteType.Swipe)
            {
                note.SwipeDirection = (SwipeDirection)random.Next(1, 5);
            }
            
            notes.Add(note);
        }
        
        return notes;
    }
    
    private NoteType DetermineNoteType(float energy, GameManager.Difficulty difficulty, Random random)
    {
        float holdChance = 0.1f;
        float swipeChance = 0.05f;
        
        switch (difficulty)
        {
            case GameManager.Difficulty.Easy:
                holdChance = 0.05f;
                swipeChance = 0f;
                break;
            case GameManager.Difficulty.Normal:
                holdChance = 0.1f;
                swipeChance = 0.05f;
                break;
            case GameManager.Difficulty.Hard:
                holdChance = 0.15f;
                swipeChance = 0.1f;
                break;
            case GameManager.Difficulty.Expert:
                holdChance = 0.2f;
                swipeChance = 0.15f;
                break;
        }
        
        double roll = random.NextDouble();
        if (roll < swipeChance) return NoteType.Swipe;
        if (roll < swipeChance + holdChance) return NoteType.Hold;
        return NoteType.Tap;
    }
    
    #endregion
    
    #region Utility Methods
    
    private float[] ComputeMagnitudeSpectrum(float[] window)
    {
        int n = window.Length;
        float[] magnitude = new float[n / 2 + 1];
        
        for (int k = 0; k <= n / 2; k++)
        {
            float real = 0, imag = 0;
            for (int t = 0; t < n; t++)
            {
                double angle = 2 * Math.PI * k * t / n;
                real += window[t] * (float)Math.Cos(angle);
                imag -= window[t] * (float)Math.Sin(angle);
            }
            magnitude[k] = (float)Math.Sqrt(real * real + imag * imag) / n;
        }
        
        return magnitude;
    }
    
    private float[] ComputeAdaptiveThreshold(float[] flux, int windowSize)
    {
        float[] threshold = new float[flux.Length];
        
        for (int i = 0; i < flux.Length; i++)
        {
            int start = Math.Max(0, i - windowSize);
            int end = Math.Min(flux.Length - 1, i + windowSize);
            
            float sum = 0;
            int count = 0;
            for (int j = start; j <= end; j++)
            {
                sum += flux[j];
                count++;
            }
            
            threshold[i] = (sum / count) * 1.5f;
        }
        
        return threshold;
    }
    
    private int GetDominantFrequencyBand(float[] samples, int startIdx, int sampleRate)
    {
        int windowSize = 1024;
        if (startIdx + windowSize >= samples.Length) return 0;
        
        float[] window = new float[windowSize];
        for (int i = 0; i < windowSize; i++)
        {
            window[i] = samples[startIdx + i];
        }
        
        var spectrum = ComputeMagnitudeSpectrum(window);
        
        // Divide into 4 bands
        int bandSize = spectrum.Length / 4;
        float[] bandEnergies = new float[4];
        
        for (int b = 0; b < 4; b++)
        {
            for (int i = b * bandSize; i < (b + 1) * bandSize && i < spectrum.Length; i++)
            {
                bandEnergies[b] += spectrum[i];
            }
        }
        
        int maxBand = 0;
        for (int b = 1; b < 4; b++)
        {
            if (bandEnergies[b] > bandEnergies[maxBand])
                maxBand = b;
        }
        
        return maxBand;
    }
    
    private float HannWindow(int index, int size)
    {
        return 0.5f * (1 - (float)Math.Cos(2 * Math.PI * index / (size - 1)));
    }
    
    #endregion
}

/// <summary>
/// Represents a detected onset in audio
/// </summary>
internal struct Onset
{
    public float Time;
    public float Energy;
    public int FrequencyBand;
}