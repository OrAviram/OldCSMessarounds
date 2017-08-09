#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(set = 0, binding = 0) uniform UniformMVPMatrices
{
	mat4 model;
	mat4 view;
	mat4 projection;
} mvpMatrices;

layout(location = 0) in vec2 inVertexPosition;
layout(location = 1) in vec3 inVertexColor;

layout(location = 0) out vec3 vertexColor;

void main()
{
	//gl_Position = mvpMatrices.projection * mvpMatrices.view * mvpMatrices.model * vec4(inVertexPosition, 0, 1);
	gl_Position = vec4(inVertexPosition, 0, 1);
	//vertexColor = inVertexColor;
	vertexColor = vec3(mvpMatrices.model[0][0], mvpMatrices.model[0][1], mvpMatrices.view[1][1]);
}