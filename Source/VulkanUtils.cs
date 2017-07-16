using System;
using SharpVulkan;

namespace LearningCSharp
{
    public static class VulkanUtils
    {
#       if DEBUG
        public static string[] Extensions => new string[] { "VK_EXT_debug_report" };
        public static string[] ValidationLayers => new string[] { "VK_LAYER_LUNARG_standard_validation" };
        public const bool ENABLE_VALIDATION_LAYERS = true;
#       else
        public static string[] Extensions => new string[] { };
        public static string[] ValidationLayers => null;
        public const bool ENABLE_VALIDATION_LAYERS = false;
#       endif
    }
}