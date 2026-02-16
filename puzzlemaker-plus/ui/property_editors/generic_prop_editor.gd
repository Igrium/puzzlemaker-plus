extends HBoxContainer


func _on_prop_editor_handle_property_changed(oldVal: String, newVal: String) -> void:
	$LineEdit.text = newVal

func _on_line_edit_text_submitted(new_text: String) -> void:
	$PropEditorHandle.SetPropValue(new_text)

func _on_line_edit_tree_exiting() -> void:
	$PropEditorHandle.SetPropValue($LineEdit.text)
