using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;

namespace Nand2TetrisJackCompiler
{
    interface ILogger
    {
        void LogWarning(string message, int lineNumber);
        void LogError(string message, int lineNumber);
        void LogUnexpectedTextError(string expected, string found, int lineNumber);
    }

    class CompilationEngine
    {
        enum ClassSymbolKind { Field, Static }
        enum IdentifierUsage { ClassDefinition, SubroutineDefinition, VariableDefinition, Usage }
        enum SubroutineType { Constructor, Function, Method }

        Tokenizer tokenizer;
        XmlTextWriter xmlWriter;
        VMWriter vmWriter;
        ILogger logger;
        SymbolTable symbolTable;
        string className = string.Empty;
        List<string> methodNames;

        int currentLabeledStatementID;

        public CompilationEngine(Tokenizer tokenizer, ILogger logger)
        {
            this.tokenizer = tokenizer;
            this.logger = logger;
            symbolTable = new SymbolTable();
            methodNames = new List<string>();
        }

        public void CompileFile(string xmlOutPath, string vmOutPath)
        {
            vmWriter = new VMWriter(vmOutPath);
            xmlWriter = new XmlTextWriter(xmlOutPath, Encoding.ASCII);
            xmlWriter.Formatting = Formatting.Indented;
            try
            {
                tokenizer.Advance();
                if (tokenizer.CurrentType == TokenType.Keyword && tokenizer.Keyword == "class")
                    CompileClass();
                else
                    logger.LogError("File must start with a class.", tokenizer.CurrentLineNumber);
            }
            catch (ArgumentOutOfRangeException)
            {
                logger.LogError("Unexpected end of file.", tokenizer.LastLineNumber);
            }
            finally
            {
                xmlWriter.Dispose();
                vmWriter.Dispose();
            }
        }

