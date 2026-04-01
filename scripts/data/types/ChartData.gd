# ============================================================
# ChartData - 谱面数据
# ============================================================
extends RefCounted

var id: String = ""
var difficulty: int = 1
var track_count: int = 4
var bpm: float = 120.0
var offset: float = 0.0
var notes: Array = []

const NoteData = preload("res://scripts/data/types/NoteData.gd")

var total_notes: int:
    get:
        return notes.size()


static func from_dict(data: Dictionary):
    var chart = new()
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
