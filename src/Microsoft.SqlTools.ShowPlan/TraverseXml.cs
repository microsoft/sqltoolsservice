using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    public class TraverseXml : ParseXmlHelperMethods
    {
        ParameterManager parameterManager = new ParameterManager();

        /// <summary>
        /// Filter nodes based on Cardinality scenario and set each nodes' root probable cause list
        /// </summary>
        /// <param name="showPlanXmlStatement">Object containing relevant information for a given show plan xml statement</param>
        /// <returns>Object containing all relevant information, now including parameters, for a given show plan xml statement</returns>
        protected internal ShowPlanXmlStatement SetRootCausesForProblemNodes(ShowPlanXmlStatement showPlanXmlStatement)
        {
            ShowPlanNodesCardinalityThresholdScenarioAnalyzer na = new ShowPlanNodesCardinalityThresholdScenarioAnalyzer();
            na.AnalyzeNodesForScenario(showPlanXmlStatement); //  Analyzes the cardinality difference btwn estimate and actual numbers of rows 
            parameterManager.AddParametersToNodesWhoReferenceThatParameter(showPlanXmlStatement);   //  Set each relOpNode root cause lists
            return showPlanXmlStatement;
        }

        /// <summary>
        /// Get all values of all the relOpNodes in this showplan statement and 
        /// create RelOpNode instances for each and add all of them to the statement 
        /// </summary>
        /// <param name="dictionaryOfSelectedNodes">Dictionary of lists of selected nodes and their corresponding labels</param>
        /// <param name="spnode">ShowPlanNode containing information relevant to entire statement</param>
        /// <param name="xDocNamespaceMngr">Xdoc with xml loaded and namespace manager with showplan namespace loaded</param>
        /// <param name="allParametersFoundAndNodeIds">List of parameters found in showplan and their corresponding node id's</param>
        /// <returns>ShowPlanXml node populated with relOpNodes</returns>
        protected internal ShowPlanXmlStatement AddRelOpNodesToShowplan(Dictionary<string, XmlNodeList> dictionaryOfSelectedNodes, ShowPlanXmlStatement spnode, XdocAndNamespaceManager xDocNamespaceMngr, List<Parameter> allParametersFoundAndNodeIds)
        {
            XmlNodeList relOpList = dictionaryOfSelectedNodes["RelOp"];
            if (relOpList == null || relOpList.Count.Equals(0))
            { throw new ArgumentNullException(nameof(dictionaryOfSelectedNodes)); }
            Dictionary<string, HashSet<string>> nodeIdAndColRefs = new Dictionary<string, HashSet<string>>();
            Dictionary<string, int> nodeDepths = new Dictionary<string, int>();
            XmlNode firstRelOp = relOpList.Item(0);
            string firstNodeId = firstRelOp?.Attributes?["NodeId"] != null ? firstRelOp.Attributes["NodeId"].Value : "-1";
            Traverse(firstRelOp, firstNodeId, nodeIdAndColRefs);
            IEnumerator relOp = relOpList.GetEnumerator();
            while (relOp.MoveNext())
            {
                XmlNode currentRelOp = (XmlNode)relOp.Current;
                EstimateRowsAndExecutions estimateNumberOfRowsAndExecutions = GetEstimateNumberOfRowsAndExecutions(currentRelOp);
                double estimateNumberOfRows = estimateNumberOfRowsAndExecutions.EstimateNumberOfRows;
                double estimateExecutions = estimateNumberOfRowsAndExecutions.EstimateNumberOfExecutions;
                double actualNumberOfRows = GetActualNumberOfRows(currentRelOp, xDocNamespaceMngr.Nsmgr);
                string logicalOp = GetLogicalOp(currentRelOp);
                int nodeId = GetNodeId(currentRelOp);
                string nodeIdAsString = nodeId.ToString();
                string physicalOp = GetPhysicalOp(currentRelOp);
                HashSet<string> tableRefs = (nodeIdAndColRefs.ContainsKey(nodeIdAsString))
                    ? nodeIdAndColRefs[nodeIdAsString]
                    : new HashSet<string>();
                List<Parameter> paramsRefByNode = parameterManager.GetParametersForNode(nodeId, allParametersFoundAndNodeIds);
                RelOpNode relOpNode = new RelOpNode(estimateNumberOfRows, actualNumberOfRows, nodeId, null, estimateExecutions, logicalOp, physicalOp, tableRefs, paramsRefByNode);
                spnode.AddRelOpNode(relOpNode);
            }
            return spnode;
        }

        /// <summary>
        /// Dictionary safe add function for associating column reference table name(s) with the last seen nodeId
        /// </summary>
        /// <param name="tableName">Found column reference's table name value</param>
        /// <param name="nodeIdAndColRefsResultDict">Dictionary to be populated with nodeId's and their corresponding column references</param>
        /// <param name="lastNodeId">String literal representation of last seen node id</param>
        /// <returns>Dictionary with newly added nodeId and its corresponding column reference(s)</returns>
        private Dictionary<string, HashSet<string>> AddColumnReferenceToResultDict(string tableName, Dictionary<string, HashSet<string>> nodeIdAndColRefsResultDict, string lastNodeId)
        {
            if (tableName != null)
            {
                if (nodeIdAndColRefsResultDict.ContainsKey(lastNodeId))
                {
                    nodeIdAndColRefsResultDict[lastNodeId].Add(tableName);//add to dict, lastnodeid, tablename
                }
                else
                {
                    nodeIdAndColRefsResultDict.Add(lastNodeId, new HashSet<string> { tableName });//add to dict, lastnodeid, tablename
                }
            }
            return nodeIdAndColRefsResultDict;
        }

        /// <summary>
        /// If xml Node is RelOp, get it's Id and keep track of it
        /// Else if xml Node is ColumnReference associate it with the last seen NodeId
        /// </summary>
        /// <param name="currentNode">Current Xml node which we are iterating on</param>
        /// <param name="prevNodeId">String literal representation of last seen node id</param>
        /// <param name="nodeIdAndColRefsResultDict">Dictionary to be populated with nodeId's and their corresponding column references</param>
        /// <returns>The last seen node id</returns>
        private string DoNodeWork(XmlNode currentNode, string prevNodeId, Dictionary<string, HashSet<string>> nodeIdAndColRefsResultDict)
        {
            if (currentNode.Name.Equals("RelOp"))
            {
                prevNodeId = currentNode.Attributes?["NodeId"] != null ? currentNode.Attributes["NodeId"].Value : prevNodeId;
            }
            else if (currentNode.Name.Equals("ColumnReference"))
            {
                string tableName = currentNode.Attributes?["Table"]?.Value;
                if (tableName != null)
                {
                    nodeIdAndColRefsResultDict = AddColumnReferenceToResultDict(tableName, nodeIdAndColRefsResultDict, prevNodeId);
                }
            }
            return prevNodeId;
        }

        /// <summary>
        /// Traverses through xml, starting at the passed in xmlNode
        /// and populates result dictionary, nodeIdAndColRefsResultDict with nodeId's and their corresponding column references
        /// as well as populates the dictionary, nodeDepths with node id's and their corresponding depth in the xml
        /// </summary>
        /// <param name="xmlNode">Xml node where traversal begins</param>
        /// <param name="nodeId">First node Id, recursively changed/kept track of</param>
        /// <param name="nodeIdAndColRefsResultDict">Dictionary to be populated with nodeId's and their corresponding column references</param>
        public void Traverse(XmlNode xmlNode, string nodeId, Dictionary<string, HashSet<string>> nodeIdAndColRefsResultDict)
        {
            nodeId = DoNodeWork(xmlNode, nodeId, nodeIdAndColRefsResultDict);
            if (xmlNode.HasChildNodes)
            {
                foreach (XmlNode childXmlNode in xmlNode.ChildNodes)
                {
                    Traverse(childXmlNode, nodeId, nodeIdAndColRefsResultDict);
                }
            }
        }
    }
}
