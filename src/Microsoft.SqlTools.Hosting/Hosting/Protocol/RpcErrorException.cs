//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using StreamJsonRpc;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public static class RpcErrorException
    {
        public static LocalRpcException Create(string errorMessage, int errorCode = 0, string data = null)
        {
            return new LocalRpcException(errorMessage)
            {
                ErrorCode = errorCode,
                ErrorData = data
            };
        }

        public static LocalRpcException Create(Exception exception)
        {
            return new LocalRpcException(exception.Message, exception)
            {
                ErrorCode = exception.HResult,
                ErrorData = exception.StackTrace
            };
        }
    }
}
