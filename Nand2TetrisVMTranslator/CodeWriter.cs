using System;
using System.IO;

namespace Nand2TetrisVMTranslator
{
    enum MemorySegment { Invalid = 0, Pointer, Temp, Static, Constant, Argument, Local, This, That }

    class CodeWriter
    {
        FileStream file;
        int equalsNumber = 0;
        int lessThanNumber = 0;
        int greaterThanNumber = 0;
        string currentFileName;
        string currentlyWrittenFunction;
        int functionReturnsCount = 0;

        public CodeWriter(string outputPath)
        {
            file = new FileStream(outputPath, FileMode.OpenOrCreate);
            file.SetLength(0);
            currentlyWrittenFunction = "bootstrap";
        }

        public void WriteInit()
        {
            // It expects me to do the commented out version, but since the pushing is of 0 anyway, I can just increase the stack beforehand, and
            // then instead of doing a full call write I can just go to the function name.
            // Technically it works.
            // Actually it didn't so now I am using the alright code.

            WriteLine("@256");
            //WriteLine("@261");
            WriteLine("D = A");
            WriteLine("@SP");
            WriteLine("M = D");

            WriteCall("Sys.init", 0);
            //WriteLine("@" + GetFunctionName("Sys.init"));
            //WriteLine("0;JMP");
        }

        public void SetFileName(string name) => currentFileName = name;

        public void WriteArithmetic(string command)
        {
            // Storing the second to last number pushed in the D register.
            WriteLine("@SP");
            WriteLine("A = M - 1");
            WriteLine("D = M");

            // If the command is on a single input, simply modify the value on the stack.
            // Otherwise, write to the earlier of the two values and reduce the stack pointer.
            if (command == "neg")
                WriteLine("M = -D");
            else if (command == "not")
                WriteLine("M = !D");
            else
            {
                WriteLine("@SP");
                WriteLine("M = M - 1");
                WriteLine("A = M - 1");

                if (command == "add")
                    WriteLine("M = M + D");
                else if (command == "sub")
                    WriteLine("M = M - D");
                else if (command == "eq")
                    WriteBooleanArithmetic("JEQ", "EQUALITY_END_OF_NUMBER", ref equalsNumber);
                else if (command == "gt")
                    WriteBooleanArithmetic("JGT", "GREATER_THAN_END_OF_NUMBER", ref greaterThanNumber);
                else if (command == "lt")
                    WriteBooleanArithmetic("JLT", "LESS_THAN_END_OF_NUMBER", ref lessThanNumber);
                else if (command == "and")
                    WriteLine("M = M & D");
                else if (command == "or")
                    WriteLine("M = M | D");
            }
        }

        void WriteBooleanArithmetic(string jumpCondition, string labelOpening, ref int labelNumber)
        {
            // Default to true (-1). If the jump condition is not met, set the value to false (0).
            WriteLine("D = M - D");
            WriteLine("M = -1");

            string endLabel = labelOpening + "_" + labelNumber;
            WriteLine("@" + endLabel);
            WriteLine("D;" + jumpCondition);
            WriteLine("@SP");
            WriteLine("A = M - 1");
            WriteLine("M = 0");
            WriteLine("(" + endLabel + ")");
            labelNumber++;
        }

        public void WriteStackCommand(bool push, string segmentName, ushort index, ILogger logger, int lineNumber, ref bool success)
        {
            if (segmentName == string.Empty)
                return;

            MemorySegment segment = GetMemorySegment(segmentName);
            if (segment == MemorySegment.Invalid)
            {
                CommandType commandType = push ? CommandType.Push : CommandType.Pop;
                logger.LogTranslationError("Invalid segment " + segmentName + ".", currentFileName, lineNumber, commandType);
                success = false;
            }

            if (!push && segment == MemorySegment.Constant)
            {
                logger.LogTranslationError("Cannot pop into constant memory segment.", currentFileName, lineNumber, CommandType.Pop);
                success = false;
            }

            if (segment == MemorySegment.Pointer)
                WriteLine(index == 0 ? "@THIS" : "@THAT");
            else if (segment == MemorySegment.Temp)
                WriteLine("@R" + (index + 5));
            else if (segment == MemorySegment.Static)
                WriteLine("@" + currentFileName + "." + index);
            else
            {
                WriteLine("@" + index);
                WriteLine("D = A");

                if (segment != MemorySegment.Constant)
                {
                    if (segment == MemorySegment.Argument)
                        WriteLine("@ARG");
                    else if (segment == MemorySegment.Local)
                        WriteLine("@LCL");
                    else if (segment == MemorySegment.This)
                        WriteLine("@THIS");
                    else if (segment == MemorySegment.That)
                        WriteLine("@THAT");

                    WriteLine("A = M + D");
                }
            }

            if (push)
            {
                if (segment != MemorySegment.Constant)
                    WriteLine("D = M");

                WritePushD();
            }
            else
            {
                WriteLine("D = A");
                WriteLine("@R13");
                WriteLine("M = D");

                WriteLine("@SP");
                WriteLine("AM = M - 1");
                WriteLine("D = M");

                WriteLine("@R13");
                WriteLine("A = M");
                WriteLine("M = D");
            }
        }

