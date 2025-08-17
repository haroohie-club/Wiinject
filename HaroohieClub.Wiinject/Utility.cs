using System.IO;

namespace HaroohieClub.Wiinject;

internal class Utility
{
    public static string PathCombineAgnostic(params string[] paths)
    {
        return Path.Combine(paths).Replace('\\', '/');
    }

    public static string PathRelativeAgnostic(string path1, string path2)
    {
        return Path.GetRelativePath(path1, path2).Replace('\\', '/');
    }
}