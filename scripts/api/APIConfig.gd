## API 配置 - 全局静态函数
## 用于 API 认证和设置存储

const BASE_URL = "https://api.rhythmx.app/v1"
const AUTH_ENDPOINT = "/auth"
const SONGS_ENDPOINT = "/songs"
const CHARTS_ENDPOINT = "/charts"
const SYNC_ENDPOINT = "/sync"
const LEADERBOARD_ENDPOINT = "/leaderboard"
const ACHIEVEMENTS_ENDPOINT = "/achievements"

# 设置
const REQUEST_TIMEOUT_SECONDS = 30
const MAX_RETRY_COUNT = 3
const RETRY_DELAY_MS = 1000

# 存储键
const TOKEN_KEY = "rhythmx_token"
const USER_ID_KEY = "rhythmx_user_id"
const LAST_SYNC_KEY = "rhythmx_last_sync"


## 获取存储的 token
static func get_token() -> String:
    var config = ConfigFile.new()
    if config.load("user://api_config.cfg") == OK:
        return config.get_value("auth", "token", "")
    return ""


## 设置 token
static func set_token(token: String) -> void:
    var config = ConfigFile.new()
    config.load("user://api_config.cfg")
    config.set_value("auth", "token", token)
    config.save("user://api_config.cfg")


## 获取用户 ID
static func get_user_id() -> String:
    var config = ConfigFile.new()
    if config.load("user://api_config.cfg") == OK:
        return config.get_value("auth", "user_id", "")
    return ""


## 设置用户 ID
static func set_user_id(user_id: String) -> void:
    var config = ConfigFile.new()
    config.load("user://api_config.cfg")
    config.set_value("auth", "user_id", user_id)
    config.save("user://api_config.cfg")


## 清除认证信息
static func clear_auth() -> void:
    var config = ConfigFile.new()
    config.load("user://api_config.cfg")
    config.set_value("auth", "token", "")
    config.set_value("auth", "user_id", "")
    config.save("user://api_config.cfg")


## 获取最后同步时间
static func get_last_sync_time() -> int:
    var config = ConfigFile.new()
    if config.load("user://api_config.cfg") == OK:
        return config.get_value("sync", "last_sync", 0)
    return 0


## 设置最后同步时间
static func set_last_sync_time(timestamp: int) -> void:
    var config = ConfigFile.new()
    config.load("user://api_config.cfg")
    config.set_value("sync", "last_sync", timestamp)
    config.save("user://api_config.cfg")


## 检查是否已认证
static func is_authenticated() -> bool:
    return not get_token().is_empty() and not get_user_id().is_empty()
