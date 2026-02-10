shader_type canvas_item;

uniform float ocean_level : hint_range(0.0, 1.0) = 0.56;
uniform float temperature : hint_range(0.0, 1.0) = 0.52;
uniform float atmosphere_density : hint_range(0.0, 1.0) = 0.62;
uniform float mountain_intensity : hint_range(0.0, 1.0) = 0.55;
uniform float polar_coverage : hint_range(0.0, 1.0) = 0.55;
uniform float desert_ratio : hint_range(0.0, 1.0) = 0.45;
uniform float plate_count : hint_range(1.0, 64.0) = 24.0;
uniform float wind_cells : hint_range(1.0, 24.0) = 8.0;
uniform float erosion_iterations : hint_range(0.0, 16.0) = 4.0;
uniform float erosion_strength : hint_range(0.0, 1.0) = 0.16;
uniform float heat_factor : hint_range(1.0, 1000.0) = 560.0;
uniform float cloud_speed : hint_range(0.0, 2.0) = 0.2;
uniform float element_type : hint_range(0.0, 3.0) = 0.0;
uniform float preview_exposure : hint_range(0.6, 1.6) = 1.0;
uniform float preview_contrast : hint_range(0.7, 1.4) = 1.0;
uniform float preview_saturation : hint_range(0.6, 1.4) = 0.92;
uniform float rim_strength : hint_range(0.0, 1.0) = 0.38;
uniform float time_sec = 0.0;

const float PI = 3.14159265359;

const vec3 OCEAN_DEEP = vec3(0.05000, 0.19000, 0.37000);
const vec3 OCEAN_MID = vec3(0.16000, 0.42000, 0.62000);
const vec3 OCEAN_SHALLOW = vec3(0.32000, 0.61000, 0.75000);
const vec3 COASTLAND = vec3(0.82000, 0.79000, 0.63000);
const vec3 TROPICAL_RAIN_FOREST = vec3(0.00000, 0.28235, 0.00000);
const vec3 TROPICAL_SEASONAL_FOREST = vec3(0.04706, 0.55294, 0.04706);
const vec3 SHRUBLAND = vec3(0.37647, 0.47059, 0.09412);
const vec3 SAVANNAH = vec3(0.95686, 0.95686, 0.54510);
const vec3 TROPICAL_DESERT = vec3(0.65882, 0.37647, 0.28235);
const vec3 TEMPERATE_RAIN_FOREST = vec3(0.39216, 0.70588, 0.39216);
const vec3 TEMPERATE_SEASONAL_FOREST = vec3(0.38431, 0.56078, 0.33725);
const vec3 CHAPARRAL = vec3(0.56078, 0.51765, 0.60392);
const vec3 GRASSLAND = vec3(0.56471, 0.84706, 0.28235);
const vec3 STEPPE = vec3(0.74902, 0.74902, 0.74902);
const vec3 TEMPERATE_DESERT = vec3(0.84706, 0.65882, 0.47059);
const vec3 BOREAL_FOREST = vec3(0.00000, 0.37647, 0.28235);
const vec3 TAIGA = vec3(0.28235, 0.56471, 0.56471);
const vec3 TUNDRA = vec3(0.54902, 0.80000, 0.74118);
const vec3 ICE = vec3(0.70196, 0.92549, 1.00000);
const vec3 ROCKY_MOUNTAIN = vec3(0.52000, 0.50000, 0.46000);
const vec3 SNOWY_MOUNTAIN = vec3(0.90196, 0.95294, 1.00000);
const vec3 RIVER = vec3(0.20000, 0.49000, 0.71000);

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

float fbm(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	float frequency = 1.0;
	for (int i = 0; i < 5; i++) {
		value += noise2d(p * frequency) * amplitude;
		frequency *= 2.0;
		amplitude *= 0.5;
	}
	return value;
}

float ridge(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	float frequency = 1.0;
	for (int i = 0; i < 4; i++) {
		float n = noise2d(p * frequency);
		n = 1.0 - abs(n * 2.0 - 1.0);
		value += n * amplitude;
		frequency *= 2.05;
		amplitude *= 0.56;
	}
	return value;
}

vec3 element_land_tint(float element) {
	if (element < 0.5) {
		return vec3(0.30, 0.52, 0.31);
	} else if (element < 1.5) {
		return vec3(0.62, 0.18, 0.10);
	} else if (element < 2.5) {
		return vec3(0.73, 0.86, 0.95);
	}
	return vec3(0.68, 0.74, 0.82);
}

vec3 element_ocean_tint(float element) {
	if (element < 0.5) {
		return vec3(0.08, 0.26, 0.56);
	} else if (element < 1.5) {
		return vec3(0.22, 0.06, 0.08);
	} else if (element < 2.5) {
		return vec3(0.38, 0.56, 0.78);
	}
	return vec3(0.34, 0.46, 0.68);
}

