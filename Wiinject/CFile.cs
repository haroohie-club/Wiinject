using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Wiinject
{
    public class CFile
    {
        public string FilePath { get; set; }
        public string Name => Path.GetFileNameWithoutExtension(FilePath);
        public string OutPath => Path.Combine(Path.GetDirectoryName(FilePath), $"{Name}.o");
        public List<CFunction> Functions { get; set; } = new();
        private StringBuilder _objdumpOutputReader;

        private static readonly Regex _FuncRegex = new(@"<(?<functionName>[\w\d_-]+)>:");

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

    public class CFunction
    {
        private static readonly Regex _DataRegex =
            new(@"(?<assembledInstruction>[\dA-Fa-f]{2} [\dA-Fa-f]{2} [\dA-Fa-f]{2} [\dA-Fa-f]{2})[\t ]+(?<disassembledInstruction>\w+(?:[\t ]+[a-fr\d,()-]+)?)(?: <(?<branchRef>[\w_-]+)>)?");

        public string Name { get; set; }
        public uint EntryPoint { get; set; }
        public byte[] Data { get; set; }
        public List<Instruction> Instructions { get; set; }
        public HashSet<CFunction> FunctionRefs { get; set; } = new();

        private string _data;

        public CFunction(string name, string data)
        {
            Name = name;
            _data = data;
            Instructions = _DataRegex.Matches(_data).Select(d => new Instruction(d.Groups["disassembledInstruction"].Value, d.Groups["assembledInstruction"].Value, d.Groups["branchRef"].Value)).ToList();
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

        public bool IsBranchLink => Text.StartsWith("bl") && !Text.StartsWith("blr") && !Text.StartsWith("blt") && !Text.StartsWith("ble");

        public Instruction(string text)
        {
            Text = text;
            Assemble();
        }

        public void ResolveBranch(int relativeBranch)
        {
            if (IsBranchLink)
            {
                Text = $"bl 0x{(long)relativeBranch:X16}";
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
