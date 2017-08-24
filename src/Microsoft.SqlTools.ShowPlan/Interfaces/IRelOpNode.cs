using System.Collections.Generic;

namespace Microsoft.SqlTools.ShowPlan
{
    public interface IRelOpNode
    {
        double ActualRows { get; set; }
        double EstimateExecutions { get; set; }
        double EstimateRows { get; set; }
        string LogicalOp { get; set; }
        int NodeId { get; set; }
        List<RootCause> NodeRootCauses { get; set; }
        List<Parameter> Parameters { get; set; }
        string PhysicalOp { get; set; }
        HashSet<string> TableReferences { get; set; }
        double Threshold { get; set; }

        void AddNodeRootCause(RootCause rootCause);
        string GetStringRelOpNode();
    }
}