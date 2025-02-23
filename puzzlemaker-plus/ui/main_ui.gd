extends Control

signal on_open_test_vmf(path: String)

signal on_undo
signal on_redo

func _ready():
	$MenuBar/Edit.set_item_shortcut(0, _make_shortcut("ui_undo"))
	$MenuBar/Edit.set_item_shortcut(1, _make_shortcut("ui_redo"))

func _on_file_id_pressed(id: int) -> void:
	if id == 0:
		_open_test_vmf()

func _open_test_vmf():
	$FD_VMFTest.popup()
	pass


func _on_fd_vmf_test_file_selected(path: String) -> void:
	on_open_test_vmf.emit(path)


func _on_edit_index_pressed(index: int) -> void:
	if index == 0:
		on_undo.emit()
	elif index == 1:
		on_redo.emit()

func _make_key_shortcut(key: Key, ctrl_pressed: bool = true, shift_pressed: bool = false) -> Shortcut:
	var shortcut = Shortcut.new()
	var inputevent = InputEventKey.new()
	inputevent.keycode = key
	inputevent.ctrl_pressed = ctrl_pressed
	inputevent.shift_pressed = shift_pressed
	shortcut.events.append(inputevent)
	return shortcut

func _make_shortcut(action: StringName) -> Shortcut:
	var shortcut = Shortcut.new()
	shortcut.events.append_array(InputMap.action_get_events(action))
	return shortcut

func _on_debug_index_pressed(index: int) -> void:
	if index == 0:
		_export_scene_gltf()

func _export_scene_gltf() -> void:
	var gltf_document_save := GLTFDocument.new()
	var gltf_state_save := GLTFState.new()
	gltf_document_save.append_from_scene(get_tree().current_scene, gltf_state_save)

	gltf_document_save.write_to_filesystem(gltf_state_save, "user://scene.gltf")
	print("Wrote gltf to " + ProjectSettings.globalize_path("user://scene.gltf"))
