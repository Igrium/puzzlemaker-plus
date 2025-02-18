## Renders a 16x16x16 chunk of voxels in the world.

class_name WorldChunk
extends MeshInstance3D

## The chunk's position in chunk coordinates (world coords / 16)
@export
var pos := Vector3i(0, 0, 0)

@export
var material : Material

## Called when the user clicks on the geometry in this chunk.
signal on_input_event(camera: Node, event: InputEvent, event_position: Vector3, normal: Vector3, shape_idx: int)

@onready
var _collision_shape: CollisionShape3D = $Area3D/CollisionShape3D


func _ready() -> void:
	Editor.connect("OnOpenProject", render)
	Editor.connect("OnUpdatedSelection", _on_updated_selection)
	render()
	pass

func render() -> void:
	var a_mesh = ArrayMesh.new()
	var shape = ConcavePolygonShape3D.new()
	
	var world: PuzzlemakerWorld = Editor.World
	
	if (world):
		world.RenderChunkAndCollision(a_mesh, shape, pos * 16, 16, true)
		# var surface_index = a_mesh.get_surface_count() - 1
		# if (surface_index >= 0):
		# 	a_mesh.surface_set_material(surface_index, material)
		
		self.mesh = a_mesh
		_collision_shape.shape = shape
	else:
		push_warning("No world found")

func _on_input_event(camera: Node, event: InputEvent, event_position: Vector3, normal: Vector3, shape_idx: int) -> void:
	on_input_event.emit(camera, event, event_position, normal, shape_idx)

func _on_updated_selection(selection: AABB):
	selection = selection.grow(.001) # avoid z-fighting
	var local_start := selection.position - Vector3(pos);
	var local_end := selection.end - Vector3(pos)

	set_instance_shader_parameter("selection_start", local_start)
	set_instance_shader_parameter("selection_end", local_end)
	
