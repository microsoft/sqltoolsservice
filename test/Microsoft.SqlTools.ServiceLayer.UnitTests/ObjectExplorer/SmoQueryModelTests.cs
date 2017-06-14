//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class SmoQueryModelTests
    {

        [Fact]
        public void ShouldFindDatabaseQuerierFromRealPath()
        {
            // Given the extension type loader is set to find SmoCollectionQuerier objects
            IMultiServiceProvider serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
            // When I request a database compatible querier
            SmoQuerier querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(typeof(Database)));
            // Then I expect to get back the SqlDatabaseQuerier
            Assert.NotNull(querier);
            Assert.Equal(typeof(SqlDatabaseQuerier), querier.GetType());

            // And I expect the service provider to have been set by the extension code
            Assert.NotNull(querier.ServiceProvider);
        }
        
        [Fact]
        public void ShouldFindQuerierIfInExtensionList()
        {
            VerifyQuerierLookup(typeof(Table), typeof(SqlTableQuerier), expectExists: true);
        }

        [Fact]
        public void ShouldNotFindQuerierIfNotInExtensionList()
        {
            VerifyQuerierLookup(typeof(Database), null, expectExists: false);
        }

        [Fact]
        public void SqlServerDdlTriggerQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(ServerDdlTrigger));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Fact]
        public void SqlSynonymQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(Synonym));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Fact]
        public void SqlTriggerQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(Trigger));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Fact]
        public void SqlFullTextIndexQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(FullTextIndex));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        private SmoQuerier GetSmoQuerier(Type querierType)
        {
            // Given the extension type loader is set to find SmoCollectionQuerier objects
            IMultiServiceProvider serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
            // When I request a compatible querier
            SmoQuerier querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(querierType));
            // Then I expect to get back the Querier
            Assert.NotNull(querier);

            // And I expect the service provider to have been set by the extension code
            Assert.NotNull(querier.ServiceProvider);

            return querier;
        }

        private static void VerifyQuerierLookup(Type smoType, Type querierType, bool expectExists)
        {
            ExtensionServiceProvider serviceProvider = ExtensionServiceProvider.Create(new Type[] {
                typeof(SqlTableQuerier),
                typeof(SqlLinkedServerQuerier)
            });
            SmoQuerier querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(smoType));
            if (expectExists)
            {
                Assert.NotNull(querier);
                Assert.Equal(querierType, querier.GetType());
                Assert.NotNull(querier.ServiceProvider);
            }
            else
            {
                Assert.Null(querier);
            }
        }

    }
}
