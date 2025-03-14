## Simplifies the creation of menus by allowing each menu item to be a node.
## NOTE: only works when menu is created statically.

class_name PopupMenuItem
extends Node

var _index: int

signal focused
signal pressed

## The text to use as this menu item's label.
@export var text: String

## The name of a binding to use as this menu item's keyboard shortcut
@export var accelerator: StringName

## Add a separator atop this item.
@export var separator := false

func _ready() -> void:
	var parent: PopupMenu = get_parent()
	assert(parent is PopupMenu, "PopupMenuItem must be a direct child of a PopupMenu!")

	parent.id_focused.connect(_on_parent_id_focused)
	parent.id_pressed.connect(_on_parent_id_pressed)
	
	if text == null:
		text = get_name()
	
	if (separator):
		parent.add_separator()

	_index = parent.item_count;
	parent.add_item(text, _index)
	
	if accelerator:
		var shortcut = Shortcut.new()
		shortcut.events.append_array(InputMap.action_get_events(accelerator))
		parent.set_item_shortcut(_index, shortcut)

func _on_parent_id_focused(id: int):
	if id == _index:
		focused.emit()

func _on_parent_id_pressed(id: int):
	if id == _index:
		pressed.emit()
