shader_type spatial;
render_mode specular_disabled;

#include "res://shaders/tree_wind.gdshaderinc"

uniform sampler2D oak_color : source_color, repeat_enable, filter_linear_mipmap_anisotropic;
uniform sampler2D palm_color : source_color, repeat_enable, filter_linear_mipmap_anisotropic;
uniform sampler2D oak_normal : repeat_enable, filter_linear_mipmap_anisotropic;
uniform sampler2D palm_normal : repeat_enable, filter_linear_mipmap_anisotropic;

varying vec3 world_point;
varying vec3 world_normal;
varying flat float bark_type;

void vertex() {
	VERTEX += tree_wind(VERTEX, MODEL_MATRIX, TIME);
	world_point = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
	bark_type = UV2.x;
}

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise2(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash12(i), hash12(i + vec2(1, 0)), f.x),
		mix(hash12(i + vec2(0, 1)), hash12(i + vec2(1)), f.x), f.y);
}

void fragment() {
	vec3 tint = plant_color(COLOR.rgb, OUTPUT_IS_SRGB);
	vec3 normal = normalize(world_normal);
	vec2 surface = vec2(dot(world_point.xz, vec2(normal.z, -normal.x)), world_point.y);
	float footprint = length(fwidth(world_point));
	float filter = (isnan(footprint) || isinf(footprint)) ? 0.0 : 1.0 - smoothstep(0.012, 0.085, footprint);
	float mottling = noise2(world_point.xz * 5.0 + world_point.yy * vec2(0.9, 1.7));
	float grain = noise2(surface * 87.0) * filter;
	float structure = 0.0;
	float relief = 0.0014 * grain;
	if (bark_type < 0.5) {
		vec2 grooves = UV * vec2(38.0, 2.4);
		grooves.x += noise2(grooves * 0.31 + 7.6) * 1.90;
		float fiber = noise2(grooves);
		float fissure = 1.0 - smoothstep(0.22, 0.43, fiber);
		structure = fissure * 0.32 * filter;
		relief -= fissure * 0.011 * filter;
	} else if (bark_type < 1.5) {
		vec2 layers = vec2(UV.x * 13.0, UV.y * 6.4);
		float cracked = noise2(layers * vec2(1.42, 0.68));
		float cut = abs(fract(layers.y + noise2(layers * 0.41) * 0.86) - 0.53);
		float scar = (1.0 - smoothstep(0.021, 0.123, cut)) * smoothstep(0.29, 0.72, cracked);
		structure = scar * 0.26 * filter;
		relief -= scar * 0.0042 * filter;
	} else {
		float fibers = noise2(surface * vec2(76.0, 17.0));
		structure = (1.0 - fibers) * 0.21;
		relief -= fibers * 0.0032 * filter;
	}
	float shading = 0.71 + mottling * 0.40 + grain * 0.16 - structure;
	if (isnan(shading) || isinf(shading)) {
		shading = 1.0;
	}
	vec3 albedo = tint * shading;
	ROUGHNESS = 0.84 + mottling * 0.12;
	SPECULAR = 0.0;
	vec3 dx = dFdx(world_point);
	vec3 dy = dFdy(world_point);
	vec3 tangent = cross(dy, normal);
	vec3 bitangent = cross(normal, dx);
	float det = dot(dx, tangent);
	vec3 shaded_normal = normal;
	if (bark_type < 2.5 && abs(det) > 0.00000001) {
		vec3 slope = sign(det) * (dFdx(relief) * tangent + dFdy(relief) * bitangent);
		if (!any(isnan(slope)) && !any(isinf(slope))) {
			vec3 bump = normal - clamp(slope / abs(det), vec3(-0.42), vec3(0.42));
			shaded_normal = normalize(bump);
		}
	}
	float photo_detail = 1.0 - smoothstep(40.0, 68.0, distance(world_point, CAMERA_POSITION_WORLD));
	if (bark_type < 1.5 && photo_detail > 0.001) {
		bool oak = bark_type < 0.5;
		vec2 coords = UV * vec2(oak ? 2.0 : 1.65, oak ? -1.0 : -0.78);
		vec3 scan = oak ? texture(oak_color, coords).rgb : texture(palm_color, coords).rgb;
		albedo = mix(albedo, scan * (0.79 + 0.30 * mottling), photo_detail * (oak ? 0.88 : 0.78));
		if (filter * photo_detail > 0.03) {
			vec2 du = dFdx(coords);
			vec2 dv = dFdy(coords);
			vec3 along = tangent * du.x + bitangent * dv.x;
			vec3 across = tangent * du.y + bitangent * dv.y;
			float size = max(dot(along, along), dot(across, across));
			if (size > 0.00000001 && !isinf(size)) {
				vec2 pattern = (oak ? texture(oak_normal, coords).rg : texture(palm_normal, coords).rg) * 2.0 - 1.0;
				vec3 offset = clamp((along * pattern.x + across * pattern.y) * inversesqrt(size),
					vec3(-0.64), vec3(0.64)) * filter * 0.64;
				if (!any(isnan(offset)) && !any(isinf(offset))) {
					shaded_normal = normalize(shaded_normal + offset);
				}
			}
		}
	}
	ALBEDO = clamp(albedo, vec3(0.0), vec3(1.0));
	NORMAL = normalize(mat3(VIEW_MATRIX) * shaded_normal);
}
