@tool
#class_name CollisionGenerator
extends Node
class_name CollisionGenerator

@export var base_node: Node
@export_tool_button("Generate Collision", "Callable") var generate_action = generate_collision

func generate_collision():
	if base_node == null:
		push_warning("Please set base node.")
		return;
	
	_generate_collision_recursive(base_node)
	
func _generate_collision_recursive(base: Node):
	for child in base.get_children():
		if child is MeshInstance3D:
			var collision = CollisionShape3D.new()
			collision.shape = child.mesh.create_convex_shape()
			
			# Add to child first so we inherit its transform
			child.add_child(collision)
			collision.reparent(base_node)
			
			if (Engine.is_editor_hint()):
				var scene = get_tree().get_edited_scene_root()
				collision.set_owner(scene)
			
		_generate_collision_recursive(child)

func _reparent_collision_children(mesh: Node, base: Node):
	for child in mesh.get_children():
		if (child is CollisionObject3D):
			for child2 in child.get_children():
				child2.reparent(base)
			if !Engine.is_editor_hint():
				child.queue_free()