vec3 resolve_biome_color(float temp, float humidity, float relief, float lat01, float forest_pref, float grass_pref) {
	float mountain_gate = mix(0.62, 0.46, mountain_intensity);
	if (relief > mountain_gate) {
		float snow_gate = mix(0.52, 0.34, temp) + lat01 * 0.24;
		return relief > snow_gate ? SNOWY_MOUNTAIN : ROCKY_MOUNTAIN;
	}

	float humidity_adjusted = clamp(humidity + forest_pref * 0.18 - grass_pref * 0.12 - desert_ratio * 0.18, 0.0, 1.0);
	float aridity = clamp(1.0 - humidity_adjusted + desert_ratio * 0.22 + grass_pref * 0.12, 0.0, 1.0);
	float polar_mask = clamp(
		(lat01 - mix(0.82, 0.56, polar_coverage)) * 2.4
		+ (0.42 - temp) * 1.05,
		0.0,
		1.0);

	if (polar_mask > 0.78) {
		return ICE;
	}

	if (polar_mask > 0.52) {
		return TUNDRA;
	}

	if (temp >= 0.72) {
		if (aridity < 0.18 - forest_pref * 0.06) return TROPICAL_RAIN_FOREST;
		if (aridity < 0.32) return TROPICAL_SEASONAL_FOREST;
		if (aridity < 0.46 + grass_pref * 0.12) return SAVANNAH;
		if (aridity < 0.62) return SHRUBLAND;
		return TROPICAL_DESERT;
	}

	if (temp >= 0.54) {
		if (aridity < 0.20 - forest_pref * 0.05) return TEMPERATE_RAIN_FOREST;
		if (aridity < 0.34) return TEMPERATE_SEASONAL_FOREST;
		if (aridity < 0.46 + grass_pref * 0.18) return GRASSLAND;
		if (aridity < 0.62 + grass_pref * 0.10) return SHRUBLAND;
		if (aridity < 0.74) return CHAPARRAL;
		return TEMPERATE_DESERT;
	}

	if (temp >= 0.36) {
		if (aridity < 0.24 - forest_pref * 0.04) return BOREAL_FOREST;
		if (aridity < 0.42) return TAIGA;
		if (aridity < 0.62 + grass_pref * 0.14) return STEPPE;
		return TEMPERATE_DESERT;
	}

	return aridity < 0.48 ? TUNDRA : ICE;
}

