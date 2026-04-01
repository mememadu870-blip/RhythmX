# ============================================================
# PlayerData - 玩家存档数据
# ============================================================
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
var achievements: Array = []

var audio_offset: float = 0.0
var note_speed: float = 1.0

const SAVE_PATH = "user://playerdata.json"

const SongRecord = preload("res://scripts/data/types/SongRecord.gd")
const AchievementRecord = preload("res://scripts/data/types/AchievementRecord.gd")


static func load_data():
    if FileAccess.file_exists(SAVE_PATH):
        var file = FileAccess.open(SAVE_PATH, FileAccess.READ)
        if file:
            var json = file.get_as_text()
            var data = JSON.parse_string(json)
            if data:
                return from_dict(data)
    return new()


static func create_new():
    var data = new()
    data.player_id = str(Time.get_ticks_msec()) + "_" + str(randi())
    return data


func save() -> void:
    var json = JSON.stringify(to_dict())
    var file = FileAccess.open(SAVE_PATH, FileAccess.WRITE)
    if file:
        file.store_string(json)


static func from_dict(data: Dictionary):
    var pd = new()
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


func get_record(song_id: String):
    for rec in song_records:
        if rec.song_id == song_id:
            return rec
    return null


func update_record(song_id: String, score: int, max_combo: int, grade: String, difficulty: int) -> void:
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
