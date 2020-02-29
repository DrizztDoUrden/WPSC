using System;
using System.Collections.Generic;
using System.Text;

namespace WPSC.Lua.Instructions
{
    public class FunctionDeclaration : IInstruction
    {
        public bool IsLocal { get; }
        public Function Function { get; }

        public FunctionDeclaration(Function func, bool isLocal)
        {
            Function = func;
            IsLocal = isLocal;
        }
    }
}
