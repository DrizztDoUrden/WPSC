namespace WPSC.Lua.Instructions
{
    public class Indexing : IInstruction, IRValue, ILValue
    {
        public IRValue Container { get; }
        public IRValue Index { get; }

        public Indexing(IRValue container, IRValue index)
        {
            Container = container;
            Index = index;
        }
    }
}
