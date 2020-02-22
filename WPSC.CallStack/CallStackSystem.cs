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
            target.Write($"CallStack:Register(\"file root\", \"{EncodeToLuaString(fileName)}\", 0)");
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
                const string keyword = "function";
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

                    kwStart = line.IndexOf(keyword, searchStart);
                    var endStart = line.IndexOf(endKw, searchStart);
                    searchStart = 0;

                    if (endStart != -1 && CheckKWBorders(line, endStart, endStart + endKw.Length) &&
                        (kwStart == -1 || endStart < kwStart))
                    {
                        if (!isRecurring)
                            throw new Exception($"Unexpected end at {fileName}:{line}");
                        return (line, lineNumber);
                    }

                    if (kwStart == -1)
                    {
                        ++lineNumber;
                        target.WriteLine(line);
                        line = source.ReadLine();
                        continue;
                    }

                    afterKw = kwStart + keyword.Length;
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
