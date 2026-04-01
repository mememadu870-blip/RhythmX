# ============================================================
# SongRecord - 歌曲记录
# ============================================================
extends RefCounted

var song_id: String = ""
var play_count: int = 0
var high_score: int = 0
var high_score_grade: String = ""
var high_score_difficulty: int = 1
var max_combo: int = 0
var last_played: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
var full_combo: bool = false
var all_perfect: bool = false


static func from_dict(data: Dictionary):
    var rec = new()
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
