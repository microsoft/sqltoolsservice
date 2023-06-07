//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.Connection.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public interface IDataSourceFactory
    {
        IDataSource Create(ConnectionDetails connectionDetails, string ownerUri);
    }
}