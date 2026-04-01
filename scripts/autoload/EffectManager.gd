class_name EffectManagerClass
extends Node

## 视觉效果管理器 - 粒子、轨迹等
## 单例 autoload 用于全局效果访问

signal on_beat_pulse(beat: int)

# 效果设置
@export var lane_glow_duration: float = 0.2
@export var background_intensity_base: float = 0.3
@export var background_intensity_max: float = 1.0

# 判定颜色
const PERFECT_COLOR: Color = Color(0.6, 1.0, 1.0)
const GREAT_COLOR: Color = Color(1.0, 0.9, 0.3)
const GOOD_COLOR: Color = Color(0.5, 0.8, 0.5)
const MISS_COLOR: Color = Color(1.0, 0.3, 0.3)

# 效果场景引用
var _perfect_effect_scene: PackedScene
var _great_effect_scene: PackedScene
var _good_effect_scene: PackedScene
var _miss_effect_scene: PackedScene
var _combo_effect_scene: PackedScene
var _big_combo_effect_scene: PackedScene

# 活动粒子跟踪
var _active_particles: Array[GPUParticles2D] = []
var _current_background_intensity: float = 0.3

static var instance: EffectManagerClass


func _ready() -> void:
    instance = self
    load_effect_scenes()
    _current_background_intensity = background_intensity_base


func load_effect_scenes() -> void:
    # 尝试从资源加载效果场景
    _perfect_effect_scene = load("res://resources/effects/PerfectEffect.tscn")
    _great_effect_scene = load("res://resources/effects/GreatEffect.tscn")
    _good_effect_scene = load("res://resources/effects/GoodEffect.tscn")
    _miss_effect_scene = load("res://resources/effects/MissEffect.tscn")
    _combo_effect_scene = load("res://resources/effects/ComboEffect.tscn")
    _big_combo_effect_scene = load("res://resources/effects/BigComboEffect.tscn")


# ============================================================
# 击中效果
# ============================================================
func play_hit_effect(judgment: ScoreManagerClass.Judgment, position: Vector2, parent: Node) -> void:
    var effect_scene = get_effect_scene(judgment)

    if effect_scene:
        var effect = effect_scene.instantiate() as GPUParticles2D
        if effect:
            effect.position = position
            parent.add_child(effect)
            effect.emitting = true

            # 效果结束后自动移除
            var timer = get_tree().create_timer(effect.lifetime)
            timer.timeout.connect(func(): effect.queue_free())

            _active_particles.append(effect)
    else:
        create_dynamic_hit_effect(judgment, position, parent)


func get_effect_scene(judgment: ScoreManagerClass.Judgment) -> PackedScene:
    match judgment:
        ScoreManagerClass.Judgment.PERFECT:
            return _perfect_effect_scene
        ScoreManagerClass.Judgment.GREAT:
            return _great_effect_scene
        ScoreManagerClass.Judgment.GOOD:
            return _good_effect_scene
        ScoreManagerClass.Judgment.MISS:
            return _miss_effect_scene
    return _perfect_effect_scene


func create_dynamic_hit_effect(judgment: ScoreManagerClass.Judgment, position: Vector2, parent: Node) -> void:
    var particles = GPUParticles2D.new()
    particles.position = position
    particles.amount = 20
    particles.lifetime = 0.3
    particles.explosiveness = 0.8
    particles.one_shot = true
    particles.emitting = true

    var color = get_judgment_color(judgment)

    var material = ParticleProcessMaterial.new()
    material.emission_shape = ParticleProcessMaterial.EmissionShape.POINT
    material.direction = Vector3(0, -1, 0)
    material.spread = 45.0
    material.initial_velocity_min = 100.0
    material.initial_velocity_max = 200.0
    material.scale_min = 2.0
    material.scale_max = 4.0
    particle_material_set_color(material, color)

    particles.process_material = material
    parent.add_child(particles)

    var timer = get_tree().create_timer(0.5)
    timer.timeout.connect(func(): particles.queue_free())


func particle_material_set_color(material: ParticleProcessMaterial, color: Color) -> void:
    # Godot 4 中使用 color 属性
    material.color = color


func get_judgment_color(judgment: ScoreManagerClass.Judgment) -> Color:
    match judgment:
        ScoreManagerClass.Judgment.PERFECT:
            return PERFECT_COLOR
        ScoreManagerClass.Judgment.GREAT:
            return GREAT_COLOR
        ScoreManagerClass.Judgment.GOOD:
            return GOOD_COLOR
        ScoreManagerClass.Judgment.MISS:
            return MISS_COLOR
    return Color.WHITE


