//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Helper static class for the BrowseFolder dialog
    /// </summary>
    internal static class BrowseFolderHelper
    {
        /// <summary>
        /// Get the initial directory for the browse folder dialog
        /// </summary>
        /// <param name="serverConnection">The connection to the server</param>
        /// <returns></returns>
        public static string GetBrowseStartPath(ServerConnection    serverConnection)
        {
            string result = String.Empty;
    
            // if (US.Current.SSMS.TaskForms.ServerFileSystem.LastPath.TryGetValue(serverConnection.TrueName, out result))
            // {
            //     return result;
            // }

            if ((result == null) || (result.Length == 0))
            {
                // try and fetch the default location from SMO...
                Microsoft.SqlServer.Management.Smo.Server server = new Microsoft.SqlServer.Management.Smo.Server(serverConnection);
                result = server.Settings.DefaultFile;
            
                if ((result == null) || (result.Length == 0))
                {
                    // if the default file property doesn't return a string, 
                    // use the location of the model database's data file.
                    Enumerator  enumerator  = new Enumerator();
                    Request     request     = new Request();    
                    request.Urn             = "Server/Database[@Name='model']/FileGroup[@Name='PRIMARY']/File";
                    request.Fields          = new string[1] {"FileName"};
                    DataSet     dataSet     = enumerator.Process(serverConnection, request);

                    if (0 < dataSet.Tables[0].Rows.Count)
                    {
                        string path = dataSet.Tables[0].Rows[0][0].ToString();
                        result = PathWrapper.GetDirectoryName(path);
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Static class with Utility functions dealing with filenames
    /// </summary>
    internal static class FileNameHelper
    {
        /// <summary>
        /// Checks whether a filename has invalid characters
        /// </summary>
        /// <param name="testName">filename to check</param>
        /// <returns>true if filename has only valid characters</returns>
        internal static bool IsValidFilename(string testName)
        {
            bool isValid = false;
            if (!string.IsNullOrEmpty(testName))
            {
                Regex containsBadCharacter = new Regex("[" + Regex.Escape(new String(Path.GetInvalidFileNameChars())) + "]");
                isValid = !containsBadCharacter.IsMatch(testName);
            }
            return isValid;
        }
    }
}
