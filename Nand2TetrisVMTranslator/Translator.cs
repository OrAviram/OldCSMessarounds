using System;
using System.IO;
using System.Collections.Generic;

namespace Nand2TetrisVMTranslator
{
    class Translator
    {
        List<string> filePaths;
        CodeWriter writer;
        Parser parser;
        ILogger logger;

        public Translator(ILogger logger)
        {
            this.logger = logger;
        }

        public bool ReadProgram(string inputPath, string outputName)
        {
            string outFolder = inputPath.Substring(0, inputPath.LastIndexOf('\\') + 1);
            string outPath = outFolder + outputName;

            filePaths = new List<string>();
            if (inputPath.EndsWith(".vm"))
                filePaths.Add(inputPath);
            else if (Directory.Exists(inputPath))
            {
                string[] files = Directory.GetFiles(inputPath);
                foreach (string file in files)
                {
                    if (file.EndsWith(".vm"))
                        filePaths.Add(file);
                }
            }
            else
            {
                logger.LogInputError("The input path must either end in .vm or be a directory.");
                return false;
            }

            if (filePaths.Count == 0)
            {
                logger.LogInputError("No files found and it sucks.");
                return false;
            }

            writer = new CodeWriter(outPath);
            parser = new Parser();
            return true;
        }

        public void CloseProgram()
        {
            writer.Close();
        }

        public bool TranslateProgram(bool writeBootstrap)
        {
            if (writeBootstrap)
                writer.WriteInit();

            bool success = true;
            foreach (string file in filePaths)
            {
                parser.SetFile(file);
                string fileName = file.Substring(file.LastIndexOf('\\') + 1);
                fileName = fileName.Substring(0, fileName.IndexOf(".vm"));
                writer.SetFileName(fileName);
                TranslateFile(ref success, fileName);
            }
            return success;
        }

        void TranslateFile(ref bool success, string name)
        {
            while (parser.HasMoreCommands)
            {
                parser.Advance(ref success, logger, name);
                TranslateCommand(ref success, name);
            }
        }

        void TranslateCommand(ref bool success, string fileName)
        {
            switch (parser.CommandType)
            {
                case CommandType.Arithmetic:
                    writer.WriteArithmetic(parser.CommandName);
                    break;

                case CommandType.Push:
                    writer.WriteStackCommand(true, parser.Argument1, parser.Argument2, logger, parser.LineNumber, ref success);
                    break;

                case CommandType.Pop:
                    writer.WriteStackCommand(false, parser.Argument1, parser.Argument2, logger, parser.LineNumber, ref success);
                    break;

                case CommandType.Label:
                    writer.WriteLabel(parser.Argument1);
                    break;

                case CommandType.Goto:
                    writer.WriteGoto(parser.Argument1);
                    break;

                case CommandType.If:
                    writer.WriteIf(parser.Argument1);
                    break;

                case CommandType.Function:
                    writer.WriteFunction(parser.Argument1, parser.Argument2);
                    break;

                case CommandType.Return:
                    writer.WriteReturn();
                    break;

                case CommandType.Call:
                    writer.WriteCall(parser.Argument1, parser.Argument2);
                    break;

                default:
                    logger.LogTranslationError("Invalid command '" + parser.CommandName + "'.", fileName, parser.LineNumber, CommandType.Invalid);
                    success = false;
                    break;
            }
        }
    }
}