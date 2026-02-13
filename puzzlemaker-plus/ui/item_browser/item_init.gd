extends Sprite2D

class_name ItemInit

signal completed(success: bool, pos: Vector3)

var ItemType: ItemTypeProxy

func _process(delta: float) -> void:
	$SubViewport/Subject.rotate(Vector3(0, 1, 0), delta)
	var mouse_pos = get_viewport().get_mouse_position()
	global_position = mouse_pos
	
	# Update raycast data
	var cam := get_viewport().get_camera_3d()
	
	var start := cam.project_ray_origin(mouse_pos)
	var end = cam.project_ray_normal(mouse_pos) * 4096
	
	$Raycast.global_position = start
	$Raycast.target_position = end
	
	$Raycast.force_raycast_update()
	
	if $Raycast.is_colliding():
		completed.emit(true, $Raycast.get_collision_point())
	pass

func _input(event: InputEvent) -> void:
	var e := event as InputEventMouseButton
	if e and e.button_index == 0 and !e.pressed:
		completed.emit(false, Vector3(0,0,0))
	
