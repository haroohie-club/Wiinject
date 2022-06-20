using Mono.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Wiinject.Interfaces;

namespace Wiinject
{
    public class Program
    {
        public enum WiinjectReturnCode
        {
            OK,
            ERROR,
        }

        public static int Main(string[] args)
        {
            string folder = string.Empty, outputFolder = ".", patchName = "patch", inputPatch = string.Empty, devkitProPath = "C:\\devkitPro", symbolsMap = string.Empty;
            uint[] injectionAddresses = Array.Empty<uint>(), injectionEndAddresses = Array.Empty<uint>();
            bool consoleOutput = false, emitC = false;

            OptionSet options = new()
            {
                { "f|folder=", "The folder where your source files live", f => folder = f },
                { "m|dolphin-map|map|symbols=", "A Dolphin symbols .map file containing any functions you wish to reference", m => symbolsMap = m },
                { "i|injection-addresses=", "The addresses to inject function code at, comma delimited. The code at these addresses should be safe to overwrite.",
                    i => injectionAddresses = i.Split(',').Select(a => uint.Parse(a, NumberStyles.HexNumber)).ToArray() },
                { "e|injection-ends=",
                    "The addresses at which the above injection sites end (are no longer safe to overwrite), comma delimited. If the code goes past the last address in this list, an error will be thrown.",
                    e => injectionEndAddresses = e.Split(',').Select(a => uint.Parse(a, NumberStyles.HexNumber)).ToArray() },
                { "o|output-folder=", "The folder to output the Riivolution patch.xml & assembled ASM bin file(s) to.", o => outputFolder = o },
                { "n|patch-name=", "The name of the patch to output. The patch will be out put to {output_folder}/Riivolution/{patch_name}.xml and the ASM bin(s) will be output to {output_folder}/{patch_name}/patch{i}.bin.",
                    n => patchName = n },
                { "p|input-patch=", "The base Riivolution patch that will be modified by Wiinject to contain the memory patches. A blank base template will be created if this is not provided.", p => inputPatch = p },
                { "d|devkitpro-path=", "The path to a devkitPro installation containing devkitPPC.", d => devkitProPath = d },
                { "console-output", "Rather than producing an ASM patch, simply output the XML to the console. This will still save the ASM bin, however.", c => consoleOutput = true },
                { "emit-c", "Emits assembled C functions to the console so you can modify your assembly calls to those functions to work with the registries used by the compiler.", c => emitC = true },
            };

            options.Parse(args);

            if (string.IsNullOrEmpty(folder))
            {
                options.WriteOptionDescriptions(Console.Out);
                return (int)WiinjectReturnCode.OK;
            }

            Directory.CreateDirectory(Path.Combine(outputFolder, patchName));
            Directory.CreateDirectory(Path.Combine(outputFolder, "Riivolution"));

            Dictionary<string, (string, string)[]> asmFiles = new();
            foreach (string directory in Directory.GetDirectories(folder))
            {
                asmFiles.Add(Path.GetFileName(directory), Directory.GetFiles(directory, "*.s", SearchOption.AllDirectories).Select(f => (Path.GetFileNameWithoutExtension(f), File.ReadAllText(f))).ToArray());
            }
            (string, string)[] cFiles = Directory.GetFiles(folder, "*.c", SearchOption.AllDirectories).Select(f => (Path.GetFileNameWithoutExtension(f), File.ReadAllText(f))).ToArray();

            string gccExe = "powerpc-eabi-gcc";
            string objdumpExe = "powerpc-eabi-objdump";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                gccExe += ".exe";
                objdumpExe += ".exe";
            }
            string gccPath = Path.Combine(devkitProPath, "devkitPPC", "bin", gccExe);
            string objdumpPath = Path.Combine(devkitProPath, "devkitPPC", "bin", objdumpExe);

            string[] symbolsMapParsed = Array.Empty<string>();
            if (!string.IsNullOrEmpty(symbolsMap))
            {
                symbolsMapParsed = File.ReadAllLines(symbolsMap);
            }

            WiinjectResult result = new();
            try
            {
                result = WiinjectEngine.AssemblePatch(injectionAddresses, injectionEndAddresses, asmFiles, cFiles, inputPatch, symbolsMapParsed, gccPath, objdumpPath, patchName);
            }
            catch (WiinjectException ex)
            {
                Console.WriteLine(ex.Message);
                return (int)WiinjectReturnCode.ERROR;
            }

            foreach (string binPatch in result.OutputBinaryPatches.Keys)
            {
                File.WriteAllBytes(Path.Combine(outputFolder, patchName, binPatch), result.OutputBinaryPatches[binPatch]);
            }

            if (emitC)
            {
                foreach (string cFile in result.EmittedCFiles)
                {
                    Console.WriteLine(cFile);
                }
            }

            if (consoleOutput)
            {
                Console.WriteLine(result.OutputRiivolution.PatchXml.OuterXml);
            }
            else
            {
                string outputPath = Path.Combine(outputFolder, "Riivolution", $"{patchName}.xml");
                result.OutputRiivolution.PatchXml.Save(outputPath);
                Console.WriteLine($"Wrote to {outputPath}");
            }

            return (int)WiinjectReturnCode.OK;
        }
    }
}
