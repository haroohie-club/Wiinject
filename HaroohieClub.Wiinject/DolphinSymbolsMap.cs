using System.IO;
using System.Text;

namespace HaroohieClub.Wiinject
{
    public static class DolphinSymbolsMap
    {
        public static void WriteSymbolsMap(string[] lines, string outputFilePath)
        {
            StringBuilder sb = new();
            
            foreach (string line in lines)
            {
                string[] components = line.Split(' ');
                if (components.Length < 5)
                {
                    continue;
                }

                sb.AppendLine($"{components[4]} = 0x{components[0]}");
            }
            
            File.WriteAllText(outputFilePath, sb.ToString());
        }
    }
}
