using System;
using System.IO;
using System.Collections.Generic;

namespace AnotherProject
{
    class Program
    {
        static Dictionary<string, int> variables;
        static Dictionary<string, int> labels;
        static Dictionary<string, int> predefinedSymbols;

        static double intPow(double b, int exponent)
        {
            double result = 1;
            for (int i = 0; i < exponent; i++)
            {
                result *= b;
            }
            return result;
        }

        static int GetDigit(int number, int digit, int b)
        {
            return (int)(Math.Floor(number / intPow(b, digit)) - b * Math.Floor(number / intPow(b, digit + 1)));
        }

        static void Main(string[] args)
        {
            //Random random = new Random();

            //double b = 1;
            //double d = 0.1;
            //double n0 = 100;

            //double n = n0;
            //double nRandom = n0;
            //int t = 0;
            //double sum = n0;
            //double sumInRandom = n0;
            //double average = n0;
            //double averageInRandom = n0;
            //while (true)
            //{
            //    Console.WriteLine("Amount: " + n);
            //    Console.WriteLine("Prediction:" + (b/d + intPow(1-d, t)*(n0 - b/d)));
            //    Console.WriteLine("Random Amount:" + nRandom);
            //    Console.WriteLine("Average:" + average);
            //    Console.WriteLine("Random Average:" + averageInRandom);
            //    Console.WriteLine();

            //    double deaths = d * n;
            //    n = n + b - deaths;
            //    t++;

            //    int deathsInRandom = 0;
            //    for (int i = 0; i < n; i++)
            //    {
            //        double c = random.NextDouble();
            //        if (c <= d)
            //            deathsInRandom++;
            //    }
            //    nRandom = nRandom + b - deathsInRandom;

            //    sum = sum + n;
            //    average = sum / (t + 1);

            //    sumInRandom = sumInRandom + nRandom;
            //    averageInRandom = sumInRandom / (t + 1);

            //    Console.ReadKey(true);
            //}
            //Console.ReadKey();
            //return;

            variables = new Dictionary<string, int>();
            labels = new Dictionary<string, int>();
            InitializePredefinedSymbols();

            Console.WriteLine("Enter path of ASM file.");
            string asmPath = Console.ReadLine();
            List<string> lines = new List<string>();
            using (FileStream file = File.Open(asmPath, FileMode.Open))
            {
                int length = (int)file.Length;
                byte[] data = new byte[length];
                file.Read(data, 0, length);

                int previousLineStart = 0;
                int relevantCharacterIndex = 0;
                List<char> relevantCharacters = new List<char>();
                for (int i = 0; i < length; i++)
                {
                    char character = Convert.ToChar(data[i]);
                    if (character == ' ')
                        continue;

                    relevantCharacters.Add(character);
                    if (character == '\n')
                    {
                        lines.Add(new string(relevantCharacters.ToArray(), previousLineStart, relevantCharacterIndex - previousLineStart));
                        previousLineStart = relevantCharacterIndex + 1;
                    }
                    relevantCharacterIndex++;
                }
                lines.Add(new string(relevantCharacters.ToArray(), previousLineStart, relevantCharacters.Count - previousLineStart));
            }

            int commandCount = 0;
            List<string> commandLines = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                int lineEnd = lines[i].Length;
                if (lines[i].Contains("//"))
                    lineEnd = lines[i].IndexOf("//");
                else if (lines[i].Contains("\r"))
                    lineEnd = lines[i].IndexOf('\r');

                if (lineEnd == 0)
                    continue;

                string line = lines[i].Substring(0, lineEnd);
                if (line[0] != '\r')
                {
                    if (line[0] == '(')
                        AddLabel(lines[i], commandCount);
                    else
                    {
                        commandCount++;
                        commandLines.Add(line);
                    }
                }
            }

            string[] machineCode = new string[commandCount];
            for (int i = 0; i < commandCount; i++)
            {
                if (commandLines[i][0] == '@')
                    machineCode[i] = ParseA(commandLines[i]);
                else
                    machineCode[i] = ParseC(commandLines[i]);
            }

