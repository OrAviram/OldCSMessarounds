using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class LogicalDevice : IDisposable
    {
        public Device NativeDevice { get; private set; }
        public Queue GraphicsQueue { get; private set; }

        private PhysicalDevice physicalDevice;

        public LogicalDevice(VulkanPhysicalDevice vkPhysicalDevice, string[] extensions)
        {
            physicalDevice = vkPhysicalDevice.NativeDevice;
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
            
            uint extensionsCount = 0;
            IntPtr[] extensionNames = new IntPtr[] { IntPtr.Zero };
            if (extensions != null && extensions.Length > 0)
            {
                extensionNames = FilterExtensions(extensions).Select(Marshal.StringToHGlobalAnsi).ToArray();
                extensionsCount = (uint)extensionNames.Length;
            }

            fixed(void* extensionNamesPtr = &extensionNames[0])
            {
                DeviceCreateInfo createInfo = new DeviceCreateInfo
                {
                    StructureType = StructureType.DeviceCreateInfo,
                    EnabledFeatures = new IntPtr(&features),
                    QueueCreateInfoCount = 1,
                    QueueCreateInfos = new IntPtr(&queueCreateInfo),
                    EnabledExtensionCount = extensionsCount,
                    EnabledExtensionNames = (IntPtr)extensionNamesPtr,
                };
                NativeDevice = physicalDevice.CreateDevice(ref createInfo);
            }
            GraphicsQueue = NativeDevice.GetQueue(graphicsFamilyIndex, 0);
        }

        string[] FilterExtensions(string[] desiredExtensions)
        {
            ExtensionProperties[] availableExtensionProperties = physicalDevice.GetDeviceExtensionProperties();
            List<string> availableExtensions = new List<string>();
            for (int i = 0; i < availableExtensionProperties.Length; i++)
            {
                fixed (void* name = &availableExtensionProperties[i].ExtensionName)
                    availableExtensions.Add(Marshal.PtrToStringAnsi((IntPtr)name));
            }

            string[] extensions = desiredExtensions.Where(availableExtensions.Contains).ToArray();
            foreach (string extension in desiredExtensions)
            {
                if (!extensions.Contains(extension))
                    throw new Exception("Couldn't find desired device extension '" + extension + "'!");
            }
            return extensions;
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