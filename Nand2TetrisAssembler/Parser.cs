using System;
using System.IO;
using System.Collections.Generic;

namespace Nand2TetrisAssembler
{
    enum CommandType { Invalid = 0, Address = 1, Compute = 2, Label = 3 }
    
    class Parser
    {
        struct Command { public int lineNumber; public string trimmedLine; }
        List<Command> commands;
        int currentCommandIndex = -1;
        int destinationEnd = -1;
        int jumpConditionStart = -1;

        public Parser(string path)
        {
            commands = new List<Command>();
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                int length = (int)file.Length;
                byte[] data = new byte[length];
                file.Read(data, 0, length);

                int previousLineStart = 0;
                int relevantCharacterIndex = 0;
                int lineNumber = 1;
                List<char> relevantCharacters = new List<char>();
                for (int i = 0; i < length; i++)
                {
                    char character = Convert.ToChar(data[i]);
                    if (character == ' ')
                        continue;

                    relevantCharacters.Add(character);
                    if (character == '\n' || i == data.Length - 1)
                    {
                        int lineLength = relevantCharacterIndex - previousLineStart;
                        if (character != '\n')
                            lineLength = relevantCharacterIndex - previousLineStart + 1;

                        string line = new string(relevantCharacters.ToArray(), previousLineStart, lineLength);
                        int trimmedLineLength = line.Length;
                        if (line.Contains("//"))
                            trimmedLineLength = line.IndexOf("//");
                        else if (line.Contains("\r"))
                            trimmedLineLength = line.IndexOf("\r");

                        if (trimmedLineLength > 0)
                        {
                            string trimmedLine = line.Substring(0, trimmedLineLength);
                            Command command = new Command { lineNumber = lineNumber, trimmedLine = trimmedLine };
                            commands.Add(command);
                        }
                        previousLineStart = relevantCharacterIndex + 1;
                        lineNumber++;
                    }
                    relevantCharacterIndex++;
                }
            }
        }

        /// <summary>
        /// Line number in the file of the current command.
        /// </summary>
        public int LineNumber => commands[currentCommandIndex].lineNumber;

        /// <summary>
        /// Are there more commands in the input?
        /// </summary>
        public bool HasMoreCommands => currentCommandIndex < commands.Count - 1;

        /// <summary>
        /// The dest mnemonic in the current C-command (dest=comp;jump).
        /// Should only be called when <seealso cref="CommandType"/> is <seealso cref="CommandType.Compute"/>.
        /// </summary>
        public string Dest
        {
            get
            {
                if (destinationEnd == -1)
                    return string.Empty;

                return commands[currentCommandIndex].trimmedLine.Substring(0, destinationEnd);
            }
        }

        /// <summary>
        /// The comp mnemonic in the current C-command (dest=comp;jump).
        /// Should only be called when <seealso cref="CommandType"/> is <seealso cref="CommandType.Compute"/>.
        /// </summary>
        public string Comp
        {
            get
            {
                string line = commands[currentCommandIndex].trimmedLine;

                int computationStart = 0;
                if (destinationEnd != -1)
                    computationStart = destinationEnd + 1;

                int computationEnd = line.Length - 1;
                if (jumpConditionStart != -1)
                    computationEnd = jumpConditionStart - 1;

                if (computationEnd <= computationStart - 1)
                    return null;

                return line.Substring(computationStart, computationEnd - computationStart + 1);
            }
        }

        /// <summary>
        /// The jump mnemonic in the current C-command (dest=comp;jump).
        /// Should only be called when <seealso cref="CommandType"/> is <seealso cref="CommandType.Compute"/>.
        /// </summary>
        public string Jump
        {
            get
            {
                if (jumpConditionStart == -1)
                    return string.Empty;

                string line = commands[currentCommandIndex].trimmedLine;
                return commands[currentCommandIndex].trimmedLine.Substring(jumpConditionStart + 1, line.Length - 1 - jumpConditionStart);
            }
        }

        /// <summary>
        /// The type of the current command. Invalid if <seealso cref="Advance"/> wasn't called.
        /// </summary>
        public CommandType CommandType
        {
            get
            {
                if (currentCommandIndex == -1)
                    return CommandType.Invalid;

                char firstCharacter = commands[currentCommandIndex].trimmedLine[0];
                if (firstCharacter == '@')
                    return CommandType.Address;

                if (firstCharacter == '(')
                    return CommandType.Label;

                if (Comp == null)
                    return CommandType.Invalid;

                return CommandType.Compute;
            }
        }

        /// <summary>
        /// The symbol or decimal of the current command if it's @X or (X).
        /// Should only be called if <seealso cref="CommandType"/> is <seealso cref="CommandType.Address"/> or <seealso cref="CommandType.Label"/>.
        /// </summary>
        public string Symbol
        {
            get
            {
                string line = commands[currentCommandIndex].trimmedLine;
                switch (CommandType)
                {
                    case CommandType.Label:
                        int end = line.IndexOf(')');
                        if (end == -1)
                            return null;

                        return line.Substring(1, end - 1);

                    case CommandType.Address:
                        return line.Substring(1, line.Length - 1);

                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Resets the current command pointed at, so that Advance sets the current command to the first command.
        /// </summary>
        public void Reset()
        {
            currentCommandIndex = -1;
        }

        /// <summary>
        /// Reads the next command from the input and makes it the current command.
        /// Should only be called if <seealso cref="HasMoreCommands"/> is true.
        /// </summary>
        public void Advance()
        {
            currentCommandIndex++;
            string line = commands[currentCommandIndex].trimmedLine;
            jumpConditionStart = line.IndexOf(';');
            destinationEnd = line.IndexOf('=');
        }
    }
}