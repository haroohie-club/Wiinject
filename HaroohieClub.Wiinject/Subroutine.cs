namespace HaroohieClub.Wiinject;

internal class Subroutine(string name, uint address, ReplacementMode mode, string code, int size)
{
    public string Name { get; set; } = name;
    public uint Address { get; set; } = address;
    public ReplacementMode ReplacementMode { get; set; } = mode;
    public string Code { get; set; } = code;
    public int Size { get; set; } = size;

    public static ReplacementMode GetReplacementMode(string value)
    {
        return value.ToLower().Trim() switch
        {
            "hook" => ReplacementMode.Hook,
            "repl" => ReplacementMode.Repl,
            "ref" => ReplacementMode.Ref,
            _ => ReplacementMode.Unknown,
        };
    }
}

internal enum ReplacementMode
{
    Hook,
    Repl,
    Ref,
    Unknown,
}