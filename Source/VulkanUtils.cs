using System.Collections.Generic;
using SharpVulkan;

namespace LearningCSharp
{    
    public static class VulkanUtils
    {
        public static QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            QueueFamilyIndices indices = QueueFamilyIndices.Default;
            QueueFamilyProperties[] queueFamilies = device.QueueFamilyProperties;

            for (int i = 0; i < queueFamilies.Length; i++)
            {
                QueueFamilyProperties queueFamily = queueFamilies[i];
                if (queueFamily.QueueCount > 0 && (queueFamily.QueueFlags & QueueFlags.Graphics) == 0)
                    indices.graphicsFamily = i;

                if (indices.IsComplete)
                    break;
            }
            return indices;
        }

        public static VulkanPhysicalDevice PickBestGPU(PhysicalDevice[] devices)
        {
            KeyValuePair<int, PhysicalDevice> bestGPU = new KeyValuePair<int, PhysicalDevice>(INVALID_GPU_SCORE, PhysicalDevice.Null);
            QueueFamilyIndices queueFamilyIndices = QueueFamilyIndices.Default;

            foreach (PhysicalDevice device in devices)
            {
                int rating = CalculateGPURating(device, out queueFamilyIndices);
                if (rating == INVALID_GPU_SCORE)
                    continue;

                if (rating > bestGPU.Key)
                    bestGPU = new KeyValuePair<int, PhysicalDevice>(rating, device);
            }
            return new VulkanPhysicalDevice(bestGPU.Value, queueFamilyIndices);
        }

        public const int INVALID_GPU_SCORE = -1;
        public static int CalculateGPURating(PhysicalDevice device, out QueueFamilyIndices queueFamilyIndices)
        {
            queueFamilyIndices = FindQueueFamilies(device);
            if (!queueFamilyIndices.IsComplete)
                return INVALID_GPU_SCORE;

            device.GetProperties(out PhysicalDeviceProperties properties);

            int score = INVALID_GPU_SCORE;
            switch (properties.DeviceType)
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