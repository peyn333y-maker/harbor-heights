shader_type spatial;
render_mode cull_disabled, specular_schlick_ggx;

#include "res://shaders/tree_wind.gdshaderinc"

varying vec3 leaf_point;
varying float leaf_distance;

void vertex() {
	leaf_point = VERTEX;
	leaf_distance = distance((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz, CAMERA_POSITION_WORLD);
	VERTEX += leaf_wind(VERTEX, NORMAL, UV, UV2, MODEL_MATRIX, TIME);
}

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

void fragment() {
	vec3 tint = plant_color(COLOR.rgb, OUTPUT_IS_SRGB);
	vec2 leaf_uv = clamp(UV, vec2(0.0), vec2(1.0));
	float fine = 1.0 - smoothstep(4.0, 17.0, leaf_distance);
	float width = mix(0.125, 0.009, fine);
	float midrib = (1.0 - smoothstep(0.014 + width, 0.062 + width, abs(leaf_uv.x - 0.5))) * (1.0 - leaf_uv.y * 0.66);
	float vein_shift = leaf_uv.y * 10.5 - abs(leaf_uv.x - 0.5) * 1.30;
	float veins = (1.0 - smoothstep(0.032, 0.090, abs(fract(vein_shift) - 0.5))) * fine;
	float grain = (hash12(floor((leaf_point.xz + leaf_point.yy * vec2(0.71, 0.31)) * 175.0)) - 0.5) * fine;
	float edge = smoothstep(0.33, 0.495, abs(leaf_uv.x - 0.5));
	float shading = 0.88 + midrib * 0.17 + veins * 0.11 + grain * 0.13 + leaf_uv.y * 0.10 - edge * 0.11;
	if (isnan(shading) || isinf(shading)) {
		shading = 1.0;
	}
	ALBEDO = tint * shading;
	ROUGHNESS = clamp(0.64 + edge * 0.13 + grain * 0.07, 0.55, 0.83);
	SPECULAR = 0.22;
	BACKLIGHT = tint * vec3(0.33, 0.38, 0.23);
	EMISSION = tint * 0.025;
}
