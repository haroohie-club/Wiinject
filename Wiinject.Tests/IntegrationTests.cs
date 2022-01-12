using NUnit.Framework;
using System.IO;

namespace Wiinject.Tests
{
    public class IntegrationTests
    {
        [Test]
        public void SuccessfulAsmOnlyTest()
        {
            int returnCode = Program.Main(new string[] {
                "-f", "test-cases",
                "-i", "80004000,80014000",
                "-e", "80004010,80014100",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.OK, returnCode);
            Assert.AreEqual(File.ReadAllText(Path.Combine(".", "out-cases", "Riivolution", "test-patch.xml")), @"<wiidisc>
  <patch>
    <memory offset=""0x8006FCA4"" value=""7F86E3787F67DB78"" />
    <memory offset=""0x8006FCB0"" value=""7F28CB78"" />
    <memory offset=""0x8006FBA4"" value=""7F26CB787F47D378"" />
    <memory offset=""0x8006FBB0"" value=""7F88E378"" />
    <memory offset=""0x80017250"" value=""4BFFCDB1"" />
    <memory offset=""0x8001726C"" value=""4BFFCDB1"" />
    <memory offset=""0x80004000"" valuefile=""/test-patch/patch0.bin"" />
    <memory offset=""0x80014000"" valuefile=""/test-patch/patch1.bin"" />
  </patch>
</wiidisc>");
        }

        [Test]
        public void AddressCountMismatchTest()
        {
            int returnCode = Program.Main(new string[] {
                "-f", "test-cases",
                "-i", "80004000",
                "-e", "80004010,80014100",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.ADDRESS_COUNT_MISMATCH, returnCode);
        }

        [Test]
        public void InjectionSitesTooSmallTest()
        {
            int returnCode = Program.Main(new string[] {
                "-f", "test-cases",
                "-i", "80004000",
                "-e", "80004010",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.INJECTION_SITES_TOO_SMALL, returnCode);
        }
    }
}
