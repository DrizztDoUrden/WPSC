using System;

namespace WPSC.Lua.Exceptions
{
    public class TokenizerException : Exception
    {
        public TokenizerException(string message) : base(message) { }
    }
}
