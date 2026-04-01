class_name GameplayUIClass
extends Control

## 游戏播放 UI 控制器
## 单例 autoload

const GameData = preload("res://scripts/data/GameData.gd")
const GameManagerClass = preload("res://scripts/autoload/GameManager.gd")

# Node 引用
var _notes_container: Node2D
var _track_container: Control
var _score_value_label: Label
var _combo_value_label: Label
var _judgment_label: Label
var _song_name_label: Label
var _difficulty_label: Label
var _progress_bar: ProgressBar
var _pause_button: Button
var _pause_menu: PanelContainer

# 轨道视觉
var _track_lanes: Array[Control] = []

# 当前歌曲数据
var _current_song: GameData.SongData
var _current_chart: GameData.ChartData
var _difficulty: int = GameManagerClass.Difficulty.NORMAL

# 音符管理
var _active_notes: Array[NoteObjectClass] = []
var _note_queue: Array[GameData.NoteData] = []
var _current_time: float = 0.0

# 音符设置
var _note_speed: float = 400.0
var _fall_distance: float = 800.0
var _hit_line_y: float = 400.0
var _spawn_y: float = -400.0
var _despawn_y: float = 500.0

# 判定窗口 (秒)
var _perfect_window: float = 0.045
var _great_window: float = 0.090
var _good_window: float = 0.135

# 游戏状态
var _is_playing: bool = false
var _is_paused: bool = false
var _game_started: bool = false

# 击中音效
var _hit_sound: AudioStream

static var instance: GameplayUIClass


func _ready() -> void:
    instance = self

    get_node_references()
    setup_track_lanes()
    connect_signals()
    load_current_song()
    create_hit_sound()

    # 延迟开始游戏
    var timer = get_tree().create_timer(1.5)
    timer.timeout.connect(begin_playback)


func get_node_references() -> void:
    _notes_container = get_node_or_null("TrackContainer/NotesContainer") as Node2D
    _track_container = get_node_or_null("TrackContainer") as Control
    _score_value_label = get_node_or_null("ScoreContainer/ScoreValue") as Label
    _combo_value_label = get_node_or_null("ComboContainer/ComboValue") as Label
    _judgment_label = get_node_or_null("JudgmentLabel") as Label
    _song_name_label = get_node_or_null("TopBar/SongName") as Label
    _difficulty_label = get_node_or_null("TopBar/Difficulty") as Label
    _progress_bar = get_node_or_null("ProgressBar") as ProgressBar
    _pause_button = get_node_or_null("PauseButton") as Button
    _pause_menu = get_node_or_null("PauseMenu") as PanelContainer

    # 隐藏判定标签
    if _judgment_label:
        _judgment_label.visible = false

    # 隐藏暂停菜单
    if _pause_menu:
        _pause_menu.visible = false


func setup_track_lanes() -> void:
    var track_count = 4
    if _current_chart:
        track_count = _current_chart.track_count

    _track_lanes.resize(track_count)

    var track_width = 720.0
    if _track_container:
        track_width = _track_container.size.x

    var lane_width = track_width / track_count

    # 获取轨道视觉
    for i in range(track_count):
        var lane_path = "TrackContainer/Lane" + str(i)
        var lane = get_node_or_null(lane_path) as Control

        if lane == null and _track_container:
            # 动态创建轨道
            var color_rect = ColorRect.new()
            color_rect.name = "Lane" + str(i)
            color_rect.size = Vector2(lane_width, 800)
            color_rect.position = Vector2(i * lane_width, 0)
            var lane_color = Color.WHITE
            if TrackManager:
                lane_color = Color(TrackManager.get_track_color(i), 0.1)
            color_rect.color = lane_color
            _track_container.add_child(color_rect)
            lane = color_rect

        _track_lanes[i] = lane


func connect_signals() -> void:
    # 暂停按钮
    if _pause_button:
        _pause_button.pressed.connect(on_pause_pressed)

    # 暂停菜单按钮
    var resume_button = get_node_or_null("PauseMenu/PauseVBox/ResumeButton") as Button
    var restart_button = get_node_or_null("PauseMenu/PauseVBox/RestartButton") as Button
    var quit_button = get_node_or_null("PauseMenu/PauseVBox/QuitButton") as Button

    if resume_button:
        resume_button.pressed.connect(on_resume_pressed)
    if restart_button:
        restart_button.pressed.connect(on_restart_pressed)
    if quit_button:
        quit_button.pressed.connect(on_quit_pressed)

    # 订阅分数事件
    if ScoreManager:
        ScoreManager.on_judgment.connect(on_judgment)
        ScoreManager.on_score_changed.connect(on_score_changed)
        ScoreManager.on_combo_changed.connect(on_combo_changed)

    # 订阅音频事件
    if AudioManager:
        AudioManager.on_song_end.connect(on_song_end)


