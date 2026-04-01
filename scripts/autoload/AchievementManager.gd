class_name AchievementManagerClass
extends Node

## 成就系统管理器
## 单例 autoload

const GameData = preload("res://scripts/data/GameData.gd")

signal on_achievement_unlock(record: GameData.AchievementRecord)
signal on_achievements_loaded

var _definitions: Dictionary = {}
var _records: Dictionary = {}
var _hidden_achievements: Array[GameData.AchievementDefinition] = []

static var instance: AchievementManagerClass


func _ready() -> void:
    instance = self
    initialize_achievements()
    load_records()


func initialize_achievements() -> void:
    # 常规成就
    add_achievement("first_clear", "First Steps", "Clear your first song", 1, false)
    add_achievement("first_import", "Music Collector", "Import your first local song", 1, false)
    add_achievement("combo_50", "Combo Starter", "Achieve 50 combo", 50, false)
    add_achievement("combo_100", "Combo Master", "Achieve 100 combo", 100, false)
    add_achievement("combo_500", "Combo Legend", "Achieve 500 combo", 500, false)
    add_achievement("combo_1000", "Combo King", "Achieve 1000 combo", 1000, false)
    add_achievement("collection_10", "Song Library", "Collect 10 songs", 10, false)
    add_achievement("collection_50", "Music Archive", "Collect 50 songs", 50, false)
    add_achievement("first_s_rank", "S-Rank Achiever", "Get an S rank on any song", 1, false)
    add_achievement("first_full_combo", "Full Combo!", "Achieve full combo on any song", 1, false)
    add_achievement("first_all_perfect", "Perfect Player", "Get all perfect on any song", 1, false)
    add_achievement("chart_create_1", "Chart Creator", "Create your first chart", 1, false)
    add_achievement("chart_create_10", "Chart Architect", "Create 10 charts", 10, false)

    # 隐藏成就
    add_achievement("hidden_code_sound", "???", "Unknown achievement", 100, true, "Hidden Track: Code of Sound")
    add_achievement("hidden_silent", "???", "Unknown achievement", 20, true, "Hidden Track: Silent Challenge")
    add_achievement("hidden_reverse", "???", "Unknown achievement", 1, true, "Hidden Track: Reverse World")
    add_achievement("hidden_chaos", "???", "Unknown achievement", 100, true, "Hidden Track: Chaos Maze")
    add_achievement("hidden_developer", "???", "Unknown achievement", 100, true, "Hidden Track: Developer Mode")


func add_achievement(id: String, name: String, description: String, target: int, is_hidden: bool, reward: String = "") -> void:
    var def = GameData.AchievementDefinition.new()
    def.id = id
    def.name = name
    def.description = description
    def.target = target
    def.is_hidden = is_hidden
    def.reward = reward

    _definitions[id] = def

    if is_hidden:
        _hidden_achievements.append(def)


func load_records() -> void:
    var player_data = GameData.PlayerData.load_data()

    for record in player_data.achievements:
        _records[record.achievement_id] = record

    # 初始化缺失的成就
    for def_id in _definitions.keys():
        if not _records.has(def_id):
            _records[def_id] = GameData.AchievementRecord.new()
            _records[def_id].achievement_id = def_id
            _records[def_id].unlocked = false
            _records[def_id].progress = 0
            _records[def_id].target = _definitions[def_id].target

    on_achievements_loaded.emit()


func unlock_achievement(achievement_id: String) -> void:
    if not _definitions.has(achievement_id):
        return
    if not _records.has(achievement_id):
        return

    var record = _records[achievement_id]
    if record.unlocked:
        return

    record.unlocked = true
    record.unlock_time = Time.get_datetime_dict_from_datetime_string(Time.get_datetime_string_from_system(), false)
    record.progress = _definitions[achievement_id].target

    save_records()
    on_achievement_unlock.emit(record)

    print("Achievement unlocked: " + _definitions[achievement_id].name)


func update_progress(achievement_id: String, progress: int) -> void:
    if not _definitions.has(achievement_id):
        return
    if not _records.has(achievement_id):
        return

    var record = _records[achievement_id]
    if record.unlocked:
        return

    record.progress = max(record.progress, progress)

    if record.progress >= _definitions[achievement_id].target:
        unlock_achievement(achievement_id)
    else:
        save_records()


func get_record(achievement_id: String) -> GameData.AchievementRecord:
    if _records.has(achievement_id):
        return _records[achievement_id]
    return null


func get_all_records() -> Array[GameData.AchievementRecord]:
    return _records.values()


func get_visible_achievements() -> Array[GameData.AchievementRecord]:
    var visible = []
    for record in _records.values():
        if _definitions.has(record.achievement_id):
            var def = _definitions[record.achievement_id]
            if not def.is_hidden or record.unlocked:
                visible.append(record)
    return visible


func get_total_progress() -> int:
    var total = 0
    var unlocked = 0

    for def in _definitions.values():
        if not def.is_hidden:
            total += 1
            if _records.has(def.id) and _records[def.id].unlocked:
                unlocked += 1

    if total > 0:
        return unlocked * 100 / total
    return 0


func get_unlocked_count() -> int:
    var count = 0
    for record in _records.values():
        if record.unlocked:
            count += 1
    return count


func save_records() -> void:
    var player_data = GameData.PlayerData.load_data()
    player_data.achievements = get_all_records()
    player_data.save()


func on_song_clear(song_id: String, result: GameData.ScoreResult, difficulty: int) -> void:
    # 首次通关
    if get_unlocked_count() == 0:
        unlock_achievement("first_clear")

    # 连击成就
    if result.max_combo >= 50:
        update_progress("combo_50", result.max_combo)
    if result.max_combo >= 100:
        update_progress("combo_100", result.max_combo)
    if result.max_combo >= 500:
        update_progress("combo_500", result.max_combo)
    if result.max_combo >= 1000:
        update_progress("combo_1000", result.max_combo)

    if result.is_full_combo:
        unlock_achievement("first_full_combo")

    if result.is_all_perfect:
        unlock_achievement("first_all_perfect")

    if result.grade == "S" or result.grade == "S+":
        unlock_achievement("first_s_rank")


func on_song_import() -> void:
    var player_data = GameData.PlayerData.load_data()

    if player_data.imported_songs.size() == 1:
        unlock_achievement("first_import")

    update_progress("collection_10", player_data.imported_songs.size())
    update_progress("collection_50", player_data.imported_songs.size())


func on_chart_created() -> void:
    var player_data = GameData.PlayerData.load_data()

    if player_data.created_charts.size() == 1:
        unlock_achievement("chart_create_1")

    update_progress("chart_create_10", player_data.created_charts.size())
