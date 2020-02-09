using System;
using System.Collections.Generic;
using System.Linq;

namespace WPSC
{
    public class Argument
    {
        public string Id { get; set; } = "<unknown>";
        public string[] Names { get; set; } = Array.Empty<string>();
        public Func<string, bool> ValueValidator { get; set; } = s => true;
        public uint MaxValues = 1;
    }

    public struct ArgsParseResult
    {
        public Dictionary<string, List<string>> Parsed { get; set; }
        public string[] Unparsed { get; set; }
    }

    public class Args
    {
        public Argument[] Arguments { get; set; } = Array.Empty<Argument>();

        public ArgsParseResult Parse(string[] args)
        {
            var ret = new ArgsParseResult
            {
                Parsed = new Dictionary<string, List<string>>()
            };
            var unparsed = new List<string>();
            var lowered = args.Select(s => s.ToLower()).ToArray();

            for (var argId = 0; argId < args.Length; ++argId)
            {
                Argument? def = Arguments.FirstOrDefault(d => d.Names.Contains(lowered[argId]));
                if (def == null)
                {
                    unparsed.Add(args[argId]);
                    continue;
                }
                var values = new List<string>();
                ret.Parsed.Add(def.Id, values);
                ++argId;
                while (argId < args.Length
                    && (values.Count <= def.MaxValues)
                    && (def.ValueValidator(args[argId]))
                    && !Arguments.Any(d => d.Names.Contains(lowered[argId])))
                    values.Add(args[argId++]);
                --argId;
            }

            ret.Unparsed = unparsed.ToArray();
            return ret;
        }
    }
}
