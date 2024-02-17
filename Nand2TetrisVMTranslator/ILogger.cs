namespace Nand2TetrisVMTranslator
{
    interface ILogger
    {
        void LogTranslationError(string message, string fileName, int lineNumber, CommandType commandType);
        void LogInputError(string message);
    }
}