shader_type spatial;
render_mode specular_schlick_ggx;

varying vec3 world_point;

void vertex() {
	world_point = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 p = world_point.xz;
	float a = sin(p.x * 1.42 + p.y * 0.81 + TIME * 0.51);
	float b = sin(p.x * 0.73 - p.y * 1.78 - TIME * 0.73);
	float c = sin(p.x * 0.37 + p.y * 0.24 + a * 0.38);
	float detail = 1.0 - smoothstep(0.4, 1.7, length(fwidth(p)));
	vec3 ripple = vec3(a * 0.035 + b * 0.015, 0.0, b * 0.024 + c * 0.035) * detail;
	NORMAL = normalize(NORMAL + mat3(VIEW_MATRIX) * ripple);
	float horizon = pow(1.0 - max(dot(NORMAL, VIEW), 0.0), 4.0);
	ALBEDO = mix(vec3(0.025, 0.11, 0.116), vec3(0.16, 0.275, 0.30), horizon) * (0.98 + c * 0.036);
	METALLIC = 0.32;
	ROUGHNESS = 0.31 + (a + 1.0) * 0.075 * detail;
	SPECULAR = 0.58;
}
