using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    class ParameterManager : FundamentalSharedParserLogic
    {
        /// <summary>
        /// Called 1st
        /// Get all parameters that exist in a given statement 
        /// Given a statement xml node, select its column references, check if they are problem column references
        /// If so, add the parameter object to the resulting parameter list
        /// </summary>
        /// <param name="statement">xml statement node</param>
        /// <param name="nsmgr">namespace manager with showplan namespace loaded</param>
        /// <returns>List of resulting paramater objects representing all column references for the showplan statement</returns>
        protected internal List<Parameter> GetParametersForStatement(XmlNode statement, XmlNamespaceManager nsmgr)
        {
            string statementXPath = "shp:QueryPlan//shp:ParameterList/shp:ColumnReference";
            XmlNodeList parameterList = statement.SelectNodes(statementXPath, nsmgr);
            if (parameterList == null) { throw new ArgumentNullException(nameof(statement)); } //but does param list always exist..?
            IEnumerator parameter = parameterList.GetEnumerator();
            List<Parameter> resParams = new List<Parameter>();
            //for each column reference inside of ParameterList
            while (parameter.MoveNext())
            {
                XmlNode currentParam = (XmlNode)parameter.Current;
                Parameter param = GetParameterRootCauseIfExists(currentParam); // can return null if param is not probable root cause
                if (param != null)
                {
                    resParams.Add(param);
                }
            }
            return resParams;
        }

        /// <summary>
        /// When called, GetParameterRootCauseIfExists is called 2nd
        /// GetParameterRootCauseIfExists is called by GetParametersForStatement when there are parameter column references to iterate through
        /// Checks for parameter difference for given xml node with column reference 
        /// Checks if compiled and runtime values differ or if compiled value is missing
        /// If so, this column reference is a problem, a new parameter object is created
        /// This method is private bc called by GetParametersForStatement
        /// </summary>
        /// <param name="currentParameterNode">Xml node containing a parameter/column reference</param>
        /// <returns>Detect parameter difference, if exists return new parameter object, else returns null when parameter is not a probable root cause </returns>       
        private Parameter GetParameterRootCauseIfExists(XmlNode currentParameterNode)
        {
            string columnReference = GetStatementLabel(currentParameterNode, "Column");
            string compiledValue = GetStatementLabel(currentParameterNode, "ParameterCompiledValue");
            string runtimeValue = GetStatementLabel(currentParameterNode, "ParameterRuntimeValue");

            Boolean bothExist = !compiledValue.Equals(NotFound) && !runtimeValue.Equals(NotFound);

            //If both values are provided and the same, the rule does not apply. 
            //If NOT both values exist or if values are different, the rule applies. 
            if (!bothExist || !compiledValue.Equals(runtimeValue))
            {
                //then we have a problem with Parameter difference
                return new Parameter(columnReference, compiledValue.Equals(NotFound));
            }
            return null;
        }

        /// <summary>
        /// Called 2nd
        /// Finds all parent node ids of all column references and adds the found ids to the parameter
        /// </summary>
        /// <param name="statementNode">Xml node with statement tag</param>
        /// <param name="parametersFromList">List of found parameters with their column refrences</param>
        /// <param name="nsmgr">Namespace manager with showplan namespace loaded</param>
        /// <returns></returns>
        protected internal List<Parameter> GetParametersWithProblemNodeIds(XmlNode statementNode, List<Parameter> parametersFromList, XmlNamespaceManager nsmgr)
        {
            foreach (Parameter p in parametersFromList) // for each parameter 
            {
                string parameterName = p.ColumnReference;
                var columns = statementNode.SelectNodes($"//shp:ColumnReference[@Column='{parameterName}']", nsmgr); // find all refs to that param
                if (columns == null) continue;
                foreach (XmlElement column in columns) //for each param reference
                {
                    var c = column.ParentNode; // get first parent
                    while (c?.ParentNode != null) // while there is a parent
                    {
                        if (c.Attributes?["NodeId"] != null) // if the parent node has NodeId
                        {
                            var nodeId = c.Attributes["NodeId"].Value; // found NodeId
                            p.ProblemNodeIds.Add(Convert.ToInt32(nodeId)); // convert to int and add to list
                            // Found a ColumnReference-NodeId pair (column/nodeId)                   
                            break;
                        }
                        c = c.ParentNode; // go up to next parent
                    }
                }
            }
            return parametersFromList;
        }

        /// <summary>
        /// Called 3rd
        /// Given all parameters found and their associated id's, get a list of just the parameters associated with the given node id
        /// </summary>
        /// <param name="nodeId">Intager representation of the target node's id</param>
        /// <param name="allParametersAndNodeIds">List of all paramaters who have already been associated with their corresponding node id's</param>
        /// <returns>List of parameters for the given nodeId</returns>
        protected internal List<Parameter> GetParametersForNode(int nodeId, List<Parameter> allParametersAndNodeIds)
        {
            List<Parameter> result = new List<Parameter>();
            if (allParametersAndNodeIds.Count > 0)
            {
                foreach (Parameter parameter in allParametersAndNodeIds)
                {
                    if (parameter.ProblemNodeIds.Contains(nodeId))
                    {
                        result.Add(parameter);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Called 4th
        /// Adds corresponding problem prarameters to all relevant nodes
        /// For each parameter in the showplan node, for each of its problem node ids, find nodes with those ids and add the parameter
        /// </summary>
        /// <param name="spnode">Show Plan Xml Node containing statement information including list of relOpNodes</param>
        protected internal void AddParametersToNodesWhoReferenceThatParameter(ShowPlanXmlStatement spnode)
        {
            foreach (Parameter param in spnode.ParameterColumnReferencesWithIssue)
            {
                foreach (int problemId in param.ProblemNodeIds) //check each problem node for each parameter
                {
                    RelOpNode nodeToAddRootCauseTo = GetNodeWithId(spnode.ProblemNodes, problemId); //returns null when not found
                    if (nodeToAddRootCauseTo == null) //node not found
                    {
                        nodeToAddRootCauseTo = GetNodeWithId(spnode.AllRelOpNodes, problemId); //returns null when not found
                        //if this is null...there's nothing to add problems to...
                        if (nodeToAddRootCauseTo == null)
                        {
                            return;
                        }
                    }
                    RootCause rc = new RootCause("Parameter");
                    rc.SetParameterRootCause(param);
                    nodeToAddRootCauseTo.AddNodeRootCause(rc);
                }
            }
        }

        /// <summary>
        /// Called last/6th when there are nodes who reference parameters
        /// Given a list of RelOpNode and an int node id, return the corresponding RelOpNode with that id
        /// </summary>
        /// <param name="resNodes">List of AllRelOpNodes populated with their ids</param>
        /// <param name="id">An integer representation of the target nodeId</param>
        /// <returns>The RelOpNode whose id matches the inputted id</returns>
        protected internal RelOpNode GetNodeWithId(List<RelOpNode> resNodes, int id)
        {
            foreach (RelOpNode node in resNodes)
            {
                if (node.NodeId.Equals(id))
                {
                    return node;
                }
            }
            return null;
        }
    }
}
