namespace WPSC.Lua.Instructions
{
    public class For : IInstruction
    {
        public ILValue Variable { get; }
        public IRValue Start { get; }
        public IRValue Limit { get; }
        public IRValue Step { get; }
        public IInstruction[] Instructions { get; }

        public For(ILValue variable, IRValue start, IRValue limit, IRValue step, IInstruction[] instructions)
        {
            Variable = variable;
            Start = start;
            Limit = limit;
            Step = step;
            Instructions = instructions;
        }
    }
}
