//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    /// <summary>
    /// PropertyFactory creates properties based on template properties (this class public properties)
    /// 
    /// IMPORTANT: Property names should match those in ShowPlanXML classes
    /// 
    /// Note: to hide a property from PropertyGrid, it should be defined
    /// here with [Browsable(false)] attribute.
    /// 
    /// </summary>
    internal class PropertyFactory
    {
        #region Property templates

        [ShowInToolTip, DisplayOrder(0), DisplayNameDescription(SR.Keys.PhysicalOperation, SR.Keys.PhysicalOperationDesc)]
        public string PhysicalOp { get { return null; } }

        [ShowInToolTip, DisplayOrder(1), DisplayNameDescription(SR.Keys.LogicalOperation, SR.Keys.LogicalOperationDesc)]
        public string LogicalOp { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.EstimatedExecMode, SR.Keys.EstimatedExecModeDesc)]
        public string EstimatedExecutionMode { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.ActualExecMode, SR.Keys.ActualExecModeDesc)]
        public string ActualExecutionMode { get { return null; } }

        [ShowInToolTip, DisplayOrder(3), DisplayNameDescription(SR.Keys.Storage, SR.Keys.StorageDesc)]
        public string Storage { get { return null; } }

        [ShowInToolTip, DisplayOrder(102), DisplayNameDescription(SR.Keys.EstimatedDataSize, SR.Keys.EstimatedDataSizeDescription)]
        [TypeConverter(typeof(DataSizeTypeConverter))]
        public double EstimatedDataSize { get { return 0; } }

        [ShowInToolTip, DisplayOrder(4), DisplayNameDescription(SR.Keys.NumberOfRows, SR.Keys.NumberOfRowsDescription)]
        public double ActualRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(4), DisplayNameDescription(SR.Keys.ActualRowsRead, SR.Keys.ActualRowsReadDescription)]
        public double ActualRowsRead { get { return 0; } }

        [ShowInToolTip, DisplayOrder(5), DisplayNameDescription(SR.Keys.NumberOfBatches, SR.Keys.NumberOfBatchesDescription)]
        public double ActualBatches { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.Statement, SR.Keys.StatementDesc)]
        public string StatementText { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.Predicate, SR.Keys.PredicateDescription)]
        public string Predicate { get { return null; } }

        [ShowInToolTip, DisplayOrder(101), DisplayNameDescription(SR.Keys.EstimatedRowSize, SR.Keys.EstimatedRowSizeDescription)]
        [TypeConverter(typeof(DataSizeTypeConverter))]
        public int AvgRowSize { get { return 0; } }

        [ShowInToolTip, DisplayOrder(7), DisplayNameDescription(SR.Keys.CachedPlanSize, SR.Keys.CachedPlanSizeDescription)]
        [TypeConverter(typeof(KBSizeTypeConverter))]
        public int CachedPlanSize { get { return 0; } }

        [ShowInToolTip, DisplayOrder(7), DisplayNameDescription(SR.Keys.UsePlan)]
        public bool UsePlan { get { return false; } }

        [ShowInToolTip, DisplayOrder(7), DisplayNameDescription(SR.Keys.ContainsInlineScalarTsqlUdfs)]

        public bool ContainsInlineScalarTsqlUdfs { get { return false; } }

        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.EstimatedIoCost, SR.Keys.EstimatedIoCostDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateIO { get { return 0; } }

        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.DegreeOfParallelism, SR.Keys.DegreeOfParallelismDescription)]
        public int DegreeOfParallelism { get { return 0; } }

        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.EffectiveDegreeOfParallelism, SR.Keys.EffectiveDegreeOfParallelismDescription)]
        public int EffectiveDegreeOfParallelism { get { return 0; } }

        [ShowInToolTip, DisplayOrder(9), DisplayNameDescription(SR.Keys.EstimatedCpuCost, SR.Keys.EstimatedCpuCostDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateCPU { get { return 0; } }

        [ShowInToolTip, DisplayOrder(9), DisplayNameDescription(SR.Keys.MemoryGrant, SR.Keys.MemoryGrantDescription)]
        [TypeConverter(typeof(KBSizeTypeConverter))]
        public ulong MemoryGrant { get { return 0; } }

        [DisplayOrder(10), DisplayNameDescription(SR.Keys.ParameterList, SR.Keys.ParameterListDescription)]
        public object ParameterList { get { return null; } }

        [ShowInToolTip, DisplayOrder(10), DisplayNameDescription(SR.Keys.NumberOfExecutions, SR.Keys.NumberOfExecutionsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double ActualExecutions { get { return 0; } }

        [ShowInToolTip, DisplayOrder(10), DisplayNameDescription(SR.Keys.EstimatedNumberOfExecutions, SR.Keys.EstimatedNumberOfExecutionsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]

        public double EstimateExecutions { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(12), DisplayNameDescription(SR.Keys.ObjectShort, SR.Keys.ObjectDescription)]
        public object Object { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.IndexKind, SR.Keys.IndexKindDescription)]
        public string IndexKind { get { return null; } }

        [DisplayOrder(12), DisplayNameDescription(SR.Keys.OperationArgumentShort, SR.Keys.OperationArgumentDescription)]
        public string Argument { get { return null; } }

        [ShowInToolTip, DisplayOrder(111), DisplayNameDescription(SR.Keys.ActualRebinds, SR.Keys.ActualRebindsDescription)]
        public object ActualRebinds { get { return null; } }

        [ShowInToolTip, DisplayOrder(112), DisplayNameDescription(SR.Keys.ActualRewinds, SR.Keys.ActualRewindsDescription)]

        public object ActualRewinds { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLocallyAggregatedRows, SR.Keys.ActualLocallyAggregatedRowsDescription)]
        public object ActualLocallyAggregatedRows { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualElapsedms, SR.Keys.ActualElapsedmsDescription)]
        public object ActualElapsedms { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualCPUms, SR.Keys.ActualCPUmsDescription)]
        public object ActualCPUms { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualScans, SR.Keys.ActualScansDescription)]
        public object ActualScans { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLogicalReads, SR.Keys.ActualLogicalReadsDescription)]
        public object ActualLogicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualPhysicalReads, SR.Keys.ActualPhysicalReadsDescription)]
        public object ActualPhysicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualPageServerReads, SR.Keys.ActualPageServerReadsDescription)]
        public object ActualPageServerReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualReadAheads, SR.Keys.ActualReadAheadsDescription)]
        public object ActualReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualPageServerReadAheads, SR.Keys.ActualPageServerReadAheadsDescription)]
        public object ActualPageServerReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobLogicalReads, SR.Keys.ActualLobLogicalReadsDescription)]
        public object ActualLobLogicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobPhysicalReads, SR.Keys.ActualLobPhysicalReadsDescription)]
        public object ActualLobPhysicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobPageServerReads, SR.Keys.ActualLobPageServerReadsDescription)]
        public object ActualLobPageServerReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobReadAheads, SR.Keys.ActualLobReadAheadsDescription)]
        public object ActualLobReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobPageServerReadAheads, SR.Keys.ActualLobPageServerReadAheadsDescription)]
        public object ActualLobPageServerReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualIOStatistics, SR.Keys.ActualIOStatisticsDescription)]
        public object ActualIOStatistics { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualTimeStatistics, SR.Keys.ActualTimeStatisticsDescription)]
        public object ActualTimeStatistics { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualMemoryGrantStats, SR.Keys.ActualMemoryGrantStats)]
        public object ActualMemoryGrantStats { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcRowCount, SR.Keys.HpcRowCountDescription)]
        public object HpcRowCount { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcKernelElapsedUs, SR.Keys.HpcKernelElapsedUsDescription)]
        public object HpcKernelElapsedUs { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcHostToDeviceBytes, SR.Keys.HpcHostToDeviceBytesDescription)]
        public object HpcHostToDeviceBytes { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcDeviceToHostBytes, SR.Keys.HpcDeviceToHostBytesDescription)]
        public object HpcDeviceToHostBytes { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.InputMemoryGrant, SR.Keys.InputMemoryGrant)]
        public object InputMemoryGrant { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.OutputMemoryGrant, SR.Keys.OutputMemoryGrant)]
        public object OutputMemoryGrant { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.UsedMemoryGrant, SR.Keys.UsedMemoryGrant)]
        public object UsedMemoryGrant { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.IsGraphDBTransitiveClosure, SR.Keys.IsGraphDBTransitiveClosureDescription)]
        public bool IsGraphDBTransitiveClosure { get { return false; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.IsInterleavedExecuted, SR.Keys.IsInterleavedExecutedDescription)]
        public bool IsInterleavedExecuted { get { return false; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.IsAdaptive, SR.Keys.IsAdaptiveDescription)]
        public bool IsAdaptive { get { return false; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.AdaptiveThresholdRows, SR.Keys.AdaptiveThresholdRowsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double AdaptiveThresholdRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.EstimatedJoinType, SR.Keys.EstimatedJoinTypeDescription)]
        public string EstimatedJoinType { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.ActualJoinType, SR.Keys.ActualJoinTypeDescription)]
        public string ActualJoinType { get { return null; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsPerExecution, SR.Keys.EstimatedNumberOfRowsPerExecutionDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsPerExecution, SR.Keys.EstimatedNumberOfRowsPerExecutionDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double StatementEstRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsForAllExecutions, SR.Keys.EstimatedNumberOfRowsForAllExecutionsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRowsAllExecs { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsForAllExecutions, SR.Keys.EstimatedNumberOfRowsForAllExecutionsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double StatementEstRowsAllExecs { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedRowsRead, SR.Keys.EstimatedRowsReadDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimatedRowsRead { get { return 0; } }

        [DisplayOrder(101), DisplayNameDescription(SR.Keys.EstimatedRebinds, SR.Keys.EstimatedRebindsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRebinds { get { return 0; } }

        [DisplayOrder(102), DisplayNameDescription(SR.Keys.EstimatedRewinds, SR.Keys.EstimatedRewindsDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRewinds { get { return 0; } }

        [DisplayOrder(200), DisplayNameDescription(SR.Keys.DefinedValues, SR.Keys.DefinedValuesDescription)]
        public string DefinedValues { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(201), DisplayNameDescription(SR.Keys.OutputList, SR.Keys.OutputListDescription)]
        public object OutputList { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(202), DisplayNameDescription(SR.Keys.Warnings, SR.Keys.WarningsDescription)]
        public object Warnings { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Parallel, SR.Keys.ParallelDescription)]
        public bool Parallel { get { return false; } }

        [DisplayOrder(204), DisplayNameDescription(SR.Keys.SetOptions, SR.Keys.SetOptionsDescription)]
        public object StatementSetOptions { get { return null; } }

        [DisplayOrder(205), DisplayNameDescription(SR.Keys.OptimizationLevel, SR.Keys.OptimizationLevelDescription)]
        public string StatementOptmLevel { get { return null; } }

        [DisplayOrder(206), DisplayNameDescription(SR.Keys.StatementOptmEarlyAbortReason)]
        public string StatementOptmEarlyAbortReason { get { return null; } }

        [DisplayOrder(211), DisplayNameDescription(SR.Keys.MemoryFractions, SR.Keys.MemoryFractionsDescription)]
        public object MemoryFractions { get { return null; } }

        [DisplayOrder(211), DisplayNameDescription(SR.Keys.MemoryFractionsInput, SR.Keys.MemoryFractionsInputDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double Input { get { return 0; } }

        [DisplayOrder(212), DisplayNameDescription(SR.Keys.MemoryFractionsOutput, SR.Keys.MemoryFractionsOutputDescription)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double Output { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteDestination, SR.Keys.RemoteDestinationDescription)]
        public string RemoteDestination { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteObject, SR.Keys.RemoteObjectDescription)]
        public string RemoteObject { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteSource, SR.Keys.RemoteSourceDescription)]
        public string RemoteSource { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteQuery, SR.Keys.RemoteQueryDescription)]
        public string RemoteQuery { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.UsedUdxColumns, SR.Keys.UsedUdxColumnsDescription)]
        public object UsedUDXColumns { get { return null; } }

        [ShowInToolTip, DisplayOrder(204), DisplayNameDescription(SR.Keys.UdxName, SR.Keys.UdxNameDescription)]
        public string UDXName { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.InnerSideJoinColumns, SR.Keys.InnerSideJoinColumnsDescription)]
        public object InnerSideJoinColumns { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(204), DisplayNameDescription(SR.Keys.OuterSideJoinColumns, SR.Keys.OuterSideJoinColumnsDescription)]
        public object OuterSideJoinColumns { get { return null; } }

        [DisplayOrder(205), DisplayNameDescription(SR.Keys.Residual, SR.Keys.ResidualDescription)]
        public string Residual { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(206), DisplayNameDescription(SR.Keys.PassThru, SR.Keys.PassThruDescription)]
        public string PassThru { get { return null; } }

        [ShowInToolTip, DisplayOrder(207), DisplayNameDescription(SR.Keys.ManyToMany, SR.Keys.ManyToManyDescription)]
        public bool ManyToMany { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.PartitionColumns, SR.Keys.PartitionColumnsDescription)]
        public object PartitionColumns { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(204), DisplayNameDescription(SR.Keys.OrderBy, SR.Keys.OrderByDescription)]
        public object OrderBy { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(205), DisplayNameDescription(SR.Keys.HashKeys, SR.Keys.HashKeysDescription)]
        public object HashKeys { get { return null; } }

        [ShowInToolTip, DisplayOrder(206), DisplayNameDescription(SR.Keys.ProbeColumn, SR.Keys.ProbeColumnDescription)]
        public object ProbeColumn { get { return null; } }

        [ShowInToolTip, DisplayOrder(207), DisplayNameDescription(SR.Keys.PartitioningType, SR.Keys.PartitioningTypeDescription)]
        public string PartitioningType { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.GroupBy, SR.Keys.GroupByDescription)]
        public object GroupBy { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.GroupingSets, SR.Keys.GroupingSetsDescription)]
        public object GroupingSets { get { return null; } }

        [DisplayOrder(200), DisplayNameDescription(SR.Keys.RollupInfo, SR.Keys.RollupInfoDescription)]
        public object RollupInfo { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.HighestLevel, SR.Keys.HighestLevelDescription)]
        public object HighestLevel { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.RollupLevel, SR.Keys.RollupLevelDescription)]
        [Browsable(true), ImmutableObject(true)]
        public object RollupLevel { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.Level, SR.Keys.LevelDescription)]
        public object Level { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.SegmentColumn, SR.Keys.SegmentColumnDescription)]
        public object SegmentColumn { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.HashKeysBuild, SR.Keys.HashKeysBuildDescription)]
        public object HashKeysBuild { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.HashKeysProbe, SR.Keys.HashKeysProbeDescription)]
        public object HashKeysProbe { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.BuildResidual, SR.Keys.BuildResidualDescription)]
        public string BuildResidual { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.ProbeResidual, SR.Keys.ProbeResidualDescription)]
        public string ProbeResidual { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SetPredicate, SR.Keys.SetPredicateDescription)]
        public string SetPredicate { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.RankColumns, SR.Keys.RankColumnsDescription)]
        public object RankColumns { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.ActionColumn, SR.Keys.ActionColumnDescription)]
        public object ActionColumn { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.OriginalActionColumn, SR.Keys.OriginalActionColumnDescription)]
        public object OriginalActionColumn { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Rows, SR.Keys.RowsDescription)]
        public int Rows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(150), DisplayNameDescription(SR.Keys.Partitioned, SR.Keys.PartitionedDescription)]
        public object Partitioned { get { return null; } }

        [DisplayOrder(156), DisplayNameDescription(SR.Keys.PartitionsAccessed)]
        public object PartitionsAccessed { get { return null; } }

        [ShowInToolTip, DisplayOrder(152), DisplayNameDescription(SR.Keys.PartitionCount)]
        public object PartitionCount { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.TieColumns, SR.Keys.TieColumnsDescription)]
        public object TieColumns { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.IsPercent, SR.Keys.IsPercentDescription)]
        public bool IsPercent { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.WithTies, SR.Keys.WithTiesDescription)]
        public bool WithTies { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.TopExpression, SR.Keys.TopExpressionDescription)]
        public string TopExpression { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Distinct, SR.Keys.DistinctDescription)]
        public bool Distinct { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(205), DisplayNameDescription(SR.Keys.OuterReferences, SR.Keys.OuterReferencesDescription)]
        public object OuterReferences { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.PartitionId, SR.Keys.PartitionIdDescription)]
        public object PartitionId { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.Ordered, SR.Keys.OrderedDescription)]
        public bool Ordered { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.ScanDirection, SR.Keys.ScanDirectionDescription)]
        public object ScanDirection { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SeekPredicate, SR.Keys.SeekPredicateDescription)]
        public object SeekPredicate { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SeekPredicate, SR.Keys.SeekPredicateDescription)]
        public object SeekPredicateNew { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SeekPredicate, SR.Keys.SeekPredicateDescription)]
        public object SeekPredicatePart { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(205), DisplayNameDescription(SR.Keys.SeekPredicates, SR.Keys.SeekPredicatesDescription)]
        public string SeekPredicates { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.ForcedIndex, SR.Keys.ForcedIndexDescription)]
        public bool ForcedIndex { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(5), DisplayNameDescription(SR.Keys.Values, SR.Keys.ValuesDescription)]
        public object Values { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.ColumnsWithNoStatistics, SR.Keys.ColumnsWithNoStatisticsDescription)]
        public object ColumnsWithNoStatistics { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.NoJoinPredicate, SR.Keys.NoJoinPredicateDescription)]
        public bool NoJoinPredicate { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.SpillToTempDb, SR.Keys.SpillToTempDbDescription)]
        public object SpillToTempDb { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.StartupExpression, SR.Keys.StartupExpressionDescription)]
        public bool StartupExpression { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Query)]
        public string Query { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Stack)]
        public bool Stack { get { return false; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.RowCount)]
        public bool RowCount { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.Optimized)]
        public bool Optimized { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.WithPrefetch)]
        public bool WithPrefetch { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.Prefix)]
        public object Prefix { get { return null; } }

        [DisplayOrder(7), DisplayNameDescription(SR.Keys.StartRange, SR.Keys.StartRangeDescription)]
        public object StartRange { get { return null; } }

        [DisplayOrder(8), DisplayNameDescription(SR.Keys.EndRange, SR.Keys.EndRangeDescription)]
        public object EndRange { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.RangeColumns)]
        public object RangeColumns { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.RangeExpressions)]
        public object RangeExpressions { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ScanType)]
        public object ScanType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ColumnReference)]
        public object ColumnReference { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectServer, SR.Keys.ObjectServerDescription)]
        public string Server { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectDatabase, SR.Keys.ObjectDatabaseDescription)]
        public string Database { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectIndex, SR.Keys.ObjectIndexDescription)]
        public string Index { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectSchema, SR.Keys.ObjectSchemaDescription)]
        public string Schema { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectTable, SR.Keys.ObjectTableDescription)]
        public string Table { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectAlias, SR.Keys.ObjectAliasDescription)]
        public string Alias { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectColumn, SR.Keys.ObjectColumnDescription)]
        public string Column { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectComputedColumn, SR.Keys.ObjectComputedColumnDescription)]
        public bool ComputedColumn { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ParameterDataType)]
        public string ParameterDataType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ParameterCompiledValue)]
        public string ParameterCompiledValue { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ParameterRuntimeValue)]
        public string ParameterRuntimeValue { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorPlan)]
        public object CursorPlan { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorOperation)]
        public object Operation { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorName)]
        public string CursorName { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorActualType)]
        public object CursorActualType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorRequestedType)]
        public object CursorRequestedType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorConcurrency)]
        public object CursorConcurrency { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ForwardOnly)]
        public bool ForwardOnly { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.QueryPlan)]
        public object QueryPlan { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.OperationType)]
        public object OperationType { get { return null; } }

        [ShowInToolTip, DisplayOrder(300), DisplayNameDescription(SR.Keys.NodeId)]
        public int NodeId { get { return 0; } }

        [ShowInToolTip, DisplayOrder(301), DisplayNameDescription(SR.Keys.PrimaryNodeId)]
        public int PrimaryNodeId { get { return 0; } }

        [ShowInToolTip, DisplayOrder(302), DisplayNameDescription(SR.Keys.ForeignKeyReferencesCount)]
        public int ForeignKeyReferencesCount { get { return 0; } }

        [ShowInToolTip, DisplayOrder(303), DisplayNameDescription(SR.Keys.NoMatchingIndexCount)]
        public int NoMatchingIndexCount { get { return 0; } }

        [ShowInToolTip, DisplayOrder(304), DisplayNameDescription(SR.Keys.PartialMatchingIndexCount)]
        public int PartialMatchingIndexCount { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.WhereJoinColumns)]
        public object WhereJoinColumns { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.ProcName)]
        public string ProcName { get { return null; } }

        [DisplayOrder(400), DisplayNameDescription(SR.Keys.InternalInfo)]
        public object InternalInfo { get { return null; } }

        [ShowInToolTip, DisplayOrder(220), DisplayNameDescription(SR.Keys.RemoteDataAccess, SR.Keys.RemoteDataAccessDescription)]
        public bool RemoteDataAccess { get { return false; } }

        [DisplayOrder(220), DisplayNameDescription(SR.Keys.CloneAccessScope, SR.Keys.CloneAccessScopeDescription)]
        public string CloneAccessScope { get { return null; } }

        [ShowInToolTip, DisplayOrder(220), DisplayNameDescription(SR.Keys.Remoting, SR.Keys.RemotingDescription)]
        public bool Remoting { get { return false; } }

        [DisplayOrder(201), DisplayNameDescription(SR.Keys.Activation)]
        public object Activation { get { return null; } }

        [DisplayOrder(201), DisplayNameDescription(SR.Keys.BrickRouting)]
        public object BrickRouting { get { return null; } }

        [DisplayOrder(201), DisplayNameDescription(SR.Keys.FragmentIdColumn)]
        public object FragmentIdColumn { get { return null; } }
        public string CardinalityEstimationModelVersion { get { return null; } }
        public string CompileCPU { get { return null; } }
        public string CompileMemory { get { return null; } }
        public string CompileTime { get { return null; } }
        public string NonParallelPlanReason { get { return null; } }
        public string QueryHash { get { return null; } }
        public string QueryPlanHash { get { return null; } }
        public bool RetrievedFromCache { get { return false; } }
        public bool SecurityPolicyApplied { get { return false; } }
        public bool NoExpandHint { get { return false; } }
        public double TableCardinality { get { return 0; } }

        #endregion

        #region Non-browsable properties

        // The following properties should be hidden from UI


        [Browsable(false)]
        public string PhysicalOperationKind { get { return null; } }

        [Browsable(false)]
        public double EstimatedTotalSubtreeCost { get { return 0; } }

        [Browsable(false)]
        public double StatementSubTreeCost { get { return 0; } }

        [Browsable(false)]
        public double TotalSubtreeCost { get { return 0; } }

        [Browsable(false)]
        public int Parent { get { return 0; } }

        [Browsable(false)]
        public int StatementId { get { return 0; } }

        [Browsable(false)]
        public int StatementCompId { get { return 0; } }

        [Browsable(false)]
        public object RunTimeInformation { get { return null; } }

        [Browsable(false)]
        public object StatementType { get { return null; } }

        /// <summary>
        /// Run time partition summary should not show up as one node. Details such as PartitionsAccessed is displayed in individually.
        /// </summary>
        [Browsable(false)]
        public object RunTimePartitionSummary { get { return null; } }
        [Browsable(false)]
        public object SkeletonNode { get { return null; } }
        [Browsable(false)]
        public object SkeletonHasMatch { get { return null; } }

        #endregion

        #region CreateProperty
        public static PropertyDescriptor CreateProperty(PropertyDescriptor property, object value)
        {
            Type type = null;

            // In case of xml Choice group, the property name can be general like "Item" or "Items".
            // The real names are specified by XmlElementAttributes. We need to save the type of
            // value to extract its original name from its XmlElementAttribute.
            if (property.Name == "Items" || property.Name == "Item")
            {
                type = value.GetType();
            }

            // Convert value if ObjectWrapperTypeConverter supports it
            if (ObjectWrapperTypeConverter.Default.CanConvertFrom(property.PropertyType))
            {
                value = ObjectWrapperTypeConverter.Default.ConvertFrom(value);
            }

            if (value == null)
            {
                return null;
            }

            PropertyDescriptor templateProperty = Properties[property.Name];

            if (templateProperty != null)
            {
                return new PropertyValue(templateProperty, value);
            }
            else
            {
                IEnumerable attributeCollection = property.Attributes;
                string propertyName = property.Name;

                // In case of xml Choice group, the property name can be general like "Item" or "Items".
                // The real names are specified by XmlElementAttributes. property.Attributes does not
                // return all the attributes. Hence we need extract custom attributes through 
                // PropertyInfo class.
                if (type != null)
                {
                    attributeCollection = PropertyFactory.GetAttributeCollectionForChoiceElement(property);
                }

                foreach (object attrib in attributeCollection)
                {
                    XmlElementAttribute attribute = attrib as XmlElementAttribute;

                    if (attribute != null && !string.IsNullOrEmpty(attribute.ElementName))
                    {
                        if ((type == null) || (type.Equals(attribute.Type)))
                        {
                            propertyName = attribute.ElementName;

                            templateProperty = Properties[propertyName];
                            if (templateProperty != null)
                            {
                                return new PropertyValue(templateProperty, value);
                            }
                        }
                    }
                }

                // TODO: review this debug code
                return new PropertyValue(propertyName, value);
            }
        }

        private static IEnumerable GetAttributeCollectionForChoiceElement(PropertyDescriptor property)
        {
            Type type = property.ComponentType;
            PropertyInfo pInfo = type.GetProperty("Items");

            if (pInfo == null)
            {
                //Try using item.
                pInfo = type.GetProperty("Item");
            }

            if (pInfo != null)
            {
                return pInfo.GetCustomAttributes(true);
            }

            return property.Attributes;
        }
        public static PropertyDescriptor CreateProperty(string propertyName, object value)
        {
            PropertyDescriptor templateProperty = Properties[propertyName];

            if (templateProperty != null)
            {
                return new PropertyValue(templateProperty, value);
            }
            else
            {
                // TODO: review this debug code
                return new PropertyValue(propertyName, value);
            }
        }

        #endregion

        #region Implementation details

        private PropertyFactory() { }

        private static PropertyDescriptorCollection Properties = TypeDescriptor.GetProperties(typeof(PropertyFactory));

        #endregion
    }
}
