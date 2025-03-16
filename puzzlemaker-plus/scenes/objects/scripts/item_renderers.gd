extends Node3D

@export var item_renderer_scene := preload("res://scenes/objects/item_renderer.tscn")

var items: Dictionary[Item, ItemRenderer] = {}

func _ready() -> void:
	var project: PuzzlemakerProject = $"..".project
	
	$"..".project.ItemAdded.connect(_on_item_added)
	$"..".project.ItemRemoved.connect(_on_item_removed)
	
	for item: Item in project.GetItems():
		_on_item_added(item)
	
func _on_item_added(item: Item):
	var renderer = _create_item_renderer(item)
	add_child(renderer)
	items[item] = renderer

func _on_item_removed(item: Item):
	var renderer = items.get(item, null)
	if (renderer != null):
		items[item] = null
		renderer.queue_free()

func _clear():
	items.clear()
	for n in get_children():
		n.queue_free()

func _create_item_renderer(item: Item) -> ItemRenderer:
	var scene: ItemRenderer = item_renderer_scene.instantiate()
	scene.set_item(item)
	scene.name = item.ID
	return scene
