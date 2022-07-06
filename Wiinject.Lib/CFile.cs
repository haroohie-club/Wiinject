using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Wiinject.Interfaces;

namespace Wiinject
{
    public class CFile
    {
        public string FilePath { get; set; }
        public string Name => Path.GetFileNameWithoutExtension(FilePath);
        public string OutPath => Path.Combine(Path.GetTempPath(), $"{Name}.o");
        public List<CFunction> Functions { get; set; } = new();
        private StringBuilder _objdumpOutputReader;

        private static readonly Regex _FuncRegex = new(@"<(?<functionName>\.?[\w\d_-]+)>:");

        public CFile(string filePath)
        {
            FilePath = filePath;
        }

        public void Compile(string gccPath, string objdumpPath)
        {
            // compile the C file
            using Process gccProcess = Process.Start(gccPath, $"\"{FilePath}\" -o \"{OutPath}\"");
            gccProcess.WaitForExit();

            // read objdump output to parse file
            using Process objdumpProcess = new();
            objdumpProcess.StartInfo.FileName = objdumpPath;
            objdumpProcess.StartInfo.Arguments = $"-D {OutPath}";
            objdumpProcess.StartInfo.UseShellExecute = false;
            objdumpProcess.StartInfo.RedirectStandardOutput = true;

            _objdumpOutputReader = new();
            objdumpProcess.OutputDataReceived += ObjdumpProcess_OutputDataReceived;

            objdumpProcess.Start();
            objdumpProcess.BeginOutputReadLine();

            objdumpProcess.WaitForExit();

            string objdumpOutput = _objdumpOutputReader.ToString();

            string[] functions = _FuncRegex.Split(objdumpOutput);
            for (int i = 1; i < functions.Length - 1; i += 2)
            {
                Functions.Add(new(functions[i], functions[i - 1][^9..^1], functions[i + 1]));
            }
            foreach (CFunction function in Functions)
            {
                function.ResolveFunctionRefs(Functions);
            }

            File.Delete(OutPath);
        }

