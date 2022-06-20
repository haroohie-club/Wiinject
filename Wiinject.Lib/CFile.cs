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
        public string Name { get; set; }
        public string Text { get; set; }
        public string FilePath => Path.Combine(Path.GetTempPath(), $"{Name}.c");
        public string OutPath => Path.Combine(Path.GetTempPath(), $"{Name}.o");
        public List<CFunction> Functions { get; set; } = new();
        private StringBuilder _objdumpOutputReader;

        private static readonly Regex _FuncRegex = new(@"<(?<functionName>[\w\d_-]+)>:");

        public CFile(string fileName, string fileContents)
        {
            Name = fileName;
            Text = fileContents;
        }

        public void Compile(string gccPath, string objdumpPath)
        {
            // create the temp C file
            File.WriteAllText(FilePath, Text);

            // compile the C file
            using Process gccProcess = Process.Start(gccPath, $"\"{FilePath}\" -o \"{OutPath}\"");
            gccProcess.WaitForExit();

            // read objdump output to parse file
            using Process objdumpProcess = new();
            objdumpProcess.StartInfo.FileName = objdumpPath;
            objdumpProcess.StartInfo.Arguments = $"-d {OutPath}";
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
                Functions.Add(new(functions[i], functions[i + 1]));
            }
            foreach (CFunction function in Functions)
            {
                function.ResolveFunctionRefs(Functions);
            }

            File.Delete(FilePath);
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
            new(@"(?<assembledInstruction>[\dA-Fa-f]{2} [\dA-Fa-f]{2} [\dA-Fa-f]{2} [\dA-Fa-f]{2})[\t ]+(?<disassembledInstruction>\w+(?:[\t ]+[a-fr\d,()-]+)?)(?: <(?<branchRef>[\w_-]+)>)?");

        public string Name { get; set; }
        public uint EntryPoint { get; set; }
        public byte[] Data { get; set; }
        public List<Instruction> Instructions { get; set; }
        public HashSet<CFunction> FunctionRefs { get; set; } = new();
        public bool Existing => false;

        private string _data;

        public CFunction(string name, string data)
        {
            Name = name;
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
                    uint InstructionPoint = EntryPoint + (uint)i * 4;
                    uint BranchPoint = FunctionRefs.First(f => f.Name == Instructions[i].BranchRef).EntryPoint;
                    Instructions[i].ResolveBranch((int)(BranchPoint - InstructionPoint));
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
                Match match = _branchLinkRegex.Match(Text);
                Text = $"{match.Groups["mnemonic"].Value} 0x{(long)relativeBranch:X16}";
                Assemble();
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
