shader_type canvas_item;

uniform float ocean_level : hint_range(0.0, 1.0) = 0.35;
uniform float temperature : hint_range(0.0, 1.0) = 0.5;
uniform float atmosphere_density : hint_range(0.0, 1.0) = 0.5;
uniform float cloud_speed : hint_range(0.0, 2.0) = 0.2;
uniform float element_type : hint_range(0.0, 3.0) = 0.0;
uniform float time_sec = 0.0;

float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise2d(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash12(i);
	float b = hash12(i + vec2(1.0, 0.0));
	float c = hash12(i + vec2(0.0, 1.0));
	float d = hash12(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void fragment() {
	vec2 uv = UV * 2.0 - 1.0;
	float r = length(uv);
	if (r > 1.0) {
		COLOR = vec4(0.0);
	} else {
		float sphere_z = sqrt(max(0.0, 1.0 - r * r));
		vec3 normal = normalize(vec3(uv, sphere_z));

		float n = noise2d(normal.xy * 3.5 + vec2(0.0, time_sec * cloud_speed));
		float terrain = n * 0.75 + normal.z * 0.25;

		vec3 land_color;
		vec3 ocean_color;

		if (element_type < 0.5) {
			land_color = vec3(0.22, 0.56, 0.34);
			ocean_color = vec3(0.08, 0.26, 0.52);
		} else if (element_type < 1.5) {
			land_color = vec3(0.62, 0.18, 0.1);
			ocean_color = vec3(0.22, 0.06, 0.08);
		} else if (element_type < 2.5) {
			land_color = vec3(0.73, 0.86, 0.95);
			ocean_color = vec3(0.38, 0.56, 0.78);
		} else {
			land_color = vec3(0.68, 0.74, 0.82);
			ocean_color = vec3(0.34, 0.46, 0.68);
		}

		float temp_shift = (temperature - 0.5) * 0.35;
		land_color = mix(land_color * vec3(0.8, 0.9, 1.1), land_color * vec3(1.15, 0.95, 0.85), temperature);
		ocean_color = mix(ocean_color * vec3(0.8, 0.9, 1.15), ocean_color * vec3(1.1, 0.95, 0.88), temperature);

		float coastline = smoothstep(ocean_level - 0.03, ocean_level + 0.03, terrain);
		vec3 base = mix(ocean_color, land_color, coastline);

		float cloud = noise2d(normal.xy * 8.0 + vec2(time_sec * cloud_speed, 0.0));
		float cloud_mask = smoothstep(0.64, 0.88, cloud) * atmosphere_density;
		base = mix(base, vec3(0.95), cloud_mask * 0.45);

		vec3 light_dir = normalize(vec3(-0.5, 0.35, 0.9));
		float diffuse = clamp(dot(normal, light_dir), 0.0, 1.0);
		float fresnel = pow(1.0 - max(dot(normal, vec3(0.0, 0.0, 1.0)), 0.0), 2.5) * atmosphere_density;

		vec3 shaded = base * (0.35 + diffuse * 0.85);
		shaded += vec3(0.35, 0.5, 0.85) * fresnel * 0.45;

		float edge = smoothstep(1.0, 0.92, r);
		COLOR = vec4(shaded + temp_shift * vec3(0.2, 0.08, -0.15), edge);
	}
}
