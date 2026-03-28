using System.IO;
using System.Text.RegularExpressions;

namespace CopySave.Windows
{
    internal static class FileNameHelper
    {
        private static readonly Regex InvalidNameCharacters = new Regex("[<>:\"/\\\\|?*\\u0000-\\u001F]+", RegexOptions.Compiled);
        private static readonly Regex InvalidExtensionCharacters = new Regex("[^A-Za-z0-9_-]+", RegexOptions.Compiled);
        private static readonly Regex TrailingDotsAndSpaces = new Regex("[. ]+$", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.Compiled);

        public static string SanitizeName(string value)
        {
            var sanitized = (value ?? string.Empty).Trim();
            sanitized = InvalidNameCharacters.Replace(sanitized, string.Empty);
            sanitized = Whitespace.Replace(sanitized, " ");
            sanitized = TrailingDotsAndSpaces.Replace(sanitized, string.Empty);
            return string.IsNullOrWhiteSpace(sanitized) ? "clipboard" : sanitized;
        }

        public static string SanitizeExtension(string value)
        {
            var sanitized = (value ?? string.Empty).Trim().TrimStart('.');
            sanitized = InvalidExtensionCharacters.Replace(sanitized, string.Empty).ToLowerInvariant();
            return string.IsNullOrWhiteSpace(sanitized) ? "txt" : sanitized;
        }

        public static string BuildUniqueFilePath(string directory, string fileName, string extension)
        {
            var safeName = SanitizeName(fileName);
            var safeExtension = SanitizeExtension(extension);
            var candidatePath = Path.Combine(directory, safeName + "." + safeExtension);
            var counter = 2;

            while (File.Exists(candidatePath))
            {
                candidatePath = Path.Combine(directory, safeName + "_" + counter + "." + safeExtension);
                counter += 1;
            }

            return candidatePath;
        }
    }
}
