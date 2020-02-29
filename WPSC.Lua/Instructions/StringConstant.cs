namespace WPSC.Lua.Instructions
{
    public enum StringType
    {
        Quotes,
        SingleQuotes,
    }

    public class StringConstant : IRValue
    {
        public string Value { get; }
        public StringType Type { get; }

        public StringConstant(string value, StringType type)
        {
            Value = value;
            Type = type;
        }
    }
}
