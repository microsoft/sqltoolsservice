namespace Microsoft.SqlTools.ShowPlan
{
    public interface INodeCardinalityThresholdAnalyzer
    {
        void AnalyzeRowsForCardinalityEstimationScenario(ShowPlanXmlStatement showPlanStatement);
        BoolThreshold FoundIssueWhenComparingNumberOfRows(double numberOfRows1, double numberOfRows2);
    }
}