//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    public abstract class AddDatabaseReferenceParams : SqlProjectParams
    {
        public bool SuppressMissingDependencies { get; set; }


        public string? DatabaseVariable { get; set; }
    }
}