        void CompileClass()
        {
            symbolTable.StartClass();
            methodNames.Clear();

            xmlWriter.WriteStartElement("class");
            {
                xmlWriter.WriteElementString("keyword", "class");

                tokenizer.Advance();
                CompileIdentifier(IdentifierUsage.ClassDefinition);
                className = tokenizer.Identifier;

                tokenizer.Advance();
                ExpectSymbol('{');

                tokenizer.Advance();
                while (tokenizer.HasMoreTokens)
                {
                    if (tokenizer.Keyword == "field")
                        CompileClassVar(ClassSymbolKind.Field);
                    else if (tokenizer.Keyword == "static")
                        CompileClassVar(ClassSymbolKind.Static);
                    else
                        break;

                    tokenizer.Advance();
                }

                while (tokenizer.HasMoreTokens)
                {
                    if (tokenizer.Keyword == "function")
                        CompileSubroutine(SubroutineType.Function);
                    else if (tokenizer.Keyword == "method")
                        CompileSubroutine(SubroutineType.Method);
                    else if (tokenizer.Keyword == "constructor")
                        CompileSubroutine(SubroutineType.Constructor);
                    else
                        break;

                    tokenizer.Advance();
                }

                ExpectSymbol('}');
                if (tokenizer.HasMoreTokens)
                    logger.LogError("End of file expected.", tokenizer.CurrentLineNumber);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileClassVar(ClassSymbolKind kind)
        {
            xmlWriter.WriteStartElement("classVarDec");
            {
                SymbolKind symbolKind = SymbolKind.None;
                switch (kind)
                {
                    case ClassSymbolKind.Field:
                        xmlWriter.WriteElementString("keyword", "field");
                        symbolKind = SymbolKind.Field;
                        break;
                    case ClassSymbolKind.Static:
                        xmlWriter.WriteElementString("keyword", "static");
                        symbolKind = SymbolKind.Static;
                        break;
                }

                tokenizer.Advance();
                CompileVariableList(symbolKind);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileSubroutine(SubroutineType type)
        {
            symbolTable.StartSubroutine();
            currentLabeledStatementID = 0;

            xmlWriter.WriteStartElement("subroutineDec");
            {
                bool isMethod = false;
                switch (type)
                {
                    case SubroutineType.Constructor:
                        xmlWriter.WriteElementString("keyword", "constructor");
                        break;
                    case SubroutineType.Function:
                        xmlWriter.WriteElementString("keyword", "function");
                        break;
                    case SubroutineType.Method:
                        xmlWriter.WriteElementString("keyword", "method");
                        symbolTable.Define("this", className, SymbolKind.Argument);
                        isMethod = true;
                        break;
                }

                tokenizer.Advance();
                bool isVoid = false;
                if (!CompileType(out string t))
                {
                    if (tokenizer.Keyword != "void")
                        logger.LogError("Return type must be either 'void', 'int', 'boolean', 'char', or an identifier.", tokenizer.CurrentLineNumber);
                    else
                    {
                        xmlWriter.WriteElementString("keyword", "void");
                        isVoid = true;
                    }
                }
                
                tokenizer.Advance();
                CompileIdentifier(IdentifierUsage.SubroutineDefinition);
                string subroutineName = tokenizer.Identifier;

                if (isMethod)
                    methodNames.Add(subroutineName);

                tokenizer.Advance();
                ExpectSymbol('(');

                tokenizer.Advance();
                CompileParameterList();
                ExpectSymbol(')');

                tokenizer.Advance();
                CompileSubroutineBody(subroutineName, type, isVoid);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileParameterList()
        {
            xmlWriter.WriteStartElement("parameterList");
            {
                if (tokenizer.Symbol == ')')
                {
                    xmlWriter.WriteString("\n");
                    xmlWriter.WriteEndElement();
                    return;
                }

                bool compileComma = false;
                do
                {
                    if (compileComma)
                    {
                        xmlWriter.WriteElementString("symbol", ",");
                        tokenizer.Advance();
                    }

                    if (!CompileType(out string type))
                        logger.LogError("Invalid type " + tokenizer.RawText + ".", tokenizer.CurrentLineNumber);

                    tokenizer.Advance();
                    CompileIdentifier(IdentifierUsage.VariableDefinition, SymbolKind.Argument, type);

                    tokenizer.Advance();
                    compileComma = true;
                }
                while (tokenizer.Symbol == ',');
            }
            xmlWriter.WriteEndElement();
        }

        void CompileSubroutineBody(string name, SubroutineType type, bool isVoid)
        {
            xmlWriter.WriteStartElement("subroutineBody");
            {
                ExpectSymbol('{');
                tokenizer.Advance();
                while (tokenizer.Keyword == "var")
                {
                    CompileSubroutineVar();
                    tokenizer.Advance();
                }

                int localsCount = symbolTable.VarCount(SymbolKind.Variable);
                vmWriter.WriteFunction(className + "." + name, localsCount);
                if (type == SubroutineType.Method)
                {
                    vmWriter.WritePush(MemorySegment.Argument, 0);
                    vmWriter.WritePop(MemorySegment.Pointer, 0);
                }
                else if (type == SubroutineType.Constructor)
                {
                    // Write allocation code and set pointer[0].
                }

                bool returned = CompileStatements(isVoid);
                if (!returned)
                    logger.LogWarning("Missing return statement.", tokenizer.CurrentLineNumber);

                ExpectSymbol('}');
            }
            xmlWriter.WriteEndElement();
        }

        void CompileSubroutineVar()
        {
            xmlWriter.WriteStartElement("varDec");
            {
                xmlWriter.WriteElementString("keyword", "var");

                tokenizer.Advance();
                CompileVariableList(SymbolKind.Variable);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileVariableList(SymbolKind symbolKind)
        {
            if (!CompileType(out string type))
                logger.LogError("Invalid type " + tokenizer.RawText + ".", tokenizer.CurrentLineNumber);
            
            tokenizer.Advance();
            CompileIdentifier(IdentifierUsage.VariableDefinition, symbolKind, type);

            tokenizer.Advance();
            while (tokenizer.Symbol != ';')
            {
                ExpectSymbol(',');

                tokenizer.Advance();
                CompileIdentifier(IdentifierUsage.VariableDefinition, symbolKind, type);

                tokenizer.Advance();
            }
            xmlWriter.WriteElementString("symbol", ";");
        }

        bool CompileStatements(bool isVoid)
        {
            bool returns = false;
            xmlWriter.WriteStartElement("statements");
            {
                while (tokenizer.CurrentType == TokenType.Keyword)
                {
                    if (tokenizer.Keyword == "do")
                        CompileDoStatement();
                    else if (tokenizer.Keyword == "let")
                        CompileLetStatement();
                    else if (tokenizer.Keyword == "if")
                        CompileIfStatement(isVoid);
                    else if (tokenizer.Keyword == "while")
                        CompileWhileStatement(isVoid);
                    else if (tokenizer.Keyword == "return")
                    {
                        CompileReturnStatement(isVoid);
                        returns = true;
                    }
                    else
                        break;
                }
            }
            xmlWriter.WriteEndElement();
            return returns;
        }

        void CompileDoStatement()
        {
            xmlWriter.WriteStartElement("doStatement");
            {
                xmlWriter.WriteElementString("keyword", "do");

                tokenizer.Advance();
                CompileSubroutineCall();
                vmWriter.WritePop(MemorySegment.Temp, 0);

                tokenizer.Advance();
                ExpectSymbol(';');

                tokenizer.Advance();
            }
            xmlWriter.WriteEndElement();
        }

        void CompileLetStatement()
        {
            xmlWriter.WriteStartElement("letStatement");
            {
                xmlWriter.WriteElementString("keyword", "let");

                tokenizer.Advance();
                CompileIdentifier(IdentifierUsage.Usage);
                string targetName = tokenizer.Identifier;

                tokenizer.Advance();
                if (tokenizer.Symbol == '[')
                {
                    CompileIndexer();
                    tokenizer.Advance();
                }
                ExpectSymbol('=');

                tokenizer.Advance();
                CompileExpression();
                ExpectSymbol(';');
                PopToSymbol(targetName);
                
                tokenizer.Advance();
            }
            xmlWriter.WriteEndElement();
        }

        void CompileIfStatement(bool isVoid)
        {
            string elseLabel = "Else_" + currentLabeledStatementID;
            string endIfLabel = "EndIf_" + currentLabeledStatementID;
            currentLabeledStatementID++;

            xmlWriter.WriteStartElement("ifStatement");
            {
                xmlWriter.WriteElementString("keyword", "if");

                tokenizer.Advance();
                ExpectSymbol('(');

                tokenizer.Advance();
                CompileExpression();
                vmWriter.WriteArithmetic(ArithmeticCommand.Not);

                ExpectSymbol(')');

                tokenizer.Advance();
                ExpectSymbol('{');

                tokenizer.Advance();
                vmWriter.WriteIfGoto(elseLabel);
                CompileStatements(isVoid);
                vmWriter.WriteGoto(endIfLabel);

                ExpectSymbol('}');

                tokenizer.Advance();
                vmWriter.WriteLabel(elseLabel);
                if (tokenizer.Keyword == "else")
                {
                    xmlWriter.WriteElementString("keyword", "else");

                    tokenizer.Advance();
                    ExpectSymbol('{');

                    tokenizer.Advance();
                    CompileStatements(isVoid);
                    ExpectSymbol('}');

                    tokenizer.Advance();
                }
                vmWriter.WriteLabel(endIfLabel);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileWhileStatement(bool isVoid)
        {
            string beginWhileLabel = "BeginWhile_" + currentLabeledStatementID;
            string endWhileLabel = "EndWhile_" + currentLabeledStatementID;
            currentLabeledStatementID++;
            xmlWriter.WriteStartElement("whileStatement");
            {
                xmlWriter.WriteElementString("keyword", "while");

                tokenizer.Advance();
                ExpectSymbol('(');

                tokenizer.Advance();
                vmWriter.WriteLabel(beginWhileLabel);
                CompileExpression();

                vmWriter.WriteArithmetic(ArithmeticCommand.Not);
                vmWriter.WriteIfGoto(endWhileLabel);

                ExpectSymbol(')');

                tokenizer.Advance();
                ExpectSymbol('{');

                tokenizer.Advance();
                CompileStatements(isVoid);
                vmWriter.WriteGoto(beginWhileLabel);

                ExpectSymbol('}');
                tokenizer.Advance();

                vmWriter.WriteLabel(endWhileLabel);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileReturnStatement(bool isVoid)
        {
            xmlWriter.WriteStartElement("returnStatement");
            {
                xmlWriter.WriteElementString("keyword", "return");

                tokenizer.Advance();
                if (tokenizer.Symbol != ';')
                {
                    if (isVoid)
                        logger.LogError("Cannot return value from void subroutine.", tokenizer.CurrentLineNumber);
                    else
                        CompileExpression();
                }
                else
                {
                    if (isVoid)
                        vmWriter.WritePush(MemorySegment.Constant, 0);
                    else
                        logger.LogError("Non-void functions must return a value.", tokenizer.CurrentLineNumber);
                }

                ExpectSymbol(';');
                vmWriter.WriteReturn();

                tokenizer.Advance();
            }
            xmlWriter.WriteEndElement();
        }

        void CompileExpression()
        {
            xmlWriter.WriteStartElement("expression");
            {
                CompileTerm();
                while (IsOp())
                {
                    char op = tokenizer.Symbol;
                    tokenizer.Advance();
                    CompileTerm();
                    CompileOp(op);
                }
            }
            xmlWriter.WriteEndElement();
        }

        bool IsOp()
        {
            char s = tokenizer.Symbol;
            if (s == '+' || s == '-' || s == '*' || s == '/' || s == '&' || s == '|' || s == '<' || s == '>' || s == '=')
            {
                xmlWriter.WriteElementString("symbol", s.ToString());
                return true;
            }
            return false;
        }

        void CompileOp(char op)
        {
            switch (op)
            {
                case '+':
                    vmWriter.WriteArithmetic(ArithmeticCommand.Add);
                    break;
                case '-':
                    vmWriter.WriteArithmetic(ArithmeticCommand.Subtract);
                    break;
                case '*':
                    vmWriter.WriteCall("Math.multiply", 2);
                    break;
                case '/':
                    vmWriter.WriteCall("Math.divide", 2);
                    break;
                case '&':
                    vmWriter.WriteArithmetic(ArithmeticCommand.And);
                    break;
                case '|':
                    vmWriter.WriteArithmetic(ArithmeticCommand.Or);
                    break;
                case '<':
                    vmWriter.WriteArithmetic(ArithmeticCommand.LessThan);
                    break;
                case '>':
                    vmWriter.WriteArithmetic(ArithmeticCommand.GreaterThan);
                    break;
                case '=':
                    vmWriter.WriteArithmetic(ArithmeticCommand.Equals);
                    break;
            }
        }

        void CompileTerm()
        {
            xmlWriter.WriteStartElement("term");
            if (tokenizer.CurrentType == TokenType.IntegerConstant)
            {
                xmlWriter.WriteElementString("integerConstant", tokenizer.IntegerValue.ToString());
                vmWriter.WritePush(MemorySegment.Constant, tokenizer.IntegerValue);
                tokenizer.Advance();
            }
            else if (tokenizer.CurrentType == TokenType.StringConstant)
            {
                xmlWriter.WriteElementString("stringConstant", tokenizer.StringValue);
                // TODO: Push String variable (from operating system).
                tokenizer.Advance();
            }
            else if (tokenizer.CurrentType == TokenType.Keyword)
            {
                string keyword = tokenizer.Keyword;
                if (keyword == "true")
                {
                    xmlWriter.WriteElementString("keyword", keyword);
                    vmWriter.WritePush(MemorySegment.Constant, 1);
                    vmWriter.WriteArithmetic(ArithmeticCommand.Negate);
                }
                else if (keyword == "false" || keyword == "null")
                {
                    xmlWriter.WriteElementString("keyword", keyword);
                    vmWriter.WritePush(MemorySegment.Constant, 0);
                }
                else if (keyword == "this")
                {
                    // TODO: handle this
                }
                else
                    logger.LogError("Unexpected keyword '" + keyword + "'.", tokenizer.CurrentLineNumber);

                tokenizer.Advance();
            }
            else if (tokenizer.CurrentType == TokenType.Symbol)
            {
                char symbol = tokenizer.Symbol;
                if (symbol == '(')
                {
                    xmlWriter.WriteElementString("symbol", "(");

                    tokenizer.Advance();
                    CompileExpression();

                    ExpectSymbol(')');
                    tokenizer.Advance();
                }
                else if (symbol == '-' || symbol == '~')
                {
                    xmlWriter.WriteElementString("symbol", symbol.ToString());

                    tokenizer.Advance();
                    CompileTerm();
                    if (symbol == '-')
                        vmWriter.WriteArithmetic(ArithmeticCommand.Negate);
                    else
                        vmWriter.WriteArithmetic(ArithmeticCommand.Not);
                }
                else
                    logger.LogError("Unexpected symbol '" + symbol + "'.", tokenizer.CurrentLineNumber);
            }
            else if (tokenizer.CurrentType == TokenType.Identifier)
            {
                // TODO: Ya know, pushes and shit.
                string name = tokenizer.Identifier;
                CompileIdentifier(IdentifierUsage.Usage);
                tokenizer.Advance();

                if (tokenizer.CurrentType == TokenType.Symbol)
                {
                    char symbol = tokenizer.Symbol;
                    if (symbol == '.')
                    {
                        tokenizer.GoBack();
                        CompileSubroutineCall();
                        tokenizer.Advance();
                        //xmlWriter.WriteElementString("symbol", symbol.ToString());
                        //
                        //tokenizer.Advance();
                        //CompileSubroutineCall();
                        //tokenizer.Advance();
                    }
                    else if (symbol == '(')
                    {
                        // TODO: handle direct function calls
                        CompileArgumentList();
                        tokenizer.Advance();
                    }
                    else if (symbol == '[')
                    {
                        // TODO: handle arrays
                        CompileIndexer();
                        tokenizer.Advance();
                    }
                    else
                        PushSymbol(name);
                }
                else
                    PushSymbol(name);
            }
            xmlWriter.WriteEndElement();
        }

        void CompileIndexer()
        {
            xmlWriter.WriteElementString("symbol", "[");
            
            tokenizer.Advance();
            CompileExpression();
            ExpectSymbol(']');
        }

        void CompileSubroutineCall()
        {
            string subroutineName = string.Empty;
            bool isMethod = false;
            string identifierName = tokenizer.Identifier;
            CompileIdentifier(IdentifierUsage.Usage);

            SymbolKind symbolKind = symbolTable.KindOf(identifierName);
            if (symbolKind != SymbolKind.None)
            {
                // Method on a defined variable.

                subroutineName = symbolTable.TypeOf(identifierName) + ".";
                isMethod = true;

                tokenizer.Advance();
                ExpectSymbol('.');

                tokenizer.Advance();
                CompileIdentifier(IdentifierUsage.Usage);
                subroutineName += tokenizer.Identifier;

                PushSymbol(identifierName);
            }
            else
            {
                tokenizer.Advance();
                if (tokenizer.Symbol == '.')
                {
                    // Function in a different class.
                    xmlWriter.WriteElementString("symbol", ".");

                    tokenizer.Advance();
                    subroutineName = identifierName + "." + tokenizer.Identifier;
                }
                else
                {
                    // Function or method in this class, need to check to know if we need to push this.
                    subroutineName = className + "." + identifierName;
                    if (methodNames.Contains(identifierName))
                    {
                        isMethod = true;
                        PushSymbol("this");
                    }
                }
            }

            tokenizer.Advance();
            int argumentCount = CompileArgumentList();
            if (isMethod)
                argumentCount++;

            vmWriter.WriteCall(subroutineName, argumentCount);
        }

        int CompileArgumentList()
        {
            ExpectSymbol('(');
        
            tokenizer.Advance();
            int expressionCount = CompileExpressionList();
            ExpectSymbol(')');

            return expressionCount;
        }

        int CompileExpressionList()
        {
            int expressionCount = 0;
            xmlWriter.WriteStartElement("expressionList");
            {
                if (tokenizer.Symbol == ')')
                {
                    xmlWriter.WriteString("\n");
                    xmlWriter.WriteEndElement();
                    return 0;
                }

                bool compileComma = false;
                do
                {
                    expressionCount++;
                    if (compileComma)
                    {
                        xmlWriter.WriteElementString("symbol", ",");
                        tokenizer.Advance();
                    }
                    CompileExpression();
                    compileComma = true;
                }
                while (tokenizer.Symbol == ',');
            }
            xmlWriter.WriteEndElement();
            return expressionCount;
        }

        void PushSymbol(string name)
        {
            int runningIndex = symbolTable.IndexOf(name).Value;
            SymbolKind symbolKind = symbolTable.KindOf(name);

            switch (symbolKind)
            {
                case SymbolKind.Field:
                    vmWriter.WritePush(MemorySegment.This, runningIndex);
                    break;
                case SymbolKind.Static:
                    vmWriter.WritePush(MemorySegment.Static, runningIndex);
                    break;
                case SymbolKind.Variable:
                    vmWriter.WritePush(MemorySegment.Local, runningIndex);
                    break;
                case SymbolKind.Argument:
                    vmWriter.WritePush(MemorySegment.Argument, runningIndex);
                    break;
            }
        }

        void PopToSymbol(string name)
        {
            int runningIndex = symbolTable.IndexOf(name).Value;
            SymbolKind symbolKind = symbolTable.KindOf(name);

            switch (symbolKind)
            {
                case SymbolKind.Field:
                    vmWriter.WritePop(MemorySegment.This, runningIndex);
                    break;
                case SymbolKind.Static:
                    vmWriter.WritePop(MemorySegment.Static, runningIndex);
                    break;
                case SymbolKind.Variable:
                    vmWriter.WritePop(MemorySegment.Local, runningIndex);
                    break;
                case SymbolKind.Argument:
                    vmWriter.WritePop(MemorySegment.Argument, runningIndex);
                    break;
            }
        }

        bool CompileType(out string type)
        {
            if (tokenizer.CurrentType == TokenType.Keyword)
            {
                string t = tokenizer.Keyword;
                if (t != "int" && t != "char" && t != "boolean")
                {
                    type = null;
                    return false;
                }
                else
                {
                    xmlWriter.WriteElementString("keyword", tokenizer.Keyword);
                    type = tokenizer.Keyword;
                }
            }
            else if (tokenizer.CurrentType == TokenType.Identifier)
            {
                xmlWriter.WriteElementString("identifier", tokenizer.Identifier);
                type = tokenizer.Identifier;
            }
            else
            {
                type = null;
                return false;
            }
            return true;
        }

        void CompileIdentifier(IdentifierUsage usage, SymbolKind symbolKind = SymbolKind.None, string type = null)
        {
            if (tokenizer.CurrentType != TokenType.Identifier)
                logger.LogError("Identifier expected.", tokenizer.CurrentLineNumber);
            else
            {
                xmlWriter.WriteStartElement("identifier");
                {
                    xmlWriter.WriteAttributeString("usage", usage.ToString());

                    if (usage == IdentifierUsage.VariableDefinition)
                    {
                        symbolTable.Define(tokenizer.Identifier, type, symbolKind);
                        xmlWriter.WriteAttributeString("type", type);
                    }
                    int? index = symbolTable.IndexOf(tokenizer.Identifier);
                    if (index.HasValue)
                    {
                        xmlWriter.WriteAttributeString("kind", symbolTable.KindOf(tokenizer.Identifier).ToString());
                        xmlWriter.WriteAttributeString("index", index.Value.ToString());
                    }

                    xmlWriter.WriteString(tokenizer.Identifier);
                }
                xmlWriter.WriteEndElement();
            }
        }

        void ExpectSymbol(char symbol)
        {
            string str = symbol.ToString();
            if (tokenizer.Symbol != symbol)
                logger.LogUnexpectedTextError(str, tokenizer.RawText, tokenizer.CurrentLineNumber);
            else
                xmlWriter.WriteElementString("symbol", str);
        }
    }
}