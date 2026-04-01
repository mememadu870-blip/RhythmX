class_name APIManagerClass
extends Node

## API 管理器 - 模拟实现
## 提供与未来真实 API 对接的接口

const GameData = preload("res://scripts/data/GameData.gd")

# 信号
signal auth_changed(is_logged_in: bool)

# 状态
var is_online: bool = true
var _current_user_id: String = "mock_user_001"
var _current_token: String = "mock_token_abc123"

# 模拟数据
var _mock_songs: Dictionary = {}
var _mock_leaderboards: Dictionary = {}
var _mock_achievements: Array[Dictionary] = []

var _rng: RandomNumberGenerator = RandomNumberGenerator.new()

static var instance: APIManagerClass


func _ready() -> void:
    instance = self
    _rng.seed = Time.get_ticks_msec()
    initialize_mock_data()


func initialize_mock_data() -> void:
    # 创建 10 首模拟歌曲
    for i in range(1, 11):
        var song = create_mock_song(i)
        _mock_songs[song.id] = song

    # 创建模拟排行榜
    for song_id in _mock_songs.keys():
        for diff in range(4):
            var key = song_id + "_" + str(diff)
            _mock_leaderboards[key] = create_mock_leaderboard(song_id, diff)

    # 创建模拟成就
    _mock_achievements = create_mock_achievements()

    print("MockAPIManager initialized with %d songs, %d achievements" % [_mock_songs.size(), _mock_achievements.size()])


func create_mock_song(index: int) -> GameData.SongData:
    var song = GameData.SongData.new()
    song.id = "song_%02d" % index
    song.name = get_mock_song_name(index)
    song.artist = get_mock_artist_name(index)
    song.bpm = 120.0 + index * 10.0
    song.duration = 180.0 + index * 15.0
    song.is_imported = false
    song.is_favorite = index <= 2
    song.play_count = index * 10 if index <= 5 else 0
    song.high_score = 800000 + index * 20000 if index <= 5 else 0

    # 为每个难度生成谱面
    for d in range(4):
        var chart = create_mock_chart(song, d)
        song.charts.append(chart)

    return song


func get_mock_song_name(index: int) -> String:
    var names = [
        "Neon Pulsar", "Digital Dreams", "Crystal Wave", "Thunder Strike",
        "Midnight Run", "Starlight Serenade", "Electric Soul", "Cosmic Journey",
        "Rainbow Road", "Final Frontier"
    ]
    return names[min(index - 1, names.size() - 1)]


func get_mock_artist_name(index: int) -> String:
    var artists = [
        "Synthwave Masters", "Chiptune Collective", "Electronic Dreams", "Bass Warriors",
        "Melody Makers", "Rhythm Rebels", "Sound Architects", "Beat Breakers",
        "Audio Alchemists", "Music Machines"
    ]
    return artists[min(index - 1, artists.size() - 1)]


func create_mock_chart(song: GameData.SongData, difficulty: int) -> GameData.ChartData:
    var chart = GameData.ChartData.new()
    chart.id = song.id + "_chart_" + str(difficulty)
    chart.difficulty = difficulty
    chart.track_count = 4
    chart.bpm = song.bpm
    chart.offset = 0.1

    # 生成模拟音符
    var density = GameManagerClass.DIFFICULTY_DENSITY_MULTIPLIERS[difficulty]
    var note_count = int(density * 100.0 * (song.duration / 180.0))

    for i in range(note_count):
        var note = GameData.NoteData.new()
        note.time = i * (song.duration / note_count)
        note.track = _rng.randi_range(0, 3)
        note.type = GameData.GameData.NoteType.TAP

        # 添加变化
        if difficulty >= GameManagerClass.Difficulty.HARD and _rng.randf() < 0.15:
            note.type = GameData.GameData.NoteType.HOLD
            note.end_time = note.time + _rng.randf_range(0.3, 1.5)

        if difficulty == GameManagerClass.Difficulty.EXPERT and _rng.randf() < 0.1:
            note.type = GameData.NoteData.Swipe
            note.swipe_direction = _rng.randi_range(1, 4)

        chart.notes.append(note)

    return chart


