//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

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
                ValidFor = ValidForFlag.Sql2016 | ValidForFlag.Sql2017 | ValidForFlag.Sql2019 | ValidForFlag.Sql2022OrHigher | ValidForFlag.AzureV12,
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
                ValidFor = ValidForFlag.Sql2022OrHigher | ValidForFlag.AzureV12,
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
            var orNode = new NodeOrFilter
            {
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
            var orNode = new NodeOrFilter
            {
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
            Assert.That(sql2016ServerVersion, Is.EqualTo(expectedSql2016Filters), "GetPropertyFilter did not construct the URN filter string as expected when excluding filters that aren't valid for the given server type.");

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
            var orNode = new NodeOrFilter
            {
                FilterList = new List<NodePropertyFilter> {
                    TemporalFilter,
                    LedgerHistoryFilter
                }
            };

            var orNode2 = new NodeOrFilter
            {
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
        [Test]
        public void TestAddPropertyFiltersToExistingURNs()
        {
            var Node = new List<NodePropertyFilter> {
                    TemporalFilter,
                    LedgerHistoryFilter
                };

            var nodeList = new List<INodeFilter> {
                new NodePropertyFilter(){
                    Property = "Schema",
                    Values = new List<object> {"jsdafl983!@$#%535343]]]][[["},
                    Type = typeof(string),
                    ValidFor = ValidForFlag.Sql2022OrHigher
                }
            };

            string allFiltersValid = INodeFilter.GetPropertyFilter(Node, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedAllFilters = "[(@TemporalType = 1) and (@LedgerType = 1)]";
            Assert.That(allFiltersValid, Is.EqualTo(expectedAllFilters), "GetPropertyFilter did not construct the URN filter string as expected");

            string newUrn = INodeFilter.AddPropertyFilterToFilterString(allFiltersValid, nodeList, typeof(SqlHistoryTableQuerier), ValidForFlag.Sql2022OrHigher);
            string expectedNewUrn = "[(@TemporalType = 1) and (@LedgerType = 1) and (@Schema = 'jsdafl983!@$#%535343]]]][[[')]";
            Assert.That(newUrn, Is.EqualTo(expectedNewUrn), "GetPropertyFilter did not construct the URN filter string as expected");
        }

        [Test]
        public void TestDateFilters()
        {
            // Testing date filter with equals
            var filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "2021-01-01" },
                    FilterType = FilterType.EQUALS,
                    IsDateTime = true
                }
            };

            string filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@CreateDate >= datetime('2021-01-01 00:00:00.000') and @CreateDate <= datetime('2021-01-01 23:59:59.999'))]"), "Error parsing date filter with equals operator");


            // Testing date filter with less than
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "2021-01-01" },
                    FilterType = FilterType.LESSTHAN,
                    IsDateTime = true
                }
            };

            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@CreateDate < datetime('2021-01-01 00:00:00.000'))]"), "Error parsing date filter with less than operator");

            // Testing date filter with greater than
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "2021-01-01" },
                    FilterType = FilterType.GREATERTHAN,
                    IsDateTime = true
                }
            };

            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@CreateDate > datetime('2021-01-01 23:59:59.999'))]"), "Error parsing date filter with greater than operator");

            // Testing date filter with between
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new string[] {"2021-01-01", "2021-01-02"}},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = true
                }
            };

            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@CreateDate >= datetime('2021-01-01 00:00:00.000') and @CreateDate <= datetime('2021-01-02 23:59:59.999'))]"), "Error parsing date filter with between operator");


            // Testing date filter with not equals
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "2021-01-01" },
                    FilterType = FilterType.NOTEQUALS,
                    IsDateTime = true,
                }
            };

            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(not(@CreateDate >= datetime('2021-01-01 00:00:00.000') and @CreateDate <= datetime('2021-01-01 23:59:59.999')))]"), "Error parsing date filter with not equals operator");

            // Testing date filter with not between

            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new string[] {"2021-01-01", "2021-01-02"}},
                    FilterType = FilterType.NOTBETWEEN,
                    IsDateTime = true
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(not(@CreateDate >= datetime('2021-01-01 00:00:00.000') and @CreateDate <= datetime('2021-01-02 23:59:59.999')))]"), "Error parsing date filter with not between operator");

            // Testing date filter LessThanOrEquals
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "2021-01-01" },
                    FilterType = FilterType.LESSTHANOREQUAL,
                    IsDateTime = true
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@CreateDate <= datetime('2021-01-01 23:59:59.999'))]"), "Error parsing date filter with LessThanOrEquals operator");

            // Testing date filter GreaterThanOrEquals
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "2021-01-01" },
                    FilterType = FilterType.GREATERTHANOREQUAL,
                    IsDateTime = true
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@CreateDate >= datetime('2021-01-01 00:00:00.000'))]"), "Error parsing date filter with GreaterThanOrEquals operator");


            // Testing date filter with invalid date
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "invalid value" },
                    FilterType = FilterType.EQUALS,
                    IsDateTime = true
                }
            };
            Assert.Throws<FormatException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown for creating a date sfc filter with invalid date");

            // Testing date filter with invalid date for between operator
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new string[] {"invalid value", "2021-01-02"}},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = true
                }
            };
            Assert.Throws<FormatException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown when value array contains invalid date value for between operator");
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> {"2021-01-02"},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = true
                }
            };
            Assert.Throws<InvalidCastException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown when only one date value is provided for between operator");
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "CreateDate",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new string[] {"2021-01-02"}},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = true
                }
            };
            Assert.Throws<IndexOutOfRangeException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown when only one value is provided in date array for between operator");
        }

        [Test]
        public void TextNumericFilters()
        {
            // Testing numeric filter with equals
            var filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "100" },
                    FilterType = FilterType.EQUALS,
                    IsDateTime = false
                }
            };

            string filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount = 100)]"), "Error parsing numeric filter with equals operator");

            // Testing numeric filter with less than
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { 100 },
                    FilterType = FilterType.LESSTHAN,
                    IsDateTime = false
                }
            };

            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount < 100)]"), "Error parsing numeric filter with less than operator");

            // Testing numeric filter with greater than
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { 100 },
                    FilterType = FilterType.GREATERTHAN,
                    IsDateTime = false
                }
            };

            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount > 100)]"), "Error parsing numeric filter with greater than operator");

            // Testing numeric filter with between
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new object[] {100, 200}},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = false
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount >= 100 and @RowCount <= 200)]"), "Error parsing numeric filter with between operator");

            // Testing numeric filter with not equals
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { 100 },
                    FilterType = FilterType.NOTEQUALS,
                    IsDateTime = false,
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount != 100)]"), "Error parsing numeric filter with not equals operator");

            // Testing numeric filter with not between
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new object[] {100, 200}},
                    FilterType = FilterType.NOTBETWEEN,
                    IsDateTime = false
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(not(@RowCount >= 100 and @RowCount <= 200))]"), "Error parsing numeric filter with not between operator");

            // Testing numeric filter LessThanOrEquals
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { 100 },
                    FilterType = FilterType.LESSTHANOREQUAL,
                    IsDateTime = false
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount <= 100)]"), "Error parsing numeric filter with LessThanOrEquals operator");

            // Testing numeric filter GreaterThanOrEquals
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { 100 },
                    FilterType = FilterType.GREATERTHANOREQUAL,
                    IsDateTime = false
                }
            };
            filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(@RowCount >= 100)]"), filterString, "Error parsing numeric filter with GreaterThanOrEquals operator");

            // Testing numeric filter with invalid value
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "invalid value" },
                    FilterType = FilterType.EQUALS,
                    IsDateTime = false
                }
            };
            Assert.Throws<FormatException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown for creating a numeric sfc filter with invalid number");

            // Testing numeric filter with invalid value for between operator
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new object[] {"invalid value", 200}},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = false
                }
            };
            Assert.Throws<FormatException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown for creating a numberic sfc filter with invalid array for between operator");
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> {200},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = false
                }
            };
            Assert.Throws<InvalidCastException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown when a single value is passed for between operator");
            filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "RowCount",
                    Type = typeof(int),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { new object[] {200}},
                    FilterType = FilterType.BETWEEN,
                    IsDateTime = false
                }
            };
            Assert.Throws<IndexOutOfRangeException>(() => INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All), "Error not thrown when the array contains single value for between operator");
        }

        [Test]
        public void TestContainsFilter()
        {
            // Testing text filter with ends with operator
            var filterList = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "Name",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { "test" },
                    FilterType = FilterType.CONTAINS,
                    IsDateTime = false
                }
            };
            var filterString = INodeFilter.GetPropertyFilter(filterList, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(filterString, Is.EqualTo("[(contains(@Name, 'test'))]"), "Error parsing text filter with contains operator");
        }

        public static IEnumerable<TestCaseData> ConvertExpandNodeFilterToNodeFilterTestCases
        {
            get
            {
                // Test case for equals operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.Equals,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = false,
                        FilterType = FilterType.EQUALS,
                        IsDateTime = false
                    }
                );

                // Test case for not equals operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.NotEquals,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = true,
                        FilterType = FilterType.EQUALS,
                        IsDateTime = false
                    }
                );

                // Test case for like operator with string value 
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.Contains,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = false,
                        FilterType = FilterType.CONTAINS,
                        IsDateTime = false
                    }
                );

                // Test case for not like operator with string value
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.NotContains,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = true,
                        FilterType = FilterType.CONTAINS,
                        IsDateTime = false
                    }
                );

                // Test case for date filter with equals operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Date",
                        Operator = NodeFilterOperator.Equals,
                        Value = "2020-01-01"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Date",
                        Type = NodeFilterPropertyDataType.Date
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Date",
                        Type = typeof(string),
                        Values = new List<object> { "2020-01-01" },
                        IsNotFilter = false,
                        FilterType = FilterType.EQUALS,
                        IsDateTime = true
                    }
                );

                // test case for date with between operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Date",
                        Operator = NodeFilterOperator.Between,
                        Value = JArray.FromObject(new string[] { "2020-01-01", "2020-01-02" })
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Date",
                        Type = NodeFilterPropertyDataType.Date
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Date",
                        Type = typeof(string),
                        Values = new List<object> { new string[] { "2020-01-01", "2020-01-02" } },
                        IsNotFilter = false,
                        FilterType = FilterType.BETWEEN,
                        IsDateTime = true
                    }
                );

                // test case for date with not between operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Date",
                        Operator = NodeFilterOperator.NotBetween,
                        Value = JArray.FromObject(new string[] { "2020-01-01", "2020-01-02" })
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Date",
                        Type = NodeFilterPropertyDataType.Date
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Date",
                        Type = typeof(string),
                        Values = new List<object> { new string[] { "2020-01-01", "2020-01-02" } },
                        IsNotFilter = true,
                        FilterType = FilterType.NOTBETWEEN,
                        IsDateTime = true
                    }
                );

                // test case for date with starts with operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.StartsWith,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = false,
                        FilterType = FilterType.STARTSWITH,
                        IsDateTime = false
                    }
                );

                // test case for date with not starts with operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.NotStartsWith,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = true,
                        FilterType = FilterType.STARTSWITH,
                        IsDateTime = false
                    }
                );

                // test case for date with ends with operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.EndsWith,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = false,
                        FilterType = FilterType.ENDSWITH,
                        IsDateTime = false
                    }
                );

                // test case for date with not ends with operator
                yield return new TestCaseData(
                    new NodeFilter()
                    {
                        Name = "Name",
                        Operator = NodeFilterOperator.NotEndsWith,
                        Value = "test"
                    },
                    new NodeFilterProperty()
                    {
                        Name = "Name",
                        Type = NodeFilterPropertyDataType.String
                    },
                    new NodePropertyFilter()
                    {
                        Property = "Name",
                        Type = typeof(string),
                        Values = new List<object> { "test" },
                        IsNotFilter = true,
                        FilterType = FilterType.ENDSWITH,
                        IsDateTime = false
                    }
                );
            }
        }

        [Test]
        [TestCaseSource("ConvertExpandNodeFilterToNodeFilterTestCases")]
        public void TestConvertExpandNodeFilterToNodeFilter(NodeFilter filter, NodeFilterProperty prop, INodeFilter expectedParsedFilter)
        {
            INodeFilter actualParsedFilter = ObjectExplorerUtils.ConvertExpandNodeFilterToNodeFilter(filter, prop);
            Assert.That(
                JsonConvert.SerializeObject(actualParsedFilter),
                Is.EqualTo(JsonConvert.SerializeObject(expectedParsedFilter)),
                $"Error parsing node filter '{prop.Name} ({prop.Type.ToString()})' with operator '{filter.Operator.ToString()}' and value '{filter.Value}'"
            );
        }

        [Test]
        [TestCase("test''test", "[(@Name = 'test''''test')]", "[(like(@Name, 'test''''test%'))]")]
        [TestCase("test'test", "[(@Name = 'test''test')]", "[(like(@Name, 'test''test%'))]")]
        [TestCase("test'test'test", "[(@Name = 'test''test''test')]", "[(like(@Name, 'test''test''test%'))]")]
        [TestCase("test'[test]test", "[(@Name = 'test''[test]test')]", "[(like(@Name, 'test''[[]test]test%'))]")]
        [TestCase("test^][%test", "[(@Name = 'test^][%test')]", "[(like(@Name, 'test[^]][[][%]test%'))]")]
        [TestCase("test%test", "[(@Name = 'test%test')]", "[(like(@Name, 'test[%]test%'))]")]
        [TestCase("test[test", "[(@Name = 'test[test')]", "[(like(@Name, 'test[[]test%'))]")]
        [TestCase("test]test", "[(@Name = 'test]test')]", "[(like(@Name, 'test]test%'))]")]
        [TestCase("test%%'%%test", "[(@Name = 'test%%''%%test')]", "[(like(@Name, 'test[%][%]''[%][%]test%'))]")]
        public void TestFilterValuesWithSpecialCharacters(string input, string expectedFilterValue, string expectedLikeFilterValue)
        {
            var nonLikeFilter = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "Name",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { input },
                    FilterType = FilterType.EQUALS,
                    IsDateTime = false
                }
            };

            var actualFilterValue = INodeFilter.GetPropertyFilter(nonLikeFilter, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(actualFilterValue, Is.EqualTo(expectedFilterValue));

            var likeFilter = new List<NodePropertyFilter>
            {
                new NodePropertyFilter()
                {
                    Property = "Name",
                    Type = typeof(string),
                    ValidFor = ValidForFlag.All,
                    Values = new List<object> { input },
                    FilterType = FilterType.STARTSWITH,
                    IsDateTime = false
                }
            };

            var actualLikeFilterValue = INodeFilter.GetPropertyFilter(likeFilter, typeof(SqlHistoryTableQuerier), ValidForFlag.All);
            Assert.That(actualLikeFilterValue, Is.EqualTo(expectedLikeFilterValue));
        }
    }
}
