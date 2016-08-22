//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Hosting.Contracts
{
    public class VersionRequest
    {
        public static readonly
           RequestType<string, string> Type =
           RequestType<string, string>.Create("version");
    }
}
