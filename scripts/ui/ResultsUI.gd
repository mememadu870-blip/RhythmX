class_name ResultsUIClass
extends Control

## 结算界面 UI 控制器

const GameData = preload("res://scripts/data/GameData.gd")
const GameManagerClass = preload("res://scripts/autoload/GameManager.gd")

var _grade_label: Label
var _score_value_label: Label
var _max_combo_value_label: Label
var _perfect_value_label: Label
var _great_value_label: Label
var _good_value_label: Label
var _miss_value_label: Label
var _accuracy_value_label: Label
var _full_combo_badge: Label
var _all_perfect_badge: Label
var _new_record_badge: Label
var _retry_button: Button
var _next_button: Button
var _back_button: Button
var _song_name_label: Label
var _artist_label: Label

var _result: GameData.ScoreResult
var _song: GameData.SongData
var _difficulty: int = GameManagerClass.Difficulty.NORMAL


func _ready() -> void:
    # 获取节点引用
    _grade_label = get_node_or_null("VBoxContainer/GradeContainer/GradeLabel") as Label
    _score_value_label = get_node_or_null("VBoxContainer/ScoreContainer/ScoreValue") as Label
    _max_combo_value_label = get_node_or_null("VBoxContainer/StatsContainer/MaxComboValue") as Label
    _perfect_value_label = get_node_or_null("VBoxContainer/StatsContainer/PerfectValue") as Label
    _great_value_label = get_node_or_null("VBoxContainer/StatsContainer/GreatValue") as Label
    _good_value_label = get_node_or_null("VBoxContainer/StatsContainer/GoodValue") as Label
    _miss_value_label = get_node_or_null("VBoxContainer/StatsContainer/MissValue") as Label
    _accuracy_value_label = get_node_or_null("VBoxContainer/StatsContainer/AccuracyValue") as Label
    _full_combo_badge = get_node_or_null("VBoxContainer/BadgesContainer/FullComboBadge") as Label
    _all_perfect_badge = get_node_or_null("VBoxContainer/BadgesContainer/AllPerfectBadge") as Label
    _new_record_badge = get_node_or_null("VBoxContainer/GradeContainer/NewRecordBadge") as Label
    _retry_button = get_node_or_null("VBoxContainer/ButtonContainer/RetryButton") as Button
    _next_button = get_node_or_null("VBoxContainer/ButtonContainer/NextButton") as Button
    _back_button = get_node_or_null("VBoxContainer/ButtonContainer/BackButton") as Button
    _song_name_label = get_node_or_null("SongInfo/SongName") as Label
    _artist_label = get_node_or_null("SongInfo/Artist") as Label

    # 连接信号
    if _retry_button:
        _retry_button.pressed.connect(on_retry_pressed)
    if _next_button:
        _next_button.pressed.connect(on_next_pressed)
    if _back_button:
        _back_button.pressed.connect(on_back_pressed)

    load_results()
    play_result_animation()


func load_results() -> void:
    if ScoreManager:
        _result = ScoreManager.get_result()

    if GameManager:
        _song = GameManager.current_song
        _difficulty = GameManager.current_difficulty

    # 如果没有结果，创建模拟数据
    if _result == null:
        _result = GameData.ScoreResult.new()
        _result.score = 985420
        _result.max_combo = 256
        _result.perfect_count = 180
        _result.great_count = 30
        _result.good_count = 10
        _result.miss_count = 5
        _result.accuracy = 0.985
        _result.grade = "S"
        _result.is_full_combo = false
        _result.is_all_perfect = false


func play_result_animation() -> void:
    # 动画分数计数器
    animate_score()

    # 延迟显示等级
    var timer1 = get_tree().create_timer(0.5)
    timer1.timeout.connect(show_grade)

    # 延迟显示统计
    var timer2 = get_tree().create_timer(0.8)
    timer2.timeout.connect(show_stats)

    # 显示徽章
    var timer3 = get_tree().create_timer(1.2)
    timer3.timeout.connect(show_badges)


func animate_score() -> void:
    if _score_value_label == null:
        return

    var target_score = _result.score
    var tween = create_tween()
    tween.tween_method(set_score_display, 0, target_score, 1.5)


func set_score_display(score: int) -> void:
    if _score_value_label:
        _score_value_label.text = "%d" % score


func show_grade() -> void:
    if _grade_label == null:
        return

    _grade_label.text = _result.grade

    # 设置等级颜色
    var grade_color = Color.WHITE
    match _result.grade:
        "S+", "S":
            grade_color = Color(1.0, 0.85, 0.0)
        "A":
            grade_color = Color(0.3, 1.0, 0.5)
        "B":
            grade_color = Color(0.5, 0.8, 1.0)
        "C":
            grade_color = Color(0.8, 0.6, 0.4)
        _:
            grade_color = Color(0.6, 0.4, 0.4)

    _grade_label.modulate = grade_color


func show_stats() -> void:
    if _max_combo_value_label:
        _max_combo_value_label.text = str(_result.max_combo)

    if _perfect_value_label:
        _perfect_value_label.text = str(_result.perfect_count)

    if _great_value_label:
        _great_value_label.text = str(_result.great_count)

    if _good_value_label:
        _good_value_label.text = str(_result.good_count)

    if _miss_value_label:
        _miss_value_label.text = str(_result.miss_count)

    if _accuracy_value_label:
        _accuracy_value_label.text = "%.2f%%" % (_result.accuracy * 100)

    # 歌曲信息
    if _song:
        if _song_name_label:
            _song_name_label.text = _song.name
        if _artist_label:
            _artist_label.text = _song.artist


func show_badges() -> void:
    if _result.is_full_combo and _full_combo_badge:
        _full_combo_badge.visible = true

    if _result.is_all_perfect and _all_perfect_badge:
        _all_perfect_badge.visible = true

    # 检查新记录
    if _song:
        var player_data = GameData.PlayerData.load_data()
        var record = player_data.get_record(_song.id)
        if record == null or _result.score > record.high_score:
            if _new_record_badge:
                _new_record_badge.visible = true


func on_retry_pressed() -> void:
    if _song == null:
        return

    var chart = _song.get_chart(_difficulty)
    if chart and GameManager:
        GameManager.start_game(_song, chart, _difficulty)


func on_next_pressed() -> void:
    # 进入下一首歌
    var songs = SongLibrary.songs if SongLibrary else []
    if songs.size() == 0:
        if GameManager:
            GameManager.return_to_main_menu()
        return

    # 找到当前歌曲索引并获取下一首
    var current_index = -1
    for i in range(songs.size()):
        if songs[i].id == _song.id:
            current_index = i
            break

    var next_index = (current_index + 1) % songs.size()
    var next_song = songs[next_index]

    var chart = next_song.get_chart(_difficulty)
    if next_song and chart and GameManager:
        GameManager.start_game(next_song, chart, _difficulty)
    else:
        if GameManager:
            GameManager.change_state(GameManagerClass.GameState.SONG_SELECTION)


func on_back_pressed() -> void:
    if GameManager:
        GameManager.return_to_main_menu()
