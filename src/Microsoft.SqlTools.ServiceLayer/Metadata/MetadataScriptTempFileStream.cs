//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    /// <summary>
    /// This class is responsible for reading, writing, and checking the validity of script files.
    /// </summary>
    public static class MetadataScriptTempFileStream
    {
        private const short NumOfDaysSinceLastWrite = 30;

        /// <summary>
        /// This method writes the passed in scripts to a temporary file.
        /// </summary>
        /// <param name="serverName">The name of the server which will go on to become the name of the file.</param>
        /// <param name="scripts">The generated scripts that will be written to the temporary file.</param>
        public static void Write(string serverName, IEnumerable<string> scripts)
        {
            var tempFileName = $"{serverName}.tmp";
            var generatedScripts = scripts.ToList();

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                using (StreamWriter sw = new StreamWriter(tempFilePath, false))
                {
                    foreach (var script in generatedScripts)
                    {
                        sw.WriteLine(script);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to write scripts to temporary file. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reads the scripts associated with the provided server name.
        /// </summary>
        /// <param name="serverName">The name of the server to retrieve the scripts for.</param>
        /// <returns>List containing all the scripts in the file.</returns>
        public static IEnumerable<string> Read(string serverName)
        {
            var tempFileName = $"{serverName}.tmp";
            var scripts = new List<string>();

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!File.Exists(tempFilePath))
                {
                    return scripts;
                }

                using (StreamReader sr = new StreamReader(tempFilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            scripts.Add(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to read scripts from temporary file. Error: {ex.Message}");
                throw;
            }

            return scripts;
        }

        /// <summary>
        /// Checks to see if the temporary file containing the scripts for a server is valid.
        /// </summary>
        /// <param name="serverName">The name of the file assosiated with the given server name.</param>
        /// <returns>Flag indiciating that the script file is valid.</returns>
        public static bool IsScriptTempFileValid(string serverName)
        {
            var tempFileName = $"{serverName}.tmp";

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!File.Exists(tempFilePath))
                {
                    return false;
                }
                else
                {
                    var lastWriteTime = File.GetLastWriteTime(tempFilePath);
                    return (DateTime.Now - lastWriteTime).TotalDays < NumOfDaysSinceLastWrite;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to determine if the script file is older than 30 days. Error: {ex.Message}");
                throw;
            }
        }
    }
}
