//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Contracts.Internal
{
    /// <summary>
    /// Defines a message that is sent from the client to request
    /// that the server shut down.
    /// </summary>
    public class ShutdownRequest
    {
        public static readonly RequestType<object, object> Type = RequestType<object, object>.Create("shutdown");
    }
}

