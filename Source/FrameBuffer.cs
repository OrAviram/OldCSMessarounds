using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class FrameBuffer : IDisposable
    {
        public Framebuffer NativeFrameBuffer { get; private set; }
        private Device device;

        public FrameBuffer(Device device, RenderPass renderPass, ImageView[] attachments, VulkanSurface surface)
        {
            this.device = device;

            fixed(void* attachmentsPtr = &attachments[0])
            {
                FramebufferCreateInfo createInfo = new FramebufferCreateInfo
                {
                    StructureType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = (uint)attachments.Length,
                    Attachments = (IntPtr)attachmentsPtr,
                    Width = surface.ImageExtents.Width,
                    Height = surface.ImageExtents.Height,
                    Layers = 1,
                };
                NativeFrameBuffer = device.CreateFramebuffer(ref createInfo);
            }
        }

        public void Dispose()
        {
            device.DestroyFramebuffer(NativeFrameBuffer);
            GC.SuppressFinalize(this);
        }

        ~FrameBuffer()
        {
            Dispose();
        }
    }
}