func create_hit_sound() -> void:
    # 击中音效可以在这里创建或加载
    pass


func load_current_song() -> void:
    if GameManager:
        _current_song = GameManager.current_song
        _current_chart = GameManager.current_chart
        _difficulty = GameManager.current_difficulty

    if _current_song == null or _current_chart == null:
        push_error("No song or chart loaded!")
        return_to_menu()
        return

    # 队列音符
    var sorted_notes: Array[GameData.NoteData] = _current_chart.notes.duplicate()
    sorted_notes.sort_custom(func(a, b): return a.time < b.time)

    _note_queue.clear()
    for note in sorted_notes:
        _note_queue.append(note)

    # 更新 UI
    if _song_name_label:
        _song_name_label.text = _current_song.name

    if _difficulty_label:
        _difficulty_label.text = GameManagerClass.DIFFICULTY_NAMES[_difficulty]
        _difficulty_label.modulate = GameManagerClass.DIFFICULTY_COLORS[_difficulty]

    # 初始化分数管理器
    if ScoreManager:
        ScoreManager.init(_current_chart.notes.size())

    # 加载音频
    if not _current_song.audio_path.is_empty():
        if AudioManager:
            AudioManager.load_song_from_path(_current_song.audio_path, _current_chart.bpm, _current_chart.offset)

    # 应用音符速度
    if GameManager:
        _note_speed = GameManager.note_speed * 400.0


func begin_playback() -> void:
    _game_started = true
    _is_playing = true
    _is_paused = false

    if AudioManager:
        AudioManager.play()


func _process(_delta: float) -> void:
    if not _game_started:
        return

    if not _is_playing or _is_paused:
        return

    if AudioManager:
        _current_time = AudioManager.current_time

    # 生成新音符
    spawn_notes()

    # 更新活动音符
    update_notes(_delta)

    # 检查错过的音符
    check_missed_notes()

    # 更新进度条
    update_progress()

    # 更新长按音符
    if TrackManager:
        TrackManager.update_hold_notes()


func spawn_notes() -> void:
    var spawn_time = _current_time + (_fall_distance / _note_speed)

    while _note_queue.size() > 0:
        var note_data = _note_queue[0]

        if note_data.time <= spawn_time:
            _note_queue.pop_front()
            spawn_note(note_data)
        else:
            break


func spawn_note(note_data: GameData.NoteData) -> void:
    var note = NoteObjectClass.new()
    note.data = note_data
    note.track_width = _track_container.size.x / _current_chart.track_count if _track_container else 180.0
    note.set_speed(_note_speed)
    note.initialize(note_data, _hit_line_y, _despawn_y)
    note.set_spawn_position(_spawn_y)

    # 计算初始位置
    var x = calculate_track_x(note_data.track)
    var time_diff = note_data.time - _current_time
    var y = _hit_line_y - (time_diff * _note_speed)

    note.position = Vector2(x, y)

    if _notes_container:
        _notes_container.add_child(note)
    _active_notes.append(note)


func calculate_track_x(track: int) -> float:
    var track_width = 720.0
    var track_count = 4
    if _track_container:
        track_width = _track_container.size.x
    if _current_chart:
        track_count = _current_chart.track_count

    var lane_width = track_width / track_count
    return track * lane_width + lane_width / 2.0


func update_notes(_delta: float) -> void:
    for note in _active_notes:
        if note == null or note.was_hit or note.was_missed:
            continue
        note.update_position(_current_time)


func check_missed_notes() -> void:
    var miss_time = _current_time - _good_window

    var missed_notes = []
    for note in _active_notes:
        if note == null or note.was_hit or note.was_missed:
            continue
        if note.data.time < miss_time:
            missed_notes.append(note)

    for note in missed_notes:
        handle_miss(note)


