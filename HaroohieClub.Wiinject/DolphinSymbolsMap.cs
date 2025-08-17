using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace HaroohieClub.Wiinject;

internal static class DolphinSymbolsMap
{
    public static void WriteSymbolsMap(string[] lines, string outputFilePath)
    {
        StringBuilder sb = new();
            
        foreach (string line in lines)
        {
            string[] components = line.Split(' ');
            if (components.Length < 6)
            {
                continue;
            }

            components[5] = Regex.Replace(components[5], @"[@%#!*\[\]\(\)<>{}:'""\|\^`\\?;]", "");
            sb.AppendLine($"{components[5]} = 0x{components[0].ToUpper()};");
        }
            
        File.WriteAllText(outputFilePath, sb.ToString());
    }
}