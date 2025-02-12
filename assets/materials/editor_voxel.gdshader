shader_type spatial;

uniform sampler2D tex : source_color;

instance uniform vec3 selection_start;
instance uniform vec3 selection_end;

uniform vec3 tint_color : source_color = vec3(1, 1, 0);

varying vec3 pos;

void vertex() {
	pos = VERTEX;
}

void fragment() {
	vec3 color = texture(tex, UV).rgb;
	
	bool selected = selection_start.x <= pos.x && pos.x <= selection_end.x
				&& selection_start.y <= pos.y && pos.y <= selection_end.y
				&& selection_start.z <= pos.z && pos.z <= selection_end.z;
		
	
	ALBEDO = color;
	EMISSION = selected ? tint_color : vec3(0, 0, 0);
	// Called for every pixel the material is visible on.
}
