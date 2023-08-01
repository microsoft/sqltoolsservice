//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Logger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    public static class MetadataScriptCacher
    {
        public static void WriteToCache(string serverName, IEnumerable<string> generatedScripts)
        {
            var tempFileName = $"{serverName}.tmp";
            var generatedScriptsList = generatedScripts.ToList();

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                if (Path.Exists(tempFilePath))
                {
                    var cachedScripts = MetadataScriptCacher.ReadCache(serverName).ToList();

                    // If the number of previously cached scripts is different then the cache should be refreshed
                    var cacheRefreshNeeded = cachedScripts.Count != generatedScriptsList.Count ? true : false;
                    
                    // No need to check if a refresh is needed, if it's already known that the cache needs to be refreshed
                    if (cacheRefreshNeeded != true)
                    {
                        foreach (var script in cachedScripts)
                        {
                            if (generatedScriptsList.IndexOf(script) == -1)
                            {
                                cacheRefreshNeeded = true;
                                break;
                            }
                        }
                    }
                    

                    if (cacheRefreshNeeded)
                    {
                        MetadataScriptCacher.WriteScripts(tempFilePath, generatedScriptsList);
                    }
                }
                else
                {
                    MetadataScriptCacher.WriteScripts(tempFilePath, generatedScriptsList);
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Warning, $"Failed to write metadata to cache. Error: {ex.Message}");
                throw ex;
            }
        }

        private static void WriteScripts(string tempFilePath, IEnumerable<string> scripts)
        {
            using (StreamWriter sw = new StreamWriter(tempFilePath, false))
            {
                foreach (var script in scripts)
                {
                    sw.WriteLine(script);
                }
            }
        }

        public static IEnumerable<string> ReadCache(string serverName)
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
                        if (line != null)
                        {
                            scripts.Add(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Warning, $"Failed to read metadata from cache. Error: {ex.Message}");
                throw ex;
            }

            return scripts;
        }
    }
}
