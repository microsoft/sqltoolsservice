//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// A class that holds information about a physical or logical operator, or a statement.
    /// </summary>
    internal static class OperationTable
    {
        #region Public members

        public static Operation GetStatement(string statementTypeName)
        {
            Operation operation;
            
            if (!Statements.TryGetValue(statementTypeName, out operation))
            {
                operation = Operation.CreateUnknown(statementTypeName, "languageConstructCatchAll");
            }

            return operation;
        }

        public static Operation GetCursorType(string cursorTypeName)
        {
            Operation operation;
            
            if (!CursorTypes.TryGetValue(cursorTypeName, out operation))
            {
                cursorTypeName = GetNameFromXmlEnumAttribute(cursorTypeName, typeof(CursorType));
                operation = Operation.CreateUnknown(cursorTypeName, "cursorCatchAll");
            }

            return operation;
        }

        public static Operation GetPhysicalOperation(string operationType)
        {
            Operation operation;

            if (!PhysicalOperations.TryGetValue(operationType, out operation))
            {
                operationType = GetNameFromXmlEnumAttribute(operationType, typeof(PhysicalOpType));
                operation = Operation.CreateUnknown(operationType, "iteratorCatchAll");
            }

            return operation;
        }

        public static Operation GetLogicalOperation(string operationType)
        {
            Operation operation;

            if (!LogicalOperations.TryGetValue(operationType, out operation))
            {
                operationType = GetNameFromXmlEnumAttribute(operationType, typeof(LogicalOpType));
                // Should not use Operation.CreateUnknown here, because it would
                // use some default description and icons. Instead we should fall back to description
                // and Icon from the corresponding physical operation.
                operation = new Operation(null, operationType);
            }

            return operation;
        }

        public static Operation GetUdf()
        {
            return new Operation(null, SR.Keys.Udf, null, "languageConstructCatchAll");
        }

        public static Operation GetStoredProc()
        {
            return new Operation(null, SR.Keys.StoredProc, null, "languageConstructCatchAll");
        }

        #endregion

        #region Implementation details

        static OperationTable()
        {
            Operation[] physicalOperationList = new Operation[]
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// XML ShowPlan Operators (see showplanxml.cs for the list)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("AdaptiveJoin",           SR.Keys.AdaptiveJoin,           SR.Keys.AdaptiveJoinDescription,            "adaptiveJoin"),
                new Operation("Assert",                 SR.Keys.Assert,                 SR.Keys.AssertDescription,                  "assert"),
                new Operation("Bitmap",                 SR.Keys.Bitmap,                 SR.Keys.BitmapDescription,                  "bitmap"),
                new Operation("ClusteredIndexDelete",   SR.Keys.ClusteredIndexDelete,   SR.Keys.ClusteredIndexDeleteDescription,    "clusteredIndexDelete"),
                new Operation("ClusteredIndexInsert",   SR.Keys.ClusteredIndexInsert,   SR.Keys.ClusteredIndexInsertDescription,    "clusteredIndexInsert"),
                new Operation("ClusteredIndexScan",     SR.Keys.ClusteredIndexScan,     SR.Keys.ClusteredIndexScanDescription,      "clusteredIndexScan"),
                new Operation("ClusteredIndexSeek",     SR.Keys.ClusteredIndexSeek,     SR.Keys.ClusteredIndexSeekDescription,      "clusteredIndexSeek"),
                new Operation("ClusteredIndexUpdate",   SR.Keys.ClusteredIndexUpdate,   SR.Keys.ClusteredIndexUpdateDescription,    "clusteredIndexUpdate"),
                new Operation("ClusteredIndexMerge",    SR.Keys.ClusteredIndexMerge,    SR.Keys.ClusteredIndexMergeDescription,     "clusteredIndexMerge"),
                new Operation("ClusteredUpdate",        SR.Keys.ClusteredUpdate,        SR.Keys.ClusteredUpdateDescription,         "clusteredUpdate"),
                new Operation("Collapse",               SR.Keys.Collapse,               SR.Keys.CollapseDescription,                "collapse"),
                new Operation("ComputeScalar",          SR.Keys.ComputeScalar,          SR.Keys.ComputeScalarDescription,           "computeScalar"),
                new Operation("Concatenation",          SR.Keys.Concatenation,          SR.Keys.ConcatenationDescription,           "concatenation"),
                new Operation("ConstantScan",           SR.Keys.ConstantScan,           SR.Keys.ConstantScanDescription,            "constantScan"),
                new Operation("DeletedScan",            SR.Keys.DeletedScan,            SR.Keys.DeletedScanDescription,             "deletedScan"),
                new Operation("Filter",                 SR.Keys.Filter,                 SR.Keys.FilterDescription,                  "filter"),
                new Operation("HashMatch",              SR.Keys.HashMatch,              SR.Keys.HashMatchDescription,               "hashMatch"),
                new Operation("IndexDelete",            SR.Keys.IndexDelete,            SR.Keys.IndexDeleteDescription,             "indexDelete"),
                new Operation("IndexInsert",            SR.Keys.IndexInsert,            SR.Keys.IndexInsertDescription,             "indexInsert"),
                new Operation("IndexScan",              SR.Keys.IndexScan,              SR.Keys.IndexScanDescription,               "indexScan"),
                new Operation("ColumnstoreIndexDelete", SR.Keys.ColumnstoreIndexDelete, SR.Keys.ColumnstoreIndexDeleteDescription,  "columnstoreIndexDelete"),
                new Operation("ColumnstoreIndexInsert", SR.Keys.ColumnstoreIndexInsert, SR.Keys.ColumnstoreIndexInsertDescription,  "columnstoreIndexInsert"),
                new Operation("ColumnstoreIndexMerge",  SR.Keys.ColumnstoreIndexMerge,  SR.Keys.ColumnstoreIndexMergeDescription,   "columnstoreIndexMerge"),
                new Operation("ColumnstoreIndexScan",   SR.Keys.ColumnstoreIndexScan,   SR.Keys.ColumnstoreIndexScanDescription,    "columnstoreIndexScan"),
                new Operation("ColumnstoreIndexUpdate", SR.Keys.ColumnstoreIndexUpdate, SR.Keys.ColumnstoreIndexUpdateDescription,  "columnstoreIndexUpdate"),
                new Operation("IndexSeek",              SR.Keys.IndexSeek,              SR.Keys.IndexSeekDescription,               "indexSeek"),
                new Operation("IndexSpool",             SR.Keys.IndexSpool,             SR.Keys.IndexSpoolDescription,              "indexSpool"),
                new Operation("IndexUpdate",            SR.Keys.IndexUpdate,            SR.Keys.IndexUpdateDescription,             "indexUpdate"),
                new Operation("InsertedScan",           SR.Keys.InsertedScan,           SR.Keys.InsertedScanDescription,            "insertedScan"),
                new Operation("LogRowScan",             SR.Keys.LogRowScan,             SR.Keys.LogRowScanDescription,              "logRowScan"),
                new Operation("MergeInterval",          SR.Keys.MergeInterval,          SR.Keys.MergeIntervalDescription,           "mergeInterval"),
                new Operation("MergeJoin",              SR.Keys.MergeJoin,              SR.Keys.MergeJoinDescription,               "mergeJoin"),
                new Operation("NestedLoops",            SR.Keys.NestedLoops,            SR.Keys.NestedLoopsDescription,             "nestedLoops"),
                new Operation("Parallelism",            SR.Keys.Parallelism,            SR.Keys.ParallelismDescription,             "parallelism"),
                new Operation("ParameterTableScan",     SR.Keys.ParameterTableScan,     SR.Keys.ParameterTableScanDescription,      "parameterTableScan"),
                new Operation("Print",                  SR.Keys.Print,                  SR.Keys.PrintDescription,                   "print"),
                new Operation("Put",                    SR.Keys.Put,                    SR.Keys.PutDescription,                     "put"),
                new Operation("Rank",                   SR.Keys.Rank,                   SR.Keys.RankDescription,                    "rank"),
                // using the temporary icon as of now. Once the new icon is available, it will be updated.
                new Operation("ForeignKeyReferencesCheck",  SR.Keys.ForeignKeyReferencesCheck,   SR.Keys.ForeignKeyReferencesCheckDescription, "foreignKeyReferencesCheck"),
                new Operation("RemoteDelete",           SR.Keys.RemoteDelete,           SR.Keys.RemoteDeleteDescription,            "remoteDelete"),
                new Operation("RemoteIndexScan",        SR.Keys.RemoteIndexScan,        SR.Keys.RemoteIndexScanDescription,         "remoteIndexScan"),
                new Operation("RemoteIndexSeek",        SR.Keys.RemoteIndexSeek,        SR.Keys.RemoteIndexSeekDescription,         "remoteIndexSeek"),
                new Operation("RemoteInsert",           SR.Keys.RemoteInsert,           SR.Keys.RemoteInsertDescription,            "remoteInsert"),
                new Operation("RemoteQuery",            SR.Keys.RemoteQuery,            SR.Keys.RemoteQueryDescription,             "remoteQuery"),
                new Operation("RemoteScan",             SR.Keys.RemoteScan,             SR.Keys.RemoteScanDescription,              "remoteScan"),
                new Operation("RemoteUpdate",           SR.Keys.RemoteUpdate,           SR.Keys.RemoteUpdateDescription,            "remoteUpdate"),
                new Operation("RIDLookup",              SR.Keys.RIDLookup,              SR.Keys.RIDLookupDescription,               "ridLookup"),
                new Operation("RowCountSpool",          SR.Keys.RowCountSpool,          SR.Keys.RowCountSpoolDescription,           "rowCountSpool"),
                new Operation("Segment",                SR.Keys.Segment,                SR.Keys.SegmentDescription,                 "segment"),
                new Operation("Sequence",               SR.Keys.Sequence,               SR.Keys.SequenceDescription,                "sequence"),
                new Operation("SequenceProject",        SR.Keys.SequenceProject,        SR.Keys.SequenceProjectDescription,         "sequenceProject"),
                new Operation("Sort",                   SR.Keys.Sort,                   SR.Keys.SortDescription,                    "sort"),
                new Operation("Split",                  SR.Keys.Split,                  SR.Keys.SplitDescription,                   "split"),
                new Operation("StreamAggregate",        SR.Keys.StreamAggregate,        SR.Keys.StreamAggregateDescription,         "streamAggregate"),
                new Operation("Switch",                 SR.Keys.Switch,                 SR.Keys.SwitchDescription,                  "switchStatement"),
                new Operation("Tablevaluedfunction",    SR.Keys.TableValueFunction,     SR.Keys.TableValueFunctionDescription,      "tableValuedFunction"),
                new Operation("TableDelete",            SR.Keys.TableDelete,            SR.Keys.TableDeleteDescription,             "tableDelete"),
                new Operation("TableInsert",            SR.Keys.TableInsert,            SR.Keys.TableInsertDescription,             "tableInsert"),
                new Operation("TableScan",              SR.Keys.TableScan,              SR.Keys.TableScanDescription,               "tableScan"),
                new Operation("TableSpool",             SR.Keys.TableSpool,             SR.Keys.TableSpoolDescription,              "tableSpool"),
                new Operation("TableUpdate",            SR.Keys.TableUpdate,            SR.Keys.TableUpdateDescription,             "tableUpdate"),
                new Operation("TableMerge",             SR.Keys.TableMerge,             SR.Keys.TableMergeDescription,              "tableMerge"),
                new Operation("TFP",                    SR.Keys.TFP,                    SR.Keys.TFPDescription,                     "tfp"),
                new Operation("Top",                    SR.Keys.Top,                    SR.Keys.TopDescription,                     "top"),
                new Operation("UDX",                    SR.Keys.UDX,                    SR.Keys.UDXDescription,                     "udx"),
                new Operation("BatchHashTableBuild",    SR.Keys.BatchHashTableBuild,    SR.Keys.BatchHashTableBuildDescription,     "batchHashTableBuild"),
                new Operation("WindowSpool",            SR.Keys.Window,                 SR.Keys.WindowDescription,                  "windowSpool"),
                new Operation("WindowAggregate",        SR.Keys.WindowAggregate,        SR.Keys.WindowAggregateDescription,         "windowAggregate"),

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// XML ShowPlan Cursor Operators (see showplanxml.cs for the list)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
                new Operation("FetchQuery",             SR.Keys.FetchQuery,             SR.Keys.FetchQueryDescription,              "fetchQuery"),
                new Operation("PopulateQuery",          SR.Keys.PopulationQuery,        SR.Keys.PopulationQueryDescription,         "populateQuery"),
                new Operation("RefreshQuery",           SR.Keys.RefreshQuery,           SR.Keys.RefreshQueryDescription,            "refreshQuery"),

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// Shiloh Operators (see star\sqlquery\src\plan.cpp for the list)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("Result",                 SR.Keys.Result,                 SR.Keys.ResultDescription,                  "result"),
                new Operation("Aggregate",              SR.Keys.Aggregate,              SR.Keys.AggregateDescription,               "aggregate"),
                new Operation("Assign",                 SR.Keys.Assign,                 SR.Keys.AssignDescription,                  "assign"),                  
                new Operation("ArithmeticExpression",   SR.Keys.ArithmeticExpression,   SR.Keys.ArithmeticExpressionDescription,    "arithmeticExpression"),
                new Operation("BookmarkLookup",         SR.Keys.BookmarkLookup,         SR.Keys.BookmarkLookupDescription,          "bookmarkLookup"), 
                new Operation("Convert",                SR.Keys.Convert,                SR.Keys.ConvertDescription,                 "convert"),                 
                new Operation("Declare",                SR.Keys.Declare,                SR.Keys.DeclareDescription,                 "declare"),                 
                new Operation("Delete",                 SR.Keys.Delete,                 SR.Keys.DeleteDescription,                  "deleteOperator"),                  
                new Operation("Dynamic",                SR.Keys.Dynamic,                SR.Keys.DynamicDescription,                 "dynamic"),                 
                new Operation("HashMatchRoot",          SR.Keys.HashMatchRoot,          SR.Keys.HashMatchRootDescription,           "hashMatchRoot"),         
                new Operation("HashMatchTeam",          SR.Keys.HashMatchTeam,          SR.Keys.HashMatchTeamDescription,           "hashMatchTeam"),         
                new Operation("If",                     SR.Keys.If,                     SR.Keys.IfDescription,                      "ifOperator"),                      
                new Operation("Insert",                 SR.Keys.Insert,                 SR.Keys.InsertDescription,                  "insert"),                  
                new Operation("Intrinsic",              SR.Keys.Intrinsic,              SR.Keys.IntrinsicDescription,               "intrinsic"),               
                new Operation("Keyset",                 SR.Keys.Keyset,                 SR.Keys.KeysetDescription,                  "keyset"),                  
                new Operation("Locate",                 SR.Keys.Locate,                 SR.Keys.LocateDescription,                  "locate"),                  
                new Operation("PopulationQuery",        SR.Keys.PopulationQuery,        SR.Keys.PopulationQueryDescription,         "populationQuery"),        
                new Operation("SetFunction",            SR.Keys.SetFunction,            SR.Keys.SetFunctionDescription,             "setFunction"),            
                new Operation("Snapshot",               SR.Keys.Snapshot,               SR.Keys.SnapshotDescription,                "snapshot"),               
                new Operation("Spool",                  SR.Keys.Spool,                  SR.Keys.SpoolDescription,                   "spool"),                   
                new Operation("TSQL",                   SR.Keys.SQL,                    SR.Keys.SQLDescription,                     "tsql"),                    
                new Operation("Update",                 SR.Keys.Update,                 SR.Keys.UpdateDescription,                  "update"),

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// Fake Operators - Used to special case existing operators and expose them using different name / icons (see sqlbu#434739)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                               
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("KeyLookup",              SR.Keys.KeyLookup,              SR.Keys.KeyLookupDescription,               "keyLookup"),

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// PDW Operators (See PDW comment tags in showplanxml.xsd)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("Apply",                  SR.Keys.Apply,                  SR.Keys.ApplyDescription,                   "apply"),
                new Operation("Broadcast",              SR.Keys.Broadcast,              SR.Keys.BroadcastDescription,               "broadcast"),
                new Operation("ComputeToControlNode",   SR.Keys.ComputeToControlNode,   SR.Keys.ComputeToControlNodeDescription,    "computeToControlNode"), 
                new Operation("ConstTableGet",          SR.Keys.ConstTableGet,          SR.Keys.ConstTableGetDescription,           "constTableGet"),
                new Operation("ControlToComputeNodes",  SR.Keys.ControlToComputeNodes,  SR.Keys.ControlToComputeNodesDescription,   "controlToComputeNodes"),
                new Operation("ExternalBroadcast",      SR.Keys.ExternalBroadcast,      SR.Keys.ExternalBroadcastDescription,       "externalBroadcast"),
                new Operation("ExternalExport",         SR.Keys.ExternalExport,         SR.Keys.ExternalExportDescription,          "externalExport"),
                new Operation("ExternalLocalStreaming", SR.Keys.ExternalLocalStreaming, SR.Keys.ExternalLocalStreamingDescription,  "externalLocalStreaming"),
                new Operation("ExternalRoundRobin",     SR.Keys.ExternalRoundRobin,     SR.Keys.ExternalRoundRobinDescription,      "externalRoundRobin"),
                new Operation("ExternalShuffle",        SR.Keys.ExternalShuffle,        SR.Keys.ExternalShuffleDescription,         "externalShuffle"),
                new Operation("Get",                    SR.Keys.Get,                    SR.Keys.GetDescription,                     "get"),
                new Operation("GbApply",                SR.Keys.GbApply,                SR.Keys.GbApplyDescription,                 "groupByApply"),
                new Operation("GbAgg",                  SR.Keys.GbAgg,                  SR.Keys.GbAggDescription,                   "groupByAggregate"),
                new Operation("Join",                   SR.Keys.Join,                   SR.Keys.JoinDescription,                    "join"),
                new Operation("LocalCube",              SR.Keys.LocalCube,              SR.Keys.LocalCubeDescription,               "localCube"),
                new Operation("Project",                SR.Keys.Project,                SR.Keys.ProjectDescription,                 "project"),
                new Operation("Shuffle",                SR.Keys.Shuffle,                SR.Keys.ShuffleDescription,                 "shuffle"),
                new Operation("SingleSourceRoundRobin", SR.Keys.SingleSourceRoundRobin, SR.Keys.SingleSourceRoundRobinDescription,  "singleSourceRoundRobin"),
                new Operation("SingleSourceShuffle",    SR.Keys.SingleSourceShuffle,    SR.Keys.SingleSourceShuffleDescription,     "singleSourceShuffle"),
                new Operation("Trim",                   SR.Keys.Trim,                   SR.Keys.TrimDescription,                    "trim"),
                new Operation("Union",                  SR.Keys.Union,                  SR.Keys.UnionDescription,                   "union"),
                new Operation("UnionAll",               SR.Keys.UnionAll,               SR.Keys.UnionAllDescription,                "unionAll"),
            };

            PhysicalOperations = DictionaryFromList(physicalOperationList);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Logical Operations
            /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Operation[] logicalOperationList = new Operation[]
            {
                new Operation("Aggregate",              SR.Keys.LogicalOpAggregate),
                new Operation("AntiDiff",               SR.Keys.LogicalOpAntiDiff),
                new Operation("Assert",                 SR.Keys.LogicalOpAssert),
                new Operation("BitmapCreate",           SR.Keys.LogicalOpBitmapCreate),
                new Operation("ClusteredIndexScan",     SR.Keys.LogicalOpClusteredIndexScan),
                new Operation("ClusteredIndexSeek",     SR.Keys.LogicalOpClusteredIndexSeek),
                new Operation("ClusteredUpdate",        SR.Keys.LogicalOpClusteredUpdate),
                new Operation("Collapse",               SR.Keys.LogicalOpCollapse),
                new Operation("ComputeScalar",          SR.Keys.LogicalOpComputeScalar),
                new Operation("Concatenation",          SR.Keys.LogicalOpConcatenation),
                new Operation("ConstantScan",           SR.Keys.LogicalOpConstantScan),
                new Operation("CrossJoin",              SR.Keys.LogicalOpCrossJoin),
                new Operation("Delete",                 SR.Keys.LogicalOpDelete),
                new Operation("DeletedScan",            SR.Keys.LogicalOpDeletedScan),
                new Operation("DistinctSort",           SR.Keys.LogicalOpDistinctSort),
                new Operation("Distinct",               SR.Keys.LogicalOpDistinct),
                new Operation("DistributeStreams",      SR.Keys.LogicalOpDistributeStreams, SR.Keys.DistributeStreamsDescription, "parallelismDistribute"),
                new Operation("EagerSpool",             SR.Keys.LogicalOpEagerSpool),
                new Operation("Filter",                 SR.Keys.LogicalOpFilter),
                new Operation("FlowDistinct",           SR.Keys.LogicalOpFlowDistinct),
                new Operation("FullOuterJoin",          SR.Keys.LogicalOpFullOuterJoin),
                new Operation("GatherStreams",          SR.Keys.LogicalOpGatherStreams, SR.Keys.GatherStreamsDescription, "parallelism"),
                new Operation("IndexScan",              SR.Keys.LogicalOpIndexScan),
                new Operation("IndexSeek",              SR.Keys.LogicalOpIndexSeek),
                new Operation("InnerApply",             SR.Keys.LogicalOpInnerApply),
                new Operation("InnerJoin",              SR.Keys.LogicalOpInnerJoin),
                new Operation("Insert",                 SR.Keys.LogicalOpInsert),
                new Operation("InsertedScan",           SR.Keys.LogicalOpInsertedScan),
                new Operation("IntersectAll",           SR.Keys.LogicalOpIntersectAll),
                new Operation("Intersect",              SR.Keys.LogicalOpIntersect),
                new Operation("KeyLookup",              SR.Keys.LogicalKeyLookup),
                new Operation("LazySpool",              SR.Keys.LogicalOpLazySpool),
                new Operation("LeftAntiSemiApply",      SR.Keys.LogicalOpLeftAntiSemiApply),
                new Operation("LeftAntiSemiJoin",       SR.Keys.LogicalOpLeftAntiSemiJoin),
                new Operation("LeftDiffAll",            SR.Keys.LogicalOpLeftDiffAll),
                new Operation("LeftDiff",               SR.Keys.LogicalOpLeftDiff),
                new Operation("LeftOuterApply",         SR.Keys.LogicalOpLeftOuterApply),
                new Operation("LeftOuterJoin",          SR.Keys.LogicalOpLeftOuterJoin),
                new Operation("LeftSemiApply",          SR.Keys.LogicalOpLeftSemiApply),
                new Operation("LeftSemiJoin",           SR.Keys.LogicalOpLeftSemiJoin),
                new Operation("LogRowScan",             SR.Keys.LogicalOpLogRowScan),
                new Operation("MergeInterval",          SR.Keys.LogicalOpMergeInterval),
                new Operation("ParameterTableScan",     SR.Keys.LogicalOpParameterTableScan),
                new Operation("PartialAggregate",       SR.Keys.LogicalOpPartialAggregate),
                new Operation("Print",                  SR.Keys.LogicalOpPrint),
                new Operation("Put",                    SR.Keys.LogicalOpPut),
                new Operation("Rank",                   SR.Keys.LogicalOpRank),
                new Operation("ForeignKeyReferencesCheck",  SR.Keys.LogicalOpForeignKeyReferencesCheck),
                new Operation("RemoteDelete",           SR.Keys.LogicalOpRemoteDelete),
                new Operation("RemoteIndexScan",        SR.Keys.LogicalOpRemoteIndexScan),
                new Operation("RemoteIndexSeek",        SR.Keys.LogicalOpRemoteIndexSeek),
                new Operation("RemoteInsert",           SR.Keys.LogicalOpRemoteInsert),
                new Operation("RemoteQuery",            SR.Keys.LogicalOpRemoteQuery),
                new Operation("RemoteScan",             SR.Keys.LogicalOpRemoteScan),
                new Operation("RemoteUpdate",           SR.Keys.LogicalOpRemoteUpdate),
                new Operation("RepartitionStreams",     SR.Keys.LogicalOpRepartitionStreams, SR.Keys.RepartitionStreamsDescription, "parallelismRepartition"),
                new Operation("RIDLookup",              SR.Keys.LogicalOpRIDLookup),
                new Operation("RightAntiSemiJoin",      SR.Keys.LogicalOpRightAntiSemiJoin),
                new Operation("RightDiffAll",           SR.Keys.LogicalOpRightDiffAll),
                new Operation("RightDiff",              SR.Keys.LogicalOpRightDiff),
                new Operation("RightOuterJoin",         SR.Keys.LogicalOpRightOuterJoin),
                new Operation("RightSemiJoin",          SR.Keys.LogicalOpRightSemiJoin),
                new Operation("Segment",                SR.Keys.LogicalOpSegment),
                new Operation("Sequence",               SR.Keys.LogicalOpSequence),
                new Operation("Sort",                   SR.Keys.LogicalOpSort),
                new Operation("Split",                  SR.Keys.LogicalOpSplit),
                new Operation("Switch",                 SR.Keys.LogicalOpSwitch),
                new Operation("Tablevaluedfunction",    SR.Keys.LogicalOpTableValuedFunction),
                new Operation("TableScan",              SR.Keys.LogicalOpTableScan),
                new Operation("Top",                    SR.Keys.LogicalOpTop),
                new Operation("TopNSort",               SR.Keys.LogicalOpTopNSort),
                new Operation("UDX",                    SR.Keys.LogicalOpUDX),
                new Operation("Union",                  SR.Keys.LogicalOpUnion),
                new Operation("Update",                 SR.Keys.LogicalOpUpdate),
                new Operation("Merge",                  SR.Keys.LogicalOpMerge),
                new Operation("MergeStats",             SR.Keys.LogicalOpMergeStats),
                new Operation("LocalStats",             SR.Keys.LogicalOpLocalStats),
                new Operation("BatchHashTableBuild",    SR.Keys.LogicalOpBatchHashTableBuild),
                new Operation("WindowSpool",            SR.Keys.LogicalOpWindow),
            };

            LogicalOperations = DictionaryFromList(logicalOperationList);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Statements
            /// Name / Type                     SR Display Name Key     SR Description Key      Image                  
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////
            // TODO: may need to put a few more statements in here
            Operation[] statementList = new Operation[]
            {
                new Operation("SELECT",         null,                   null,                   "result"),
                new Operation("COND",           null,                   null,                   "ifOperator")
            };

            Statements = DictionaryFromList(statementList);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Cursor types
            /// Name / Type                     SR Display Name Key     SR Description Key              Image                         
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Operation[] cursorTypeList = new Operation[]
            {
                new Operation("Dynamic",        SR.Keys.Dynamic,        SR.Keys.DynamicDescription,     "dynamic"),
                new Operation("FastForward",    SR.Keys.FastForward,    SR.Keys.FastForwardDescription, "cursorCatchAll"),
                new Operation("Keyset",         SR.Keys.Keyset,         SR.Keys.KeysetDescription,      "keyset"),
                new Operation("SnapShot",       SR.Keys.Snapshot,       SR.Keys.SnapshotDescription,    "snapshot")
            };

            CursorTypes = DictionaryFromList(cursorTypeList);
        }

        private static Dictionary<string, Operation> DictionaryFromList(Operation[] list)
        {
            Dictionary<string, Operation> dictionary = new Dictionary<string, Operation>(list.Length);
            foreach (Operation item in list)
            {
                dictionary.Add(item.Name, item);
            }

            return dictionary;
        }

        private static string GetNameFromXmlEnumAttribute(string enumMemberName, Type enumType)
        {
            Debug.Assert(enumType.IsEnum);

            foreach (MemberInfo member in enumType.GetMembers())
            {
                if (member.Name == enumMemberName)
                {
                    object[] attributes = member.GetCustomAttributes(typeof(System.Xml.Serialization.XmlEnumAttribute), true);
                    foreach (System.Xml.Serialization.XmlEnumAttribute attribute in attributes.Cast<XmlEnumAttribute>())
                    {
                        return attribute.Name;
                    }

                    break;
                }
            }

            // If nothing has been found, just return enumMemberName.
            return enumMemberName;
        }

        #endregion

        #region Private members

        private static readonly Dictionary<string, Operation> PhysicalOperations;
        private static readonly Dictionary<string, Operation> LogicalOperations;
        private static readonly Dictionary<string, Operation> Statements;
        private static readonly Dictionary<string, Operation> CursorTypes;

        #endregion

    }
}