func create_mock_leaderboard(song_id: String, difficulty: int) -> Array[Dictionary]:
    var entries = []
    var grades = ["S+", "S", "A", "A", "B", "B", "C", "D"]

    for i in range(1, 21):
        entries.append({
            "rank": i,
            "user_id": "user_%02d" % i,
            "nickname": get_mock_player_name(i),
            "score": max(100000, 1000000 - i * 40000 + _rng.randi_range(-5000, 5000)),
            "grade": grades[min(i - 1, grades.size() - 1)],
            "max_combo": max(10, 200 - i * 8 + _rng.randi_range(-5, 5)),
            "play_time": Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
        })

    return entries


func get_mock_player_name(index: int) -> String:
    var names = [
        "ProPlayer", "RhythmKing", "BeatMaster", "NoteHunter",
        "ComboQueen", "SpeedDemon", "MusicLover", "ChartMaker",
        "SonicWave", "NightCore", "DayCore", "BassDrop",
        "MelodyChaser", "SoundSeeker", "TempoTamer", "FlowFinder",
        "GrooveGuardian", "PulsePounder", "SyncSurfer", "BeatBender"
    ]
    return names[min(index - 1, names.size() - 1)]


func create_mock_achievements() -> Array[Dictionary]:
    return [
        {"id": "first_clear", "name": "First Steps", "description": "Clear your first song", "target": 1, "is_hidden": false},
        {"id": "first_import", "name": "Music Collector", "description": "Import your first local song", "target": 1, "is_hidden": false},
        {"id": "combo_50", "name": "Combo Starter", "description": "Achieve 50 combo", "target": 50, "is_hidden": false},
        {"id": "combo_100", "name": "Combo Master", "description": "Achieve 100 combo", "target": 100, "is_hidden": false},
        {"id": "combo_500", "name": "Combo Legend", "description": "Achieve 500 combo", "target": 500, "is_hidden": false},
        {"id": "combo_1000", "name": "Combo King", "description": "Achieve 1000 combo", "target": 1000, "is_hidden": false},
        {"id": "collection_10", "name": "Song Library", "description": "Collect 10 songs", "target": 10, "is_hidden": false},
        {"id": "collection_50", "name": "Music Archive", "description": "Collect 50 songs", "target": 50, "is_hidden": false},
        {"id": "first_s_rank", "name": "S-Rank Achiever", "description": "Get an S rank on any song", "target": 1, "is_hidden": false},
        {"id": "first_full_combo", "name": "Full Combo!", "description": "Achieve full combo on any song", "target": 1, "is_hidden": false},
        {"id": "first_all_perfect", "name": "Perfect Player", "description": "Get all perfect on any song", "target": 1, "is_hidden": false},
        {"id": "chart_create_1", "name": "Chart Creator", "description": "Create your first chart", "target": 1, "is_hidden": false},
        {"id": "chart_create_10", "name": "Chart Architect", "description": "Create 10 charts", "target": 10, "is_hidden": false},
        {"id": "hidden_1", "name": "???", "description": "Unknown achievement", "target": 100, "is_hidden": true, "reward": "Hidden Track: Code of Sound"},
        {"id": "hidden_2", "name": "???", "description": "Unknown achievement", "target": 20, "is_hidden": true, "reward": "Hidden Track: Silent Challenge"},
        {"id": "hidden_3", "name": "???", "description": "Unknown achievement", "target": 1, "is_hidden": true, "reward": "Hidden Track: Reverse World"},
        {"id": "hidden_4", "name": "???", "description": "Unknown achievement", "target": 100, "is_hidden": true, "reward": "Hidden Track: Chaos Maze"},
        {"id": "hidden_5", "name": "???", "description": "Unknown achievement", "target": 100, "is_hidden": true, "reward": "Hidden Track: Developer Mode"}
    ]


# ============================================================
# 认证相关
# ============================================================
func send_otp(phone_number: String) -> Dictionary:
    await simulate_network_delay()

    return {
        "success": true,
        "message": "OTP sent successfully (Mock)",
        "error_code": 0
    }


func verify_otp(phone_number: String, code: String) -> Dictionary:
    await simulate_network_delay()

    # 模拟：接受任何 6 位代码
    if code.length() == 6:
        _current_user_id = "mock_user_" + str(phone_number.hash())
        _current_token = "mock_token_" + str(Time.get_ticks_usec()).substr(0, 8)

        # 存储认证信息
        APIConfig.set_user_id(_current_user_id)
        APIConfig.set_token(_current_token)

        return {
            "success": true,
            "message": "Login successful (Mock)",
            "user_id": _current_user_id,
            "token": _current_token,
            "nickname": "MockPlayer",
            "error_code": 0
        }

    return {
        "success": false,
        "message": "Invalid OTP (Mock)",
        "error_code": 401
    }


