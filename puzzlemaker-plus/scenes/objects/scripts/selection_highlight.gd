extends MeshInstance3D

var i_mesh := ImmediateMesh.new()
var material := ORMMaterial3D.new()

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	self.mesh = i_mesh
	Editor.connect("OnUpdatedSelection", update_selection)
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = Color.YELLOW


func update_selection(selection: AABB):
	i_mesh.clear_surfaces()

	var grid_scale: int = Editor.GridScale

	var size := selection.size
	var flatten := Vector3(1 if size.x != 0 else 0, 1 if size.y != 0 else 0, 1 if size.z != 0 else 0)

	_draw_selection_highlight(i_mesh, _grow_box(selection, flatten * (.01 * grid_scale)), material)
	_draw_selection_highlight(i_mesh, _grow_box(selection, flatten * (-.01 * grid_scale)), material)
	self.mesh = i_mesh

static func _draw_selection_highlight(m: ImmediateMesh, selection: AABB, mat: Material = null):
	@warning_ignore("shadowed_global_identifier")
	var min: Vector3 = selection.position
	@warning_ignore("shadowed_global_identifier")
	var max: Vector3 = selection.end
	
	m.surface_begin(Mesh.PRIMITIVE_LINES, mat)

	_draw_line(m, min, Vector3(max.x, min.y, min.z))
	_draw_line(m, min, Vector3(min.x, max.y, min.z))
	_draw_line(m, min, Vector3(min.x, min.y, max.z))

	_draw_line(m, max, Vector3(min.x, max.y, max.z))
	_draw_line(m, max, Vector3(max.x, min.y, max.z))
	_draw_line(m, max, Vector3(max.x, max.y, min.z))
	
	_draw_line(m, Vector3(min.x, min.y, max.z), Vector3(max.x, min.y, max.z))
	_draw_line(m, Vector3(max.x, min.y, min.z), Vector3(max.x, min.y, max.z))

	_draw_line(m, Vector3(min.x, max.y, min.z), Vector3(max.x, max.y, min.z))
	_draw_line(m, Vector3(min.x, max.y, min.z), Vector3(min.x, max.y, max.z))

	_draw_line(m, Vector3(min.x, min.y, max.z), Vector3(min.x, max.y, max.z))
	_draw_line(m, Vector3(max.x, min.y, min.z), Vector3(max.x, max.y, min.z))

	m.surface_end()

static func _draw_line(m: ImmediateMesh, start: Vector3, end: Vector3):
	m.surface_add_vertex(start)
	m.surface_add_vertex(end)

static func _grow_box(box: AABB, by: Vector3) -> AABB:
	box.position.x -= by.x
	box.position.y -= by.y
	box.position.z -= by.z

	box.size.x += by.x * 2
	box.size.y += by.y * 2
	box.size.z += by.z * 2
	return box
