shader_type spatial;
render_mode cull_disabled, specular_schlick_ggx;

uniform float coast;
uniform float front;
uniform float age;
uniform float storm;
uniform float surge = -1.0;
varying vec3 point;
varying vec3 slope;
varying float crest_foam;
varying float shore_foam;
varying float bore;

float shore(float z) {
	return coast + sin(z * .0043 + .7) * 6.3 + sin(z * .0092) * 1.8;
}

// Match the gameplay height field in HarborWater.cs.
vec4 water_fields(vec2 p) {
	float bank = shore(p.y);
	float shelf = smoothstep(bank - 3., bank + 13., p.x);
	float phase = (p.x - coast) * .51 + p.y * .038 + age * 2.25;
	float ripple = sin(phase) + .36 * sin(phase * 1.82 - p.y * .12);
	float height = -.39 + shelf * (.065 + storm * .28) * ripple;
	float shoreline = exp(-abs(p.x - bank - 1.8) * .37) * (.5 + .5 * sin(phase));
	float breakers = 0.;
	float flood = 0.;
	if (surge >= 0.) {
		float bend = sin(p.y * .009) * 14. + sin(p.y * .027) * 4.;
		float d = p.x - front - bend;
		float wet = smoothstep(-9., 10., d);
		float reach = smoothstep(-3134., -3102., p.x);
		float strength = smoothstep(0., 4., surge)
			* (1. - .62 * smoothstep(0., 36., max(0., surge - (coast + 265. + 3130.) / 16.8)));
		float crest = exp(-pow((d - 15.) / 24., 2.));
		float trailing = exp(-max(d, 0.) / 430.);
		flood = wet * reach * strength;
		height += flood * (1.25 + 3.4 * trailing + 29. * crest
			+ .45 * sin(d * .19 + p.y * .09 - age * 2.8));
		breakers = reach * strength * smoothstep(15., 30., height) * (.83 + .17 * crest);
		breakers += flood * exp(-abs(d + 1.) * .36) * .39;
	}
	return vec4(height, breakers, shoreline, flood);
}

void vertex() {
	vec2 p = VERTEX.xz;
	vec4 fields = water_fields(p);
	VERTEX.y = fields.x;
	crest_foam = fields.y;
	shore_foam = fields.z;
	bore = fields.w;
	float hx = water_fields(p + vec2(.7, 0.)).x - fields.x;
	float hz = water_fields(p + vec2(0., .7)).x - fields.x;
	slope = normalize(vec3(-hx, .7, -hz));
	point = VERTEX;
}

float grain(vec2 p) {
	vec2 cell = floor(p);
	vec2 f = fract(p);
	f *= f * (3. - 2. * f);
	vec4 values = fract(sin(vec4(dot(cell, vec2(12.9898, 78.233))) +
		vec4(0., 12.9898, 78.233, 91.2228)) * 43758.5453);
	return mix(mix(values.x, values.y, f.x), mix(values.z, values.w, f.x), f.y);
}

void fragment() {
	vec2 p = point.xz;
	float wave = sin(p.x * 1.2 + p.y * .71 + age * 2.6);
	float wave2 = sin(p.x * .74 - p.y * 1.39 + age * 1.8);
	float sharp = 1. - smoothstep(.4, 3.8, length(fwidth(p)));
	vec2 foam_pos = vec2(p.y * .69, (point.y + p.x * .25) * .47 + age * .85);
	float streaks = grain(foam_pos * .54) * .57 + grain(foam_pos * 1.86) * .29;
	streaks += grain(foam_pos * 6.3) * .14 * sharp;
	float turbulence = grain(vec2(p.y * .204 + wave2 * .061, p.x * .35 + age * 1.52));
	vec3 ripples = vec3(wave * .069, 0., wave2 * .072) * sharp * (.64 + storm);
	NORMAL = normalize(mat3(VIEW_MATRIX) * (normalize(slope) + ripples));
	float horizon = pow(1. - clamp(dot(NORMAL, VIEW), 0., 1.), 3.1);
	vec3 shallows = vec3(.022, .23, .24);
	vec3 deep = vec3(.016, .091, .126);
	vec3 water = mix(shallows, deep, smoothstep(5., 92., p.x - coast));
	water = mix(water, vec3(.031, .185, .172) * (.68 + turbulence * .52), bore);
	water = mix(water, vec3(.27, .39, .42), horizon * (1. - bore * .79) * .61);
	float froth = smoothstep(.235, .75, crest_foam + streaks * .37);
	float scum = bore * smoothstep(.61, .80, turbulence * .6 + streaks * .45) * .31;
	float beach = shore_foam * smoothstep(.34, .68, turbulence + streaks * .22);
	float foam = clamp(froth + beach * .51 + scum, 0., 1.);
	vec3 chalk = vec3(.67, .85, .79);
	ALBEDO = mix(water, chalk, foam);
	EMISSION = chalk * foam * (.14 + .26 * crest_foam);
	ROUGHNESS = mix(.25 + storm * .14, .76, foam);
	METALLIC = .22 * (1. - foam);
	SPECULAR = .59;
}