        MemorySegment GetMemorySegment(string name)
        {
            if (name == "pointer")
                return MemorySegment.Pointer;
            else if (name == "temp")
                return MemorySegment.Temp;
            else if (name == "static")
                return MemorySegment.Static;
            else if (name == "constant")
                return MemorySegment.Constant;
            else if (name == "argument")
                return MemorySegment.Argument;
            else if (name == "local")
                return MemorySegment.Local;
            else if (name == "this")
                return MemorySegment.This;
            else if (name == "that")
                return MemorySegment.That;

            return MemorySegment.Invalid;
        }

        public void WriteFunction(string name, int localsCount)
        {
            WriteLine("(" + GetFunctionName(name) + ")");
            for (int i = 0; i < localsCount; i++)
            {
                WriteLine("@SP");
                WriteLine("A = M");
                WriteLine("M = 0");

                WriteLine("@SP");
                WriteLine("M = M + 1");
            }
            currentlyWrittenFunction = name;
        }

        public void WriteReturn()
        {
            // FRAME (R13) = LCL
            WriteLine("@LCL");
            WriteLine("D = M");
            WriteLine("@R13");
            WriteLine("M = D");

            // Since *(LCL-5), which stores the return point, may be overwritten with the return value, we store it in R14.
            WriteLine("@5");
            WriteLine("A = D - A");
            WriteLine("D = M");
            WriteLine("@R14");
            WriteLine("M = D");

            // *ARG = pop
            WriteLine("@SP");
            WriteLine("A = M - 1");
            WriteLine("D = M");
            WriteLine("@ARG");
            WriteLine("A = M");
            WriteLine("M = D");

            // SP = ARG + 1
            WriteLine("@ARG");
            WriteLine("D = M");
            WriteLine("@SP");
            WriteLine("M = D + 1");

            // THAT = *(FRAME - 1)
            ReduceFrameAndWriteValueToPointer("THAT");

            // THIS = *(FRAME - 2)
            ReduceFrameAndWriteValueToPointer("THIS");

            // ARG = *(FRAME - 3)
            ReduceFrameAndWriteValueToPointer("ARG");

            // LCL = *(FRAME - 4)
            ReduceFrameAndWriteValueToPointer("LCL");

            // A = return address = *(FRAME - 5), and jump
            WriteLine("@R14");
            WriteLine("A = M");
            WriteLine("0;JMP");
        }

        void ReduceFrameAndWriteValueToPointer(string pointerName)
        {
            WriteLine("@R13");
            WriteLine("AM = M - 1");
            WriteLine("D = M");
            WriteLine("@" + pointerName);
            WriteLine("M = D");
        }

        public void WriteCall(string name, int argumentCount)
        {
            string returnLabel = "FUNCTION_RETURN_" + functionReturnsCount;
            functionReturnsCount++;

            WriteLine("@" + returnLabel);
            WriteLine("D = A");
            WritePushD();

            PushPointer("LCL");
            PushPointer("ARG");
            PushPointer("THIS");
            PushPointer("THAT");

            // LCL = SP and ARG = SP - argumentCount - 5
            WriteLine("@SP");
            WriteLine("D = M");
            WriteLine("@LCL");
            WriteLine("M = D");
            WriteLine("@" + argumentCount);
            WriteLine("D = D - A");
            WriteLine("@5");
            WriteLine("D = D - A");
            WriteLine("@ARG");
            WriteLine("M = D");

            WriteLine("@" + GetFunctionName(name));
            WriteLine("0;JMP");

            WriteLine("(" + returnLabel + ")");
        }

        void PushPointer(string name)
        {
            WriteLine("@" + name);
            WriteLine("D = M");
            WritePushD();
        }

        string GetFunctionName(string userName) => "FUNCTION_" + userName;

        public void WriteLabel(string name)
        {
            WriteLine("(" + GetLabelName(name) + ")");
        }

        public void WriteGoto(string destination)
        {
            WriteLine("@" + GetLabelName(destination));
            WriteLine("0;JMP");
        }

        public void WriteIf(string destination)
        {
            WriteLine("@SP");
            WriteLine("AM = M - 1");
            WriteLine("D = M");

            WriteLine("@" + GetLabelName(destination));
            WriteLine("D;JNE");
        }

        string GetLabelName(string userName) => "USER_LABEL_" + currentlyWrittenFunction + "::" + userName;

        void WritePushD()
        {
            WriteLine("@SP");
            WriteLine("M = M + 1");
            WriteLine("A = M - 1");
            WriteLine("M = D");
        }

        void WriteLine(string line)
        {
            for (int i = 0; i < line.Length; i++)
                file.WriteByte(Convert.ToByte(line[i]));

            file.WriteByte(Convert.ToByte('\n'));
        }

        public void Close()
        {
            file.Dispose();
        }
    }
}