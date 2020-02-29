namespace WPSC.Lua.Tokens
{
    public class StringToken : Token<string>
    {
        public char Border { get; }

        public StringToken(char border, string value)
            : base(value)
        {
            Border = border;
        }

        public override string ToString() => $"{Border}{Value}{Border}";
    }
}