func update_progress() -> void:
    if _progress_bar == null:
        return

    var duration = 1.0
    if AudioManager:
        duration = AudioManager.song_duration

    if duration > 0:
        _progress_bar.value = (_current_time / duration) * 100.0


# ============================================================
# 输入处理
# ============================================================
func handle_track_input(track: int) -> void:
    if not _is_playing or _is_paused:
        return

    # 寻找最近的可击中的音符
    var closest_note: NoteObjectClass = null
    var min_time_diff = 1000000.0

    for note in _active_notes:
        if note == null or note.was_hit or note.was_missed:
            continue
        if note.data.track != track:
            continue

        var time_diff = abs(note.data.time - _current_time)

        if time_diff < min_time_diff and time_diff <= _good_window:
            min_time_diff = time_diff
            closest_note = note

    if closest_note:
        hit_note(closest_note)


func handle_hold_release(track: int) -> void:
    if not TrackManager:
        return

    var lane = TrackManager.get_lane(track)
    if lane.has("hold_note") and lane["hold_note"]:
        var note = lane["hold_note"]
        var current_time = 0.0
        if AudioManager:
            current_time = AudioManager.current_time

        # 检查按住时间是否足够
        var hold_progress = (current_time - note.data.time) / note.data.duration

        if hold_progress < 0.8:
            # 早期释放 - 算作miss
            note.release_hold()
            if ScoreManager:
                ScoreManager.process_miss()
            remove_note(note)


func hit_note(note: NoteObjectClass) -> void:
    if note == null or note.was_hit:
        return

    var judgment = ScoreManagerClass.Judgment.MISS
    if ScoreManager:
        judgment = ScoreManager.judge_hit(_current_time, note.data.time)

    # 播放击中音效
    if _hit_sound:
        if AudioManager:
            AudioManager.play_hit_sound(_hit_sound)

    # 处理长按音符
    if note.data.type == GameData.NoteType.HOLD:
        note.start_hold()
        if TrackManager:
            TrackManager.set_holding_note(note.data.track, note)
    else:
        note.mark_hit()
        remove_note(note)

    # 显示击中效果
    if TrackManager:
        TrackManager.show_hit_effect(note.data.track, judgment)


func handle_miss(note: NoteObjectClass) -> void:
    if note == null or note.was_missed:
        return

    note.mark_missed()
    if ScoreManager:
        ScoreManager.process_miss()

    if TrackManager:
        TrackManager.show_miss_effect(note.data.track)

    remove_note(note)


func remove_note(note: NoteObjectClass) -> void:
    var idx = _active_notes.find(note)
    if idx >= 0:
        _active_notes.remove_at(idx)
    # 音符会通过动画自动释放


# ============================================================
# 视觉效果
# ============================================================
func show_hit_effect(track: int, judgment: ScoreManagerClass.Judgment, x: float) -> void:
    var position = Vector2(x, _hit_line_y)
    if EffectManager:
        EffectManager.play_hit_effect(judgment, position, _notes_container)

        # 闪烁轨道
        if _track_lanes.size() > track and _track_lanes[track]:
            EffectManager.flash_lane(_track_lanes[track], judgment)

        # 连击效果
        var combo = 0
        if ScoreManager:
            combo = ScoreManager.combo
        if combo >= 50:
            EffectManager.play_combo_effect(combo, position, _notes_container)


func show_miss_effect(track: int, x: float) -> void:
    var position = Vector2(x, _hit_line_y)
    if EffectManager:
        EffectManager.play_hit_effect(ScoreManagerClass.Judgment.MISS, position, _notes_container)


func highlight_track(track: int, highlight: bool, color: Color) -> void:
    if _track_lanes.size() <= track or _track_lanes[track] == null:
        return

    var lane = _track_lanes[track]
    if lane == null:
        return

    if highlight:
        if EffectManager:
            EffectManager.glow_lane(lane, color, 0.3)
    else:
        if EffectManager:
            EffectManager.reset_lane_glow(lane)


# ============================================================
# 分数事件
# ============================================================
func on_judgment(judgment: ScoreManagerClass.Judgment, combo: int) -> void:
    if _judgment_label == null:
        return

    _judgment_label.text = JudgmentToString(judgment).to_upper()
    if EffectManager:
        _judgment_label.modulate = EffectManager.get_judgment_color(judgment)
    _judgment_label.visible = true

    # 缩放动画
    var tween = _judgment_label.create_tween()
    tween.tween_property(_judgment_label, "scale", Vector2(1.3, 1.3), 0.05)
    tween.tween_property(_judgment_label, "scale", Vector2(1.0, 1.0), 0.15)

    # 延迟隐藏
    var timer = get_tree().create_timer(0.5)
    timer.timeout.connect(func(): _judgment_label.visible = false)


