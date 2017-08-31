#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(binding = 0) uniform MVPMatrices
{
	mat4 model;
	//mat4 view;
	//mat4 projection;
} mvpMatrices;

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;

layout(location = 5) out vec4 vertexColor;

void main()
{
	gl_Position = /*mvpMatrices.projection * mvpMatrices.view * */ mvpMatrices.model * vec4(position, 1);
	vertexColor = color;
}