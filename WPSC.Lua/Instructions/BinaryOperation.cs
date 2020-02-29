namespace WPSC.Lua.Instructions
{
    public enum BinaryOperator
    {
        Add,
        Substract,
        Multiply,
        Divide,
        FloorDivide,
        Modulo,
        Power,

        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        ShiftLeft,
        ShiftRight,

        And,
        Or,

        Equals,
        NotEquals,
        LessOrEquals,
        Less,
        GreaterOrEquals,
        Greater,

        Concatenate,
    }

    public class BinaryOperation : IRValue
    {
        BinaryOperator Operator { get; }
        public IRValue Left { get; }
        public IRValue Right { get; }

        public BinaryOperation(BinaryOperator op, IRValue left, IRValue right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }
    }
}
