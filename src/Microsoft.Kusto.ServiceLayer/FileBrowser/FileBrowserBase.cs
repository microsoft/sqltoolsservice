//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    public struct FileInfo
    {
        // Empty for folder
        public string fileName;
        public string folderName;
        public string path;
        // Includes file name in the path. Empty for folder.
        public string fullPath;
    }

    /// <summary>
    /// Base class for file browser
    /// </summary>
    public abstract class FileBrowserBase
    {
        private Enumerator enumerator = null;
        protected ServerConnection connection = null;

        protected Enumerator Enumerator
        {
            get
            {
                return this.enumerator = (this.enumerator ?? new Enumerator());
            }
        }

        /// <summary>
        /// Separator string for components of the file path. Defaults to \ for Windows and / for Linux
        /// </summary>
        internal char PathSeparator { get; set; }

        /// <summary>
        /// Returns the PathSeparator values of the Server.
        /// </summary>
        /// <returns>PathSeparator</returns>
        internal static char GetPathSeparator(Enumerator enumerator, ServerConnection connection)
        {
            var req = new Request();
            req.Urn = "Server";
            req.Fields = new[] { "PathSeparator" };
            string pathSeparator = string.Empty;

            using (DataSet ds = enumerator.Process(connection, req))
            {
                if (IsValidDataSet(ds))
                {
                    pathSeparator = Convert.ToString(ds.Tables[0].Rows[0][0], System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(pathSeparator))
            {
                pathSeparator = @"\";
            }

            return pathSeparator[0];
        }

        /// <summary>
        /// Enumerates the FileInfo objects associated with drives 
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal static IEnumerable<FileInfo> EnumerateDrives(Enumerator enumerator, ServerConnection connection)
        {
            // if not supplied, server name will be obtained from urn
            Request req = new Request();
            bool clustered = false;

            req.Urn = "Server/Information";
            req.Fields = new string[] { "IsClustered", "PathSeparator", "HostPlatform" };

            var pathSeparator = @"\";
            var hostPlatform = HostPlatformNames.Windows;
            try
            {
                using (DataSet ds = enumerator.Process(connection, req))
                {
                    if (IsValidDataSet(ds))
                    {
                        clustered = Convert.ToBoolean(ds.Tables[0].Rows[0][0],
                            CultureInfo.InvariantCulture);
                        pathSeparator = Convert.ToString(ds.Tables[0].Rows[0][1], CultureInfo.InvariantCulture);
                        hostPlatform = Convert.ToString(ds.Tables[0].Rows[0][2], CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (UnknownPropertyEnumeratorException)
            {
                //there can be no clusters on 7.0 server
            }

            // we need to issue different queries to get all fixed drives on a normal server, and
            // shared drives on a cluster
            req.Urn = clustered ? "Server/AvailableMedia[@SharedDrive=true()]" : "Server/Drive";
            req.Fields = new[] { "Name" };

            using (DataSet ds = enumerator.Process(connection, req))
            {
                if (IsValidDataSet(ds))
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        var fileInfo = new FileInfo
                        {
                            fileName = string.Empty,
                            path = Convert.ToString(ds.Tables[0].Rows[i][0], System.Globalization.CultureInfo.InvariantCulture)
                        };

                        // if we're looking at shared devices on a clustered server
                        // they already have \ on the drive
                        // sys.dm_os_enumerate_fixed_drives appends a \ on Windows for sql17+
                        if (!clustered && hostPlatform == HostPlatformNames.Windows && !fileInfo.path.EndsWith(pathSeparator))
                        {
                            fileInfo.path += pathSeparator;
                        }

                        yield return fileInfo;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates files and folders that are immediate children of the given path on the server
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="connection"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static IEnumerable<FileInfo> EnumerateFilesInFolder(Enumerator enumerator, ServerConnection connection, string path)
        {
            var request = new Request
            {
                Urn = "Server/File[@Path='" + Urn.EscapeString(path) + "']",
                Fields = new[] { "Name", "IsFile", "FullName" },
                OrderByList = new[]
                {
                    new OrderBy
                    {
                        Field = "IsFile"
                    },
                    new OrderBy
                    {
                        Field = "Name"
                    }
                }
            };

            using (DataSet ds = enumerator.Process(connection, request))
            {
                if (IsValidDataSet(ds))
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        bool isFile = Convert.ToBoolean((object)row[1], CultureInfo.InvariantCulture);
                        yield return new FileInfo
                        {
                            path = isFile ? path : Convert.ToString((object)row[2], CultureInfo.InvariantCulture),
                            fileName = isFile ? Convert.ToString((object)row[0], CultureInfo.InvariantCulture) : String.Empty,
                            folderName = isFile ? String.Empty : Convert.ToString((object)row[0], CultureInfo.InvariantCulture),
                            fullPath = isFile ? Convert.ToString((object)row[2], CultureInfo.InvariantCulture) : String.Empty
                        };
                    }
                }
            }
        }

        internal static bool IsValidDataSet(DataSet ds)
        {
            return (ds != null 
                && ds.Tables != null 
                && ds.Tables.Count > 0 
                && ds.Tables[0].Rows != null 
                && ds.Tables[0].Rows.Count > 0) ;
        }
    }
}
