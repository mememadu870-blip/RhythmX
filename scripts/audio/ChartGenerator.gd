class_name ChartGeneratorClass
extends Node

## 自动谱面生成器
## 基于检测的起始点和能量创建节奏游戏谱面

const GameData = preload("res://scripts/data/GameData.gd")

const FFT_SIZE = 2048

static var instance: ChartGeneratorClass

var _rng: RandomNumberGenerator = RandomNumberGenerator.new()


func _ready() -> void:
    instance = self
    _rng.seed = Time.get_ticks_msec()


## 异步生成谱面
func generate_chart_async(stream: AudioStreamWAV, difficulty: int, bpm: float) -> GameData.ChartData:
    await get_tree().create_timer(0.1).timeout
    return generate_chart(stream, bpm, difficulty)


## 一次性生成所有难度谱面
func generate_all_charts(stream: AudioStreamWAV, bpm: float) -> Array[GameData.ChartData]:
    var charts = []

    for difficulty in range(4):
        var chart = generate_chart(stream, bpm, difficulty)
        charts.append(chart)

    return charts


## 生成单个难度谱面
func generate_chart(stream: AudioStreamWAV, bpm: float, difficulty: int) -> GameData.ChartData:
    var chart = GameData.ChartData.new()
    chart.id = str(_rng.randi()).substr(0, 8)
    chart.difficulty = difficulty
    chart.track_count = 4
    chart.bpm = bpm
    chart.offset = 0.0

    # 获取音频数据
    var data = stream.data
    if data == null or data.size() == 0:
        push_error("No audio data for chart generation")
        return chart

    # 转换为浮点采样
    var samples = convert_to_float_samples(data, stream.format)
    var mono_samples = convert_to_mono(samples, 2 if stream.stereo else 1)

    # 检测起始点
    var onsets = detect_onsets(mono_samples, stream.mix_rate)

    # 计算节拍间隔
    var beat_interval = 60.0 / bpm

    # 根据难度密度过滤起始点
    var density = GameManagerClass.DIFFICULTY_DENSITY_MULTIPLIERS[difficulty]
    var filtered_onsets = filter_onsets(onsets, beat_interval, density)

    # 转换为音符
    chart.notes = generate_notes(filtered_onsets, stream.mix_rate, difficulty)

    print("Generated %d notes for %s difficulty" % [chart.notes.size(), GameManagerClass.DIFFICULTY_NAMES[difficulty]])

    return chart


## 从音频文件路径生成谱面
func generate_chart_from_file_async(path: String, difficulty: int, bpm: float) -> GameData.ChartData:
    if not FileAccess.file_exists(path):
        push_error("Audio file not found: " + path)
        return create_empty_chart(difficulty, bpm)

    if path.get_extension().to_lower() == "wav":
        var stream = ResourceLoader.load(path) as AudioStreamWAV
        if stream == null:
            push_error("Failed to load audio file: " + path)
            return create_empty_chart(difficulty, bpm)
        return await generate_chart_async(stream, difficulty, bpm)

    # 其他格式支持有限
    print("Chart generation for " + path.get_extension() + " not fully supported")
    return create_empty_chart(difficulty, bpm)


func create_empty_chart(difficulty: int, bpm: float) -> GameData.ChartData:
    var chart = GameData.ChartData.new()
    chart.id = str(_rng.randi()).substr(0, 8)
    chart.difficulty = difficulty
    chart.track_count = 4
    chart.bpm = bpm
    chart.offset = 0.0
    chart.notes = []
    return chart


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


# ============================================================
# 起始点检测
# ============================================================
func detect_onsets(samples: PackedFloat32Array, sample_rate: int) -> Array[Dictionary]:
    var onsets = []

    var hop_size = FFT_SIZE / 4
    var prev_spectrum = []
    prev_spectrum.resize(FFT_SIZE / 2 + 1)

    # 频谱通量计算
    var flux_list = []

    var i = 0
    while i < samples.size() - FFT_SIZE:
        var window = PackedFloat32Array()
        window.resize(FFT_SIZE)
        for j in range(FFT_SIZE):
            window[j] = samples[i + j] * hann_window(j, FFT_SIZE)

        var spectrum = compute_magnitude_spectrum(window)

        var flux = 0.0
        for k in range(spectrum.size()):
            var diff = spectrum[k] - prev_spectrum[k]
            if diff > 0:
                flux += diff
            prev_spectrum[k] = spectrum[k]

        flux_list.append(flux)
        i += hop_size

    # 自适应阈值
    var threshold = compute_adaptive_threshold(flux_list, 10)

    # 峰值选取
    for idx in range(1, flux_list.size() - 1):
        if flux_list[idx] > threshold[idx] and \
           flux_list[idx] > flux_list[idx - 1] and \
           flux_list[idx] > flux_list[idx + 1]:
            var time = float(idx * hop_size) / float(sample_rate)
            var energy = flux_list[idx]
            var freq_band = get_dominant_frequency_band(samples, idx * hop_size, sample_rate)

            onsets.append({
                "time": time,
                "energy": energy,
                "frequency_band": freq_band
            })

    return onsets


