shader_type canvas_item;

uniform vec4 line_color : source_color = vec4(1, 1, 0, .5);
uniform float line_thickness : hint_range(0, 10) = 1.0;

void fragment() {
	vec2 size = TEXTURE_PIXEL_SIZE * line_thickness;

	float outline = texture(TEXTURE, UV + vec2(-size.x, 0)).a;
	outline += texture(TEXTURE, UV + vec2(0, size.y)).a;
	outline += texture(TEXTURE, UV + vec2(size.x, 0)).a;
	outline += texture(TEXTURE, UV + vec2(0, -size.y)).a;
	outline += texture(TEXTURE, UV + vec2(-size.x, size.y)).a;
	outline += texture(TEXTURE, UV + vec2(size.x, size.y)).a;
	outline += texture(TEXTURE, UV + vec2(-size.x, -size.y)).a;
	outline += texture(TEXTURE, UV + vec2(size.x, -size.y)).a;

	outline = min(outline, 1.0);

	vec4 color = texture(TEXTURE, UV);
	float alpha = outline - color.a;

	COLOR = line_color;
	COLOR.a *= alpha;
	//COLOR = vec4(line_color, outline - color.a);
	//COLOR = texture(TEXTURE, UV);

}