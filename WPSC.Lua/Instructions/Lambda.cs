namespace WPSC.Lua.Instructions
{
    public class Lambda : IRValue
    {
        public Function Function { get; }

        public Lambda(Function func) => Function = func;
    }
}
