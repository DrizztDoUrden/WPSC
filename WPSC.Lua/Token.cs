namespace WPSC.Lua
{
    public class Token
    {
        public string FileName { get; set; } = "";
        public FilePosition Start { get; set; }
        public FilePosition End { get; set; }
    }

    public class Token<TValue> : Token
    {
        public TValue Value { get; }

        public Token(TValue value) => Value = value;

        public override string ToString() => Value!.ToString();
    }
}
