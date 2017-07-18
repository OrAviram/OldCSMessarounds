#version 450
#extension GL_ARB_separate_shader_objects : enable

out gl_PerVertex
{
	vec4 gl_Position;
};

const float SIZE = .5;
vec2 vertices[3] = vec2[] (vec2(0, -SIZE), vec2(SIZE, SIZE), vec2(-SIZE, SIZE));
vec3 colors[3] = vec3[] (vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1));

layout(location = 0) out vec3 vertexColor;

void main()
{
	gl_Position = vec4(vertices[gl_VertexIndex], 0, 1);
	vertexColor = colors[gl_VertexIndex];
}