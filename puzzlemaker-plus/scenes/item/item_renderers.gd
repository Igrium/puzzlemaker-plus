extends Node3D

var items: Dictionary[Item, ItemRenderer] = {}

func _ready() -> void:
	Editor.OnOpenProject.connect(_on_open_project)
	_on_open_project(Editor.Project) # open the initial project

func _on_open_project(project: PuzzlemakerProject):
	_clear()
	project.ItemAdded.connect(_on_item_added)
	project.ItemRemoved.connect(_on_item_removed)
	
func _on_item_added(item: Item):
	var renderer = ItemRenderer.create_item_renderer(item)
	add_child(renderer)
	items[item] = renderer
	print("Added item " + item.ID)

func _on_item_removed(item: Item):
	var renderer = items.get(item, null)
	if (renderer != null):
		items[item] = null
		renderer.queue_free()

func _clear():
	items.clear()
	for n in get_children():
		n.queue_free()
