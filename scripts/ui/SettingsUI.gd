class_name SettingsUIClass
extends Control

## 设置界面 UI 控制器

const GameData = preload("res://scripts/data/GameData.gd")

var _audio_offset_slider: HSlider
var _offset_value_label: Label
var _speed_slider: HSlider
var _speed_value_label: Label
var _volume_slider: HSlider
var _volume_value_label: Label
var _fullscreen_toggle: CheckButton
var _show_combo_toggle: CheckButton
var _show_judgment_toggle: CheckButton
var _account_info_label: Label
var _logout_button: Button
var _reset_button: Button
var _back_button: Button


func _ready() -> void:
    # 获取节点引用
    _audio_offset_slider = get_node_or_null("ScrollContainer/SettingsVBox/AudioSection/AudioOffsetSlider") as HSlider
    _offset_value_label = get_node_or_null("ScrollContainer/SettingsVBox/AudioSection/AudioOffsetContainer/OffsetValue") as Label
    _speed_slider = get_node_or_null("ScrollContainer/SettingsVBox/AudioSection/SpeedSlider") as HSlider
    _speed_value_label = get_node_or_null("ScrollContainer/SettingsVBox/AudioSection/SpeedContainer/SpeedValue") as Label
    _volume_slider = get_node_or_null("ScrollContainer/SettingsVBox/AudioSection/VolumeSlider") as HSlider
    _volume_value_label = get_node_or_null("ScrollContainer/SettingsVBox/AudioSection/VolumeContainer/VolumeValue") as Label
    _fullscreen_toggle = get_node_or_null("ScrollContainer/SettingsVBox/VisualSection/FullscreenToggle") as CheckButton
    _show_combo_toggle = get_node_or_null("ScrollContainer/SettingsVBox/VisualSection/ShowComboToggle") as CheckButton
    _show_judgment_toggle = get_node_or_null("ScrollContainer/SettingsVBox/VisualSection/ShowJudgmentToggle") as CheckButton
    _account_info_label = get_node_or_null("ScrollContainer/SettingsVBox/AccountSection/AccountInfo") as Label
    _logout_button = get_node_or_null("ScrollContainer/SettingsVBox/AccountSection/LogoutButton") as Button
    _reset_button = get_node_or_null("ScrollContainer/SettingsVBox/ResetButton") as Button
    _back_button = get_node_or_null("Header/BackButton") as Button

    # 连接信号
    if _audio_offset_slider:
        _audio_offset_slider.value_changed.connect(on_audio_offset_changed)
    if _speed_slider:
        _speed_slider.value_changed.connect(on_speed_changed)
    if _volume_slider:
        _volume_slider.value_changed.connect(on_volume_changed)
    if _fullscreen_toggle:
        _fullscreen_toggle.toggled.connect(on_fullscreen_toggled)
    if _logout_button:
        _logout_button.pressed.connect(on_logout_pressed)
    if _reset_button:
        _reset_button.pressed.connect(on_reset_pressed)
    if _back_button:
        _back_button.pressed.connect(on_back_pressed)

    load_settings()


func load_settings() -> void:
    var player_data = GameData.PlayerData.load_data()

    if _audio_offset_slider:
        _audio_offset_slider.value = player_data.audio_offset
    if _offset_value_label:
        _offset_value_label.text = "%.0f ms" % player_data.audio_offset

    if _speed_slider:
        _speed_slider.value = player_data.note_speed
    if _speed_value_label:
        _speed_value_label.text = "%.1fx" % player_data.note_speed

    if _fullscreen_toggle:
        _fullscreen_toggle.button_pressed = DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN

    update_account_info()


func update_account_info() -> void:
    if _account_info_label:
        if CloudManager and CloudManager.is_logged_in:
            _account_info_label.text = "Logged in as: " + CloudManager.current_user_nickname
        else:
            _account_info_label.text = "Not logged in"


func on_audio_offset_changed(value: float) -> void:
    if GameManager:
        GameManager.set_audio_offset(value)
    if _offset_value_label:
        _offset_value_label.text = "%.0f ms" % value


func on_speed_changed(value: float) -> void:
    if GameManager:
        GameManager.set_note_speed(value)
    if _speed_value_label:
        _speed_value_label.text = "%.1fx" % value


func on_volume_changed(value: float) -> void:
    if AudioManager:
        AudioManager.set_music_volume(value / 100.0)
    if _volume_value_label:
        _volume_value_label.text = "%.0f%%" % value


func on_fullscreen_toggled(is_on: bool) -> void:
    if is_on:
        DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
    else:
        DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)


func on_logout_pressed() -> void:
    if CloudManager:
        CloudManager.logout()
    update_account_info()


func on_reset_pressed() -> void:
    if _audio_offset_slider:
        _audio_offset_slider.value = 0
    if _speed_slider:
        _speed_slider.value = 1.0
    if _volume_slider:
        _volume_slider.value = 100

    if GameManager:
        GameManager.set_audio_offset(0)
        GameManager.set_note_speed(1.0)


func on_back_pressed() -> void:
    if GameManager:
        GameManager.return_to_main_menu()
