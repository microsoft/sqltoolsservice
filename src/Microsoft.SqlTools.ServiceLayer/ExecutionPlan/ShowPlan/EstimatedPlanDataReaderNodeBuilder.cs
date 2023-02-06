//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Builds hierarchy of Graph objects from SQL 2000 Estimated ShowPlan Record Set
    /// </summary>
	internal sealed class EstimatedPlanDataReaderNodeBuilder : DataReaderNodeBuilder
    {
        #region Constructor

        /// <summary>
        /// Constructs EstimatedPlanDataReaderNodeBuilder
        /// </summary>
        public EstimatedPlanDataReaderNodeBuilder() : base()
        {
        }

        #endregion

        #region Overrides

        protected override ShowPlanType ShowPlanType
        {
            get { return ShowPlanType.Estimated; }
            
        }

        /// <summary>
        /// Gets index of Node Id in the recordset
        /// </summary>
        protected override int NodeIdIndex
        {
            get { return 2; }
        }

        /// <summary>
        /// Gets index of Parent Id in the recordset
        /// </summary>
        protected override int ParentIndex
        {
            get { return 3; }
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
            NodeBuilderConstants.EstimateExecutions
        };

        #endregion
    }
}
