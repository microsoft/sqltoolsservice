//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class SelfCleaningTempFile : IDisposable
    {
        private bool disposed;

        public SelfCleaningTempFile()
        {
            var fileName = $"{GetSafeCurrentTestName()}_{Guid.NewGuid():N}.tmp";
            FilePath = Path.Combine(Path.GetTempPath(), fileName);
            using (File.Create(FilePath))
            {
            }
            Console.WriteLine($"Created temp file {FilePath}");
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
                    Console.WriteLine($"Cleaned up temp file {FilePath}");
                }
                catch
                {
                    Console.WriteLine($"Failed to cleanup {FilePath}");
                }
            }

            disposed = true;
        }

        #endregion

        private static string GetSafeCurrentTestName()
        {
            var testName = TestContext.CurrentContext?.Test?.Name;
            if (string.IsNullOrWhiteSpace(testName))
            {
                return "tempfile";
            }

            var invalidFileNameChars = Path.GetInvalidFileNameChars().ToHashSet();
            var sanitized = new string(testName
                .Select(ch => invalidFileNameChars.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch)
                .ToArray())
                .Trim('_');

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "tempfile";
            }

            const int maxLength = 80;
            return sanitized.Length <= maxLength ? sanitized : sanitized.Substring(0, maxLength);
        }

    }
}
