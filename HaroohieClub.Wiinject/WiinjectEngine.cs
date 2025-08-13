using System.IO;

namespace HaroohieClub.Wiinject
{
    public class WiinjectEngine
    {
        public static WiinjectResult AssemblePatch(
            uint[] injectionAddresses,
            uint[] injectionEndAddresses,
            string sourcePath,
            string inputPatch,
            string symbolsPath,
            string ninjaPath,
            string gccPath,
            string objdumpPath,
            string patchName)
        {
            WiinjectResult result = new();

            if (injectionAddresses.Length != injectionEndAddresses.Length)
            {
                throw new AddressCountMismatchException();
            }

            if (!string.IsNullOrEmpty(inputPatch))
            {
                result.OutputRiivolution = new(inputPatch, Directory.GetDirectories(sourcePath));
            }
            else
            {
                result.OutputRiivolution = new(Directory.GetDirectories(sourcePath));
            }

            InjectionSite[] injectionSites = new InjectionSite[injectionAddresses.Length];
            for (int i = 0; i < injectionSites.Length; i++)
            {
                injectionSites[i] = new() { StartAddress = injectionAddresses[i], EndAddress = injectionEndAddresses[i] };
            }
            
            

            return result;
        }
    }
}
