//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.Migration.Contracts
{
    /// <summary>
    /// Describes an item returned by SQL Assessment RPC methods
    /// </summary>
    public class ImpactedObjectInfo
    {
        public string Name { get; set; }
        public string ImpactDetail { get; set; }
        public string ObjectType { get; set; }
    }
}
