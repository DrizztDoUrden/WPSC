using System.Collections.Generic;
using System.Linq;
using WPSC.Lua.Tokens;

namespace WPSC.Lua.Data
{
    public class Lua5_3 : ILuaData
    {
        public static Lua5_3 Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new Lua5_3();
                    return _instance;
                }
            }
        }

        public IReadOnlyDictionary<string, KeywordToken> Keywords { get; } = new[]
        {
            "and",
            "break",
            "do",
            "else",
            "elseif",
            "end",
            "false",
            "for",
            "function",
            "goto",
            "if",
            "in",
            "local",
            "nil",
            "not",
            "or",
            "repeat",
            "return",
            "then",
            "true",
            "until",
            "while",
        }.ToDictionary(k => k, k => new KeywordToken(k));

        public IReadOnlyDictionary<string, OperatorToken> Operators { get; } = new[]
        {
            "...",

            "..",
            "::",
            "<<",
            ">>",
            "//",
            "==",
            "~=",
            "<=",
            ">=",

            "+",
            "-",
            "*",
            "/",
            "%",
            "^",
            "#",
            "&",
            "~",
            "|",
            ">",
            "<",
            "=",
            "(",
            ")",
            "{",
            "}",
            "[",
            "]",
            ":",
            ";",
            ",",
            ".",
        }.OrderByDescending(op => op.Length).ToDictionary(op => op, op => new OperatorToken(op));

        private static Lua5_3? _instance = null;
        private static readonly object _lock = new object();

        private Lua5_3() { }
    }
}
