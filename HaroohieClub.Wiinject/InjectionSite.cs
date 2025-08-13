using System.Collections.Generic;

namespace HaroohieClub.Wiinject;

public class InjectionSite
{
    public uint StartAddress { get; set; }
    public uint EndAddress { get; set; }
    public uint CurrentAddress => StartAddress + (uint)RoutineMashup.Count;
    public int Length => (int)(EndAddress - StartAddress + 4); // +4 for including the end address
    public List<byte> RoutineMashup { get; set; } = [];
}