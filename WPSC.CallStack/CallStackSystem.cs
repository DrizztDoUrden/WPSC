using System;
using System.IO;
using System.Reflection;
using System.Text;
using WPSC.Core;

namespace WPSC.CallStack
{
    public class CallStackSystem : ICallStackSystem
    {
        public void IncludeLibrary(TextWriter target)

            => target.WriteLine(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CallStack.lua")));

        public void ProcessFile(string fileName, TextReader source, TextWriter target)
        {
            target.WriteLine($"CallStack:Register(\"file root\", \"{EncodeToLuaString(fileName)}\", 0)");
            ProcessBlock(fileName, source, target);
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

        private static (string?, int) ProcessBlock(string fileName, TextReader source, TextWriter target, (string? line, int lineNumber)? state = null)
        {
            var line = state?.line ?? source.ReadLine();
            var lineNumber = state?.lineNumber ?? 1;
            var isRecurring = state != null;
            var searchStart = 0;

            while (line != null)
            {
                const string funcKw = "function";
                const string thenKw = "then";
                const string doKw = "do";
                const string endKw = "end";
                int kwStart, afterKw = -1;

                do
                {
                    while (searchStart >= line.Length)
                    {
                        ++lineNumber;
                        target.WriteLine(line);
                        line = source.ReadLine();
                        searchStart = 0;

                        if (line == null)
                            return (null, lineNumber);
                    }

                    kwStart = line.IndexOf(funcKw, searchStart);
                    var thenStart = line.IndexOf(thenKw, searchStart);
                    var doStart = line.IndexOf(doKw, searchStart);
                    var endStart = line.IndexOf(endKw, searchStart);
                    searchStart = 0;

                    if (endStart != -1 && CheckKWBorders(line, endStart, endStart + endKw.Length) &&
                        (kwStart == -1 || endStart < kwStart) &&
                        (doStart == -1 || endStart < doStart) &&
                        (thenStart == -1 || endStart < thenStart))
                    {
                        if (!isRecurring)
                            throw new Exception($"Unexpected end at {fileName}:{line}");
                        return (line, lineNumber);
                    }

                    if (thenStart != -1 && CheckKWBorders(line, thenStart, thenStart + thenKw.Length) &&
                        (doStart == -1 || thenStart < doStart) &&
                        (kwStart == -1 || thenStart < kwStart))
                    {
                        var thenAfter = thenStart + thenKw.Length;
                        target.Write(line[..thenAfter]);
                        // we can add registering of every block for debug purposes
                        line = line[thenAfter..];
                        (line, lineNumber) = ProcessBlock(fileName, source, target, (line, lineNumber));
                        if (line == null)
                            throw new Exception($"Unexpected EOF at {fileName}:{lineNumber}");
                        endStart = line.IndexOf(endKw);
                        var afterEnd = endStart + endKw.Length;
                        searchStart = afterEnd;
                        continue;
                    }

                    if (doStart != -1 && CheckKWBorders(line, doStart, doStart + doKw.Length) &&
                        (kwStart == -1 || doStart < kwStart))
                    {
                        var doAfter = doStart + doKw.Length;
                        target.Write(line[..doAfter]);
                        // we can add registering of every block for debug purposes
                        line = line[doAfter..];
                        (line, lineNumber) = ProcessBlock(fileName, source, target, (line, lineNumber));
                        if (line == null)
                            throw new Exception($"Unexpected EOF at {fileName}:{lineNumber}");
                        endStart = line.IndexOf(endKw);
                        var afterEnd = endStart + endKw.Length;
                        searchStart = afterEnd;
                        continue;
                    }

                    if (kwStart == -1)
                    {
                        ++lineNumber;
                        target.WriteLine(line);
                        line = source.ReadLine();
                        continue;
                    }

                    afterKw = kwStart + funcKw.Length;
                }
                while (kwStart == -1 || !CheckKWBorders(line, kwStart, afterKw));

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
                (line, lineNumber) = ProcessBlock(fileName, source, target, (line, lineNumber));
                if (line == null)
                    throw new Exception($"Unexpected EOF at {fileName}:{lineNumber}");

                {
                    var endStart = line.IndexOf(endKw);
                    var afterEnd = endStart + endKw.Length;
                    searchStart = afterEnd;
                }
            }

            return (null, lineNumber);
        }
    }
}
