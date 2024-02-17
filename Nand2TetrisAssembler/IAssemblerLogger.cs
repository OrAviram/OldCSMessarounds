namespace Nand2TetrisAssembler
{
    interface IAssemblerLogger
    {
        void LogError(string message, int lineNumber, CommandType commandType);
    }
}