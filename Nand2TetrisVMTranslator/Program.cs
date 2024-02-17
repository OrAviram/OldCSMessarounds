using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Nand2TetrisVMTranslator
{
    class Program
    {
        class Logger : ILogger
        {
            public void LogInputError(string message)
            {
                LearningCSharp.Logger.Log(message, ConsoleColor.Yellow);
            }

            public void LogTranslationError(string message, string fileName, int lineNumber, CommandType commandType)
            {
                if (commandType == CommandType.Invalid)
                    LearningCSharp.Logger.Log("Error on line " + lineNumber + " in file '" + fileName + "': " + message, ConsoleColor.Red);
                else
                    LearningCSharp.Logger.Log("Error on line " + lineNumber + " in file '" + fileName + "' on command of type " + commandType + ": " + message, ConsoleColor.Red);
            }
        }

        abstract class YieldInstruction
        {
            public abstract bool ShouldContinue { get; }
        }

        class WaitForSeconds : YieldInstruction
        {
            public float seconds;
            DateTime timeStarted;

            public WaitForSeconds(float s)
            {
                seconds = s;
                timeStarted = DateTime.Now;
            }

            public override bool ShouldContinue => (float)(DateTime.Now - timeStarted).TotalSeconds > seconds;
        }

        class Obj
        {
            List<IEnumerator> coroutines;

            public virtual void Start()
            {
                coroutines = new List<IEnumerator>();
            }

            public virtual void Update()
            {
                for (int i = 0; i < coroutines.Count; i++)
                {
                    YieldInstruction instruction = coroutines[i].Current as YieldInstruction;
                    if (instruction != null)
                    {
                        if (instruction.ShouldContinue)
                        {
                            if (!coroutines[i].MoveNext())
                                coroutines.RemoveAt(i);
                        }
                    }
                }
            }

            protected void StartCoroutine(IEnumerator coroutine)
            {
                if (coroutine.MoveNext())
                    coroutines.Add(coroutine);
            }
        }

        class TestObj : Obj
        {
            public override void Start()
            {
                base.Start();
                StartCoroutine(Fun());
                StartCoroutine(Fun2("Funniernoseseseseseses", 1));
            }

            public override void Update()
            {
                base.Update();
                Console.WriteLine("Mah man!");
            }

            IEnumerator Fun2(string txt, float t)
            {
                while (true)
                {
                    Console.WriteLine(txt);
                    yield return new WaitForSeconds(t);
                }
            }

            IEnumerator Fun()
            {
                Console.WriteLine("Hello!");
                yield return new WaitForSeconds(2);
                Console.WriteLine("I waited...");
                yield return new WaitForSeconds(3);
                for (int i = 0; i < 3; i++)
                    Console.Write("Waitingers! ");
                Console.WriteLine();
                yield return new WaitForSeconds(1);
                Console.WriteLine("Alright done.");
            }
        }

        static void Main(string[] args)
        {
            //Obj fun = new TestObj();
            //fun.Start();
            //while (true)
            //{
            //    fun.Update();
            //    Thread.Sleep(10);
            //}

            Logger logger = new Logger();
            Translator translator = new Translator(logger);
            while (true)
            {
                Console.WriteLine("Insert a .vm file to translate, or a directory containing a virtual machine program.");
                string inPath = Console.ReadLine();
                Console.WriteLine("Insert the name of the output assembly file.");
                string outName = Console.ReadLine();

                Console.WriteLine("Press w to write bootstrap code, and any other button to translate the program without it.");
                ConsoleKeyInfo b = Console.ReadKey(true);
                bool readSuccessfully = translator.ReadProgram(inPath, outName);
                if (!readSuccessfully)
                {
                    Console.WriteLine("Failed to read file. Try a new input file path.");
                    Console.WriteLine();
                    translator.CloseProgram();
                    continue;
                }

                bool translatedSuccessfully = translator.TranslateProgram(b.Key == ConsoleKey.W);
                if (!translatedSuccessfully)
                {
                    Console.WriteLine("Failed to translate program. Fix the errors and try translating again.");
                    Console.WriteLine();
                    translator.CloseProgram();
                    continue;
                }
                Console.WriteLine("Translation completed successfully.");
                translator.CloseProgram();

                Console.WriteLine("Press Esc to exit the translator, and any other button to translate another program.");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                    break;

                Console.WriteLine();
            }
        }
    }
}