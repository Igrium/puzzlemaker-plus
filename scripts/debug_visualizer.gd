extends Node

func _input(event):
	if event is InputEventKey and Input.is_key_pressed(KEY_F10):
		var vp = get_viewport()
		vp.debug_draw = (vp.debug_draw + 1) % 5
		print("Set debug view mode: %s" % vp.debug_draw)
