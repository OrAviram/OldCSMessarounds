using System;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanDebugger : IDisposable
    {
        private DebugReportCallback callback;
        Instance vkInstance;

        public VulkanDebugger(Instance vkInstance)
        {
            if (VulkanUtils.ENABLE_VALIDATION_LAYERS)
            {
                this.vkInstance = vkInstance;

                DebugReportCallbackCreateInfo createInfo = new DebugReportCallbackCreateInfo
                {
                    StructureType = StructureType.DebugReportCallbackCreateInfo,
                    Callback = Marshal.GetFunctionPointerForDelegate(new VulkanDebugReportCallbackDel(DebugCallback)),
                    Flags = (uint)(/*DebugReportFlags.Debug |*/ DebugReportFlags.Error | DebugReportFlags.Information | DebugReportFlags.PerformanceWarning | DebugReportFlags.Warning),
                };
                callback = vkInstance.CreateDebugReportCallback(ref createInfo);
            }
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
            if (VulkanUtils.ENABLE_VALIDATION_LAYERS)
                vkInstance.DestroyDebugReportCallback(callback);

            GC.SuppressFinalize(this);
        }

        ~VulkanDebugger()
        {
            Dispose();
        }

        delegate void VulkanDebugReportCallbackDel(DebugReportFlags flags, DebugReportObjectType objectType, ulong obj, PointerSize location, int messageCode, string layerPrefix, string message, IntPtr userData);
    }
}