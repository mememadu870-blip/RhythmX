class_name AchievementsUIClass
extends Control

## 成就界面 UI 控制器

const GameData = preload("res://scripts/data/GameData.gd")

var _progress_label: Label
var _progress_bar: ProgressBar
var _hidden_label: Label
var _achievement_list: VBoxContainer
var _all_tab: Button
var _regular_tab: Button
var _hidden_tab: Button
var _back_button: Button

var _current_tab: int = 0  # 0 = All, 1 = Regular, 2 = Hidden


func _ready() -> void:
    # 获取节点引用
    _progress_label = get_node_or_null("SummaryContainer/ProgressLabel") as Label
    _progress_bar = get_node_or_null("SummaryContainer/ProgressBar") as ProgressBar
    _hidden_label = get_node_or_null("SummaryContainer/HiddenLabel") as Label
    _achievement_list = get_node_or_null("ScrollContainer/AchievementList") as VBoxContainer
    _all_tab = get_node_or_null("TabContainer/AllTab") as Button
    _regular_tab = get_node_or_null("TabContainer/RegularTab") as Button
    _hidden_tab = get_node_or_null("TabContainer/HiddenTab") as Button
    _back_button = get_node_or_null("Header/BackButton") as Button

    # 连接信号
    if _all_tab:
        _all_tab.pressed.connect(func(): select_tab(0))
    if _regular_tab:
        _regular_tab.pressed.connect(func(): select_tab(1))
    if _hidden_tab:
        _hidden_tab.pressed.connect(func(): select_tab(2))
    if _back_button:
        _back_button.pressed.connect(on_back_pressed)

    refresh_list()


func refresh_list() -> void:
    update_summary()
    populate_achievements()


func update_summary() -> void:
    if AchievementManager == null:
        return

    var progress = AchievementManager.get_total_progress()
    var records = AchievementManager.get_all_records()
    var unlocked = 0
    var total = 0
    var hidden_unlocked = 0

    for record in records:
        if AchievementManager.definitions.has(record.achievement_id):
            var def = AchievementManager.definitions[record.achievement_id]
            if not def.is_hidden:
                total += 1
                if record.unlocked:
                    unlocked += 1
            elif record.unlocked:
                hidden_unlocked += 1

    if _progress_label:
        _progress_label.text = "%d / %d Unlocked" % [unlocked, total]

    if _progress_bar:
        _progress_bar.value = progress

    if _hidden_label:
        _hidden_label.text = "Hidden: %d / 5" % hidden_unlocked


func populate_achievements() -> void:
    if _achievement_list == null or AchievementManager == null:
        return

    # 清除现有项目
    for child in _achievement_list.get_children():
        child.queue_free()

    var records: Array[GameData.AchievementRecord] = []

    match _current_tab:
        1:  # Regular
            for r in AchievementManager.get_all_records():
                if AchievementManager.definitions.has(r.achievement_id):
                    var def = AchievementManager.definitions[r.achievement_id]
                    if not def.is_hidden:
                        records.append(r)
        2:  # Hidden (only show unlocked)
            for r in AchievementManager.get_all_records():
                if r.unlocked and AchievementManager.definitions.has(r.achievement_id):
                    var def = AchievementManager.definitions[r.achievement_id]
                    if def.is_hidden:
                        records.append(r)
        _:  # All visible
            records = AchievementManager.get_visible_achievements()

    for record in records:
        create_achievement_item(record)


func create_achievement_item(record: GameData.AchievementRecord) -> void:
    var container = HBoxContainer.new()
    container.custom_minimum_size = Vector2(0, 60)

    if AchievementManager.definitions.has(record.achievement_id):
        var def = AchievementManager.definitions[record.achievement_id]

        var name_label = Label.new()
        name_label.text = "???" if (def.is_hidden and not record.unlocked) else def.name
        name_label.size_flags_horizontal = Control.SIZE_EXPAND
        container.add_child(name_label)

        var progress_label = Label.new()
        progress_label.text = "✓" if record.unlocked else "%d/%d" % [record.progress, def.target]
        container.add_child(progress_label)

    _achievement_list.add_child(container)


func select_tab(tab: int) -> void:
    _current_tab = tab

    # 更新标签按钮状态
    if _all_tab:
        _all_tab.modulate = GameManagerClass.DIFFICULTY_COLORS[0] if tab == 0 else Color.WHITE
    if _regular_tab:
        _regular_tab.modulate = GameManagerClass.DIFFICULTY_COLORS[1] if tab == 1 else Color.WHITE
    if _hidden_tab:
        _hidden_tab.modulate = GameManagerClass.DIFFICULTY_COLORS[3] if tab == 2 else Color.WHITE

    populate_achievements()


func on_back_pressed() -> void:
    if GameManager:
        GameManager.return_to_main_menu()
