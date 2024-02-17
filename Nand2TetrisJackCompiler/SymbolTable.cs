using System.Collections.Generic;

namespace Nand2TetrisJackCompiler
{
    enum SymbolKind
    {
        Field, Static,
        Variable, Argument,
        None
    }

    struct Symbol
    {
        public string type;
        public int runningIndex;
        public SymbolKind kind;
    }

    class SymbolTable
    {
        Dictionary<string, Symbol> subroutineSymbols;
        Dictionary<string, Symbol> classSymbols;
        int argumentCount; // variableCount = subroutineSymbols.Count - argumentCount
        int fieldCount; // staticCount = classSymbols.Count - fieldCount

        public SymbolTable()
        {
            subroutineSymbols = new Dictionary<string, Symbol>();
            classSymbols = new Dictionary<string, Symbol>();
        }

        public void StartSubroutine()
        {
            subroutineSymbols.Clear();
            argumentCount = 0;
        }

        public void StartClass()
        {
            classSymbols.Clear();
            fieldCount = 0;
        }

        public void Define(string name, string type, SymbolKind kind)
        {
            int runningIndex = 0;
            Dictionary<string, Symbol> dictionary = null;
            switch (kind)
            {
                case SymbolKind.Field:
                    runningIndex = fieldCount;
                    fieldCount++;
                    dictionary = classSymbols;
                    break;

                case SymbolKind.Static:
                    runningIndex = classSymbols.Count - fieldCount;
                    dictionary = classSymbols;
                    break;

                case SymbolKind.Variable:
                    runningIndex = subroutineSymbols.Count - argumentCount;
                    dictionary = subroutineSymbols;
                    break;

                case SymbolKind.Argument:
                    runningIndex = argumentCount;
                    argumentCount++;
                    dictionary = subroutineSymbols;
                    break;

                default:
                    return;
            }
            
            dictionary.Add(name, new Symbol
            {
                kind = kind,
                type = type,
                runningIndex = runningIndex,
            });
        }

        public int VarCount(SymbolKind kind)
        {
            Dictionary<string, Symbol> dictionary =
                kind == SymbolKind.Argument || kind == SymbolKind.Variable
                ? subroutineSymbols : classSymbols;

            int count = 0;
            foreach (var pair in dictionary)
            {
                if (pair.Value.kind == kind)
                    count++;
            }
            return count;
        }

        public SymbolKind KindOf(string name)
        {
            Symbol? symbol = GetSymbol(name);
            if (symbol.HasValue)
                return symbol.Value.kind;

            return SymbolKind.None;
        }

        public string TypeOf(string name)
        {
            Symbol? symbol = GetSymbol(name);
            if (symbol.HasValue)
                return symbol.Value.type;

            return null;
        }

        public int? IndexOf(string name)
        {
            Symbol? symbol = GetSymbol(name);
            if (symbol.HasValue)
                return symbol.Value.runningIndex;

            return null;
        }

        Symbol? GetSymbol(string name)
        {
            if (subroutineSymbols.ContainsKey(name))
                return subroutineSymbols[name];

            if (classSymbols.ContainsKey(name))
                return classSymbols[name];

            return null;
        }
    }
}