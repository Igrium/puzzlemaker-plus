extends Node


func _input(_event: InputEvent) -> void:
	if Input.is_action_just_pressed("toggle_portalability"):
		$Operations.TogglePortalability()


func _on_main_ui_on_undo() -> void:
	$Operations.Undo()


func _on_main_ui_on_redo() -> void:
	$Operations.Redo()
