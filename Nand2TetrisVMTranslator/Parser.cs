using System;
using System.IO;
using System.Collections.Generic;

namespace Nand2TetrisVMTranslator
{
    enum CommandType { Invalid, Arithmetic, Push, Pop, Label, Goto, If, Function, Return, Call }

    class Parser
    {
        struct Command { public int lineNumber; public string trimmedLine; }
        List<Command> commands;
        int currentCommand = -1;
        
        public void SetFile(string path)
        {
            currentCommand = -1;
            commands = new List<Command>();
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int length = (int)file.Length;
                byte[] data = new byte[length];
                file.Read(data, 0, length);

                List<char> relevantCharacters = new List<char>();

                int characterIndex = 0;
                int lineStart = 0;
                int lineNumber = 1;
                while (characterIndex < data.Length)
                {
                    char character = Convert.ToChar(data[characterIndex]);
                    relevantCharacters.Add(character);
                    if (character == '\n')
                    {
                        int lineLength = relevantCharacters.Count - 1 - lineStart;
                        AddCommand(relevantCharacters, lineStart, lineLength, lineNumber);
                        lineStart = relevantCharacters.Count;
                        lineNumber++;
                    }

                    if (character == ' ')
                    {
                        int i = characterIndex;
                        while (i < data.Length && Convert.ToChar(data[i]) == ' ')
                            i++;

                        characterIndex = i;
                    }
                    else
                        characterIndex++;
                }
                AddCommand(relevantCharacters, lineStart, relevantCharacters.Count - lineStart, lineNumber);
            }
        }

        void AddCommand(List<char> relevantCharacters, int start, int length, int lineNumber)
        {
            string line = new string(relevantCharacters.ToArray(), start, length);
            int trimmedLineLength = line.Length;
            if (line.Contains("//"))
                trimmedLineLength = line.IndexOf("//");
            else if (line.Contains("\r"))
                trimmedLineLength = line.IndexOf("\r");

            if (trimmedLineLength > 0)
            {
                string trimmedLine = line.Substring(0, trimmedLineLength);
                if (!trimmedLine.EndsWith(" "))
                    trimmedLine = trimmedLine + " ";

                Command command = new Command { lineNumber = lineNumber, trimmedLine = trimmedLine };
                commands.Add(command);
            }
        }

        private CommandType commandType = CommandType.Invalid;
        public CommandType CommandType => commandType;

        private string commandName;
        public string CommandName => commandName;

        private string argument1;
        public string Argument1 => argument1;

        private ushort argument2;
        public ushort Argument2 => argument2;

        public int LineNumber => commands[currentCommand].lineNumber;

        /// <summary>
        /// Are there more commands in the file?
        /// </summary>
        public bool HasMoreCommands => currentCommand < commands.Count - 1;

        /// <summary>
        /// Moves to the next command in the file. After construction of the parser, no current command is set, and so this must be called.
        /// </summary>
        public void Advance(ref bool success, ILogger logger, string fileName)
        {
            commandType = CommandType.Invalid;
            commandName = string.Empty;
            argument1 = string.Empty;
            argument2 = 0;

            currentCommand++;
            string line = commands[currentCommand].trimmedLine;
            string[] split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 3)
            {
                logger.LogTranslationError("A command cannot contain more than three parts.", fileName, LineNumber, CommandType.Invalid);
                success = false;
            }

            commandName = split[0];
            commandType = ParseCommand(commandName);
            int requiredArgumentCount = GetRequiredArgumentCount(commandType);
            int suppliedArgumentCount = split.Length - 1;
            if (requiredArgumentCount != -1 && requiredArgumentCount != suppliedArgumentCount)
            {
                logger.LogTranslationError("Command '" + commandName + "' requires " + requiredArgumentCount + " arguments. " + suppliedArgumentCount + " arguments were supplied.", fileName, LineNumber, commandType);
                success = false;
            }

            if (split.Length > 1)
            {
                argument1 = split[1];
                if (split.Length > 2)
                {
                    if (!ushort.TryParse(split[2], out argument2))
                    {
                        logger.LogTranslationError("Second argument must be a number.", fileName, LineNumber, commandType);
                        success = false;
                    }
                }
            }
        }

        CommandType ParseCommand(string command)
        {
            if (command == "add" || command == "sub" || command == "neg" || command == "eq" || command == "gt" || command == "lt" || command == "and" || command == "or" || command == "not")
                return CommandType.Arithmetic;
            else if (command == "push")
                return CommandType.Push;
            else if (command == "pop")
                return CommandType.Pop;
            else if (command == "label")
                return CommandType.Label;
            else if (command == "goto")
                return CommandType.Goto;
            else if (command == "if-goto")
                return CommandType.If;
            else if (command == "function")
                return CommandType.Function;
            else if (command == "call")
                return CommandType.Call;
            else if (command == "return")
                return CommandType.Return;

            return CommandType.Invalid;
        }

        int GetRequiredArgumentCount(CommandType type)
        {
            switch (type)
            {
                case CommandType.Return:
                case CommandType.Arithmetic:
                    return 0;

                case CommandType.Label:
                case CommandType.Goto:
                case CommandType.If:
                    return 1;

                case CommandType.Push:
                case CommandType.Pop:
                case CommandType.Call:
                case CommandType.Function:
                    return 2;
            }
            return -1;
        }
    }
}