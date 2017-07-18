using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanSemaphore : IDisposable
    {
        private static SemaphoreCreateInfo createInfo = new SemaphoreCreateInfo
        {
            StructureType = StructureType.SemaphoreCreateInfo,
        };

        public Semaphore NativeSemaphore { get; private set; }
        private Device device;

        public VulkanSemaphore(Device device)
        {
            this.device = device;
            NativeSemaphore = device.CreateSemaphore(ref createInfo);
        }

        public void Dispose()
        {
            device.DestroySemaphore(NativeSemaphore);
            GC.SuppressFinalize(this);
        }

        ~VulkanSemaphore()
        {
            Dispose();
        }
    }
}