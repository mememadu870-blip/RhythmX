# ============================================================
# SongData - 歌曲数据
# ============================================================
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

const GameTypes = preload("res://scripts/game/GameTypes.gd")
const ChartData = preload("res://scripts/data/types/ChartData.gd")


func get_chart(difficulty: int):
    for chart in charts:
        if chart.difficulty == difficulty:
            return chart
    return null


func has_chart(difficulty: int) -> bool:
    for chart in charts:
        if chart.difficulty == difficulty:
            return true
    return false


static func from_dict(data: Dictionary):
    var song = new()
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
