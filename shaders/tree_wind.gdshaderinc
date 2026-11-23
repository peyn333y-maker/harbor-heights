uniform float wind_strength : hint_range(0.0, 2.0) = 1.0;

vec3 tree_wind(vec3 local_point, mat4 model, float time) {
	vec2 root = model[3].xz;
	float height = clamp(local_point.y * 0.131, 0.0, 1.45);
	float phase = dot(root, vec2(0.071, 0.045));
	float gust = 0.108 + 0.097 * sin(time * 1.26 + phase)
		+ 0.039 * sin(time * 2.36 - phase * 0.85);
	float crosswind = 0.035 * sin(time * 1.53 - phase * 0.71);
	vec3 delta = vec3(gust * 0.80 - crosswind * 0.60, 0.0, gust * 0.60 + crosswind * 0.80);
	// Tree bases use rotation and per-axis scale, so the axes remain orthogonal.
	vec3 x_axis = model[0].xyz;
	vec3 z_axis = model[2].xyz;
	vec3 local = vec3(dot(delta, x_axis) / max(dot(x_axis, x_axis), 0.0001),
		0.0, dot(delta, z_axis) / max(dot(z_axis, z_axis), 0.0001));
	return local * (height * height * wind_strength);
}

vec3 plant_color(vec3 color, bool output_is_srgb) {
	if (any(isnan(color)) || any(isinf(color))) {
		return vec3(0.12, 0.16, 0.083);
	}
	vec3 bounded = clamp(color, vec3(0.0), vec3(1.0));
	return output_is_srgb ? bounded : pow(bounded, vec3(2.2));
}

vec3 leaf_wind(vec3 local_point, vec3 normal, vec2 uv, vec2 flex, mat4 model, float time) {
	vec2 root = model[3].xz;
	float phase = flex.y + dot(root, vec2(0.046, -0.062));
	float bow = flex.x * flex.x * sin(time * 2.25 + phase - flex.x * 2.5) * 0.140;
	float flutter = uv.y * uv.y * (sin(time * 5.1 + phase) * 0.014 + sin(time * 7.15 - phase * 1.74) * 0.006);
	return tree_wind(local_point, model, time) + vec3(bow * 0.24, bow, bow * 0.35) * wind_strength
		+ normal * flutter * wind_strength;
}
