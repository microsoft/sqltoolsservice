//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class SmoQueryModelTests
    {
        IMultiServiceProvider serviceProvider;

        [SetUp]
        public void Setup()
        {
            // Given the extension type loader is set to find SmoCollectionQuerier objects
            var assembly = typeof(SmoQuerier).Assembly;
            serviceProvider = ExtensionServiceProvider.CreateFromAssembliesInDirectory(Path.GetDirectoryName(assembly.Location), new string[] { Path.GetFileName(assembly.Location) });
        }

        [Test]
        public void ShouldFindDatabaseQuerierFromRealPath()
        {
            // When I request a database compatible querier
            SmoQuerier querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(typeof(Database)));
            // Then I expect to get back the SqlDatabaseQuerier
            Assert.NotNull(querier);
            Assert.AreEqual(typeof(SqlDatabaseQuerier), querier.GetType());

            // And I expect the service provider to have been set by the extension code
            Assert.NotNull(querier.ServiceProvider);
        }
        
        [Test]
        public void ShouldFindQuerierIfInExtensionList()
        {
            VerifyQuerierLookup(typeof(Table), typeof(SqlTableQuerier), expectExists: true);
        }

        [Test]
        public void ShouldNotFindQuerierIfNotInExtensionList()
        {
            VerifyQuerierLookup(typeof(Database), null, expectExists: false);
        }

        [Test]
        public void SqlServerDdlTriggerQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(ServerDdlTrigger));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Test]
        public void SqlSynonymQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(Synonym));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Test]
        public void SqlTriggerQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(Trigger));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Test]
        public void SqlFullTextIndexQuerierShouldNotBeAvailableForSqlDw()
        {
            SmoQuerier querier = GetSmoQuerier(typeof(FullTextIndex));
            Assert.False(querier.ValidFor.HasFlag(ValidForFlag.SqlDw));
        }

        [Test]
        public void TableValuedFunctionsIncludeInlineFunctions()
        {
            var tableFactory = new TableValuedFunctionsChildFactory();
            var filters = tableFactory.Filters;
            Assert.True(filters.ToList().Any(filter => {
                var f = filter as NodePropertyFilter;
                return f.Values.Contains(UserDefinedFunctionType.Table) && f.Values.Contains(UserDefinedFunctionType.Inline);
            }));
        }

        private SmoQuerier GetSmoQuerier(Type querierType)
        {
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
                Assert.AreEqual(querierType, querier.GetType());
                Assert.NotNull(querier.ServiceProvider);
            }
            else
            {
                Assert.Null(querier);
            }
        }
    }
}
