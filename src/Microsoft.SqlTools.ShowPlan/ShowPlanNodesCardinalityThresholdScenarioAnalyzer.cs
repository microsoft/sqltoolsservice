using System;

namespace Microsoft.SqlTools.ShowPlan
{
    public class ShowPlanNodesCardinalityThresholdScenarioAnalyzer : IShowPlanNodesScenarioAnalyzer
    {
        private const double CardinalityEstimateDifferenceThresholdSlope1 = 5;
        private const double CardinalityEstimateDifferenceThresholdSlope2 = 1.2;
        private const double CardinalityEstimateDifferenceThresholdCrossOverPoint = 100000;

        /// <summary>
        /// Given estimate and actual numbers of rows, determine if there is a cardinality estimation issue
        /// Determine if there is Inaccurate Cardinality Estimation issue (significant difference between the number of estimate vs the number actual rows) 
        ///    The formula for threshold is:
        ///    If estimated rows is less than or equal to 1, threshold = 2 (special case)
        ///    If estimated rows is less than c, threshold = estimated rows * k1
        ///    Else threshold = estimated rows * k2 + c * (k1 – k2)   make sure threshold continues w/out dropping 
        ///        Engine code reference: CQOptEnvTransient::UllCalculateCEInaccuracyThreshold.
        ///            The default values Engine uses are
        ///                k1 = 5
        ///                k2 = 1.2
        ///                c = 100,000
        ///
        ///    Even that those are configurable, on practice we don’t know if anyone changed those, so we could take those as is for now.
        ///
        ///    As an option, RelOp Nodes/All nodes may be used instead of Result Nodes who exceed threshold as a 
        ///    mode to detect any CE difference regardless of threshold.
        /// </summary>
        /// <param name="numberOfRows1">Actual or Estimate number of rows on node for single plan analysis</param>
        /// <param name="numberOfRows2">Estimate or Actual number of rows on node for single plan analysis</param>
        /// <returns>if there is issue found between two number of rows properties</returns>
        ///public BoolThreshold FoundIssueWhenComparingNumberOfRows(double numberOfRows1, double numberOfRows2)
        protected internal BoolThreshold FoundIssueWhenComparingNumberOfRows(double numberOfRows1, double numberOfRows2)
        {
            // find smaller value, bigger value, and the value diff
            double smallerRows;
            //double biggerRows;
            double differenceBetweenRows;
            double threshold;

            if (numberOfRows1 < numberOfRows2)
            {
                smallerRows = numberOfRows1;
                //biggerRows = numberOfRows2;
                differenceBetweenRows = numberOfRows2 - numberOfRows1;
            }
            else
            {
                smallerRows = numberOfRows2;
                //biggerRows = numberOfRows1;
                differenceBetweenRows = numberOfRows1 - numberOfRows2;
            }

            // calculate threshold based on the smaller row
            if (smallerRows <= 1.0)
            {
                // special case
                threshold = 2;
            }
            else if (smallerRows < CardinalityEstimateDifferenceThresholdCrossOverPoint) //if smaller less than 10000
            {
                threshold = smallerRows * CardinalityEstimateDifferenceThresholdSlope1; // *5
            }
            else //case 1000 < smaller and diff > ((smaller * 1.2) + (10000 * (5 - 1.2)))
            {
                threshold = smallerRows * CardinalityEstimateDifferenceThresholdSlope2 + CardinalityEstimateDifferenceThresholdCrossOverPoint * (CardinalityEstimateDifferenceThresholdSlope1 - CardinalityEstimateDifferenceThresholdSlope2);
            }
            Boolean exceeded = differenceBetweenRows > threshold;
            return new BoolThreshold() { ExceededThreshold = exceeded, Threshold = threshold };
        }

        /// <summary>
        /// Sets list of problem nodes for the given statement 
        /// For each relOpNode in the given statement, check if it has actual rows, if so
        /// then calculate its threshold and whether or not its threshold is exceeded
        /// if any relOpNode exceeds this threshold it is added to the problem node list
        /// </summary>
        /// <param name="showPlanStatement">Statement object with list of all relOpNodes</param>
        /// public void AnalyzeNodesForScenario(ShowPlanXmlStatement showPlanStatement)
        public void AnalyzeNodesForScenario(ShowPlanXmlStatement showPlanStatement)        
        {
            foreach (RelOpNode relOpNode in showPlanStatement.AllRelOpNodes)
            {
                if (!relOpNode.ActualRows.Equals(-1)) //if there is # of actual rows which we need to compare...
                {
                    BoolThreshold relOpNodeThresholdAndBooleanExceeded = FoundIssueWhenComparingNumberOfRows(Convert.ToDouble(relOpNode.EstimateRows), Convert.ToDouble(relOpNode.ActualRows));
                    relOpNode.Threshold = relOpNodeThresholdAndBooleanExceeded.Threshold; //set node thredhold to calculated threshold 
                    if (relOpNodeThresholdAndBooleanExceeded.ExceededThreshold) { showPlanStatement.AddProblemNode(relOpNode); } // if node exceeded threshold, add it to show plan nodes' list of result nodes
                }
            }
        }
    }
}
