shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec2 radii = vec2(32.0);
uniform float pulse_strength : hint_range(0.0, 1.0) = 0.0;
uniform vec4 glow_color : source_color = vec4(0.22, 0.65, 1.0, 1.0);
varying vec2 local_position;

void vertex() {
    local_position = VERTEX;
}

void fragment() {
    vec2 normalized_position = local_position / radii;
    float radial_length = length(normalized_position);
    // Gradient normalization keeps the boundary band approximately constant in local pixels.
    float gradient = length(normalized_position / radii) / max(radial_length, 0.0001);
    float distance_to_edge = abs(radial_length - 1.0) / max(gradient, 0.0001);
    float antialias = max(fwidth(distance_to_edge), 0.25);
    float core = 1.0 - smoothstep(0.6, 0.6 + antialias, distance_to_edge);
    float halo = exp(-distance_to_edge * distance_to_edge / 12.0);
    vec3 tint = mix(glow_color.rgb, vec3(0.85, 0.96, 1.0), core);
    COLOR = vec4(tint, (core * 0.85 + halo * 0.4) * pulse_strength * glow_color.a);
}
