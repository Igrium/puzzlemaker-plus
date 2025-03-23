shader_type spatial;
render_mode blend_mul;

uniform sampler2D shadow_mask;

void fragment() {
	ALBEDO = texture(shadow_mask, UV).rgb;
}

