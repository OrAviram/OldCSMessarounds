using System;
using System.IO;

namespace Nand2TetrisAssembler
{
    class Logger : IAssemblerLogger
    {
        public void LogError(string message, int lineNumber, CommandType commandType)
        {
            string openMessage = "Error on line " + lineNumber + " of command type " + commandType + ": ";
            LearningCSharp.Logger.Log(openMessage + message, ConsoleColor.Red);
        }
    }

    class Program
    {
        // Note that the other assembler, in AnotherProject, works, but it doesn't have error handling.
        // It was made simply as an attempt I made at making an assembler, without using the book's instructions.
        // This project is based on what the book supplies.
        static void Main(string[] args)
        {
            Logger logger = new Logger();
            Console.WriteLine("Enter target assembly code file path.");
            string inPath = Console.ReadLine();

            Console.WriteLine("Enter a file path to save the assembly code to.");

            bool startAssembly = false;
            string outPath = string.Empty;
            while (!startAssembly)
            {
                outPath = Console.ReadLine();
                if (File.Exists(outPath))
                {
                    Console.WriteLine("A file in that path already exists. Should it be replaced? (r to replace, anything else to keep)");
                    string answer = Console.ReadLine();
                    if (answer == "r")
                        startAssembly = true;
                    else
                        Console.WriteLine("Enter another file path.");
                }
                else
                    startAssembly = true;
            }

            Assembler p = new Assembler(inPath, logger);
            p.Assemble(outPath, out bool succeeded);
            if (succeeded)
                Console.WriteLine("Assembly ended successfully.");
            else
                Console.WriteLine("Assembly failed.");

            Console.ReadKey();
        }
    }
}