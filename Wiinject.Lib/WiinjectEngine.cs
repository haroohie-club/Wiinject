using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Wiinject.Interfaces;

namespace Wiinject
{
    public class WiinjectEngine
    {
        public static WiinjectResult AssemblePatch(
            uint[] injectionAddresses,
            uint[] injectionEndAddresses,
            Dictionary<string, (string, string)[]> asmFiles,
            string[] cFilesArray,
            string inputPatch,
            string[] symbolsMap,
            string gccPath,
            string objdumpPath,
            string patchName)
        {
            WiinjectResult result = new();

            if (injectionAddresses.Length != injectionEndAddresses.Length)
            {
                throw new AddressCountMismatchException();
            }

            if (!string.IsNullOrEmpty(inputPatch))
            {
                result.OutputRiivolution = new(inputPatch, asmFiles.Keys);
            }
            else
            {
                result.OutputRiivolution = new(asmFiles.Keys);
            }

            List<CFile> cFiles = new();
            if (cFilesArray.Length > 0)
            {
                if (!System.IO.File.Exists(gccPath))
                {
                    throw new GccNotFoundException(gccPath);
                }
                if (!System.IO.File.Exists(objdumpPath))
                {
                    throw new ObjdumpNotFoundException(objdumpPath);
                }

                cFiles = cFilesArray.Select(f => new CFile(f)).ToList();
                foreach (CFile cFile in cFiles)
                {
                    cFile.Compile(gccPath, objdumpPath);
                }
            }

            InjectionSite[] injectionSites = new InjectionSite[injectionAddresses.Length];
            for (int i = 0; i < injectionSites.Length; i++)
            {
                injectionSites[i] = new() { StartAddress = injectionAddresses[i], EndAddress = injectionEndAddresses[i] };
            }
            List<IFunction> resolvedFunctions = new();
            resolvedFunctions.AddRange(DolphinSymbolsMap.ParseDolphinSymbolsMap(symbolsMap));
            int binPatchNumber = 0;

            foreach (string patchId in asmFiles.Keys)
            {
                Regex varRegex = new(@"\$(?<name>[\w\d_]+): (?<instruction>.+)\r?\n");
                Regex funcRegex = new(@"(?<mode>repl|hook|ref|hex)_(?<address>[A-F\d]{8}):");
                List<Variable> variables = new();
                List<Routine> routines = new();

                InjectionSite[] patchInjectionSites = new InjectionSite[injectionAddresses.Length];
                for (int i = 0; i < patchInjectionSites.Length; i++)
                {
                    patchInjectionSites[i] = new() { StartAddress = injectionSites[i].CurrentAddress, EndAddress = injectionSites[i].EndAddress };
                }

                foreach ((string asmFileName, string asmFileText) in asmFiles[patchId])
                {
                    if (cFiles.Any(c => c.Name == asmFileName))
                    {
                        CFile cFile = cFiles.First(c => c.Name == asmFileName);

                        foreach (Match bl in Routine.BlRegex.Matches(asmFileText))
                        {
                            CFunction function = cFile.Functions.First(f => f.Name == bl.Groups["function"].Value);
                            if (!resolvedFunctions.Any(f => f.Name == function.Name))
                            {
                                resolvedFunctions.AddRange(function.FunctionsToResolve(resolvedFunctions.Where(f => !f.Existing).Select(f => (CFunction)f).ToList()));
                            }
                        }

                        CFunction? rawData = cFile.Functions.FirstOrDefault(f => f.Name == ".rodata");
                        if (rawData is not null)
                        {
                            foreach (InjectionSite injectionSite in patchInjectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
                            {
                                if (injectionSite.RoutineMashup.Count + rawData.Instructions.Count * 4 > injectionSite.Length)
                                {
                                    continue;
                                }

                                rawData.EntryPoint = injectionSite.CurrentAddress;
                                rawData.SetDataFromInstructions();
                                injectionSite.RoutineMashup.AddRange(rawData.Data);
                                break;
                            }
                        }

                        foreach (IFunction iFunction in resolvedFunctions)
                        {
                            if (!iFunction.Existing)
                            {
                                bool injected = false;
                                CFunction function = (CFunction)iFunction;
                                foreach (InjectionSite injectionSite in patchInjectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
                                {
                                    if (injectionSite.RoutineMashup.Count + function.Instructions.Count * 4 > injectionSite.Length)
                                    {
                                        continue;
                                    }

                                    function.EntryPoint = injectionSite.CurrentAddress;
                                    function.ResolveBranches();
                                    if (rawData is not null)
                                    {
                                        function.FixJumpTableJumps(rawData);
                                    }
                                    function.SetDataFromInstructions();
                                    injectionSite.RoutineMashup.AddRange(function.Data);
                                    injected = true;
                                    break;
                                }
                                if (!injected)
                                {
                                    throw new InjectionSitesTooSmallException($"Could not inject function {function.Name}; function longer than any available injection site.");
                                }

                                result.EmittedCFiles.Add($"== {function.Name} ==\n\n{string.Join('\n', function.Instructions.Select(i => i.Text))}");
                            }
                        }
                    }

                    string[] variableDeclarations = varRegex.Split(asmFileText);
                    for (int i = 1; i < variableDeclarations.Length; i += 3)
                    {
                        variables.Add(new(variableDeclarations[i], variableDeclarations[i + 1]));
                    }

                    if (variables.Any(v => variables.Count(v2 => v2.Name == v.Name) > 1))
                    {
                        throw new DuplicateVariableNameException(variables.First(v => variables.Count(v2 => v2.Name == v.Name) > 1).Name);
                    }

                    string[] assemblyRoutines = funcRegex.Split(asmFileText);

                    for (int i = 1; i < assemblyRoutines.Length; i += 3)
                    {
                        routines.Add(new(assemblyRoutines[i], uint.Parse(assemblyRoutines[i + 1], NumberStyles.HexNumber), assemblyRoutines[i + 2]));
                    }
                }

                if (patchInjectionSites.Sum(s => s.Length) < 4)
                {
                    throw new InjectionSitesTooSmallException($"Max injection length with provided addresses calculated to be {patchInjectionSites.Sum(s => s.Length)} which is less than one instruction.");
                }

                foreach (Variable variable in variables)
                {
                    bool injected = false;
                    foreach (InjectionSite injectionSite in patchInjectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
                    {
                        if (injectionSite.RoutineMashup.Count + variable.Data.Length > injectionSite.Length)
                        {
                            continue;
                        }

                        variable.InsertionPoint = injectionSite.StartAddress + (uint)injectionSite.RoutineMashup.Count;
                        injectionSite.RoutineMashup.AddRange(variable.Data);
                        injected = true;
                        break;
                    }
                    if (!injected)
                    {
                        throw new InjectionSitesTooSmallException($"Could not inject variable {variable.Name}; variable longer than any available injection site.");
                    }
                }

                foreach (Routine routine in routines)
                {
                    if (routine.RoutineMode == Routine.Mode.HEX)
                    {
                        result.OutputRiivolution.AddMemoryPatch(routine.InsertionPoint, routine.Data, patchId);
                    }
                    else if (routine.RoutineMode == Routine.Mode.REPL)
                    {
                        routine.ReplaceBl(resolvedFunctions, routine.InsertionPoint);
                        result.OutputRiivolution.AddMemoryPatch(routine.InsertionPoint, routine.Data, patchId);
                    }
                    else
                    {
                        bool injected = false;
                        foreach (InjectionSite injectionSite in patchInjectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
                        {
                            if (injectionSite.RoutineMashup.Count + routine.Data.Length > injectionSite.Length)
                            {
                                continue;
                            }

                            uint branchLocation = injectionSite.StartAddress + (uint)injectionSite.RoutineMashup.Count;
                            routine.SetBranchInstruction(branchLocation);
                            routine.ReplaceLv(variables);
                            routine.ReplaceBl(resolvedFunctions, branchLocation);
                            injectionSite.RoutineMashup.AddRange(routine.Data);
                            injected = true;
                            result.OutputRiivolution.AddMemoryPatch(routine.InsertionPoint, routine.BranchInstruction, patchId);
                            break;
                        }
                        if (!injected)
                        {
                            throw new InjectionSitesTooSmallException($"Error: could not inject routine beginning with {routine.Assembly.TakeWhile(c => c != '\n' && c != '\r')}; " +
                                $"routine longer than any available injection site.");
                        }
                    }
                }

                InjectionSite[] usedInjectionSites = patchInjectionSites.Where(s => s.RoutineMashup.Count > 0).ToArray();
                for (int i = 0; i < usedInjectionSites.Length; i++)
                {
                    result.OutputBinaryPatches.Add($"patch{binPatchNumber}.bin", usedInjectionSites[i].RoutineMashup.ToArray());
                    result.OutputRiivolution.AddMemoryFilesPatch(usedInjectionSites[i].StartAddress, $"/{patchName}/patch{binPatchNumber}.bin", patchId);
                    binPatchNumber++;
                }

                injectionSites = patchInjectionSites;
            }

            return result;
        }
    }
}
