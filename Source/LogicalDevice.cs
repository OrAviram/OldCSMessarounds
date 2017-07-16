using System;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class LogicalDevice : IDisposable
    {
        public Device NativeDevice { get; private set; }
        public Queue GraphicsQueue { get; private set; }

        public LogicalDevice(VulkanPhysicalDevice vkPhysicalDevice)
        {
            PhysicalDevice physicalDevice = vkPhysicalDevice.NativeDevice;
            physicalDevice.GetFeatures(out PhysicalDeviceFeatures features);

            QueueFamilyIndices queueFamilyIndices = vkPhysicalDevice.QueueFamilyIndices;
            uint graphicsFamilyIndex = (uint)queueFamilyIndices.graphicsFamily;

            float queuePriorities = 1;
            DeviceQueueCreateInfo queueCreateInfo = new DeviceQueueCreateInfo
            {
                StructureType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = graphicsFamilyIndex,
                QueuePriorities = new IntPtr(&queuePriorities),
            };

            DeviceCreateInfo createInfo = new DeviceCreateInfo
            {
                StructureType = StructureType.DeviceCreateInfo,
                EnabledFeatures = new IntPtr(&features),
                QueueCreateInfoCount = 1,
                QueueCreateInfos = new IntPtr(&queueCreateInfo),
            };
            NativeDevice = physicalDevice.CreateDevice(ref createInfo);
            GraphicsQueue = NativeDevice.GetQueue(graphicsFamilyIndex, 0);
        }

        public void Dispose()
        {
            NativeDevice.Destroy();
            GC.SuppressFinalize(this);
        }

        ~LogicalDevice()
        {
            Dispose();
        }
    }
}