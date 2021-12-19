using System;
using System.IO;
using System.Runtime.InteropServices;

// Temporary hacky program to get Keystone working on any platform
namespace WiinjectDllCopier
{
    class Program
    {
        static void Main(string[] args)
        {
            string outDir = args[0];
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.Copy(Path.Combine(outDir, "win-x64", "keystone.dll"), Path.Combine(outDir, "keystone.dll"), overwrite: true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                File.Copy(Path.Combine(outDir, "osx-x64", "keystone.dylib"), Path.Combine(outDir, "keystone.dylib"), overwrite: true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.Copy(Path.Combine(outDir, "linux-x64", "keystone.so"), Path.Combine(outDir, "keystone.so"), overwrite: true);
            }
        }
    }
}
