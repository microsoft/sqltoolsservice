//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    /// <summary>
    /// A class that lists String constants common to XML Show Plan Node Parsing
    /// </summary>
    public sealed class NodeBuilderConstants
    {
        public static readonly string ActualExecutions =    "ActualExecutions";
        public static readonly string ActualRows =          "ActualRows";
        public static readonly string Argument =            "Argument";
        public static readonly string AvgRowSize =          "AvgRowSize";
        public static readonly string DefinedValues =       "DefinedValues";
        public static readonly string ElapsedTime =         "ElapsedTime";
        public static readonly string EstimateCPU =         "EstimateCPU";
        public static readonly string EstimateExecutions =  "EstimateExecutions";
        public static readonly string EstimateIO =          "EstimateIO";
        public static readonly string EstimateRows =        "EstimateRows";
        public static readonly string LogicalOp =           "LogicalOp";
        public static readonly string NodeId =              "NodeId";
        public static readonly string OutputList =          "OutputList";
        public static readonly string Parallel =            "Parallel";
        public static readonly string ParameterCompiledValue = "ParameterCompiledValue";
        public static readonly string ParameterList =       "ParameterList";
        public static readonly string ParameterRuntimeValue = "ParameterRuntimeValue";
        public static readonly string PhysicalOp =          "PhysicalOp";
        public static readonly string SeekPredicate =       "SeekPredicate";
        public static readonly string SeekPredicates =      "SeekPredicates";
        public static readonly string StatementText =       "StatementText";
        public static readonly string StatementType =       "StatementType";
        public static readonly string TotalSubtreeCost =    "TotalSubtreeCost";
        public static readonly string Warnings =            "Warnings";

        public static readonly string Database =            "Database";
        public static readonly string Table =               "Table";
        public static readonly string Schema =              "Schema";
        public static readonly string Predicate =           "Predicate";
        public static readonly string Storage =             "Storage";
        public static readonly string Index =               "Index";
        public static readonly string Object =              "Object";

        //constants for Live Nodes
        public static readonly string Status =              "Status";
        public static readonly string OpenTime =            "OpenTime";
        public static readonly string CompletionEstimate =  "CompletionEstimate";
        public static readonly string CloseTime =           "CloseTime";

        //constants for ShowPlan Comparison
        public static readonly string SkeletonNode =        "SkeletonNode";
        public static readonly string SkeletonHasMatch =    "SkeletonHasMatch";
    }

    /// <summary>
    /// ShowPlan type
    /// </summary>
    public enum ShowPlanType
    {
        Unknown,
        Actual,
        Estimated,
        Live
    }
}
