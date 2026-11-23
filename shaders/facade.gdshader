shader_type spatial;
render_mode specular_schlick_ggx;

#include "res://shaders/facade_glazing.gdshaderinc"

uniform vec4 openings[7];
varying flat vec2 facade_data;

void vertex() {
	facade_data = UV2;
}

float aperture(vec2 p, vec4 rect, vec2 inset, vec2 aa) {
	return band(p.x, rect.x + inset.x, rect.z - inset.x, aa.x)
		* band(p.y, rect.y + inset.y, rect.w - inset.y, aa.y);
}

void fragment() {
	int style = int(facade_data.x);
	vec2 cell = floor(UV);
	vec2 p = fract(UV);
	vec2 aa = clamp(fwidth(UV) * 0.63, vec2(0.001), vec2(0.20));
	float seed = mod(facade_data.y * 193.0 + cell.x * 79.0 + cell.y * 141.0, 7919.0);
	float choice = hash12(vec2(seed, 13.79));
	float pane_variation = hash12(vec2(seed, 8.12));
	vec3 tint = OUTPUT_IS_SRGB ? COLOR.rgb : pow(COLOR.rgb, vec3(2.2));
	vec3 wall = tint * (0.94 + hash12(cell + facade_data.y) * 0.13);
	bool shop = style == 4 || style == 7;
	bool door = style == 7 ? cell.x == 1.0 : mod(cell.x + facade_data.y, 4.0) == 0.0;
	int variant = shop ? (door ? 6 : 4) : style;
	float reveal = 0.0;
	float frame = 0.0;
	float glass = 0.0;
	if (style != 5) {
		vec4 window_rect = openings[variant];
		float reveal_width = shop ? 0.010 : 0.016;
		float frame_width = style == 1 ? 0.023 : 0.032;
		reveal = aperture(p, window_rect, vec2(0.0), aa);
		frame = aperture(p, window_rect, vec2(reveal_width), aa);
		glass = aperture(p, window_rect, vec2(reveal_width + frame_width), aa);
		float horizontal = mix(window_rect.y, window_rect.w, shop ? 0.71 : 0.64);
		if (style == 0 || style == 3 || shop) {
			glass *= 1.0 - band(p.y, horizontal - 0.009, horizontal + 0.009, aa.y);
		}
		if (style == 1 || style == 2 || shop) {
			glass *= 1.0 - band(p.x, 0.491, 0.509, aa.x);
		}
		if (style == 0 || style == 3) {
			float sill = band(p.y, window_rect.y - 0.042, window_rect.y + 0.006, aa.y)
				* band(p.x, window_rect.x - 0.038, window_rect.z + 0.038, aa.x);
			wall *= 1.0 + sill * 0.12;
		} else if (style == 1) {
			wall = mix(wall, vec3(0.13, 0.142, 0.15), 0.18);
		}
	}

	vec3 eye = normalize(mat3(INV_VIEW_MATRIX) * VIEW);
	vec3 n = normalize(mat3(INV_VIEW_MATRIX) * NORMAL);
	float grazing = pow(1.0 - clamp(dot(n, eye), 0.0, 1.0), 3.0);
	vec3 exterior = glazing_reflection(eye, n);
	vec3 room = glazing_room(choice, shop);
	if (!shop && choice > 0.44 && choice < 0.76) {
		float blinds = smoothstep(0.35 + choice * 0.22, 0.44 + choice * 0.22, p.y);
		room = mix(room, vec3(0.15, 0.144, 0.113) * 0.88, blinds * 0.72);
	}
	vec3 pane = mix(room, exterior, clamp(0.27 + grazing * 0.54 + pane_variation * 0.18, 0.0, 1.0));
	vec3 alloy = shop ? vec3(0.225, 0.147, 0.083) : vec3(0.13, 0.142, 0.15);
	vec3 trim = mix(wall, alloy, 0.72) * 0.80;
	ALBEDO = mix(mix(wall * (1.0 - reveal * 0.25), trim, frame), pane * 0.49, glass);
	EMISSION = pane * (0.38 + grazing * 0.24) * glass;
	METALLIC = mix(style == 1 ? 0.32 : 0.0, 0.16, glass);
	ROUGHNESS = mix(style == 1 ? 0.55 : 0.87, 0.17 + pane_variation * 0.12, glass);
	SPECULAR = mix(0.29, 0.51, glass);
	AO = 1.0 - (reveal - glass) * 0.34;
}
