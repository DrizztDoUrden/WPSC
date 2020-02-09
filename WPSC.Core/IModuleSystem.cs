using System.IO;

namespace WPSC
{
    public interface IModuleSystem
    {
        public void IncludeModuleLibrary(TextWriter writer);
        public void ModuleDefinitionStart(TextWriter writer, string fileName);
        public void ModuleDefinitionEnd(TextWriter writer, string fileName);
    }
}
