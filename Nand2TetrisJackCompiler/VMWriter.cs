using System;
using System.IO;

namespace Nand2TetrisJackCompiler
{
    enum MemorySegment { Constant, Argument, Local, Static, This, That, Pointer, Temp }
    enum ArithmeticCommand { Add, Subtract, Negate, Equals, GreaterThan, LessThan, And, Or, Not }

    class VMWriter : IDisposable
    {
        FileStream fileStream;

        public VMWriter(string outPath)
        {
            fileStream = File.Open(outPath, FileMode.OpenOrCreate);
        }

        public void WritePush(MemorySegment segment, int index)
        {
            WriteLine("push " + TranslateSegment(segment) + " " + index);
        }

        public void WritePop(MemorySegment segment, int index)
        {
            WriteLine("pop " + TranslateSegment(segment) + " " + index);
        }

        string TranslateSegment(MemorySegment segment)
        {
            switch (segment)
            {
                case MemorySegment.Constant: return "constant";
                case MemorySegment.Argument: return "argument";
                case MemorySegment.Local: return "local";
                case MemorySegment.Static: return "static";
                case MemorySegment.This: return "this";
                case MemorySegment.That: return "that";
                case MemorySegment.Pointer: return "pointer";
                case MemorySegment.Temp: return "temp";
            }
            return string.Empty;
        }

        public void WriteArithmetic(ArithmeticCommand command)
        {
            switch (command)
            {
                case ArithmeticCommand.Add:
                    WriteLine("add");
                    break;
                case ArithmeticCommand.Subtract:
                    WriteLine("sub");
                    break;
                case ArithmeticCommand.Negate:
                    WriteLine("neg");
                    break;
                case ArithmeticCommand.Equals:
                    WriteLine("eq");
                    break;
                case ArithmeticCommand.GreaterThan:
                    WriteLine("gt");
                    break;
                case ArithmeticCommand.LessThan:
                    WriteLine("lt");
                    break;
                case ArithmeticCommand.And:
                    WriteLine("and");
                    break;
                case ArithmeticCommand.Or:
                    WriteLine("or");
                    break;
                case ArithmeticCommand.Not:
                    WriteLine("not");
                    break;
            }
        }

        public void WriteLabel(string label)
        {
            WriteLine("label " + label);
        }

        public void WriteGoto(string label)
        {
            WriteLine("goto " + label);
        }

        public void WriteIfGoto(string label)
        {
            WriteLine("if-goto " + label);
        }

        public void WriteCall(string functionName, int argumentCount)
        {
            WriteLine("call " + functionName + " " + argumentCount);
        }

        public void WriteFunction(string name, int localsCount)
        {
            WriteLine("function " + name + " " + localsCount);
        }

        public void WriteReturn()
        {
            WriteLine("return");
        }

        void WriteLine(string str)
        {
            string strWithLine = str + "\n";
            foreach (char c in strWithLine)
                fileStream.WriteByte(Convert.ToByte(c));
        }

        public void Dispose()
        {
            fileStream.Dispose();
        }
    }
}