            Console.WriteLine("Enter path of output Hack file. Use 'p' to write the text in this window instead and not to a file.");
            string hackPath = Console.ReadLine();
            if (hackPath == "p")
            {
                foreach (var m in machineCode)
                    Console.WriteLine(m);
            }
            else
            {
                using (FileStream outFile = new FileStream(hackPath, FileMode.Create))
                {
                    List<byte> data = new List<byte>();
                    for (int i = 0; i < machineCode.Length; i++)
                    {
                        for (int j = 0; j < machineCode[i].Length; j++)
                            data.Add(Convert.ToByte(machineCode[i][j]));

                        data.Add(Convert.ToByte('\n'));
                    }
                    outFile.Write(data.ToArray(), 0, data.Count);
                }
            }
            Console.ReadKey();
        }

        private static void InitializePredefinedSymbols()
        {
            predefinedSymbols = new Dictionary<string, int>()
            {
                ["SP"] = 0,
                ["LCL"] = 1,
                ["ARG"] = 2,
                ["THIS"] = 3,
                ["THAT"] = 4,
                ["SCREEN"] = 16384,
                ["KBD"] = 24576,
            };

            for (int i = 0; i <= 15; i++)
                predefinedSymbols.Add("R" + i, i);
        }

        private static void AddLabel(string line, int command)
        {
            int endIndex = line.IndexOf(')');
            string name = line.Substring(1, endIndex  - 1);
            if (labels.ContainsKey(name))
                return;

            labels.Add(name, command);
        }

        static string ParseA(string line)
        {
            // @x translates to 0y where y is a 15-bit base 2 representation of x
            char[] result = new char[16];
            result[0] = '0';
            string numberText = line.Substring(1, line.Length - 1);

            bool decimalNumber = int.TryParse(numberText, out int x);
            if (!decimalNumber)
            {
                if (predefinedSymbols.ContainsKey(numberText))
                    x = predefinedSymbols[numberText];
                else
                    x = GetVariableOrLabel(numberText);
            }
            for (int i = 1; i < result.Length; i++)
                result[i] = GetDigit(x, result.Length - i - 1, 2).ToString()[0];

            return new string(result);
        }

        static int GetVariableOrLabel(string name)
        {
            bool hasVariable = variables.TryGetValue(name, out int x);
            if (hasVariable)
                return x;

            bool hasLabel = labels.TryGetValue(name, out x);
            if (hasLabel)
                return x;

            x = 16 + variables.Count;
            variables.Add(name, x);
            return x;
        }

        static string ParseC(string line)
        {
            // Dest=Op;J translates to 111a dddd ddoo ojjj where stuffs.
            char[] result = new char[16];
            for (int i = 0; i < 3; i++)
                result[i] = '1';

            int jumpConditionStart = -1;
            int destinationEnd = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '=')
                    destinationEnd = i;

