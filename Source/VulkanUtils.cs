using System;
using SharpVulkan;

namespace LearningCSharp
{
    public delegate void VulkanDebugReportCallbackDel(DebugReportFlags flags, DebugReportObjectType objectType, ulong obj, PointerSize location, int messageCode, string layerPrefix, string message, IntPtr userData);

    public static class VulkanUtils
    {
        #if DEBUG
        public const bool ENABLE_VALIDATION_LAYERS = true;
        #else
        public const bool ENABLE_VALIDATION_LAYERS = false;
        #endif
    }
}