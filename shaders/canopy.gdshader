shader_type spatial;
render_mode cull_disabled, specular_schlick_ggx;

#include "res://shaders/tree_wind.gdshaderinc"

uniform sampler2D leaf_cluster : source_color, filter_linear_mipmap, repeat_disable;
varying flat float atlas_tile;

void vertex() {
	atlas_tile = UV2.x;
	VERTEX += leaf_wind(VERTEX, NORMAL, UV, vec2(0, UV2.y), MODEL_MATRIX, TIME);
}

void fragment() {
	vec2 tile = vec2(mod(atlas_tile, 2.0), floor(atlas_tile / 2.0));
	vec4 foliage = texture(leaf_cluster, (tile + vec2(0.016) + UV * 0.968) * 0.5);
	if (foliage.a < 0.35) {
		discard;
	}
	vec3 tint = plant_color(COLOR.rgb, OUTPUT_IS_SRGB);
	ALBEDO = tint * (0.68 + foliage.r * 0.71);
	EMISSION = tint * 0.025;
	BACKLIGHT = tint * 0.30;
	ROUGHNESS = 0.85;
	SPECULAR = 0.20;
}
