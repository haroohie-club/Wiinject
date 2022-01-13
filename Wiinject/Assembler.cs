using Keystone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Wiinject
{
    public static class Assembler
    {
        public static byte[] Assemble(string asm)
        {
            Engine keystone = new(Architecture.PPC, Mode.X64);
            keystone.ThrowOnError = true;
            EncodedData data = keystone.Assemble(asm, 0);

            List<byte> bigEndianData = new();

            for (int i = 0; i < data.Buffer.Length; i += 4)
            {
                bigEndianData.AddRange(data.Buffer.Skip(i).Take(4).Reverse());
            }

            return bigEndianData.ToArray();
        }
    }

    public class Routine
    {
        public Mode RoutineMode { get; set; }
        public string Assembly { get; private set; }
        public uint InsertionPoint { get; private set; }
        public byte[] Data { get; private set; }
        public byte[] BranchInstruction { get; private set; }

        public static readonly Regex BlRegex = new(@"(?<mnemonic>bc?l)[\t ]+=(?<function>[\w\d_]+)");
        public static readonly Regex LvRegex = new(@"lv[\t ]+(?<register>\d{1,2})[\t ]*,[\t ]*\$(?<variableName>[\w\d_]+)");

        public Routine(string mode, uint insertionPoint, string assembly)
        {
            RoutineMode = (Mode)Enum.Parse(typeof(Mode), mode.ToUpper());
            Assembly = assembly;
            InsertionPoint = insertionPoint;
            string blReplacedAssembly = BlRegex.Replace(assembly, "bl 0x800000"); // temporarily replace bls for assembly; will be resolved in later steps
            string lvReplacedAssembly = LvRegex.Replace(blReplacedAssembly, "lis 1,0x8000\naddi 1,1,0x0000"); // also temporarily replace the variable refs
            Data = Assembler.Assemble(lvReplacedAssembly);
        }

        public void SetBranchInstruction(uint branchTo)
        {
            int relativeBranch = (int)(branchTo - InsertionPoint);
            string instruction = $"bl 0x{(long)relativeBranch:X16}";
            BranchInstruction = Assembler.Assemble(instruction);
        }

        public void ReplaceBl(List<CFunction> functions, uint injectionPoint)
        {
            if (!BlRegex.IsMatch(Assembly))
            {
                return;
            }

            StringBuilder sb = new();
            foreach (string line in Assembly.Replace("\r\n", "\n").Split('\n'))
            {
                Match match = BlRegex.Match(line);
                if (match.Success)
                {
                    int relativeBranch = (int)(functions.First(f => f.Name == match.Groups["function"].Value).EntryPoint - injectionPoint);
                    sb.AppendLine($"{match.Groups["mnemonic"].Value} 0x{(long)relativeBranch:X16}");
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            Assembly = sb.ToString();
            Data = Assembler.Assemble(LvRegex.Replace(Assembly, "lis 1,0x8000\naddi 1,1,0x0000")); // replace any lv instructions
        }

        public void ReplaceLv(List<Variable> variables)
        {
            if (!LvRegex.IsMatch(Assembly))
            {
                return;
            }

            StringBuilder sb = new();
            foreach (string line in Assembly.Replace("\r\n", "\n").Split('\n'))
            {
                Match match = LvRegex.Match(line);
                if (match.Success)
                {
                    uint variableAddress = variables.First(f => f.Name == match.Groups["variableName"].Value).InsertionPoint;
                    sb.AppendLine($"lis {match.Groups["register"].Value},0x{variableAddress >> 16:X4}");
                    sb.AppendLine($"addi {match.Groups["register"].Value},{match.Groups["register"].Value},0x{variableAddress & 0xFFFF:X4}");
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            Assembly = sb.ToString();
            Data = Assembler.Assemble(BlRegex.Replace(Assembly, "bl 0x8000000")); // replace any bl function instructions
        }

        public enum Mode
        {
            HOOK,
            REPL
        }
    }

    public class Variable
    {
        public string Name { get; set; }
        public string Instruction { get; set; }
        public byte[] Data { get; set; }
        public uint InsertionPoint { get; set; }

        public Variable(string name, string instruction)
        {
            Name = name;
            Instruction = instruction;
            Data = Assembler.Assemble(instruction);
        }
    }

    public class InjectionSite
    {
        public uint StartAddress { get; set; }
        public uint EndAddress { get; set; }
        public uint CurrentAddress => StartAddress + (uint)RoutineMashup.Count;
        public int Length => (int)(EndAddress - StartAddress + 4); // +4 for including the end address
        public List<byte> RoutineMashup { get; set; } = new();
    }
}
