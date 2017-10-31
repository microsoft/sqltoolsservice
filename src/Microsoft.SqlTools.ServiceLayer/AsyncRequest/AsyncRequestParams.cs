//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.AsyncRequest
{
    /// <summary>
    /// The base class contains the properties to run a request async
    /// </summary>
    public class AsyncRequestParams
    {
        public ConnectionInfo ConnectionInfo { get; set; }

        public string OwnerUri { get; set; }
    }
}
