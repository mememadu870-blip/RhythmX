# ============================================================
# 全局枚举类型 - 可在其他脚本中通过 GameData.NoteType 访问
# ============================================================
enum NoteType {
    TAP,
    HOLD,
    SWIPE
}

enum SwipeDirection {
    NONE,
    LEFT,
    RIGHT,
    UP,
    DOWN
}

# 引用 GameManager 的 Difficulty 枚举（避免循环依赖）
const GameManagerClass = preload("res://scripts/autoload/GameManager.gd")


# ============================================================
# GameData - 游戏数据定义（包含 SongData、ChartData、NoteData、PlayerData 等）
# ============================================================
class_name GameData
extends RefCounted

var id: String = ""
var name: String = ""
var artist: String = ""
var bpm: float = 120.0
var duration: float = 0.0
var audio_path: String = ""
var is_imported: bool = false
var is_favorite: bool = false
var play_count: int = 0
var high_score: int = 0
var charts: Array = []


func get_chart(difficulty: GameManagerClass.Difficulty) -> ChartData:
    for chart in charts:
        if chart.difficulty == difficulty:
            return chart
    return null


func has_chart(difficulty: GameManagerClass.Difficulty) -> bool:
    for chart in charts:
        if chart.difficulty == difficulty:
            return true
    return false


static func from_dict(data: Dictionary) -> SongData:
    var song = SongData.new()
    song.id = data.get("id", "")
    song.name = data.get("name", "")
    song.artist = data.get("artist", "")
    song.bpm = data.get("bpm", 120.0)
    song.duration = data.get("duration", 0.0)
    song.audio_path = data.get("audio_path", "")
    song.is_imported = data.get("is_imported", false)
    song.is_favorite = data.get("is_favorite", false)
    song.play_count = data.get("play_count", 0)
    song.high_score = data.get("high_score", 0)

    var charts_data = data.get("charts", [])
    for chart_data in charts_data:
        song.charts.append(ChartData.from_dict(chart_data))

    return song


func to_dict() -> Dictionary:
    var charts_data = []
    for chart in charts:
        charts_data.append(chart.to_dict())

    return {
        "id": id,
        "name": name,
        "artist": artist,
        "bpm": bpm,
        "duration": duration,
        "audio_path": audio_path,
        "is_imported": is_imported,
        "is_favorite": is_favorite,
        "play_count": play_count,
        "high_score": high_score,
        "charts": charts_data
    }


# ============================================================
# ChartData - 谱面数据
# ============================================================
class ChartData
extends RefCounted

var id: String = ""
var difficulty: GameManagerClass.Difficulty = GameManagerClass.Difficulty.NORMAL
var track_count: int = 4
var bpm: float = 120.0
var offset: float = 0.0
var notes: Array = []

var total_notes: int:
    get:
        return notes.size()


static func from_dict(data: Dictionary) -> ChartData:
    var chart = ChartData.new()
    chart.id = data.get("id", "")
    chart.difficulty = data.get("difficulty", 1)
    chart.track_count = data.get("track_count", 4)
    chart.bpm = data.get("bpm", 120.0)
    chart.offset = data.get("offset", 0.0)

    var notes_data = data.get("notes", [])
    for note_data in notes_data:
        chart.notes.append(NoteData.from_dict(note_data))

    return chart


func to_dict() -> Dictionary:
    var notes_data = []
    for note in notes:
        notes_data.append(note.to_dict())

    return {
        "id": id,
        "difficulty": difficulty,
        "track_count": track_count,
        "bpm": bpm,
        "offset": offset,
        "notes": notes_data
    }


# ============================================================
# NoteData - 音符数据
# ============================================================
class NoteData
extends RefCounted

var time: float = 0.0
var end_time: float = 0.0
var track: int = 0
var type: NoteType = NoteType.TAP
var swipe_direction: SwipeDirection = SwipeDirection.NONE

var duration: float:
    get:
        return end_time - time


