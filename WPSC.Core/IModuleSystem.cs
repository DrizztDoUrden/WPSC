using System.IO;

namespace WPSC
{
    public interface IModuleSystem
    {
        public void IncludeModuleLibrary(TextWriter writer);
        public void ModuleDefinitionStart(string root, TextWriter writer, string fileName);
        public void ModuleDefinitionEnd(string root, TextWriter writer, string fileName);
    }
}
