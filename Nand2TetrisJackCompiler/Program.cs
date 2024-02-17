using System;
using System.Xml;
using System.Text;
using System.IO;

namespace Nand2TetrisJackCompiler
{
    class Program
    {
        class Logger : ILogger
        {
            public string fileName;
            public bool success = true;

            void Log(string text, ConsoleColor color)
            {
                ConsoleColor previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = previousColor;
            }

            public void LogWarning(string message, int lineNumber)
            {
                Log(string.Format("Compilation warning ({0}, {1}): {2}", fileName, lineNumber, message), ConsoleColor.Yellow);
            }

            public void LogError(string message, int lineNumber)
            {
                Log(string.Format("Compilation error ({0}, {1}): {2}", fileName, lineNumber, message), ConsoleColor.Red);
                success = false;
            }

            public void LogUnexpectedTextError(string expected, string found, int lineNumber)
            {
                LogError(string.Format("Expected '{0}', found '{1}'.", expected, found), lineNumber);
            }
        }

        static void Main(string[] args)
        {
            bool askForFile = true;
            string[] files = null;
            while (askForFile)
            {
                Console.WriteLine("Enter file or directory path");
                string path = Console.ReadLine();

                if (path.EndsWith(".jack"))
                    files = new string[] { path };
                else if (Directory.Exists(path))
                    files = Directory.GetFiles(path, "*.jack", SearchOption.AllDirectories);

                if (files == null || files.Length == 0)
                    Console.WriteLine("File must either end in .jack or be a directory containing jack files.");
                else
                    askForFile = false;
            }

            Logger logger = new Logger();
            Tokenizer tokenizer = new Tokenizer();
            CompilationEngine compilationEngine = new CompilationEngine(tokenizer, logger);
            bool success = true;
            foreach (string file in files)
            {
                int nameIndex = Math.Max(file.LastIndexOf('/'), file.LastIndexOf('\\'));
                string fileName = file.Substring(nameIndex + 1, file.Length - 1 - nameIndex);
                logger.fileName = fileName;

                tokenizer.SetFile(file);
                string outDirectory = file.Substring(0, file.IndexOf(".jack"));
                compilationEngine.CompileFile(outDirectory + ".xml", outDirectory + ".vm");
                tokenizer.Reset();
                TokenizeFile(tokenizer, outDirectory + "T.xml");

                success &= logger.success;
            }
            if (success)
                Console.WriteLine("Compilation ended successfully.");
            else
                Console.WriteLine("Compilation failed.");

            Console.ReadKey();
        }

        static void TokenizeFile(Tokenizer tokenizer, string outPath)
        {
            XmlTextWriter writer = new XmlTextWriter(outPath, Encoding.ASCII);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartElement("tokens");
            while (tokenizer.HasMoreTokens)
            {
                tokenizer.Advance();

                switch (tokenizer.CurrentType)
                {
                    case TokenType.Keyword:
                        writer.WriteElementString("keyword", tokenizer.Keyword);
                        break;
                    case TokenType.Symbol:
                        writer.WriteElementString("symbol", tokenizer.Symbol.ToString());
                        break;
                    case TokenType.IntegerConstant:
                        writer.WriteElementString("integerConstant", tokenizer.IntegerValue.ToString());
                        break;
                    case TokenType.StringConstant:
                        writer.WriteElementString("stringConstant", tokenizer.StringValue);
                        break;
                    case TokenType.Identifier:
                        writer.WriteElementString("identifier", tokenizer.Identifier);
                        break;
                    default:
                        break;
                }
            }
            writer.WriteEndElement();
            writer.Dispose();
        }
    }
}