                if (line[i] == ';')
                    jumpConditionStart = i + 1;
            }

            string jumpConditionText = string.Empty;
            if (jumpConditionStart != -1)
            {
                char[] jumpCondition = new char[3];
                for (int i = 0; i < 3; i++)
                    jumpCondition[i] = line[jumpConditionStart + i];

                jumpConditionText = new string(jumpCondition);
            }
            TranslateJumpCondition(jumpConditionText, out result[13], out result[14], out result[15]);

            string destinationText = line.Substring(0, destinationEnd);
            TranslateDestination(destinationText, out result[10], out result[11], out result[12]);

            int operationStart = 0;
            if (destinationEnd != 0)
                operationStart = destinationEnd + 1;

            int operationEnd = line.Length - 1;
            if (jumpConditionStart != -1)
                operationEnd = jumpConditionStart - 2;

            string operation = line.Substring(operationStart, operationEnd - operationStart + 1);
            bool inputOperation = TranslateOperation(operation, out result[3], out result[4], out result[5], out result[6], out result[7], out result[8], out result[9]);
            if (inputOperation == false)
                Console.WriteLine("Mah man you fucked up, some line is bad. Here is line, since I didn't keep index: " + line);

            return new string(result);
        }

        static void TranslateJumpCondition(string text, out char j1, out char j2, out char j3)
        {
            if (text == "JGT") { j1 = '0'; j2 = '0'; j3 = '1'; }
            else if (text == "JEQ") { j1 = '0'; j2 = '1'; j3 = '0'; }
            else if (text == "JGE") { j1 = '0'; j2 = '1'; j3 = '1'; }
            else if (text == "JLT") { j1 = '1'; j2 = '0'; j3 = '0'; }
            else if (text == "JNE") { j1 = '1'; j2 = '0'; j3 = '1'; }
            else if (text == "JLE") { j1 = '1'; j2 = '1'; j3 = '0'; }
            else if (text == "JMP") { j1 = '1'; j2 = '1'; j3 = '1'; }
            else { j1 = '0'; j2 = '0'; j3 = '0'; }
        }

        static void TranslateDestination(string text, out char d1, out char d2, out char d3)
        {
            if (text == "M") { d1 = '0'; d2 = '0'; d3 = '1'; }
            else if (text == "D") { d1 = '0'; d2 = '1'; d3 = '0'; }
            else if (text == "MD") { d1 = '0'; d2 = '1'; d3 = '1'; }
            else if (text == "A") { d1 = '1'; d2 = '0'; d3 = '0'; }
            else if (text == "AM") { d1 = '1'; d2 = '0'; d3 = '1'; }
            else if (text == "AD") { d1 = '1'; d2 = '1'; d3 = '0'; }
            else if (text == "AMD") { d1 = '1'; d2 = '1'; d3 = '1'; }
            else { d1 = '0'; d2 = '0'; d3 = '0'; }
        }

        static bool TranslateOperation(string text, out char a, out char c1, out char c2, out char c3, out char c4, out char c5, out char c6)
        {
            if (text == "0") { a = '0'; c1 = '1'; c2 = '0'; c3 = '1'; c4 = '0'; c5 = '1'; c6 = '0'; }
            else if (text == "1") { a = '0'; c1 = '1'; c2 = '1'; c3 = '1'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "-1") { a = '0'; c1 = '1'; c2 = '1'; c3 = '1'; c4 = '0'; c5 = '1'; c6 = '0'; }
            else if (text == "D") { a = '0'; c1 = '0'; c2 = '0'; c3 = '1'; c4 = '1'; c5 = '0'; c6 = '0'; }
            else if (text == "A") { a = '0'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '0'; }
            else if (text == "M") { a = '1'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '0'; }
            else if (text == "!D") { a = '0'; c1 = '0'; c2 = '0'; c3 = '1'; c4 = '1'; c5 = '0'; c6 = '1'; }
            else if (text == "!A") { a = '0'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '1'; }
            else if (text == "!M") { a = '1'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '1'; }
            else if (text == "-D") { a = '0'; c1 = '0'; c2 = '0'; c3 = '1'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "-A") { a = '0'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '1'; }
            else if (text == "-M") { a = '1'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '1'; }
            else if (text == "D+1") { a = '0'; c1 = '0'; c2 = '1'; c3 = '1'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "A+1") { a = '0'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "M+1") { a = '1'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "D-1") { a = '0'; c1 = '0'; c2 = '0'; c3 = '1'; c4 = '1'; c5 = '1'; c6 = '0'; }
            else if (text == "A-1") { a = '0'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '0'; }
            else if (text == "M-1") { a = '1'; c1 = '1'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '0'; }
            else if (text == "D+A" || text == "A+D") { a = '0'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '0'; }
            else if (text == "D+M" || text == "M+D") { a = '1'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '0'; }
            else if (text == "D-A") { a = '0'; c1 = '0'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '1'; }
            else if (text == "D-M") { a = '1'; c1 = '0'; c2 = '1'; c3 = '0'; c4 = '0'; c5 = '1'; c6 = '1'; }
            else if (text == "A-D") { a = '0'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "M-D") { a = '1'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '1'; c5 = '1'; c6 = '1'; }
            else if (text == "D&A" || text == "A&D") { a = '0'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '0'; }
            else if (text == "D&M" || text == "M&D") { a = '1'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '0'; }
            else if (text == "D|A" || text == "A|D") { a = '0'; c1 = '0'; c2 = '1'; c3 = '0'; c4 = '1'; c5 = '0'; c6 = '1'; }
            else if (text == "D|M" || text == "M|D") { a = '1'; c1 = '0'; c2 = '1'; c3 = '0'; c4 = '1'; c5 = '0'; c6 = '1'; }
            else { a = '0'; c1 = '0'; c2 = '0'; c3 = '0'; c4 = '0'; c5 = '0'; c6 = '0'; return false; }

            return true;
        }
    }
}