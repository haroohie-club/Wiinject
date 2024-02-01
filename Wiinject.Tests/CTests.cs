using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Wiinject.Tests
{
    public class CTests
    {
        [Test]
        [TestCase("Backward", 0x80012030, 0x80013040, "4B FF EF D5")]
        [TestCase("Forward", 0x80013040, 0x80012030, "48 00 0F F5")]
        public void ResolveBranchesTest(string direction, uint returnTestEntryPoint, uint callTestEntryPoint, string expectedAsm)
        {
            List<CFunction> functions =
            [
                new("return_test", "00008000", TestHelpers.ReturnTestC) { EntryPoint = returnTestEntryPoint },
                new("call_test", "00008000", TestHelpers.CallTestC) { EntryPoint = callTestEntryPoint },
            ];

            functions[0].ResolveFunctionRefs(functions);
            functions[1].ResolveFunctionRefs(functions);

            List<CFunction> resolvedFunctions = [];
            resolvedFunctions = functions[1].FunctionsToResolve(resolvedFunctions);
            Assert.That(functions, Is.EquivalentTo(resolvedFunctions));

            functions[0].ResolveBranches();
            functions[1].ResolveBranches();

            functions[0].SetDataFromInstructions();
            functions[1].SetDataFromInstructions();

            Console.WriteLine(functions[1].Data.ToHexString());
            Assert.That(functions[1].Data.ToHexString(), Contains.Substring(expectedAsm));
        }

        [Test]
        public void ResolveRecursiveBranchTest()
        {
            List<CFunction> functions = [new("recursion_test", "00008000", TestHelpers.RecursionTestC) { EntryPoint = 0x8001200 }];
            functions[0].ResolveFunctionRefs(functions);

            List<CFunction> resolvedFunctions = [];
            resolvedFunctions = functions[0].FunctionsToResolve(resolvedFunctions);
            Assert.That(functions, Is.EquivalentTo(resolvedFunctions));

            functions[0].ResolveBranches();
            functions[0].SetDataFromInstructions();

            Console.WriteLine(functions[0].Data.ToHexString());
            Assert.That(functions[0].Data.ToHexString(), Contains.Substring("4B FF FF D1"));
        }
    }
}
