extends Node3D
class_name ItemRenderer

signal set_selected(selected: bool)

const ITEM_RENDERER_SCENE := preload("res://scenes/objects/item_renderer.tscn")
@export var placeholder_mesh := preload("res://assets/models/placeholder_cube.tscn")

var _editor_model: Node
var editor_model: Node:
	get:
		return _editor_model

var _item: Item
var item: Item:
	get:
		return _item

var selected: bool = false:
	get:
		return selected
	set(value):
		selected = value
		_on_set_selected(value)

func _ready() -> void:
	Editor.connect("UpdatedSelectedItems", _on_updated_item_selection)

@warning_ignore("shadowed_variable")
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
	
	for child in $Area3D.get_children():
		child.queue_free()

	var model_name: String = item.GetEditorModel()

	var model: PackedScene = null

	if (model_name != null):
		print("Loading model " + model_name)
		model = PackageManager.LoadModel(model_name)
	
	if (model == null):
		model = placeholder_mesh
	
	_editor_model = model.instantiate()
	$Area3D.add_child(_editor_model)
	# There's a weird issue where transforms from the scene aren't respected in the function it's instantiated.
	# Call this deferred to let them initialize properly.
	$CollisionGenerator.generate_collision.call_deferred()

func _on_updated_item_selection(items: Array[Item]):
	if _item == null:
		return
	
	selected = items.has(_item)

func _on_set_selected(is_selected: bool):
	set_selected.emit(is_selected)
	$OutlineRenderer.outline_enabled = is_selected
	
func _on_area_3d_input_event(_camera: Node, event: InputEvent, _event_position: Vector3, _normal: Vector3, _shape_idx: int) -> void:
	if event is InputEventMouseButton:
		if event.pressed and event.button_index == 1:
			Editor.SelectItem(item, event.shift_pressed)
			$Draggable.StartDragging()


func _on_draggable_drag_dropped(_node: Node3D, pos: Vector3, rot: Vector3) -> void:
	Editor.MoveItem(item, pos, rot)


func _on_rotation_handle_drag_dropped(_node: Node3D, _angle: float, rot: Basis) -> void:
	Editor.MoveItem(item, global_position, rot.get_euler())
