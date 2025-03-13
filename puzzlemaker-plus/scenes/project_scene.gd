extends Node3D

## Re-created for every opened project to store nodes related to said project.
class_name ProjectScene

var project: PuzzlemakerProject

func _ready() -> void:
	if (project.World.IsEmpty()):
		project.AddInitialVoxels()
