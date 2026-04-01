class_name MainMenuUIClass
extends Control

## 主菜单 UI 控制器

const GameData = preload("res://scripts/data/GameData.gd")
const GameManagerClass = preload("res://scripts/autoload/GameManager.gd")

var _user_name_label: Label
var _user_stats_label: Label
var _achievement_progress_label: Label


func _ready() -> void:
    # 获取节点引用
    _user_name_label = get_node_or_null("UserInfoContainer/UserInfo/UserName") as Label
    _user_stats_label = get_node_or_null("UserInfoContainer/UserInfo/UserStats") as Label
    _achievement_progress_label = get_node_or_null("AchievementProgress/AchievementLabel") as Label

    update_ui()


func update_ui() -> void:
    # 更新用户信息
    var player_data = GameData.PlayerData.load_data()

    if _user_name_label:
        _user_name_label.text = player_data.nickname if player_data.nickname else "Guest"

    if _user_stats_label:
        _user_stats_label.text = "Played: %d songs" % player_data.total_play_count

    # 更新成就进度
    if _achievement_progress_label:
        var unlocked = 0
        if AchievementManager:
            unlocked = AchievementManager.get_unlocked_count()
        _achievement_progress_label.text = "%d achievements unlocked" % unlocked


# 信号回调方法
func on_play_pressed() -> void:
    print("OnPlayPressed called")
    if GameManager:
        GameManager.change_state(GameManagerClass.GameState.SONG_SELECTION)


func on_import_pressed() -> void:
    print("OnImportPressed called")
    if ImportSongUI:
        ImportSongUI.show_import_dialog()


func on_editor_pressed() -> void:
    print("OnEditorPressed called")
    if GameManager:
        GameManager.change_state(GameManagerClass.GameState.CHART_EDITOR)


func on_achievements_pressed() -> void:
    print("OnAchievementsPressed called")
    if GameManager:
        GameManager.change_state(GameManagerClass.GameState.ACHIEVEMENTS)


func on_settings_pressed() -> void:
    print("OnSettingsPressed called")
    if GameManager:
        GameManager.change_state(GameManagerClass.GameState.SETTINGS)
