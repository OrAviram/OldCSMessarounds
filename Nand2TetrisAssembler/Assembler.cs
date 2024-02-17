using System;
using System.IO;
using System.Collections.Generic;
using LearningCSharp;

namespace Nand2TetrisAssembler
{
    class Assembler
    {
        Parser parser;
        Dictionary<string, ushort> symbols;
        ushort variableCount = 0;
        IAssemblerLogger logger;

        public Assembler(string inputPath, IAssemblerLogger logger)
        {
            this.logger = logger;
            parser = new Parser(inputPath);
            symbols = new Dictionary<string, ushort>()
            {
                ["SP"] = 0,
                ["LCL"] = 1,
                ["ARG"] = 2,
                ["THIS"] = 3,
                ["THAT"] = 4,
                ["SCREEN"] = 16384,
                ["KBD"] = 24576,
            };

            for (ushort i = 0; i <= 15; i++)
                symbols.Add("R" + i, i);
        }

        public void Assemble(string targetPath, out bool succeeded)
        {
            succeeded = true;
            FirstPass(ref succeeded);
            using (FileStream target = new FileStream(targetPath, FileMode.OpenOrCreate))
                SecondPass(target, ref succeeded);
        }

        void FirstPass(ref bool succeeded)
        {
            parser.Reset();
            ushort command = 0;
            while (parser.HasMoreCommands)
            {
                parser.Advance();
                CommandType type = parser.CommandType;
                if (type == CommandType.Label)
                {
                    string name = parser.Symbol;
                    if (name == null)
                    {
                        succeeded = false;
                        logger.LogError("Missing closing bracket.", parser.LineNumber, CommandType.Label);
                        continue;
                    }

                    if (symbols.ContainsKey(name))
                    {
                        succeeded = false;
                        logger.LogError("Label '" + name + "' has already been defined.", parser.LineNumber, CommandType.Label);
                        continue;
                    }

                    symbols.Add(name, command);
                }
                else
                    command++;
            }
        }
        
        void SecondPass(FileStream target, ref bool succeeded)
        {
            parser.Reset();
            target.Position = 0;
            while (parser.HasMoreCommands)
            {
                parser.Advance();
                CommandType type = parser.CommandType;
                if (type == CommandType.Invalid)
                {
                    succeeded = false;
                    logger.LogError("Invalid command.", parser.LineNumber, type);
                    continue;
                }

                char[] machineCode = null;
                switch (type)
                {
                    case CommandType.Address:
                        machineCode = GenerateA();
                        break;

                    case CommandType.Compute:
                        machineCode = GenerateC(ref succeeded);
                        break;

                    case CommandType.Label:
                        continue;
                }

                for (int i = 0; i < 16; i++)
                    target.WriteByte(Convert.ToByte(machineCode[i]));

                target.WriteByte(Convert.ToByte('\n'));
            }
            target.SetLength(target.Position - 1);
        }

        char[] GenerateA()
        {
            char[] result = new char[16];
            result[0] = '0';
            bool decimalNumber = ushort.TryParse(parser.Symbol, out ushort x);
            if (!decimalNumber)
                x = AddVariable(parser.Symbol);

            for (int i = 1; i < result.Length; i++)
                result[i] = MathUtils.GetDigit(x, result.Length - i - 1, 2).ToString()[0];

            return result;
        }

        ushort AddVariable(string name)
        {
            if (symbols.ContainsKey(name))
                return symbols[name];

            ushort value = (ushort)(16 + variableCount);
            symbols.Add(name, value);
            variableCount++;
            return value;
        }

        char[] GenerateC(ref bool succeeded)
        {
            char[] result = new char[16];
            result[0] = '1';
            result[1] = '1';
            result[2] = '1';

            bool validComputation = CodeTranslator.TranslateComputation(parser.Comp, out result[3], out result[4], out result[5], out result[6], out result[7], out result[8], out result[9]);
            if (!validComputation)
            {
                Console.WriteLine(parser.Comp == "M+D\n");
                logger.LogError("Invalid computation " + parser.Comp + ".", parser.LineNumber, CommandType.Compute);
                succeeded = false;
            }

            bool validDestination = CodeTranslator.TranslateDestination(parser.Dest, out result[10], out result[11], out result[12]);
            if (!validDestination)
            {
                logger.LogError("Invalid destination " + parser.Dest + ".", parser.LineNumber, CommandType.Compute);
                succeeded = false;
            }

            bool validJumpCondition = CodeTranslator.TranslateJumpCondition(parser.Jump, out result[13], out result[14], out result[15]);
            if (!validJumpCondition)
            {
                logger.LogError("Invalid jump condition " + parser.Jump + ".", parser.LineNumber, CommandType.Compute);
                succeeded = false;
            }

            return result;
        }
    }
}