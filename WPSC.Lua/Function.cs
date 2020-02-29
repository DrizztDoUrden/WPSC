using System;
using System.Collections.Generic;
using WPSC.Lua.Tokens;

namespace WPSC.Lua
{
    public class Function
    {
        public IdentifierToken? Name { get; set; }
        public List<Token<string>> Parameters { get; } = new List<Token<string>>();
        public List<IInstruction> Body { get; } = new List<IInstruction>();
    }
}
