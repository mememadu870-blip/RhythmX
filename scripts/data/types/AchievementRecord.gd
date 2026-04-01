# ============================================================
# AchievementRecord - 成就记录
# ============================================================
extends RefCounted

var achievement_id: String = ""
var unlocked: bool = false
var unlock_time: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
var progress: int = 0
var target: int = 0


static func from_dict(data: Dictionary):
    var rec = new()
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
