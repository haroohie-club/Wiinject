using System.Collections.Generic;

namespace Wiinject
{
    public class WiinjectResult
    {
        public List<string> EmittedCFiles { get; set; } = new();
        public Dictionary<string, byte[]> OutputBinaryPatches { get; set; } = new();
        public Riivolution OutputRiivolution { get; set; }
    }
}
