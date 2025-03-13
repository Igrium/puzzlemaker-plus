extends Node3D
class_name ItemRenderer

const ITEM_RENDERER_SCENE := preload("res://scenes/item_renderer.tscn")

@export var placeholder_mesh := preload("res://assets/models/placeholder_cube.tscn")

var _editor_model: Node

var _item: Item

func set_item(item: Item):
	if (is_instance_valid(_item)):
		push_warning("Item renderer already has an item instance. Behavior may be unexpected.")
	
	_item = item
	item.UpdatePosition.connect(_update_position)
	item.UpdateRotation.connect(_update_rotation)
	
	position = item.Position
	rotation = item.Rotation
	update_model()

func _update_position(_old_pos: Vector3, new_pos: Vector3):
	self.position = new_pos

func _update_rotation(_old_rot: Vector3, new_rot: Vector3):
	self.rotation = new_rot 

func update_model():
	if (is_instance_valid(_editor_model)):
		_editor_model.queue_free()
		_editor_model = null

	var model_name: String = _item.GetEditorModel()

	var model: PackedScene = null

	if (model_name != null):
		print("Loading model " + model_name)
		model = PackageManager.LoadModel(model_name)
	
	if (model == null):
		model = placeholder_mesh
	
	_editor_model = model.instantiate()
	add_child(_editor_model)
