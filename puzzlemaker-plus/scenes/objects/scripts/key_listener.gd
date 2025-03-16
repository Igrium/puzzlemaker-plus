extends Node


func _unhandled_input(event: InputEvent) -> void:
	if (event.is_pressed()):
		if InputMap.action_has_event("toggle_portalability", event):
			$Operations.TogglePortalability()
		if InputMap.action_has_event("extend", event):
			_extend_retract(true)
		if InputMap.action_has_event("retract", event):
			_extend_retract(false)

func _extend_retract(extend: bool):
	var selection: AABB = Editor.Selection
	if selection.has_volume():
		if extend:
			$Operations.FillSelection()
		else:
			$Operations.EmptySelection()
	elif selection.has_surface():
		$Operations.ExtrudeSelection(extend)
	else:
		push_warning("No selection")
