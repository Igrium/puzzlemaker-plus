extends Node

func _input(event):
	if event is InputEventKey and Input.is_key_pressed(KEY_F10):
		var vp = get_viewport()
		vp.debug_draw = (vp.debug_draw + 1) % 5
		print("Set debug view mode: %s" % vp.debug_draw)
	elif event is InputEventKey and Input.is_key_pressed(KEY_F4):
		export_scene()

func export_scene():
	var gltf_document_save := GLTFDocument.new()
	var gltf_state_save := GLTFState.new()
	gltf_document_save.append_from_scene(get_parent(), gltf_state_save)

	gltf_document_save.write_to_filesystem(gltf_state_save, "user://scene.gltf")
	print("Wrote gltf to " + ProjectSettings.globalize_path("user://scene.gltf"))
