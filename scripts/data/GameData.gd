# ============================================================
# GameData - 游戏数据定义（包含 SongData、ChartData、NoteData、PlayerData 等）
# 外部文件使用 const GameData = preload("res://scripts/data/GameData.gd") 访问
# ============================================================

# 引用 GameTypes 避免循环依赖
const GameTypes = preload("res://scripts/game/GameTypes.gd")

# 引用所有数据类型
const SongData = preload("res://scripts/data/types/SongData.gd")
const ChartData = preload("res://scripts/data/types/ChartData.gd")
const NoteData = preload("res://scripts/data/types/NoteData.gd")
const SongRecord = preload("res://scripts/data/types/SongRecord.gd")
const AchievementRecord = preload("res://scripts/data/types/AchievementRecord.gd")
const AchievementDefinition = preload("res://scripts/data/types/AchievementDefinition.gd")
const PlayerData = preload("res://scripts/data/types/PlayerData.gd")
const LeaderboardEntry = preload("res://scripts/data/types/LeaderboardEntry.gd")
const PlayRecordRequest = preload("res://scripts/data/types/PlayRecordRequest.gd")
const ScoreResult = preload("res://scripts/data/types/ScoreResult.gd")

# 导出 GameTypes 的枚举方便访问
enum Difficulty { EASY = 0, NORMAL = 1, HARD = 2, EXPERT = 3 }
enum NoteType { TAP = 0, HOLD = 1, SWIPE = 2 }
enum SwipeDirection { NONE = 0, LEFT = 1, RIGHT = 2, UP = 3, DOWN = 4 }

# 导出难度相关常量
static var DIFFICULTY_DENSITY_MULTIPLIERS: Array[float] = GameTypes.DIFFICULTY_DENSITY_MULTIPLIERS
static var DIFFICULTY_NAMES: Array[String] = GameTypes.DIFFICULTY_NAMES
static var DIFFICULTY_COLORS: Array[Color] = GameTypes.DIFFICULTY_COLORS
