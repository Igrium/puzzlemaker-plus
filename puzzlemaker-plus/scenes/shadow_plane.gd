extends MeshInstance3D

var _image := Image.create(4, 4, false, Image.FORMAT_L8)
var _texture: ImageTexture

func _ready() -> void:
	Editor.connect("OnChunksUpdated", _on_chunks_updated)

func _on_chunks_updated(_updated_chunks: PackedVector3Array):
	var bounds: AABB = Editor.GetWorldBounds()

	var origin = Vector3(bounds.position.x + bounds.size.x / 2, bounds.position.y - 4, bounds.position.z + bounds.size.z / 2)
	self.position = origin

	#var tex_gen = ShadowTextureGenerator.new()
	#tex_gen.connect("GenerationComplete", _on_generation_complete)
	#tex_gen.GenerateShadowMaskAsync(int(origin.y))

	if mesh is PlaneMesh:
		mesh.size = Vector2(bounds.size.x, bounds.size.z)
	else:
		push_warning("mesh should be plane mesh")
	
func _on_generation_complete(width: int, height: int, data: PackedByteArray):
	_image.set_data(width, height, false, Image.FORMAT_L8, data)
	if _texture == null:
		_texture = ImageTexture.create_from_image(_image)
	else:
		_texture.set_image(_image)

	var mat := mesh.surface_get_material(0) as ShaderMaterial
	mat.set_shader_parameter("shadow_mask", _texture)
	
