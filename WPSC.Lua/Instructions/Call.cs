namespace WPSC.Lua.Instructions
{
    public class Call : IInstruction, IRValue
    {
        public IRValue Callee { get; }
        public IRValue[] Operands { get; }

        public Call(IRValue callee, IRValue[] operands)
        {
            Callee = callee;
            Operands = operands;
        }
    }
}
