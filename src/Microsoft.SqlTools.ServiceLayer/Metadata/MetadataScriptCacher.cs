//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Logger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    public static class MetadataScriptCacher
    {
        public static void WriteToCache(string serverName, StringCollection scripts)
        {
            var tempFileName = $"{serverName}.tmp";

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                using (StreamWriter sw = new StreamWriter(tempFilePath, true))
                {
                    foreach (var script in scripts)
                    {
                        if (script != null)
                        {
                            sw.WriteLine(script);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Warning, $"Failed to write metadata to cache. Error: {ex.Message}");
            }
        }

        public static StringCollection ReadCache(string serverName)
        {
            var tempFileName = $"{serverName}.tmp";
            var stringCollection = new StringCollection();

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (!File.Exists(tempFilePath))
                {
                    return stringCollection;
                }

                using (StreamReader sr = new StreamReader(tempFilePath))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (line != null)
                        {
                            stringCollection.Add(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Warning, $"Failed to read metadata from cache. Error: {ex.Message}");
            }

            return stringCollection;
        }
    }
}
