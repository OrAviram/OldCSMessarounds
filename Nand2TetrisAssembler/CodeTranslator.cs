namespace Nand2TetrisAssembler
{
    static class CodeTranslator
    {
        public static bool TranslateJumpCondition(string text, out char j1, out char j2, out char j3)
        {
            if (text == "JGT") { j1 = '0'; j2 = '0'; j3 = '1'; }
            else if (text == "JEQ") { j1 = '0'; j2 = '1'; j3 = '0'; }
            else if (text == "JGE") { j1 = '0'; j2 = '1'; j3 = '1'; }
            else if (text == "JLT") { j1 = '1'; j2 = '0'; j3 = '0'; }
            else if (text == "JNE") { j1 = '1'; j2 = '0'; j3 = '1'; }
            else if (text == "JLE") { j1 = '1'; j2 = '1'; j3 = '0'; }
            else if (text == "JMP") { j1 = '1'; j2 = '1'; j3 = '1'; }
            else if (text == "") { j1 = '0'; j2 = '0'; j3 = '0'; }
            else { j1 = '0'; j2 = '0'; j3 = '0'; return false; }

            return true;
        }

        public static bool TranslateDestination(string text, out char d1, out char d2, out char d3)
        {
            if (text == "M") { d1 = '0'; d2 = '0'; d3 = '1'; }
            else if (text == "D") { d1 = '0'; d2 = '1'; d3 = '0'; }
            else if (text == "MD") { d1 = '0'; d2 = '1'; d3 = '1'; }
            else if (text == "A") { d1 = '1'; d2 = '0'; d3 = '0'; }
            else if (text == "AM") { d1 = '1'; d2 = '0'; d3 = '1'; }
            else if (text == "AD") { d1 = '1'; d2 = '1'; d3 = '0'; }
            else if (text == "AMD") { d1 = '1'; d2 = '1'; d3 = '1'; }
            else if (text == "") { d1 = '0'; d2 = '0'; d3 = '0'; }
            else { d1 = '0'; d2 = '0'; d3 = '0'; return false; }

            return true;
        }

        public static bool TranslateComputation(string text, out char a, out char c1, out char c2, out char c3, out char c4, out char c5, out char c6)
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
