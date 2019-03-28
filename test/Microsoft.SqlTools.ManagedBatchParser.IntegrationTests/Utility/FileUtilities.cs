using System.IO;

namespace Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.Utility
{
    internal class FileUtilities
    {
        /// <summary>
        /// Turns off the read-only attribute for this file
        /// </summary>
        /// <param name="fullFilePath"></param>
        internal static void SetFileReadWrite(string fullFilePath)
        {
            if (!string.IsNullOrEmpty(fullFilePath) &&
                File.Exists(fullFilePath))
            {
                File.SetAttributes(fullFilePath, FileAttributes.Normal);
            }
        }
    }
}