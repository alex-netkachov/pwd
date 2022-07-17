using System.Linq;

namespace pwd;

public static class Salted
{
    public const string Text = "Salted__";
    public static readonly byte[] Bytes = {0x53, 0x61, 0x6c, 0x74, 0x65, 0x64, 0x5f, 0x5f};

    public static bool Equals(
        byte[] bytes)
    {
        return bytes.Length == Bytes.Length && !Bytes.Where((t, i) => bytes[i] != t).Any();
    }
}