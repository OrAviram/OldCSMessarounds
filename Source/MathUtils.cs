using System;

namespace LearningCSharp
{
    public static class MathUtils
    {
        public static double IntPow(double b, int exponent)
        {
            double result = 1;
            for (int i = 0; i < exponent; i++)
            {
                result *= b;
            }
            return result;
        }

        public static int GetDigit(int number, int digit, int b)
        {
            return (int)(Math.Floor(number / IntPow(b, digit)) - b * Math.Floor(number / IntPow(b, digit + 1)));
        }

        public static uint Clamp(this uint value, uint min, uint max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}