//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Connection
{
    public class DataSourceConnectionFactoryTests
    {
        [Test]
        public void CreateDataSourceConnection_Returns_Connection()
        {
            var dataSourceFactoryMock = new Mock<IDataSourceFactory>(); 
            var connectionFactory = new DataSourceConnectionFactory(dataSourceFactoryMock.Object);
            var connection = connectionFactory.CreateDataSourceConnection(new ConnectionDetails(), "");
            
            Assert.IsNotNull(connection);
        }
    }
}