        private void ObjdumpProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            _objdumpOutputReader.AppendLine(e.Data);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CFunction : IFunction
    {
        private static readonly Regex _DataRegex =
            new(@"(?<assembledInstruction>[\dA-Fa-f]{2} [\dA-Fa-f]{2} [\dA-Fa-f]{2} [\dA-Fa-f]{2})[\t ]+(?<disassembledInstruction>\.?\w+(?:[\t ]+[a-fr\d,()-]+)?)(?: <(?<branchRef>[\w_-]+)>)?");

        public string Name { get; set; }
        public uint EntryPoint { get; set; }
        public uint SimulatedEntryPoint { get; set; }
        public byte[] Data { get; set; }
        public List<Instruction> Instructions { get; set; }
        public HashSet<CFunction> FunctionRefs { get; set; } = new();
        public bool Existing => false;

        private string _data;

        public CFunction(string name, string simulatedEntryPoint, string data)
        {
            Name = name;
            SimulatedEntryPoint = uint.Parse(simulatedEntryPoint, NumberStyles.HexNumber);
            _data = data;
            Data = Array.Empty<byte>();
            Instructions = _DataRegex.Matches(_data).Select(d => new Instruction(d.Groups["disassembledInstruction"].Value, d.Groups["assembledInstruction"].Value, d.Groups["branchRef"].Value)).ToList();
        }

        public CFunction(string name, uint entryPoint)
        {
            Name = name;
            EntryPoint = entryPoint;
            Instructions = new();
            FunctionRefs = new();
            Data = Array.Empty<byte>();
            _data = string.Empty;
        }

        public void ResolveFunctionRefs(IEnumerable<CFunction> cFunctions)
        {
            foreach (Instruction instruction in Instructions.Where(i => i.IsBranchLink))
            {
                if (!string.IsNullOrEmpty(instruction.BranchRef))
                {
                    FunctionRefs.Add(cFunctions.First(f => instruction.BranchRef.Contains(f.Name)));
                }
            }
        }

        public void ResolveBranches()
        {
            for (int i = 0; i < Instructions.Count; i++)
            {
                if (Instructions[i].IsBranchLink)
                {
                    uint instructionPoint = EntryPoint + (uint)i * 4;
                    uint branchPoint = FunctionRefs.First(f => f.Name == Instructions[i].BranchRef).EntryPoint;
                    Instructions[i].ResolveBranch((int)(branchPoint - instructionPoint));
                }
            }
        }

        // we assume all `bctr`s are jumptable jumps. this is probably a bad assumption
        // i don't care right now though, i just need my switch statement to work
        public void FixJumpTableJumps(CFunction rawData)
        {
            Regex shiftedRegex = new(@"lis\s+r(?<r1>\d{1,2}),(?<shifted>\d+)");
            Regex addedRegex = new(@"addi\s+r(?<r1>\d{1,2}),r(?<r2>\d{1,2}),(?<added>\d+)");

            for (int i = 0; i < Instructions.Count; i++)
            {
                if (Instructions[i].IsBranchToCountRegister)
                {
                    try
                    {
                        Match loadShiftedMatch = shiftedRegex.Match(Instructions[i - 8].Text);
                        Match loadAddedMatch = addedRegex.Match(Instructions[i - 7].Text);
                        Match branchShiftedMatch = shiftedRegex.Match(Instructions[i - 4].Text);
                        Match branchAddedMatch = addedRegex.Match(Instructions[i - 3].Text);

                        uint loadFrom = (uint.Parse(loadShiftedMatch.Groups["shifted"].Value) << 16) + uint.Parse(loadAddedMatch.Groups["added"].Value);
                        uint branchFrom = (uint.Parse(branchShiftedMatch.Groups["shifted"].Value) << 16) + uint.Parse(branchAddedMatch.Groups["added"].Value);

                        int loadDifference = (int)(loadFrom - rawData.SimulatedEntryPoint);
                        int branchDifference = (int)(branchFrom - SimulatedEntryPoint);

                        uint newLoadFrom = (uint)(rawData.EntryPoint + loadDifference);
                        uint newBranchFrom = (uint)(EntryPoint + branchDifference);

                        if ((newLoadFrom & 0xFFFF) > 0x7FFF)
                        {
                            Instructions[i - 8].Text = $"lis {loadShiftedMatch.Groups["r1"].Value},{(newLoadFrom >> 16) + 1}";
                            Instructions[i - 7].Text = $"subi {loadAddedMatch.Groups["r1"].Value},{loadAddedMatch.Groups["r2"].Value},{0x10000 - (newLoadFrom & 0xFFFF)}";
                        }
                        else
                        {
                            Instructions[i - 8].Text = $"lis {loadShiftedMatch.Groups["r1"].Value},{newLoadFrom >> 16}";
                            Instructions[i - 7].Text = $"addi {loadAddedMatch.Groups["r1"].Value},{loadAddedMatch.Groups["r2"].Value},{newLoadFrom & 0xFFFF}";
                        }

                        if ((newBranchFrom & 0xFFFF) > 0x7FFF)
                        {
                            Instructions[i - 4].Text = $"lis {branchShiftedMatch.Groups["r1"].Value},{(newBranchFrom >> 16) + 1}";
                            Instructions[i - 3].Text = $"subi {branchAddedMatch.Groups["r1"].Value},{branchAddedMatch.Groups["r2"].Value},{0x10000 - (newBranchFrom & 0xFFFF)}";
                        }
                        else
                        {
                            Instructions[i - 4].Text = $"lis {branchShiftedMatch.Groups["r1"].Value},{newBranchFrom >> 16}";
                            Instructions[i - 3].Text = $"addi {branchAddedMatch.Groups["r1"].Value},{branchAddedMatch.Groups["r2"].Value},{newBranchFrom & 0xFFFF}";
                        }

                        Instructions[i - 8].Assemble();
                        Instructions[i - 7].Assemble();
                        Instructions[i - 4].Assemble();
                        Instructions[i - 3].Assemble();
                    }
                    catch (Exception ex)
                    {
                        throw new JumptableFixingException($"Failed to fix jump table jumps for function {Name} at instruction {i}. Error: {ex.Message}");
                    }
                }
            }
        }

        public List<CFunction> FunctionsToResolve(List<CFunction> alreadyResolvedFunctions)
        {
            foreach (CFunction refFunction in FunctionRefs)
            {
                if (refFunction.Name == Name)
                {
                    continue;
                }
                else if (!alreadyResolvedFunctions.Any(f => f.Name == refFunction.Name))
                {
                    refFunction.FunctionsToResolve(alreadyResolvedFunctions);
                }
            }
            if (!alreadyResolvedFunctions.Any(f => f.Name == Name))
            {
                alreadyResolvedFunctions.Add(this);
            }

            return alreadyResolvedFunctions;
        }

        public void SetDataFromInstructions()
        {
            Data = Instructions.SelectMany(i => i.Data).ToArray();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Instruction
    {
        public string Text { get; set; }
        public byte[] Data { get; set; }
        public string BranchRef { get; set; }

        private static readonly Regex _branchLinkRegex = new(@"(?<mnemonic>bc?l)[\t ]");
        public bool IsBranchLink => _branchLinkRegex.IsMatch(Text);
        public bool IsBranchToCountRegister => Text.Contains("bctr");

        public Instruction(string text)
        {
            Data = Array.Empty<byte>();
            BranchRef = string.Empty;
            Text = text;
            Assemble();
        }

        public void ResolveBranch(int relativeBranch)
        {
            if (IsBranchLink)
            {
                string oldInstruction = Text;
                try
                {
                    Match match = _branchLinkRegex.Match(Text);
                    Text = $"{match.Groups["mnemonic"].Value} 0x{(long)relativeBranch:X16}";
                    Assemble();
                }
                catch (Exception ex)
                {
                    throw new FailedToResolveBranchLinkException($"Failed to resolve branch link in instruction {oldInstruction} for branch 0x{relativeBranch:X16}. Message: {ex.Message}");
                }
            }
        }

        public Instruction(string text, string data, string branchRef)
        {
            Text = text;
            Data = data.Split(' ').Select(b => byte.Parse(b, NumberStyles.HexNumber)).ToArray();
            BranchRef = branchRef;
        }

        public void Assemble()
        {
            Data = Assembler.Assemble(Text);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
