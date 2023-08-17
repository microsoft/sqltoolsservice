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
    public static class MetadataScriptTempFileStream
    {
        private const string DirectoryName = "TableAndViewScripts";

        public static void Write(string serverName, IEnumerable<string> scripts)
        {
            var tempFileName = $"{DirectoryName}/{serverName}.tmp";
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

        public static IEnumerable<string> Read(string serverName)
        {
            var tempFileName = $"{DirectoryName}/{serverName}.tmp";
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
                        if (line != null)
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

        public static bool IsScriptTempFileValid(string serverName)
        {
            var tempFileName = $"{DirectoryName}/{serverName}.tmp";

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
                    return (DateTime.Now - lastWriteTime).TotalDays < 30;
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
