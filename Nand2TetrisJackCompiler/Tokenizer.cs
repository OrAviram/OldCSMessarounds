using System;
using System.Collections.Generic;
using System.IO;

namespace Nand2TetrisJackCompiler
{
    enum TokenType
    {
        Keyword, Symbol, IntegerConstant, StringConstant, Identifier
    }

    class Tokenizer
    {
        struct Token
        {
            public int lineNumber;
            public string text;
            public TokenType type;

            public Token(int lineNumber, string text, TokenType type)
            {
                this.lineNumber = lineNumber;
                this.text = text;
                this.type = type;
            }
        }

        List<Token> tokens;
        int currentIndex;
        
        public void Reset() { currentIndex = -1; }

        public void SetFile(string path)
        {
            currentIndex = -1;
            tokens = new List<Token>();
            using (FileStream file = File.Open(path, FileMode.Open))
            {
                byte[] data = new byte[file.Length];
                file.Read(data, 0, (int)file.Length);

                int lineNumber = 1;
                bool readingLineComment = false;
                bool readingLongComment = false;
                bool readingString = false;
                string stringConstant = string.Empty;
                string word = string.Empty;
                for (int i = 0; i < data.Length; i++)
                {
                    char c = Convert.ToChar(data[i]);
                    if (c == '\r')
                        continue;

                    if (c == '\n')
                    {
                        if (!readingLineComment && word != string.Empty && !readingString)
                            AddWord(ref word, lineNumber);

                        lineNumber++;
                        readingLineComment = false;
                        continue;
                    }

                    if (c == '/' && i + 1 < data.Length)
                    {
                        char next = Convert.ToChar(data[i + 1]);
                        if (next == '/')
                        {
                            readingLineComment = true;
                            i++;
                            continue;
                        }
                        else if (next == '*')
                        {
                            readingLongComment = true;
                            i++;
                            continue;
                        }
                    }

                    if (c == '*' && i + 1 < data.Length)
                    {
                        if (Convert.ToChar(data[i + 1]) == '/')
                        {
                            readingLongComment = false;
                            i++;
                            continue;
                        }
                    }

                    if (readingLongComment || readingLineComment)
                        continue;

                    if ((c == '\t' || c == ' ') && word != string.Empty && !readingString)
                        AddWord(ref word, lineNumber);

                    if (IsSymbol(c))
                    {
                        if (word != string.Empty)
                            AddWord(ref word, lineNumber);

                        tokens.Add(new Token(lineNumber, c.ToString(), TokenType.Symbol));
                        continue;
                    }

                    if (readingString)
                        stringConstant += c;

                    if (c == '\"')
                    {
                        if (readingString)
                        {
                            tokens.Add(new Token(lineNumber, stringConstant, TokenType.StringConstant));
                            stringConstant = string.Empty;
                            readingString = false;
                        }
                        else
                        {
                            stringConstant += c;
                            readingString = true;
                        }
                        word = string.Empty;
                        continue;
                    }

                    if (c != ' ' && c != '\t')
                        word += c;
                }
            }
        }

        void AddWord(ref string word, int lineNumber)
        {
            TokenType type;
            if (IsKeyword(word))
                type = TokenType.Keyword;
            else if (int.TryParse(word, out int r))
                type = TokenType.IntegerConstant;
            else
                type = TokenType.Identifier;

            tokens.Add(new Token(lineNumber, word, type));
            word = string.Empty;
        }

        static bool IsSymbol(char c)
        {
            return c == '{' || c == '}' || c == '(' || c == ')' || c == '[' || c == ']'
                || c == '&' || c == '|' || c == '<' || c == '>' || c == '=' || c == '~'
                || c == '+' || c == '-' || c == '*' || c == '/'
                || c == '.' || c == ',' || c == ';';
        }

        static bool IsKeyword(string word)
        {
            return word == "class" || word == "constructor" || word == "function" || word == "method"
                || word == "let" || word == "do" || word == "if" || word == "else" || word == "while" || word == "return"
                || word == "int" || word == "char" || word == "boolean" || word == "void"
                || word == "true" || word == "false" || word == "null" || word == "this"
                || word == "field" || word == "static" || word == "var";
        }

        public TokenType CurrentType => tokens[currentIndex].type;
        public int CurrentLineNumber => tokens[currentIndex].lineNumber;

        public string Keyword => tokens[currentIndex].text;
        public char Symbol => tokens[currentIndex].text[0];
        public string Identifier => tokens[currentIndex].text;
        public string RawText => tokens[currentIndex].text;

        public int IntegerValue { get; private set; }
        public string StringValue { get; private set; }

        public int LastLineNumber => tokens[tokens.Count - 1].lineNumber;

        public void Advance()
        {
            currentIndex++;
            IntegerValue = 0;
            StringValue = string.Empty;

            string text = tokens[currentIndex].text;
            if (CurrentType == TokenType.IntegerConstant)
                IntegerValue = int.Parse(text);
            else if (CurrentType == TokenType.StringConstant)
                StringValue = text.Substring(1, text.Length - 2);
        }

        public void GoBack()
        {
            currentIndex -= 2;
            Advance();
        }

        public bool HasMoreTokens => currentIndex < tokens.Count - 1;
    }
}