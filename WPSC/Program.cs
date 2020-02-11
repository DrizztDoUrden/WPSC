using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WPSC.Module;
using WPSC.WcData;

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
            var moduleSystem = LoadModuleSystem(options);

            var map = new WCMap(options.TargetDirectory);
            var rootName = "WPSC_GENERATED";
            map.FindCategory(rootName)?.Remove();

            Category? rootCached = null;
            Category root() => rootCached ?? (rootCached = map.CreateCategory(rootName));

            if (moduleSystem != null)
            {
                using var writer = new StringWriter();
                moduleSystem.IncludeModuleLibrary(writer);
                var moduleScript = root().CreateScript("Module System");
                moduleScript.Source = writer.ToString();
            }

            var files = Directory
                .GetFiles(options.SourceDirectory, "*.lua", SearchOption.AllDirectories)
                .Where(f => !options.Excludes.Contains(Path.GetRelativePath(options.SourceDirectory, f)));

            Category? modules = null;
            ProcessDir(options, options.SourceDirectory, moduleSystem, () => modules ?? (modules = root().CreateCategory("Modules")) );

            map.Save(options.TargetDirectory);
        }

        static void ProcessDir(Options options, string dir, IModuleSystem? moduleSystem, Func<Category> category)
        {
            foreach (var file in Directory.GetFiles(dir, "*.lua").Where(f => Path.GetFileName(f)[0] != '.' && !options.Excludes.Contains(Path.GetRelativePath(options.SourceDirectory, f))))
                ProcessFile(options, file, moduleSystem, category());

            foreach (var subDir in Directory.GetDirectories(dir).Where(f => Path.GetFileName(f)[0] != '.' && !options.Excludes.Contains(Path.GetRelativePath(options.SourceDirectory, f))))
            {
                Category? subCategory = null;
                ProcessDir(options, subDir, moduleSystem, () => subCategory ?? (subCategory = category().CreateCategory(Path.GetFileName(subDir))));
            }
        }

        static void ProcessFile(Options options, string file, IModuleSystem? moduleSystem, Category category)
        {
            using var writer = new StringWriter();
            var relative = Path.GetRelativePath(options.SourceDirectory, file);
            writer.WriteLine($"-- Start of file {relative}");
            if (moduleSystem != null)
                moduleSystem.ModuleDefinitionStart(options.SourceDirectory, writer, file);
            writer.WriteLine(File.ReadAllText(file));
            if (moduleSystem != null)
                moduleSystem.ModuleDefinitionEnd(options.SourceDirectory, writer, file);
            writer.Write($"-- End of file {relative}");
            category.CreateScript(Path.GetFileName(file)[0..^4]).Source = writer.ToString();
        }

        static IModuleSystem? LoadModuleSystem(Options options)
        {
            switch (options.ModuleSystem)
            {
                case "wpsc":
                    return new ModuleSystem();
                case "":
                    return null;
                default:
                    var path = Path.Combine(options.SourceDirectory, options.ModuleSystem);
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path)
                            .GetExportedTypes()
                            .FirstOrDefault(t => t.GetInterfaces().Any(i => i == typeof(IModuleSystem)))
                            ?.GetConstructor(Array.Empty<Type>())
                            ?.Invoke(null) as IModuleSystem
                            ?? throw new Exception($"Can't load {path} specified as module system.");
                    }

                    path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, options.ModuleSystem);
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(options.ModuleSystem)
                            .GetExportedTypes()
                            .FirstOrDefault(t => t.GetInterfaces().Any(i => i == typeof(IModuleSystem)))
                            ?.GetConstructor(Array.Empty<Type>())
                            ?.Invoke(null) as IModuleSystem
                            ?? throw new Exception($"Can't load {path} specified as module system.");
                    }

                    throw new Exception($"Can't find module system {options.ModuleSystem}.");
            }
        }
    }
}
