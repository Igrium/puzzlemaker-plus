@tool
#class_name CollisionGenerator
extends Node
class_name CollisionGenerator

@export var base_node: Node
## If set and there are collision shapes in the node's children, pull them out instead of generating more.
@export var allow_yoink: bool
@export_tool_button("Generate Collision", "Callable") var generate_action = generate_collision

func generate_collision():
	if base_node == null:
		push_warning("Please set base node.")
		return;
	
	var did_yoink: bool = false
	if (allow_yoink):
		for child in base_node.get_children():
			if _reparent_collision_children(child, base_node):
				did_yoink = true
	
	if !did_yoink:
		_generate_collision_recursive(base_node)

## TODO: can this whole thing be optimized?
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

func _has_collision_shape(node: Node) -> bool:

	for child in node.get_children():
		if child is CollisionShape3D or _has_collision_shape(child):
			return true
	
	return false

func _reparent_collision_children(node: Node, base: Node) -> bool:
	var success := false
	for child in node.get_children():
		if child is CollisionShape3D:
			child.reparent(base)
			success = true
		if _reparent_collision_children(child, base):
			success = true
	
	return success
#func _reparent_collision_children(node: Node, base: Node) -> bool:
	#for child in node.get_children():
		#if (child is CollisionObject3D):
			#for child2 in child.get_children():
				#child2.reparent(base)
			#if !Engine.is_editor_hint():
				#child.queue_free()
