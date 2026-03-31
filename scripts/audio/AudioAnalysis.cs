using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RhythmX;

/// <summary>
/// BPM detection and audio analysis utilities
/// </summary>
public partial class AudioAnalysis : Node
{
    public static AudioAnalysis Instance { get; private set; }
    
    private const int FFT_SIZE = 2048;
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    /// <summary>
    /// Analyze BPM from an audio stream
    /// </summary>
    public async Task<double> AnalyzeBpmAsync(AudioStreamWav stream)
    {
        await Task.Delay(100);
        return AnalyzeBpmSync(stream);
    }
    
    public double AnalyzeBpmSync(AudioStreamWav stream)
    {
        // Get audio data from stream
        var data = stream.Data;
        
        if (data == null || data.Length == 0)
        {
            GD.PrintErr("No audio data to analyze");
            return 120; // Default BPM
        }
        
        // Convert byte data to float samples
        float[] samples = ConvertToFloatSamples(data, stream.Format);
        
        // Convert to mono if stereo
        float[] monoSamples = ConvertToMono(samples, stream.Stereo ? 2 : 1);
        
        // Detect beats using spectral flux
        List<double> beatTimes = DetectBeats(monoSamples, (int)stream.MixRate);
        
        // Calculate BPM from average beat interval
        if (beatTimes.Count < 2)
        {
            GD.Print("Not enough beats detected, using default BPM");
            return 120;
        }
        
        double totalInterval = 0;
        for (int i = 1; i < beatTimes.Count; i++)
        {
            totalInterval += beatTimes[i] - beatTimes[i - 1];
        }
        
        double avgInterval = totalInterval / (beatTimes.Count - 1);
        double bpm = 60.0 / avgInterval;
        
        // Clamp to reasonable range
        if (bpm < 60) bpm *= 2;
        else if (bpm > 200) bpm /= 2;
        
        return Math.Round(bpm);
    }
    
    /// <summary>
    /// Analyze BPM from audio file path
    /// </summary>
    public async Task<double> AnalyzeBpmFromFileAsync(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"Audio file not found: {path}");
            return 120;
        }
        
        string extension = path.GetExtension().ToLower();
        
        if (extension == "wav")
        {
            var stream = AudioStreamWav.LoadFromFile(path);
            return await AnalyzeBpmAsync(stream);
        }
        
        // For other formats, we'd need additional processing
        // Return default for now
        GD.Print($"BPM analysis for {extension} not fully supported, using default");
        return 120;
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
    
    private List<double> DetectBeats(float[] samples, int sampleRate)
    {
        int windowSize = 1024;
        int hopSize = 512;
        
        List<float> spectralFlux = new List<float>();
        float[] prevMag = new float[windowSize / 2 + 1];
        
        // Calculate spectral flux
        for (int i = 0; i < samples.Length - windowSize; i += hopSize)
        {
            float[] window = new float[windowSize];
            for (int j = 0; j < windowSize; j++)
            {
                window[j] = samples[i + j] * HannWindow(j, windowSize);
            }
            
            float[] spectrum = ComputeMagnitudeSpectrum(window);
            
            float flux = 0;
            for (int k = 0; k < spectrum.Length; k++)
            {
                float diff = spectrum[k] - prevMag[k];
                if (diff > 0) flux += diff;
                prevMag[k] = spectrum[k];
            }
            spectralFlux.Add(flux);
        }
        
        // Peak detection
        List<double> beatTimes = new List<double>();
        float threshold = CalculateThreshold(spectralFlux);
        
        for (int i = 1; i < spectralFlux.Count - 1; i++)
        {
            if (spectralFlux[i] > threshold &&
                spectralFlux[i] > spectralFlux[i - 1] &&
                spectralFlux[i] > spectralFlux[i + 1])
            {
                double time = (i * hopSize) / (double)sampleRate;
                beatTimes.Add(time);
            }
        }
        
        return beatTimes;
    }
    
    private float[] ComputeMagnitudeSpectrum(float[] window)
    {
        int n = window.Length;
        float[] magnitude = new float[n / 2 + 1];
        
        // Simple DFT (for production, consider using a proper FFT library)
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
    
    private float HannWindow(int index, int size)
    {
        return 0.5f * (1 - (float)Math.Cos(2 * Math.PI * index / (size - 1)));
    }
    
    private float CalculateThreshold(List<float> flux)
    {
        if (flux.Count == 0) return 0;
        
        float sum = 0;
        foreach (var f in flux) sum += f;
        return (sum / flux.Count) * 1.5f;
    }
    
    /// <summary>
    /// Get audio duration in seconds
    /// </summary>
    public double GetDuration(AudioStream stream)
    {
        if (stream is AudioStreamWav wav)
        {
            return wav.GetLength();
        }
        else if (stream is AudioStreamOggVorbis ogg)
        {
            return ogg.GetLength();
        }
        
        return 0;
    }
}