using System;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Utility
{
    public class SelfCleaningFile : IDisposable
    {
        private bool disposed;

        public SelfCleaningFile()
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
