using System;

namespace WPSC.Lua.Instructions
{
    public class InPlaceSet : IInPlaceInitializationElement
    {
        public string? Name { get; }
        public IRValue Value { get; }

        public InPlaceSet(string? name, IRValue value)
        {
            Name = name;
            Value = value;
        }
    }
}
