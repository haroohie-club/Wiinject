using Keystone;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Wiinject
{
    public static class Assembler
    {
        public static byte[] Assemble(string asm)
        {
            Engine keystone = new(Architecture.PPC, Mode.X64);
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

        public Routine(string mode, uint insertionPoint, string assembly)
        {
            RoutineMode = (Mode)Enum.Parse(typeof(Mode), mode.ToUpper());
            Assembly = assembly;
            InsertionPoint = insertionPoint;
            Data = Assembler.Assemble(assembly);
        }

        public void SetBranchInstruction(uint branchTo)
        {
            uint relativeBranch = branchTo - InsertionPoint;
            string instruction = $"bl 0x{relativeBranch:X7}";
            BranchInstruction = Assembler.Assemble(instruction);
        }

        public enum Mode
        {
            HOOK,
            REPL
        }
    }
}
