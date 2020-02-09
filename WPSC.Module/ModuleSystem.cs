using System;
using System.IO;

namespace WPSC.Module
{
    public class ModuleSystem : IModuleSystem
    {
        public void IncludeModuleLibrary(TextWriter writer) => throw new NotImplementedException();
        public void ModuleDefinitionEnd(TextWriter writer, string fileName) => throw new NotImplementedException();
        public void ModuleDefinitionStart(TextWriter writer, string fileName) => throw new NotImplementedException();
    }
}
