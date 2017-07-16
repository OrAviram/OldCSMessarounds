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

        private DebugReportCallback debugReportCallback;

        public VulkanRenderer(string applicationName, Version applicationVersion, string engineName, Version engineVersion)
        {
            string[] desiredValidationLayers = null;
            List<string> desiredExtensions = new List<string> { "VK_KHR_surface", "VK_KHR_win32_surface" };

            if (VulkanUtils.ENABLE_VALIDATION_LAYERS)
            {
                desiredValidationLayers = new string[] { "VK_LAYER_LUNARG_standard_validation" };
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
                Init(ref appInfo, desiredExtensions.ToArray(), desiredValidationLayers);
            }
            finally
            {
                Marshal.FreeHGlobal(appInfo.ApplicationName);
                Marshal.FreeHGlobal(appInfo.EngineName);
            }
        }

        void Init(ref ApplicationInfo appInfo, string[] desiredExtensions, string[] desiredValidationLayers)
        {
            fixed(ApplicationInfo* appInfoPtr = &appInfo)
                Instance = new VulkanInstance(appInfoPtr, desiredExtensions, desiredValidationLayers);

            CreateDebugReportCallback();
        }

        void CreateDebugReportCallback()
        {
            DebugReportCallbackCreateInfo createInfo = new DebugReportCallbackCreateInfo
            {
                StructureType = StructureType.DebugReportCallbackCreateInfo,
                Callback = Marshal.GetFunctionPointerForDelegate(new VulkanDebugReportCallbackDel(DebugCallback)),
                Flags = (uint)(/*DebugReportFlags.Debug |*/ DebugReportFlags.Error | DebugReportFlags.Information | DebugReportFlags.PerformanceWarning | DebugReportFlags.Warning),
            };
            debugReportCallback = Instance.NativeInstance.CreateDebugReportCallback(ref createInfo);
        }

        void DebugCallback(DebugReportFlags flags, DebugReportObjectType objType, ulong obj, PointerSize location, int code, string layerPrefix, string msg, IntPtr userData)
        {
            switch (flags)
            {
                case DebugReportFlags.Error:
                    Logger.Log("VULKAN ERROR: " + msg, ConsoleColor.Red);
                    break;

                case DebugReportFlags.Warning:
                    Logger.Log("VULKAN WARNING: " + msg, ConsoleColor.Yellow);
                    break;

                case DebugReportFlags.PerformanceWarning:
                    Logger.Log("VULKAN PERFORMANCE WARNING: " + msg, ConsoleColor.Green);
                    break;

                case DebugReportFlags.Information:
                    Logger.Log("VULKAN INFORMATION: " + msg, ConsoleColor.Cyan);
                    break;

                case DebugReportFlags.Debug:
                    Logger.Log("VULKAN DEBUG: " + msg, ConsoleColor.Gray);
                    break;
            }
        }

        public void Dispose()
        {
            Instance.NativeInstance.DestroyDebugReportCallback(debugReportCallback);
            Instance.Dispose();
            GC.SuppressFinalize(this);
        }

        ~VulkanRenderer()
        {
            Dispose();
        }
    }
}