//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
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
        private const short ScriptFileExpirationInDays = 10;

        /// <summary>
        /// This method writes the passed in scripts to a temporary file.
        /// </summary>
        /// <param name="serverName">The name of the server which will go on to become the name of the file.</param>
        /// <param name="databaseName">The name of the database context was generated for which will become part of the filename.</param>
        /// <param name="scripts">The generated scripts that will be written to the temporary file.</param>
        public static void Write(string serverName, string databaseName, IEnumerable<string> scripts)
        {
            var encodedServerAndDatabaseName = Base64Encode($"{serverName}_{databaseName}");
            var tempFileName = $"{encodedServerAndDatabaseName}.tmp";
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
            var encodedServerName = Base64Encode(serverName);
            var tempFileName = $"{encodedServerName}.tmp";
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
        /// Determines if the script file for a server is too old and needs to be updated
        /// </summary>
        /// <param name="serverName">The name of the file associated with the given server name.</param>
        /// <returns>True: The file was created within the expiration period; False: The script file needs to be created
        /// or updated because it is too old.</returns>
        public static bool IsScriptTempFileUpdateNeeded(string serverName)
        {
            var encodedServerName = Base64Encode(serverName);
            var tempFileName = $"{encodedServerName}.tmp";

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!File.Exists(tempFilePath))
                {
                    return true;
                }
                else
                {
                    /**
                     * Generated scripts don't need to be super up to date, so 30 days was chosen as the amount of time
                     * before the scripts are re-generated. This expiration date may change in the future,
                     * but for now this is what we're going with.
                     */
                    var lastWriteTime = File.GetLastWriteTime(tempFilePath);
                    var isUpdateNeeded = (DateTime.Now - lastWriteTime).TotalDays < ScriptFileExpirationInDays ? false : true;

                    return isUpdateNeeded;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to determine if the script file is older than {ScriptFileExpirationInDays} days. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Encodes a string to it's base 64 string representation.
        /// </summary>
        /// <param name="str">The string to base64 encode.</param>
        /// <returns>Base64 encoded string.</returns>
        private static string Base64Encode(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(bytes);
        }
    }
}
