class_name GameManagerClass
extends Node

## 游戏管理器 - 处理游戏状态和场景转换
## 单例 autoload

# 引用 GameTypes 避免循环依赖
const GameTypes = preload("res://scripts/game/GameTypes.gd")
const GameData = preload("res://scripts/data/GameData.gd")

# 使用 GameTypes 的枚举
enum Difficulty {
	EASY = 0,
	NORMAL = 1,
	HARD = 2,
	EXPERT = 3
}

enum GameState {
	MAIN_MENU,
	SONG_SELECTION,
	PLAYING,
	PAUSED,
	RESULTS,
	CHART_EDITOR,
	SETTINGS,
	ACHIEVEMENTS
}

# 难度密度倍率
static var DIFFICULTY_DENSITY_MULTIPLIERS: Array[float] = [0.4, 0.7, 1.0, 1.3]
# 难度名称
static var DIFFICULTY_NAMES: Array[String] = ["Easy", "Normal", "Hard", "Expert"]
# 难度颜色
static var DIFFICULTY_COLORS: Array[Color] = [Color("#4CAF50"), Color("#2196F3"), Color("#F44336"), Color("#9C27B0")]

# 当前游戏状态
var current_state: GameState = GameState.MAIN_MENU
var current_song: GameData.SongData
var current_chart: GameData.ChartData
var current_difficulty: int = GameTypes.Difficulty.NORMAL

var audio_offset: float = 0.0
var note_speed: float = 1.0

var _config_file: ConfigFile
var _config_loaded: bool = false

static var instance: GameManagerClass


func _ready() -> void:
	instance = self
	_config_file = ConfigFile.new()

	# 加载设置
	audio_offset = get_config_value("settings", "audio_offset", 0.0)
	note_speed = get_config_value("settings", "note_speed", 1.0)


func get_config_value(section: String, key: String, default: Variant) -> Variant:
	if not _config_loaded:
		_config_file.load("user://settings.cfg")
		_config_loaded = true
	return _config_file.get_value(section, key, default)


func save_config_value(section: String, key: String, value: Variant) -> void:
	_config_file.set_value(section, key, value)
	_config_file.save("user://settings.cfg")


# ============================================================
# 游戏状态管理
# ============================================================
func change_state(new_state: GameState) -> void:
	if current_state == new_state:
		return

	current_state = new_state

	match new_state:
		GameState.MAIN_MENU:
			get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")
		GameState.SONG_SELECTION:
			get_tree().change_scene_to_file("res://scenes/SongSelection.tscn")
		GameState.PLAYING:
			get_tree().change_scene_to_file("res://scenes/Gameplay.tscn")
		GameState.RESULTS:
			get_tree().change_scene_to_file("res://scenes/Results.tscn")
		GameState.SETTINGS:
			get_tree().change_scene_to_file("res://scenes/Settings.tscn")
		GameState.ACHIEVEMENTS:
			get_tree().change_scene_to_file("res://scenes/Achievements.tscn")
		GameState.CHART_EDITOR:
			get_tree().change_scene_to_file("res://scenes/ChartEditor.tscn")


func start_game(song: GameData.SongData, chart: GameData.ChartData, difficulty: int) -> void:
	current_song = song
	current_chart = chart
	current_difficulty = difficulty
	change_state(GameState.PLAYING)


func return_to_main_menu() -> void:
	change_state(GameState.MAIN_MENU)


# ============================================================
# 设置管理
# ============================================================
func set_audio_offset(offset: float) -> void:
	audio_offset = clamp(offset, -300.0, 300.0)
	save_config_value("settings", "audio_offset", audio_offset)


func set_note_speed(speed: float) -> void:
	note_speed = clamp(speed, 0.5, 3.0)
	save_config_value("settings", "note_speed", note_speed)
