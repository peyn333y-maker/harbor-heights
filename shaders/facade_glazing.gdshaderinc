float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float band(float coord, float start, float end, float aa) {
	return smoothstep(start - aa, start + aa, coord) * (1.0 - smoothstep(end - aa, end + aa, coord));
}

vec3 glazing_reflection(vec3 eye, vec3 normal) {
	vec3 reflected = reflect(-eye, normal);
	float sky = smoothstep(-0.35, 0.40, reflected.y);
	return mix(vec3(0.033, 0.058, 0.074), vec3(0.158, 0.252, 0.326), sky);
}

vec3 glazing_room(float choice, bool shop) {
	float room_light = shop ? mix(0.25, 0.86, choice) : (choice > 0.80 ? 0.62 : 0.19);
	return mix(vec3(0.037, 0.039, 0.035), vec3(0.24, 0.165, 0.087), room_light);
}