func get_current_user() -> Dictionary:
    await simulate_network_delay()

    var has_auth = not APIConfig.get_token().is_empty()

    return {
        "success": has_auth,
        "message": "User retrieved (Mock)" if has_auth else "Not authenticated (Mock)",
        "user_id": APIConfig.get_user_id() if has_auth else null,
        "token": APIConfig.get_token() if has_auth else null,
        "nickname": "MockPlayer" if has_auth else null,
        "error_code": 0 if has_auth else 401
    }


func logout() -> bool:
    await simulate_network_delay()

    _current_user_id = ""
    _current_token = ""
    APIConfig.clear_auth()

    return true


# ============================================================
# 歌曲和谱面
# ============================================================
func get_song_list(page: int = 0, limit: int = 20) -> Array[GameData.SongData]:
    await simulate_network_delay()

    var songs = []
    for song in _mock_songs.values():
        songs.append(song)

    # 分页
    var start = page * limit
    var end = min(start + limit, songs.size())

    if start >= songs.size():
        return []

    return songs.slice(start, end)


func get_song_detail(song_id: String) -> GameData.SongData:
    await simulate_network_delay()

    if _mock_songs.has(song_id):
        return _mock_songs[song_id]

    return null


func get_community_charts(song_id: String) -> Array[GameData.ChartData]:
    await simulate_network_delay()

    var charts = []
    for i in range(3):
        var chart = GameData.ChartData.new()
        chart.id = song_id + "_community_" + str(i)
        chart.difficulty = _rng.randi_range(0, 3)
        chart.track_count = 4
        chart.bpm = 120.0 + _rng.randi_range(0, 60)
        charts.append(chart)

    return charts


func upload_chart(chart: GameData.ChartData, song_id: String) -> Dictionary:
    await simulate_network_delay()

    return {
        "success": true,
        "message": "Chart uploaded successfully (Mock)",
        "chart_id": chart.id + "_uploaded",
        "download_url": "https://mock.storage.rhythmx.app/" + chart.id
    }


# ============================================================
# 玩家数据同步
# ============================================================
func sync_player_data(local_data: GameData.PlayerData) -> GameData.PlayerData:
    await simulate_network_delay()

    if not APIConfig.get_user_id().is_empty():
        local_data.player_id = APIConfig.get_user_id()

    local_data.last_play_date = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
    APIConfig.set_last_sync_time(Time.get_unix_time_from_system() as int)

    return local_data


func upload_play_record(record: Dictionary) -> bool:
    await simulate_network_delay()
    return true


func get_leaderboard(song_id: String, difficulty: int) -> Array[Dictionary]:
    await simulate_network_delay()

    var key = song_id + "_" + str(difficulty)
    if _mock_leaderboards.has(key):
        return _mock_leaderboards[key]

    return []


# ============================================================
# 成就
# ============================================================
func get_achievement_definitions() -> Array[Dictionary]:
    await simulate_network_delay()
    return _mock_achievements


func sync_achievements(achievements: Array[GameData.AchievementRecord]) -> bool:
    await simulate_network_delay()
    return true


# ============================================================
# 健康检查
# ============================================================
func is_online_check() -> bool:
    await get_tree().create_timer(0.1).timeout
    return is_online


func get_api_status() -> Dictionary:
    await get_tree().create_timer(0.1).timeout

    return {
        "is_online": is_online,
        "version": "1.0.0-mock",
        "server_time": Time.get_datetime_dict_from_system(),
        "maintenance_mode": 0,
        "message": "Mock API is running"
    }


# ============================================================
# 工具
# ============================================================
func simulate_network_delay() -> void:
    # 模拟 100-500ms 网络延迟
    var delay = _rng.randf_range(0.1, 0.5)
    await get_tree().create_timer(delay).timeout


## 用于测试：切换离线模式
func set_online_status(online: bool) -> void:
    is_online = online
    print("MockAPI online status set to: ", online)


## 用于测试：清除并重新生成模拟数据
func reset_mock_data() -> void:
    initialize_mock_data()
