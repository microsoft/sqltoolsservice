//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    /// <summary>
    /// Extension of graph with some handy included methods specific for ShowPlan use
    /// </summary>
    public class ShowPlanGraph : Graph
    {
        private Dictionary<Node, object> nodeStmtMap = new Dictionary<Node, object>();

        public Dictionary<Node, object> NodeStmtMap
        {
            get { return this.nodeStmtMap; }
        }

        /// <summary>
        /// Gets the SQL Statement for this graph.
        /// </summary>
        public string Statement
        {
            get
            {
                // Special case: in the case of UDF or SP graphs thr root node doesn't
                // have StatementText. We should use Procedure Name instead
                return RootNode["StatementText"] as string ?? RootNode["ProcName"] as string ?? "";
            }
        }

        /// <summary>
        /// The StatementId as recorded in the RootNode for this graph, -1 if not available
        /// </summary>
        public int StatementId
        {
            get
            {
                return PullIntFromRoot("StatementId");
            }
        }

        /// <summary>
        /// The StatementCompId as recorded in the RootNode for this graph, -1 if not available
        /// </summary>
        public int StatementCompId
        {
            get
            {
                return PullIntFromRoot("StatementCompId");
            }
        }

        /// <summary>
        /// Contains the raw xml document for the graph. Used to save graphs. 
        /// </summary>
        public string XmlDocument { get; set; }

        /// <summary>
        /// The QueryPlanHash as recorded in the RootNode for this graph, null if not available
        /// </summary>
        public string QueryPlanHash
        {
            get
            {
                return RootNode["QueryPlanHash"] as string;
            }
        }

        internal Node RootNode
        {
            get
            {
                return this.Root;
            }
        }

        /// <summary>
        /// Helper method to parse an XMLString and return the set of ShowPlan graphs for it
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static ShowPlanGraph[] ParseShowPlanXML(object showPlan, ShowPlanType type = ShowPlanType.Unknown)
        {
            // Create a builder compatible with the data source
            INodeBuilder nodeBuilder = NodeBuilderFactory.Create(showPlan, type);

            // Parse showplan data
            return nodeBuilder.Execute(showPlan);
        }

        private int PullIntFromRoot(string name)
        {
            string statementId = RootNode[name].ToString();

            if (statementId != null)
            {
                int id;
                if (Int32.TryParse(statementId, out id))
                {
                    return id;
                }
            }

            //error condition, return -1
            return -1;
        }
    }
}
