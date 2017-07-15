using System;

namespace LearningCSharp
{
    public static class Logger
    {
        public static void Log(object message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }
    }
}