static func from_dict(data: Dictionary) -> NoteData:
    var note = NoteData.new()
    note.time = data.get("time", 0.0)
    note.end_time = data.get("end_time", 0.0)
    note.track = data.get("track", 0)
    note.type = data.get("type", NoteType.TAP)
    note.swipe_direction = data.get("swipe_direction", SwipeDirection.NONE)
    return note


func to_dict() -> Dictionary:
    return {
        "time": time,
        "end_time": end_time,
        "track": track,
        "type": type,
        "swipe_direction": swipe_direction
    }


# ============================================================
# PlayerData - 玩家存档数据
# ============================================================
class PlayerData
extends RefCounted

var player_id: String = ""
var nickname: String = "Player"
var total_play_count: int = 0
var total_play_time: float = 0.0
var max_combo: int = 0
var first_play_date: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
var last_play_date: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)

var song_records: Array = []
var favorite_songs: Array[String] = []
var imported_songs: Array[String] = []
var created_charts: Array[String] = []
var achievements: Array[AchievementRecord] = []

var audio_offset: float = 0.0
var note_speed: float = 1.0

const SAVE_PATH = "user://playerdata.json"


static func load_data() -> PlayerData:
    if FileAccess.file_exists(SAVE_PATH):
        var file = FileAccess.open(SAVE_PATH, FileAccess.READ)
        if file:
            var json = file.get_as_text()
            var data = JSON.parse_string(json)
            if data:
                return from_dict(data)
    return create_new()


static func create_new() -> PlayerData:
    var data = PlayerData.new()
    data.player_id = str(Time.get_ticks_msec()) + "_" + str(randi())
    return data


func save() -> void:
    var json = JSON.stringify(to_dict())
    var file = FileAccess.open(SAVE_PATH, FileAccess.WRITE)
    if file:
        file.store_string(json)


static func from_dict(data: Dictionary) -> PlayerData:
    var pd = PlayerData.new()
    pd.player_id = data.get("player_id", "")
    pd.nickname = data.get("nickname", "Player")
    pd.total_play_count = data.get("total_play_count", 0)
    pd.total_play_time = data.get("total_play_time", 0.0)
    pd.max_combo = data.get("max_combo", 0)
    pd.first_play_date = data.get("first_play_date", Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false))
    pd.last_play_date = data.get("last_play_date", Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false))

    var records_data = data.get("song_records", [])
    for rd in records_data:
        pd.song_records.append(SongRecord.from_dict(rd))

    pd.favorite_songs = data.get("favorite_songs", [])
    pd.imported_songs = data.get("imported_songs", [])
    pd.created_charts = data.get("created_charts", [])

    var ach_data = data.get("achievements", [])
    for ad in ach_data:
        pd.achievements.append(AchievementRecord.from_dict(ad))

    pd.audio_offset = data.get("audio_offset", 0.0)
    pd.note_speed = data.get("note_speed", 1.0)

    return pd


func to_dict() -> Dictionary:
    var records_data = []
    for rec in song_records:
        records_data.append(rec.to_dict())

    var ach_data = []
    for ach in achievements:
        ach_data.append(ach.to_dict())

    return {
        "player_id": player_id,
        "nickname": nickname,
        "total_play_count": total_play_count,
        "total_play_time": total_play_time,
        "max_combo": max_combo,
        "first_play_date": first_play_date,
        "last_play_date": last_play_date,
        "song_records": records_data,
        "favorite_songs": favorite_songs,
        "imported_songs": imported_songs,
        "created_charts": created_charts,
        "achievements": ach_data,
        "audio_offset": audio_offset,
        "note_speed": note_speed
    }


func get_record(song_id: String) -> SongRecord:
    for rec in song_records:
        if rec.song_id == song_id:
            return rec
    return null


