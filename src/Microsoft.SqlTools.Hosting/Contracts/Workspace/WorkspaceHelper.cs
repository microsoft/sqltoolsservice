using System.Text.RegularExpressions;

namespace Microsoft.SqlTools.Hosting.Contracts.Workspace
{
    public class WorkspaceHelper
    {
        public static bool IsPathInMemoryOrNonFileUri(string path)
        {
            string scheme = GetScheme(path);
            if (!string.IsNullOrEmpty(scheme))
            {
                return !scheme.Equals("file");
            }
            return false;
        }
        
        public static string GetScheme(string uri)
        {
            string windowsFilePattern = @"^(?:[\w]\:|\\)";
            if (Regex.IsMatch(uri, windowsFilePattern))
            {
                // Handle windows paths, these conflict with other "URI" handling
                return null;
            }

            // Match anything that starts with xyz:, as VSCode send URIs in the format untitled:, git: etc.
            string pattern = "^([a-z][a-z0-9+.-]*):";
            Match match = Regex.Match(uri, pattern);
            if (match != null && match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
    }
}