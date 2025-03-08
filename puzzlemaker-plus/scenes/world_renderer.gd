extends Node3D

@export var chunk_scene := preload("res://scenes/world_chunk.tscn")

var chunks: Dictionary[Vector3i, WorldChunk] = {}

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	Editor.connect("OnOpenProject", _on_open_project)
	Editor.connect("OnChunksUpdated", _on_chunks_updated)

	Editor.NewProject()
	Editor.AddTestVoxels()

func _on_open_project(_project):
	clear()

func _on_chunks_updated(updated_chunks: PackedVector3Array):
	for pos in updated_chunks:
		var chunk_pos := Vector3i(pos)
		var chunk = _get_chunk(chunk_pos)
		chunk.render()
	

func _get_chunk(pos: Vector3i) -> WorldChunk:
	var chunk: WorldChunk
	if chunks.has(pos):
		chunk = chunks[pos]
	else:
		chunk = _create_chunk(pos)
		chunks[pos] = chunk

	return chunk

func _create_chunk(pos: Vector3i) -> WorldChunk:
	var chunk: WorldChunk = chunk_scene.instantiate()
	chunk.pos = pos
	chunk.position = pos * 16
	chunk.connect("on_input_event", _on_input_event)
	add_child(chunk)
	return chunk

func clear():
	for chunk in chunks.values():
		chunk.queue_free()
	chunks.clear()

func _on_input_event(_camera: Node, event: InputEvent, event_position: Vector3, normal: Vector3, _shape_idx: int) -> void:
	if event is InputEventMouseButton:
		if event.pressed and event.button_index == 1:
			Editor.SelectRaycast(event_position, normal, event.shift_pressed)
