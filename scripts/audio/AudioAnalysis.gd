class_name AudioAnalysisClass
extends Node

## BPM 检测和音频分析工具
## 单例 autoload

const FFT_SIZE = 2048

static var instance: AudioAnalysisClass


func _ready() -> void:
    instance = self


## 从音频流分析 BPM
func analyze_bpm_async(stream: AudioStreamWAV) -> float:
    await get_tree().create_timer(0.1).timeout
    return analyze_bpm_sync(stream)


func analyze_bpm_sync(stream: AudioStreamWAV) -> float:
    # 从流中获取音频数据
    var data = stream.data

    if data == null or data.size() == 0:
        push_error("No audio data to analyze")
        return 120.0  # 默认 BPM

    # 转换为浮点采样
    var samples = convert_to_float_samples(data, stream.format)

    # 如果是立体声则转换为单声道
    var mono_samples = convert_to_mono(samples, 2 if stream.stereo else 1)

    # 使用频谱通量检测节拍
    var beat_times = detect_beats(mono_samples, stream.mix_rate)

    # 从平均节拍间隔计算 BPM
    if beat_times.size() < 2:
        print("Not enough beats detected, using default BPM")
        return 120.0

    var total_interval = 0.0
    for i in range(1, beat_times.size()):
        total_interval += beat_times[i] - beat_times[i - 1]

    var avg_interval = total_interval / (beat_times.size() - 1)
    var bpm = 60.0 / avg_interval

    # 限制到合理范围
    if bpm < 60:
        bpm *= 2
    elif bpm > 200:
        bpm /= 2

    return round(bpm)


## 从音频文件路径分析 BPM
func analyze_bpm_from_file_async(path: String) -> float:
    if not FileAccess.file_exists(path):
        push_error("Audio file not found: " + path)
        return 120.0

    var extension = path.get_extension().to_lower()

    if extension == "wav":
        var stream = ResourceLoader.load(path) as AudioStreamWAV
        if stream == null:
            push_error("Failed to load audio file: " + path)
            return 120.0
        return await analyze_bpm_async(stream)

    # 其他格式支持有限
    print("BPM analysis for " + extension + " not fully supported, using default")
    return 120.0


func convert_to_float_samples(data: PackedByteArray, format: int) -> PackedFloat32Array:
    var bytes_per_sample = 2 if format == AudioStreamWAV.FORMAT_16_BITS else 1
    var sample_count = data.size() / bytes_per_sample

    var samples = PackedFloat32Array()
    samples.resize(sample_count)

    for i in range(sample_count):
        if format == AudioStreamWAV.FORMAT_16_BITS:
            var value = data[i * 2] | (data[i * 2 + 1] << 8)
            samples[i] = value / 32768.0
        else:
            samples[i] = (data[i] - 128) / 128.0

    return samples


func convert_to_mono(samples: PackedFloat32Array, channels: int) -> PackedFloat32Array:
    if channels <= 1:
        return samples

    var mono_count = samples.size() / channels
    var mono = PackedFloat32Array()
    mono.resize(mono_count)

    for i in range(mono_count):
        var sum = 0.0
        for c in range(channels):
            sum += samples[i * channels + c]
        mono[i] = sum / channels

    return mono


func detect_beats(samples: PackedFloat32Array, sample_rate: int) -> Array[float]:
    var window_size = 1024
    var hop_size = 512

    var spectral_flux = []
    var prev_mag = []
    prev_mag.resize(window_size / 2 + 1)

    # 计算频谱通量
    var i = 0
    while i < samples.size() - window_size:
        var window = PackedFloat32Array()
        window.resize(window_size)
        for j in range(window_size):
            window[j] = samples[i + j] * hann_window(j, window_size)

        var spectrum = compute_magnitude_spectrum(window)

        var flux = 0.0
        for k in range(spectrum.size()):
            var diff = spectrum[k] - prev_mag[k]
            if diff > 0:
                flux += diff
            prev_mag[k] = spectrum[k]

        spectral_flux.append(flux)
        i += hop_size

    # 峰值检测
    var beat_times = []
    var threshold = calculate_threshold(spectral_flux)

    for idx in range(1, spectral_flux.size() - 1):
        if spectral_flux[idx] > threshold and \
           spectral_flux[idx] > spectral_flux[idx - 1] and \
           spectral_flux[idx] > spectral_flux[idx + 1]:
            var time = (idx * hop_size) / float(sample_rate)
            beat_times.append(time)

    return beat_times


func compute_magnitude_spectrum(window: PackedFloat32Array) -> PackedFloat32Array:
    var n = window.size()
    var magnitude = PackedFloat32Array()
    magnitude.resize(n / 2 + 1)

    # 简单 DFT
    for k in range(n / 2 + 1):
        var real = 0.0
        var imag = 0.0
        for t in range(n):
            var angle = 2.0 * PI * float(k) * float(t) / float(n)
            real += window[t] * cos(angle)
            imag -= window[t] * sin(angle)
        magnitude[k] = sqrt(real * real + imag * imag) / float(n)

    return magnitude


func hann_window(index: int, size: int) -> float:
    return 0.5 * (1.0 - cos(2.0 * PI * float(index) / float(size - 1)))


func calculate_threshold(flux: Array[float]) -> float:
    if flux.size() == 0:
        return 0.0

    var sum = 0.0
    for f in flux:
        sum += f

    return (sum / float(flux.size())) * 1.5


## 获取音频时长（秒）
func get_duration(stream: AudioStream) -> float:
    if stream is AudioStreamWAV:
        return stream.get_length()
    elif stream is AudioStreamOGGVorbis:
        return stream.get_length()
    return 0.0
