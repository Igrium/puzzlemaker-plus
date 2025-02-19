extends Control

signal on_open_test_vmf(path: String)

signal on_undo
signal on_redo

func _ready():
	$MenuBar/Edit.set_item_shortcut(0, _make_shortcut(KEY_Z, true))
	$MenuBar/Edit.set_item_shortcut(1, _make_shortcut(KEY_Z, true, true))

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

func _make_shortcut(key: Key, ctrl_pressed: bool = true, shift_pressed: bool = false) -> Shortcut:
	var shortcut = Shortcut.new()
	var inputevent = InputEventKey.new()
	inputevent.keycode = key
	inputevent.ctrl_pressed = ctrl_pressed
	inputevent.shift_pressed = shift_pressed
	shortcut.events.append(inputevent)
	return shortcut


func _on_debug_index_pressed(index: int) -> void:
	if index == 0:
		$ThreadingTest.DoThreadingTest()
