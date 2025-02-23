extends Node


func _input(_event: InputEvent) -> void:
	if Input.is_action_just_pressed("toggle_portalability"):
		$Operations.TogglePortalability()
	if Input.is_action_just_pressed("extend"):
		_extend_retract(true)
	if Input.is_action_just_pressed("retract"):
		_extend_retract(false)

func _on_main_ui_on_undo() -> void:
	$Operations.Undo()


func _on_main_ui_on_redo() -> void:
	$Operations.Redo()

func _extend_retract (extend: bool):
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
