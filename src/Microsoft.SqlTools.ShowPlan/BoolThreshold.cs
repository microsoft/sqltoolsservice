using System;

namespace Microsoft.SqlTools.ShowPlan
{
    /// <summary>
    /// Tuple of (Boolean, double) 
    /// Boolean ExceededThreshold is true when the threshold has been exceeded 
    /// Threshold is calculated based on estimate and actual numbers of rows for a given RelOpNode inside AnalyzeNodesForScenario::ShowPlanNodesCardinalityThresholdScenarioAnalyzer
    /// </summary>
    public class BoolThreshold
    {
        /// <summary>
        /// Boolean ExceededThreshold is true when the threshold has been exceeded 
        /// and false when the threshold has not been exceeded
        /// </summary>
        public Boolean ExceededThreshold { get; set; }
        /// <summary>
        /// Threshold is calculated based on estimate and actual numbers of rows for a given RelOpNode
        /// This Threshold is calculated inside AnalyzeNodesForScenario::ShowPlanNodesCardinalityThresholdScenarioAnalyzer
        /// </summary>
        public double Threshold { get; set;}
    }
}
