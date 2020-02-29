namespace WPSC.Lua.Instructions
{
    public class NumberConstant : IRValue
    {
        public double Value { get; }

        public NumberConstant(double value) => Value = value;
    }
}
