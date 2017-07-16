using System;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class VulkanDebugger : IDisposable
    {
        public static string[] ValidationLayers => new string[] { "VK_LAYER_LUNARG_standard_validation" };

        private DebugReportCallback callback;
        Instance vkInstance;

        public VulkanDebugger(Instance vkInstance)
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
            vkInstance.DestroyDebugReportCallback(callback);
            GC.SuppressFinalize(this);
        }

        ~VulkanDebugger()
        {
            Dispose();
        }
    }
}