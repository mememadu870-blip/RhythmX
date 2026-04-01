# ============================================================
# ScoreResult - 分数结果
# ============================================================
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
