using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    public class ParseXmlHelperMethods : FundamentalSharedParserLogic
    {

        /// <summary>
        /// Get a dictionary of key words and corresponding
        /// lists which point to different levels in the XML utilized to parse
        /// </summary>
        /// <param name="xdocNsmgr">xdoc with xml loaded and namespace with namespace loaded</param>
        /// <returns>Dictionary with lists of selected nodes and their corresponding labels</returns>
        protected internal Dictionary<string, XmlNodeList> GetDictionaryOfSelectedNodes(XdocAndNamespaceManager xdocNsmgr)
        {
            Dictionary<string, XmlNodeList> dictionaryOfSelectedNodes = new Dictionary<string, XmlNodeList>();
            
           
            XmlNode root = xdocNsmgr.Xdoc.DocumentElement;
           
          
            if (root == null) { throw new ArgumentNullException(nameof(root)); }

            // show plan batch sequence node
            XmlNode batchSequence = root.SelectSingleNode("descendant::shp:BatchSequence", xdocNsmgr.Nsmgr);
            if (batchSequence == null){throw new ArgumentNullException(nameof(xdocNsmgr));}

            // show plan statements node list
            XmlNodeList statementsList = batchSequence.SelectNodes("shp:Batch/shp:Statements", xdocNsmgr.Nsmgr);
            if (statementsList == null || statementsList.Count.Equals(0)) { throw new ArgumentNullException(nameof(statementsList)); }

            // show plan relOpNodes node list
            XmlNodeList relOpList = batchSequence.SelectNodes("shp:Batch/shp:Statements/shp:StmtSimple/shp:QueryPlan//shp:RelOp", xdocNsmgr.Nsmgr);
            if (relOpList == null || relOpList.Count.Equals(0)) { throw new ArgumentNullException(nameof(relOpList)); }

            // add lists of selected nodes to dictionary
            if (dictionaryOfSelectedNodes.ContainsKey("StmtSimple")) { dictionaryOfSelectedNodes["StmtSimple"] = statementsList; } 
            else { dictionaryOfSelectedNodes.Add("StmtSimple", statementsList); }
            if (dictionaryOfSelectedNodes.ContainsKey("RelOp")) { dictionaryOfSelectedNodes["RelOp"] = relOpList; } 
            else { dictionaryOfSelectedNodes.Add("RelOp", relOpList); }
            return dictionaryOfSelectedNodes;            
        }

        /// <summary>
        /// Given xml node with label RelOp get its actual number of rows as a double
        /// </summary>
        /// <param name="currentRelOp">Xml node with label RelOp</param>
        /// <param name="nsmgr">namespace manager with showplan namespace loaded</param>
        /// <returns>Double representation of actual number of rows upon execution</returns>
        protected internal double GetActualNumberOfRows(XmlNode currentRelOp, XmlNamespaceManager nsmgr)
        {
            string actualRowQuery = "shp:RunTimeInformation//shp:RunTimeCountersPerThread";
            XmlNodeList allActualNumberOfRows = currentRelOp.SelectNodes(actualRowQuery, nsmgr);
            Debug.Assert(allActualNumberOfRows != null, "actualRows != null");
            IEnumerator actualRowsEnumerator = allActualNumberOfRows.GetEnumerator();
            double actualNumberOfRowsFromSum = 0;
            while (actualRowsEnumerator.MoveNext())
            {
                XmlNode current = (XmlNode)actualRowsEnumerator.Current;
                actualNumberOfRowsFromSum += current?.Attributes?["ActualRows"] != null ? Convert.ToDouble(current.Attributes["ActualRows"].Value) : 0;
            }
            if (Math.Abs(actualNumberOfRowsFromSum) <= 0) { actualNumberOfRowsFromSum = -1; }
            return actualNumberOfRowsFromSum;
        }

        /// <summary>
        /// Given xml node with label RelOp get its node id
        /// </summary>
        /// <param name="currentRelOp">Xml node with label RelOp</param>
        /// <returns>Integer representation of the node id</returns>
        protected internal int GetNodeId(XmlNode currentRelOp)
        {
            return Int32.Parse(GetStatementLabel(currentRelOp, "NodeId")); // nodeid for rel op node
        }

        /// <summary>
        /// Given xml node with label RelOp get its logical operator
        /// </summary>
        /// <param name="currentRelOp">Xml node with label RelOp</param>
        /// <returns>String literal representation of logical Operator</returns>
        protected internal string GetLogicalOp(XmlNode currentRelOp)
        {
            return GetStatementLabel(currentRelOp, "LogicalOp");
        }

        /// <summary>
        /// Given xml node with label RelOp get its physical operator
        /// </summary>
        /// <param name="currentRelOp">Xml node with label RelOp</param>
        /// <returns>String literal representation of physical operator</returns>
        protected internal string GetPhysicalOp(XmlNode currentRelOp)
        {
            return GetStatementLabel(currentRelOp, "PhysicalOp");
        }

        /// <summary>
        /// Given xml node of label relOpNode, calculate its estimate number of rows and estimate number of executions
        /// </summary>
        /// <param name="currentRelOp">Xml node with label RelOp</param>
        /// <returns>EstimateRowsAndExecutions object containing both the estimate number of rows and estimate number of executions</returns>
        protected internal EstimateRowsAndExecutions GetEstimateNumberOfRowsAndExecutions(XmlNode currentRelOp)
        {
            double estimateRewinds = Convert.ToDouble(GetStatementLabel(currentRelOp, "EstimateRewinds")); // estimateRewinds for rel op node
            double estimateRebinds = Convert.ToDouble(GetStatementLabel(currentRelOp, "EstimateRebinds")); // estimateRebinds for rel op node
            double estimateNumberOfExecutions = estimateRewinds + estimateRebinds + 1; // calculate and set EstExecutions for this node
            double estimateNumberOfRows = Convert.ToDouble(GetStatementLabel(currentRelOp, "EstimateRows")); // estimate rows for rel op node
            estimateNumberOfRows *= estimateNumberOfExecutions;
            return new EstimateRowsAndExecutions { EstimateNumberOfRows = estimateNumberOfRows, EstimateNumberOfExecutions = estimateNumberOfExecutions };
        }
    }
}
