using System;
using System.Linq;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public sealed unsafe class LogicalDevice : IDisposable
    {
        public Device NativeDevice { get; private set; }
        public Queue GraphicsQueue { get; private set; }
        public Queue PresentationQueue { get; private set; }
        public Swapchain Swapchain { get; private set; }

        private VulkanInstance instance;

        public LogicalDevice(VulkanInstance instance)
        {
            this.instance = instance;

            string[] extensionNames = instance.DeviceExtensionNames;
            uint[] uniqueQueueFamilies = new uint[] { instance.GraphicsQueueFamilyIndex, instance.PresentationQueueFamilyIndex };
            DeviceQueueCreateInfo* queueCreateInfos = stackalloc DeviceQueueCreateInfo[uniqueQueueFamilies.Length];

            float queuePriority = 1;
            for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                queueCreateInfos[i] = new DeviceQueueCreateInfo
                {
                    StructureType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = uniqueQueueFamilies[i],
                    QueueCount = 1,
                    QueuePriorities = new IntPtr(&queuePriority),
                };
            }

            IntPtr[] extensionsPtrPtr = extensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();
            fixed (void* extensionsPtr = &extensionsPtrPtr[0])
            {
                instance.PhysicalDevice.GetFeatures(out PhysicalDeviceFeatures features);
                DeviceCreateInfo createInfo = new DeviceCreateInfo
                {
                    StructureType = StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
                    QueueCreateInfos = (IntPtr)queueCreateInfos,
                    EnabledFeatures = new IntPtr(&features),
                    EnabledExtensionCount = (uint)extensionNames.Length,
                    EnabledExtensionNames = (IntPtr)extensionsPtr,
                };
                NativeDevice = instance.PhysicalDevice.CreateDevice(ref createInfo);
            }
            GraphicsQueue = NativeDevice.GetQueue(instance.GraphicsQueueFamilyIndex, 0);
            PresentationQueue = NativeDevice.GetQueue(instance.PresentationQueueFamilyIndex, 0);

            foreach (IntPtr extensionsPtr in extensionsPtrPtr)
                Marshal.FreeHGlobal(extensionsPtr);

            CreateSwapchain(uniqueQueueFamilies);
        }

        void CreateSwapchain(uint[] queueFamilies)
        {
            uint imageCount = instance.SurfaceCapabilities.MinImageCount + 1;
            if (instance.SurfaceCapabilities.MaxImageCount > 0 && imageCount > instance.SurfaceCapabilities.MaxImageCount)
                imageCount = instance.SurfaceCapabilities.MaxImageCount;

            fixed(void* queueFamiliesPtr = &queueFamilies[0])
            {
                SwapchainCreateInfo createInfo = new SwapchainCreateInfo
                {
                    StructureType = StructureType.SwapchainCreateInfo,
                    Surface = instance.Surface,
                    MinImageCount = imageCount,
                    ImageFormat = instance.SwapchainImageFormat.Format,
                    ImageColorSpace = instance.SwapchainImageFormat.ColorSpace,
                    ImageExtent = instance.SwapchainImageExtent,
                    ImageArrayLayers = 1,
                    ImageUsage = ImageUsageFlags.ColorAttachment,
                    PreTransform = instance.SurfaceCapabilities.CurrentTransform,
                    CompositeAlpha = CompositeAlphaFlags.Opaque,
                    PresentMode = instance.SurfacePresentMode,
                    Clipped = true,
                    OldSwapchain = Swapchain.Null,
                };

                if (instance.GraphicsQueueFamilyIndex != instance.PresentationQueueFamilyIndex)
                {
                    createInfo.ImageSharingMode = SharingMode.Concurrent;
                    createInfo.QueueFamilyIndexCount = 2;
                    createInfo.QueueFamilyIndices = (IntPtr)queueFamiliesPtr;
                }
                else
                {
                    createInfo.ImageSharingMode = SharingMode.Exclusive;
                    createInfo.QueueFamilyIndexCount = 0;
                    createInfo.QueueFamilyIndices = IntPtr.Zero;
                }
                Swapchain = NativeDevice.CreateSwapchain(ref createInfo);
            }
        }

        public void Dispose()
        {
            NativeDevice.DestroySwapchain(Swapchain);
            NativeDevice.Destroy();
            GC.SuppressFinalize(this);
        }

        ~LogicalDevice()
        {
            Dispose();
        }
    }
}