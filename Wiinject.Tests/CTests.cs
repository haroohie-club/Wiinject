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
            List<CFunction> functions = new();
            functions.Add(new("return_test", TestHelpers.ReturnTestC) { EntryPoint = returnTestEntryPoint });
            functions.Add(new("call_test", TestHelpers.CallTestC) { EntryPoint = callTestEntryPoint });

            functions[0].ResolveFunctionRefs(functions);
            functions[1].ResolveFunctionRefs(functions);

            List<CFunction> resolvedFunctions = new();
            resolvedFunctions = functions[1].FunctionsToResolve(resolvedFunctions);
            Assert.AreEqual(functions, resolvedFunctions);

            functions[0].ResolveBranches();
            functions[1].ResolveBranches();

            functions[0].SetDataFromInstructions();
            functions[1].SetDataFromInstructions();

            Console.WriteLine(functions[1].Data.ToHexString());
            Assert.IsTrue(functions[1].Data.ToHexString().Contains(expectedAsm));
        }
        
        [Test]
        public void ResolveRecursiveBranchTest()
        {
            List<CFunction> functions = new();
            functions.Add(new("recursion_test", TestHelpers.RecursionTestC) { EntryPoint = 0x8001200 });
            functions[0].ResolveFunctionRefs(functions);

            List<CFunction> resolvedFunctions = new();
            resolvedFunctions = functions[0].FunctionsToResolve(resolvedFunctions);
            Assert.AreEqual(functions, resolvedFunctions);

            functions[0].ResolveBranches();
            functions[0].SetDataFromInstructions();

            Console.WriteLine(functions[0].Data.ToHexString());
            Assert.IsTrue(functions[0].Data.ToHexString().Contains("4B FF FF D1"));
        }
    }
}
