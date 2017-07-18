using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class Shader : IDisposable
    {
        public ShaderModule NativeShaderModule { get; private set; }
        public ShaderStageFlags PipelineShaderStage { get; private set; }
        private Device device;

        private Shader(Device device, IntPtr code, int size, ShaderStageFlags shaderStage)
        {
            PipelineShaderStage = shaderStage;
            this.device = device;

            ShaderModuleCreateInfo createInfo = new ShaderModuleCreateInfo
            {
                StructureType = StructureType.ShaderModuleCreateInfo,
                Code = code,
                CodeSize = (uint)size,
            };
            NativeShaderModule = device.CreateShaderModule(ref createInfo);
        }

        public static Shader LoadShader(string path, LogicalDevice device, ShaderStageFlags shaderStage)
        {
            byte[] file = File.ReadAllBytes(path);
            fixed (void* codePtr = &file[0])
                return new Shader(device.NativeDevice, (IntPtr)codePtr, file.Length, shaderStage);
        }

        public void Dispose()
        {
            device.DestroyShaderModule(NativeShaderModule);
            GC.SuppressFinalize(this);
        }

        ~Shader()
        {
            Dispose();
        }
    }
}
