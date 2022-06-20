using NUnit.Framework;
using System.IO;

namespace Wiinject.Tests
{
    public class IntegrationTests
    {
        [Test]
        public void SuccessfulAsmOnlyTest()
        {
            int returnCode = Program.Main(new string[]
            {
                "-f", "test-cases/good",
                "-i", "80004000,80014000",
                "-e", "80004010,80014100",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.OK, returnCode);
            Assert.AreEqual(@"<wiidisc>
  <patch id=""patch1"">
    <memory offset=""0x8006FCA4"" value=""7F86E3787F67DB78"" />
    <memory offset=""0x8006FCB0"" value=""7F28CB78"" />
    <memory offset=""0x8006FBA4"" value=""7F26CB787F47D378"" />
    <memory offset=""0x8006FBB0"" value=""7F88E378"" />
    <memory offset=""0x80017250"" value=""4BFFCDB1"" />
    <memory offset=""0x8001726C"" value=""4BFFCDB1"" />
    <memory offset=""0x801A8000"" value=""80014060"" />
    <memory offset=""0x80004000"" valuefile=""/test-patch/patch0.bin"" />
    <memory offset=""0x80014000"" valuefile=""/test-patch/patch1.bin"" />
  </patch>
  <patch id=""patch2"">
    <memory offset=""0x801A8010"" value=""FADEDBAE0123456789"" />
    <memory offset=""0x80017250"" value=""4BFFCE19"" />
    <memory offset=""0x80014068"" valuefile=""/test-patch/patch2.bin"" />
  </patch>
</wiidisc>",
            File.ReadAllText(Path.Combine(".", "out-cases", "Riivolution", "test-patch.xml")));
        }

        [Test]
        public void BadAsmTest()
        {
            Keystone.KeystoneException exception = Assert.Throws<Keystone.KeystoneException>(delegate
            {
                Program.Main(new string[]
                {
                    "-f", "test-cases/bad",
                    "-i", "80004000,80014000",
                    "-e", "80004010,80014100",
                    "-o", "out-cases",
                    "-n", "test-patch"
                });
            });

            Assert.AreEqual("Error while assembling instructions.", exception.Message);
        }

        [Test]
        public void AddressCountMismatchTest()
        {
            int returnCode = Program.Main(new string[]
            {
                "-f", "test-cases/good",
                "-i", "80004000",
                "-e", "80004010,80014100",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.ERROR, returnCode);
        }

        [Test]
        public void GccNotFoundTest()
        {
            int returnCode = Program.Main(new string[]
            {
                "-f", "test-cases/gcc_not_found",
                "-i", "80004000",
                "-e", "80014000",
                "-o", "out-cases",
                "-n", "test-patch",
                "-d", "devkitpro-not-here",
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.ERROR, returnCode);
        }

        [Test]
        public void InjectionSitesTooSmallTest()
        {
            int returnCode = Program.Main(new string[]
            {
                "-f", "test-cases/good",
                "-i", "80004000",
                "-e", "80004010",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.ERROR, returnCode);
        }

        [Test]
        public void DuplicateVariablesTest()
        {
            int returnCode = Program.Main(new string[]
            {
                "-f", "test-cases/duplicate_variables",
                "-i", "80004000",
                "-e", "80014000",
                "-o", "out-cases",
                "-n", "test-patch"
            });

            Assert.AreEqual((int)Program.WiinjectReturnCode.ERROR, returnCode);
        }
    }
}
