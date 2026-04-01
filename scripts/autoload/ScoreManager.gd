class_name ScoreManagerClass
extends Node

## 处理计分、连击和等级计算

const GameData = preload("res://scripts/data/GameData.gd")

signal on_judgment(judgment: Judgment, combo: int)
signal on_combo_changed(combo: int)
signal on_score_changed(score: int)

# 判定窗口 (秒)
const PERFECT_WINDOW: float = 0.045
const GREAT_WINDOW: float = 0.090
const GOOD_WINDOW: float = 0.135

# 基础分数
const PERFECT_SCORE: int = 300
const GREAT_SCORE: int = 200
const GOOD_SCORE: int = 100

# 连击阈值和倍率
const COMBO_THRESHOLDS: Array[int] = [10, 25, 50, 100, 200, 500]
const COMBO_MULTIPLIERS: Array[float] = [1.0, 1.1, 1.2, 1.3, 1.5, 2.0]

enum Judgment {
    PERFECT,
    GREAT,
    GOOD,
    MISS
}

# 分数状态
var score: int = 0
var combo: int = 0
var max_combo: int = 0
var perfect_count: int = 0
var great_count: int = 0
var good_count: int = 0
var miss_count: int = 0
var total_notes: int = 0
var accuracy: float = 1.0

var grade: String:
    get:
        return calculate_grade()

var is_full_combo: bool:
    get:
        return miss_count == 0 and total_notes > 0

var is_all_perfect: bool:
    get:
        return perfect_count == total_notes and total_notes > 0

var _judgments: Array[Dictionary] = []

static var instance: ScoreManagerClass


func _ready() -> void:
    instance = self


func init(total_notes_count: int) -> void:
    total_notes = total_notes_count
    reset_score()


func reset_score() -> void:
    score = 0
    combo = 0
    max_combo = 0
    perfect_count = 0
    great_count = 0
    good_count = 0
    miss_count = 0
    accuracy = 1.0
    _judgments.clear()


func judge_hit(hit_time: float, target_time: float) -> Judgment:
    var offset_ms = abs(hit_time - target_time) * 1000.0

    var judgment: Judgment
    if offset_ms <= PERFECT_WINDOW * 1000:
        judgment = Judgment.PERFECT
    elif offset_ms <= GREAT_WINDOW * 1000:
        judgment = Judgment.GREAT
    elif offset_ms <= GOOD_WINDOW * 1000:
        judgment = Judgment.GOOD
    else:
        judgment = Judgment.MISS

    process_judgment(judgment, hit_time - target_time)
    return judgment


func process_miss() -> void:
    process_judgment(Judgment.MISS, 0.0)


func process_judgment(judgment: Judgment, offset: float) -> void:
    _judgments.append({
        "type": judgment,
        "offset": offset,
        "time": current_time()
    })

    match judgment:
        Judgment.PERFECT:
            perfect_count += 1
            combo += 1
            add_score(PERFECT_SCORE)
        Judgment.GREAT:
            great_count += 1
            combo += 1
            add_score(GREAT_SCORE)
        Judgment.GOOD:
            good_count += 1
            combo += 1
            add_score(GOOD_SCORE)
        Judgment.MISS:
            miss_count += 1
            combo = 0

    if combo > max_combo:
        max_combo = combo

    update_accuracy()

    on_judgment.emit(judgment, combo)
    on_combo_changed.emit(combo)
    on_score_changed.emit(score)


func add_score(base_score: int) -> void:
    var multiplier: float = get_combo_multiplier()
    score += int(base_score * multiplier)


func get_combo_multiplier() -> float:
    for i in range(COMBO_THRESHOLDS.size() - 1, -1, -1):
        if combo >= COMBO_THRESHOLDS[i]:
            return COMBO_MULTIPLIERS[i]
    return 1.0


func update_accuracy() -> void:
    if total_notes == 0:
        accuracy = 1.0
        return

    var total_weight: float = float(perfect_count) * 1.0 + float(great_count) * 0.7 + float(good_count) * 0.4
    var max_weight: float = float(perfect_count + great_count + good_count + miss_count)
    accuracy = total_weight / max_weight


func calculate_grade() -> String:
    if total_notes == 0:
        return "D"

    if is_all_perfect:
        return "S+"
    if is_full_combo and accuracy >= 0.98:
        return "S+"
    if accuracy >= 0.95:
        return "S"
    if accuracy >= 0.90:
        return "A"
    if accuracy >= 0.80:
        return "B"
    if accuracy >= 0.70:
        return "C"
    return "D"


func get_result() -> GameData.ScoreResult:
    var result = GameData.ScoreResult.new()
    result.score = score
    result.max_combo = max_combo
    result.perfect_count = perfect_count
    result.great_count = great_count
    result.good_count = good_count
    result.miss_count = miss_count
    result.total_notes = total_notes
    result.accuracy = accuracy
    result.grade = grade
    result.is_full_combo = is_full_combo
    result.is_all_perfect = is_all_perfect
    return result


func current_time() -> float:
    if AudioManager:
        return AudioManager.current_time
    return 0.0
