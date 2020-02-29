using WPSC.Lua.Tokens;

namespace WPSC.Lua.Instructions
{
    public class Identifier : IRValue, ILValue
    {
        public IdentifierToken Name { get; }

        public Identifier(IdentifierToken name) => Name = name;
    }
}
