shader_type spatial;
render_mode specular_schlick_ggx;

varying vec3 world_point;
varying vec3 world_normal;
varying flat vec2 finish;

void vertex() {
	world_point = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
	finish = UV2;
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
	vec3 tint = OUTPUT_IS_SRGB ? COLOR.rgb : pow(COLOR.rgb, vec3(2.2));
	vec3 n = normalize(world_normal);
	vec2 p = vec2(dot(world_point.xz, vec2(n.z, -n.x)), world_point.y);
	if (abs(n.y) > 0.88) {
		p = world_point.xz;
	}
	float pixel = length(fwidth(world_point));
	float wear = noise2(p * 1.45);
	float speckle = (noise2(p * 87.0) - 0.5) * (1.0 - smoothstep(0.004, 0.017, pixel));
	float relief = speckle * 0.0004;
	ALBEDO = tint * (0.87 + wear * 0.24 + speckle * 0.22);
	ROUGHNESS = 0.53 + wear * 0.22;
	METALLIC = 0.29;

	if (finish.x > 0.5 && finish.x < 1.5) {
		vec2 along_plank = vec2(cos(finish.y), sin(finish.y));
		float lengthwise = dot(world_point.xz, along_plank);
		float widthwise = dot(world_point.xz, vec2(-along_plank.y, along_plank.x));
		float grain_axis = widthwise * 64.0 + world_point.y * 47.0;
		float grain = noise2(vec2(lengthwise * 1.9, grain_axis + noise2(p * 3.9) * 1.7));
		float fibers = noise2(vec2(lengthwise * 9.8, grain_axis * 3.7));
		grain = mix(grain, 0.5, smoothstep(0.003, 0.020, pixel));
		ALBEDO = tint * (0.69 + grain * 0.42 + wear * 0.19 + fibers * 0.08);
		ROUGHNESS = 0.68 + grain * 0.24;
		METALLIC = 0.0;
		relief = (grain - 0.5) * 0.0012;
	} else if (finish.x > 1.5 && finish.x < 2.5) {
		float chips = smoothstep(0.72, 0.91, noise2(p * 38.0)) * (1.0 - smoothstep(0.004, 0.021, pixel));
		float dust = clamp((0.62 - world_point.y) * 1.05, 0.0, 0.35) * wear;
		ALBEDO = mix(tint * (0.84 + wear * 0.25 + speckle * 0.21), vec3(0.12, 0.105, 0.092), chips * 0.40 + dust);
		ROUGHNESS = 0.41 + wear * 0.21 + chips * 0.17;
		METALLIC = 0.09;
		relief -= chips * 0.0006;
	} else if (finish.x > 2.5 && finish.x < 3.5) {
		float power = clamp(finish.y, 0.0, 1.0);
		vec2 matrix = p * 156.0;
		float LED = 1.0 - smoothstep(0.13, 0.31, length(fract(matrix) - 0.5));
		LED = mix(LED, 0.29, smoothstep(0.003, 0.015, pixel));
		float rim = pow(1.0 - abs(dot(NORMAL, VIEW)), 2.0);
		ALBEDO = tint * (0.20 + 0.29 * power + 0.16 * rim);
		EMISSION = tint * power * (0.76 + LED * 1.36);
		METALLIC = 0.12;
		ROUGHNESS = 0.19 + (1.0 - power) * 0.09;
		SPECULAR = 0.47;
		relief = 0.0;
	} else if (finish.x > 3.5) {
		bool palm = finish.x < 4.5;
		vec2 bark = palm ? vec2(UV.x * 10.0, world_point.y * 6.65) : p * vec2(22.0, 2.2);
		float warp = noise2(bark * vec2(0.7, 0.23) + 3.8);
		float scar_pos = fract(bark.y + warp * 0.31 + abs(fract(bark.x - 0.5) - 0.5) * 0.26);
		float scars = 1.0 - smoothstep(0.025, 0.15, abs(scar_pos - 0.52));
		float fissures = noise2(vec2(bark.x + warp * 0.78, bark.y * 0.86));
		float cracks = 1.0 - smoothstep(0.17, 0.39, fissures);
		float detail = (palm ? scars * 0.70 + cracks * 0.36 : cracks * 0.86) * (1.0 - smoothstep(0.013, 0.062, pixel));
		ALBEDO = tint * (0.78 + fissures * 0.32 + speckle * 0.27 - detail * 0.39);
		ROUGHNESS = 0.88;
		METALLIC = 0.0;
		relief = speckle * 0.0011 - detail * 0.0044;
	}

	vec3 dx = dFdx(world_point);
	vec3 dy = dFdy(world_point);
	vec3 tx = cross(dy, n);
	vec3 ty = cross(n, dx);
	float det = dot(dx, tx);
	if (abs(det) > 0.00000001) {
		vec3 gradient = sign(det) * (dFdx(relief) * tx + dFdy(relief) * ty);
		NORMAL = normalize(mat3(VIEW_MATRIX) * (abs(det) * n - gradient));
	}
}
