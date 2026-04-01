# ============================================================
# PlayRecordRequest - 游玩记录请求
# ============================================================
extends RefCounted

var song_id: String = ""
var chart_id: String = ""
var difficulty: int = 1
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
