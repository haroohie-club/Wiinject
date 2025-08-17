using System.Collections.Generic;

namespace HaroohieClub.Wiinject;

/// <summary>
/// Representation of a site to inject code
/// </summary>
public class InjectionSite
{
    /// <summary>
    /// The start address for assembled code to be injected
    /// </summary>
    public uint StartAddress { get; set; }
    /// <summary>
    /// The end address for assembled code to be injected
    /// </summary>
    public uint EndAddress { get; set; }
    /// <summary>
    /// The total length of the injection site
    /// </summary>
    public int Length => (int)(EndAddress - StartAddress + 4); // +4 for including the end address
    /// <summary>
    /// The directories this injection site includes
    /// </summary>
    public List<string> CodeDirs { get; set; } = [];
}