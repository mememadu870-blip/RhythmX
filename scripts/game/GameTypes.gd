# ============================================================
# GameTypes - 游戏类型和枚举定义
# 避免循环依赖的公共类型定义
# ============================================================
class_name GameTypes
extends RefCounted

# 难度枚举
enum Difficulty {
    EASY,
    NORMAL,
    HARD,
    EXPERT
}

# 音符类型
enum NoteType {
    TAP,
    HOLD,
    SWIPE
}

# 滑动方向
enum SwipeDirection {
    NONE,
    LEFT,
    RIGHT,
    UP,
    DOWN
}

# 难度密度倍率
static var DIFFICULTY_DENSITY_MULTIPLIERS: Array[float] = [0.4, 0.7, 1.0, 1.3]

# 难度名称
static var DIFFICULTY_NAMES: Array[String] = ["Easy", "Normal", "Hard", "Expert"]

# 难度颜色
static var DIFFICULTY_COLORS: Array[Color] = [
    Color(0.5, 1.0, 0.5),   # Easy - 绿色
    Color(1.0, 1.0, 0.5),   # Normal - 黄色
    Color(1.0, 0.5, 0.5),   # Hard - 红色
    Color(0.8, 0.3, 1.0)    # Expert - 紫色
]
