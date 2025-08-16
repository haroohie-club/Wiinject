using System.Collections.Generic;

namespace HaroohieClub.Wiinject;

/// <summary>
/// A class for the result of a Wiinject run
/// </summary>
public class WiinjectResult
{
    /// <summary>
    /// A set of output binary patches to write to disk
    /// </summary>
    public Dictionary<string, byte[]> OutputBinaryPatches { get; set; } = [];
    /// <summary>
    /// The output Riivolution patch
    /// </summary>
    public Riivolution? OutputRiivolution { get; set; }
}