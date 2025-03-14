extends Node3D

## The scene that will be instantiated when a project is opened.
@export var project_packed_scene: PackedScene
@export var editor_theme: EditorTheme

var _project_scene: ProjectScene

func _ready() -> void:
	Editor.connect("OnOpenProject", _on_open_project)
	_on_open_project(Editor.Project)
	
func _on_open_project(project: PuzzlemakerProject):
	if (is_instance_valid(_project_scene)):
		remove_child(_project_scene)
		_project_scene.queue_free()
	
	_project_scene = project_packed_scene.instantiate()
	_project_scene.project = project
	add_child(_project_scene)
