//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// This code is copied from the source described in the comment below.

// =======================================================================================
// Microsoft Windows Server AppFabric Customer Advisory Team (CAT) Best Practices Series
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/appfabriccat/ and  copied from
// sqlmain ./sql/manageability/mfx/common/
//
// =======================================================================================
// Copyright © 2012 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// =======================================================================================

// namespace Microsoft.SQL.CAT.BestPractices.SqlAzure.Framework
// namespace Microsoft.SqlServer.Management.Common

using System;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// Defines a arguments for event handler which will be invoked whenever a retry condition is encountered.
    /// </summary>
    internal sealed class RetryCallbackEventArgs : EventArgs
    {
        private readonly int _retryCount;
        private readonly Exception _exception;
        private readonly TimeSpan _delay;

        public RetryCallbackEventArgs(int retryCount, Exception exception, TimeSpan delay)
        {
            _retryCount = retryCount;
            _exception = exception;
            _delay = delay;
        }

        public TimeSpan Delay
        {
            get { return _delay; }
        } 

        public Exception Exception
        {
            get { return _exception; }
        }

        public int RetryCount
        {
            get { return _retryCount; }
        }
    }
}
