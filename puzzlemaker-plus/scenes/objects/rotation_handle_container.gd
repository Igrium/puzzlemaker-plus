extends Node3D

@export var rotation_handle_scene: PackedScene
@export var rotation_handle_90_scene: PackedScene

var _has_init = false

func _on_item_renderer_set_selected(selected: bool) -> void:
	if selected:
		if !_has_init:
			_init_handle()
		self.visible = true
	else:
		self.visible = false

func _init_handle():
	var item: Item = $"..".item
	var mode := item.GetRotationMode()

	var node: RotationGizmo = null
	if mode == 1:
		node = rotation_handle_90_scene.instantiate()
	elif mode == 2:
		node = rotation_handle_scene.instantiate()
	
	if node != null:
		node.Target = $".."
		node.DragDropped.connect(_on_complete_rotation)
		add_child(node)

	_has_init = true

func _on_complete_rotation(node: Node3D, _angle: float, rot: Basis):
	Editor.MoveItem($"..".item, node.global_position, rot.get_euler())
