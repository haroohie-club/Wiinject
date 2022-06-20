using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Wiinject
{
    public class Riivolution
    {
        public XmlDocument PatchXml { get; set; } = new();

        public Riivolution(IEnumerable<string> patchIds)
        {
            XmlElement root = PatchXml.CreateElement("wiidisc");
            PatchXml.AppendChild(root);
            foreach (string patchId in patchIds)
            {
                if (PatchXml["wiidisc"].GetElementsByTagName("patch").Cast<XmlElement>().FirstOrDefault(x => x.Attributes["id"].Value == patchId && x.ParentNode.Name == "wiidisc") is null)
                {
                    XmlElement patch = PatchXml.CreateElement("patch");
                    patch.SetAttribute("id", patchId);
                    PatchXml["wiidisc"].AppendChild(patch);
                }
            }
        }

        public Riivolution(string riivolutionPatchDocument, IEnumerable<string> patchIds)
        {
            PatchXml.Load(riivolutionPatchDocument);
            foreach (string patchId in patchIds)
            {
                if (PatchXml["wiidisc"].GetElementsByTagName("patch").Cast<XmlElement>().FirstOrDefault(x => x.Attributes["id"].Value == patchId && x.ParentNode.Name == "wiidisc") is null)
                {
                    XmlElement patch = PatchXml.CreateElement("patch");
                    patch.SetAttribute("id", patchId);
                    PatchXml["wiidisc"].AppendChild(patch);
                }
            }
        }

        public void AddMemoryPatch(uint offset, byte[] value, string patchId)
        {
            XmlElement parent = PatchXml["wiidisc"].GetElementsByTagName("patch").Cast<XmlElement>().First(x => x.Attributes["id"].Value == patchId && x.ParentNode.Name == "wiidisc");

            XmlElement memoryPatch = PatchXml.CreateElement("memory");
            memoryPatch.SetAttribute("offset", $"0x{offset:X8}");
            memoryPatch.SetAttribute("value", $"{string.Join("", value.Select(b => $"{b:X2}"))}");
            parent.AppendChild(memoryPatch);
        }

        public void AddMemoryFilesPatch(uint offset, string fileName, string patchId)
        {
            XmlElement parent = PatchXml["wiidisc"].GetElementsByTagName("patch").Cast<XmlElement>().First(x => x.Attributes["id"].Value == patchId && x.ParentNode.Name == "wiidisc");

            XmlElement memoryFilePatch = PatchXml.CreateElement("memory");
            memoryFilePatch.SetAttribute("offset", $"0x{offset:X8}");
            memoryFilePatch.SetAttribute("valuefile", fileName);
            parent.AppendChild(memoryFilePatch);
        }
    }
}
