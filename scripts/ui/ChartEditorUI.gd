class_name ChartEditorUIClass
extends Control

## 谱面编辑器 UI 控制器

const GameData = preload("res://scripts/data/GameData.gd")
const GameManagerClass = preload("res://scripts/autoload/GameManager.gd")

# 节点引用
var _song_name_label: Label
var _bpm_label: Label
var _notes_container: Node2D
var _track_container: Control
var _timeline_slider: VSlider
var _time_label: Label
var _tap_tool: Button
var _hold_tool: Button
var _swipe_tool: Button
var _erase_tool: Button
var _undo_button: Button
var _redo_button: Button
var _clear_button: Button
var _play_button: Button
var _stop_button: Button
var _save_button: Button
var _export_button: Button
var _back_button: Button

# 谱面数据
var _current_chart: GameData.ChartData
var _audio_path: String = ""
var _current_tool: GameData.NoteType = GameData.NoteType.TAP
var _erase_mode: bool = false
var _is_playing: bool = false
var _current_time: float = 0.0
var _duration: float = 180.0

# 音符存储
var _notes: Array = []
var _undo_stack: Array[Array] = []
var _redo_stack: Array[Array] = []

# 网格设置
var _pixels_per_second: float = 100.0
var _track_width: float = 130.0


func _ready() -> void:
    get_node_references()
    connect_signals()
    create_new_chart()


func get_node_references() -> void:
    _song_name_label = get_node_or_null("Header/SongName") as Label
    _bpm_label = get_node_or_null("Header/BPMLabel") as Label
    _notes_container = get_node_or_null("TrackContainer/NotesContainer") as Node2D
    _track_container = get_node_or_null("TrackContainer") as Control
    _timeline_slider = get_node_or_null("TimelineContainer/TimelineSlider") as VSlider
    _time_label = get_node_or_null("TimelineContainer/TimeLabel") as Label
    _tap_tool = get_node_or_null("ToolContainer/ToolVBox/TapTool") as Button
    _hold_tool = get_node_or_null("ToolContainer/ToolVBox/HoldTool") as Button
    _swipe_tool = get_node_or_null("ToolContainer/ToolVBox/SwipeTool") as Button
    _erase_tool = get_node_or_null("ToolContainer/ToolVBox/EraseTool") as Button
    _undo_button = get_node_or_null("ToolContainer/ToolVBox/UndoButton") as Button
    _redo_button = get_node_or_null("ToolContainer/ToolVBox/RedoButton") as Button
    _clear_button = get_node_or_null("ToolContainer/ToolVBox/ClearButton") as Button
    _play_button = get_node_or_null("BottomBar/PlayButton") as Button
    _stop_button = get_node_or_null("BottomBar/StopButton") as Button
    _save_button = get_node_or_null("BottomBar/SaveButton") as Button
    _export_button = get_node_or_null("BottomBar/ExportButton") as Button
    _back_button = get_node_or_null("Header/BackButton") as Button


func connect_signals() -> void:
    # 工具按钮
    if _tap_tool:
        _tap_tool.pressed.connect(func(): select_tool(GameData.NoteType.TAP, false))
    if _hold_tool:
        _hold_tool.pressed.connect(func(): select_tool(GameData.NoteType.HOLD, false))
    if _swipe_tool:
        _swipe_tool.pressed.connect(func(): select_tool(GameData.NoteType.SWIPE, false))
    if _erase_tool:
        _erase_tool.pressed.connect(func(): select_tool(GameData.NoteType.TAP, true))

    # 操作按钮
    if _undo_button:
        _undo_button.pressed.connect(on_undo_pressed)
    if _redo_button:
        _redo_button.pressed.connect(on_redo_pressed)
    if _clear_button:
        _clear_button.pressed.connect(on_clear_pressed)

    var load_audio_btn = get_node_or_null("ToolContainer/ToolVBox/LoadAudioButton") as Button
    if load_audio_btn:
        load_audio_btn.pressed.connect(on_load_audio_pressed)

    if _play_button:
        _play_button.pressed.connect(on_play_pressed)
    if _stop_button:
        _stop_button.pressed.connect(on_stop_pressed)
    if _save_button:
        _save_button.pressed.connect(on_save_pressed)
    if _export_button:
        _export_button.pressed.connect(on_export_pressed)
    if _back_button:
        _back_button.pressed.connect(on_back_pressed)

    # 时间轴滑块
    if _timeline_slider:
        _timeline_slider.value_changed.connect(on_timeline_changed)