func JudgmentToString(judgment: ScoreManagerClass.Judgment) -> String:
    match judgment:
        ScoreManagerClass.Judgment.PERFECT:
            return "Perfect"
        ScoreManagerClass.Judgment.GREAT:
            return "Great"
        ScoreManagerClass.Judgment.GOOD:
            return "Good"
        ScoreManagerClass.Judgment.MISS:
            return "Miss"
    return ""


func on_score_changed(score: int) -> void:
    if _score_value_label:
        _score_value_label.text = "%d" % score


func on_combo_changed(combo: int) -> void:
    if _combo_value_label == null:
        return

    _combo_value_label.text = str(combo) if combo > 0 else ""

    if combo >= 100:
        # 大连击动画
        var tween = _combo_value_label.create_tween()
        tween.tween_property(_combo_value_label, "scale", Vector2(1.5, 1.5), 0.1)
        tween.tween_property(_combo_value_label, "scale", Vector2(1.0, 1.0), 0.2)


# ============================================================
# 暂停控制
# ============================================================
func _input(event: InputEvent) -> void:
    if event is InputEventKey:
        if event.is_action_pressed("pause") or event.keycode == KEY_ESCAPE:
            if _is_paused:
                on_resume_pressed()
            else:
                on_pause_pressed()


func on_pause_pressed() -> void:
    if not _is_playing:
        return

    _is_paused = true
    if AudioManager:
        AudioManager.pause()

    if _pause_menu:
        _pause_menu.visible = true

    get_tree().paused = true


func on_resume_pressed() -> void:
    _is_paused = false
    if AudioManager:
        AudioManager.resume()

    if _pause_menu:
        _pause_menu.visible = false

    get_tree().paused = false


func on_restart_pressed() -> void:
    get_tree().paused = false
    get_tree().reload_current_scene()


func on_quit_pressed() -> void:
    get_tree().paused = false
    if GameManager:
        GameManager.return_to_main_menu()


# ============================================================
# 歌曲结束
# ============================================================
func on_song_end() -> void:
    _is_playing = false

    # 获取结果
    var result: GameData.ScoreResult = null
    if ScoreManager:
        result = ScoreManager.get_result()

    if result and _current_song:
        # 保存结果
        var player_data = GameData.PlayerData.load_data()
        player_data.update_record(_current_song.id, result.score, result.max_combo, result.grade, _difficulty)
        player_data.total_play_count += 1
        player_data.save()

        # 检查成就
        check_achievements(result)

    # 进入结算界面
    if GameManager:
        GameManager.change_state(GameManagerClass.GameState.RESULTS)


func check_achievements(result: GameData.ScoreResult) -> void:
    if AchievementManager == null:
        return

    if result.is_full_combo:
        AchievementManager.unlock_achievement("first_full_combo")

    if result.is_all_perfect:
        AchievementManager.unlock_achievement("first_all_perfect")

    if result.grade == "S+" or result.grade == "S":
        AchievementManager.unlock_achievement("first_s_rank")

    if result.max_combo >= 100:
        AchievementManager.update_progress("combo_100", result.max_combo)
    if result.max_combo >= 500:
        AchievementManager.update_progress("combo_500", result.max_combo)
    if result.max_combo >= 1000:
        AchievementManager.update_progress("combo_1000", result.max_combo)


func return_to_menu() -> void:
    if GameManager:
        GameManager.return_to_main_menu()


func _exit_tree() -> void:
    # 取消订阅事件
    if ScoreManager:
        ScoreManager.on_judgment.disconnect(on_judgment)
        ScoreManager.on_score_changed.disconnect(on_score_changed)
        ScoreManager.on_combo_changed.disconnect(on_combo_changed)

    if AudioManager:
        AudioManager.on_song_end.disconnect(on_song_end)

    # 清理音符
    for note in _active_notes:
        if note and is_instance_valid(note):
            note.queue_free()
    _active_notes.clear()
    _note_queue.clear()
