using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WPSC
{
    class Options
    {
        public bool HasErrors { get; set; } = false;
        public string SourceDirectory { get; set; } = "";
        public string TargetDirectory { get; set; } = "";
        public string ModuleSystem { get; set; } = "wpsc";
        public string[] Excludes { get; set; } = Array.Empty<string>();

        public Options(in ArgsParseResult parseResult)
        {
            var hasErrors = false;
            if (parseResult.Parsed.TryGetValue("Source", out var sourceDir) && sourceDir.Count == 1)
                SourceDirectory = sourceDir[0];
            if (!parseResult.Parsed.TryGetValue("Target", out var targetDir) || targetDir.Count != 1)
                LogError("-target", out hasErrors);
            if (parseResult.Parsed.TryGetValue("Exclude", out var exclude) && exclude.Count > 0)
                Excludes = exclude.ToArray();
            if (parseResult.Parsed.TryGetValue("Module", out var module))
                ModuleSystem = module.Count == 0 ? "" : module[0];
            HasErrors = hasErrors;
            if (hasErrors)
                return;
            TargetDirectory = targetDir![0];
        }

        static void LogError(string optionName, out bool hasErrors)
        {
            Console.Error.WriteLine($"{optionName} is missing.");
            hasErrors = true;
        }

        public string ResolveSubPath(string path)
        {
            if (string.IsNullOrEmpty(_cachedSourceRoot))
                _cachedSourceRoot = ProduceCachedSourceRoot();
            return _cachedSourceRoot + path;
        }

        private string _cachedSourceRoot = "";

        private string ProduceCachedSourceRoot() => Path.GetFullPath(SourceDirectory) + "/";
    }
}
