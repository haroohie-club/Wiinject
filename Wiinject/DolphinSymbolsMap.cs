using System.Collections.Generic;
using System.Globalization;

namespace Wiinject
{
    public class DolphinSymbolsMap
    {
        public static List<CFunction> ParseDolphinSymbolsMap(IEnumerable<string> lines)
        {
            List<CFunction> functions = new();

            foreach (string line in lines)
            {
                string[] components = line.Split(' ');
                if (components.Length < 5)
                {
                    continue;
                }
                functions.Add(new(components[4], uint.Parse(components[0], NumberStyles.HexNumber)));
            }

            return functions;
        }
    }
}
