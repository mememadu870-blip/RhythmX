class_name NoteObjectClass
extends Node2D

## 节奏游戏音符的视觉表现
## 处理音符渲染、移动和判定

const GameData = preload("res://scripts/data/GameData.gd")

# 音符数据
var data: GameData.NoteData

# 视觉设置
var track_width: float = 180.0
var note_color: Color = Color(0.4, 1.0, 1.0)
var note_speed: float = 400.0

# 状态
var was_hit: bool = false
var was_missed: bool = false
var is_holding: bool = false

# 视觉组件
var _note_sprite: Sprite2D
var _hold_tail: Sprite2D

# 参考位置
var _spawn_y: float = -400.0
var _hit_line_y: float = 400.0
var _despawn_y: float = 500.0

# 长按音符跟踪
var _hold_start_time: float = 0.0
var _hold_end_time: float = 0.0


func _ready() -> void:
    create_visuals()


func create_visuals() -> void:
    # 创建音符精灵
    _note_sprite = Sprite2D.new()
    update_visuals()
    add_child(_note_sprite)

    # 如果是长按音符，创建尾部
    if data and data.type == GameData.NoteType.HOLD:
        create_hold_tail()


func update_visuals() -> void:
    if data == null:
        return

    var size = get_note_size()

    var image = Image.create(int(size.x), int(size.y), false, Image.FORMAT_RGBA8)
    image.fill(note_color)

    var texture = ImageTexture.create_from_image(image)
    _note_sprite.texture = texture
    _note_sprite.offset = Vector2(0, size.y / 2.0)


func get_note_size() -> Vector2:
    if data == null:
        return Vector2(track_width - 20, 30)

    match data.type:
        GameData.NoteType.HOLD:
            return Vector2(track_width - 20, 40)
        GameData.NoteType.SWIPE:
            return Vector2(track_width - 20, 50)
        _:
            return Vector2(track_width - 20, 30)


func create_hold_tail() -> void:
    if data == null or data.type != GameData.NoteType.HOLD:
        return

    _hold_tail = Sprite2D.new()

    var hold_duration = data.duration
    var hold_length = hold_duration * note_speed

    var image = Image.create(int(track_width - 20), int(hold_length), false, Image.FORMAT_RGBA8)
    var tail_color = Color(note_color.r, note_color.g, note_color.b, 0.5)
    image.fill(tail_color)

    var texture = ImageTexture.create_from_image(image)
    _hold_tail.texture = texture
    _hold_tail.offset = Vector2(0, hold_length / 2.0)
    _hold_tail.position = Vector2(0, 20)

    add_child(_hold_tail)

    _hold_start_time = data.time
    _hold_end_time = data.end_time


func initialize(note_data: GameData.NoteData, hit_line_y: float, despawn_y: float) -> void:
    data = note_data
    _hit_line_y = hit_line_y
    _despawn_y = despawn_y

    # 根据音符类型设置颜色
    match data.type:
        GameData.NoteType.HOLD:
            note_color = Color(1.0, 0.8, 0.2)   # 金色 - 长按
        GameData.NoteType.SWIPE:
            note_color = Color(0.8, 0.4, 1.0)   # 紫色 - 滑动
        _:
            note_color = Color(0.4, 1.0, 1.0)   # 青色 - 点击

    was_hit = false
    was_missed = false
    is_holding = false

    update_visuals()

    if data.type == GameData.NoteType.HOLD:
        create_hold_tail()


func set_speed(speed: float) -> void:
    note_speed = speed

    # 更新长按尾部长度
    if _hold_tail and data:
        var hold_length = data.duration * speed

        var image = Image.create(int(track_width - 20), int(hold_length), false, Image.FORMAT_RGBA8)
        var tail_color = Color(note_color.r, note_color.g, note_color.b, 0.5)
        image.fill(tail_color)

        var texture = ImageTexture.create_from_image(image)
        _hold_tail.texture = texture
        _hold_tail.offset = Vector2(0, hold_length / 2.0)


func set_spawn_position(spawn_y: float) -> void:
    _spawn_y = spawn_y


func update_position(current_time: float) -> void:
    if data == null:
        return

    # 根据时间差计算 Y 位置
    var time_diff = data.time - current_time
    var y = _hit_line_y - (time_diff * note_speed)

    position = Vector2(position.x, y)


func mark_hit() -> void:
    was_hit = true
    play_hit_animation()


func mark_missed() -> void:
    was_missed = true
    play_miss_animation()


func start_hold() -> void:
    is_holding = true
    # 改变颜色表示正在按住
    note_color = Color(1.0, 1.0, 0.5)
    update_visuals()


func complete_hold() -> void:
    is_holding = false
    was_hit = true
    play_hit_animation()


func release_hold() -> void:
    is_holding = false
    if not was_hit:
        mark_missed()


func play_hit_animation() -> void:
    # 放大并淡出
    var tween = create_tween()
    tween.tween_property(self, "scale", Vector2(1.5, 1.5), 0.1)
    tween.tween_property(self, "modulate:a", 0.0, 0.2)
    tween.tween_callback(queue_free)


func play_miss_animation() -> void:
    # 快速淡出
    var tween = create_tween()
    tween.tween_property(self, "modulate", Color(1.0, 0.3, 0.3), 0.1)
    tween.tween_property(self, "modulate:a", 0.0, 0.3)
    tween.tween_callback(queue_free)


func get_time_offset(current_time: float) -> float:
    if data == null:
        return 0.0
    return current_time - data.time


func is_within_hit_window(current_time: float, window_ms: float = 135.0) -> bool:
    if data == null:
        return false
    var time_diff = abs(current_time - data.time)
    return time_diff <= window_ms / 1000.0


func get_judgment(current_time: float) -> ScoreManagerClass.Judgment:
    if data == null:
        return ScoreManagerClass.Judgment.MISS

    var offset_ms = abs(current_time - data.time) * 1000

    if offset_ms <= 45:
        return ScoreManagerClass.Judgment.PERFECT
    elif offset_ms <= 90:
        return ScoreManagerClass.Judgment.GREAT
    elif offset_ms <= 135:
        return ScoreManagerClass.Judgment.GOOD
    else:
        return ScoreManagerClass.Judgment.MISS


func _draw() -> void:
    if data == null:
        return

    var size = get_note_size()

    # 绘制音符主体
    draw_rect(Rect2(-size.x / 2.0, 0, size.x, size.y), note_color)

    # 绘制长按尾部
    if data.type == GameData.NoteType.HOLD and data.duration > 0:
        var hold_length = data.duration * note_speed
        var tail_color = Color(note_color.r, note_color.g, note_color.b, 0.5)
        draw_rect(Rect2(-size.x / 2.0, size.y, size.x, hold_length), tail_color)
