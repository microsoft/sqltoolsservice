//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecutionGraph
{
    /// <summary>
    /// Parses stytement type ShowPlan XML nodes
    /// </summary>
    internal class StatementParser : XmlPlanParser
    {
        /// <summary>
        /// Creates new node and adds it to the graph.
        /// </summary>
        /// <param name="item">Item being parsed.</param>
        /// <param name="parentItem">Parent item.</param>
        /// <param name="parentNode">Parent node.</param>
        /// <param name="context">Node builder context.</param>
        /// <returns>The node that corresponds to the item being parsed.</returns>
        public override Node GetCurrentNode(object item, object parentItem, Node parentNode, NodeBuilderContext context)
        {
            return NewNode(context);
        }

        /// <summary>
        /// Determines Operation that corresponds to the object being parsed.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Operation that corresponds to the node.</returns>
        protected override Operation GetNodeOperation(Node node)
        {
            object statementType = node["StatementType"];
            Operation statement = statementType != null
                ? OperationTable.GetStatement(statementType.ToString())
                : Operation.Unknown;


            return statement;
        }

        /// <summary>
        /// Determines node subtree cost from existing node properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Node subtree cost.</returns>
        protected override double GetNodeSubtreeCost(Node node)
        {
            object value = node["StatementSubTreeCost"];
            return value != null ? Convert.ToDouble(value, CultureInfo.CurrentCulture) : 0;
        }

        protected override bool ShouldParseItem(object parsedItem)
        {
            // Special case. An empty statement without QueryPlan but with 
            // a UDF or StoreProc should be skipped
            StmtSimpleType statement = parsedItem as StmtSimpleType;
            if (statement != null)
            {
                // We use hidden wrapper statements for UDFs and StoredProcs
                // Hidden statements don't have any of their properties defined
                // We can use one of properties which is always set by server
                // such as StatementIdSpecified to distinguish between a real
                // statement and a hidden wrapper statement
                if (!statement.StatementIdSpecified)
                {
                    return false;
                }
            }

            // By default, the statement is parsed
            return true;
        }

        /// <summary>
        /// Enumerates FunctionType blocks and removes all items from UDF and StoredProc properties.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public override IEnumerable<FunctionTypeItem> ExtractFunctions(object parsedItem)
        {
            StmtSimpleType statement = parsedItem as StmtSimpleType;
            if (statement != null)
            {
                // If this is a simple statement it may have UDF and StoredProc fields
                if (statement.UDF != null)
                {
                    foreach (FunctionType function in statement.UDF)
                    {
                        yield return new FunctionTypeItem(function, FunctionTypeItem.ItemType.Udf);
                    }
                    statement.UDF = null;
                }

                if (statement.StoredProc != null)
                {
                    yield return new FunctionTypeItem(statement.StoredProc, FunctionTypeItem.ItemType.StoredProcedure);
                    statement.StoredProc = null;
                }
            }
            else
            {
                // This is some other type of Statement. Call ExtractFunctions for all its children
                foreach (object item in GetChildren(parsedItem))
                {
                    XmlPlanParser parser = XmlPlanParserFactory.GetParser(item.GetType());
                    foreach (FunctionTypeItem functionItem in parser.ExtractFunctions(item))
                    {
                        yield return functionItem;
                    }
                }
            }

            yield break;
        }

        /// <summary>
        /// protected constructor prevents this object from being externally instantiated
        /// </summary>
        protected StatementParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static StatementParser statementParser = null;
        public static StatementParser Instance
        {
            get
            {
                if (statementParser == null)
                {
                    statementParser = new StatementParser();
                }
                return statementParser;
            }
        }
    }
}
