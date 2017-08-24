using System.Collections.Generic;

namespace Microsoft.SqlTools.ShowPlan
{
    public interface IShowPlanXmlStatement
    {
        string CardinalityEstimationModelVersion { get; set; }
        List<Parameter> ParameterColumnReferencesWithIssue { get; set; }
        List<RelOpNode> ProblemNodes { get; set; }
        string QueryHash { get; set; }
        string QueryPlanHash { get; set; }
        List<RelOpNode> AllRelOpNodes { get; set; }

        void AddParameterColumnReference(Parameter parameterColumnReference);
        void AddProblemNode(RelOpNode problemRelOpNode);
        void AddRelOpNode(RelOpNode relOpNode);
        string GetStringShowPlanXmlProblemNodes();
    }
}