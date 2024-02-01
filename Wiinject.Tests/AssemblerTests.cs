using Keystone;
using NUnit.Framework;
using System.Collections.Generic;
using Wiinject.Interfaces;

namespace Wiinject.Tests
{
    public class AssemblerTests
    {
        [Test]
        public void AssemblerFailsOnInvalidAssemblyTest()
        {
            string invalidAsm = @"mr r1,r2";

            Assert.Throws<KeystoneException>(() => Assembler.Assemble(invalidAsm));
        }

        [Test]
        public void AssemblerSucceedsOnValidAssemblyTest()
        {
            string validAsm = @"mr 1,2";

            Assert.DoesNotThrow(() => Assembler.Assemble(validAsm));
            Assert.That(Assembler.Assemble(validAsm).ToHexString(), Is.EqualTo("7C 41 13 78"));
        }

        [Test]
        [TestCase("Forward", 0x80004000, 0x80103980, "48 0F F9 81")]
        [TestCase("Backward", 0x80103980, 0x80004000, "4B F0 06 81")]
        public void RoutineSetBranchInstructionTest(string direction, uint insertionPoint, uint branchTo, string hexStringResult)
        {
            Routine routine = new("hook", insertionPoint, @"mr 1,2");
            routine.SetBranchInstruction(branchTo);

            Assert.That(routine.BranchInstruction.ToHexString(), Is.EqualTo(hexStringResult));
        }

        [Test]
        [TestCase("Forward", 0x80004000, 0x80103980, 0x80100000, "48 00 39 69")]
        [TestCase("Backward", 0x80004000, 0x80103980, 0x80106000, "4B FF D9 69")]
        public void RoutineReplaceBlTest(string direction, uint routineInsertionPoint, uint functionInjectionPoint, uint routineInjectionPoint, string hexResult)
        {
            Routine routine = new("hook", routineInsertionPoint, TestHelpers.TestFunctionCallAsm);
            List<IFunction> functions =
            [
                new CFunction("test_function", "00008000", TestHelpers.TestFunctionC) { EntryPoint = functionInjectionPoint },
            ];

            routine.ReplaceBl(functions, routineInjectionPoint);

            Assert.That(routine.Data.ToHexString().Contains(hexResult), Is.True);
        }
    }
}