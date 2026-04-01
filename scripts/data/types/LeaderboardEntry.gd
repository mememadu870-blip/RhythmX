# ============================================================
# LeaderboardEntry - 排行榜条目
# ============================================================
extends RefCounted

var rank: int = 0
var user_id: String = ""
var nickname: String = ""
var score: int = 0
var grade: String = ""
var max_combo: int = 0
var play_time: Variant = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
