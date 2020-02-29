using System;
using System.Collections.Generic;
using System.Text;

namespace WPSC.Lua.Instructions
{
    public class InPlaceInitialization : IRValue
    {
        public IInPlaceInitializationElement[] Elements { get; }

        public InPlaceInitialization(IInPlaceInitializationElement[] elements) => Elements = elements;
    }
}
