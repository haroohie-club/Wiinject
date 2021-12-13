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
            string folder = "", outputFolder = ".", patchName = "patch", inputPatch = "";
            uint injectionAddress = 0, maxInjectionLength = 0;
            bool consoleOutput = false;

            OptionSet options = new()
            {
                { "f|folder=", f => folder = f },
                { "i|injection-address=", i => injectionAddress = uint.Parse(i, System.Globalization.NumberStyles.HexNumber) },
                { "e|end-injection=", e => maxInjectionLength = uint.Parse(e, System.Globalization.NumberStyles.HexNumber) - injectionAddress },
                { "o|output-folder=", o => outputFolder = o },
                { "n|patch-name=", n => patchName = n },
                { "p|input-patch=", p => inputPatch = p },
                { "console-output", c => consoleOutput = true },
            };

            options.Parse(args);

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Riivolution riivolution;
            if (!string.IsNullOrEmpty(inputPatch))
            {
                riivolution = new(inputPatch);
            }
            else
            {
                riivolution = new();
            }

            string[] asmFiles = Directory.GetFiles(folder, "*.s", SearchOption.AllDirectories);
            Regex hookRegex = new(@"(?<mode>repl|hook)_(?<address>[A-F\d]{8}):");
            List<Routine> routines = new();
            List<byte> routineMashup = new();

            foreach (string asmFile in asmFiles)
            {
                string asmFileText = File.ReadAllText(asmFile);
                string[] assemblyRoutines = hookRegex.Split(asmFileText);

                for (int i = 1; i < assemblyRoutines.Length; i += 3)
                {
                    routines.Add(new Routine(assemblyRoutines[i], uint.Parse(assemblyRoutines[i + 1], System.Globalization.NumberStyles.HexNumber), assemblyRoutines[i + 2]));
                }
            }

            foreach (Routine routine in routines)
            {
                if (routine.RoutineMode == Routine.Mode.REPL)
                {
                    riivolution.AddMemoryPatch(routine.InsertionPoint, routine.Data);
                }
                else
                {
                    uint branchLocation = injectionAddress + (uint)routineMashup.Count;
                    routine.SetBranchInstruction(branchLocation);
                    routineMashup.AddRange(routine.Data);
                    riivolution.AddMemoryPatch(routine.InsertionPoint, routine.BranchInstruction);
                }
            }

            if (routineMashup.Count > maxInjectionLength)
            {
                Console.WriteLine($"Error: instruction set length {routineMashup.Count} exceeded max injection length of {maxInjectionLength}.");
                return;
            }

            Directory.CreateDirectory(Path.Combine(outputFolder, patchName));
            Directory.CreateDirectory(Path.Combine(outputFolder, "Riivolution"));
            File.WriteAllBytes(Path.Combine(outputFolder, patchName, "patch.bin"), routineMashup.ToArray());
            riivolution.AddMemoryFilesPatch(injectionAddress, $"/{patchName}/patch.bin");

            if (consoleOutput)
            {
                Console.WriteLine(riivolution.PatchXml.OuterXml);
            }
            else
            {
                string outputPath = Path.Combine(outputFolder, "Riivolution", $"{patchName}.xml");
                riivolution.PatchXml.Save(outputPath);
                Console.WriteLine($"Wrote to {outputPath}");
            }
        }
    }
}
