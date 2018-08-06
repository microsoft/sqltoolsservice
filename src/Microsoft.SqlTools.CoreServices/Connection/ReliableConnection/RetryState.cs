//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.CoreServices.Connection.ReliableConnection
{
    internal class RetryState 
    {
        private int _retryCount = 0;
        private TimeSpan _delay = TimeSpan.Zero;
        private Exception _lastError = null;
        private bool _isDelayDisabled = false;

        /// <summary>
        /// Gets or sets the current retry attempt count.
        /// </summary>
        public int RetryCount 
        {
            get
            {
                return _retryCount;
            }
            set
            {
                _retryCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the delay indicating how long the current thread will be suspended for before the next iteration will be invoked.
        /// </summary>
        public TimeSpan Delay 
        {
            get
            {
                return _delay;
            }
            set
            {
                _delay = value;
            }
        }

        /// <summary>
        /// Gets or sets the exception which caused the retry conditions to occur.
        /// </summary>
        public Exception LastError 
        {
            get
            {
                return _lastError;
            }
            set
            {
                _lastError = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we should ignore delay in order to be able to execute our tests faster
        /// </summary>
        /// <remarks>Intended for test use ONLY</remarks>
        internal bool IsDelayDisabled 
        { 
            get
            {
                return _isDelayDisabled;
            }
            set
            {
                _isDelayDisabled = value;
            }
        }

        public virtual void Reset()
        {
            this.IsDelayDisabled = false;
            this.RetryCount = 0;
            this.Delay = TimeSpan.Zero;
            this.LastError = null;
        }
    }
}