namespace WPSC.Lua.Instructions
{
    public class ForIn : IInstruction
    {
        public ILValue[] Variables { get; }
        public IRValue[] Container { get; }
        public IInstruction[] Instructions { get; }

        public ForIn(ILValue[] variables, IRValue[] container, IInstruction[] instructions)
        {
            Variables = variables;
            Container = container;
            Instructions = instructions;
        }
    }
}
