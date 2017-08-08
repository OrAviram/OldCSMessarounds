using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class UniformBuffer<T> : Buffer<T>
        where T : struct
    {
        public DescriptorSetLayout NativeDescriptorSetLayout { get; private set; }

        private Device device;

        public UniformBuffer(PhysicalDevice physicalDevice, Device device, uint binding, ShaderStageFlags shaderStages, T[] bufferData)
            : base(physicalDevice, device, bufferData, BufferUsageFlags.UniformBuffer)
        {
            this.device = device;
            
            DescriptorSetLayoutBinding layoutBinding = new DescriptorSetLayoutBinding
            {
                Binding = binding,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = shaderStages,
                ImmutableSamplers = IntPtr.Zero,
            };

            DescriptorSetLayoutCreateInfo createInfo = new DescriptorSetLayoutCreateInfo
            {
                StructureType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 1,
                Bindings = new IntPtr(&layoutBinding),
            };
            NativeDescriptorSetLayout = device.CreateDescriptorSetLayout(ref createInfo);
        }

        public override void Dispose()
        {
            base.Dispose();
            device.DestroyDescriptorSetLayout(NativeDescriptorSetLayout);
        }

        ~UniformBuffer()
        {
            Dispose();
        }
    }
}