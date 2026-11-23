shader_type sky;
render_mode disable_fog;

uniform float storm = 0.0;
uniform vec3 sun_direction = vec3(-0.63, 0.45, 0.63);

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

void sky() {
	float h = max(EYEDIR.y, 0.0);
	float angle_to_sun = max(dot(EYEDIR, sun_direction), 0.0);
	vec3 horizon = mix(vec3(0.42, 0.57, 0.68), vec3(0.85, 0.65, 0.44), pow(angle_to_sun, 5.0) * 0.68);
	vec3 color = mix(horizon, vec3(0.039, 0.184, 0.39), smoothstep(-0.055, 0.55, h));
	color += vec3(0.75, 0.37, 0.16) * pow(angle_to_sun, 28.0) * 0.43;
	if (!AT_CUBEMAP_PASS && EYEDIR.y > 0.008) {
		vec2 p = EYEDIR.xz / (h + 0.18);
		p = vec2(p.x * 3.1 + p.y * 1.1, p.y * 6.9) + vec2(8.9, 13.7);
		float broad = noise2(p * 0.66);
		float cloud = broad * 0.53 + noise2(p * 1.57) * 0.27 + noise2(p * 3.8) * 0.13;
		cloud += noise2(p * 8.6) * 0.05 + noise2(p * 18.0) * 0.02;
		float amount = smoothstep(0.53, 0.73, cloud) * smoothstep(0.008, 0.16, h);
		vec3 vapor = mix(vec3(0.48, 0.54, 0.58), vec3(1.19, 1.04, 0.87), smoothstep(0.52, 0.76, cloud));
		color = mix(color, vapor, amount * 0.93);
		float sun_disc = smoothstep(0.99982, 0.99994, angle_to_sun);
		color += vec3(12.0, 7.5, 3.9) * sun_disc;
	}
	vec2 weather_dir = EYEDIR.xz / (0.45 + h) * 5.0 + vec2(2.4, 9.7);
	float overcast = noise2(weather_dir) * 0.65 + noise2(weather_dir * 2.3) * 0.35;
	vec3 storm_color = mix(vec3(0.39, 0.50, 0.51), vec3(0.11, 0.18, 0.22), smoothstep(0.0, 0.66, h));
	storm_color *= 0.76 + overcast * 0.45;
	COLOR = mix(color, storm_color, storm);
}
