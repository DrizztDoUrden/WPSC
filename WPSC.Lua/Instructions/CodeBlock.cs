namespace WPSC.Lua.Instructions
{
    public class CodeBlock : IInstruction
    {
        public IInstruction[] Instructions { get; }

        public CodeBlock(IInstruction[] instructions) => Instructions = instructions;
    }
}
