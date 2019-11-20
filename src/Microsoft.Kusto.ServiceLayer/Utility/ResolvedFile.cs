//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.Kusto.ServiceLayer.Utility
{
    /// <summary>
    /// Utility object holding a result of a file resolve action.
    ///
    /// Workspace APIs support reading from disk if a file hasn't been
    /// officially opened via VSCode APIs with a buffer. This is problematic
    /// in the case where it's not a file on disk as any attempt will cause
    /// an exception to be thrown.
    /// 
    /// To mitigate this a ResolvedFile object has an additional flag indicating
    /// if the file can be read from disk.
    /// </summary>
    internal class ResolvedFile
    {
        public ResolvedFile(string filePath, string clientUri, bool canReadFromDisk)
        {
            FilePath = filePath;
            ClientUri = clientUri;
            CanReadFromDisk = canReadFromDisk;
        }

        public string FilePath { get; private set; }

        public string ClientUri { get; private set; }

        public bool CanReadFromDisk { get; private set; }

        public string LowercaseClientUri
        {
            get
            {
                return ClientUri?.ToLower();
            }
        }
    }
}