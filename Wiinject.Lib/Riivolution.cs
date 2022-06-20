using System.Linq;
using System.Xml;

namespace Wiinject
{
    public class Riivolution
    {
        public XmlDocument PatchXml { get; set; } = new();

        public Riivolution()
        {
            XmlElement root = PatchXml.CreateElement("wiidisc");
            PatchXml.AppendChild(root);
            XmlElement patch = PatchXml.CreateElement("patch");
            PatchXml["wiidisc"].AppendChild(patch);
        }

        public Riivolution(string patch)
        {
            PatchXml.Load(patch);
        }

        public void AddMemoryPatch(uint offset, byte[] value)
        {
            XmlElement memoryPatch = PatchXml.CreateElement("memory");
            memoryPatch.SetAttribute("offset", $"0x{offset:X8}");
            memoryPatch.SetAttribute("value", $"{string.Join("", value.Select(b => $"{b:X2}"))}");
            PatchXml["wiidisc"]["patch"].AppendChild(memoryPatch);
        }

        public void AddMemoryFilesPatch(uint offset, string fileName)
        {
            XmlElement memoryFilePatch = PatchXml.CreateElement("memory");
            memoryFilePatch.SetAttribute("offset", $"0x{offset:X8}");
            memoryFilePatch.SetAttribute("valuefile", fileName);
            PatchXml["wiidisc"]["patch"].AppendChild(memoryFilePatch);
        }
    }
}
