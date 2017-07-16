namespace LearningCSharp
{
    public static class VulkanUtils
    {
        #if DEBUG
        public const bool ENABLE_VALIDATION_LAYERS = true;
        #else
        public const bool ENABLE_VALIDATION_LAYERS = false;
        #endif
    }
}