func filter_onsets(onsets: Array[Dictionary], beat_interval: float, density: float) -> Array[Dictionary]:
    if onsets.size() == 0:
        return onsets

    # 按时间排序
    onsets.sort_custom(func(a, b): return a.time < b.time)

    var filtered = []
    var min_interval = beat_interval * 0.25 / density
    var last_time = -min_interval

    for onset in onsets:
        if onset.time - last_time >= min_interval:
            filtered.append(onset)
            last_time = onset.time

    return filtered


# ============================================================
# 音符生成
# ============================================================
func generate_notes(onsets: Array[Dictionary], sample_rate: int, difficulty: int) -> Array[GameData.NoteData]:
    var notes = []
    var track_count = 4

    for onset in onsets:
        # 根据频带分配轨道
        var track = clamp(onset.frequency_band, 0, track_count - 1)

        # 随机化增加变化 (20% 几率)
        if _rng.randf() < 0.2:
            track = _rng.randi_range(0, track_count - 1)

        # 根据能量和难度确定音符类型
        var note_type = determine_note_type(onset.energy, difficulty)

        var note = GameData.NoteData.new()
        note.time = onset.time
        note.track = track
        note.type = note_type

        if note_type == GameData.NoteType.HOLD:
            # 根据能量生成按住时长
            var hold_duration = _rng.randf_range(0.3, 1.0)
            note.end_time = onset.time + hold_duration
        elif note_type == GameData.NoteType.SWIPE:
            note.swipe_direction = _rng.randi_range(1, 4)

        notes.append(note)

    return notes


func determine_note_type(energy: float, difficulty: int) -> GameData.NoteType:
    var hold_chance = 0.1
    var swipe_chance = 0.05

    match difficulty:
        GameManagerClass.Difficulty.EASY:
            hold_chance = 0.05
            swipe_chance = 0.0
        GameManagerClass.Difficulty.NORMAL:
            hold_chance = 0.1
            swipe_chance = 0.05
        GameManagerClass.Difficulty.HARD:
            hold_chance = 0.15
            swipe_chance = 0.1
        GameManagerClass.Difficulty.EXPERT:
            hold_chance = 0.2
            swipe_chance = 0.15

    var roll = _rng.randf()
    if roll < swipe_chance:
        return GameData.NoteType.SWIPE
    elif roll < swipe_chance + hold_chance:
        return GameData.NoteType.HOLD
    return GameData.NoteType.TAP


# ============================================================
# 工具方法
# ============================================================
func compute_magnitude_spectrum(window: PackedFloat32Array) -> PackedFloat32Array:
    var n = window.size()
    var magnitude = PackedFloat32Array()
    magnitude.resize(n / 2 + 1)

    for k in range(n / 2 + 1):
        var real = 0.0
        var imag = 0.0
        for t in range(n):
            var angle = 2.0 * PI * float(k) * float(t) / float(n)
            real += window[t] * cos(angle)
            imag -= window[t] * sin(angle)
        magnitude[k] = sqrt(real * real + imag * imag) / float(n)

    return magnitude


func compute_adaptive_threshold(flux: Array[float], window_size: int) -> Array[float]:
    var threshold = []
    threshold.resize(flux.size())

    for i in range(flux.size()):
        var start = max(0, i - window_size)
        var end = min(flux.size() - 1, i + window_size)

        var sum = 0.0
        var count = 0
        for j in range(start, end + 1):
            sum += flux[j]
            count += 1

        threshold[i] = (sum / float(count)) * 1.5

    return threshold


func get_dominant_frequency_band(samples: PackedFloat32Array, start_idx: int, sample_rate: int) -> int:
    var window_size = 1024
    if start_idx + window_size >= samples.size():
        return 0

    var window = PackedFloat32Array()
    window.resize(window_size)
    for i in range(window_size):
        window[i] = samples[start_idx + i]

    var spectrum = compute_magnitude_spectrum(window)

    # 分为 4 个频带
    var band_size = spectrum.size() / 4
    var band_energies = []
    band_energies.resize(4)

    for b in range(4):
        var energy = 0.0
        for i in range(b * band_size, min((b + 1) * band_size, spectrum.size())):
            energy += spectrum[i]
        band_energies[b] = energy

    var max_band = 0
    for b in range(1, 4):
        if band_energies[b] > band_energies[max_band]:
            max_band = b

    return max_band


func hann_window(index: int, size: int) -> float:
    return 0.5 * (1.0 - cos(2.0 * PI * float(index) / float(size - 1)))
