shader_type spatial;

render_mode unshaded, cull_back, depth_draw_opaque, shadows_disabled;

uniform sampler2D detail_tex : source_color;
uniform bool use_detail_tex = false;

uniform vec4 base_color : source_color = vec4(1.0, 0.78, 0.34, 1.0);
uniform vec4 hot_color : source_color = vec4(1.0, 0.98, 0.74, 1.0);
uniform float emission_strength : hint_range(0.0, 48.0) = 22.0;
uniform float flow_speed : hint_range(0.0, 4.0) = 0.52;
uniform float granulation_scale : hint_range(2.0, 48.0) = 14.5;
uniform float pulse_strength : hint_range(0.0, 1.0) = 0.12;
uniform float detail_mix : hint_range(0.0, 1.0) = 0.12;
uniform float brightness_multiplier : hint_range(0.2, 4.0) = 1.0;

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

float fbm(vec2 p)
{
	float value = 0.0;
	float amp = 0.5;
	for (int i = 0; i < 4; i++)
	{
		value += noise21(p) * amp;
		p *= 2.03;
		amp *= 0.5;
	}
	return value;
}

void fragment()
{
	float t = TIME * flow_speed;
	vec3 nrm = normalize(NORMAL);
	vec3 p = nrm * granulation_scale;

	float warp_a = fbm(vec2(p.x * 0.85 + t * 0.27, p.z * 0.92 - t * 0.21));
	float warp_b = fbm(vec2(p.y * 0.88 - t * 0.19, p.x * 0.95 + t * 0.24));
	p += vec3(warp_a - 0.5, warp_b - 0.5, (warp_a - warp_b) * 0.75) * 1.35;

	float n1 = fbm(vec2(p.x + t * 0.31, p.y - t * 0.18));
	float n2 = fbm(vec2(p.y - t * 0.22, p.z + t * 0.26));
	float n3 = fbm(vec2(p.z + t * 0.16, p.x + t * 0.23));
	float n4 = fbm(vec2((p.x + p.y) * 0.73 - t * 0.12, (p.y + p.z) * 0.81 + t * 0.17));
	float grain = fbm(vec2((p.x - p.z) * 1.72 + t * 0.41, (p.y + p.z) * 1.86 - t * 0.33));

	float plasma = clamp((n1 + n2 + n3 + n4) * 0.25, 0.0, 1.0);
	plasma = mix(plasma, grain, 0.33);
	plasma = smoothstep(0.12, 0.93, plasma);

	vec3 sun_col = mix(base_color.rgb, hot_color.rgb, plasma);

	if (use_detail_tex)
	{
		vec3 w = pow(abs(nrm), vec3(4.0));
		w = max(w, vec3(0.0001));
		w /= (w.x + w.y + w.z);

		vec2 uvx = fract(nrm.yz * 0.5 + 0.5 + vec2(t * 0.012, -t * 0.009));
		vec2 uvy = fract(nrm.zx * 0.5 + 0.5 + vec2(-t * 0.010, t * 0.011));
		vec2 uvz = fract(nrm.xy * 0.5 + 0.5 + vec2(t * 0.008, t * 0.010));

		vec3 d_x = texture(detail_tex, uvx).rgb;
		vec3 d_y = texture(detail_tex, uvy).rgb;
		vec3 d_z = texture(detail_tex, uvz).rgb;
		vec3 detail = d_x * w.x + d_y * w.y + d_z * w.z;

		float lum = dot(detail, vec3(0.299, 0.587, 0.114));
		vec3 heat = mix(vec3(0.78, 0.38, 0.14), vec3(1.18, 0.9, 0.56), pow(clamp(lum, 0.0, 1.0), 0.65));
		vec3 boosted = sun_col * (0.82 + lum * 0.58);
		sun_col = mix(sun_col, mix(boosted, heat, 0.35), clamp(detail_mix, 0.0, 0.32));
	}

	float pulse = 1.02 + sin(TIME * 1.58 + plasma * 8.0) * pulse_strength;
	float flicker = 0.985 + 0.04 * sin(TIME * 8.4 + n2 * 12.0);
	sun_col *= pulse * flicker;
	sun_col = max(sun_col, base_color.rgb * 0.96);

	ALBEDO = sun_col * 0.72 * brightness_multiplier;
	EMISSION = sun_col * (emission_strength + plasma * 10.5) * brightness_multiplier;
	ALPHA = 1.0;
}
