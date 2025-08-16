using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HaroohieClub.Wiinject;

/// <summary>
/// Static class that drives assembling and patch creation
/// </summary>
public static class WiinjectEngine
{
    /// <summary>
    /// Assembles a patch given injection sites, code, and the necessary tools
    /// </summary>
    /// <param name="injectionAddresses">The injection site starting points</param>
    /// <param name="injectionEndAddresses">The injection site ending points</param>
    /// <param name="sourcePath">The path to the source code to assemble into the patch</param>
    /// <param name="dolphinMapPath">The path to the Dolphin function map</param>
    /// <param name="ninjaPath">The path to the ninja build system executable</param>
    /// <param name="devkitPpcPath">The path to the devkitPPC root folder (e.g. C:\devkitPro\devkitPPC or /opt/devkitpro/devkitPPC)</param>
    /// <param name="patchName">The name of the output patch</param>
    /// <param name="inputPatch">An input patch to base the current patch off of</param>
    /// <param name="symTableHelperPath">The path to the NitroPacker.SymTableHelper executable (optional, will default to the Wiinject dir)</param>
    /// <returns>A Wiinject result object indicating the outcome of the operation</returns>
    /// <exception cref="AddressCountMismatchException"></exception>
    public static WiinjectResult AssemblePatch(
        uint[] injectionAddresses,
        uint[] injectionEndAddresses,
        string sourcePath,
        string dolphinMapPath,
        string ninjaPath,
        string devkitPpcPath,
        string patchName,
        string? inputPatch = null,
        string symTableHelperPath = "")
    {
        string exeExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
        WiinjectResult result = new();

        if (injectionAddresses.Length != injectionEndAddresses.Length)
        {
            throw new AddressCountMismatchException();
        }

        if (!string.IsNullOrEmpty(inputPatch))
        {
            result.OutputRiivolution = new(inputPatch, Directory.GetDirectories(sourcePath));
        }
        else
        {
            result.OutputRiivolution = new(Directory.GetDirectories(sourcePath));
        }

        List<InjectionSite> injectionSites = [];
        for (int i = 0; i < injectionAddresses.Length; i++)
        {
            injectionSites.Add(new() { StartAddress = injectionAddresses[i], EndAddress = injectionEndAddresses[i] });
        }
        int currentInjectionSite = 0;
        int currentSize = 0;

        foreach (string patchDir in Directory.GetDirectories(sourcePath))
        {
            string constructedSourceDir = Path.Combine(patchDir, "constructed_src");
            if (Directory.Exists(constructedSourceDir))
            {
                Directory.Delete(constructedSourceDir, true);
            }
            Directory.CreateDirectory(constructedSourceDir);
            
            string asmDirBase = Path.Combine(constructedSourceDir, "hacks");
            string cDirBase = Path.Combine(constructedSourceDir, "funcs");
            string asmDir = asmDirBase;
            string cDir = cDirBase;
            string replDir = Path.Combine(constructedSourceDir, "repl");
            Directory.CreateDirectory(asmDir);
            Directory.CreateDirectory(cDir);
            Directory.CreateDirectory(replDir);
            
            Regex funcRegex = new(@"(?<mode>repl|hook|ref)_(?<address>[A-Fa-f\d]{8}):");
            List<Subroutine> subroutines = [];
            foreach (string asmFilePath in Directory.GetFiles(patchDir, "*.s"))
            {
                string asm = File.ReadAllText(asmFilePath);
                MatchCollection matches = funcRegex.Matches(asm);
                subroutines.AddRange(matches.Select(m =>
                {
                    string subroutineCode = asm[(m.Index + m.Length)..(m.NextMatch().Captures.Count == 0 ? asm.Length : m.NextMatch().Index)];
                    string tmpPath = Path.GetTempFileName();
                    File.WriteAllText($"{tmpPath}.s", subroutineCode);
                    ProcessStartInfo gccInfo = new()
                    {
                        FileName = Path.Combine(devkitPpcPath, "bin", $"powerpc-eabi-gcc{exeExt}"),
                        ArgumentList = { "-nodefaultlibs", "-c", "-o", $"{tmpPath}.o", $"{tmpPath}.s" },
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };
                    ProcessStartInfo objCopyInfo = new()
                    {
                        FileName = Path.Combine(devkitPpcPath, "bin", $"powerpc-eabi-objcopy{exeExt}"),
                        ArgumentList = { "-O", "binary", $"{tmpPath}.o", $"{tmpPath}.bin" },
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };
                    Process.Start(gccInfo)?.WaitForExit();
                    for (int i = 0; i < 100 && !File.Exists($"{tmpPath}.o"); i++) ;
                    Process.Start(objCopyInfo)?.WaitForExit();
                    for (int i = 0; i < 100 && !File.Exists($"{tmpPath}.bin"); i++) ;
                    int size = File.ReadAllBytes($"{tmpPath}.bin").Length;
                    File.Delete(tmpPath);
                    File.Delete($"{tmpPath}.o");
                    File.Delete($"{tmpPath}.bin");
                    return new Subroutine(m.Value.Replace(":", "").Trim(),
                        uint.Parse(m.Groups["address"].Value, NumberStyles.HexNumber),
                        Subroutine.GetReplacementMode(m.Groups["mode"].Value),
                        subroutineCode, size);
                }));
            }

            List<InjectionSite> funcInjectionSites = [];
            foreach (string cFilePath in Directory.GetFiles(patchDir, "*.c"))
            {
                ProcessStartInfo gccInfo = new()
                {
                    FileName = Path.Combine(devkitPpcPath, "bin", $"powerpc-eabi-gcc{exeExt}"),
                    ArgumentList = { "-nodefaultlibs", "-c", "-o", $"{cFilePath}.o", $"{cFilePath}" },
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                ProcessStartInfo objCopyInfo = new()
                {
                    FileName = Path.Combine(devkitPpcPath, "bin", $"powerpc-eabi-objcopy{exeExt}"),
                    ArgumentList = { "-O", "binary", $"{cFilePath}.o", $"{cFilePath}.bin" },
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                Process.Start(gccInfo)?.WaitForExit();
                for (int i = 0; i < 100 && !File.Exists($"{cFilePath}.o"); i++) ;
                Process.Start(objCopyInfo)?.WaitForExit();
                for (int i = 0; i < 100 && !File.Exists($"{cFilePath}.bin"); i++) ;
                int size = File.ReadAllBytes($"{cFilePath}.bin").Length;
                File.Delete($"{cFilePath}.o");
                File.Delete($"{cFilePath}.bin");

                currentSize += size;
                while (currentSize > injectionSites[currentInjectionSite].Length)
                {
                    injectionSites[currentInjectionSite].CodeDirs.Add(Path.GetFileName(cDir));
                    funcInjectionSites.Add(injectionSites[currentInjectionSite]);
                    currentSize = size;
                    currentInjectionSite++;
                    if (currentInjectionSite >= injectionSites.Count)
                    {
                        throw new InjectionSitesTooSmallException("Injection sites too small for compiled C code!");
                    }
                    cDir = $"{cDirBase}{currentInjectionSite}";
                    Directory.CreateDirectory(cDir);
                }
                
                File.Copy(cFilePath, Path.Combine(cDir, Path.GetFileName(cFilePath)));
            }

            if (currentSize > 0)
            {
                injectionSites[currentInjectionSite].CodeDirs.Add(Path.GetFileName(cDir));
                injectionSites.Insert(currentInjectionSite + 1,
                    new()
                    {
                        StartAddress = injectionSites[currentInjectionSite].StartAddress + (uint)currentSize + 4,
                        EndAddress = injectionSites[currentInjectionSite].EndAddress
                    });
                injectionSites[currentInjectionSite].EndAddress = injectionSites[currentInjectionSite + 1].StartAddress - 4;
                funcInjectionSites.Add(injectionSites[currentInjectionSite]);
                currentInjectionSite++;
                currentSize = 0;
            }

            List<InjectionSite> hackInjectionSites = [];
            StringBuilder mainCode = new();
            foreach (Subroutine subroutine in subroutines)
            {
                switch (subroutine.ReplacementMode)
                {
                    case ReplacementMode.Hook:
                    case ReplacementMode.Ref:
                        currentSize += subroutine.Size;
                        while (currentSize > injectionSites[currentInjectionSite].Length)
                        {
                            if (mainCode.Length > 0)
                            {
                                File.WriteAllText(Path.Combine(asmDir, "main_code.s"), mainCode.ToString());
                                mainCode = new();
                                hackInjectionSites.Add(injectionSites[currentInjectionSite]);
                                injectionSites[currentInjectionSite].CodeDirs.Add(Path.GetFileName(asmDir));
                            }
                            
                            currentSize = subroutine.Size;
                            currentInjectionSite++;
                            if (currentInjectionSite >= injectionSites.Count)
                            {
                                throw new InjectionSitesTooSmallException("Injection sites too small for ASM hacks!");
                            }
                            asmDir = $"{asmDirBase}{currentInjectionSite}";
                            Directory.CreateDirectory(asmDir);
                        }
                        mainCode.AppendLine($"{subroutine.ReplacementMode.ToString().ToLower()}_{subroutine.Address:X8}:");
                        mainCode.AppendLine(subroutine.Code);
                        break;
                    
                    case ReplacementMode.Repl:
                        File.WriteAllText(Path.Combine(replDir, $"{subroutine.Address:X8}.s"),
                            $"{subroutine.ReplacementMode.ToString().ToLower()}_{subroutine.Address:X8}:\n{subroutine.Code}");
                        break;
                    
                    default:
                    case ReplacementMode.Unknown:
                        throw new WiinjectException($"Unknown function code encountered for function at 0x{subroutine.Address:X8}!");
                }
            }

            if (mainCode.Length > 0)
            {
                File.WriteAllText(Path.Combine(asmDir, "main_code.s"), mainCode.ToString());

                injectionSites[currentInjectionSite].CodeDirs.Add(Path.GetFileName(asmDir));
                injectionSites.Insert(currentInjectionSite + 1,
                    new()
                    {
                        StartAddress = injectionSites[currentInjectionSite].StartAddress + (uint)currentSize + 4,
                        EndAddress = injectionSites[currentInjectionSite].EndAddress
                    });
                injectionSites[currentInjectionSite].EndAddress = injectionSites[currentInjectionSite + 1].StartAddress - 4;
                hackInjectionSites.Add(injectionSites[currentInjectionSite]);
                currentInjectionSite++;
                currentSize = 0;
            }
            
            List<string> symbolFiles = GenerateNinjaBuildFile(constructedSourceDir, devkitPpcPath, funcInjectionSites, hackInjectionSites, dolphinMapPath, symTableHelperPath, "", exeExt);
            ProcessStartInfo ninja = new()
            {
                FileName = ninjaPath,
                WorkingDirectory = constructedSourceDir,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process.Start(ninja)?.WaitForExit();

            Dictionary<string, uint> symbolsMap = [];
            foreach (string symbolFile in symbolFiles)
            {
                foreach (string line in File.ReadAllLines(Path.Combine(constructedSourceDir, symbolFile)).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    string[] symbolSplit = line.Split('=');
                    if (symbolSplit.Length != 2 || symbolsMap.ContainsKey(symbolSplit[0].Trim()))
                    {
                        continue;
                    }

                    if (uint.TryParse(symbolSplit[1].Trim()[2..^1], NumberStyles.HexNumber, null, out uint address))
                    {
                        symbolsMap.Add(symbolSplit[0].Trim(), address);
                    }
                }
            }
            
            foreach (Subroutine subroutine in subroutines)
            {
                switch (subroutine.ReplacementMode)
                {
                    case ReplacementMode.Hook:
                        result.OutputRiivolution.AddMemoryPatch(subroutine.Address,
                            [0x48, ..BitConverter.GetBytes(symbolsMap[subroutine.Name] - subroutine.Address + 1).Take(3).Reverse()],
                            patchDir);
                        break;
                    
                    case ReplacementMode.Repl:
                        result.OutputRiivolution.AddMemoryPatch(subroutine.Address,
                                File.ReadAllBytes(Path.Combine(constructedSourceDir, "build", $"{subroutine.Address:X8}.bin")),
                                patchDir);
                        break;
                    
                    case ReplacementMode.Ref:
                        result.OutputRiivolution.AddMemoryPatch(subroutine.Address, 
                            BitConverter.GetBytes(symbolsMap[subroutine.Name]).Reverse().ToArray(),
                            patchDir);
                        break;
                    
                    default:
                    case ReplacementMode.Unknown:
                        throw new WiinjectException($"Unknown function code encountered for function at 0x{subroutine.Address:X8}!");
                }
            }

            foreach (InjectionSite injectionSite in funcInjectionSites.Concat(hackInjectionSites))
            {
                uint loc = injectionSite.StartAddress;
                foreach (string dir in injectionSite.CodeDirs)
                {
                    if (!Directory.Exists(Path.Combine(constructedSourceDir, "build", dir)))
                    {
                        continue;
                    }

                    string patchDirName = Path.GetFileName(patchDir);
                    result.OutputBinaryPatches.Add($"{patchDirName}-{dir}.bin", File.ReadAllBytes(Path.Combine(constructedSourceDir, "build", dir, "newcode.bin")));
                    result.OutputRiivolution.AddMemoryFilesPatch(loc, $"{patchDirName}-{dir}.bin", patchDir);
                    loc += (uint)result.OutputBinaryPatches[$"{patchDirName}-{dir}.bin"].Length;
                }
            }
        }

        return result;
    }

    private static List<string> GenerateNinjaBuildFile(string sourceDir, string devkitPpcPath, List<InjectionSite> funcInjectionSites, List<InjectionSite> hackInjectionSites, string dolphinMapPath, string symTableHelperPath = "", string? linkerFlags = null, string exeExt = "")
    {
        StringBuilder sb = new();
        sb.AppendLine("# File generated by Wiinject");
        sb.AppendLine();

        sb.AppendLine($"DEVKITPPC        = {Utility.PathCombineAgnostic(devkitPpcPath, "bin")}/");
        sb.AppendLine($"CC               = ${{DEVKITPPC}}powerpc-eabi-gcc{exeExt}");
        sb.AppendLine($"LD               = ${{DEVKITPPC}}powerpc-eabi-ld{exeExt}");
        sb.AppendLine($"OBJCOPY          = ${{DEVKITPPC}}powerpc-eabi-objcopy{exeExt}");
        sb.AppendLine($"OBJDUMP          = ${{DEVKITPPC}}powerpc-eabi-objdump{exeExt}");
        sb.AppendLine($"SYMTABLEHELPER   = {(string.IsNullOrEmpty(symTableHelperPath) ? 
            Utility.PathCombineAgnostic(AppContext.BaseDirectory, $"NitroPacker.SymTableHelper{exeExt}") :
            symTableHelperPath.Replace('\\', '/'))}");
        sb.AppendLine();
        
        sb.AppendLine("rule cc");
        sb.AppendLine("  command = ${CC} -nodefaultlibs -c -o $out $in");
        sb.AppendLine();

        sb.AppendLine("rule ld");
        sb.AppendLine($"  command = ${{LD}} -Ttext=0x${{codeaddr}} ${{ldflags}} {linkerFlags ?? string.Empty} -o $out $in");
        sb.AppendLine();

        sb.AppendLine("rule objcopy");
        sb.AppendLine("  command = ${OBJCOPY} -O binary -j .text $in $out");
        sb.AppendLine();

        sb.AppendLine("rule objdump");
        sb.AppendLine(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "  command = cmd.exe /c \"\"${OBJDUMP}\" -t $in\" > $out"
            : "  command = ${OBJDUMP} -t $in > $out");
        sb.AppendLine();
        
        sb.AppendLine("rule symtablehelper");
        sb.AppendLine("  command = ${SYMTABLEHELPER} $in $out");
        sb.AppendLine();

        sb.AppendLine("build build: phony");
        sb.AppendLine();

        string[] hackDirs = Directory.GetDirectories(sourceDir, "hacks*");
        string[] funcDirs = Directory.GetDirectories(sourceDir, "funcs*");
        string replDir = Utility.PathCombineAgnostic(sourceDir,  "repl");
        
        DolphinSymbolsMap.WriteSymbolsMap(File.ReadAllLines(dolphinMapPath), Path.Combine(sourceDir, "dolphin.x"));
        
        List<string> symbolFiles = ["dolphin.x"];
        List<string> dependencies = WriteBuildLines(sb, funcDirs, sourceDir, funcInjectionSites, $"-T {string.Join(" -T ", symbolFiles)}", symbolFiles, "");
        dependencies = WriteBuildLines(sb, hackDirs, sourceDir, hackInjectionSites, $"-T {string.Join(" -T ", symbolFiles)}", symbolFiles, string.Join(' ', dependencies));
        string dependenciesStr = string.Join(' ',  dependencies);
        
        foreach (string replFile in Directory.GetFiles(replDir, "*.s"))
        {
            string baseName = Path.GetFileNameWithoutExtension(replFile).ToUpper();
            sb.AppendLine($"build build/{baseName}.o: cc {Utility.PathRelativeAgnostic(sourceDir, replFile)} || build {dependenciesStr}");
            sb.AppendLine();

            sb.AppendLine($"build build/{baseName}.elf: ld build/{baseName}.o || build {dependenciesStr}");
            sb.AppendLine($"  codeaddr = {baseName}");
            sb.AppendLine($"  ldflags = {(symbolFiles.Count == 0 ? string.Empty : $"-T {string.Join(" -T ", symbolFiles)}")}");
            sb.AppendLine();
            
            sb.AppendLine($"build build/{baseName}.bin: objcopy build/{baseName}.elf || build {dependenciesStr}");
            sb.AppendLine();
        }
        
        File.WriteAllText(Path.Combine(sourceDir, "build.ninja"), sb.ToString());
        return symbolFiles;
    }

    private static List<string>  WriteBuildLines(StringBuilder sb, string[] dirs, string sourceDir, List<InjectionSite> injectionSites, string linkerFlags, List<string> symbolFiles, string dependencies)
    {
        int currentInjectionSite = 0;
        List<string> newDependencies = [];
        foreach (string dir in dirs)
        {
            List<string> objFiles = [];
            string dirName = Path.GetFileName(dir);
            foreach (string file in Directory.GetFiles(dir))
            {
                string objFile = Utility.PathCombineAgnostic("build", dirName, $"{Path.GetFileNameWithoutExtension(file)}.o");
                objFiles.Add(objFile);
                sb.AppendLine($"build {objFile}: cc {Utility.PathRelativeAgnostic(sourceDir, file)} || build {dependencies}");
                sb.AppendLine();
            }

            if (objFiles.Count == 0)
            {
                continue;
            }

            sb.Append($"build build/{dirName}/newcode.elf: ld ");
            sb.AppendJoin(' ', objFiles);
            sb.AppendLine($" || build {dependencies}");
            sb.AppendLine($"  codeaddr = {injectionSites[currentInjectionSite++].StartAddress:X8}");
            sb.AppendLine($"  ldflags = {linkerFlags}");
            sb.AppendLine();

            sb.AppendLine($"build build/{dirName}/newcode.bin: objcopy build/{dirName}/newcode.elf || build {dependencies}");
            sb.AppendLine();
            
            sb.AppendLine($"build build/{dirName}/newcode.sym: objdump build/{dirName}/newcode.elf || build {dependencies}");
            sb.AppendLine();
            
            sb.AppendLine($"build build/{dirName}/newcode.x: symtablehelper build/{dirName}/newcode.sym || build {dependencies}");
            sb.AppendLine();
            
            symbolFiles.Add($"build/{dirName}/newcode.x");
            newDependencies.Add($"build/{dirName}/newcode.x");
        }

        return newDependencies;
    }
}