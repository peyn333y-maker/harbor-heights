shader_type spatial;
render_mode specular_schlick_ggx;

uniform sampler2D beach_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D beach_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float beach_coast;

// Surface type is encoded per face, keeping one draw per chunk for pavements.
varying vec3 world_point;
varying vec3 world_normal;
varying flat float surface;

void vertex() {
	world_point = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
	surface = UV2.x;
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
	vec2 p = world_point.xz;
	float footprint = max(length(dFdx(p)), length(dFdy(p)));
	float grain = mix(hash12(floor(p * 78.0)) - 0.5, 0.0, smoothstep(0.007, 0.027, footprint));
	float mottling = noise2(p * 0.83) * 0.65 + noise2(p * 0.085) * 0.35;
	vec3 tint = OUTPUT_IS_SRGB ? COLOR.rgb : pow(COLOR.rgb, vec3(2.2));
	ROUGHNESS = 0.87;
	SPECULAR = 0.24;

	if (surface < 0.5) {
		float aggregate = mix(noise2(p * 24.0), 0.5, smoothstep(0.015, 0.09, footprint));
		float patch = smoothstep(0.56, 0.74, noise2(p * 0.15 + 20.0));
		float seam_line = abs(fract(p.x / 17.3 + noise2(p * 0.12) * 0.10) - 0.5);
		float tar = (1.0 - smoothstep(0.002, 0.007 + footprint * 0.06, seam_line))
			* smoothstep(0.39, 0.69, noise2(p * 0.13));
		tint *= 0.78 + mottling * 0.37 + aggregate * 0.29 + grain * 0.28 - patch * 0.16 - tar * 0.26;
		ROUGHNESS = 0.83 - patch * 0.26 - tar * 0.21;
	} else if (surface < 1.5) {
		vec2 tile = p / vec2(1.62, 1.48);
		vec2 edge = abs(fract(tile - 0.5) - 0.5);
		float joint = 1.0 - smoothstep(0.005, 0.013 + footprint * 0.22, min(edge.x, edge.y));
		joint *= (1.0 - smoothstep(0.04, 0.26, footprint)) * abs(world_normal.y);
		float slab = hash12(floor(tile));
		tint *= 0.90 + slab * 0.13 + (mottling - 0.5) * 0.26 + grain * 0.21 - joint * 0.40;
	} else if (surface < 2.5) {
		float chipped = smoothstep(0.50, 0.76, noise2(p * 13.0));
		float wear = smoothstep(0.39, 0.75, noise2(p * 2.0));
		tint *= 0.77 + mottling * 0.42 - chipped * (1.0 - smoothstep(0.02, 0.2, footprint)) * 0.50 - wear * 0.20 + grain * 0.26;
	} else if (surface < 3.5) {
		float blades = noise2(p * 12.0);
		tint *= 0.64 + mottling * 0.50 + blades * 0.23 + grain * 0.31;
	} else if (surface > 5.5) {
		vec2 uv = vec2(p.x * .115 + p.y * .019, p.y * .142 - p.x * .029);
		vec3 coarse = texture(beach_albedo, uv * .32).rgb;
		vec3 small = texture(beach_albedo, uv * 1.56 + vec2(.49, .17)).rgb;
		vec3 detail = texture(beach_normal, uv * 1.56 + vec2(.49, .17)).rgb * 2. - 1.;
		float shore = beach_coast + sin(p.y * .0043 + .7) * 6.3 + sin(p.y * .0092) * 1.8;
		float wet = smoothstep(shore - 3.5, shore + 3.0, p.x);
		float pattern = mix(coarse.r, small.r, .51);
		tint *= .75 + pattern * .65 + (mottling - .39) * .16;
		tint *= 1. - wet * .22;
		NORMAL = normalize(NORMAL + mat3(VIEW_MATRIX) * vec3(detail.x, 0., -detail.y)
			* (.40 - wet * .22) * (1. - smoothstep(.06, .45, footprint)));
		ROUGHNESS = .92 - wet * .27;
		SPECULAR = .13 + wet * .20;
	} else if (surface > 4.5) {
		vec2 cell = (fract(UV * vec2(8.0, 17.0)) - 0.5) * 2.0;
		float radius = length(cell);
		float bump = (1.0 - smoothstep(0.30, 0.68, radius)) * (1.0 - smoothstep(0.02, 0.06, footprint));
		tint *= (0.88 + grain * 0.14 + mottling * 0.19) * (1.0 + 0.24 * bump + cell.x * bump * 0.16);
		ROUGHNESS = 0.70;
	} else {
		tint *= 0.75 + mottling * 0.46 + grain * 0.35;
	}
	ALBEDO = tint;
}