# ============================================================
# 连击效果
# ============================================================
func play_combo_effect(combo: int, position: Vector2, parent: Node) -> void:
    if combo >= 100:
        if _big_combo_effect_scene:
            spawn_effect(_big_combo_effect_scene, position, parent)
        else:
            create_combo_flash_effect(position, parent, Color.GOLD)
    elif combo >= 50:
        if _combo_effect_scene:
            spawn_effect(_combo_effect_scene, position, parent)
        else:
            create_combo_flash_effect(position, parent, Color.YELLOW)

    # 根据连击增加背景强度
    var combo_intensity = lerpf(background_intensity_base, background_intensity_max, combo / 100.0)
    set_background_intensity(combo_intensity)


func play_full_combo_effect(position: Vector2, parent: Node) -> void:
    create_combo_flash_effect(position, parent, PERFECT_COLOR, true)


func create_combo_flash_effect(position: Vector2, parent: Node, color: Color, is_full_combo: bool = false) -> void:
    var particles = GPUParticles2D.new()
    particles.position = position
    particles.amount = 50 if is_full_combo else 30
    particles.lifetime = 1.0 if is_full_combo else 0.5
    particles.explosiveness = 0.9
    particles.one_shot = true
    particles.emitting = true

    var material = ParticleProcessMaterial.new()
    material.emission_shape = ParticleProcessMaterial.EmissionShape.SPHERE
    material.emission_sphere_radius = 50.0
    material.direction = Vector3(0, 0, 0)
    material.spread = 180.0
    material.initial_velocity_min = 50.0
    material.initial_velocity_max = 150.0
    material.scale_min = 3.0
    material.scale_max = 8.0 if is_full_combo else 5.0
    particle_material_set_color(material, color)

    particles.process_material = material
    parent.add_child(particles)

    var timer = get_tree().create_timer(particles.lifetime + 0.2)
    timer.timeout.connect(func(): particles.queue_free())


# ============================================================
# 轨道效果
# ============================================================
func play_track_flash(track: int, position: Vector2, parent: Node) -> void:
    var flash = Sprite2D.new()

    var image = Image.create(100, 10, false, Image.FORMAT_RGBA8)
    image.fill(Color.WHITE)
    var texture = ImageTexture.create_from_image(image)

    flash.texture = texture
    flash.position = position
    flash.modulate = Color(1.0, 1.0, 1.0, 0.5)

    parent.add_child(flash)

    var tween = parent.create_tween()
    tween.tween_property(flash, "modulate:a", 0.0, 0.1)
    tween.tween_callback(flash.queue_free)


# ============================================================
# 轨道发光
# ============================================================
func flash_lane(lane_node: CanvasItem, judgment: ScoreManagerClass.Judgment) -> void:
    if lane_node == null:
        return

    var flash_color = get_judgment_color(judgment)

    var tween = lane_node.create_tween()
    tween.tween_property(lane_node, "modulate", flash_color, lane_glow_duration / 2.0)
    tween.tween_property(lane_node, "modulate", Color.WHITE, lane_glow_duration / 2.0)


func glow_lane(lane_node: CanvasItem, color: Color, intensity: float = 0.3) -> void:
    if lane_node == null:
        return

    var glow_color = Color(color.r, color.g, color.b, intensity)
    lane_node.modulate = glow_color


func reset_lane_glow(lane_node: CanvasItem) -> void:
    if lane_node == null:
        return
    lane_node.modulate = Color.WHITE


# ============================================================
# 背景效果
# ============================================================
func set_background_intensity(intensity: float) -> void:
    _current_background_intensity = intensity


func pulse_background() -> void:
    var start_intensity = _current_background_intensity
    var peak_intensity = minf(_current_background_intensity * 1.5, background_intensity_max)

    var tween = create_tween()
    tween.tween_method(set_background_intensity, start_intensity, peak_intensity, 0.15)
    tween.tween_method(set_background_intensity, peak_intensity, start_intensity, 0.15)

    on_beat_pulse.emit(int(Time.get_ticks_msec() / 1000))


# ============================================================
# 节拍同步
# ============================================================
func on_beat(beat: int) -> void:
    pulse_background()


# ============================================================
# 工具
# ============================================================
func spawn_effect(scene: PackedScene, position: Vector2, parent: Node) -> void:
    if scene == null:
        return

    var effect = scene.instantiate()
    effect.position = position
    parent.add_child(effect)

    if effect is GPUParticles2D:
        var particles = effect as GPUParticles2D
        particles.emitting = true
        var timer = get_tree().create_timer(particles.lifetime)
        timer.timeout.connect(func(): particles.queue_free())


func clear_all_effects() -> void:
    for particle in _active_particles:
        if particle and is_instance_valid(particle):
            particle.queue_free()
    _active_particles.clear()

    set_background_intensity(background_intensity_base)