func create_new_chart() -> void:
    _current_chart = GameData.ChartData.new()
    _current_chart.id = str(Time.get_ticks_usec())
    _current_chart.difficulty = GameManagerClass.Difficulty.NORMAL
    _current_chart.track_count = 4
    _current_chart.bpm = 120.0

    _duration = 180.0
    update_ui()
    draw_grid_lines()


func update_ui() -> void:
    if _song_name_label:
        _song_name_label.text = "New Chart" if _audio_path.is_empty() else _audio_path.get_file().get_basename()

    if _bpm_label and _current_chart:
        _bpm_label.text = "BPM: %.0f" % _current_chart.bpm

    update_time_label()


func update_time_label() -> void:
    if _time_label:
        var minutes = int(_current_time / 60)
        var seconds = int(_current_time) % 60
        _time_label.text = "%d:%02d" % [minutes, seconds]


func select_tool(tool: GameData.NoteType, erase_mode: bool) -> void:
    _current_tool = tool
    _erase_mode = erase_mode

    # 更新按钮视觉
    if _tap_tool:
        _tap_tool.modulate = Color(0.4, 1.0, 1.0) if (not erase_mode and tool == GameData.NoteType.TAP) else Color.WHITE
    if _hold_tool:
        _hold_tool.modulate = Color(0.4, 1.0, 1.0) if (not erase_mode and tool == GameData.NoteType.HOLD) else Color.WHITE
    if _swipe_tool:
        _swipe_tool.modulate = Color(0.4, 1.0, 1.0) if (not erase_mode and tool == GameData.NoteType.SWIPE) else Color.WHITE
    if _erase_tool:
        _erase_tool.modulate = Color(1.0, 0.3, 0.3) if erase_mode else Color.WHITE


func _input(event: InputEvent) -> void:
    if event is InputEventMouseButton and event.pressed:
        if _track_container == null:
            return

        var local_pos = _track_container.get_local_mouse_position()
        var track_rect = Rect2(Vector2.ZERO, _track_container.size)
        if track_rect.has_point(local_pos):
            var track = int(local_pos.x / _track_width)
            if track >= 0 and track < 4:
                if _erase_mode:
                    erase_note_at(_current_time, track)
                else:
                    add_note(_current_time, track)


func add_note(time: float, track: int) -> void:
    save_undo_state()

    var note = GameData.NoteData.new()
    note.time = time
    note.track = track
    note.type = _current_tool

    if _current_tool == GameData.NoteType.HOLD:
        note.end_time = time + 0.5
    elif _current_tool == GameData.NoteType.SWIPE:
        note.swipe_direction = randi_range(1, 4)

    _notes.append(note)
    _notes.sort_custom(func(a, b): return a.time < b.time)

    refresh_note_display()
    print("Added %s note at %.2fs on track %d" % [NoteTypeToString(_current_tool), time, track])


func erase_note_at(time: float, track: int) -> void:
    var tolerance = 0.1

    for i in range(_notes.size() - 1, -1, -1):
        var note = _notes[i]
        if note.track == track and abs(note.time - time) < tolerance:
            save_undo_state()
            _notes.remove_at(i)
            refresh_note_display()
            print("Erased note at %.2fs on track %d" % [time, track])
            return


