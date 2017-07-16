using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpVulkan;
using Version = SharpVulkan.Version;

namespace LearningCSharp
{
    public unsafe class VulkanRenderer : IDisposable
    {
        public VulkanInstance Instance { get; private set; }
        private VulkanDebugger debugger;

        public VulkanRenderer(string applicationName, Version applicationVersion, string engineName, Version engineVersion)
        {
            string[] desiredValidationLayers = null;
            List<string> desiredExtensions = new List<string> { "VK_KHR_surface", "VK_KHR_win32_surface" };

            if (VulkanUtils.ENABLE_VALIDATION_LAYERS)
            {
                desiredValidationLayers = VulkanDebugger.ValidationLayers;
                desiredExtensions.Add("VK_EXT_debug_report");
            }

            ApplicationInfo appInfo = new ApplicationInfo
            {
                StructureType = StructureType.ApplicationInfo,
                ApiVersion = Vulkan.ApiVersion,
                ApplicationName = Marshal.StringToHGlobalAnsi(applicationName),
                ApplicationVersion = applicationVersion,
                EngineName = Marshal.StringToHGlobalAnsi(engineName),
                EngineVersion = engineVersion,
            };

            try
            {
                Initialize(ref appInfo, desiredExtensions.ToArray(), desiredValidationLayers);
            }
            finally
            {
                Marshal.FreeHGlobal(appInfo.ApplicationName);
                Marshal.FreeHGlobal(appInfo.EngineName);
            }
        }

        void Initialize(ref ApplicationInfo appInfo, string[] desiredExtensions, string[] desiredValidationLayers)
        {
            fixed(ApplicationInfo* appInfoPtr = &appInfo)
                Instance = new VulkanInstance(appInfoPtr, desiredExtensions, desiredValidationLayers);

            debugger = new VulkanDebugger(Instance.NativeInstance);
        }
        
        public void Dispose()
        {
            debugger.Dispose();
            Instance.Dispose();
            GC.SuppressFinalize(this);
        }

        ~VulkanRenderer()
        {
            Dispose();
        }
    }
}