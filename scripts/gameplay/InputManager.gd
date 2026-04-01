class_name InputManagerClass
extends Node

## 处理节奏游戏的键盘和触摸输入
## 单例 autoload

signal on_track_pressed(track: int)
signal on_track_released(track: int)

# 默认按键绑定 (D, F, J, K 对应 4 个轨道)
const TRACK_ACTIONS: Array[String] = ["tap_1", "tap_2", "tap_3", "tap_4"]

var _track_pressed: Array[bool] = []
var _track_previously_pressed: Array[bool] = []
var _track_count: int = 4

static var instance: InputManagerClass


func _ready() -> void:
    instance = self
    _track_pressed = [false, false, false, false]
    _track_previously_pressed = [false, false, false, false]


func set_track_count(count: int) -> void:
    _track_count = count
    _track_pressed = []
    _track_previously_pressed = []
    for i in range(count):
        _track_pressed.append(false)
        _track_previously_pressed.append(false)


func _input(event: InputEvent) -> void:
    # 处理键盘输入
    if event is InputEventKey:
        for i in range(min(TRACK_ACTIONS.size(), _track_count)):
            if event.is_action(TRACK_ACTIONS[i]):
                if event.is_pressed() and not _track_previously_pressed[i]:
                    handle_track_down(i)
                elif not event.is_pressed() and _track_previously_pressed[i]:
                    handle_track_up(i)

        # 处理暂停
        if event.is_action_pressed("pause"):
            pass  # 暂停由 GameplayUI 处理

    # 处理触摸输入
    if event is InputEventScreenTouch:
        var track = get_track_from_position(event.position)
        if track >= 0 and track < _track_count:
            if event.pressed:
                handle_track_down(track)
            else:
                handle_track_up(track)

    # 处理鼠标输入
    if event is InputEventMouseButton:
        var track = get_track_from_position(event.position)
        if track >= 0 and track < _track_count:
            if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
                handle_track_down(track)
            elif not event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
                handle_track_up(track)


func handle_track_down(track: int) -> void:
    if track < 0 or track >= _track_count:
        return

    _track_pressed[track] = true
    on_track_pressed.emit(track)

    # 通知 TrackManager
    if TrackManager:
        TrackManager.on_track_pressed(track)

    # 通知 GameplayUI 进行音符判定
    var gameplay_ui = get_gameplay_ui()
    if gameplay_ui:
        gameplay_ui.handle_track_input(track)


func handle_track_up(track: int) -> void:
    if track < 0 or track >= _track_count:
        return

    _track_pressed[track] = false
    on_track_released.emit(track)

    if TrackManager:
        TrackManager.on_track_released(track)


func get_gameplay_ui() -> Control:
    var root = get_tree().current_scene
    if root == null:
        return null
    # 尝试获取 GameplayUI 节点
    if root.name == "Gameplay" or root.has_method("handle_track_input"):
        return root
    return null


func _process(_delta: float) -> void:
    # 更新之前的状态用于检测变化
    for i in range(_track_count):
        _track_previously_pressed[i] = _track_pressed[i]

        # 检查连续输入状态
        if Input.is_action_pressed(TRACK_ACTIONS[i]):
            _track_pressed[i] = true


func get_track_from_position(screen_position: Vector2) -> int:
    var gameplay_ui = get_gameplay_ui()
    if gameplay_ui == null:
        return -1

    var track_container = gameplay_ui.get_node_or_null("TrackContainer")
    if track_container == null:
        return -1

    # 获取轨道容器的全局位置
    var container_rect = track_container.get_global_rect()
    var local_pos = container_rect.position
    var size = track_container.size

    var track_width = size.x / _track_count
    var relative_x = screen_position.x - local_pos.x

    var track = int(relative_x / track_width)

    if track >= 0 and track < _track_count:
        return track

    return -1


func is_track_pressed(track: int) -> bool:
    if track < 0 or track >= _track_pressed.size():
        return false
    return _track_pressed[track]


## 检测滑动手势方向
func detect_swipe(start_pos: Vector2, end_pos: Vector2, min_distance: float = 50.0) -> SwipeDirection:
    var delta = end_pos - start_pos

    if delta.length() < min_distance:
        return SwipeDirection.NONE

    var angle = atan2(delta.y, delta.x) * 57.29578  # RAD_TO_DEG = 180/PI

    # 调整屏幕坐标 (Y 轴反转)
    angle = -angle

    if angle >= -45.0 and angle < 45.0:
        return SwipeDirection.RIGHT
    elif angle >= 45.0 and angle < 135.0:
        return SwipeDirection.UP
    elif angle >= -135.0 and angle < -45.0:
        return SwipeDirection.DOWN
    else:
        return SwipeDirection.LEFT


## 获取当前轨道状态数组
func get_track_states() -> Array[bool]:
    return _track_pressed
