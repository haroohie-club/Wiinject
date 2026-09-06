using System;
using System.Globalization;
using System.IO;
using Mono.Options;

namespace HaroohieClub.Wiinject.Cli;

public class Program
{
    public enum WiinjectReturnCode
    {
        Ok,
        Error,
    }

    public static int Main(string[] args)
    {
        string folder = string.Empty, outputFolder = ".", patchName = "patch", inputPatch = string.Empty, ninjaPath = "/usr/build/ninja", devkitPpcPath = "/opt/devkitpro/devkitPPC", symbolsMap = string.Empty;
        uint arenaLo = 0;

        OptionSet options = new()
        {
            { "f|folder=", "The folder where your source files live", f => folder = f },
            { "m|dolphin-map|map|symbols=", "A Dolphin symbols .map file containing any functions you wish to reference", m => symbolsMap = m },
            { "a|arena-lo=", "The arena-lo offset (see documentation for how to determine this)", a => arenaLo = uint.Parse(a, NumberStyles.HexNumber) },
            { "o|output-folder=", "The folder to output the Riivolution patch.xml & assembled ASM bin file(s) to.", o => outputFolder = o },
            { "n|patch-name=", "The name of the patch to output. The patch will be out put to {output_folder}/Riivolution/{patch_name}.xml and the ASM bin(s) will be output to {output_folder}/{patch_name}/patch{i}.bin.",
                n => patchName = n },
            { "p|input-patch=", "The base Riivolution patch that will be modified by HaroohieClub.Wiinject.Cli to contain the memory patches. A blank base template will be created if this is not provided.", p => inputPatch = p },
            { "j|ninja-path=", "The path to the ninja build executable.", j => ninjaPath = j },
            { "d|devkitppc-path=", "The path to a devkitPPC installation.", d => devkitPpcPath = d },
        };

        options.Parse(args);

        if (string.IsNullOrEmpty(folder))
        {
            options.WriteOptionDescriptions(Console.Out);
            return (int)WiinjectReturnCode.Ok;
        }

        Directory.CreateDirectory(Path.Combine(outputFolder, patchName));
        Directory.CreateDirectory(Path.Combine(outputFolder, "Riivolution"));

        WiinjectResult result;
        try
        {
            result = WiinjectEngine.AssemblePatch(arenaLo, folder, symbolsMap, ninjaPath, devkitPpcPath, patchName, inputPatch);
        }
        catch (WiinjectException ex)
        {
            Console.WriteLine(ex.Message);
            return (int)WiinjectReturnCode.Error;
        }

        foreach (string binPatch in result.OutputBinaryPatches.Keys)
        {
            File.WriteAllBytes(Path.Combine(outputFolder, patchName, binPatch), result.OutputBinaryPatches[binPatch]);
        }

        string outputPath = Path.Combine(outputFolder, "Riivolution", $"{patchName}.xml");
        result.OutputRiivolution?.PatchXml.Save(outputPath);
        Console.WriteLine($"Wrote to {outputPath}");

        return (int)WiinjectReturnCode.Ok;
    }
}