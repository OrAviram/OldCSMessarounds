using System.Collections.Generic;
using SharpVulkan;

namespace LearningCSharp
{
    public static class VulkanUtils
    {
        public static PhysicalDevice PickBestGPU(PhysicalDevice[] devices)
        {
            KeyValuePair<int, PhysicalDevice> bestGPU = new KeyValuePair<int, PhysicalDevice>(INVALID_GPU_SCORE, PhysicalDevice.Null);
            foreach (PhysicalDevice device in devices)
            {
                device.GetProperties(out PhysicalDeviceProperties properties);
                int rating = CalculateGPURating(properties.DeviceType);
                if (rating == INVALID_GPU_SCORE)
                    continue;

                if (rating > bestGPU.Key)
                    bestGPU = new KeyValuePair<int, PhysicalDevice>(rating, device);
            }
            return bestGPU.Value;
        }

        public const int INVALID_GPU_SCORE = -1;
        public static int CalculateGPURating(PhysicalDeviceType deviceType)
        {
            int score = INVALID_GPU_SCORE;
            switch (deviceType)
            {
                case PhysicalDeviceType.Other:
                    return INVALID_GPU_SCORE;

                case PhysicalDeviceType.Cpu:
                    score = 1;
                    break;

                case PhysicalDeviceType.VirtualGpu:
                    score = 2;
                    break;

                case PhysicalDeviceType.IntegratedGpu:
                    score = 3;
                    break;

                case PhysicalDeviceType.DiscreteGpu:
                    score = 4;
                    break;
            }
            return score;
        }

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