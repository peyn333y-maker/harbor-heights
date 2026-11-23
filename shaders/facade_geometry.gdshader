shader_type spatial;
render_mode specular_schlick_ggx;

#include "res://shaders/facade_glazing.gdshaderinc"

varying vec3 world_point;
varying vec3 world_normal;
varying float section_depth;
varying flat float surface_kind;
varying flat vec3 pane_data;

void vertex() {
	world_point = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
	section_depth = VERTEX.z;
	surface_kind = UV2.x;
	pane_data = INSTANCE_CUSTOM.xyz;
}

float noise2(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash12(i), hash12(i + vec2(1, 0)), f.x),
		mix(hash12(i + vec2(0, 1)), hash12(i + vec2(1, 1)), f.x), f.y);
}

vec3 room_color(vec2 pane_uv, vec3 eye, vec3 along, vec3 normal, vec2 aa, bool shop) {
	float seed = pane_data.x;
	float choice = hash12(vec2(seed, 13.79));
	float facing = max(0.14, dot(eye, normal));
	vec2 ray = -vec2(dot(eye, along), eye.y) / (max(pane_data.yz, vec2(0.6)) * facing);
	vec2 reach = (step(vec2(0.0), ray) - pane_uv) / (sign(ray + vec2(0.00001)) * max(abs(ray), vec2(0.0001)));
	float room_depth = shop ? 2.2 : 1.65;
	float travel = min(room_depth, max(0.0, min(reach.x, reach.y)));
	vec2 interior = clamp(pane_uv + ray * travel, 0.0, 1.0);
	bool ceiling = reach.y < min(reach.x, room_depth) && ray.y > 0.0;
	bool floor_hit = reach.y < min(reach.x, room_depth) && ray.y < 0.0;
	float room_light = shop ? mix(0.25, 0.86, choice) : (choice > 0.80 ? 0.62 : 0.19);
	vec3 room = glazing_room(choice, shop);
	float lamp = band(interior.x, 0.18, 0.82, aa.x) * band(travel, 0.6, 1.35, 0.09);
	if (ceiling) {
		room = room * 0.67 + vec3(0.7, 0.49, 0.26) * lamp * room_light * 0.64;
	}
	if (floor_hit) {
		room *= vec3(0.62, 0.56, 0.46) * (0.83 + noise2(vec2(interior.x, travel) * 12.2) * 0.27);
	}
	if (shop && travel > room_depth - 0.09) {
		float racks = band(interior.y, 0.19, 0.22, aa.y) + band(interior.y, 0.37, 0.40, aa.y);
		float merch = hash12(floor(interior * vec2(13.0, 7.0)) + seed);
		float cabinet = band(interior.x, 0.10, 0.91, aa.x) * band(interior.y, 0.08, 0.44, aa.y);
		room += cabinet * (vec3(0.10, 0.075, 0.043) * merch + racks * 0.055) * room_light;
	}
	if (!shop && choice > 0.44 && choice < 0.76) {
		float lowered = smoothstep(0.28 + choice * 0.49, 0.32 + choice * 0.49, pane_uv.y);
		float slat = mix(smoothstep(0.12, 0.32, fract(pane_uv.y * 30.0)), 0.78,
			smoothstep(0.006, 0.024, aa.y));
		room = mix(room, vec3(0.15, 0.144, 0.113) * (0.56 + slat * 0.44), lowered * 0.82);
	}
	return room;
}

