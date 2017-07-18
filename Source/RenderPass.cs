using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanRenderPass : IDisposable
    {
        public RenderPass NativeRenderPass { get; private set; }
        private Device nativeDevice;

        public VulkanRenderPass(LogicalDevice device, VulkanSurface surface)
        {
            nativeDevice = device.NativeDevice;

            AttachmentDescription colorAttachment = new AttachmentDescription
            {
                Format = surface.Format.Format,
                Samples = SampleCountFlags.Sample1,
                LoadOperation = AttachmentLoadOperation.Clear,
                StoreOperation = AttachmentStoreOperation.Store,
                StencilLoadOperation = AttachmentLoadOperation.DontCare,
                StencilStoreOperation = AttachmentStoreOperation.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSource,
            };

            AttachmentReference colorAttachmentReference = new AttachmentReference
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            SubpassDescription subpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                ColorAttachments = new IntPtr(&colorAttachmentReference),
            };

            RenderPassCreateInfo createInfo = new RenderPassCreateInfo
            {
                StructureType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                Attachments = new IntPtr(&colorAttachment),
                SubpassCount = 1,
                Subpasses = new IntPtr(&subpass),
            };
            NativeRenderPass = nativeDevice.CreateRenderPass(ref createInfo);
        }

        public void Dispose()
        {
            nativeDevice.DestroyRenderPass(NativeRenderPass);
            GC.SuppressFinalize(this);
        }

        ~VulkanRenderPass()
        {
            Dispose();
        }
    }
}