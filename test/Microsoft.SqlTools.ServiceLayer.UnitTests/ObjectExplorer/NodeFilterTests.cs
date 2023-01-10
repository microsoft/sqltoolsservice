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
                    ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.Sql2019|ValidForFlag.Sql2022OrHigher|ValidForFlag.AzureV12,
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
                    ValidFor = ValidForFlag.Sql2022OrHigher|ValidForFlag.AzureV12,
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

            string allFiltersValid = orNode.ToPropertyFilterString(typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedAllFilters = "((@TemporalType = 1) or (@LedgerType = 1))";
            Assert.That(allFiltersValid, Is.EqualTo(expectedAllFilters), "ToPropertyFilterString did not construct the URN filter string as expected for NodeOrFilter");

            string sql2016ServerVersion = orNode.ToPropertyFilterString(typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "((@TemporalType = 1))";
            Assert.That(sql2016ServerVersion, Is.EqualTo(expectedSql2016Filters), "ToPropertyFilterString did not construct the URN filter string as expected when excluding filters that aren't valid for the given server type.");

            string invalidQuerierType = orNode.ToPropertyFilterString(typeof(SqlTableQuerier), ValidForFlag.Sql2022OrHigher);
            Assert.That(invalidQuerierType, Is.Empty, "ToPropertyFilterString should return empty string, because no given filters match the querier type provided.");
        }

        /// <summary>
        /// Validates the output of GetPropertyFilter with an NodeOrFilter
        /// </summary>
        [Test]
        public void GetPropertyFilterWithNodeOrFilter()
        {
            var nodeList = new List<INodeFilter> {
                new NodeOrFilter {
                    FilterList = new List<NodePropertyFilter> {
                        TemporalFilter,
                        LedgerHistoryFilter
                    }
                }
            };

            string allFiltersValid = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedAllFilters = "[((@TemporalType = 1) or (@LedgerType = 1))]";
            Assert.That(allFiltersValid, Is.EqualTo(expectedAllFilters), "GetPropertyFilter did not construct the URN filter string as expected");
        }

        /// <summary>
        /// Validates the output of GetPropertyFilter with a list of NodePropertyFilters
        /// </summary>
        [Test]
        public void GetPropertyFilterWithNodePropertyFilters()
        {
            var nodeList = new List<INodeFilter> {
                TemporalFilter,
                LedgerHistoryFilter
            };

            string allFiltersValid = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedAllFilters = "[(@TemporalType = 1) and (@LedgerType = 1)]";
            Assert.That(allFiltersValid, Is.EqualTo(expectedAllFilters), "GetPropertyFilter did not construct the URN filter string as expected");

            string sql2016ServerVersion = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "[(@TemporalType = 1)]";
            Assert.That(sql2016ServerVersion, Is.EqualTo(expectedSql2016Filters), "GetPropertyFilter did not construct the URN filter string as expected when excluding filters that aren't valid for the given server type.");

            string invalidQuerierType = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlTableQuerier), ValidForFlag.Sql2022OrHigher);
            Assert.That(invalidQuerierType, Is.Empty, "GetPropertyFilter should return empty string, because no given filters match the querier type provided.");
        }

        /// <summary>
        /// Validates the output of GetPropertyFilter with a list of both NodePropertyFilters and NodeOrFilters
        /// </summary>
        [Test]
        public void GetPropertyFilterWithNodePropertyAndNodeOrFilters()
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

            string allFiltersValid = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedAllFilters = "[((@TemporalType = 1) or (@LedgerType = 1)) and (@IsSystemObject = 1)]";
            Assert.That(allFiltersValid, Is.EqualTo(expectedAllFilters), "GetPropertyFilter did not construct the URN filter string as expected");

            string sql2016ServerVersion = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "[((@TemporalType = 1)) and (@IsSystemObject = 1)]";
            Assert.That(sql2016ServerVersion, Is.EqualTo(expectedSql2016Filters),  "GetPropertyFilter did not construct the URN filter string as expected when excluding filters that aren't valid for the given server type.");

            string invalidQuerierType = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedTableQuerierFilters = "[(@IsSystemObject = 1)]";
            Assert.That(invalidQuerierType, Is.EqualTo(expectedTableQuerierFilters), "GetPropertyFilter did not construct the URN filter string as expected when excluding filters that don't match the querier type.");
        }

        /// <summary>
        /// Validates the output of GetPropertyFilter with a list of multiple NodeOrFilters with more than 2 filters, and
        /// a lone NodePropertyFilter
        /// </summary>
        [Test]
        public void GetPropertyFilterWithMixedFilters()
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

            string allFiltersValid = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedAllFilters = "[((@TemporalType = 1) or (@LedgerType = 1)) and (@IsSystemObject = 1) and ((@IsSystemObject = 1) or (@LedgerType = 1) or (@TemporalType = 1))]";
            Assert.That(allFiltersValid, Is.EqualTo(expectedAllFilters), "GetPropertyFilter did not construct the URN filter string as expected");

            string sql2016ServerVersion = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2016);
            string expectedSql2016Filters = "[((@TemporalType = 1)) and (@IsSystemObject = 1) and ((@IsSystemObject = 1) or (@TemporalType = 1))]";
            Assert.That(sql2016ServerVersion, Is.EqualTo(expectedSql2016Filters), "GetPropertyFilter did not construct the URN filter string as expected when excluding filters that aren't valid for the given server type.");

            string invalidQuerierType = INodeFilter.GetPropertyFilter(nodeList, typeof(SqlTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedTableQuerierFilters = "[(@IsSystemObject = 1) and ((@IsSystemObject = 1))]";
            Assert.That(invalidQuerierType, Is.EqualTo(expectedTableQuerierFilters), "GetPropertyFilter did not construct the URN filter string as expected when excluding filters that don't match the querier type.");
        }
    }
}