void fragment() {
	vec2 uv = UV * 2.0 - 1.0;
	float r = length(uv);
	if (r > 1.0) {
		COLOR = vec4(0.0);
		return;
	}

	float plate_norm = clamp((plate_count - 1.0) / 63.0, 0.0, 1.0);
	float wind_norm = clamp((wind_cells - 1.0) / 23.0, 0.0, 1.0);
	float erosion_norm = clamp(erosion_strength * (erosion_iterations / 16.0), 0.0, 1.0);
	float heat_profile = clamp((1000.0 - heat_factor) / 999.0, 0.0, 1.0);

	float sphere_z = sqrt(max(0.0, 1.0 - r * r));
	vec3 normal = normalize(vec3(uv, sphere_z));
	float lon = atan(normal.y, normal.x) / (2.0 * PI) + 0.5;
	float lat = asin(normal.z) / PI + 0.5;
	vec2 sphere_uv = vec2(lon, lat);
	float lat01 = abs(lat - 0.5) * 2.0;

	vec2 plate_grid = vec2(mix(5.0, 24.0, plate_norm), mix(3.0, 12.0, plate_norm));
	vec2 plate_uv = (floor(sphere_uv * plate_grid) + 0.5) / plate_grid;
	float plate_chunk = fbm(plate_uv * vec2(3.0, 3.0) + vec2(-5.0, 9.0));

	float macro = fbm(sphere_uv * mix(vec2(1.6, 1.6), vec2(3.0, 3.0), plate_norm) + vec2(17.1, -4.7));
	float tectonic = fbm(sphere_uv * mix(vec2(2.8, 2.8), vec2(5.2, 5.2), plate_norm) + vec2(-11.3, 29.7));
	float detail = fbm(sphere_uv * mix(vec2(6.0, 6.0), vec2(12.0, 12.0), plate_norm) + vec2(41.9, -63.5));
	float ridges = ridge(sphere_uv * mix(vec2(10.0, 10.0), vec2(16.0, 16.0), plate_norm) + vec2(-7.0, 8.0));

	float sea_level = clamp(ocean_level, 0.14, 0.84);
	float thermal_sea_shift = (heat_profile - 0.5) * 0.06;
	float terrain = plate_chunk * 0.34
		+ macro * 0.26
		+ tectonic * 0.18
		+ detail * 0.12
		+ ridges * 0.10
		- lat01 * 0.06
		- thermal_sea_shift * 0.40;
	terrain = clamp(terrain, 0.0, 1.0);
	terrain = mix(terrain, smoothstep(0.18, 0.84, terrain), 0.36 + plate_norm * 0.22 + erosion_norm * 0.14);
	float terrain_steps = mix(7.0, 17.0, plate_norm);
	terrain = floor(terrain * terrain_steps) / terrain_steps;

	bool is_land = terrain >= sea_level;
	float relief = clamp((terrain - sea_level) / max(0.0001, 1.0 - sea_level), 0.0, 1.0);

	vec2 climate_grid = vec2(mix(6.0, 30.0, wind_norm * 0.75 + plate_norm * 0.25), mix(4.0, 14.0, wind_norm));
	vec2 climate_uv = (floor(sphere_uv * climate_grid) + 0.5) / climate_grid;
	float climate_chunk = fbm(climate_uv * vec2(3.2, 3.2) + vec2(12.7, -3.5));
	float moisture_noise = fbm(sphere_uv * mix(vec2(3.0, 3.0), vec2(8.0, 8.0), wind_norm) + vec2(time_sec * cloud_speed * 0.015, 13.0));

	float lat_heat = 1.0 - pow(lat01, mix(1.9, 0.66, heat_profile));
	float humidity = clamp(
		climate_chunk * (0.66 + atmosphere_density * 0.24)
		+ moisture_noise * 0.14
		+ sea_level * 0.22
		+ (1.0 - lat01) * 0.12
		- relief * 0.18
		- desert_ratio * 0.08,
		0.0,
		1.0);
	float temp_field = clamp(lat_heat * 0.66 + temperature * 0.34 - relief * 0.31 + (climate_chunk - 0.5) * 0.08 * wind_norm, 0.0, 1.0);
	float climate_steps = mix(6.0, 14.0, wind_norm);
	humidity = floor(humidity * climate_steps) / climate_steps;
	temp_field = floor(temp_field * climate_steps) / climate_steps;

	float forest_pref = clamp(atmosphere_density * 0.52 + wind_norm * 0.36 + (1.0 - desert_ratio) * 0.16 - desert_ratio * 0.28, 0.0, 1.0);
	float grass_pref = clamp(desert_ratio * 0.58 + (1.0 - forest_pref) * 0.42 + erosion_norm * 0.15, 0.0, 1.0);

	vec3 base;
	if (!is_land) {
		if (terrain < sea_level * 0.5714) {
			base = OCEAN_DEEP;
		} else if (terrain < sea_level * 0.8) {
			base = OCEAN_MID;
		} else {
			base = OCEAN_SHALLOW;
		}

		float depth = clamp((sea_level - terrain) / max(0.0001, sea_level), 0.0, 1.0);
		float ocean_ice = clamp((lat01 - 0.74) * 2.4 + (0.5 - temp_field) * 0.7, 0.0, 1.0);
		base = mix(base, ICE, ocean_ice * 0.42);
		base = mix(base, base * 0.74, depth * 0.28);
	} else {
		base = resolve_biome_color(temp_field, humidity, relief, lat01, forest_pref, grass_pref);

		float coast_band = mix(0.022, 0.052, erosion_norm);
		float coast_mix = 1.0 - clamp(abs(terrain - sea_level) / coast_band, 0.0, 1.0);
		base = mix(base, COASTLAND, coast_mix * 0.68);

		float river_noise = abs(noise2d(sphere_uv * vec2(64.0, 64.0) + vec2(3.0, -9.0)) - 0.5);
		float river_width = mix(0.022, 0.032, erosion_norm);
		float river_mask = smoothstep(river_width, 0.0, river_noise)
			* step(0.06, relief)
			* step(relief, 0.56)
			* step(mix(0.74, 0.62, forest_pref), humidity);
		base = mix(base, RIVER, river_mask * 0.74);
	}

	if (element_type > 0.5) {
		vec3 element_land = element_land_tint(element_type);
		vec3 element_ocean = element_ocean_tint(element_type);
		vec3 element_mix = is_land ? element_land : element_ocean;
		float tint_strength = is_land ? 0.08 : 0.12;
		base = mix(base, element_mix, tint_strength);
	}

	float cloud = fbm(sphere_uv * vec2(11.0, 8.5) + vec2(time_sec * cloud_speed * 0.08, 0.0));
	float cloud_mask = smoothstep(0.60, 0.84, cloud) * atmosphere_density;
	base = mix(base, vec3(0.95), cloud_mask * 0.36);

	vec3 light_dir = normalize(vec3(-0.5, 0.35, 0.9));
	float diffuse = clamp(dot(normal, light_dir), 0.0, 1.0);
	float fresnel = pow(1.0 - max(dot(normal, vec3(0.0, 0.0, 1.0)), 0.0), 2.6) * atmosphere_density;

	vec3 shaded = base * (0.34 + diffuse * 0.88);
	shaded += vec3(0.35, 0.5, 0.85) * fresnel * rim_strength;

	shaded *= preview_exposure;
	shaded = (shaded - 0.5) * preview_contrast + 0.5;
	float gray = dot(shaded, vec3(0.299, 0.587, 0.114));
	shaded = mix(vec3(gray), shaded, preview_saturation);
	shaded = clamp(shaded, 0.0, 1.0);

	float edge = smoothstep(1.0, 0.92, r);
	COLOR = vec4(shaded, edge);
}
