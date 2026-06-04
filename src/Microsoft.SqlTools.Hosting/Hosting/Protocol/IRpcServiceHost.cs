//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public interface IRpcServiceHost : IRpcMethodRegistry, IEventSender, IRequestSender
    {
    }
}
