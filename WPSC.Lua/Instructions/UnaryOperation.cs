namespace WPSC.Lua.Instructions
{
    public enum UnaryOperator
    {
        Not,
        BitwiseNot,
        Brackets,
        UnaryMinus,
        Length,
    }

    public class UnaryOperation : IRValue
    {
        public UnaryOperator Opearator { get; }
        public IRValue Operand { get; }

        public UnaryOperation(UnaryOperator op, IRValue operand)
        {
            Opearator = op;
            Operand = operand;
        }
    }
}
