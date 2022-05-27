//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
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

        [ShowInToolTip, DisplayOrder(0), DisplayNameDescription(SR.Keys.PhysicalOperation, SR.Keys.PhysicalOperationDesc), BetterValue(BetterValue.None)]
        public string PhysicalOp { get { return null; } }

        [ShowInToolTip, DisplayOrder(1), DisplayNameDescription(SR.Keys.LogicalOperation, SR.Keys.LogicalOperationDesc), BetterValue(BetterValue.None)]
        public string LogicalOp { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.EstimatedExecMode, SR.Keys.EstimatedExecModeDesc), BetterValue(BetterValue.None)]
        public string EstimatedExecutionMode { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.ActualExecMode, SR.Keys.ActualExecModeDesc), BetterValue(BetterValue.None)]
        public string ActualExecutionMode { get { return null; } }

        [ShowInToolTip, DisplayOrder(3), DisplayNameDescription(SR.Keys.Storage, SR.Keys.StorageDesc), BetterValue(BetterValue.None)]
        public string Storage { get { return null; } }

        [ShowInToolTip, DisplayOrder(102), DisplayNameDescription(SR.Keys.EstimatedDataSize, SR.Keys.EstimatedDataSizeDescription), BetterValue(BetterValue.HigherNumber)]
        [TypeConverter(typeof(DataSizeTypeConverter))]
        public double EstimatedDataSize { get { return 0; } }

        [ShowInToolTip, DisplayOrder(4), DisplayNameDescription(SR.Keys.NumberOfRows, SR.Keys.NumberOfRowsDescription), BetterValue(BetterValue.HigherNumber)]
        public double ActualRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(4), DisplayNameDescription(SR.Keys.ActualRowsRead, SR.Keys.ActualRowsReadDescription), BetterValue(BetterValue.HigherNumber)]
        public double ActualRowsRead { get { return 0; } }

        [ShowInToolTip, DisplayOrder(5), DisplayNameDescription(SR.Keys.NumberOfBatches, SR.Keys.NumberOfBatchesDescription), BetterValue(BetterValue.HigherNumber)]
        public double ActualBatches { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.Statement, SR.Keys.StatementDesc), BetterValue(BetterValue.None)]
        public string StatementText { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.Predicate, SR.Keys.PredicateDescription), BetterValue(BetterValue.None)]
        public string Predicate { get { return null; } }

        [ShowInToolTip, DisplayOrder(101), DisplayNameDescription(SR.Keys.EstimatedRowSize, SR.Keys.EstimatedRowSizeDescription), BetterValue(BetterValue.HigherNumber)]
        [TypeConverter(typeof(DataSizeTypeConverter))]
        public int AvgRowSize { get { return 0; } }

        [ShowInToolTip, DisplayOrder(7), DisplayNameDescription(SR.Keys.CachedPlanSize, SR.Keys.CachedPlanSizeDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(KBSizeTypeConverter))]
        public int CachedPlanSize { get { return 0; } }

        [ShowInToolTip, DisplayOrder(7), DisplayNameDescription(SR.Keys.UsePlan), BetterValue(BetterValue.True)]
        public bool UsePlan { get { return false; } }

        [ShowInToolTip, DisplayOrder(7), DisplayNameDescription(SR.Keys.ContainsInlineScalarTsqlUdfs), BetterValue(BetterValue.None)]

        public bool ContainsInlineScalarTsqlUdfs { get { return false; } }

        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.EstimatedIoCost, SR.Keys.EstimatedIoCostDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateIO { get { return 0; } }

        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.DegreeOfParallelism, SR.Keys.DegreeOfParallelismDescription), BetterValue(BetterValue.HigherNumber)]
        public int DegreeOfParallelism { get { return 0; } }

        [ShowInToolTip, DisplayOrder(8), DisplayNameDescription(SR.Keys.EffectiveDegreeOfParallelism, SR.Keys.EffectiveDegreeOfParallelismDescription), BetterValue(BetterValue.HigherNumber)]
        public int EffectiveDegreeOfParallelism { get { return 0; } }

        [ShowInToolTip, DisplayOrder(9), DisplayNameDescription(SR.Keys.EstimatedCpuCost, SR.Keys.EstimatedCpuCostDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateCPU { get { return 0; } }

        [ShowInToolTip, DisplayOrder(9), DisplayNameDescription(SR.Keys.MemoryGrant, SR.Keys.MemoryGrantDescription), BetterValue(BetterValue.HigherNumber)]
        [TypeConverter(typeof(KBSizeTypeConverter))]
        public ulong MemoryGrant { get { return 0; } }

        [DisplayOrder(10), DisplayNameDescription(SR.Keys.ParameterList, SR.Keys.ParameterListDescription), BetterValue(BetterValue.None)]
        public object ParameterList { get { return null; } }

        [ShowInToolTip, DisplayOrder(10), DisplayNameDescription(SR.Keys.NumberOfExecutions, SR.Keys.NumberOfExecutionsDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double ActualExecutions { get { return 0; } }

        [ShowInToolTip, DisplayOrder(10), DisplayNameDescription(SR.Keys.EstimatedNumberOfExecutions, SR.Keys.EstimatedNumberOfExecutionsDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]

        public double EstimateExecutions { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(12), DisplayNameDescription(SR.Keys.ObjectShort, SR.Keys.ObjectDescription), BetterValue(BetterValue.None)]
        public object Object { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.IndexKind, SR.Keys.IndexKindDescription), BetterValue(BetterValue.None)]
        public string IndexKind { get { return null; } }

        [DisplayOrder(12), DisplayNameDescription(SR.Keys.OperationArgumentShort, SR.Keys.OperationArgumentDescription), BetterValue(BetterValue.None)]
        public string Argument { get { return null; } }

        [ShowInToolTip, DisplayOrder(111), DisplayNameDescription(SR.Keys.ActualRebinds, SR.Keys.ActualRebindsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualRebinds { get { return null; } }

        [ShowInToolTip, DisplayOrder(112), DisplayNameDescription(SR.Keys.ActualRewinds, SR.Keys.ActualRewindsDescription), BetterValue(BetterValue.HigherNumber)]

        public object ActualRewinds { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLocallyAggregatedRows, SR.Keys.ActualLocallyAggregatedRowsDescription), BetterValue(BetterValue.HigherNumber)]
        public object ActualLocallyAggregatedRows { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualElapsedms, SR.Keys.ActualElapsedmsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualElapsedms { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualCPUms, SR.Keys.ActualCPUmsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualCPUms { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualScans, SR.Keys.ActualScansDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualScans { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLogicalReads, SR.Keys.ActualLogicalReadsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualLogicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualPhysicalReads, SR.Keys.ActualPhysicalReadsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualPhysicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualPageServerReads, SR.Keys.ActualPageServerReadsDescription), BetterValue(BetterValue.HigherNumber)]
        public object ActualPageServerReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualReadAheads, SR.Keys.ActualReadAheadsDescription), BetterValue(BetterValue.HigherNumber)]
        public object ActualReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualPageServerReadAheads, SR.Keys.ActualPageServerReadAheadsDescription), BetterValue(BetterValue.HigherNumber)]
        public object ActualPageServerReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobLogicalReads, SR.Keys.ActualLobLogicalReadsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualLobLogicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobPhysicalReads, SR.Keys.ActualLobPhysicalReadsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualLobPhysicalReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobPageServerReads, SR.Keys.ActualLobPageServerReadsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualLobPageServerReads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobReadAheads, SR.Keys.ActualLobReadAheadsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualLobReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualLobPageServerReadAheads, SR.Keys.ActualLobPageServerReadAheadsDescription), BetterValue(BetterValue.HigherNumber)]
        public object ActualLobPageServerReadAheads { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualIOStatistics, SR.Keys.ActualIOStatisticsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualIOStatistics { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualTimeStatistics, SR.Keys.ActualTimeStatisticsDescription), BetterValue(BetterValue.LowerNumber)]
        public object ActualTimeStatistics { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.ActualMemoryGrantStats, SR.Keys.ActualMemoryGrantStats), BetterValue(BetterValue.HigherNumber)]
        public object ActualMemoryGrantStats { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcRowCount, SR.Keys.HpcRowCountDescription), BetterValue(BetterValue.HigherNumber)]
        public object HpcRowCount { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcKernelElapsedUs, SR.Keys.HpcKernelElapsedUsDescription), BetterValue(BetterValue.LowerNumber)]
        public object HpcKernelElapsedUs { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcHostToDeviceBytes, SR.Keys.HpcHostToDeviceBytesDescription), BetterValue(BetterValue.LowerNumber)]
        public object HpcHostToDeviceBytes { get { return null; } }

        [ShowInToolTip, DisplayOrder(221), DisplayNameDescription(SR.Keys.HpcDeviceToHostBytes, SR.Keys.HpcDeviceToHostBytesDescription), BetterValue(BetterValue.LowerNumber)]
        public object HpcDeviceToHostBytes { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.InputMemoryGrant, SR.Keys.InputMemoryGrant), BetterValue(BetterValue.LowerNumber)]
        public object InputMemoryGrant { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.OutputMemoryGrant, SR.Keys.OutputMemoryGrant), BetterValue(BetterValue.LowerNumber)]
        public object OutputMemoryGrant { get { return null; } }

        [DisplayOrder(221), DisplayNameDescription(SR.Keys.UsedMemoryGrant, SR.Keys.UsedMemoryGrant), BetterValue(BetterValue.LowerNumber)]
        public object UsedMemoryGrant { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.IsGraphDBTransitiveClosure, SR.Keys.IsGraphDBTransitiveClosureDescription), BetterValue(BetterValue.None)]
        public bool IsGraphDBTransitiveClosure { get { return false; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.IsInterleavedExecuted, SR.Keys.IsInterleavedExecutedDescription), BetterValue(BetterValue.None)]
        public bool IsInterleavedExecuted { get { return false; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.IsAdaptive, SR.Keys.IsAdaptiveDescription), BetterValue(BetterValue.None)]
        public bool IsAdaptive { get { return false; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.AdaptiveThresholdRows, SR.Keys.AdaptiveThresholdRowsDescription), BetterValue(BetterValue.None)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double AdaptiveThresholdRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.EstimatedJoinType, SR.Keys.EstimatedJoinTypeDescription), BetterValue(BetterValue.None)]
        public string EstimatedJoinType { get { return null; } }

        [ShowInToolTip, DisplayOrder(2), DisplayNameDescription(SR.Keys.ActualJoinType, SR.Keys.ActualJoinTypeDescription), BetterValue(BetterValue.None)]
        public string ActualJoinType { get { return null; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsPerExecution, SR.Keys.EstimatedNumberOfRowsPerExecutionDescription), BetterValue(BetterValue.HigherNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsPerExecution, SR.Keys.EstimatedNumberOfRowsPerExecutionDescription), BetterValue(BetterValue.HigherNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double StatementEstRows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsForAllExecutions, SR.Keys.EstimatedNumberOfRowsForAllExecutionsDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRowsAllExecs { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedNumberOfRowsForAllExecutions, SR.Keys.EstimatedNumberOfRowsForAllExecutionsDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double StatementEstRowsAllExecs { get { return 0; } }

        [ShowInToolTip, DisplayOrder(100), DisplayNameDescription(SR.Keys.EstimatedRowsRead, SR.Keys.EstimatedRowsReadDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimatedRowsRead { get { return 0; } }

        [DisplayOrder(101), DisplayNameDescription(SR.Keys.EstimatedRebinds, SR.Keys.EstimatedRebindsDescription), BetterValue(BetterValue.LowerNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRebinds { get { return 0; } }

        [DisplayOrder(102), DisplayNameDescription(SR.Keys.EstimatedRewinds, SR.Keys.EstimatedRewindsDescription), BetterValue(BetterValue.HigherNumber)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double EstimateRewinds { get { return 0; } }

        [DisplayOrder(200), DisplayNameDescription(SR.Keys.DefinedValues, SR.Keys.DefinedValuesDescription), BetterValue(BetterValue.None)]
        public string DefinedValues { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(201), DisplayNameDescription(SR.Keys.OutputList, SR.Keys.OutputListDescription), BetterValue(BetterValue.None)]
        public object OutputList { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(202), DisplayNameDescription(SR.Keys.Warnings, SR.Keys.WarningsDescription), BetterValue(BetterValue.None)]
        public object Warnings { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Parallel, SR.Keys.ParallelDescription), BetterValue(BetterValue.True)]
        public bool Parallel { get { return false; } }

        [DisplayOrder(204), DisplayNameDescription(SR.Keys.SetOptions, SR.Keys.SetOptionsDescription), BetterValue(BetterValue.None)]
        public object StatementSetOptions { get { return null; } }

        [DisplayOrder(205), DisplayNameDescription(SR.Keys.OptimizationLevel, SR.Keys.OptimizationLevelDescription), BetterValue(BetterValue.None)]
        public string StatementOptmLevel { get { return null; } }

        [DisplayOrder(206), DisplayNameDescription(SR.Keys.StatementOptmEarlyAbortReason), BetterValue(BetterValue.None)]
        public string StatementOptmEarlyAbortReason { get { return null; } }

        [DisplayOrder(211), DisplayNameDescription(SR.Keys.MemoryFractions, SR.Keys.MemoryFractionsDescription), BetterValue(BetterValue.LowerNumber)]
        public object MemoryFractions { get { return null; } }

        [DisplayOrder(211), DisplayNameDescription(SR.Keys.MemoryFractionsInput, SR.Keys.MemoryFractionsInputDescription), BetterValue(BetterValue.None)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double Input { get { return 0; } }

        [DisplayOrder(212), DisplayNameDescription(SR.Keys.MemoryFractionsOutput, SR.Keys.MemoryFractionsOutputDescription), BetterValue(BetterValue.None)]
        [TypeConverter(typeof(FloatTypeConverter))]
        public double Output { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteDestination, SR.Keys.RemoteDestinationDescription), BetterValue(BetterValue.None)]
        public string RemoteDestination { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteObject, SR.Keys.RemoteObjectDescription), BetterValue(BetterValue.None)]
        public string RemoteObject { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteSource, SR.Keys.RemoteSourceDescription), BetterValue(BetterValue.None)]
        public string RemoteSource { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.RemoteQuery, SR.Keys.RemoteQueryDescription), BetterValue(BetterValue.None)]
        public string RemoteQuery { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.UsedUdxColumns, SR.Keys.UsedUdxColumnsDescription), BetterValue(BetterValue.None)]
        public object UsedUDXColumns { get { return null; } }

        [ShowInToolTip, DisplayOrder(204), DisplayNameDescription(SR.Keys.UdxName, SR.Keys.UdxNameDescription), BetterValue(BetterValue.None)]
        public string UDXName { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.InnerSideJoinColumns, SR.Keys.InnerSideJoinColumnsDescription), BetterValue(BetterValue.None)]
        public object InnerSideJoinColumns { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(204), DisplayNameDescription(SR.Keys.OuterSideJoinColumns, SR.Keys.OuterSideJoinColumnsDescription), BetterValue(BetterValue.None)]
        public object OuterSideJoinColumns { get { return null; } }

        [DisplayOrder(205), DisplayNameDescription(SR.Keys.Residual, SR.Keys.ResidualDescription), BetterValue(BetterValue.None)]
        public string Residual { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(206), DisplayNameDescription(SR.Keys.PassThru, SR.Keys.PassThruDescription), BetterValue(BetterValue.None)]
        public string PassThru { get { return null; } }

        [ShowInToolTip, DisplayOrder(207), DisplayNameDescription(SR.Keys.ManyToMany, SR.Keys.ManyToManyDescription), BetterValue(BetterValue.None)]
        public bool ManyToMany { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.PartitionColumns, SR.Keys.PartitionColumnsDescription), BetterValue(BetterValue.None)]
        public object PartitionColumns { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(204), DisplayNameDescription(SR.Keys.OrderBy, SR.Keys.OrderByDescription), BetterValue(BetterValue.None)]
        public object OrderBy { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(205), DisplayNameDescription(SR.Keys.HashKeys, SR.Keys.HashKeysDescription), BetterValue(BetterValue.None)]
        public object HashKeys { get { return null; } }

        [ShowInToolTip, DisplayOrder(206), DisplayNameDescription(SR.Keys.ProbeColumn, SR.Keys.ProbeColumnDescription), BetterValue(BetterValue.None)]
        public object ProbeColumn { get { return null; } }

        [ShowInToolTip, DisplayOrder(207), DisplayNameDescription(SR.Keys.PartitioningType, SR.Keys.PartitioningTypeDescription), BetterValue(BetterValue.None)]
        public string PartitioningType { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.GroupBy, SR.Keys.GroupByDescription), BetterValue(BetterValue.None)]
        public object GroupBy { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.GroupingSets, SR.Keys.GroupingSetsDescription), BetterValue(BetterValue.None)]
        public object GroupingSets { get { return null; } }

        [DisplayOrder(200), DisplayNameDescription(SR.Keys.RollupInfo, SR.Keys.RollupInfoDescription), BetterValue(BetterValue.None)]
        public object RollupInfo { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.HighestLevel, SR.Keys.HighestLevelDescription), BetterValue(BetterValue.None)]
        public object HighestLevel { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.RollupLevel, SR.Keys.RollupLevelDescription), BetterValue(BetterValue.None)]
        [Browsable(true), ImmutableObject(true)]
        public object RollupLevel { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.Level, SR.Keys.LevelDescription), BetterValue(BetterValue.None)]
        public object Level { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.SegmentColumn, SR.Keys.SegmentColumnDescription), BetterValue(BetterValue.None)]
        public object SegmentColumn { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.HashKeysBuild, SR.Keys.HashKeysBuildDescription), BetterValue(BetterValue.None)]
        public object HashKeysBuild { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.HashKeysProbe, SR.Keys.HashKeysProbeDescription), BetterValue(BetterValue.None)]
        public object HashKeysProbe { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.BuildResidual, SR.Keys.BuildResidualDescription), BetterValue(BetterValue.None)]
        public string BuildResidual { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.ProbeResidual, SR.Keys.ProbeResidualDescription), BetterValue(BetterValue.None)]
        public string ProbeResidual { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SetPredicate, SR.Keys.SetPredicateDescription), BetterValue(BetterValue.None)]
        public string SetPredicate { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.RankColumns, SR.Keys.RankColumnsDescription), BetterValue(BetterValue.None)]
        public object RankColumns { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.ActionColumn, SR.Keys.ActionColumnDescription), BetterValue(BetterValue.None)]
        public object ActionColumn { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.OriginalActionColumn, SR.Keys.OriginalActionColumnDescription), BetterValue(BetterValue.None)]
        public object OriginalActionColumn { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Rows, SR.Keys.RowsDescription), BetterValue(BetterValue.HigherNumber)]
        public int Rows { get { return 0; } }

        [ShowInToolTip, DisplayOrder(150), DisplayNameDescription(SR.Keys.Partitioned, SR.Keys.PartitionedDescription),BetterValue(BetterValue.None)]
        public object Partitioned { get { return null; } }

        [DisplayOrder(156), DisplayNameDescription(SR.Keys.PartitionsAccessed), BetterValue(BetterValue.LowerNumber)]
        public object PartitionsAccessed { get { return null; } }

        [ShowInToolTip, DisplayOrder(152), DisplayNameDescription(SR.Keys.PartitionCount), BetterValue(BetterValue.LowerNumber)]
        public object PartitionCount { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.TieColumns, SR.Keys.TieColumnsDescription),BetterValue(BetterValue.None)]
        public object TieColumns { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.IsPercent, SR.Keys.IsPercentDescription),BetterValue(BetterValue.None)]
        public bool IsPercent { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.WithTies, SR.Keys.WithTiesDescription), BetterValue(BetterValue.None)]
        public bool WithTies { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.TopExpression, SR.Keys.TopExpressionDescription), BetterValue(BetterValue.None)]
        public string TopExpression { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Distinct, SR.Keys.DistinctDescription), BetterValue(BetterValue.None)]
        public bool Distinct { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(205), DisplayNameDescription(SR.Keys.OuterReferences, SR.Keys.OuterReferencesDescription), BetterValue(BetterValue.None)]
        public object OuterReferences { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.PartitionId, SR.Keys.PartitionIdDescription), BetterValue(BetterValue.None)]
        public object PartitionId { get { return null; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.Ordered, SR.Keys.OrderedDescription), BetterValue(BetterValue.None)]
        public bool Ordered { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.ScanDirection, SR.Keys.ScanDirectionDescription), BetterValue(BetterValue.None)]
        public object ScanDirection { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SeekPredicate, SR.Keys.SeekPredicateDescription), BetterValue(BetterValue.None)]
        public object SeekPredicate { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SeekPredicate, SR.Keys.SeekPredicateDescription), BetterValue(BetterValue.None)]
        public object SeekPredicateNew { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(203), DisplayNameDescription(SR.Keys.SeekPredicate, SR.Keys.SeekPredicateDescription), BetterValue(BetterValue.None)]
        public object SeekPredicatePart { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(205), DisplayNameDescription(SR.Keys.SeekPredicates, SR.Keys.SeekPredicatesDescription), BetterValue(BetterValue.None)]
        public string SeekPredicates { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.ForcedIndex, SR.Keys.ForcedIndexDescription), BetterValue(BetterValue.None)]
        public bool ForcedIndex { get { return false; } }

        [ShowInToolTip(LongString = true), DisplayOrder(5), DisplayNameDescription(SR.Keys.Values, SR.Keys.ValuesDescription), BetterValue(BetterValue.None)]
        public object Values { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.ColumnsWithNoStatistics, SR.Keys.ColumnsWithNoStatisticsDescription), BetterValue(BetterValue.None)]
        public object ColumnsWithNoStatistics { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.NoJoinPredicate, SR.Keys.NoJoinPredicateDescription), BetterValue(BetterValue.None)]
        public bool NoJoinPredicate { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.SpillToTempDb, SR.Keys.SpillToTempDbDescription), BetterValue(BetterValue.None)]
        public object SpillToTempDb { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.StartupExpression, SR.Keys.StartupExpressionDescription), BetterValue(BetterValue.None)]
        public bool StartupExpression { get { return false; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Query), BetterValue(BetterValue.None)]
        public string Query { get { return null; } }

        [DisplayOrder(203), DisplayNameDescription(SR.Keys.Stack), BetterValue(BetterValue.None)]
        public bool Stack { get { return false; } }

        [ShowInToolTip, DisplayOrder(203), DisplayNameDescription(SR.Keys.RowCount), BetterValue(BetterValue.HigherNumber)]
        public bool RowCount { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.Optimized), BetterValue(BetterValue.True)]
        public bool Optimized { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.WithPrefetch), BetterValue(BetterValue.None)]
        public bool WithPrefetch { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.Prefix), BetterValue(BetterValue.None)]
        public object Prefix { get { return null; } }

        [DisplayOrder(7), DisplayNameDescription(SR.Keys.StartRange, SR.Keys.StartRangeDescription), BetterValue(BetterValue.None)]
        public object StartRange { get { return null; } }

        [DisplayOrder(8), DisplayNameDescription(SR.Keys.EndRange, SR.Keys.EndRangeDescription), BetterValue(BetterValue.None)]
        public object EndRange { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.RangeColumns), BetterValue(BetterValue.None)]
        public object RangeColumns { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.RangeExpressions), BetterValue(BetterValue.None)]
        public object RangeExpressions { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ScanType), BetterValue(BetterValue.None)]
        public object ScanType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ColumnReference), BetterValue(BetterValue.None)]
        public object ColumnReference { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectServer, SR.Keys.ObjectServerDescription), BetterValue(BetterValue.None)]
        public string Server { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectDatabase, SR.Keys.ObjectDatabaseDescription), BetterValue(BetterValue.None)]
        public string Database { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectIndex, SR.Keys.ObjectIndexDescription), BetterValue(BetterValue.None)]
        public string Index { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectSchema, SR.Keys.ObjectSchemaDescription), BetterValue(BetterValue.None)]
        public string Schema { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectTable, SR.Keys.ObjectTableDescription), BetterValue(BetterValue.None)]
        public string Table { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectAlias, SR.Keys.ObjectAliasDescription), BetterValue(BetterValue.None)]
        public string Alias { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectColumn, SR.Keys.ObjectColumnDescription), BetterValue(BetterValue.None)]
        public string Column { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ObjectComputedColumn, SR.Keys.ObjectComputedColumnDescription), BetterValue(BetterValue.None)]
        public bool ComputedColumn { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ParameterDataType), BetterValue(BetterValue.None)]
        public string ParameterDataType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ParameterCompiledValue), BetterValue(BetterValue.None)]
        public string ParameterCompiledValue { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ParameterRuntimeValue), BetterValue(BetterValue.None)]
        public string ParameterRuntimeValue { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorPlan), BetterValue(BetterValue.None)]
        public object CursorPlan { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorOperation), BetterValue(BetterValue.None)]
        public object Operation { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorName), BetterValue(BetterValue.None)]
        public string CursorName { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorActualType), BetterValue(BetterValue.None)]
        public object CursorActualType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorRequestedType), BetterValue(BetterValue.None)]
        public object CursorRequestedType { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.CursorConcurrency), BetterValue(BetterValue.None)]
        public object CursorConcurrency { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.ForwardOnly), BetterValue(BetterValue.None)]
        public bool ForwardOnly { get { return false; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.QueryPlan), BetterValue(BetterValue.None)]
        public object QueryPlan { get { return null; } }

        [DisplayOrder(6), DisplayNameDescription(SR.Keys.OperationType), BetterValue(BetterValue.None)]
        public object OperationType { get { return null; } }

        [ShowInToolTip, DisplayOrder(300), DisplayNameDescription(SR.Keys.NodeId), BetterValue(BetterValue.None)]
        public int NodeId { get { return 0; } }

        [ShowInToolTip, DisplayOrder(301), DisplayNameDescription(SR.Keys.PrimaryNodeId), BetterValue(BetterValue.None)]
        public int PrimaryNodeId { get { return 0; } }

        [ShowInToolTip, DisplayOrder(302), DisplayNameDescription(SR.Keys.ForeignKeyReferencesCount), BetterValue(BetterValue.None)]
        public int ForeignKeyReferencesCount { get { return 0; } }

        [ShowInToolTip, DisplayOrder(303), DisplayNameDescription(SR.Keys.NoMatchingIndexCount), BetterValue(BetterValue.None)]
        public int NoMatchingIndexCount { get { return 0; } }

        [ShowInToolTip, DisplayOrder(304), DisplayNameDescription(SR.Keys.PartialMatchingIndexCount), BetterValue(BetterValue.None)]
        public int PartialMatchingIndexCount { get { return 0; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.WhereJoinColumns), BetterValue(BetterValue.None)]
        public object WhereJoinColumns { get { return null; } }

        [ShowInToolTip(LongString = true), DisplayOrder(6), DisplayNameDescription(SR.Keys.ProcName), BetterValue(BetterValue.None)]
        public string ProcName { get { return null; } }

        [DisplayOrder(400), DisplayNameDescription(SR.Keys.InternalInfo), BetterValue(BetterValue.None)]
        public object InternalInfo { get { return null; } }

        [ShowInToolTip, DisplayOrder(220), DisplayNameDescription(SR.Keys.RemoteDataAccess, SR.Keys.RemoteDataAccessDescription), BetterValue(BetterValue.None)]
        public bool RemoteDataAccess { get { return false; } }

        [DisplayOrder(220), DisplayNameDescription(SR.Keys.CloneAccessScope, SR.Keys.CloneAccessScopeDescription), BetterValue(BetterValue.None)]
        public string CloneAccessScope { get { return null; } }

        [ShowInToolTip, DisplayOrder(220), DisplayNameDescription(SR.Keys.Remoting, SR.Keys.RemotingDescription), BetterValue(BetterValue.None)]
        public bool Remoting { get { return false; } }

        [DisplayOrder(201), DisplayNameDescription(SR.Keys.Activation), BetterValue(BetterValue.None)]
        public object Activation { get { return null; } }

        [DisplayOrder(201), DisplayNameDescription(SR.Keys.BrickRouting), BetterValue(BetterValue.None)]
        public object BrickRouting { get { return null; } }

        [DisplayOrder(201), DisplayNameDescription(SR.Keys.FragmentIdColumn), BetterValue(BetterValue.None)]
        public object FragmentIdColumn { get { return null; } }
        public string CardinalityEstimationModelVersion { get { return null; } }

        [BetterValue(BetterValue.LowerNumber)]
        public string CompileCPU { get { return null; } }

        [BetterValue(BetterValue.LowerNumber)]
        public string CompileMemory { get { return null; } }

        [BetterValue(BetterValue.LowerNumber)]
        public string CompileTime { get { return null; } }

        [BetterValue(BetterValue.None)]
        public string NonParallelPlanReason { get { return null; } }

        [BetterValue(BetterValue.None)]
        public string QueryHash { get { return null; } }

        [BetterValue(BetterValue.None)]
        public string QueryPlanHash { get { return null; } }

        [BetterValue(BetterValue.True)]
        public bool RetrievedFromCache { get { return false; } }

        [BetterValue(BetterValue.None)]
        public bool SecurityPolicyApplied { get { return false; } }

        [BetterValue(BetterValue.None)]
        public bool NoExpandHint { get { return false; } }
        
        [BetterValue(BetterValue.None)]
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
