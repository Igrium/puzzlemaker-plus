extends Control


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	Packages.PackagesReloaded.connect(_on_packages_reloaded)

func _on_packages_reloaded(_initial: bool):
	_clear_children($ScrollContainer/GridContainer)
	var items: Dictionary[String, ItemTypeProxy] = Packages.GetItemTypes()

	for name in items:
		var item := items[name]		
		var thumb: Texture2D = Packages.LoadImageTexture(item.Thumbnail)
		if !thumb:
			continue
		
		var rect = TextureRect.new()
		rect.texture = thumb
		rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		rect.custom_minimum_size = Vector2(96, 96)
		
		$ScrollContainer/GridContainer.add_child(rect)

func _clear_children(parent: Node):
	for n in parent.get_children():
		n.queue_free()
