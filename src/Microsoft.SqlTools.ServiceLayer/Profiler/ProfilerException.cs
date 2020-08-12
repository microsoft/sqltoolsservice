//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    class ProfilerException : Exception
    {
        public ProfilerException(string message) : base(message)
        {
        }

        public ProfilerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
