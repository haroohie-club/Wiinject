using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Wiinject
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = "";
            uint injectionAddress = 0;
            uint maxInjectionLength = 0;

            OptionSet options = new()
            {
                { "f|folder=", f => folder = f },
                { "i|injection-address=", i => injectionAddress = uint.Parse(i, System.Globalization.NumberStyles.HexNumber) },
                { "e|end-injection=", l => maxInjectionLength = uint.Parse(l, System.Globalization.NumberStyles.HexNumber) - injectionAddress },
            };

            options.Parse(args);

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Riivolution riivolution = new();

            string[] asmFiles = Directory.GetFiles(folder, "*.s", SearchOption.AllDirectories);
            Regex hookRegex = new(@"hook_(?<address>[A-F\d]{8}):");
            List<Routine> routines = new();
            List<byte> routineMashup = new();

            foreach (string asmFile in asmFiles)
            {
                string asmFileText = File.ReadAllText(asmFile);
                string[] assemblyRoutines = hookRegex.Split(asmFileText);

                for (int i = 1; i < assemblyRoutines.Length; i += 2)
                {
                    routines.Add(new Routine(uint.Parse(assemblyRoutines[i], System.Globalization.NumberStyles.HexNumber), assemblyRoutines[i + 1]));
                }
            }

            foreach (Routine routine in routines)
            {
                uint branchLocation = injectionAddress + (uint)routineMashup.Count;
                routine.SetBranchInstruction(branchLocation);
                routineMashup.AddRange(routine.Data);

                riivolution.AddMemoryPatch(routine.InsertionPoint, routine.BranchInstruction);
            }

            if (routineMashup.Count > maxInjectionLength)
            {
                Console.WriteLine($"Error: instruction set length {routineMashup.Count} exceeded max injection length of {maxInjectionLength}.");
                return;
            }

            File.WriteAllBytes("patch.bin", routineMashup.ToArray());
            riivolution.AddMemoryFilesPatch(injectionAddress, "/Heiretsu/patch.bin");

            riivolution.PatchXml.Save("R44J8P.xml");
        }
    }
}
