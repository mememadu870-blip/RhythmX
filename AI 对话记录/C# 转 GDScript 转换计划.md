# RhythmX C# 转 GDScript 转换计划

## 项目概述

**当前状态**: Godot 4.2 C# 版本 (.NET 8)
**目标**: 纯 GDScript 版本 (移除所有 C#/.NET 依赖)
**后端代码**: 保持不变 (无后端代码，本项目为纯客户端节奏游戏)

---

## 一、项目结构分析

### 1.1 需要转换的 C# 文件清单

#### Autoload 单例管理器 (9 个)
| 文件名 | 功能描述 | 优先级 |
|--------|----------|--------|
| `GameManager.cs` | 游戏状态管理、场景切换 | 高 |
| `AudioManager.cs` | 音频播放、节拍同步 | 高 |
| `ScoreManager.cs` | 计分、连击、判定系统 | 高 |
| `SongLibrary.cs` | 歌曲库管理 | 高 |
| `AchievementManager.cs` | 成就系统 | 中 |
| `EffectManager.cs` | 视觉效果管理 | 高 |
| `StatisticsManager.cs` | 统计数据 | 低 |
| `CloudManager.cs` | 云同步 (模拟) | 低 |
| `ImportSongUI.cs` | 歌曲导入 UI | 中 |

#### 数据模型 (1 个)
| 文件名 | 功能描述 | 优先级 |
|--------|----------|--------|
| `GameData.cs` | SongData, ChartData, NoteData, PlayerData 等数据结构 | 高 |

#### 游戏核心 (3 个)
| 文件名 | 功能描述 | 优先级 |
|--------|----------|--------|
| `NoteObject.cs` | 音符对象渲染与判定 | 高 |
| `TrackManager.cs` | 轨道输入管理 | 高 |
| `InputManager.cs` | 键盘/触摸输入处理 | 高 |

#### UI 控制器 (7 个)
| 文件名 | 功能描述 | 优先级 |
|--------|----------|--------|
| `MainMenuUI.cs` | 主菜单 UI | 中 |
| `SongSelectionUI.cs` | 歌曲选择 UI | 中 |
| `GameplayUI.cs` | 游戏播放 UI | 高 |
| `ResultsUI.cs` | 结算界面 UI | 中 |
| `SettingsUI.cs` | 设置界面 UI | 中 |
| `AchievementsUI.cs` | 成就界面 UI | 低 |
| `ChartEditorUI.cs` | 谱面编辑器 UI | 低 |

#### API 相关 (4 个)
| 文件名 | 功能描述 | 优先级 |
|--------|----------|--------|
| `IAPIManager.cs` | API 接口定义和数据类型 | 中 |
| `MockAPIManager.cs` | 模拟 API 实现 | 中 |
| `APIConfig.cs` | API 配置 (静态类) | 中 |
| (无后端代码) | **用户说明无后端，保持现状** | - |

#### 音频处理 (2 个)
| 文件名 | 功能描述 | 优先级 |
|--------|----------|--------|
| `AudioAnalysis.cs` | BPM 检测、音频分析 | 中 |
| `ChartGenerator.cs` | 自动谱面生成 | 中 |

**总计**: 26 个 C# 文件需要转换

---

## 二、转换策略

### 2.1 核心原则

1. **保持功能不变**: 所有游戏逻辑、UI 交互、数据持久化保持原样
2. **一对一转换**: 每个 `.cs` 文件转换为对应的 `.gd` 文件
3. **路径映射**: `res://scripts/xxx.cs` → `res://scripts/xxx.gd`
4. **单例注册**: 在 `project.godot` 中更新 autoload 路径

### 2.2 技术映射对照表

| C# 概念 | GDScript 等价物 |
|---------|-----------------|
| `public partial class X : Node` | `class_name X extends Node` |
| `public static X Instance` | `static var instance: X` |
| `public var Property { get; set; }` | `@export var property: Type` |
| `public event Action<T> OnEvent` | `signal on_event(value: T)` |
| `GetNode<T>("path")` | `$"path" as Type` 或 `get_node("path")` |
| `QueueFree()` | `queue_free()` |
| `GD.Print()` | `print()` |
| `Mathf.Clamp()` | `clamp()` |
| `Time.GetTicksMsec()` | `Time.get_ticks_msec()` |
| `CreateTween()` | `create_tween()` |
| `_Ready()` | `_ready()` |
| `_Process(delta)` | `_process(delta)` |
| `_Input(event)` | `_input(event)` |
| `ConfigFile` | `ConfigFile` (相同) |
| `List<T>` | `Array[T]` |
| `Dictionary<K,V>` | `Dictionary` |
| `Random` | `RandomNumberGenerator` |

### 2.3 数据结构转换

```csharp
// C# 类
public class SongData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double Bpm { get; set; }
    public List<ChartData> Charts { get; set; } = new();
}
```

```gdscript
# GDScript 类
class_name SongData
extends RefCounted

var id: String = ""
var name: String = ""
var bpm: float = 120.0
var charts: Array[ChartData] = []
```

### 2.4 Enum 转换

```csharp
// C#
public enum GameState { MainMenu, SongSelection, Playing }
```

```gdscript
# GDScript (在 GameManager 中)
enum GameState { MAIN_MENU, SONG_SELECTION, PLAYING }
```

---

## 三、执行步骤

### 阶段 1: 准备工作
- [ ] 备份当前项目状态 (git commit)
- [ ] 创建 `scripts/autoload/`, `scripts/data/` 等目录结构 (已存在)
- [ ] 记录当前 `project.godot` 配置

### 阶段 2: 核心数据层 (优先转换)
1. **GameData.gd** - 所有数据结构的基础
   - SongData, ChartData, NoteData
   - PlayerData, SongRecord, AchievementRecord
   - NoteType, SwipeDirection 枚举

### 阶段 3: 核心管理器 (按依赖顺序)
2. **AudioManager.gd** - 音频系统 (无外部依赖)
3. **ScoreManager.gd** - 计分系统 (依赖 AudioManager)
4. **EffectManager.gd** - 效果系统 (依赖 ScoreManager)
5. **TrackManager.gd** - 轨道管理 (依赖 AudioManager, EffectManager)
6. **InputManager.gd** - 输入处理 (依赖 TrackManager, GameplayUI)
7. **GameManager.gd** - 游戏状态中枢 (依赖多个管理器)

### 阶段 4: 游戏系统
8. **NoteObject.gd** - 音符对象 (依赖 ScoreManager)
9. **SongLibrary.gd** - 歌曲库 (依赖 GameManager, PlayerData)
10. **AchievementManager.gd** - 成就系统 (依赖 PlayerData)
11. **StatisticsManager.gd** - 统计系统
12. **CloudManager.gd** - 云同步

### 阶段 5: UI 层
13. **MainMenuUI.gd**
14. **SongSelectionUI.gd**
15. **GameplayUI.gd**
16. **ResultsUI.gd**
17. **SettingsUI.gd**
18. **AchievementsUI.gd**
19. **ChartEditorUI.gd**
20. **ImportSongUI.gd**

### 阶段 6: API 和音频处理
21. **APIConfig.gd** (改为全局静态函数)
22. **IAPIManager.gd** (类型定义合并到 APIManager.gd)
23. **MockAPIManager.gd** → **APIManager.gd**
24. **AudioAnalysis.gd**
25. **ChartGenerator.gd**

### 阶段 7: 清理和验证
- [ ] 更新 `project.godot` 中的 autoload 配置
- [ ] 移除 C# 相关文件 (.cs, .csproj, .sln)
- [ ] 移除 `.godot/mono/` 目录
- [ ] 更新 `.gitignore` (移除 C# 相关条目)
- [ ] 测试所有功能
- [ ] 验证导出功能

---

## 四、project.godot 变更

### 变更前 (C# 版本)
```ini
config/features=PackedStringArray("4.2", "C#", "Mobile")
[autoload]
GameManager="*res://scripts/autoload/GameManager.cs"
...
[dotnet]
project/assembly_name="RhythmX"
```

### 变更后 (GDScript 版本)
```ini
config/features=PackedStringArray("4.2", "Mobile")
[autoload]
GameManager="*res://scripts/autoload/GameManager.gd"
...
# 移除 [dotnet] 段
```

---

## 五、文件结构对照

```
scripts/
├── autoload/
│   ├── GameManager.gd          # 从 .cs 转换
│   ├── AudioManager.gd
│   ├── ScoreManager.gd
│   ├── SongLibrary.gd
│   ├── AchievementManager.gd
│   ├── EffectManager.gd
│   ├── StatisticsManager.gd
│   ├── CloudManager.gd
│   └── ImportSongUI.gd
├── api/
│   ├── APIManager.gd           # 合并 IAPIManager + MockAPIManager
│   └── APIConfig.gd
├── audio/
│   ├── AudioAnalysis.gd
│   └── ChartGenerator.gd
├── data/
│   └── GameData.gd             # 所有数据类
├── gameplay/
│   ├── NoteObject.gd
│   ├── TrackManager.gd
│   └── InputManager.gd
└── ui/
    ├── MainMenuUI.gd
    ├── SongSelectionUI.gd
    ├── GameplayUI.gd
    ├── ResultsUI.gd
    ├── SettingsUI.gd
    ├── AchievementsUI.gd
    ├── ChartEditorUI.gd
    └── ImportSongUI.gd (已在 autoload)
```

---

## 六、注意事项

### 6.1 不转换的内容
- **后端代码**: 用户说明无后端代码
- **`.github/`**: GitHub Actions CI 配置保留
- **`export_presets.cfg`**: 导出配置保留
- **`assets/`**: 资源文件保留
- **`resources/`**: 主题和场景资源保留
- **`scenes/`**: 场景文件 (.tscn) 保留，仅更新引用的脚本路径

### 6.2 需要调整的内容
- **场景文件**: 更新 `.tscn` 中的脚本引用路径
- **导出预设**: 移除 .NET 相关配置

### 6.3 潜在风险
1. **异步代码**: C# 的 `async/await` 需转换为 GDScript 的 `await` 或信号回调
2. **泛型**: GDScript 泛型支持有限，需使用 `Array[Type]` 或 `Variant`
3. **JSON 序列化**: `System.Text.Json` 需转换为 `JSON.stringify()` / `JSON.parse_string()`
4. **时间精度**: `Time.GetTicksMsec()` 精度可能与 C# 不同

---

## 七、测试清单

转换完成后需验证以下功能:

- [ ] 主菜单正常显示和导航
- [ ] 歌曲列表加载和选择
- [ ] 游戏播放流程完整
- [ ] 音符判定 (Perfect/Great/Good/Miss)
- [ ] 连击系统
- [ ] 计分系统
- [ ] 暂停/恢复功能
- [ ] 设置保存和加载
- [ ] 成就解锁
- [ ] 歌曲导入功能
- [ ] 谱面编辑器功能
- [ ] 移动端触摸输入
- [ ] 键盘输入 (D,F,J,K)
- [ ] 数据持久化 (playerdata.json)

---

## 八、时间估算

| 阶段 | 文件数 | 预估时间 |
|------|--------|----------|
| 数据层 | 1 | 30 分钟 |
| 核心管理器 | 6 | 3 小时 |
| 游戏系统 | 5 | 2 小时 |
| UI 层 | 7 | 2 小时 |
| API/音频 | 4 | 1.5 小时 |
| 清理验证 | - | 1 小时 |
| **总计** | **26** | **约 10 小时** |

---

## 审批

请确认以上计划是否符合您的要求。批准后我将开始执行转换。

**批准人**: ________________
**日期**: ________________
