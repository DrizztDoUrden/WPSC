using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WPSC.CallStack;
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
            var moduleSystem = LoadSystem<IModuleSystem, ModuleSystem>(options, options.ModuleSystem, "module");
            var callStackSystem = LoadSystem<ICallStackSystem, CallStackSystem>(options, options.CallStackSystem, "call stack");

            var map = new WCMap(options.TargetDirectory);
            var rootName = "WPSC_GENERATED";
            map.FindCategory(rootName)?.Remove();

            Category? rootCached = null;
            Category root() => rootCached ?? (rootCached = map.CreateCategory(rootName));

            if (callStackSystem != null)
            {
                using var writer = new StringWriter();
                callStackSystem.IncludeLibrary(writer);
                var callStackScript = root().CreateScript("Call Stack System");
                callStackScript.Source = writer.ToString();
            }

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
            ProcessDir(options, options.SourceDirectory, moduleSystem, callStackSystem, () => modules ?? (modules = root().CreateCategory("Modules")) );

            map.Save(options.TargetDirectory);
        }

        static void ProcessDir(Options options, string dir, IModuleSystem? moduleSystem, ICallStackSystem? csSystem, Func<Category> category)
        {
            foreach (var file in Directory.GetFiles(dir, "*.lua").Where(f => Path.GetFileName(f)[0] != '.' && !options.Excludes.Contains(Path.GetRelativePath(options.SourceDirectory, f))))
                ProcessFile(options, file, moduleSystem, csSystem, category());

            foreach (var subDir in Directory.GetDirectories(dir).Where(f => Path.GetFileName(f)[0] != '.' && !options.Excludes.Contains(Path.GetRelativePath(options.SourceDirectory, f))))
            {
                Category? subCategory = null;
                ProcessDir(options, subDir, moduleSystem, csSystem, () => subCategory ?? (subCategory = category().CreateCategory(Path.GetFileName(subDir))));
            }
        }

        static void ProcessFile(Options options, string file, IModuleSystem? moduleSystem, ICallStackSystem? csSystem, Category category)
        {
            using var writer = new StringWriter();
            var relative = Path.GetRelativePath(options.SourceDirectory, file);
            writer.WriteLine($"-- Start of file {relative}");
            if (moduleSystem != null)
                moduleSystem.ModuleDefinitionStart(options.SourceDirectory, writer, file);
            if (csSystem != null)
                csSystem.ProcessFile(relative, new StreamReader(File.OpenRead(file)), writer);
            else
                writer.WriteLine(File.ReadAllText(file));
            if (moduleSystem != null)
                moduleSystem.ModuleDefinitionEnd(options.SourceDirectory, writer, file);
            writer.Write($"-- End of file {relative}");
            category.CreateScript(Path.GetFileName(file)[0..^4]).Source = writer.ToString();
        }

        static TInterface? LoadSystem<TInterface, TDefault>(Options options, string name, string type)
            where TDefault : class, TInterface, new()
            where TInterface : class
        {
            switch (name)
            {
                case "wpsc":
                    return new TDefault();
                case "":
                case "none":
                    return null;
                default:
                    return TryLoadSystemFrom<TInterface>(Path.Combine(options.SourceDirectory, name))
                        ?? TryLoadSystemFrom<TInterface>(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, name))
                        ?? throw new Exception($"Can't find {type} system {name}.");
            }
        }

        static TInterface? TryLoadSystemFrom<TInterface>(string path)
            where TInterface : class
        {
            if (File.Exists(path))
            {
                return Assembly.LoadFrom(path)
                    .GetExportedTypes()
                    .FirstOrDefault(t => t.GetInterfaces().Any(i => i == typeof(TInterface)))
                    ?.GetConstructor(Array.Empty<Type>())
                    ?.Invoke(null) as TInterface
                    ?? throw new Exception($"Can't load {path} specified as module system.");
            }
            return null;
        }
    }
}
