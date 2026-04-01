class_name TrackManagerClass
extends Node

## 管理轨道输入和视觉反馈
## 单例 autoload

const ScoreManagerClass = preload("res://scripts/autoload/ScoreManager.gd")

signal on_track_hit(track: int, judgment: ScoreManagerClass.Judgment)
signal on_track_miss(track: int)

@export var track_count: int = 4
@export var track_width: float = 720.0
@export var hit_line_y: float = 400.0

# 轨道颜色
const TRACK_COLORS: Array[Color] = [
    Color(0.6, 0.97, 1.0),   # Track 1 - 青色
    Color(1.0, 0.35, 0.89),  # Track 2 - 紫色
    Color(0.6, 0.97, 1.0),   # Track 3 - 青色
    Color(1.0, 0.35, 0.89)   # Track 4 - 紫色
]

var _lanes: Array[Dictionary] = []

static var instance: TrackManagerClass


func _ready() -> void:
    instance = self
    initialize_tracks()


func initialize_tracks() -> void:
    _lanes.clear()

    var track_spacing = track_width / track_count
    var start_x = -track_width / 2.0 + track_spacing / 2.0

    for i in range(track_count):
        _lanes.append({
            "index": i,
            "x": start_x + i * track_spacing,
            "color": get_track_color(i),
            "is_pressed": false,
            "is_holding": false,
            "press_time": 0.0,
            "hold_note": null
        })


func get_track_color(track: int) -> Color:
    if track >= 0 and track < TRACK_COLORS.size():
        return TRACK_COLORS[track]
    return Color.WHITE


func get_track_x(track: int) -> float:
    if track >= 0 and track < track_count:
        return _lanes[track]["x"]
    return 0.0


func on_track_pressed(track: int) -> void:
    if track < 0 or track >= track_count:
        return

    _lanes[track]["is_pressed"] = true
    _lanes[track]["press_time"] = Time.get_ticks_msec() / 1000.0

    # 视觉反馈
    highlight_track(track, true)


func on_track_released(track: int) -> void:
    if track < 0 or track >= track_count:
        return

    _lanes[track]["is_pressed"] = false

    # 处理长按音符释放
    if _lanes[track]["is_holding"] and _lanes[track]["hold_note"]:
        var current_time = 0.0
        if AudioManager:
            current_time = AudioManager.current_time

        var note = _lanes[track]["hold_note"]

        if current_time < note.data.end_time:
            # 早期释放
            var gameplay_ui = get_gameplay_ui()
            if gameplay_ui:
                gameplay_ui.handle_hold_release(track)

        _lanes[track]["is_holding"] = false
        _lanes[track]["hold_note"] = null

    highlight_track(track, false)


func set_holding_note(track: int, note: Node) -> void:
    if track < 0 or track >= track_count:
        return
    _lanes[track]["is_holding"] = true
    _lanes[track]["hold_note"] = note


func clear_hold_note(track: int) -> void:
    if track < 0 or track >= track_count:
        return
    _lanes[track]["is_holding"] = false
    _lanes[track]["hold_note"] = null


func is_track_holding(track: int) -> bool:
    if track < 0 or track >= track_count:
        return false
    return _lanes[track]["is_holding"]


func update_hold_notes() -> void:
    var current_time = 0.0
    if AudioManager:
        current_time = AudioManager.current_time

    for lane in _lanes:
        if lane["is_holding"] and lane["hold_note"]:
            var note = lane["hold_note"]
            if current_time >= note.data.end_time:
                if note.has_method("complete_hold"):
                    note.complete_hold()
                lane["is_holding"] = false
                lane["hold_note"] = null


func show_hit_effect(track: int, judgment: ScoreManagerClass.Judgment) -> void:
    if track < 0 or track >= track_count:
        return

    var gameplay_ui = get_gameplay_ui()
    if gameplay_ui:
        gameplay_ui.show_hit_effect(track, judgment, _lanes[track]["x"])

    on_track_hit.emit(track, judgment)


func show_miss_effect(track: int) -> void:
    if track < 0 or track >= track_count:
        return

    var gameplay_ui = get_gameplay_ui()
    if gameplay_ui:
        gameplay_ui.show_miss_effect(track, _lanes[track]["x"])

    on_track_miss.emit(track)


func highlight_track(track: int, highlight: bool) -> void:
    var gameplay_ui = get_gameplay_ui()
    if gameplay_ui:
        gameplay_ui.highlight_track(track, highlight, _lanes[track]["color"])


func get_gameplay_ui() -> Control:
    var root = get_tree().current_scene
    if root == null:
        return null
    # 尝试获取 GameplayUI 节点
    if root.name == "Gameplay" or root.has_method("handle_track_input"):
        return root
    return null


func get_lane(track: int) -> Dictionary:
    if track >= 0 and track < track_count:
        return _lanes[track]
    return {}


func _process(_delta: float) -> void:
    update_hold_notes()
