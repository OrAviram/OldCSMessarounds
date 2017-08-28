using System;

namespace LearningCSharp
{
    public static class MathUtils
    {
        public static uint Clamp(this uint value, uint min, uint max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}