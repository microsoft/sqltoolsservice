//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public interface IRequestSender
    {
        Task<TResult> SendRequest<TParams, TResult>(RequestType<TParams, TResult> requestType, TParams requestParams,
            bool waitForResponse);
    }
}
