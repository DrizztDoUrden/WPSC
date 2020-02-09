using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WPSC.Module;

namespace WPSC
{
    class Program
    {
        static Args Args = new Args
        {
            Arguments = new []
            {
                new Argument
                {
                    Id = "Source",
                    Names = new [] { "-source", },
                },
                new Argument
                {
                    Id = "Target",
                    Names = new [] { "-target", },
                },
                new Argument
                {
                    Id = "Exclude",
                    Names = new [] { "-exclude", },
                    MaxValues = uint.MaxValue,
                },
                new Argument
                {
                    Id = "ModuleSystem",
                    Names = new [] { "-module", },
                },
            },
        };

        static void Main(string[] args)
        {
            var parsed = Args.Parse(args);
            var options = new Options(parsed);

            if (options.HasErrors)
                return;

            Process(options);
        }

        static void Process(Options options)
        {
            using var output = File.OpenWrite(options.TargetDirectory + "/built.lua");
            using var writer = new StreamWriter(output);
            var moduleSystem = LoadModuleSystem(options);

            if (moduleSystem != null)
                moduleSystem.IncludeModuleLibrary(writer);

            var files = Directory
                .GetFiles(options.SourceDirectory, "*.lua", SearchOption.AllDirectories)
                .Where(f => !options.Excludes.Contains(Path.GetRelativePath(options.SourceDirectory, f)));

            foreach (var file in files)
            {
                writer.WriteLine($"-- Start of file {file}");
                if (moduleSystem != null)
                    moduleSystem.ModuleDefinitionStart(writer, file);
                writer.WriteLine(File.ReadAllText(file));
                if (moduleSystem != null)
                    moduleSystem.ModuleDefinitionEnd(writer, file);
                writer.WriteLine($"-- End of file {file}");
            }
        }

        static IModuleSystem? LoadModuleSystem(Options options)
        {
            switch (options.ModuleSystem)
            {
                case "wcps":
                    return new ModuleSystem();
                case "":
                    return null;
                default:
                    var path = options.SourceDirectory + "/" + options.ModuleSystem;
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path)
                            .GetExportedTypes()
                            .FirstOrDefault(t => t.GetInterfaces().Any(i => i == typeof(IModuleSystem)))
                            ?.GetConstructor(Array.Empty<Type>())
                            ?.Invoke(null) as IModuleSystem;
                    }
                    else
                    {
                        path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + options.ModuleSystem;
                        if (File.Exists(path))
                        {
                            return Assembly.LoadFrom(options.ModuleSystem)
                                .GetExportedTypes()
                                .FirstOrDefault(t => t.GetInterfaces().Any(i => i == typeof(IModuleSystem)))
                                ?.GetConstructor(Array.Empty<Type>())
                                ?.Invoke(null) as IModuleSystem;
                        }
                        else
                        {

                        }
                    }
                    break;
            }
        }
    }
}
