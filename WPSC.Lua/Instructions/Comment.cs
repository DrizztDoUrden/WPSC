namespace WPSC.Lua.Instructions
{
    public class Comment : IInstruction
    {
        public string Text { get; }
        public bool IsMultiLine { get; }

        public Comment(string text, bool isMultiLine)
        {
            Text = text;
            IsMultiLine = isMultiLine;
        }
    }
}
