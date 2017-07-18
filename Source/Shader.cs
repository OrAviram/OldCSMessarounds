using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class Shader : IDisposable
    {
        public ShaderModule NativeShaderModule { get; private set; }
        public PipelineShaderStageCreateInfo PipelineShaderStageCreateInfo { get; private set; }
        private Device device;

        private Shader(Device device, IntPtr code, int size, ShaderStageFlags shaderStage)
        {
            this.device = device;
            ShaderModuleCreateInfo createInfo = new ShaderModuleCreateInfo
            {
                StructureType = StructureType.ShaderModuleCreateInfo,
                Code = code,
                CodeSize = (uint)size,
            };
            NativeShaderModule = device.CreateShaderModule(ref createInfo);

            CreateShaderStage(shaderStage);
        }

        void CreateShaderStage(ShaderStageFlags shaderStage)
        {
            PipelineShaderStageCreateInfo = new PipelineShaderStageCreateInfo
            {
                StructureType = StructureType.PipelineShaderStageCreateInfo,
                Module = NativeShaderModule,
                Name = Marshal.StringToHGlobalAnsi("main"),
                SpecializationInfo = IntPtr.Zero,
                Stage = shaderStage,
            };
        }

        public static Shader LoadShader(string path, LogicalDevice device, ShaderStageFlags shaderStage)
        {
            byte[] file = File.ReadAllBytes(path);
            fixed (void* codePtr = &file[0])
                return new Shader(device.NativeDevice, (IntPtr)codePtr, file.Length, shaderStage);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(PipelineShaderStageCreateInfo.Name);
            device.DestroyShaderModule(NativeShaderModule);
            GC.SuppressFinalize(this);
        }

        ~Shader()
        {
            Dispose();
        }
    }
}
