using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanSwapchain : IDisposable
    {
        public Swapchain NativeSwapchain { get; private set; }

        private Device nativeDevice;
        private VulkanSurface surface;
        private Image[] images;
        private ImageView[] imageViews;

        public VulkanSwapchain(LogicalDevice device, VulkanSurface surface)
        {
            nativeDevice = device.NativeDevice;
            this.surface = surface;

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
                Surface = surface.NativeSurface,
            };
            NativeSwapchain = nativeDevice.CreateSwapchain(ref createInfo);

            images = nativeDevice.GetSwapchainImages(NativeSwapchain);
            CreateImageViews();
        }

        void CreateImageViews()
        {
            imageViews = new ImageView[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                ImageViewCreateInfo createInfo = new ImageViewCreateInfo
                {
                    StructureType = StructureType.ImageViewCreateInfo,
                    Components = ComponentMapping.Identity,
                    Format = surface.Format.Format,
                    Image = images[i],
                    SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, 1, 0, 1),
                    ViewType = ImageViewType.Image2D,
                };
                imageViews[i] = nativeDevice.CreateImageView(ref createInfo);
            }
        }

        public void Dispose()
        {
            foreach (ImageView imageView in imageViews)
                nativeDevice.DestroyImageView(imageView);

            nativeDevice.DestroySwapchain(NativeSwapchain);
            GC.SuppressFinalize(this);
        }

        ~VulkanSwapchain()
        {
            Dispose();
        }
    }
}