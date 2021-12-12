using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Wiinject
{
    public class Riivolution
    {
        public XmlDocument PatchXml { get; set; }

        public Riivolution()
        {
            PatchXml = new();
			PatchXml.LoadXml(@"<wiidisc version=""1"">
	<id game=""R44J8P"" />
	<options>
		<section name=""Heiretsu Replacement"">
			<option name=""Heiretsu Replacement"">
				<choice name=""Enabled"">
					<patch id=""HeiretsuFolder"" />
				</choice>
			</option>
		</section>
	</options>
	<patch id=""HeiretsuFolder"">
		<folder external=""/Heiretsu/files"" recursive=""true"" />
		<folder external=""/Heiretsu/files"" disc=""/"" />

	</patch>
</wiidisc>");
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
