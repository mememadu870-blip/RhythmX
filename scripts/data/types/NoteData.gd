# ============================================================
# NoteData - 音符数据
# ============================================================
extends RefCounted

var time: float = 0.0
var end_time: float = 0.0
var track: int = 0
var type: int = 0
var swipe_direction: int = 0

var duration: float:
    get:
        return end_time - time


static func from_dict(data: Dictionary):
    var note = new()
    note.time = data.get("time", 0.0)
    note.end_time = data.get("end_time", 0.0)
    note.track = data.get("track", 0)
    note.type = data.get("type", 0)
    note.swipe_direction = data.get("swipe_direction", 0)
    return note


func to_dict() -> Dictionary:
    return {
        "time": time,
        "end_time": end_time,
        "track": track,
        "type": type,
        "swipe_direction": swipe_direction
    }
