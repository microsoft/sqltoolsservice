//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// A wrapper around the ReaderWriterLock to make sure the locks are released even if the action fails
    /// </summary>
    public class AutoLock
    {
        private readonly ReaderWriterLock _lock;
        private readonly bool _isWriteLocked;

        /// <summary>
        /// Creates new lock given type of lock and timeput
        /// </summary>
        public AutoLock(ReaderWriterLock lockObj, bool isWriteLock, TimeSpan timeOut, Action action, out Exception exception)
        {
            exception = null;
            try
            {
                _lock = lockObj;
                _isWriteLocked = isWriteLock;
                if (_isWriteLocked)
                {
                    _lock.AcquireWriterLock(timeOut);
                }
                else
                {
                    _lock.AcquireReaderLock(timeOut);
                }
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                if (_isWriteLocked && _lock.IsWriterLockHeld)
                {
                    _lock.ReleaseWriterLock();
                }
                else if (!_isWriteLocked && _lock.IsReaderLockHeld)
                {
                    _lock.ReleaseReaderLock();
                }
            }
        }
    }
}
