shader_type canvas_item;

uniform vec4 river_color : source_color = vec4(0.2, 0.4, 0.8, 0.8);
uniform float width_factor = 1.0;

void fragment() {
    COLOR = river_color;
}
