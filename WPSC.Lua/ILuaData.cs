using System.Collections.Generic;
using WPSC.Lua.Tokens;

namespace WPSC.Lua
{
    public interface ILuaData
    {
        public IReadOnlyDictionary<string, KeywordToken> Keywords { get; }
        public IReadOnlyDictionary<string, OperatorToken> Operators { get; }
    }
}
