using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanPipelineLayout : IDisposable
    {
        public PipelineLayout NativeLayout { get; private set; }
        private Device device;

        public VulkanPipelineLayout(Device device, DescriptorSetLayout[] descriptorSetLayouts)
        {
            this.device = device;

            PipelineLayoutCreateInfo createInfo = new PipelineLayoutCreateInfo
            {
                StructureType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,
                PushConstantRanges = IntPtr.Zero,
            };

            if (descriptorSetLayouts != null && descriptorSetLayouts.Length != 0)
            {
                fixed(DescriptorSetLayout* descriptorSetLayoutsPtr = &descriptorSetLayouts[0])
                {
                    createInfo.SetLayoutCount = (uint)descriptorSetLayouts.Length;
                    createInfo.SetLayouts = (IntPtr)descriptorSetLayoutsPtr;
                }
            }
            NativeLayout = device.CreatePipelineLayout(ref createInfo);
        }

        void IDisposable.Dispose()
        {
            device.DestroyPipelineLayout(NativeLayout);
        }

        public void Dispose(bool supressFinalize = true)
        {
            (this as IDisposable).Dispose();
            if (supressFinalize)
                GC.SuppressFinalize(this);
        }

        ~VulkanPipelineLayout()
        {
            Dispose(false);
        }
    }
}
