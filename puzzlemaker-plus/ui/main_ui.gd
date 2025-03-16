extends Control

# signal on_open_test_vmf(path: String)

# signal on_undo
# signal on_redo

func _export_scene_gltf() -> void:
	var gltf_document_save := GLTFDocument.new()
	var gltf_state_save := GLTFState.new()
	gltf_document_save.append_from_scene(get_tree().current_scene, gltf_state_save)

	gltf_document_save.write_to_filesystem(gltf_state_save, "user://scene.gltf")
	print("Wrote gltf to " + ProjectSettings.globalize_path("user://scene.gltf"))


func _on_new_project_pressed() -> void:
	$UnsavedConfirmationDialog.confirm_saved(Editor.NewProject)


func _on_open_project_pressed() -> void:
	$UnsavedConfirmationDialog.confirm_saved(func () : $FD_OpenProject.visible = true)


func _on_fd_open_project_file_selected(path: String) -> void:
	Editor.OpenProjectFile(path)


func _on_save_pressed() -> void:
	if Editor.HasProjectName:
		Editor.SaveProject()
	else:
		_on_save_as_pressed()


func _on_save_as_pressed() -> void:
	$FD_SaveProject.visible = true


func _on_fd_save_project_file_selected(path: String) -> void:
	Editor.SaveProjectAs(path)


func _on_export_vmf_pressed() -> void:
	$FD_VMFExport.visible = true


func _on_fd_vmf_export_file_selected(path: String) -> void:
	VMFExporter.new().ExportVMF(path)


func _on_undo_pressed() -> void:
	Editor.Undo()


func _on_redo_pressed() -> void:
	Editor.Redo()


func _on_add_test_item_pressed() -> void:
	Editor.AddItem("testitem", Vector3(0, 0, 6))


func _on_export_gltf_pressed() -> void:
	var gltf_document_save := GLTFDocument.new()
	var gltf_state_save := GLTFState.new()
	gltf_document_save.append_from_scene(get_tree().root, gltf_state_save)

	gltf_document_save.write_to_filesystem(gltf_state_save, "user://scene.gltf")
	print("Wrote gltf to " + ProjectSettings.globalize_path("user://scene.gltf"))


func _on_select_all_pressed() -> void:
	Editor.SelectAll()


func _on_select_all_items_pressed() -> void:
	Editor.SelectAllItems()


func _on_select_none_pressed() -> void:
	Editor.ClearSelection()
