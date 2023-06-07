//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    public interface IConnectionManager
    {
        bool TryGetValue(string ownerUri, out ConnectionInfo info);
        bool ContainsKey(string ownerUri);
        bool TryAdd(string ownerUri, ConnectionInfo connectionInfo);
        bool TryRemove(string ownerUri);
    }
}