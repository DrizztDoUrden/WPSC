namespace WPSC.Lua.Instructions
{
    public class Member : IRValue, ILValue
    {
        public IRValue Container { get; }
        public string Name { get; }
        public bool IsMethod { get; }

        public Member(IRValue container, string name, bool isMethod)
        {
            Container = container;
            Name = name;
            IsMethod = isMethod;
        }
    }
}