void fragment() {
	vec3 normal = normalize(world_normal);
	vec3 along = abs(normal.y) > 0.96 ? vec3(1.0, 0.0, 0.0) : normalize(cross(vec3(0.0, 1.0, 0.0), normal));
	vec2 p = abs(normal.y) > 0.80 ? world_point.xz : vec2(dot(along, world_point), world_point.y);
	vec3 dpdx = dFdx(world_point);
	vec3 dpdy = dFdy(world_point);
	float pixel = max(length(dpdx), length(dpdy));
	vec2 uv_aa = clamp(fwidth(UV) * 0.65, vec2(0.001), vec2(0.20));
	float reveal = smoothstep(0.03, 1.0, -section_depth);
	float wall_kind = surface_kind;
	bool glass = wall_kind > 5.5 && wall_kind < 7.5;

	if (glass) {
		vec3 eye = normalize(mat3(INV_VIEW_MATRIX) * VIEW);
		float grazing = pow(1.0 - clamp(dot(normal, eye), 0.0, 1.0), 3.0);
		float pane_variation = hash12(vec2(pane_data.x, 8.12));
		vec3 exterior = glazing_reflection(eye, normal);
		vec3 interior = room_color(UV, eye, along, normal, uv_aa, wall_kind > 6.5);
		float reflectivity = clamp(0.27 + grazing * 0.54 + pane_variation * 0.18, 0.0, 1.0);
		vec3 pane = mix(interior, exterior, reflectivity);
		ALBEDO = pane * 0.49;
		EMISSION = pane * (0.38 + grazing * 0.24);
		METALLIC = 0.16;
		ROUGHNESS = 0.17 + pane_variation * 0.12;
		SPECULAR = 0.51;
	} else {
		float micro_filter = 1.0 - smoothstep(0.008, 0.032, pixel);
		float grit = (noise2(p * 89.0) - 0.5) * micro_filter;
		float mottling = noise2(p * 2.9) * 0.55 + noise2(p * 0.34) * 0.45;
		float stains = noise2(p * vec2(5.0, 0.43)) * noise2(p * 0.71 + vec2(12.1));
		vec3 tint = OUTPUT_IS_SRGB ? pow(COLOR.rgb, vec3(0.455)) : COLOR.rgb;
		vec3 masonry = tint * (0.88 + mottling * 0.24 - stains * 0.18 + grit * 0.17);
		float relief = grit * 0.0020 + mottling * 0.0018;
		float roughness = 0.87;
		float metal = 0.0;
		if (wall_kind < 0.5 || (wall_kind > 2.5 && wall_kind < 3.5)) {
			bool brick = wall_kind < 0.5;
			vec2 dimensions = brick ? vec2(0.28, 0.085) : vec2(1.16, 0.59);
			vec2 units = p / dimensions;
			units.x += mod(floor(units.y), 2.0) * 0.5;
			vec2 joint = abs(fract(units - 0.5) - 0.5) * dimensions;
			float edge = min(joint.x, joint.y);
			float blend = max(0.0025, pixel * 0.67);
			float cut = 1.0 - smoothstep(0.006 - blend, 0.010 + blend, edge);
			cut *= 1.0 - smoothstep(0.023, 0.10, pixel);
			float block = hash12(floor(units));
			vec3 mortar = masonry * (brick ? 0.57 : 0.50) + vec3(0.035, 0.029, 0.022);
			masonry = mix(masonry * mix(0.79, 1.15, block), mortar, cut * 0.72);
			relief -= cut * (brick ? 0.006 : 0.004);
		} else if (wall_kind > 1.5 && wall_kind < 2.5) {
			float pores = smoothstep(0.67, 0.87, noise2(p * 37.0)) * micro_filter;
			float panels = 1.0 - smoothstep(0.004, 0.011 + pixel, abs(fract(p.x / 1.65 - 0.5) - 0.5));
			panels *= 1.0 - smoothstep(0.025, 0.14, pixel);
			masonry *= 1.0 - pores * 0.26 - panels * 0.12;
			relief -= pores * 0.0022 + panels * 0.003;
		} else if ((wall_kind > 0.5 && wall_kind < 1.5) || wall_kind > 4.5) {
			bool bronze = wall_kind > 7.5;
			vec3 alloy = bronze ? vec3(0.225, 0.147, 0.083) : vec3(0.13, 0.142, 0.15);
			masonry = mix(masonry, alloy, wall_kind < 1.5 ? 0.18 : 0.72) * (0.81 + mottling * 0.26);
			roughness = 0.39 + mottling * 0.22;
			metal = wall_kind < 1.5 ? 0.32 : 0.57;
			relief *= 0.14;
		}

		vec3 geometric = normalize(cross(dpdx, dpdy));
		geometric *= dot(geometric, normal) < 0.0 ? -1.0 : 1.0;
		vec3 r1 = cross(dpdy, geometric);
		vec3 r2 = cross(geometric, dpdx);
		float det = dot(dpdx, r1);
		vec3 gradient = sign(det) * (dFdx(relief) * r1 + dFdy(relief) * r2);
		NORMAL = normalize(mat3(VIEW_MATRIX) * (max(abs(det), 0.00000001) * geometric - gradient * 0.88));
		ALBEDO = masonry * (1.0 - reveal * 0.19);
		AO = 1.0 - reveal * 0.58;
		AO_LIGHT_AFFECT = 0.34;
		ROUGHNESS = roughness;
		METALLIC = metal;
		SPECULAR = 0.29;
	}
}
