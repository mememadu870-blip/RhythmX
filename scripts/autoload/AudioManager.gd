class_name AudioManagerClass
extends Node

## 音频管理器 - 处理所有音频播放、同步和时序
## 单例 autoload 用于全局音频访问

signal on_beat(beat: int)
signal on_song_end
signal on_time_update(time: float)

# 音频播放器
var _music_player: AudioStreamPlayer
var _sfx_player: AudioStreamPlayer
var _hit_sound_pool: Array[AudioStreamPlayer] = []

# 时序状态
var current_time: float = 0.0
var song_duration: float = 0.0
var bpm: float = 120.0
var beat_progress: float = 0.0
var current_beat: int = 0
var is_playing: bool = false
var time_since_start: float = 0.0

var _current_stream: AudioStream
var _start_time: float = 0.0
var _pause_time: float = 0.0
var _start_offset: float = 0.0

# 音量设置
var music_volume: float = 1.0
var sfx_volume: float = 1.0
var hit_volume: float = 1.0

# _hit_sound_pool 管理
var _hit_sound_pool_size: int = 5
var _current_hit_player_index: int = 0

const HIT_SOUND_POOL_SIZE = 5


static var instance: AudioManagerClass


func _ready() -> void:
    instance = self

    # 创建音频播放器
    _music_player = AudioStreamPlayer.new()
    _sfx_player = AudioStreamPlayer.new()
    add_child(_music_player)
    add_child(_sfx_player)

    # 创建 hit 声音池
    for i in HIT_SOUND_POOL_SIZE:
        var player = AudioStreamPlayer.new()
        player.volume_db = 0.0
        add_child(player)
        _hit_sound_pool.append(player)

    load_settings()


func load_settings() -> void:
    var config = ConfigFile.new()
    if config.load("user://settings.cfg") == OK:
        music_volume = config.get_value("audio", "music_volume", 1.0)
        sfx_volume = config.get_value("audio", "sfx_volume", 1.0)
        hit_volume = config.get_value("audio", "hit_volume", 1.0)
        apply_volumes()


func apply_volumes() -> void:
    _music_player.volume_db = linear_to_db(music_volume)
    _sfx_player.volume_db = linear_to_db(sfx_volume)
    for player in _hit_sound_pool:
        player.volume_db = linear_to_db(hit_volume)


# ============================================================
# 歌曲加载
# ============================================================
func load_song(stream: AudioStream, song_bpm: float, start_offset: float = 0.0) -> void:
    _current_stream = stream
    _music_player.stream = stream
    bpm = song_bpm
    _start_offset = start_offset

    # 获取时长
    if stream:
        song_duration = stream.get_length()
    else:
        song_duration = 0.0

    current_time = 0.0
    current_beat = 0
    time_since_start = 0.0


func load_song_from_path(path: String, song_bpm: float, start_offset: float = 0.0) -> void:
    if not FileAccess.file_exists(path):
        push_error("Audio file not found: " + path)
        return

    var extension = path.get_extension().to_lower()
    var stream: AudioStream = null

    match extension:
        "wav":
            stream = ResourceLoader.load(path) as AudioStream
        "ogg":
            stream = ResourceLoader.load(path) as AudioStream
        _:
            push_error("Unsupported audio format: " + extension)
            return

    if stream:
        load_song(stream, song_bpm, start_offset)


# ============================================================
# 播放控制
# ============================================================
func play() -> void:
    if _current_stream == null:
        return
    _music_player.play()
    _start_time = Time.get_ticks_msec() / 1000.0
    is_playing = true


func play_from(time: float) -> void:
    if _current_stream == null:
        return
    _music_player.seek(time)
    _music_player.play()
    _start_time = Time.get_ticks_msec() / 1000.0 - time
    is_playing = true
    current_time = time


func pause() -> void:
    if not is_playing:
        return
    _pause_time = current_time
    _music_player.stream_paused = true
    is_playing = false


