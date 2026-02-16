extends PopupPanel


func _on_popup_hide() -> void:
	queue_free() # Context windows are re-created each time.
