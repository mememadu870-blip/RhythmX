class_name SongLibraryClass
extends Node

## 管理歌曲库 - 导入、保存、加载歌曲
## 单例 autoload

const GameData = preload("res://scripts/data/GameData.gd")

signal on_song_imported(song: GameData.SongData)
signal on_library_updated

var songs: Array[GameData.SongData] = []
var imported_songs: Array[GameData.SongData] = []

var _imported_path: String

static var instance: SongLibraryClass


func _ready() -> void:
    instance = self
    _imported_path = "user://imported_songs"

    DirAccess.make_dir_recursive_absolute(_imported_path)
    load_library()


func load_library() -> void:
    songs.clear()
    imported_songs.clear()

    # 加载模拟歌曲
    load_mock_songs()

    # 加载导入的歌曲
    var dir = DirAccess.open(_imported_path)
    if dir:
        dir.list_dir_begin()
        var file_name = dir.get_next()
        while file_name != "":
            if not dir.current_is_dir() and file_name.ends_with(".json"):
                var song = load_song_from_json(_imported_path + "/" + file_name)
                if song:
                    imported_songs.append(song)
                    songs.append(song)
            file_name = dir.get_next()

    on_library_updated.emit()


func load_mock_songs() -> void:
    var mock_names = [
        "Neon Pulsar", "Digital Dreams", "Crystal Wave", "Thunder Strike", "Midnight Run",
        "Starlight Serenade", "Electric Soul", "Cosmic Journey", "Rainbow Road", "Final Frontier"
    ]
    var mock_artists = [
        "Synthwave Masters", "Chiptune Collective", "Electronic Dreams", "Bass Warriors", "Melody Makers",
        "Rhythm Rebels", "Sound Architects", "Beat Breakers", "Audio Alchemists", "Music Machines"
    ]

    for i in range(10):
        var song = GameData.SongData.new()
        song.id = "song_%02d" % i
        song.name = mock_names[i]
        song.artist = mock_artists[i]
        song.bpm = 120.0 + i * 10.0
        song.duration = 180.0 + i * 15.0
        song.is_imported = false
        song.is_favorite = i < 2
        song.play_count = i * 10 if i < 5 else 0
        song.high_score = 800000 + i * 20000 if i < 5 else 0

        # 为每个难度生成模拟谱面
        for d in range(4):
            var chart = generate_mock_chart(song, d)
            song.charts.append(chart)

        songs.append(song)


func generate_mock_chart(song: GameData.SongData, difficulty: int) -> GameData.ChartData:
    var chart = GameData.ChartData.new()
    chart.id = song.id + "_chart_" + str(difficulty)
    chart.difficulty = difficulty
    chart.track_count = 4
    chart.bpm = song.bpm
    chart.offset = 0.0

    # 根据难度生成音符
    var density = GameData.DIFFICULTY_DENSITY_MULTIPLIERS[difficulty]
    var note_count = int(density * 100.0 * (song.duration / 180.0))
    var interval = song.duration / note_count

    var rng = RandomNumberGenerator.new()
    rng.seed = hash(song.id)

    for i in range(note_count):
        var note = GameData.NoteData.new()
        note.time = i * interval
        note.track = rng.randi_range(0, 3)
        note.type = GameData.NoteType.TAP

        # 添加一些变化
        if difficulty >= GameData.Difficulty.HARD and rng.randf() < 0.15:
            note.type = GameData.NoteType.HOLD
            note.end_time = note.time + rng.randf_range(0.5, 1.5)
        elif difficulty == GameData.Difficulty.EXPERT and rng.randf() < 0.1:
            note.type = GameData.NoteType.SWIPE
            note.swipe_direction = rng.randi_range(1, 4)

        chart.notes.append(note)

    return chart


func load_song_from_json(path: String) -> GameData.SongData:
    var file = FileAccess.open(path, FileAccess.READ)
    if file == null:
        return null

    var json = file.get_as_text()
    var data = JSON.parse_string(json)
    if data:
        return GameData.SongData.from_dict(data)

    return null


func search_songs(query: String) -> Array[GameData.SongData]:
    if query.is_empty():
        return songs

    query = query.to_lower()
    var result = []
    for song in songs:
        if song.name.to_lower().contains(query) or song.artist.to_lower().contains(query):
            result.append(song)
    return result


func add_song(song: GameData.SongData) -> void:
    songs.append(song)
    if song.is_imported:
        imported_songs.append(song)
    on_library_updated.emit()


func toggle_favorite(song_id: String) -> void:
    var song = find_song_by_id(song_id)
    if song == null:
        return

    song.is_favorite = not song.is_favorite

    var player_data = GameData.PlayerData.load_data()
    if song.is_favorite:
        if not song_id in player_data.favorite_songs:
            player_data.favorite_songs.append(song_id)
    else:
        var idx = player_data.favorite_songs.find(song_id)
        if idx >= 0:
            player_data.favorite_songs.remove_at(idx)
    player_data.save()

    on_library_updated.emit()


func find_song_by_id(song_id: String) -> GameData.SongData:
    for song in songs:
        if song.id == song_id:
            return song
    return null
