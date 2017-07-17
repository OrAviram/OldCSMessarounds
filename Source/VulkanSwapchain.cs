using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanSwapchain : IDisposable
    {
        public Swapchain NativeSwapchain { get; private set; }

        private LogicalDevice device;

        public VulkanSwapchain(LogicalDevice device, VulkanSurface surface)
        {
            this.device = device;

            // TODO: Actually create swapchain.
        }

        public void Dispose()
        {
            device.NativeDevice.DestroySwapchain(NativeSwapchain);
            GC.SuppressFinalize(this);
        }

        ~VulkanSwapchain()
        {
            Dispose();
        }
    }
}