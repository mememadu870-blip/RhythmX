class_name ImportSongUIClass
extends Control

## 导入歌曲 UI 控制器
## 单例 autoload

const GameData = preload("res://scripts/data/GameData.gd")

var _file_dialog: FileDialog
var _import_button: Button
var _cancel_button: Button
var _status_label: Label
var _progress_bar: ProgressBar
var _is_importing: bool = false
var _selected_path: String = ""

static var instance: ImportSongUIClass


func _ready() -> void:
    instance = self

    create_ui()
    visible = false


func create_ui() -> void:
    # 创建文件对话框
    _file_dialog = FileDialog.new()
    _file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
    _file_dialog.access = FileDialog.ACCESS_FILESYSTEM
    _file_dialog.filters = PackedStringArray(["*.wav", "*.ogg"])
    _file_dialog.title = "Select Audio File"
    _file_dialog.file_selected.connect(on_file_selected)
    add_child(_file_dialog)

    # 主容器
    var panel = PanelContainer.new()
    panel.custom_minimum_size = Vector2(400, 200)
    panel.anchor_right = 1.0
    panel.anchor_bottom = 1.0

    var vbox = VBoxContainer.new()
    vbox.alignment = BoxContainer.ALIGNMENT_CENTER

    # 状态标签
    _status_label = Label.new()
    _status_label.text = "Select an audio file to import"
    _status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    vbox.add_child(_status_label)

    # 进度条
    _progress_bar = ProgressBar.new()
    _progress_bar.min_value = 0
    _progress_bar.max_value = 100
    _progress_bar.value = 0
    _progress_bar.custom_minimum_size = Vector2(300, 20)
    _progress_bar.visible = false
    vbox.add_child(_progress_bar)

    # 按钮容器
    var button_box = HBoxContainer.new()
    button_box.alignment = BoxContainer.ALIGNMENT_CENTER

    _import_button = Button.new()
    _import_button.text = "Import"
    _import_button.pressed.connect(on_import_pressed)
    button_box.add_child(_import_button)

    _cancel_button = Button.new()
    _cancel_button.text = "Cancel"
    _cancel_button.pressed.connect(on_cancel_pressed)
    button_box.add_child(_cancel_button)

    vbox.add_child(button_box)
    panel.add_child(vbox)
    add_child(panel)


func show_import_dialog() -> void:
    visible = true
    _file_dialog.show()


func on_file_selected(path: String) -> void:
    _status_label.text = "Selected: " + path.get_file()
    _selected_path = path


func on_import_pressed() -> void:
    if _is_importing:
        return

    if _selected_path.is_empty():
        _status_label.text = "Please select a file first!"
        return

    _is_importing = true
    _import_button.disabled = true
    _progress_bar.visible = true
    _progress_bar.value = 0
    _status_label.text = "Importing..."

    # 使用 await 进行异步导入
    await import_song_async(_selected_path)

    _is_importing = false
    _import_button.disabled = false


func import_song_async(audio_path: String) -> void:
    # Step 1: 分析 BPM
    _status_label.text = "Analyzing BPM..."
    _progress_bar.value = 25

    var bpm = 120.0
    if AudioAnalysis:
        bpm = await AudioAnalysis.analyze_bpm_from_file_async(audio_path)
    print("Detected BPM: %.0f" % bpm)

    # Step 2: 生成谱面
    _status_label.text = "Generating charts..."
    _progress_bar.value = 50

    var stream = ResourceLoader.load(audio_path)
    var charts = []
    if ChartGenerator:
        charts = ChartGenerator.generate_all_charts(stream, bpm)

    # Step 3: 创建歌曲数据
    _status_label.text = "Saving song..."
    _progress_bar.value = 75

    var file_name = audio_path.get_file()
    var song_name = file_name.get_basename()

    var song = GameData.SongData.new()
    song.id = "imported_" + str(Time.get_ticks_usec())
    song.name = song_name
    song.artist = "Unknown Artist"
    song.bpm = bpm
    song.duration = get_audio_duration(audio_path)
    song.audio_path = audio_path
    song.is_imported = true
    song.charts = charts

    # Step 4: 添加到库
    if SongLibrary:
        SongLibrary.add_song(song)

    # 保存到玩家数据
    var player_data = GameData.PlayerData.load_data()
    player_data.imported_songs.append(song.id)
    player_data.save()

    # 检查成就
    if AchievementManager:
        AchievementManager.on_song_import()

    _progress_bar.value = 100
    _status_label.text = "Imported: %s (BPM: %.0f)" % [song_name, bpm]

    await get_tree().create_timer(1.5).timeout

    visible = false


func get_audio_duration(path: String) -> float:
    var extension = path.get_extension().to_lower()

    if extension == "wav":
        var stream = ResourceLoader.load(path) as AudioStreamWAV
        if stream:
            return stream.get_length()
    elif extension == "ogg":
        var stream = ResourceLoader.load(path) as AudioStreamOGGVorbis
        if stream:
            return stream.get_length()

    return 180.0  # 默认 3 分钟


func on_cancel_pressed() -> void:
    _selected_path = ""
    visible = false