func resume() -> void:
    if not is_playing:
        return
    _music_player.stream_paused = false
    _start_time = Time.get_ticks_msec() / 1000.0 - _pause_time
    is_playing = true


func stop() -> void:
    _music_player.stop()
    is_playing = false
    current_time = 0.0
    current_beat = 0
    time_since_start = 0.0


func seek(time: float) -> void:
    if _current_stream == null:
        return

    time = clamp(time, 0.0, song_duration)
    _music_player.seek(time)

    if is_playing:
        _start_time = Time.get_ticks_msec() / 1000.0 - time
    else:
        _pause_time = time

    current_time = time


# ============================================================
# 时间处理
# ============================================================
func _process(delta: float) -> void:
    if not is_playing or _current_stream == null:
        return

    # 更新当前时间
    var real_time = Time.get_ticks_msec() / 1000.0
    current_time = real_time - _start_time + _start_offset
    time_since_start = current_time

    # 计算节拍进度
    var beat_duration = 60.0 / bpm
    beat_progress = (current_time % beat_duration) / beat_duration

    var new_beat = int(current_time / beat_duration)

    if new_beat != current_beat:
        current_beat = new_beat
        on_beat.emit(current_beat)

        # 通知 EffectManager
        if EffectManager:
            EffectManager.on_beat(current_beat)

    # 触发时间更新事件
    on_time_update.emit(current_time)

    # 检查歌曲结束
    if current_time >= song_duration and song_duration > 0:
        is_playing = false
        on_song_end.emit()


func get_time_to_beat(beat: int) -> float:
    var beat_time = beat * (60.0 / bpm)
    return beat_time - current_time


func get_next_beat_time() -> float:
    var beat_duration = 60.0 / bpm
    return (current_beat + 1) * beat_duration


# ============================================================
# 音效
# ============================================================
func play_hit_sound(clip: AudioStream) -> void:
    if clip == null:
        return

    var player = _hit_sound_pool[_current_hit_player_index]
    player.stream = clip
    player.play()

    _current_hit_player_index = (_current_hit_player_index + 1) % _hit_sound_pool_size


func play_sfx(clip: AudioStream) -> void:
    if clip == null:
        return
    _sfx_player.stream = clip
    _sfx_player.play()


func play_sfx_from_path(path: String) -> void:
    if not FileAccess.file_exists(path):
        push_error("SFX file not found: " + path)
        return

    var extension = path.get_extension().to_lower()
    var stream: AudioStream = null

    match extension:
        "wav":
            stream = ResourceLoader.load(path) as AudioStream
        "ogg":
            stream = ResourceLoader.load(path) as AudioStream

    if stream:
        play_sfx(stream)


# ============================================================
# 音量控制
# ============================================================
func set_music_volume(volume: float) -> void:
    music_volume = clamp(volume, 0.0, 1.0)
    _music_player.volume_db = linear_to_db(music_volume)
    save_settings()


func set_sfx_volume(volume: float) -> void:
    sfx_volume = clamp(volume, 0.0, 1.0)
    _sfx_player.volume_db = linear_to_db(sfx_volume)
    save_settings()


func set_hit_volume(volume: float) -> void:
    hit_volume = clamp(volume, 0.0, 1.0)
    for player in _hit_sound_pool:
        player.volume_db = linear_to_db(hit_volume)
    save_settings()


func save_settings() -> void:
    var config = ConfigFile.new()
    config.set_value("audio", "music_volume", music_volume)
    config.set_value("audio", "sfx_volume", sfx_volume)
    config.set_value("audio", "hit_volume", hit_volume)
    config.save("user://settings.cfg")


# ============================================================
# BPM 工具
# ============================================================
func calculate_beat_time(beat: int) -> float:
    return beat * (60.0 / bpm)


func get_beat_from_time(time: float) -> int:
    return int(time * bpm / 60.0)


func get_time_in_beat(time: float) -> float:
    var beat_duration = 60.0 / bpm
    return time % beat_duration
