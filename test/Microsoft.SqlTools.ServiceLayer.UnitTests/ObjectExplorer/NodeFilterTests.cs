//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class NodeFilterTests
    {

        // Basic PropertyFilter definitions to use in tests
        public NodePropertyFilter TemporalFilter = 
            new NodePropertyFilter
                {
                    Property = "TemporalType",
                    Type = typeof(Enum),
                    TypeToReverse = typeof(SqlHistoryTableQuerier),
                    ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.Sql2019|ValidForFlag.Sql2022|ValidForFlag.AzureV12,
                    Values = new List<object>
                    {
                        { TableTemporalType.HistoryTable }
                    }
                };

        public NodePropertyFilter LedgerHistoryFilter = 
            new NodePropertyFilter
                {
                    Property = "LedgerType",
                    Type = typeof(Enum),
                    TypeToReverse = typeof(SqlHistoryTableQuerier),
                    ValidFor = ValidForFlag.Sql2022|ValidForFlag.AzureV12,
                    Values = new List<object>
                    {
                        { LedgerTableType.HistoryTable }
                    }
                };

        public NodePropertyFilter SystemObjectFilter =
            new NodePropertyFilter
                {
                    Property = "IsSystemObject",
                    Type = typeof(bool),
                    Values = new List<object> { 1 },
                };

        /// <summary>
        /// Validates the output of the ToPropertyFilterString for the NodeOrFilter class
        /// </summary>
        [Test]
        public void NodeOrFilterReturnsProperString()
        {
            var orNode = new NodeOrFilter {
                FilterList = new List<NodePropertyFilter> {
                    TemporalFilter,
                    LedgerHistoryFilter
                }
            };

            string validForBoth = orNode.ToPropertyFilterString(typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022);
            string expectedBothFilters = "((@TemporalType = 1) or (@LedgerType = 1))";
            Assert.AreEqual(expectedBothFilters, validForBoth);

            string validSql2016Only = orNode.ToPropertyFilterString(typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "((@TemporalType = 1))";
            Assert.AreEqual(expectedSql2016Filters, validSql2016Only);

            string invalidQuerierType = orNode.ToPropertyFilterString(typeof(SqlTableQuerier), ValidForFlag.Sql2022);
            Assert.AreEqual(string.Empty, invalidQuerierType);
        }

        /// <summary>
        /// Validates the output of ConcatProperties with an NodeOrFilter
        /// </summary>
        [Test]
        public void ConcatPropertiesWithNodeOrFilter()
        {
            var nodeList = new List<INodeFilter> {
                new NodeOrFilter {
                    FilterList = new List<NodePropertyFilter> {
                        TemporalFilter,
                        LedgerHistoryFilter
                    }
                }
            };

            string validForBoth = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022);
            string expectedBothFilters = "[((@TemporalType = 1) or (@LedgerType = 1))]";
            Assert.AreEqual(expectedBothFilters, validForBoth);
        }

        /// <summary>
        /// Validates the output of ConcatProperties with a list of NodePropertyFilters
        /// </summary>
        [Test]
        public void ConcatPropertiesWithNodePropertyFilters()
        {
            var nodeList = new List<INodeFilter> {
                TemporalFilter,
                LedgerHistoryFilter
            };

            string validForBoth = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022);
            string expectedBothFilters = "[(@TemporalType = 1) and (@LedgerType = 1)]";
            Assert.AreEqual(expectedBothFilters, validForBoth);

            string validSql2016Only = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "[(@TemporalType = 1)]";
            Assert.AreEqual(expectedSql2016Filters, validSql2016Only);

            string invalidQuerierType = INodeFilter.ConcatProperties(nodeList, typeof(SqlTableQuerier), ValidForFlag.Sql2022);
            Assert.AreEqual(string.Empty, invalidQuerierType);
        }

        /// <summary>
        /// Validates the output of ConcatProperties with a list of both NodePropertyFilters and NodeOrFilters
        /// </summary>
        [Test]
        public void ConcatPropertiesWithNodePropertyAndNodeOrFilters()
        {
            var orNode = new NodeOrFilter {
                FilterList = new List<NodePropertyFilter> {
                    TemporalFilter,
                    LedgerHistoryFilter
                }
            };

            var nodeList = new List<INodeFilter> {
                orNode,
                SystemObjectFilter
            };

            string validForBoth = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022);
            string expectedBothFilters = "[((@TemporalType = 1) or (@LedgerType = 1)) and (@IsSystemObject = 1)]";
            Assert.AreEqual(expectedBothFilters, validForBoth);

            string validSql2016Only = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "[((@TemporalType = 1)) and (@IsSystemObject = 1)]";
            Assert.AreEqual(expectedSql2016Filters, validSql2016Only);

            string invalidQuerierType = INodeFilter.ConcatProperties(nodeList, typeof(SqlTableQuerier), ValidForFlag.Sql2022);
            string expectedTableQuerierFilters = "[(@IsSystemObject = 1)]";
            Assert.AreEqual(expectedTableQuerierFilters, invalidQuerierType);
        }

        /// <summary>
        /// Validates the output of ConcatProperties with a list of multiple NodeOrFilters with more than 2 filters, and
        /// a lone NodePropertyFilter
        /// </summary>
        [Test]
        public void ConcatPropertiesWithMixedFilters()
        {
            // All these filters together are nonsense, but it's just testing the logic for constructing the filter string
            var orNode = new NodeOrFilter {
                FilterList = new List<NodePropertyFilter> {
                    TemporalFilter,
                    LedgerHistoryFilter
                }
            };

            var orNode2 = new NodeOrFilter {
                FilterList = new List<NodePropertyFilter> {
                    SystemObjectFilter,
                    LedgerHistoryFilter,
                    TemporalFilter
                }
            };

            var nodeList = new List<INodeFilter> {
                orNode,
                SystemObjectFilter,
                orNode2
            };

            string validForBoth = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022);
            string expectedBothFilters = "[((@TemporalType = 1) or (@LedgerType = 1)) and (@IsSystemObject = 1) and ((@IsSystemObject = 1) or (@LedgerType = 1) or (@TemporalType = 1))]";
            Assert.AreEqual(expectedBothFilters, validForBoth);

            string validSql2016Only = INodeFilter.ConcatProperties(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "[((@TemporalType = 1)) and (@IsSystemObject = 1) and ((@IsSystemObject = 1) or (@TemporalType = 1))]";
            Assert.AreEqual(expectedSql2016Filters, validSql2016Only);

            string invalidQuerierType = INodeFilter.ConcatProperties(nodeList, typeof(SqlTableQuerier), ValidForFlag.Sql2022);
            string expectedTableQuerierFilters = "[(@IsSystemObject = 1) and ((@IsSystemObject = 1))]";
            Assert.AreEqual(expectedTableQuerierFilters, invalidQuerierType);
        }
    }
}
