using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanSwapchain : IDisposable
    {
        public Swapchain NativeSwapchain { get; private set; }
        public int ImageCount { get; private set; }

        private Device nativeDevice;
        private VulkanSurface surface;
        private RenderPass renderPass;

        private Image[] images;
        private ImageView[] imageViews;
        private FrameBuffer[] frameBuffers;

        public VulkanSwapchain(LogicalDevice device, VulkanSurface surface, RenderPass renderPass)
        {
            nativeDevice = device.NativeDevice;
            this.surface = surface;
            this.renderPass = renderPass;

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
                MinImageCount = surface.MinImageCount,
                OldSwapchain = Swapchain.Null,
                PresentMode = surface.PresentMode,
                PreTransform = surface.Capabilities.CurrentTransform,
                QueueFamilyIndexCount = 0,
                QueueFamilyIndices = IntPtr.Zero,
                Surface = surface.NativeSurface,
            };
            NativeSwapchain = nativeDevice.CreateSwapchain(ref createInfo);

            images = nativeDevice.GetSwapchainImages(NativeSwapchain);
            ImageCount = images.Length;
            CreateImageViews();
        }

        public FrameBuffer GetFrameBuffer(int index)
        {
            return frameBuffers[index];
        }

        void CreateImageViews()
        {
            imageViews = new ImageView[ImageCount];
            frameBuffers = new FrameBuffer[ImageCount];
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
                frameBuffers[i] = new FrameBuffer(nativeDevice, renderPass, new ImageView[] { imageViews[i] }, surface);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < imageViews.Length; i++)
            {
                nativeDevice.DestroyImageView(imageViews[i]);
                frameBuffers[i].Dispose();
            }

            nativeDevice.DestroySwapchain(NativeSwapchain);
            GC.SuppressFinalize(this);
        }

        ~VulkanSwapchain()
        {
            Dispose();
        }
    }
}