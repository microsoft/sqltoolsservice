using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.SqlTools.ShowPlan
{
    public class RelOpNode : IRelOpNode
    {
        public double ActualRows { get; set; }
        public double EstimateExecutions { get; set; }
        public double EstimateRows { get; set; }
        public int NodeId { get; set; }
        public List<RootCause> NodeRootCauses { get; set; }
        public List<Parameter> Parameters { get; set; }
        public double Threshold { get; set; }
        public string LogicalOp { get; set; }
        public string PhysicalOp { get; set; }
        public HashSet<string> TableReferences { get; set; }

        public RelOpNode(double er, double ar, int id, double? t, double estExecutions, string lo, string po, HashSet<string> tableRefs, List<Parameter> parametersReferencedByNode) 
        {
            ActualRows = ar;
            EstimateExecutions = estExecutions;
            EstimateRows = er;
            NodeId = id;
            if(t!=null)
            {
                Threshold = (double)t;
            }
            LogicalOp = lo;
            PhysicalOp = po;
            TableReferences = tableRefs;            
            RootCause rootCauseStats = new RootCause("Statistics"); // All problem nodes will have Statistics root cause with their table references
            rootCauseStats.SetStatisticsRootCause(tableRefs);
            NodeRootCauses = new List<RootCause> { rootCauseStats };
            Parameters = parametersReferencedByNode ?? new List<Parameter>(); // if parametersReferencedByNode, set it to a new empty list
        }

        /// <summary>
        /// Adds a given root cause to the RelOpNode
        /// </summary>
        /// <param name="rootCause">RootCause object instance</param>
        public void AddNodeRootCause(RootCause rootCause)
        {
            if (rootCause == null)
            { throw new ArgumentNullException(nameof(rootCause)); }
            NodeRootCauses.Add(rootCause); 
        }

        /// <summary>
        /// Gets a string representation of the relOpNode
        /// </summary>
        /// <returns>String representing the relOpNode for displaying its attributes</returns>
        public string GetStringRelOpNode()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("NodeId = " + NodeId);

            if(!PhysicalOp.Equals(LogicalOp)) //if they are not the same print both
            {
                sb.AppendLine("Node Description = " + PhysicalOp + " (" + LogicalOp + ")");
            }
            else
            {
                sb.AppendLine("Node Description = " + PhysicalOp);
            }

            sb.AppendLine("Actual No Rows = " + ActualRows.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("EstimateExecutions = " + EstimateExecutions.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("Estimated No Rows = " + EstimateRows.ToString(CultureInfo.InvariantCulture)); 
            sb.AppendLine("Threshold = " + Threshold.ToString(CultureInfo.InvariantCulture));
            
            List<RootCause> orderedRootCauses = NodeRootCauses.OrderByDescending(o => o.Weight).ToList();
            foreach (RootCause rc in orderedRootCauses)
            {
                sb.AppendLine("\tRecommendation = " + rc.RootCauseString);
            }
            if (TableReferences != null)
            {
                foreach (string table in TableReferences)
                {
                    sb.AppendLine("\tTable Reference = " + table.ToString());
                }                
            }        
            return sb.ToString();
        }
    }
}
