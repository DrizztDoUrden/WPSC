namespace WPSC.Lua.Instructions
{
    public struct IfBlock
    {
        public IRValue Condition { get; }
        public IInstruction[] Instructions { get; }
    }

    public class If : IInstruction
    {
        public IfBlock Main { get; }
        public IfBlock[] ElseIfs { get; }
        public IInstruction[] Else { get; }

        public If(IfBlock main, IfBlock[] elseIfs, IInstruction[] @else)
        {
            Main = main;
            ElseIfs = elseIfs;
            Else = @else;
        }
    }
}
