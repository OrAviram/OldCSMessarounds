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

            SwapchainCreateInfo createInfo = new SwapchainCreateInfo
            {
                StructureType = StructureType.SwapchainCreateInfo,
                Clipped = true,
                CompositeAlpha = CompositeAlphaFlags.Opaque,
                ImageArrayLayers = 1,
                ImageColorSpace = surface.Format.ColorSpace,
                ImageExtent = surface.ImageExtents,
                ImageFormat = surface.Format.Format,
                ImageSharingMode = SharingMode.Exclusive,
                ImageUsage = ImageUsageFlags.ColorAttachment,
                MinImageCount = surface.ImageCount,
                OldSwapchain = Swapchain.Null,
                PresentMode = surface.PresentMode,
                PreTransform = surface.Capabilities.CurrentTransform,
                QueueFamilyIndexCount = 0,
                QueueFamilyIndices = IntPtr.Zero,
                Surface = surface.NativeSurface
            };
            NativeSwapchain = device.NativeDevice.CreateSwapchain(ref createInfo);
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