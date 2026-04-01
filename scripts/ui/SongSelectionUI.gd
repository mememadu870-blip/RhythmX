class_name SongSelectionUIClass
extends Control

## 歌曲选择 UI 控制器

const GameData = preload("res://scripts/data/GameData.gd")
const GameManagerClass = preload("res://scripts/autoload/GameManager.gd")

var _song_list_container: VBoxContainer
var _song_name_label: Label
var _artist_label: Label
var _bpm_value_label: Label
var _duration_value_label: Label
var _high_score_value_label: Label
var _note_count_label: Label
var _difficulty_buttons: Array[Button] = []
var _play_button: Button
var _back_button: Button
var _import_button: Button

var _songs: Array[GameData.SongData] = []
var _selected_song: GameData.SongData
var _selected_difficulty: int = GameManagerClass.Difficulty.NORMAL


func _ready() -> void:
    # 获取节点引用
    _song_list_container = get_node_or_null("MainContainer/SongListContainer/SongList/SongListVBox") as VBoxContainer
    _song_name_label = get_node_or_null("MainContainer/DetailContainer/DetailVBox/SongInfo/SongName") as Label
    _artist_label = get_node_or_null("MainContainer/DetailContainer/DetailVBox/SongInfo/Artist") as Label
    _bpm_value_label = get_node_or_null("MainContainer/DetailContainer/DetailVBox/SongInfo/StatsGrid/BPMValue") as Label
    _duration_value_label = get_node_or_null("MainContainer/DetailContainer/DetailVBox/SongInfo/StatsGrid/DurationValue") as Label
    _high_score_value_label = get_node_or_null("MainContainer/DetailContainer/DetailVBox/SongInfo/StatsGrid/HighScoreValue") as Label
    _note_count_label = get_node_or_null("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/NoteCountLabel") as Label
    _play_button = get_node_or_null("BottomBar/PlayButton") as Button
    _back_button = get_node_or_null("Header/BackButton") as Button
    _import_button = get_node_or_null("BottomBar/ImportButton") as Button

    # 获取难度按钮
    _difficulty_buttons.resize(4)
    _difficulty_buttons[0] = get_node_or_null("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/EasyButton") as Button
    _difficulty_buttons[1] = get_node_or_null("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/NormalButton") as Button
    _difficulty_buttons[2] = get_node_or_null("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/HardButton") as Button
    _difficulty_buttons[3] = get_node_or_null("MainContainer/DetailContainer/DetailVBox/DifficultyContainer/DifficultyButtons/ExpertButton") as Button

    # 连接信号
    if _back_button:
        _back_button.pressed.connect(on_back_pressed)

    if _play_button:
        _play_button.pressed.connect(on_play_pressed)

    if _import_button:
        _import_button.pressed.connect(on_import_pressed)

    for i in range(4):
        var diff = i
        if _difficulty_buttons[i]:
            _difficulty_buttons[i].pressed.connect(func(): on_difficulty_selected(diff))

    load_songs()


func load_songs() -> void:
    if SongLibrary:
        _songs = SongLibrary.songs

    # 填充歌曲列表
    if _song_list_container:
        # 清除现有项目
        for child in _song_list_container.get_children():
            child.queue_free()

        for song in _songs:
            var button = Button.new()
            button.text = song.name + "\n" + song.artist
            button.custom_minimum_size = Vector2(280, 60)
            button.pressed.connect(func(): select_song(song))
            _song_list_container.add_child(button)

    # 选择第一首歌
    if _songs.size() > 0:
        select_song(_songs[0])


func select_song(song: GameData.SongData) -> void:
    _selected_song = song
    update_song_details()
    update_difficulty_buttons()


func update_song_details() -> void:
    if _selected_song == null:
        return

    if _song_name_label:
        _song_name_label.text = _selected_song.name

    if _artist_label:
        _artist_label.text = _selected_song.artist

    if _bpm_value_label:
        _bpm_value_label.text = "%.0f" % _selected_song.bpm

    if _duration_value_label:
        var minutes = int(_selected_song.duration / 60)
        var seconds = int(_selected_song.duration) % 60
        _duration_value_label.text = "%d:%02d" % [minutes, seconds]

    if _high_score_value_label:
        if _selected_song.high_score > 0:
            _high_score_value_label.text = "%d" % _selected_song.high_score
        else:
            _high_score_value_label.text = "---"


func update_difficulty_buttons() -> void:
    if _selected_song == null:
        return

    for i in range(4):
        var diff = i
        var has_chart = _selected_song.has_chart(diff)

        if _difficulty_buttons[i]:
            _difficulty_buttons[i].disabled = not has_chart

    # 选择默认难度
    if _selected_song.has_chart(GameManagerClass.Difficulty.NORMAL):
        on_difficulty_selected(GameManagerClass.Difficulty.NORMAL)
    else:
        for i in range(4):
            if _selected_song.has_chart(i):
                on_difficulty_selected(i)
                break


func on_difficulty_selected(difficulty: int) -> void:
    _selected_difficulty = difficulty

    # 更新按钮颜色
    for i in range(4):
        if _difficulty_buttons[i]:
            var is_selected = (i == difficulty)
            _difficulty_buttons[i].modulate = GameManagerClass.DIFFICULTY_COLORS[i] if is_selected else Color(0.6, 0.6, 0.6)

    # 更新音符数量
    if _note_count_label and _selected_song:
        var chart = _selected_song.get_chart(difficulty)
        if chart:
            _note_count_label.text = "Notes: %d" % chart.total_notes
        else:
            _note_count_label.text = "Notes: N/A"


func on_play_pressed() -> void:
    if _selected_song == null:
        return

    var chart = _selected_song.get_chart(_selected_difficulty)
    if chart == null:
        return

    if GameManager:
        GameManager.start_game(_selected_song, chart, _selected_difficulty)


func on_back_pressed() -> void:
    if GameManager:
        GameManager.return_to_main_menu()


func on_import_pressed() -> void:
    if ImportSongUI:
        ImportSongUI.show_import_dialog()
