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

        public static readonly Regex BlRegex = new(@"bl[\t ]+=(?<function>[\w\d_]+)");

        public Routine(string mode, uint insertionPoint, string assembly)
        {
            RoutineMode = (Mode)Enum.Parse(typeof(Mode), mode.ToUpper());
            Assembly = assembly;
            InsertionPoint = insertionPoint;
            Data = Assembler.Assemble(BlRegex.Replace(assembly, "bl 0x800000")); // temporarily replace bls for assembly; will be resolved in later steps
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
                    sb.AppendLine(BlRegex.Replace(line, $"bl 0x{(long)relativeBranch:X16}"));
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            Assembly = sb.ToString();
            Data = Assembler.Assemble(Assembly);
        }

        public enum Mode
        {
            HOOK,
            REPL
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
