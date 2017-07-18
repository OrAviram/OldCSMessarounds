using System;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanSurface : IDisposable
    {
        public Surface NativeSurface { get; private set; }
        public SurfaceCapabilities Capabilities { get; private set; }
        public Extent2D ImageExtents { get; private set; }
        public uint MinImageCount { get; private set; }
        public SurfaceFormat Format { get; private set; }
        public PresentMode PresentMode { get; private set; }

        private Instance nativeInstance;
        private PhysicalDevice nativePhysicalDevice;
        private Window window;

        public VulkanSurface(Window window, VulkanInstance instance, VulkanPhysicalDevice physicalDevice)
        {
            nativeInstance = instance.NativeInstance;
            nativePhysicalDevice = physicalDevice.NativeDevice;
            this.window = window;

            Win32SurfaceCreateInfo createInfo = new Win32SurfaceCreateInfo
            {
                StructureType = StructureType.Win32SurfaceCreateInfo,
                InstanceHandle = Marshal.GetHINSTANCE(GetType().Module),
                WindowHandle = window.Win32Handle,
            };
            NativeSurface = nativeInstance.CreateWin32Surface(ref createInfo);

            bool surfaceSupport = nativePhysicalDevice.GetSurfaceSupport((uint)physicalDevice.QueueFamilyIndices.graphicsFamily, NativeSurface);
            if (!surfaceSupport)
                throw new Exception("WSI is not supported.");

            nativePhysicalDevice.GetSurfaceCapabilities(NativeSurface, out SurfaceCapabilities capabilities);
            Capabilities = capabilities;

            ChooseExtents();
            CalculateImageCount();
            ChooseFormat(nativePhysicalDevice.GetSurfaceFormats(NativeSurface));
            ChoosePresentMode(nativePhysicalDevice.GetSurfacePresentModes(NativeSurface));
        }

        void ChooseExtents()
        {
            if (Capabilities.CurrentExtent.Width != uint.MaxValue)
                ImageExtents = Capabilities.CurrentExtent;
            else
            {
                ImageExtents = new Extent2D((uint)window.Width, (uint)window.Height);
                ImageExtents = new Extent2D(
                    Math.Max(Capabilities.MinImageExtent.Width, Math.Min(Capabilities.MaxImageExtent.Width, ImageExtents.Width)),
                    Math.Max(Capabilities.MinImageExtent.Height, Math.Min(Capabilities.MaxImageExtent.Height, ImageExtents.Height)));
            }
        }

        void CalculateImageCount()
        {
            MinImageCount = Capabilities.MinImageCount + 1;
            if (Capabilities.MaxImageCount > 0 && MinImageCount > Capabilities.MaxImageCount)
                MinImageCount = Capabilities.MaxImageCount;
        }

        void ChooseFormat(SurfaceFormat[] availableFormats)
        {
            if (availableFormats.Length == 1 && availableFormats[0].Format == SharpVulkan.Format.Undefined)
            {
                Format = new SurfaceFormat { Format = SharpVulkan.Format.B8G8R8A8UNorm, ColorSpace = ColorSpace.SRgbNonlinear };
                return;
            }

            foreach (SurfaceFormat format in availableFormats)
            {
                if (format.Format == SharpVulkan.Format.B8G8R8A8UNorm && format.ColorSpace == ColorSpace.SRgbNonlinear)
                {
                    Format = format;
                    return;
                }
            }
            Format = availableFormats[0];
        }

        void ChoosePresentMode(PresentMode[] availablePresentModes)
        {
            PresentMode bestMode = PresentMode.Fifo;
            foreach (PresentMode presentMode in availablePresentModes)
            {
                if (presentMode == PresentMode.Mailbox)
                {
                    PresentMode = presentMode;
                    return;
                }
                else if (presentMode == PresentMode.Immediate)
                {
                    bestMode = presentMode;
                }
            }
            PresentMode = bestMode;
        }

        public void Dispose()
        {
            nativeInstance.DestroySurface(NativeSurface);
            GC.SuppressFinalize(this);
        }

        ~VulkanSurface()
        {
            Dispose();
        }
    }
}