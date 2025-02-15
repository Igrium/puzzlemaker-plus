extends Control

signal on_open_test_vmf(path: String)

func _on_file_id_pressed(id: int) -> void:
	if id == 0:
		_open_test_vmf()

func _open_test_vmf():
	$FD_VMFTest.popup()
	pass


func _on_fd_vmf_test_file_selected(path: String) -> void:
	on_open_test_vmf.emit(path)
