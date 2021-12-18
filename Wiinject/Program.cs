﻿using Mono.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Wiinject
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = "", outputFolder = ".", patchName = "patch", inputPatch = "";
            uint[] injectionAddresses = new uint[0], injectionEndAddresses = new uint[0];
            bool consoleOutput = false;

            OptionSet options = new()
            {
                { "f|folder=", "The folder where your .s ASM files live", f => folder = f },
                { "i|injection-addresses=", "The addresses to inject function code at, comma delimited. The code at these addresses should be safe to overwrite.",
                    i => injectionAddresses = i.Split(',').Select(a => uint.Parse(a, NumberStyles.HexNumber)).ToArray() },
                { "e|injection-ends=",
                    "The addresses at which the above injection sites end (are no longer safe to overwrite), comma delimited. If the code goes past the last address in this list, an error will be thrown.",
                    e => injectionEndAddresses = e.Split(',').Select(a => uint.Parse(a, NumberStyles.HexNumber)).ToArray() },
                { "o|output-folder=", "The folder to output the Riivolution patch.xml & assembled ASM bin file(s) to.", o => outputFolder = o },
                { "n|patch-name=", "The name of the patch to output. The patch will be out put to {output_folder}/Riivolution/{patch_name}.xml and the ASM bin(s) will be output to {output_folder}/{patch_name}/patch{i}.bin.",
                    n => patchName = n },
                { "p|input-patch=", "The base Riivolution patch that will be modified by Wiinject to contain the memory patches. A blank base template will be created if this is not provided.", p => inputPatch = p },
                { "console-output", "Rather than producing an ASM patch, simply output the XML to the console. This will still save the ASM bin, however.", c => consoleOutput = true },
            };

            options.Parse(args);

            if (string.IsNullOrEmpty(folder))
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (injectionAddresses.Length != injectionEndAddresses.Length)
            {
                Console.WriteLine("Error: You must provide the same number of injection addresses and end addresses");
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
            Regex funcRegex = new(@"(?<mode>repl|hook)_(?<address>[A-F\d]{8}):");
            List<Routine> routines = new();
            InjectionSite[] injectionSites = new InjectionSite[injectionAddresses.Length];
            for (int i = 0; i < injectionSites.Length; i++)
            {
                injectionSites[i] = new() { StartAddress = injectionAddresses[i], EndAddress = injectionEndAddresses[i] };
            }

            foreach (string asmFile in asmFiles)
            {
                string asmFileText = File.ReadAllText(asmFile);
                string[] assemblyRoutines = funcRegex.Split(asmFileText);

                for (int i = 1; i < assemblyRoutines.Length; i += 3)
                {
                    routines.Add(new Routine(assemblyRoutines[i], uint.Parse(assemblyRoutines[i + 1], NumberStyles.HexNumber), assemblyRoutines[i + 2]));
                }
            }

            if (injectionSites.Sum(s => s.Length) < 4)
            {
                Console.WriteLine($"Error: Max injection length with provided addresses calculated to be {injectionSites.Sum(s => s.Length)} which is less than one instruction.");
                return;
            }

            foreach (Routine routine in routines)
            {
                if (routine.RoutineMode == Routine.Mode.REPL)
                {
                    riivolution.AddMemoryPatch(routine.InsertionPoint, routine.Data);
                }
                else
                {
                    bool injected = false;
                    foreach (InjectionSite injectionSite in injectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
                    {
                        if (injectionSite.RoutineMashup.Count + routine.Data.Length > injectionSite.Length)
                        {
                            continue;
                        }

                        uint branchLocation = injectionSite.StartAddress + (uint)injectionSite.RoutineMashup.Count;
                        routine.SetBranchInstruction(branchLocation);
                        injectionSite.RoutineMashup.AddRange(routine.Data);
                        injected = true;
                        riivolution.AddMemoryPatch(routine.InsertionPoint, routine.BranchInstruction);
                        break;
                    }
                    if (!injected)
                    {
                        Console.WriteLine($"Error: could not inject routine beggining with {routine.Assembly.TakeWhile(c => c != '\n' && c != '\r')}; routine longer than any available injection site.");
                    }
                }
            }

            Directory.CreateDirectory(Path.Combine(outputFolder, patchName));
            Directory.CreateDirectory(Path.Combine(outputFolder, "Riivolution"));

            InjectionSite[] usedInjectionSites = injectionSites.Where(s => s.RoutineMashup.Count > 0).ToArray();
            for (int i = 0; i < usedInjectionSites.Length; i++)
            {
                File.WriteAllBytes(Path.Combine(outputFolder, patchName, $"patch{i}.bin"), usedInjectionSites[i].RoutineMashup.ToArray());
                riivolution.AddMemoryFilesPatch(usedInjectionSites[i].StartAddress, $"/{patchName}/patch{i}.bin");
            }

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
