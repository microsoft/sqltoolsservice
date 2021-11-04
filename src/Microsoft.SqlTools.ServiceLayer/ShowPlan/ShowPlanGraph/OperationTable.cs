//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
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
                operation = Operation.CreateUnknown(statementTypeName, "Language_construct_catch_all.ico");
            }

            return operation;
        }

        public static Operation GetCursorType(string cursorTypeName)
        {
            Operation operation;
            
            if (!CursorTypes.TryGetValue(cursorTypeName, out operation))
            {
                cursorTypeName = GetNameFromXmlEnumAttribute(cursorTypeName, typeof(CursorType));
                operation = Operation.CreateUnknown(cursorTypeName, "Cursor_catch_all_32x.ico");
            }

            return operation;
        }

        public static Operation GetPhysicalOperation(string operationType)
        {
            Operation operation;

            if (!PhysicalOperations.TryGetValue(operationType, out operation))
            {
                operationType = GetNameFromXmlEnumAttribute(operationType, typeof(PhysicalOpType));
                operation = Operation.CreateUnknown(operationType, "Iterator_catch_all.ico");
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
            return new Operation(null, SR.Keys.Udf, null, "Language_construct_catch_all.ico");
        }

        public static Operation GetStoredProc()
        {
            return new Operation(null, SR.Keys.StoredProc, null, "Language_construct_catch_all.ico");
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
                new Operation("AdaptiveJoin",           SR.Keys.AdaptiveJoin,           SR.Keys.AdaptiveJoinDescription,            "Adaptive_Join_32x.ico"),
                new Operation("Assert",                 SR.Keys.Assert,                 SR.Keys.AssertDescription,                  "Assert_32x.ico"),
                new Operation("Bitmap",                 SR.Keys.Bitmap,                 SR.Keys.BitmapDescription,                  "Bitmap_32x.ico"),
                new Operation("ClusteredIndexDelete",   SR.Keys.ClusteredIndexDelete,   SR.Keys.ClusteredIndexDeleteDescription,    "Clustered_index_delete_32x.ico"),
                new Operation("ClusteredIndexInsert",   SR.Keys.ClusteredIndexInsert,   SR.Keys.ClusteredIndexInsertDescription,    "Clustered_index_insert_32x.ico"),
                new Operation("ClusteredIndexScan",     SR.Keys.ClusteredIndexScan,     SR.Keys.ClusteredIndexScanDescription,      "Clustered_index_scan_32x.ico"),
                new Operation("ClusteredIndexSeek",     SR.Keys.ClusteredIndexSeek,     SR.Keys.ClusteredIndexSeekDescription,      "Clustered_index_seek_32x.ico"),
                new Operation("ClusteredIndexUpdate",   SR.Keys.ClusteredIndexUpdate,   SR.Keys.ClusteredIndexUpdateDescription,    "Clustered_index_update_32x.ico"),
                new Operation("ClusteredIndexMerge",    SR.Keys.ClusteredIndexMerge,    SR.Keys.ClusteredIndexMergeDescription,     "Clustered_index_merge_32x.ico"),
                new Operation("ClusteredUpdate",        SR.Keys.ClusteredUpdate,        SR.Keys.ClusteredUpdateDescription,         "Clustered_update_32x.ico"),
                new Operation("Collapse",               SR.Keys.Collapse,               SR.Keys.CollapseDescription,                "Collapse_32x.ico"),
                new Operation("ComputeScalar",          SR.Keys.ComputeScalar,          SR.Keys.ComputeScalarDescription,           "Compute_scalar_32x.ico"),
                new Operation("Concatenation",          SR.Keys.Concatenation,          SR.Keys.ConcatenationDescription,           "Concatenation_32x.ico"),
                new Operation("ConstantScan",           SR.Keys.ConstantScan,           SR.Keys.ConstantScanDescription,            "Constant_scan_32x.ico"),
                new Operation("DeletedScan",            SR.Keys.DeletedScan,            SR.Keys.DeletedScanDescription,             "Deleted_scan_32x.ico"),
                new Operation("Filter",                 SR.Keys.Filter,                 SR.Keys.FilterDescription,                  "Filter_32x.ico"),
                new Operation("HashMatch",              SR.Keys.HashMatch,              SR.Keys.HashMatchDescription,               "Hash_match_32x.ico"),
                new Operation("IndexDelete",            SR.Keys.IndexDelete,            SR.Keys.IndexDeleteDescription,             "Nonclust_index_delete_32x.ico"),
                new Operation("IndexInsert",            SR.Keys.IndexInsert,            SR.Keys.IndexInsertDescription,             "Nonclust_index_insert_32x.ico"),
                new Operation("IndexScan",              SR.Keys.IndexScan,              SR.Keys.IndexScanDescription,               "Nonclust_index_scan_32x.ico"),
                new Operation("ColumnstoreIndexDelete", SR.Keys.ColumnstoreIndexDelete, SR.Keys.ColumnstoreIndexDeleteDescription,  "Columnstore_index_delete_32x.ico"),
                new Operation("ColumnstoreIndexInsert", SR.Keys.ColumnstoreIndexInsert, SR.Keys.ColumnstoreIndexInsertDescription,  "Columnstore_index_insert_32x.ico"),
                new Operation("ColumnstoreIndexMerge",  SR.Keys.ColumnstoreIndexMerge,  SR.Keys.ColumnstoreIndexMergeDescription,   "Columnstore_index_merge_32x.ico"),
                new Operation("ColumnstoreIndexScan",   SR.Keys.ColumnstoreIndexScan,   SR.Keys.ColumnstoreIndexScanDescription,    "Columnstore_index_scan_32x.ico"),
                new Operation("ColumnstoreIndexUpdate", SR.Keys.ColumnstoreIndexUpdate, SR.Keys.ColumnstoreIndexUpdateDescription,  "Columnstore_index_update_32x.ico"),
                new Operation("IndexSeek",              SR.Keys.IndexSeek,              SR.Keys.IndexSeekDescription,               "Nonclust_index_seek_32x.ico"),
                new Operation("IndexSpool",             SR.Keys.IndexSpool,             SR.Keys.IndexSpoolDescription,              "Nonclust_index_spool_32x.ico"),
                new Operation("IndexUpdate",            SR.Keys.IndexUpdate,            SR.Keys.IndexUpdateDescription,             "Nonclust_index_update_32x.ico"),
                new Operation("InsertedScan",           SR.Keys.InsertedScan,           SR.Keys.InsertedScanDescription,            "Inserted_scan_32x.ico"),
                new Operation("LogRowScan",             SR.Keys.LogRowScan,             SR.Keys.LogRowScanDescription,              "Log_row_scan_32x.ico"),
                new Operation("MergeInterval",          SR.Keys.MergeInterval,          SR.Keys.MergeIntervalDescription,           "Merge_interval_32x.ico"),
                new Operation("MergeJoin",              SR.Keys.MergeJoin,              SR.Keys.MergeJoinDescription,               "Merge_join_32x.ico"),
                new Operation("NestedLoops",            SR.Keys.NestedLoops,            SR.Keys.NestedLoopsDescription,             "Nested_loops_32x.ico"),
                new Operation("Parallelism",            SR.Keys.Parallelism,            SR.Keys.ParallelismDescription,             "Parallelism_32x.ico"),
                new Operation("ParameterTableScan",     SR.Keys.ParameterTableScan,     SR.Keys.ParameterTableScanDescription,      "Parameter_table_scan_32x.ico"),
                new Operation("Print",                  SR.Keys.Print,                  SR.Keys.PrintDescription,                   "Print.ico"),
                new Operation("Put",                    SR.Keys.Put,                    SR.Keys.PutDescription,                     "Put_32x.ico"),
                new Operation("Rank",                   SR.Keys.Rank,                   SR.Keys.RankDescription,                    "Rank_32x.ico"),
                // using the temporary icon as of now. Once the new icon is available, it will be updated.
                new Operation("ForeignKeyReferencesCheck",  SR.Keys.ForeignKeyReferencesCheck,   SR.Keys.ForeignKeyReferencesCheckDescription, "Referential_Integrity_32x.ico"),
                new Operation("RemoteDelete",           SR.Keys.RemoteDelete,           SR.Keys.RemoteDeleteDescription,            "Remote_delete_32x.ico"),
                new Operation("RemoteIndexScan",        SR.Keys.RemoteIndexScan,        SR.Keys.RemoteIndexScanDescription,         "Remote_index_scan_32x.ico"),
                new Operation("RemoteIndexSeek",        SR.Keys.RemoteIndexSeek,        SR.Keys.RemoteIndexSeekDescription,         "Remote_index_seek_32x.ico"),
                new Operation("RemoteInsert",           SR.Keys.RemoteInsert,           SR.Keys.RemoteInsertDescription,            "Remote_insert_32x.ico"),
                new Operation("RemoteQuery",            SR.Keys.RemoteQuery,            SR.Keys.RemoteQueryDescription,             "Remote_query_32x.ico"),
                new Operation("RemoteScan",             SR.Keys.RemoteScan,             SR.Keys.RemoteScanDescription,              "Remote_scan_32x.ico"),
                new Operation("RemoteUpdate",           SR.Keys.RemoteUpdate,           SR.Keys.RemoteUpdateDescription,            "Remote_update_32x.ico"),
                new Operation("RIDLookup",              SR.Keys.RIDLookup,              SR.Keys.RIDLookupDescription,               "RID_clustered_locate_32x.ico"),
                new Operation("RowCountSpool",          SR.Keys.RowCountSpool,          SR.Keys.RowCountSpoolDescription,           "Remote_count_spool_32x.ico"),
                new Operation("Segment",                SR.Keys.Segment,                SR.Keys.SegmentDescription,                 "Segment_32x.ico"),
                new Operation("Sequence",               SR.Keys.Sequence,               SR.Keys.SequenceDescription,                "Sequence_32x.ico"),
                new Operation("SequenceProject",        SR.Keys.SequenceProject,        SR.Keys.SequenceProjectDescription,         "Sequence_project_32x.ico"),
                new Operation("Sort",                   SR.Keys.Sort,                   SR.Keys.SortDescription,                    "Sort_32x.ico"),
                new Operation("Split",                  SR.Keys.Split,                  SR.Keys.SplitDescription,                   "Split_32x.ico"),
                new Operation("StreamAggregate",        SR.Keys.StreamAggregate,        SR.Keys.StreamAggregateDescription,         "Stream_aggregate_32x.ico"),
                new Operation("Switch",                 SR.Keys.Switch,                 SR.Keys.SwitchDescription,                  "Switch_32x.ico"),
                new Operation("Tablevaluedfunction",    SR.Keys.TableValueFunction,     SR.Keys.TableValueFunctionDescription,      "Table_value_function_32x.ico"),
                new Operation("TableDelete",            SR.Keys.TableDelete,            SR.Keys.TableDeleteDescription,             "Table_delete_32x.ico"),
                new Operation("TableInsert",            SR.Keys.TableInsert,            SR.Keys.TableInsertDescription,             "Table_insert_32x.ico"),
                new Operation("TableScan",              SR.Keys.TableScan,              SR.Keys.TableScanDescription,               "Table_scan_32x.ico"),
                new Operation("TableSpool",             SR.Keys.TableSpool,             SR.Keys.TableSpoolDescription,              "Table_spool_32x.ico"),
                new Operation("TableUpdate",            SR.Keys.TableUpdate,            SR.Keys.TableUpdateDescription,             "Table_update_32x.ico"),
                new Operation("TableMerge",             SR.Keys.TableMerge,             SR.Keys.TableMergeDescription,              "Table_merge_32x.ico"),
                new Operation("TFP",                    SR.Keys.TFP,                    SR.Keys.TFPDescription,                     "Predict_32x.ico"),
                new Operation("Top",                    SR.Keys.Top,                    SR.Keys.TopDescription,                     "Top_32x.ico"),
                new Operation("UDX",                    SR.Keys.UDX,                    SR.Keys.UDXDescription,                     "UDX_32x.ico"),
                new Operation("BatchHashTableBuild",    SR.Keys.BatchHashTableBuild,    SR.Keys.BatchHashTableBuildDescription,     "BatchHashTableBuild_32x.ico"),
                new Operation("WindowSpool",            SR.Keys.Window,                 SR.Keys.WindowDescription,                  "Table_spool_32x.ico"),
                new Operation("WindowAggregate",        SR.Keys.WindowAggregate,        SR.Keys.WindowAggregateDescription,         "Window_aggregate_32x.ico"),

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// XML ShowPlan Cursor Operators (see showplanxml.cs for the list)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
                new Operation("FetchQuery",             SR.Keys.FetchQuery,             SR.Keys.FetchQueryDescription,              "Fetch_query_32x.ico"),
                new Operation("PopulateQuery",          SR.Keys.PopulationQuery,        SR.Keys.PopulationQueryDescription,         "Population_query_32x.ico"),
                new Operation("RefreshQuery",           SR.Keys.RefreshQuery,           SR.Keys.RefreshQueryDescription,            "Refresh_query_32x.ico"),

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// Shiloh Operators (see star\sqlquery\src\plan.cpp for the list)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("Result",                 SR.Keys.Result,                 SR.Keys.ResultDescription,                  "Result_32x.ico"),
                new Operation("Aggregate",              SR.Keys.Aggregate,              SR.Keys.AggregateDescription,               "Aggregate_32x.ico"),
                new Operation("Assign",                 SR.Keys.Assign,                 SR.Keys.AssignDescription,                  "Assign_32x.ico"),                  
                new Operation("ArithmeticExpression",   SR.Keys.ArithmeticExpression,   SR.Keys.ArithmeticExpressionDescription,    "Arithmetic_expression_32x.ico"),
                new Operation("BookmarkLookup",         SR.Keys.BookmarkLookup,         SR.Keys.BookmarkLookupDescription,          "Bookmark_lookup_32x.ico"), 
                new Operation("Convert",                SR.Keys.Convert,                SR.Keys.ConvertDescription,                 "Convert_32x.ico"),                 
                new Operation("Declare",                SR.Keys.Declare,                SR.Keys.DeclareDescription,                 "Declare_32x.ico"),                 
                new Operation("Delete",                 SR.Keys.Delete,                 SR.Keys.DeleteDescription,                  "Delete_32x.ico"),                  
                new Operation("Dynamic",                SR.Keys.Dynamic,                SR.Keys.DynamicDescription,                 "Dynamic_32x.ico"),                 
                new Operation("HashMatchRoot",          SR.Keys.HashMatchRoot,          SR.Keys.HashMatchRootDescription,           "Hash_match_root_32x.ico"),         
                new Operation("HashMatchTeam",          SR.Keys.HashMatchTeam,          SR.Keys.HashMatchTeamDescription,           "Hash_match_team_32x.ico"),         
                new Operation("If",                     SR.Keys.If,                     SR.Keys.IfDescription,                      "If_32x.ico"),                      
                new Operation("Insert",                 SR.Keys.Insert,                 SR.Keys.InsertDescription,                  "Insert_32x.ico"),                  
                new Operation("Intrinsic",              SR.Keys.Intrinsic,              SR.Keys.IntrinsicDescription,               "Intrinsic_32x.ico"),               
                new Operation("Keyset",                 SR.Keys.Keyset,                 SR.Keys.KeysetDescription,                  "Keyset_32x.ico"),                  
                new Operation("Locate",                 SR.Keys.Locate,                 SR.Keys.LocateDescription,                  "RID_nonclustered_locate_32x.ico"),                  
                new Operation("PopulationQuery",        SR.Keys.PopulationQuery,        SR.Keys.PopulationQueryDescription,         "Population_query_32x.ico"),        
                new Operation("SetFunction",            SR.Keys.SetFunction,            SR.Keys.SetFunctionDescription,             "Set_function_32x.ico"),            
                new Operation("Snapshot",               SR.Keys.Snapshot,               SR.Keys.SnapshotDescription,                "Snapshot_32x.ico"),               
                new Operation("Spool",                  SR.Keys.Spool,                  SR.Keys.SpoolDescription,                   "Spool_32x.ico"),                   
                new Operation("TSQL",                   SR.Keys.SQL,                    SR.Keys.SQLDescription,                     "SQL_32x.ico"),                    
                new Operation("Update",                 SR.Keys.Update,                 SR.Keys.UpdateDescription,                  "Update_32x.ico"),

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// Fake Operators - Used to special case existing operators and expose them using different name / icons (see sqlbu#434739)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                               
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("KeyLookup",              SR.Keys.KeyLookup,              SR.Keys.KeyLookupDescription,               "Bookmark_lookup_32x.ico"),

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// PDW Operators (See PDW comment tags in showplanxml.xsd)
                /// Name / Type                         SR Display Name Key             SR Description Key                          Image                              
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                new Operation("Apply",                  SR.Keys.Apply,                  SR.Keys.ApplyDescription,                   "Apply_32x.ico"),
                new Operation("Broadcast",              SR.Keys.Broadcast,              SR.Keys.BroadcastDescription,               "Broadcast_32x.ico"),
                new Operation("ComputeToControlNode",   SR.Keys.ComputeToControlNode,   SR.Keys.ComputeToControlNodeDescription,    "Compute_to_control_32x.ico"), 
                new Operation("ConstTableGet",          SR.Keys.ConstTableGet,          SR.Keys.ConstTableGetDescription,           "Const_table_get_32x.ico"),
                new Operation("ControlToComputeNodes",  SR.Keys.ControlToComputeNodes,  SR.Keys.ControlToComputeNodesDescription,   "Control_to_compute_32x.ico"),
                new Operation("ExternalBroadcast",      SR.Keys.ExternalBroadcast,      SR.Keys.ExternalBroadcastDescription,       "External_broadcast_32x.ico"),
                new Operation("ExternalExport",         SR.Keys.ExternalExport,         SR.Keys.ExternalExportDescription,          "External_export_32x.ico"),
                new Operation("ExternalLocalStreaming", SR.Keys.ExternalLocalStreaming, SR.Keys.ExternalLocalStreamingDescription,  "External_local_streaming_32x.ico"),
                new Operation("ExternalRoundRobin",     SR.Keys.ExternalRoundRobin,     SR.Keys.ExternalRoundRobinDescription,      "External_round_robin_32x.ico"),
                new Operation("ExternalShuffle",        SR.Keys.ExternalShuffle,        SR.Keys.ExternalShuffleDescription,         "External_shuffle_32x.ico"),
                new Operation("Get",                    SR.Keys.Get,                    SR.Keys.GetDescription,                     "Get_32x.ico"),
                new Operation("GbApply",                SR.Keys.GbApply,                SR.Keys.GbApplyDescription,                 "Apply_32x.ico"),
                new Operation("GbAgg",                  SR.Keys.GbAgg,                  SR.Keys.GbAggDescription,                   "Group_by_aggregate_32x.ico"),
                new Operation("Join",                   SR.Keys.Join,                   SR.Keys.JoinDescription,                    "Join_32x.ico"),
                new Operation("LocalCube",              SR.Keys.LocalCube,              SR.Keys.LocalCubeDescription,               "Intrinsic_32x.ico"),
                new Operation("Project",                SR.Keys.Project,                SR.Keys.ProjectDescription,                 "Project_32x.ico"),
                new Operation("Shuffle",                SR.Keys.Shuffle,                SR.Keys.ShuffleDescription,                 "Shuffle_32x.ico"),
                new Operation("SingleSourceRoundRobin", SR.Keys.SingleSourceRoundRobin, SR.Keys.SingleSourceRoundRobinDescription,  "Single_source_round_robin_32x.ico"),
                new Operation("SingleSourceShuffle",    SR.Keys.SingleSourceShuffle,    SR.Keys.SingleSourceShuffleDescription,     "Single_source_shuffle_32x.ico"),
                new Operation("Trim",                   SR.Keys.Trim,                   SR.Keys.TrimDescription,                    "Trim_32x.ico"),
                new Operation("Union",                  SR.Keys.Union,                  SR.Keys.UnionDescription,                   "Union_32x.ico"),
                new Operation("UnionAll",               SR.Keys.UnionAll,               SR.Keys.UnionAllDescription,                "Union_all_32x.ico"),
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
                new Operation("DistributeStreams",      SR.Keys.LogicalOpDistributeStreams, SR.Keys.DistributeStreamsDescription, "Parallelism_distribute.ico"),
                new Operation("EagerSpool",             SR.Keys.LogicalOpEagerSpool),
                new Operation("Filter",                 SR.Keys.LogicalOpFilter),
                new Operation("FlowDistinct",           SR.Keys.LogicalOpFlowDistinct),
                new Operation("FullOuterJoin",          SR.Keys.LogicalOpFullOuterJoin),
                new Operation("GatherStreams",          SR.Keys.LogicalOpGatherStreams, SR.Keys.GatherStreamsDescription, "Parallelism_32x.ico"),
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
                new Operation("RepartitionStreams",     SR.Keys.LogicalOpRepartitionStreams, SR.Keys.RepartitionStreamsDescription, "Parallelism_repartition.ico"),
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
                new Operation("SELECT",         null,                   null,                   "Result_32x.ico"),
                new Operation("COND",           null,                   null,                   "If_32x.ico")
            };

            Statements = DictionaryFromList(statementList);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Cursor types
            /// Name / Type                     SR Display Name Key     SR Description Key              Image                         
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Operation[] cursorTypeList = new Operation[]
            {
                new Operation("Dynamic",        SR.Keys.Dynamic,        SR.Keys.DynamicDescription,     "Dynamic_32x.ico"),
                new Operation("FastForward",    SR.Keys.FastForward,    SR.Keys.FastForwardDescription, "Cursor_catch_all_32x.ico"),
                new Operation("Keyset",         SR.Keys.Keyset,         SR.Keys.KeysetDescription,      "Keyset_32x.ico"),
                new Operation("SnapShot",       SR.Keys.Snapshot,       SR.Keys.SnapshotDescription,    "Snapshot_32x.ico")
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
                    foreach (System.Xml.Serialization.XmlEnumAttribute attribute in attributes)
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
