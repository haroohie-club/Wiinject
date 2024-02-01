using System.Collections.Generic;
using System.Globalization;
using Wiinject.Interfaces;

namespace Wiinject
{
    public class DolphinSymbolsMap
    {
        public static List<ExistingFunction> ParseDolphinSymbolsMap(IEnumerable<string> lines)
        {
            List<ExistingFunction> functions = [];

            foreach (string line in lines)
            {
                string[] components = line.Split(' ');
                if (components.Length < 5)
                {
                    continue;
                }
                functions.Add(new() { Name = components[4], EntryPoint = uint.Parse(components[0], NumberStyles.HexNumber) });
            }

            return functions;
        }
    }

    public class ExistingFunction : IFunction
    {
        public string Name { get; set; } = string.Empty;
        public uint EntryPoint { get; set; }
        public bool Existing => true;
    }
}
