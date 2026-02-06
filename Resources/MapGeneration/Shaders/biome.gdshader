shader_type canvas_item;

uniform sampler2D biome_colors : filter_nearest;
uniform sampler2D biome_map;

void fragment() {
    float biome_id = texture(biome_map, UV).r * 13.0;
    vec2 color_uv = vec2(biome_id / 13.0 + 0.03846, 0.5);
    COLOR = texture(biome_colors, color_uv);
}
