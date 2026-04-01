class_name CloudManagerClass
extends Node

## 云同步管理器 (模拟实现)
## 单例 autoload

var is_online: bool = true
var is_logged_in: bool = false
var current_user_id: String = ""
var current_user_nickname: String = ""

var _config_file: ConfigFile
var _config_loaded: bool = false

static var instance: CloudManagerClass


func _ready() -> void:
    instance = self
    _config_file = ConfigFile.new()
    load_auth_state()


func load_auth_state() -> void:
    current_user_id = get_config_value("auth", "user_id", "")
    var token = get_config_value("auth", "token", "")
    is_logged_in = not current_user_id.is_empty() and not token.is_empty()


func get_config_value(section: String, key: String, default: Variant) -> Variant:
    if not _config_loaded:
        _config_file.load("user://settings.cfg")
        _config_loaded = true
    return _config_file.get_value(section, key, default)


func save_config_value(section: String, key: String, value: Variant) -> void:
    _config_file.set_value(section, key, value)
    _config_file.save("user://settings.cfg")


func login(phone: String, otp: String) -> void:
    # 模拟登录
    await get_tree().create_timer(0.5).timeout

    current_user_id = "mock_user_" + str(phone.hash())
    current_user_nickname = "Player"
    is_logged_in = true

    save_config_value("auth", "user_id", current_user_id)
    save_config_value("auth", "token", "mock_token")


func logout() -> void:
    current_user_id = ""
    current_user_nickname = ""
    is_logged_in = false

    save_config_value("auth", "user_id", "")
    save_config_value("auth", "token", "")
