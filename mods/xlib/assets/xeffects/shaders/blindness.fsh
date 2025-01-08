#version 330 core
#extension GL_ARB_explicit_attrib_location: enable
uniform sampler2D primaryScene;
uniform float intensity;
uniform float flow;
uniform float time;
in vec2 uv;
out vec4 outColor;
void main () 
{
    vec4 color = texture(primaryScene, uv);
    vec2 v = vec2(uv.x - 0.5, uv.y - 0.5);
    float distance = sqrt(v.x * v.x + v.y * v.y) / 0.70710678118;
    float inten = (1.0 - distance) * (1.0 - intensity);

    outColor.r = color.r * inten;
    outColor.g = color.g * inten;
    outColor.b = color.b * inten;
    outColor.a = color.a;
}