func save_undo_state() -> void:
    var state = []
    for note in _notes:
        var note_copy = GameData.NoteData.new()
        note_copy.time = note.time
        note_copy.end_time = note.end_time
        note_copy.track = note.track
        note_copy.type = note.type
        note_copy.swipe_direction = note.swipe_direction
        state.append(note_copy)
    _undo_stack.append(state)
    _redo_stack.clear()


func refresh_note_display() -> void:
    if _notes_container == null:
        return

    # 清除现有音符
    for child in _notes_container.get_children():
        child.queue_free()

    # 绘制音符
    for note in _notes:
        draw_note(note)


func draw_note(note: GameData.NoteData) -> void:
    var note_rect = ColorRect.new()

    var x = note.track * _track_width + 10
    var y = note.time * _pixels_per_second
    var width = _track_width - 20
    var height = 20.0

    if note.type == GameData.NoteType.HOLD:
        height = note.duration * _pixels_per_second

    note_rect.position = Vector2(x, y)
    note_rect.size = Vector2(width, height)

    # 根据类型设置颜色
    match note.type:
        GameData.NoteType.HOLD:
            note_rect.color = Color(1.0, 0.8, 0.2)
        GameData.NoteType.SWIPE:
            note_rect.color = Color(0.8, 0.4, 1.0)
        _:
            note_rect.color = Color(0.4, 1.0, 1.0)

    _notes_container.add_child(note_rect)


func draw_grid_lines() -> void:
    var grid_node = get_node_or_null("TrackContainer/GridLines") as Node2D
    if grid_node == null:
        return

    # 清除现有线条
    for child in grid_node.get_children():
        child.queue_free()

    # 绘制节拍线
    if _current_chart == null:
        return

    var beat_interval = 60.0 / _current_chart.bpm
    var beat_count = int(_duration / beat_interval)

    for i in range(beat_count + 1):
        var time = i * beat_interval
        var y = time * _pixels_per_second

        var line = Line2D.new()
        line.add_point(Vector2(0, y))
        line.add_point(Vector2(520, y))
        line.default_color = Color(0.3, 0.3, 0.3, 0.5 if i % 4 == 0 else 0.2)
        line.width = 2.0 if i % 4 == 0 else 1.0

        grid_node.add_child(line)


func on_undo_pressed() -> void:
    if _undo_stack.size() == 0:
        return

    var state = _undo_stack.pop_back()
    _redo_stack.append(_notes.duplicate())
    _notes.clear()
    _notes.append_array(state)
    refresh_note_display()


func on_redo_pressed() -> void:
    if _redo_stack.size() == 0:
        return

    var state = _redo_stack.pop_back()
    _undo_stack.append(_notes.duplicate())
    _notes.clear()
    _notes.append_array(state)
    refresh_note_display()


func on_clear_pressed() -> void:
    if _notes.size() == 0:
        return

    save_undo_state()
    _notes.clear()
    refresh_note_display()


func on_load_audio_pressed() -> void:
    var dialog = FileDialog.new()
    dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
    dialog.access = FileDialog.ACCESS_FILESYSTEM
    dialog.filters = PackedStringArray(["*.wav", "*.ogg"])
    dialog.title = "Load Audio File"

    add_child(dialog)
    dialog.file_selected.connect(func(path): load_audio_file(path); dialog.queue_free())
    dialog.canceled.connect(func(): dialog.queue_free())

    dialog.show()


func on_play_pressed() -> void:
    _is_playing = true
    # 如果已加载音频则播放
    if not _audio_path.is_empty():
        if AudioManager:
            AudioManager.load_song_from_path(_audio_path, _current_chart.bpm if _current_chart else 120.0, 0)
            AudioManager.seek(_current_time)
            AudioManager.play()


func on_stop_pressed() -> void:
    _is_playing = false
    _current_time = 0.0
    if AudioManager:
        AudioManager.stop()
    update_time_label()
    refresh_note_display()


