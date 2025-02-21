extends Node


func _input(_event: InputEvent) -> void:
	if Input.is_action_just_pressed("toggle_portalability"):
		$Operations.TogglePortalability()
	if Input.is_action_just_pressed("extend"):
		$Operations.FillSelection()
	if Input.is_action_just_pressed("retract"):
		$Operations.EmptySelection()

func _on_main_ui_on_undo() -> void:
	$Operations.Undo()


func _on_main_ui_on_redo() -> void:
	$Operations.Redo()
