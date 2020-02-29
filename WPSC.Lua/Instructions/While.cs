namespace WPSC.Lua.Instructions
{
    public class While
    {
        public IRValue Condition { get; }
        public bool IsPrefix { get; }
        public IInstruction[] Instructions { get; }

        public While(IRValue condition, bool isPrefix, IInstruction[] instructions)
        {
            Condition = condition;
            IsPrefix = isPrefix;
            Instructions = instructions;
        }
    }
}
