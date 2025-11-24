using System.IO;

namespace NuLigaGui.Utilities
{
    public static class PathExtensions
    {
        public static string ToValidFileName(this string input)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input.Replace(' ', '_');
        }
    }
}
