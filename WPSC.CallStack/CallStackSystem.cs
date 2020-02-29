using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WPSC.Lua;
using WPSC.Lua.Data;

namespace WPSC.CallStack
{
    public class CallStackSystem : ICallStackSystem
    {
        public void IncludeLibrary(TextWriter target)

            => target.WriteLine(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CallStack.lua")));

        public void ProcessFile(string fileName, TextReader source, TextWriter target)
        {
            target.WriteLine($"CallStack:Register(\"file root\", \"{EncodeToLuaString(fileName)}\", 0)");

            var tokenizer = new Tokenizer(Lua5_3.Instance);
            var astBuilder = new ASTBuilder();
            var tokens = tokenizer.Parse(fileName, source);
            astBuilder.Process(tokens).Wait();
            return;

            var line = source.ReadLine();
            var lineNumber = 1;
            ProcessBlock(fileName, source, target, ref line, ref lineNumber);
        }

        private static bool CharCanBeInIdentifier(char c)
            => char.IsLetterOrDigit(c) || c == '_';

        private static bool CheckKWBorders(string line, int start, int after)
            => (start == 0 || !CharCanBeInIdentifier(line[start - 1]))
            && (after == line.Length || !CharCanBeInIdentifier(line[after]));

        private static string EncodeToLuaString(string str)
        {
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static int FindKeyword(string line, string keyword)
        {
            var start = line.IndexOf(keyword);
            while (start != -1 && !CheckKWBorders(line, start, start + keyword.Length))
                start = line.IndexOf(keyword, start + 1);
            return start;
        }

        private static void IgnoreBlock(int start, string keyword, string fileName, TextReader source, TextWriter target, ref string line, ref int lineNumber)
        {
            var after = start + keyword.Length;
            target.Write(line![..after]);
            // we can add registering of every block for debug purposes
            line = line[after..];
            string? mLine = line;
            ProcessBlock(fileName, source, target, ref mLine, ref lineNumber, true);
            line = mLine ?? throw new Exception($"Unexpected EOF at {fileName}:{lineNumber}");
        }

        private static int FindStringBorder(string line, int start, params char[] chars)
        {
            --start;
            while (true)
            {
                start = line.IndexOfAny(chars, start + 1);
                if (start <= 0 || line.HasEvenAmmountOfEscapesBefore(start))
                    return start;
            }
        }

        private static void ProcessBlock(string fileName, TextReader source, TextWriter target, ref string? line, ref int lineNumber, bool isRecurring = false)
        {
            while (line != null)
            {
                const string funcKw = "function";
                const string thenKw = "then";
                const string doKw = "do";
                const string endKw = "end";
                const string elseIfKw = "elseif";
                int kwStart, afterKw = -1;

                do
                {
                    kwStart = FindKeyword(line, funcKw);
                    var thenStart = FindKeyword(line, thenKw);
                    var doStart = FindKeyword(line, doKw);
                    var endStart = FindKeyword(line, endKw);
                    var elseIfStart = FindKeyword(line, elseIfKw);
                    var stringStart = FindStringBorder(line, 0, '\'', '\"');

                    while (stringStart != -1 &&
                        (endStart == -1 || stringStart < endStart) &&
                        (elseIfStart == -1 || stringStart < elseIfStart) &&
                        (kwStart == -1 || stringStart < kwStart) &&
                        (doStart == -1 || stringStart < doStart) &&
                        (thenStart == -1 || stringStart < thenStart))
                    {
                        var border = line[stringStart];
                        ++stringStart;
                        target.Write(line[..stringStart]);
                        var sb = new StringBuilder();

                        while (true)
                        {
                            var stringEnd = FindStringBorder(line, stringStart, border);
                            if (stringEnd != -1)
                            {
                                ++stringEnd;
                                sb.Append(line[stringStart..stringEnd]);
                                target.Write(line[stringStart..stringEnd]);
                                line = line[stringEnd..];
                                break;
                            }
                            sb.AppendLine(line[stringStart..]);
                            target.WriteLine(line[stringStart..]);
                            line = source.ReadLine();
                            ++lineNumber;
                            stringStart = 0;
                        }

                        kwStart = FindKeyword(line, funcKw);
                        thenStart = FindKeyword(line, thenKw);
                        doStart = FindKeyword(line, doKw);
                        endStart = FindKeyword(line, endKw);
                        stringStart = line.IndexOfAny(new[] { '\'', '\"' });
                    }

                    if (endStart != -1 &&
                        (elseIfStart == -1 || endStart < elseIfStart) &&
                        (kwStart == -1 || endStart < kwStart) &&
                        (doStart == -1 || endStart < doStart) &&
                        (thenStart == -1 || endStart < thenStart))
                    {
                        if (!isRecurring)
                            throw new Exception($"Unexpected end at {fileName}:{lineNumber}");
                        var after = endStart + endKw.Length;
                        target.Write(line[..after]);
                        line = line[after..];
                        return;
                    }

                    if (elseIfStart != -1 &&
                        (kwStart == -1 || elseIfStart < kwStart) &&
                        (doStart == -1 || elseIfStart < doStart) &&
                        (thenStart == -1 || elseIfStart < thenStart))
                    {
                        if (!isRecurring)
                            throw new Exception($"Unexpected elseif at {fileName}:{lineNumber}");
                        var after = elseIfStart + elseIfKw.Length;
                        target.Write(line[..after]);
                        line = line[after..];
                        return;
                    }

                    if (thenStart != -1 &&
                        (doStart == -1 || thenStart < doStart) &&
                        (kwStart == -1 || thenStart < kwStart))
                    {
                        IgnoreBlock(thenStart, thenKw, fileName, source, target, ref line, ref lineNumber);
                        continue;
                    }

                    if (doStart != -1 &&
                        (kwStart == -1 || doStart < kwStart))
                    {
                        IgnoreBlock(doStart, doKw, fileName, source, target, ref line, ref lineNumber);
                        continue;
                    }

                    if (kwStart == -1)
                    {
                        ++lineNumber;
                        target.WriteLine(line);
                        line = source.ReadLine();
                        if (line == null)
                            return;
                        continue;
                    }

                    afterKw = kwStart + funcKw.Length;
                }
                while (kwStart == -1);

                var paramsStart = line.IndexOf('(', afterKw);
                var nameSb = new StringBuilder();
                var lastNameStart = afterKw;

                while (paramsStart == -1)
                {
                    nameSb.Append(line[lastNameStart..]);
                    nameSb.Append(' ');
                    lastNameStart = 0;
                    ++lineNumber;
                    target.WriteLine(line);
                    line = source.ReadLine();
                    if (line == null)
                        throw new Exception($"Unexpected EOF at {fileName}:{lineNumber}");
                    paramsStart = line.IndexOf('(');
                }

                nameSb.Append(line[lastNameStart..paramsStart].Trim());
                var name = nameSb.ToString().Trim();
                if (name.Length == 0) name = "lambda";
                var paramsEnd = line.IndexOf(')', paramsStart + 1);

                while (paramsEnd == -1)
                {
                    ++lineNumber;
                    target.WriteLine(line);
                    line = source.ReadLine();
                    if (line == null)
                        throw new Exception($"Unexpected EOF at {fileName}:{lineNumber}");
                    paramsEnd = line.IndexOf(')');
                }

                target.Write(line[..(paramsEnd + 1)]);
                target.Write($"CallStack:Register(\"{EncodeToLuaString(name)}\", \"{EncodeToLuaString(fileName)}\", {lineNumber})");
                line = line[(paramsEnd + 1)..];
                ProcessBlock(fileName, source, target, ref line, ref lineNumber, true);
            }
        }
    }
}
