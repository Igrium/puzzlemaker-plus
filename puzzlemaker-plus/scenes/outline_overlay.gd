extends Node

@export var stencil_viewport: SubViewport
@export var stencil_camera: Camera3D

func _process(_delta: float) -> void:
	var viewport := get_viewport()
	var camera := viewport.get_camera_3d()
	
	if (stencil_viewport.size != viewport.size):
		stencil_viewport.size = viewport.size;
	
	if camera:
		stencil_camera.fov = camera.fov
		stencil_camera.global_transform = camera.global_transform
	
