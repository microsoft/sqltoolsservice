//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseViewContext : SqlObjectViewContext
    {
        public DatabaseViewContext(InitializeViewRequestParams parameters) : base(parameters)
        {
        }

        public override void Dispose()
        {
        }
    }
}