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
            (string, string)[] asmFiles,
            (string fileName, string fileContents)[] cFilesArray,
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
                result.OutputRiivolution = new(inputPatch);
            }
            else
            {
                result.OutputRiivolution = new();
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

                cFiles = cFilesArray.Select(f => new CFile(f.fileName, f.fileContents)).ToList();
                foreach (CFile cFile in cFiles)
                {
                    cFile.Compile(gccPath, objdumpPath);
                }
            }

            Regex varRegex = new(@"\$(?<name>[\w\d_]+): (?<instruction>.+)\r?\n");
            Regex funcRegex = new(@"(?<mode>repl|hook|ref|hex)_(?<address>[A-F\d]{8}):");
            List<Variable> variables = new();
            List<Routine> routines = new();
            InjectionSite[] injectionSites = new InjectionSite[injectionAddresses.Length];
            for (int i = 0; i < injectionSites.Length; i++)
            {
                injectionSites[i] = new() { StartAddress = injectionAddresses[i], EndAddress = injectionEndAddresses[i] };
            }

            List<IFunction> resolvedFunctions = new();
            resolvedFunctions.AddRange(DolphinSymbolsMap.ParseDolphinSymbolsMap(symbolsMap));

            foreach ((string asmFileName, string asmFileText) in asmFiles)
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

                    foreach (IFunction iFunction in resolvedFunctions)
                    {
                        if (!iFunction.Existing)
                        {
                            bool injected = false;
                            CFunction function = (CFunction)iFunction;
                            foreach (InjectionSite injectionSite in injectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
                            {
                                if (injectionSite.RoutineMashup.Count + function.Instructions.Count * 4 > injectionSite.Length)
                                {
                                    continue;
                                }

                                function.EntryPoint = injectionSite.CurrentAddress;
                                function.ResolveBranches();
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

            if (injectionSites.Sum(s => s.Length) < 4)
            {
                throw new InjectionSitesTooSmallException($"Max injection length with provided addresses calculated to be {injectionSites.Sum(s => s.Length)} which is less than one instruction.");
            }

            foreach (Variable variable in variables)
            {
                bool injected = false;
                foreach (InjectionSite injectionSite in injectionSites.OrderBy(s => s.Length - s.RoutineMashup.Count))
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
                    result.OutputRiivolution.AddMemoryPatch(routine.InsertionPoint, routine.Data);
                }
                else if (routine.RoutineMode == Routine.Mode.REPL)
                {
                    routine.ReplaceBl(resolvedFunctions, routine.InsertionPoint);
                    result.OutputRiivolution.AddMemoryPatch(routine.InsertionPoint, routine.Data);
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
                        routine.ReplaceLv(variables);
                        routine.ReplaceBl(resolvedFunctions, branchLocation);
                        injectionSite.RoutineMashup.AddRange(routine.Data);
                        injected = true;
                        result.OutputRiivolution.AddMemoryPatch(routine.InsertionPoint, routine.BranchInstruction);
                        break;
                    }
                    if (!injected)
                    {
                        throw new InjectionSitesTooSmallException($"Error: could not inject routine beginning with {routine.Assembly.TakeWhile(c => c != '\n' && c != '\r')}; " +
                            $"routine longer than any available injection site.");
                    }
                }
            }

            InjectionSite[] usedInjectionSites = injectionSites.Where(s => s.RoutineMashup.Count > 0).ToArray();
            for (int i = 0; i < usedInjectionSites.Length; i++)
            {
                result.OutputBinaryPatches.Add($"patch{i}.bin", usedInjectionSites[i].RoutineMashup.ToArray());
                result.OutputRiivolution.AddMemoryFilesPatch(usedInjectionSites[i].StartAddress, $"/{patchName}/patch{i}.bin");
            }

            return result;
        }
    }
}
