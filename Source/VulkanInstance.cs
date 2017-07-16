using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanInstance
    {
        public Instance NativeInstance { get; private set; }

        public VulkanInstance(ApplicationInfo* applicationInfo, string[] desiredExtensions, string[] desiredValidationLayers)
        {
            bool enableExtensions = desiredExtensions != null && desiredExtensions.Length > 0;
            bool enableValidation = desiredValidationLayers != null && desiredValidationLayers.Length > 0 && VulkanUtils.ENABLE_VALIDATION_LAYERS;

            string[] filteredExtensions = null;
            IntPtr[] extensionsPtrPtr = null;
            if (enableExtensions)
            {
                filteredExtensions = FilterExtensions(desiredExtensions);
                extensionsPtrPtr = filteredExtensions.Select(Marshal.StringToHGlobalAnsi).ToArray();
            }

            string[] filteredLayers = null;
            IntPtr[] layersPtrPtr = null;
            if (enableValidation)
            {
                filteredLayers = FilterValidationLayers(desiredValidationLayers);
                layersPtrPtr = filteredLayers.Select(Marshal.StringToHGlobalAnsi).ToArray();
            }

            try
            {
                IntPtr validationLayerNames = IntPtr.Zero;
                uint validationLayersCount = 0;

                IntPtr extensionNames = IntPtr.Zero;
                uint extensionsCount = 0;

                if (enableValidation)
                {
                    fixed (void* layersPtr = &layersPtrPtr[0])
                    {
                        validationLayerNames = (IntPtr)layersPtr;
                        validationLayersCount = (uint)filteredLayers.Length;
                    }
                }

                if (enableExtensions)
                {
                    fixed (void* extensionsPtr = &extensionsPtrPtr[0])
                    {
                        extensionNames = (IntPtr)extensionsPtr;
                        extensionsCount = (uint)filteredExtensions.Length;
                    }
                }

                CreateInstance(applicationInfo, extensionsCount, extensionNames, validationLayersCount, validationLayerNames);
            }
            finally
            {
                if (enableExtensions)
                {
                    foreach (IntPtr name in extensionsPtrPtr)
                        Marshal.FreeHGlobal(name);
                }

                if (enableValidation)
                {
                    foreach (IntPtr name in layersPtrPtr)
                        Marshal.FreeHGlobal(name);
                }
            }
        }

        void CreateInstance(ApplicationInfo* applicationInfo, uint extensionsCount, IntPtr extensionNames, uint layerCount, IntPtr layerNames)
        {
            InstanceCreateInfo createInfo = new InstanceCreateInfo
            {
                StructureType = StructureType.InstanceCreateInfo,
                ApplicationInfo = (IntPtr)applicationInfo,
                EnabledExtensionCount = extensionsCount,
                EnabledExtensionNames = extensionNames,
                EnabledLayerCount = layerCount,
                EnabledLayerNames = layerNames,
            };
            NativeInstance = Vulkan.CreateInstance(ref createInfo);
        }

        string[] FilterValidationLayers(string[] desiredLayers)
        {
            LayerProperties[] availableLayerProperties = Vulkan.InstanceLayerProperties;
            List<string> availableLayers = new List<string>();
            for (int i = 0; i < availableLayerProperties.Length; i++)
            {
                fixed (void* namePtr = &availableLayerProperties[i].LayerName)
                    availableLayers.Add(Marshal.PtrToStringAnsi(new IntPtr(namePtr)));
            }

            string[] enabledExtensions = desiredLayers.Where(availableLayers.Contains).ToArray();
            foreach (string desiredLayer in desiredLayers)
            {
                if (!enabledExtensions.Contains(desiredLayer))
                    throw new Exception("Desired validation layer '" + desiredLayer + "' is not supported!");
            }
            return enabledExtensions;
        }

        string[] FilterExtensions(string[] desiredExtensions)
        {
            ExtensionProperties[] availableExtensionProperties = Vulkan.GetInstanceExtensionProperties();
            List<string> availableExtentions = new List<string>();
            for (int i = 0; i < availableExtensionProperties.Length; i++)
            {
                fixed (void* namePtr = &availableExtensionProperties[i].ExtensionName)
                    availableExtentions.Add(Marshal.PtrToStringAnsi(new IntPtr(namePtr)));
            }

            string[] enabledExtensions = desiredExtensions.Where(availableExtentions.Contains).ToArray();
            foreach (string desiredExtention in desiredExtensions)
            {
                if (!enabledExtensions.Contains(desiredExtention))
                    throw new Exception("Desired extention '" + desiredExtention + "' is not supported!");
            }
            return enabledExtensions;
        }
    }
}