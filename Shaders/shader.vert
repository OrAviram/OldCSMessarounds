#version 450
#extension GL_ARB_separate_shader_objects : enable

const float HEIGHT = .5;

vec2 vertices[6] = vec2[]
(
	vec2(-.5, -HEIGHT), vec2(0, HEIGHT), vec2(-1, HEIGHT),
	vec2(.5, -HEIGHT), vec2(1, HEIGHT), vec2(0, HEIGHT)
);

vec3 colors[6] = vec3[]
(
	vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1),
	vec3(1, 0, 0), vec3(0, 0, 1), vec3(0, 1, 0)
);

layout(location = 0) out vec3 vertexColor;

void main()
{
	gl_Position = vec4(vertices[gl_VertexIndex], 0, 1);
	vertexColor = colors[gl_VertexIndex];
	//vertexColor = vec3(1, 1, 1);
}