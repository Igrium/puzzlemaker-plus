## Draws an outline on all mesh instances 
extends Node

## Outline applies to all children of this node.
@export var root_node: Node

@export var outline_material: Material

# Meshes need to have their materials duplicated before an outline can be added.
# keep track of meshes with duplicated materials here.
var _processed_meshes: Dictionary[WeakRef, bool]


var outline_enabled := false:
	get:
		return outline_enabled
	set(value):
		outline_enabled = value
		update_outline()

func update_outline():
	if outline_enabled:
		_enable_outline(root_node)
	else:
		_disable_outline(root_node)

func _enable_outline(node: Node):
	if node is MeshInstance3D:
		for i in node.mesh.get_surface_count():
			var override = _get_or_create_surface_override(node, i)
			override.next_pass = outline_material
	
	for child in node.get_children():
		_enable_outline(child)


func _disable_outline(node: Node):
	if node is MeshInstance3D:
		for i in node.get_surface_override_material_count():
			var override = node.get_surface_override_material(i)
			if (override != null):
				override.next_pass = null
	
	for child in node.get_children():
		_disable_outline(child)


func _get_or_create_surface_override(node: MeshInstance3D, index: int) -> Material:
	var override := node.get_surface_override_material(index)
	if (override == null):
		var mat = node.mesh.surface_get_material(index)
		if (mat == null):
			push_warning("Mesh had no materials.")
			override = StandardMaterial3D.new()
		else:
			override = mat.duplicate()
		
		node.set_surface_override_material(index, override)
	
	return override
