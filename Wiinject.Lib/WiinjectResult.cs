using System.Collections.Generic;

namespace Wiinject
{
    public class WiinjectResult
    {
        public List<string> EmittedCFiles { get; set; } = [];
        public Dictionary<string, byte[]> OutputBinaryPatches { get; set; } = [];
        public Riivolution OutputRiivolution { get; set; }
    }
}
