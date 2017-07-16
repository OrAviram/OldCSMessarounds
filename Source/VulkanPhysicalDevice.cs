using SharpVulkan;

namespace LearningCSharp
{
    public struct QueueFamilyIndices
    {
        public static QueueFamilyIndices Default { get; } = new QueueFamilyIndices(-1);

        public int graphicsFamily;

        public QueueFamilyIndices(int graphicsFamily)
        {
            this.graphicsFamily = graphicsFamily;
        }

        public bool IsComplete => graphicsFamily >= 0;
    }

    public class VulkanPhysicalDevice
    {
        public PhysicalDevice NativeDevice { get; private set; }
        public QueueFamilyIndices QueueFamilyIndices { get; private set; }
        
        public VulkanPhysicalDevice(PhysicalDevice nativeDevice, QueueFamilyIndices queueFamilyIndices)
        {
            NativeDevice = nativeDevice;
            QueueFamilyIndices = queueFamilyIndices;
        }

        public bool IsValid => NativeDevice != PhysicalDevice.Null && QueueFamilyIndices.IsComplete;
    }
}