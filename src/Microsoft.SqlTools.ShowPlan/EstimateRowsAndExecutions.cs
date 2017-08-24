namespace Microsoft.SqlTools.ShowPlan
{
    /// <summary>
    /// Tuple of (double, double) used to store RelOpNode information in regards to estimate number of rows and executions
    /// double EstimateNumberOfExecutions = number of times the query is estimated to be run/executed
    /// double EstimateNumberOfRows = estimatNumberOfRows* estimateNumberOfExecutions
    /// </summary>
    public class EstimateRowsAndExecutions 
    {
        /// <summary>
        /// EstimateNumberOfExecutions = number of times the query is estimated to be run/executed
        /// </summary>
        public double EstimateNumberOfExecutions { get; set; }
        /// <summary>
        /// EstimateNumberOfRows = estimatNumberOfRows* estimateNumberOfExecutions
        /// </summary>
        public double EstimateNumberOfRows { get; set; }
    }
}
