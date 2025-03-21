extends Node

func _on_input_event(_camera: Node, event: InputEvent, _event_position: Vector3, _normal: Vector3, _shape_idx: int) -> void:
	if event is InputEventMouseButton:
		if event.pressed and event.button_index == 1:
			$"..".StartDragging()


func _on_rotation_handle_drag_started(_node: Node3D) -> void:
	$"../rotation".visible = true


func _on_rotation_handle_drag_dropped(_node: Node3D, _angle: float, _rotation: Basis) -> void:
	$"../rotation".visible = false
