using System;

namespace WPSC.Lua.Exceptions
{
    public class SyntaxerException : Exception
    {
        public SyntaxerException(string message) : base(message) { }
    }
}
