//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;

namespace Microsoft.SqlTools.JsonRpc.Utility
{
    public class SelfCleaningTempFile : IDisposable
    {
        private bool disposed;

        public SelfCleaningTempFile()
        {
            FilePath = Path.GetTempFileName();
        }

        public string FilePath { get; private set; }

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                try
                {
                    File.Delete(FilePath);
                }
                catch
                {
                    Console.WriteLine($"Failed to cleanup {FilePath}");
                }
            }

            disposed = true;
        }

        #endregion

    }
}
