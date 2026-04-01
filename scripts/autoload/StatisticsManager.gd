class_name StatisticsManagerClass
extends Node

## 统计数据跟踪
## 单例 autoload

const GameData = preload("res://scripts/data/GameData.gd")

var _recent_plays: Array[Dictionary] = []
var _overall_stats: Dictionary = {}

static var instance: StatisticsManagerClass


func _ready() -> void:
    instance = self
    load_statistics()


func load_statistics() -> void:
    if FileAccess.file_exists("user://statistics.json"):
        var file = FileAccess.open("user://statistics.json", FileAccess.READ)
        if file:
            var json = file.get_as_text()
            var data = JSON.parse_string(json)
            if data:
                _recent_plays = data.get("recent_plays", [])
                _overall_stats = data.get("overall", {})


func save_statistics() -> void:
    var data = {
        "recent_plays": _recent_plays,
        "overall": _overall_stats
    }
    var json = JSON.stringify(data)
    var file = FileAccess.open("user://statistics.json", FileAccess.WRITE)
    if file:
        file.store_string(json)


func record_play(song: GameData.SongData, result: GameData.ScoreResult, difficulty: int) -> void:
    var session = {
        "song_id": song.id,
        "song_name": song.name,
        "difficulty": difficulty,
        "score": result.score,
        "grade": result.grade,
        "max_combo": result.max_combo,
        "accuracy": result.accuracy,
        "play_time": Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
    }

    _recent_plays.insert(0, session)
    if _recent_plays.size() > 50:
        _recent_plays.remove_at(_recent_plays.size() - 1)

    if not _overall_stats.has("total_plays"):
        _overall_stats["total_plays"] = 0
    if not _overall_stats.has("total_notes"):
        _overall_stats["total_notes"] = 0
    if not _overall_stats.has("max_combo"):
        _overall_stats["max_combo"] = 0

    _overall_stats["total_plays"] += 1
    _overall_stats["total_notes"] += result.perfect_count + result.great_count + result.good_count + result.miss_count

    if result.max_combo > _overall_stats["max_combo"]:
        _overall_stats["max_combo"] = result.max_combo

    save_statistics()


func get_recent_plays(count: int = 10) -> Array[Dictionary]:
    return _recent_plays.slice(0, min(count, _recent_plays.size()))


func get_overall_statistics() -> Dictionary:
    return _overall_stats
