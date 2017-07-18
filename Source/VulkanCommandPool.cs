using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanCommandPool : IDisposable
    {
        public CommandPool NativeCommandPool { get; private set; }
        private Device device;

        public VulkanCommandPool(Device device, uint queueFamilyIndex)
        {
            this.device = device;
            CommandPoolCreateInfo createInfo = new CommandPoolCreateInfo
            {
                StructureType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamilyIndex,
            };
            NativeCommandPool = device.CreateCommandPool(ref createInfo);
        }

        public void Dispose()
        {
            device.DestroyCommandPool(NativeCommandPool);
            GC.SuppressFinalize(this);
        }

        ~VulkanCommandPool()
        {
            Dispose();
        }
    }
}