func update_record(song_id: String, score: int, max_combo: int, grade: String, difficulty: GameManagerClass.Difficulty) -> void:
    var record = get_record(song_id)
    if record == null:
        record = SongRecord.new()
        record.song_id = song_id
        song_records.append(record)

    record.play_count += 1
    record.last_played = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)

    if score > record.high_score:
        record.high_score = score
        record.high_score_grade = grade
        record.high_score_difficulty = difficulty

    if max_combo > record.max_combo:
        record.max_combo = max_combo

    save()


# ============================================================
# SongRecord - 歌曲记录
# ============================================================
class SongRecord
extends RefCounted

var song_id: String = ""
var play_count: int = 0
var high_score: int = 0
var high_score_grade: String = ""
var high_score_difficulty: GameManagerClass.Difficulty = GameManagerClass.Difficulty.NORMAL
var max_combo: int = 0
var last_played: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
var full_combo: bool = false
var all_perfect: bool = false


static func from_dict(data: Dictionary) -> SongRecord:
    var rec = SongRecord.new()
    rec.song_id = data.get("song_id", "")
    rec.play_count = data.get("play_count", 0)
    rec.high_score = data.get("high_score", 0)
    rec.high_score_grade = data.get("high_score_grade", "")
    rec.high_score_difficulty = data.get("high_score_difficulty", 1)
    rec.max_combo = data.get("max_combo", 0)
    rec.last_played = data.get("last_played", Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false))
    rec.full_combo = data.get("full_combo", false)
    rec.all_perfect = data.get("all_perfect", false)
    return rec


func to_dict() -> Dictionary:
    return {
        "song_id": song_id,
        "play_count": play_count,
        "high_score": high_score,
        "high_score_grade": high_score_grade,
        "high_score_difficulty": high_score_difficulty,
        "max_combo": max_combo,
        "last_played": last_played,
        "full_combo": full_combo,
        "all_perfect": all_perfect
    }


# ============================================================
# AchievementRecord - 成就记录
# ============================================================
class AchievementRecord
extends RefCounted

var achievement_id: String = ""
var unlocked: bool = false
var unlock_time: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
var progress: int = 0
var target: int = 0


static func from_dict(data: Dictionary) -> AchievementRecord:
    var rec = AchievementRecord.new()
    rec.achievement_id = data.get("achievement_id", "")
    rec.unlocked = data.get("unlocked", false)
    rec.unlock_time = data.get("unlock_time", Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false))
    rec.progress = data.get("progress", 0)
    rec.target = data.get("target", 0)
    return rec


func to_dict() -> Dictionary:
    return {
        "achievement_id": achievement_id,
        "unlocked": unlocked,
        "unlock_time": unlock_time,
        "progress": progress,
        "target": target
    }


# ============================================================
# AchievementDefinition - 成就定义
# ============================================================
class AchievementDefinition
extends RefCounted

var id: String = ""
var name: String = ""
var description: String = ""
var target: int = 0
var is_hidden: bool = false
var icon_path: String = ""
var reward: String = ""


# ============================================================
# API 相关数据结构
# ============================================================
class LeaderboardEntry
extends RefCounted

var rank: int = 0
var user_id: String = ""
var nickname: String = ""
var score: int = 0
var grade: String = ""
var max_combo: int = 0
var play_time: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)


class PlayRecordRequest
extends RefCounted

var song_id: String = ""
var chart_id: String = ""
var difficulty: GameManagerClass.Difficulty = GameManagerClass.Difficulty.NORMAL
var score: int = 0
var max_combo: int = 0
var grade: String = ""
var perfect_count: int = 0
var great_count: int = 0
var good_count: int = 0
var miss_count: int = 0
var accuracy: float = 0.0
var is_full_combo: bool = false
var is_all_perfect: bool = false
var play_time: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)


class ScoreResult
extends RefCounted

var score: int = 0
var max_combo: int = 0
var perfect_count: int = 0
var great_count: int = 0
var good_count: int = 0
var miss_count: int = 0
var total_notes: int = 0
var accuracy: float = 1.0
var grade: String = "D"
var is_full_combo: bool = false
var is_all_perfect: bool = false
