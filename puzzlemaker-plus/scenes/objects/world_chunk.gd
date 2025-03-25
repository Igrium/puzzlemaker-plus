## Renders a 16x16x16 chunk of voxels in the world.

class_name WorldChunk
extends MeshInstance3D

## The chunk's position in chunk coordinates (world coords / 16)
@export
var pos := Vector3i(0, 0, 0)

@export
var material : Material

@export
var edge_mesh_instace: MeshInstance3D

## Called when the user clicks on the geometry in this chunk.
signal on_input_event(camera: Node, event: InputEvent, event_position: Vector3, normal: Vector3, shape_idx: int)

@onready
var _collision_shape: CollisionShape3D = $Area3D/CollisionShape3D

# var _quad_mesh: QuadMesh

func _ready() -> void:
	Editor.connect("OnUpdatedSelection", _on_updated_selection)
	render()
	pass

func render() -> void:
	var shape = ConcavePolygonShape3D.new()
	
	var edge_mesh: ArrayMesh
	if edge_mesh_instace != null:
		edge_mesh = edge_mesh_instace.mesh
		if edge_mesh == null:
			edge_mesh = ArrayMesh.new()
			edge_mesh_instace.mesh = edge_mesh
	
	if mesh == null:
		mesh = ArrayMesh.new()

	var generator: WorldMeshGenerator = WorldMeshGenerator.Create(mesh, shape, pos, true)
	generator.EdgeMesh = edge_mesh

	generator.DoGreedyMeshThreaded()
	await generator.QuadsComputed
	_collision_shape.shape = shape

	# var edge_mesh: ArrayMesh
	# if edge_mesh_instace != null:
	# 	edge_mesh = ArrayMesh.new()
	# 	generator.EdgeMesh = edge_mesh

	# generator.DoGreedyMeshThreaded()
	
	# _quad_mesh = await generator.QuadsComputed
	# self.mesh = a_mesh
	# _collision_shape.shape = shape
	
	# if edge_mesh_instace != null:
	# 	await generator.EdgeModelGenerated
	# 	edge_mesh_instace.mesh = edge_mesh

func _on_input_event(camera: Node, event: InputEvent, event_position: Vector3, normal: Vector3, shape_idx: int) -> void:
	on_input_event.emit(camera, event, event_position, normal, shape_idx)

func _on_updated_selection(selection: AABB):
	selection = selection.grow(.001) # avoid z-fighting

	set_instance_shader_parameter("selection_start", selection.position)
	set_instance_shader_parameter("selection_end", selection.end)
	
