using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace HaroohieClub.Wiinject;

/// <summary>
/// Class representing a Riivolution patch
/// </summary>
public class Riivolution
{
    /// <summary>
    /// The raw XML for the patch
    /// </summary>
    public XmlDocument PatchXml { get; set; } = new();

    /// <summary>
    /// Constructor for a Riivolution patch that takes various patch IDs to construct separate sections
    /// </summary>
    /// <param name="patchIds">The names of the different patches to include in the patch file</param>
    public Riivolution(IEnumerable<string> patchIds)
    {
        XmlElement root = PatchXml.CreateElement("wiidisc");
        PatchXml.AppendChild(root);
        foreach (string patchId in patchIds)
        {
            if (PatchXml["wiidisc"]?.GetElementsByTagName("patch").Cast<XmlElement>().FirstOrDefault(x => x.Attributes["id"]?.Value == patchId && x.ParentNode is
                {
                    Name: "wiidisc"
                }) is null)
            {
                XmlElement patch = PatchXml.CreateElement("patch");
                patch.SetAttribute("id", patchId);
                PatchXml["wiidisc"]?.AppendChild(patch);
            }
        }
    }

    /// <summary>
    /// Constructor for a Riivolution patch that takes patch IDs and an existing Riivolution patch to modify
    /// </summary>
    /// <param name="riivolutionPatchDocument">The path to the existing Riivolution patch document</param>
    /// <param name="patchIds">The names of the different patches to include in the patch file</param>
    public Riivolution(string riivolutionPatchDocument, IEnumerable<string> patchIds)
    {
        PatchXml.Load(riivolutionPatchDocument);
        foreach (string patchId in patchIds)
        {
            if (PatchXml["wiidisc"]?.GetElementsByTagName("patch").Cast<XmlElement>().FirstOrDefault(x => x.Attributes["id"]?.Value == patchId && x.ParentNode is
                {
                    Name: "wiidisc"
                }) is null)
            {
                XmlElement patch = PatchXml.CreateElement("patch");
                patch.SetAttribute("id", patchId);
                PatchXml["wiidisc"]?.AppendChild(patch);
            }
        }
    }

    /// <summary>
    /// Adds a memory patch to the Riivolution patch file
    /// </summary>
    /// <param name="offset">The offset to insert the memory patch</param>
    /// <param name="value">The binary assembled code to insert</param>
    /// <param name="patchId">The name of the patch section to insert the memory patch within</param>
    public void AddMemoryPatch(uint offset, byte[] value, string patchId)
    {
        XmlElement parent = PatchXml["wiidisc"]?.GetElementsByTagName("patch").Cast<XmlElement>().First(x => x.Attributes["id"]?.Value == patchId && x.ParentNode.Name == "wiidisc");

        XmlElement memoryPatch = PatchXml.CreateElement("memory");
        memoryPatch.SetAttribute("offset", $"0x{offset:X8}");
        memoryPatch.SetAttribute("value", $"{string.Join("", value.Select(b => $"{b:X2}"))}");
        parent?.AppendChild(memoryPatch);
    }

    /// <summary>
    /// Adds a memory patch that uses a binary file to the Riivolution patch file
    /// </summary>
    /// <param name="offset">The offset to insert the memory patch</param>
    /// <param name="fileName">The path to the assembled code file</param>
    /// <param name="patchId">The name of the patch section to insert the memory patch within</param>
    public void AddMemoryFilesPatch(uint offset, string fileName, string patchId)
    {
        XmlElement parent = PatchXml["wiidisc"]?.GetElementsByTagName("patch").Cast<XmlElement>().First(x => x.Attributes["id"]?.Value == patchId && x.ParentNode.Name == "wiidisc");

        XmlElement memoryFilePatch = PatchXml.CreateElement("memory");
        memoryFilePatch.SetAttribute("offset", $"0x{offset:X8}");
        memoryFilePatch.SetAttribute("valuefile", fileName);
        parent?.AppendChild(memoryFilePatch);
    }
}