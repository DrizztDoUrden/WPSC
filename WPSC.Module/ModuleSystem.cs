using System;
using System.IO;
using System.Reflection;

namespace WPSC.Module
{
    public class ModuleSystem : IModuleSystem
    {
        public void IncludeModuleLibrary(TextWriter writer)
            => writer.WriteLine(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Module.lua")));

        public void ModuleDefinitionStart(string root, TextWriter writer, string fileName)
        {
            var moduleName = Path.GetRelativePath(root, fileName)[0..^4].Replace(Path.DirectorySeparatorChar, '.');
            writer.WriteLine($"Module(\"{moduleName}\", function()");
        }

        public void ModuleDefinitionEnd(string root, TextWriter writer, string fileName) => writer.WriteLine("end)");
    }
}