func on_save_pressed() -> void:
    if _current_chart == null:
        return

    _current_chart.notes = _notes.duplicate()

    # 保存到用户目录
    DirAccess.make_dir_recursive_absolute("user://charts")
    var path = "user://charts/" + _current_chart.id + ".json"

    var json_data = _current_chart.to_dict()
    var json = JSON.stringify(json_data)
    var file = FileAccess.open(path, FileAccess.WRITE)
    if file:
        file.store_string(json)
        print("Chart saved to: " + path)

    # 添加到玩家数据
    var player_data = GameData.PlayerData.load_data()
    if not _current_chart.id in player_data.created_charts:
        player_data.created_charts.append(_current_chart.id)
        player_data.save()

        if AchievementManager:
            AchievementManager.on_chart_created()


func on_export_pressed() -> void:
    if _current_chart == null or _notes.size() == 0:
        print("No notes to export!")
        return

    var export_path = "user://exports/" + _current_chart.id + "_export.json"
    DirAccess.make_dir_recursive_absolute("user://exports")

    var notes_data = []
    for note in _notes:
        notes_data.append({
            "time": note.time,
            "end_time": note.end_time,
            "track": note.track,
            "type": NoteTypeToString(note.type),
            "swipe_direction": SwipeDirectionToString(note.swipe_direction)
        })

    var export_data = {
        "chart_id": _current_chart.id,
        "bpm": _current_chart.bpm,
        "difficulty": GameManagerClass.DIFFICULTY_NAMES[_current_chart.difficulty],
        "notes": notes_data
    }

    var json = JSON.stringify(export_data)
    var file = FileAccess.open(export_path, FileAccess.WRITE)
    if file:
        file.store_string(json)
        print("Chart exported to: " + export_path)


func on_back_pressed() -> void:
    if AudioManager:
        AudioManager.stop()
    if GameManager:
        GameManager.return_to_main_menu()


func on_timeline_changed(value: float) -> void:
    _current_time = value / 100.0 * _duration
    update_time_label()
    update_notes_position()


func update_notes_position() -> void:
    if _notes_container == null:
        return
    _notes_container.position = Vector2(0, -_current_time * _pixels_per_second)


func _process(delta: float) -> void:
    if not _is_playing:
        return

    _current_time += delta

    if _current_time >= _duration:
        _current_time = _duration
        _is_playing = false

    update_time_label()
    update_notes_position()

    if _timeline_slider and not _timeline_slider.has_focus():
        _timeline_slider.value = _current_time / _duration * 100.0


func load_audio_file(path: String) -> void:
    _audio_path = path

    # 分析 BPM
    _ = load_audio_async(path)


func load_audio_async(path: String) -> void:
    var bpm = 120.0
    if AudioAnalysis:
        bpm = await AudioAnalysis.analyze_bpm_from_file_async(path)

    if _current_chart:
        _current_chart.bpm = bpm

    # 获取时长
    var ext = path.get_extension().to_lower()
    if ext == "wav":
        var stream = ResourceLoader.load(path) as AudioStream
        if stream:
            _duration = stream.get_length()
    elif ext == "ogg":
        var stream = ResourceLoader.load(path) as AudioStream
        if stream:
            _duration = stream.get_length()

    update_ui()
    draw_grid_lines()
    print("Loaded audio: BPM=%.0f, Duration=%.1fs" % [bpm, _duration])


func load_chart(chart: GameData.ChartData) -> void:
    _current_chart = chart
    _notes.clear()
    _notes.append_array(chart.notes)
    refresh_note_display()
    update_ui()


func NoteTypeToString(type: GameData.NoteType) -> String:
    match type:
        GameData.NoteType.HOLD:
            return "Hold"
        GameData.NoteType.SWIPE:
            return "Swipe"
        _:
            return "Tap"


func SwipeDirectionToString(dir: GameData.SwipeDirection) -> String:
    match dir:
        GameData.SwipeDirection.LEFT:
            return "Left"
        GameData.SwipeDirection.RIGHT:
            return "Right"
        GameData.SwipeDirection.UP:
            return "Up"
        GameData.SwipeDirection.DOWN:
            return "Down"
        _:
            return "None"
