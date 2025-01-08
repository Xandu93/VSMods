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
    vec3 gray = vec3(0.5, 0.5, 0.5);
    outColor = vec4(vec3(dot(color.rgb, gray)), color.a);
}