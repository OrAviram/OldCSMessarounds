#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(location = 0) in vec2 inVertexPosition;
layout(location = 1) in vec3 inVertexColor;

layout(location = 0) out vec3 vertexColor;

void main()
{
	gl_Position = vec4(inVertexPosition, 0, 1);
	vertexColor = inVertexColor;
}