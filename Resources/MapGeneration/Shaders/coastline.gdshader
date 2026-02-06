shader_type canvas_item;

uniform vec4 ocean_color : source_color = vec4(0.16, 0.38, 0.65, 1.0);
uniform vec4 shallow_color : source_color = vec4(0.24, 0.48, 0.75, 1.0);
uniform float scale = 1.0;

void fragment() {
    vec2 uv = UV * scale;
    float wave = sin(uv.x * 10.0 + TIME) * 0.02;
    wave += sin(uv.y * 15.0 - TIME * 0.8) * 0.02;
    vec4 color = mix(ocean_color, shallow_color, wave + 0.5);
    COLOR = color;
}
