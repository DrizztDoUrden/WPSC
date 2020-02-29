namespace WPSC.Lua.Instructions
{
    public class Set : IInstruction
    {
        public bool IsDeclaringLocal { get; }
        public ILValue[] Lefts { get; }
        public IRValue[] Rights { get; }

        public Set(ILValue[] lefts, IRValue[] rights, bool isDeclaringLocal)
        {
            Lefts = lefts;
            Rights = rights;
            IsDeclaringLocal = isDeclaringLocal;
        }
    }
}
