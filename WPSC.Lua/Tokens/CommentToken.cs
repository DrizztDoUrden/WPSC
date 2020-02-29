namespace WPSC.Lua.Tokens
{
    public class CommentToken : Token<string>
    {
        public bool IsMultiLine { get; }

        public CommentToken(string text, bool isMultiLine)
            : base(text)
        {
            IsMultiLine = isMultiLine;
        }

        public override string ToString() => IsMultiLine ? $"--[[{Value}]]" : $"--{Value}";
    }
}
