shader_type spatial;

render_mode unshaded, cull_back, depth_draw_never, blend_add, shadows_disabled;

uniform vec4 core_color : source_color = vec4(1.0, 0.78, 0.34, 1.0);
uniform vec4 rim_color : source_color = vec4(1.0, 0.94, 0.62, 0.96);
uniform float intensity : hint_range(0.0, 18.0) = 6.6;
uniform float distance_boost : hint_range(0.5, 3.5) = 1.0;
uniform float rim_power : hint_range(0.8, 8.0) = 3.1;
uniform float flicker_speed : hint_range(0.0, 8.0) = 1.6;
uniform float band_scale : hint_range(0.0, 32.0) = 8.0;
uniform float jet_strength : hint_range(0.0, 3.0) = 1.15;
uniform float jet_speed : hint_range(0.0, 8.0) = 2.1;
uniform float jet_scale : hint_range(1.0, 24.0) = 10.0;
uniform float noise_scale : hint_range(0.5, 24.0) = 7.5;

float hash21(vec2 p)
{
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise21(vec2 p)
{
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);

	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));

	float x1 = mix(a, b, u.x);
	float x2 = mix(c, d, u.x);
	return mix(x1, x2, u.y);
}

void fragment()
{
	vec3 normal_dir = normalize(NORMAL);
	vec3 view_dir = normalize(VIEW);
	vec2 centered_uv = UV - vec2(0.5);
	float radial = clamp(length(centered_uv) * 2.0, 0.0, 2.0);
	float angle = atan(centered_uv.y, centered_uv.x);

	float fresnel = pow(1.0 - clamp(dot(normal_dir, view_dir), 0.0, 1.0), rim_power);
	float bands = 0.5 + 0.5 * sin((UV.x + UV.y + radial * 0.25) * band_scale + TIME * flicker_speed);

	float jet_wave = 0.5 + 0.5 * sin(angle * jet_scale + TIME * jet_speed + radial * 2.4);
	float jet_noise_a = noise21(vec2(angle * 2.2, radial * noise_scale + TIME * 0.22));
	float jet_noise_b = noise21(vec2(angle * 4.4 - TIME * 0.18, radial * (noise_scale * 1.12)));
	float jets = pow(clamp(jet_wave * 0.52 + jet_noise_a * 0.34 + jet_noise_b * 0.24, 0.0, 1.0), 1.7);

	float inner_suppress = smoothstep(0.52, 0.92, radial);
	float rim_mask = smoothstep(0.58, 1.18, radial);
	float corona = fresnel * (0.42 + 0.26 * bands) + jets * jet_strength * rim_mask * 0.72;
	corona *= inner_suppress;
	corona *= distance_boost;

	float alpha = smoothstep(0.34, 0.82, corona) * rim_color.a;
	if (alpha < 0.02)
	{
		discard;
	}

	vec3 glow = mix(core_color.rgb, rim_color.rgb, clamp(corona, 0.0, 1.0));
	ALBEDO = vec3(0.0);
	EMISSION = glow * alpha * intensity;
	ALPHA = alpha;
}
