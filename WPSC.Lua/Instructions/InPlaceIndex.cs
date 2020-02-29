using System;

namespace WPSC.Lua.Instructions
{
    public class InPlaceIndex : IInPlaceInitializationElement
    {
        public IRValue Index { get; }
        public IRValue Value { get; }

        public InPlaceIndex(IRValue index, IRValue value)
        {
            Index = index;
            Value = value;
        }
    }
}
