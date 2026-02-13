extends TextureRect

class_name ItemIcon

var item_type: ItemTypeProxy

var item_init_scene := preload("res://ui/item_browser/item_init.tscn")

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.is_pressed() and event.button_index == MOUSE_BUTTON_LEFT:
			var node := item_init_scene.instantiate() as ItemInit
			node.item_type = item_type
			node.update_model()
			$"../../../..".add_child(node)
