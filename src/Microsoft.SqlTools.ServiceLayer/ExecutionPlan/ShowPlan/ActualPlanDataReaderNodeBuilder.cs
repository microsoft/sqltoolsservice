//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Builds hierarchy of Graph objects from SQL 2000 Actual ShowPlan Record Set
    /// </summary>
	internal class ActualPlanDataReaderNodeBuilder : DataReaderNodeBuilder
    {
        #region Constructor

        /// <summary>
        /// Constructs ActualPlanDataReaderNodeBuilder
        /// </summary>
        public ActualPlanDataReaderNodeBuilder() : base()
        {
        }

        #endregion

        #region Overrides

        protected override ShowPlanType ShowPlanType
        {
            get { return ShowPlanType.Actual; }
        }

        /// <summary>
        /// Gets index of Node Id in the recordset
        /// </summary>
        protected override int NodeIdIndex
        {
            get { return 4; }
        }

        /// <summary>
        /// Gets index of Parent Id in the recordset
        /// </summary>
        protected override int ParentIndex
        {
            get { return 5; }
        }

        /// <summary>
        /// Gets property names that correspond to values returned
        /// in each ShowPlan row.
        /// </summary>
        /// <returns>Array of property names</returns>
        protected override string[] GetPropertyNames()
        {
            return propertyNames;
        }

        #endregion

        #region Private members

        private static string[] propertyNames = new string[]
        {
            NodeBuilderConstants.ActualRows,       // Rows
            NodeBuilderConstants.ActualExecutions, // Executes
            NodeBuilderConstants.StatementText,    // StmtText
            null,               // StmtId
            NodeBuilderConstants.NodeId,
            null,               // Parent
            NodeBuilderConstants.PhysicalOp,
            NodeBuilderConstants.LogicalOp,
            NodeBuilderConstants.Argument,
            NodeBuilderConstants.DefinedValues,
            NodeBuilderConstants.EstimateRows,
            NodeBuilderConstants.EstimateIO,
            NodeBuilderConstants.EstimateCPU,
            NodeBuilderConstants.AvgRowSize,
            NodeBuilderConstants.TotalSubtreeCost,
            NodeBuilderConstants.OutputList,
            NodeBuilderConstants.Warnings,
            NodeBuilderConstants.StatementType,    // Type
            NodeBuilderConstants.Parallel,
            null,                // EstimateExecutions
        };

        #endregion
    }
}
