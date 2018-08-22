using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IKoshelev.ProjectFocuser
{
    public static class SlnFileParser
    {
        public static Dictionary<string, string> GetProjectNamesToGuidsDict(string slnPath)
        {
            var text = File.ReadAllText(slnPath);

            //Raw regex: /Project\(\"\{([0-9ABCDEF-]*)\}\"\) = \"([^"]*)\", \"([^"]*)\", \"\{([0-9ABCDEF-]*)\}\"/g
            var regex = new Regex(@"Project\(\""\{([0-9ABCDEF-]*)\}\""\) = \""([^""]*)\"", \""([^""]*)\"", \""\{([0-9ABCDEF-]*)\}\""");

            var matches = regex.Matches(text);

            var dict = matches.Cast<Match>()
                                .Where(x => x.Groups[3].Value.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                                .ToDictionary(
                                        x => x.Groups[2].Value,
                                        x => x.Groups[4].Value);

            return dict;
        }
    }
}
