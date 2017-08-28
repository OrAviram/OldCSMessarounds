#version 450
#extension GL_ARB_separate_shader_objects : enable

const float SIZE = .5;

vec3 positions[] = vec3[](
	vec3(0, -SIZE, 0),
	vec3(SIZE, SIZE, 0),
	vec3(-SIZE, SIZE, 0)
	);

vec4 colors[] = vec4[](
	vec4(1, 0, 0, 1),
	vec4(0, 1, 0, 1),
	vec4(0, 0, 1, 1)
	);

layout(location = 0) out vec4 vertexColor;

void main()
{
	gl_Position = vec4(positions[gl_VertexIndex], 1);
	vertexColor = colors[gl_VertexIndex];
}