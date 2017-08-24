namespace Microsoft.SqlTools.ShowPlan
{
    public interface IShowPlanNodesScenarioAnalyzer 
    {
        void AnalyzeNodesForScenario(ShowPlanXmlStatement showPlanStatement);
    }
}
