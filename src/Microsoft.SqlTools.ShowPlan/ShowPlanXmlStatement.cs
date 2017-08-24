using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ShowPlan
{
    public class ShowPlanXmlStatement : IShowPlanXmlStatement
    {
        public ShowPlanXmlStatement( string cemv, string queryHash, string queryPlanHash, List<Parameter> parametersWithNodeIds )
        {
            CardinalityEstimationModelVersion = cemv;
            ParameterColumnReferencesWithIssue = parametersWithNodeIds;
            QueryHash = queryHash;
            QueryPlanHash = queryPlanHash;
            AllRelOpNodes = new List<RelOpNode>(); 
            ProblemNodes = new List<RelOpNode>(); 
        }
        public string CardinalityEstimationModelVersion { get; set; }
        public List<Parameter> ParameterColumnReferencesWithIssue { get; set; }
        public string QueryHash { get; set; }
        public string QueryPlanHash { get; set; }
        public List<RelOpNode> AllRelOpNodes { get; set; }
        public List<RelOpNode> ProblemNodes { get; set; }
        /// <summary>
        /// Add a RelOpNode which has been identified as a problem node to this ShowPlanStatement's problem nodes 
        /// </summary>
        /// <param name="problemRelOpNode">RelOpNode to be added to problem node list, which has been identified as a problem node</param>
        public void AddProblemNode(RelOpNode problemRelOpNode)
        {
            ProblemNodes.Add(problemRelOpNode);
        }
        /// <summary>
        /// Add a parameter to this ShowPlanStatement's list of parameter column references
        /// </summary>
        /// <param name="parameterColumnReference">Object of type parameter to be added, contains column reference for this statement</param>
        public void AddParameterColumnReference(Parameter parameterColumnReference)
        {
            ParameterColumnReferencesWithIssue.Add(parameterColumnReference);
        }
        /// <summary>
        /// Add a RelOpNode to this ShowPlanStatement's list of relOpNodes
        /// </summary>
        /// <param name="relOpNode">RelOpNode found within the statement, to be added to master list of this statement's relOpNodes</param>
        public void AddRelOpNode(RelOpNode relOpNode)
        {
            AllRelOpNodes.Add(relOpNode);
        }
        /// <summary>
        /// Function for printing ShowPlanXml statement with problem AllRelOpNodes
        /// </summary>
        /// <returns>Returns string representation of Show Plan Statement with problem AllRelOpNodes</returns>
        public string GetStringShowPlanXmlProblemNodes()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("QueryHash = " + QueryHash); 
            sb.AppendLine("QueryPlanHash = " + QueryPlanHash);
            sb.AppendLine("CardinalityEstimationModelVersion = " + CardinalityEstimationModelVersion);
            foreach (RelOpNode ron in ProblemNodes)
            {
                sb.AppendLine(ron.GetStringRelOpNode());
            }
            return sb.ToString();
        }
        /// <summary>
        /// Function for printing ShowPlanXml statement with ALL AllRelOpNodes
        /// </summary>
        /// <returns>Returns string representation of Show Plan Statement with ALL AllRelOpNodes</returns>
        public string GetStringShowPlanXmlAllNodes()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("QueryHash = " + QueryHash);
            sb.AppendLine("QueryPlanHash = " + QueryPlanHash);
            sb.AppendLine("CardinalityEstimationModelVersion = " + CardinalityEstimationModelVersion);
            foreach (RelOpNode ron in AllRelOpNodes)
            {
                sb.AppendLine(ron.GetStringRelOpNode());
            }
            return sb.ToString();
        }
    }
}
