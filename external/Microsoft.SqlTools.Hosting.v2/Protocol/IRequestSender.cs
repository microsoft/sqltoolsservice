//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Interface for objects that can requests via the JSON RPC channel
    /// </summary>
    public interface IRequestSender
    {
        /// <summary>
        /// Sends a request over the JSON RPC channel. It will wait for a response to the message
        /// before completing.
        /// </summary>
        /// <param name="requestType">Configuration of the request to send</param>
        /// <param name="requestParams">Parameters for the request to send</param>
        /// <typeparam name="TParams">Type of the parameters for the request, defined by <paramref name="requestType"/></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        Task<TResult> SendRequest<TParams, TResult>(RequestType<TParams, TResult> requestType, TParams requestParams);
    }
}
