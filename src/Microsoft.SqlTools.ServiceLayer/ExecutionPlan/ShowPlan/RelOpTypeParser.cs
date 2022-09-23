//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    internal sealed class RelOpTypeParser : XmlPlanParser
    {
        #region Constants
        private const string OPERATION_INDEX_DELETE = "IndexDelete";
        private const string OPERATION_CLUSTERED_INDEX_DELETE = "ClusteredIndexDelete";
        private const string OPERATION_COLUMNSTORE_INDEX_DELETE = "ColumnstoreIndexDelete";

        private const string OPERATION_INDEX_INSERT = "IndexInsert";
        private const string OPERATION_CLUSTERED_INDEX_INSERT = "ClusteredIndexInsert";
        private const string OPERATION_COLUMNSTORE_INDEX_INSERT = "ColumnstoreIndexInsert";

        private const string OPERATION_INDEX_MERGE = "IndexMerge";
        private const string OPERATION_CLUSTERED_INDEX_MERGE = "ClusteredIndexMerge";
        private const string OPERATION_COLUMNSTORE_INDEX_MERGE = "ColumnstoreIndexMerge";

        private const string OPERATION_INDEX_SCAN = "IndexScan";
        private const string OPERATION_CLUSTERED_INDEX_SCAN = "ClusteredIndexScan";
        private const string OPERATION_COLUMNSTORE_INDEX_SCAN = "ColumnstoreIndexScan";

        private const string OPERATION_INDEX_UPDATE = "IndexUpdate";
        private const string OPERATION_CLUSTERED_INDEX_UPDATE = "ClusteredIndexUpdate";
        private const string OPERATION_COLUMNSTORE_INDEX_UPDATE = "ColumnstoreIndexUpdate";

        private const string OBJECT_NODE = "Object";
        private const string STORAGE_PROPERTY = "Storage";
        #endregion

        /// <summary>
        /// Creates new node and adds it to the graph.
        /// </summary>
        /// <param name="item">Item being parsed.</param>
        /// <param name="parentItem">Parent item.</param>
        /// <param name="parentNode">Parent node.</param>
        /// <param name="context">Node builder context.</param>
        /// <returns>The node that corresponds to the item being parsed.</returns>
        public override Node GetCurrentNode(object item, object parentItem, Node parentNode, NodeBuilderContext context)
        {
            return NewNode(context);
        }

        /// <summary>
        /// Enumerates children items of the item being parsed.
        /// </summary>
        /// <param name="parsedItem">The item being parsed.</param>
        /// <returns>Enumeration.</returns>
        public override IEnumerable GetChildren(object parsedItem)
        {
            RelOpType item = parsedItem as RelOpType;
            if (item.Item != null)
            {
                yield return item.Item;
            }

            yield break;
        }

        /// <summary>
        /// Determines Operation that corresponds to the object being parsed.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Operation that corresponds to the node.</returns>
        protected override Operation GetNodeOperation(Node node)
        {
            object physicalOpType = node["PhysicalOp"];
            object logicalOpType = node["LogicalOp"];

            if (physicalOpType == null || logicalOpType == null)
            {
                throw new FormatException(SR.Keys.UnknownShowPlanSource);
            }

            string physicalOpTypeName = physicalOpType.ToString();
            string logicalOpTypeName = logicalOpType.ToString();

            // SQLBU# 434739: Custom description and icons for KeyLookup operation:
            //
            // SQL Server 2005 doesnt expose 'KeyLookup' operations as thier own type,
            // instead they indicate Bookmark operations as a 'ClusteredIndexSeek' op
            // that is having Lookup=true. Users have to select the actual node to tell
            // if a ClusteredIndexSeek is an actual bookmark operation or not.
            //
            // Our request for having engine expose the Bookmark operation as its own type,
            // instead of exposing it as a 'ClusteredIndexSeek' cannot be addressed by
            // engine in SP2 timeframe (reasons include compatibility on published showplanxml.xsd
            // schema as well as amount of changes in components that consume the xml showplan)
            //
            // For SP2 timeframe the solution is to do an aesthetic only change:
            // SSMS interprets the xml showplan and provides custom icons and descriptions
            // for a new operation: 'KeyLookup', that is getting documented in BOL.
            const string operationClusteredIndexSeek = "ClusteredIndexSeek";
            const string operationKeyLookup = "KeyLookup";

            object lookup = node["Lookup"];
            if ((lookup != null) && (lookup is System.Boolean))
            {
                if (Convert.ToBoolean(lookup) == true)
                {
                    if (0 == string.Compare(physicalOpTypeName, operationClusteredIndexSeek, StringComparison.OrdinalIgnoreCase))
                    {
                        physicalOpTypeName = operationKeyLookup;
                    }
                    if (0 == string.Compare(logicalOpTypeName, operationClusteredIndexSeek, StringComparison.OrdinalIgnoreCase))
                    {
                        logicalOpTypeName = operationKeyLookup;
                    }
                }
            }

            /*
             * For index scans, Storage property should be read from this node.
             * Otherwise, for DML operations, Storage property should be read from this node's child "Object" element.
             */
            if (0 == string.Compare(physicalOpTypeName, OPERATION_INDEX_SCAN, StringComparison.OrdinalIgnoreCase) ||
                0 == string.Compare(physicalOpTypeName, OPERATION_CLUSTERED_INDEX_SCAN, StringComparison.OrdinalIgnoreCase))
            {
                object storage = node[STORAGE_PROPERTY];
                if ((storage != null) && (storage.Equals(StorageType.ColumnStore)))
                {
                    physicalOpTypeName = OPERATION_COLUMNSTORE_INDEX_SCAN;
                }
            }
            else
            {
                ExpandableObjectWrapper objectWrapper = (ExpandableObjectWrapper)node[OBJECT_NODE];
                if (objectWrapper != null)
                {
                    PropertyValue storagePropertyValue = (PropertyValue)objectWrapper.Properties[STORAGE_PROPERTY];

                    /*
                     * If object's storage is of type Storage.Columnstore,
                     * PhysicalOperations should be updated to their columnstore counterparts.
                     */
                    if (storagePropertyValue != null && ((storagePropertyValue).Value.Equals(StorageType.ColumnStore)))
                    {
                        if (0 == string.Compare(physicalOpTypeName, OPERATION_INDEX_DELETE, StringComparison.OrdinalIgnoreCase) ||
                            0 == string.Compare(physicalOpTypeName, OPERATION_CLUSTERED_INDEX_DELETE, StringComparison.OrdinalIgnoreCase))
                        {
                            physicalOpTypeName = OPERATION_COLUMNSTORE_INDEX_DELETE;
                        }
                        else if (0 == string.Compare(physicalOpTypeName, OPERATION_INDEX_INSERT, StringComparison.OrdinalIgnoreCase) ||
                                 0 == string.Compare(physicalOpTypeName, OPERATION_CLUSTERED_INDEX_INSERT, StringComparison.OrdinalIgnoreCase))
                        {
                            physicalOpTypeName = OPERATION_COLUMNSTORE_INDEX_INSERT;
                        }
                        else if (0 == string.Compare(physicalOpTypeName, OPERATION_INDEX_MERGE, StringComparison.OrdinalIgnoreCase) ||
                                 0 == string.Compare(physicalOpTypeName, OPERATION_CLUSTERED_INDEX_MERGE, StringComparison.OrdinalIgnoreCase))
                        {
                            physicalOpTypeName = OPERATION_COLUMNSTORE_INDEX_MERGE;
                        }
                        else if (0 == string.Compare(physicalOpTypeName, OPERATION_INDEX_UPDATE, StringComparison.OrdinalIgnoreCase) ||
                                 0 == string.Compare(physicalOpTypeName, OPERATION_CLUSTERED_INDEX_UPDATE, StringComparison.OrdinalIgnoreCase))
                        {
                            physicalOpTypeName = OPERATION_COLUMNSTORE_INDEX_UPDATE;
                        }
                    }
                }
            }

            Operation physicalOp = OperationTable.GetPhysicalOperation(physicalOpTypeName);
            Operation logicalOp = OperationTable.GetLogicalOperation(logicalOpTypeName);

            Operation resultOp = logicalOp != null && logicalOp.Image != null && logicalOp.Description != null
                ? logicalOp : physicalOp;

            node.LogicalOpUnlocName = logicalOpTypeName;
            node.PhysicalOpUnlocName = physicalOpTypeName;
            node["PhysicalOp"] = physicalOp.DisplayName;
            node["LogicalOp"] = logicalOp.DisplayName;

            Debug.Assert(logicalOp.DisplayName != null);
            Debug.Assert(physicalOp.DisplayName != null);
            Debug.Assert(resultOp.Description != null);
            Debug.Assert(resultOp.Image != null);

            return resultOp;
        }

        /// <summary>
        /// Determines node subtree cost from existing node properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        /// <returns>Node subtree cost.</returns>
        protected override double GetNodeSubtreeCost(Node node)
        {
            object value = node["PDWAccumulativeCost"] ?? node["EstimatedTotalSubtreeCost"];
            return value != null ? Convert.ToDouble(value, CultureInfo.CurrentCulture) : 0;
        }

        /// <summary>
        /// Updates node special properties such as Operator, Cost, SubtreeCost.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        public override void ParseProperties(object parsedItem, PropertyDescriptorCollection targetPropertyBag, NodeBuilderContext context)
        {
            base.ParseProperties(parsedItem, targetPropertyBag, context);

            RelOpType item = parsedItem as RelOpType;
            Debug.Assert(item != null);

            if (item.RunTimeInformation != null && item.RunTimeInformation.Length > 0)
            {
                RunTimeCounters actualRowCountCounter = new RunTimeCounters();
                RunTimeCounters actualRowsReadCountCounter = new RunTimeCounters();
                RunTimeCounters actualBatchCountCounter = new RunTimeCounters();
                RunTimeCounters actualRebindsCounter = new RunTimeCounters();
                RunTimeCounters actualRewindsCounter = new RunTimeCounters();
                RunTimeCounters actualExecutionsCounter = new RunTimeCounters();
                RunTimeCounters actualLocallyAggregatedRowsCountCounter = new RunTimeCounters();
                RunTimeCounters actualElapsedTimeCounter = new RunTimeCounters { DisplayTotalCounters = false };
                RunTimeCounters actualElapsedCPUTimeCounter = new RunTimeCounters();
                RunTimeCounters actualScansCounter = new RunTimeCounters();
                RunTimeCounters actualLogicalReadsCounter = new RunTimeCounters();
                RunTimeCounters actualPhysicalReadsCounter = new RunTimeCounters();
                RunTimeCounters actualPageServerReadsCounter = new RunTimeCounters();
                RunTimeCounters actualReadAheadsCounter = new RunTimeCounters();
                RunTimeCounters actualPageServerReadAheadsCounter = new RunTimeCounters();
                RunTimeCounters actualLobLogicalReadsCounter = new RunTimeCounters();
                RunTimeCounters actualLobPhysicalReadsCounter = new RunTimeCounters();
                RunTimeCounters actualLobPageServerReadsCounter = new RunTimeCounters();
                RunTimeCounters actualLobReadAheadsCounter = new RunTimeCounters();
                RunTimeCounters actualLobPageServerReadAheadsCounter = new RunTimeCounters();
                RunTimeCounters actualInputMemoryGrantCounter = new MemGrantRunTimeCounters();
                RunTimeCounters actualOutputMemoryGrantCounter = new MemGrantRunTimeCounters();
                RunTimeCounters actualUsedMemoryGrantCounter = new RunTimeCounters();

                RunTimeCounters hpcKernelElapsedUsCounter = new RunTimeCounters();
                RunTimeCounters hpcRowCountCounter = new RunTimeCounters();
                RunTimeCounters hpcHostToDeviceBytesCounter = new RunTimeCounters();
                RunTimeCounters hpcDeviceToHostBytesCounter = new RunTimeCounters();

                ExpandableObjectWrapper actualTimeStatsObjWrapper = new ExpandableObjectWrapper();
                ExpandableObjectWrapper actualIOStatsObjWrapper = new ExpandableObjectWrapper();
                ExpandableObjectWrapper actualMemoryGrantStatsObjWrapper = new ExpandableObjectWrapper();

                String actualExecutionModeValue = String.Empty;
                String actualJoinTypeValue = String.Empty;
                bool actualIsInterleavedExecuted = false;

                foreach (RunTimeInformationTypeRunTimeCountersPerThread counter in item.RunTimeInformation)
                {
                    if (counter.BrickIdSpecified)
                    {
                        actualRowCountCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualRows);
                        actualRebindsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualRebinds);
                        actualRewindsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualRewinds);
                        actualExecutionsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualExecutions);
                        actualLocallyAggregatedRowsCountCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLocallyAggregatedRows);

                        if (counter.ActualElapsedmsSpecified)
                        {
                            actualElapsedTimeCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualElapsedms);
                        }

                        if (counter.ActualCPUmsSpecified)
                        {
                            actualElapsedCPUTimeCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualCPUms);
                        }

                        if (counter.ActualScansSpecified)
                        {
                            actualScansCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualScans);
                        }

                        if (counter.ActualLogicalReadsSpecified)
                        {
                            actualLogicalReadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLogicalReads);
                        }

                        if (counter.ActualPhysicalReadsSpecified)
                        {
                            actualPhysicalReadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualPhysicalReads);
                        }

                        if (counter.ActualPageServerReadsSpecified)
                        {
                            actualPageServerReadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualPageServerReads);
                        }

                        if (counter.ActualReadAheadsSpecified)
                        {
                            actualReadAheadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualReadAheads);
                        }

                        if (counter.ActualPageServerReadAheadsSpecified)
                        {
                            actualPageServerReadAheadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualPageServerReadAheads);
                        }

                        if (counter.ActualLobLogicalReadsSpecified)
                        {
                            actualLobLogicalReadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLobLogicalReads);
                        }

                        if (counter.ActualLobPhysicalReadsSpecified)
                        {
                            actualLobPhysicalReadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLobPhysicalReads);
                        }

                        if (counter.ActualLobPageServerReadsSpecified)
                        {
                            actualLobPageServerReadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLobPageServerReads);
                        }

                        if (counter.ActualLobReadAheadsSpecified)
                        {
                            actualLobReadAheadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLobReadAheads);
                        }

                        if (counter.ActualLobPageServerReadAheadsSpecified)
                        {
                            actualLobPageServerReadAheadsCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualLobPageServerReadAheads);
                        }

                        if (counter.ActualRowsReadSpecified)
                        {
                            actualRowsReadCountCounter.AddCounter(counter.Thread, counter.BrickId, counter.ActualRowsRead);
                        }

                        if (counter.BatchesSpecified)
                        {
                            actualBatchCountCounter.AddCounter(counter.Thread, counter.BrickId, counter.Batches);
                        }

                        if (counter.HpcRowCountSpecified)
                        {
                            hpcRowCountCounter.AddCounter(counter.Thread, counter.BrickId, counter.HpcRowCount);
                        }

                        if (counter.HpcKernelElapsedUsSpecified)
                        {
                            hpcKernelElapsedUsCounter.AddCounter(counter.Thread, counter.BrickId, counter.HpcKernelElapsedUs);
                        }

                        if (counter.HpcHostToDeviceBytesSpecified)
                        {
                            hpcHostToDeviceBytesCounter.AddCounter(counter.Thread, counter.BrickId, counter.HpcHostToDeviceBytes);
                        }

                        if (counter.HpcDeviceToHostBytesSpecified)
                        {
                            hpcDeviceToHostBytesCounter.AddCounter(counter.Thread, counter.BrickId, counter.HpcDeviceToHostBytes);
                        }

                        if (counter.InputMemoryGrantSpecified)
                        {
                            actualInputMemoryGrantCounter.AddCounter(counter.Thread, counter.BrickId, counter.InputMemoryGrant);
                        }

                        if (counter.OutputMemoryGrantSpecified)
                        {
                            actualOutputMemoryGrantCounter.AddCounter(counter.Thread, counter.BrickId, counter.OutputMemoryGrant);
                        }

                        if (counter.UsedMemoryGrantSpecified)
                        {
                            actualUsedMemoryGrantCounter.AddCounter(counter.Thread, counter.BrickId, counter.UsedMemoryGrant);
                        }
                    }
                    else
                    {
                        actualRowCountCounter.AddCounter(counter.Thread, counter.ActualRows);
                        actualRebindsCounter.AddCounter(counter.Thread, counter.ActualRebinds);
                        actualRewindsCounter.AddCounter(counter.Thread, counter.ActualRewinds);
                        actualExecutionsCounter.AddCounter(counter.Thread, counter.ActualExecutions);
                        actualLocallyAggregatedRowsCountCounter.AddCounter(counter.Thread, counter.ActualLocallyAggregatedRows);

                        if (counter.ActualElapsedmsSpecified)
                        {
                            actualElapsedTimeCounter.AddCounter(counter.Thread, counter.ActualElapsedms);
                        }

                        if (counter.ActualCPUmsSpecified)
                        {
                            actualElapsedCPUTimeCounter.AddCounter(counter.Thread, counter.ActualCPUms);
                        }

                        if (counter.ActualScansSpecified)
                        {
                            actualScansCounter.AddCounter(counter.Thread, counter.ActualScans);
                        }

                        if (counter.ActualLogicalReadsSpecified)
                        {
                            actualLogicalReadsCounter.AddCounter(counter.Thread, counter.ActualLogicalReads);
                        }

                        if (counter.ActualPhysicalReadsSpecified)
                        {
                            actualPhysicalReadsCounter.AddCounter(counter.Thread, counter.ActualPhysicalReads);
                        }

                        if (counter.ActualPageServerReadsSpecified)
                        {
                            actualPageServerReadsCounter.AddCounter(counter.Thread, counter.ActualPageServerReads);
                        }

                        if (counter.ActualReadAheadsSpecified)
                        {
                            actualReadAheadsCounter.AddCounter(counter.Thread, counter.ActualReadAheads);
                        }

                        if (counter.ActualPageServerReadAheadsSpecified)
                        {
                            actualPageServerReadAheadsCounter.AddCounter(counter.Thread, counter.ActualPageServerReadAheads);
                        }

                        if (counter.ActualLobLogicalReadsSpecified)
                        {
                            actualLobLogicalReadsCounter.AddCounter(counter.Thread, counter.ActualLobLogicalReads);
                        }

                        if (counter.ActualLobPhysicalReadsSpecified)
                        {
                            actualLobPhysicalReadsCounter.AddCounter(counter.Thread, counter.ActualLobPhysicalReads);
                        }

                        if (counter.ActualLobPageServerReadsSpecified)
                        {
                            actualLobPageServerReadsCounter.AddCounter(counter.Thread, counter.ActualLobPageServerReads);
                        }

                        if (counter.ActualLobReadAheadsSpecified)
                        {
                            actualLobReadAheadsCounter.AddCounter(counter.Thread, counter.ActualLobReadAheads);
                        }

                        if (counter.ActualLobPageServerReadAheadsSpecified)
                        {
                            actualLobPageServerReadAheadsCounter.AddCounter(counter.Thread, counter.ActualLobPageServerReadAheads);
                        }

                        if (counter.ActualRowsReadSpecified)
                        {
                            actualRowsReadCountCounter.AddCounter(counter.Thread, counter.ActualRowsRead);
                        }

                        if (counter.BatchesSpecified)
                        {
                            actualBatchCountCounter.AddCounter(counter.Thread, counter.Batches);
                        }

                        if (counter.HpcRowCountSpecified)
                        {
                            hpcRowCountCounter.AddCounter(counter.Thread, counter.HpcRowCount);
                        }

                        if (counter.HpcKernelElapsedUsSpecified)
                        {
                            hpcKernelElapsedUsCounter.AddCounter(counter.Thread, counter.HpcKernelElapsedUs);
                        }

                        if (counter.HpcHostToDeviceBytesSpecified)
                        {
                            hpcHostToDeviceBytesCounter.AddCounter(counter.Thread, counter.HpcHostToDeviceBytes);
                        }

                        if (counter.HpcDeviceToHostBytesSpecified)
                        {
                            hpcDeviceToHostBytesCounter.AddCounter(counter.Thread, counter.HpcDeviceToHostBytes);
                        }

                        if (counter.InputMemoryGrantSpecified)
                        {
                            actualInputMemoryGrantCounter.AddCounter(counter.Thread, counter.InputMemoryGrant);
                        }

                        if (counter.OutputMemoryGrantSpecified)
                        {
                            actualOutputMemoryGrantCounter.AddCounter(counter.Thread, counter.OutputMemoryGrant);
                        }

                        if (counter.UsedMemoryGrantSpecified)
                        {
                            actualUsedMemoryGrantCounter.AddCounter(counter.Thread, counter.UsedMemoryGrant);
                        }
                    }

                    if (counter.ActualExecutions > 0)
                    {
                        actualExecutionModeValue = Enum.GetName(typeof(ExecutionModeType), counter.ActualExecutionMode);
                    }

                    if (counter.ActualJoinTypeSpecified)
                    {
                        actualJoinTypeValue = Enum.GetName(typeof(PhysicalOpType), counter.ActualJoinType);
                    }

                    if (counter.IsInterleavedExecuted)
                    {
                        actualIsInterleavedExecuted = true;
                    }
                }

                if (actualIsInterleavedExecuted)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("IsInterleavedExecuted", actualIsInterleavedExecuted));
                }

                // Create localizable properties and add them to the property bag
                targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualRows", actualRowCountCounter));
                targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualBatches", actualBatchCountCounter));
                targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualRebinds", actualRebindsCounter));
                targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualRewinds", actualRewindsCounter));
                targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualExecutions", actualExecutionsCounter));

                if (actualRowsReadCountCounter.TotalCounters > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualRowsRead", actualRowsReadCountCounter));
                }

                if (actualLocallyAggregatedRowsCountCounter.TotalCounters > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualLocallyAggregatedRows", actualLocallyAggregatedRowsCountCounter));
                }

                if (hpcRowCountCounter.TotalCounters > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("HpcRowCount", hpcRowCountCounter));
                }

                if (hpcKernelElapsedUsCounter.TotalCounters > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("HpcKernelElapsedUs", hpcKernelElapsedUsCounter));
                }

                if (hpcHostToDeviceBytesCounter.TotalCounters > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("HpcHostToDeviceBytes", hpcHostToDeviceBytesCounter));
                }

                if (hpcDeviceToHostBytesCounter.TotalCounters > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("HpcDeviceToHostBytes", hpcDeviceToHostBytesCounter));
                }

                if (!String.IsNullOrEmpty(actualExecutionModeValue))
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualExecutionMode", actualExecutionModeValue));
                }


                if (!String.IsNullOrEmpty(actualJoinTypeValue))
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualJoinType", actualJoinTypeValue));
                }

                // Populate the "Actual Time Statistics" property if applicable
                // Nested properties include "Actual Elapsed Time" and "Actual Elapsed CPU Time"
                if (actualElapsedTimeCounter.NumOfCounters > 0)
                {
                    actualTimeStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualElapsedms", actualElapsedTimeCounter));
                }

                if (actualElapsedCPUTimeCounter.NumOfCounters > 0)
                {
                    actualTimeStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualCPUms", actualElapsedCPUTimeCounter));
                }

                if (actualTimeStatsObjWrapper.Properties.Count > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualTimeStatistics", actualTimeStatsObjWrapper));
                }

                // Populate the "Actual IO Statistics" property if applicable
                // Nested properties include "Scan" and "Read" properties.
                if (actualScansCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualScans", actualScansCounter));
                }

                if (actualLogicalReadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualLogicalReads", actualLogicalReadsCounter));
                }

                if (actualPhysicalReadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualPhysicalReads", actualPhysicalReadsCounter));
                }

                if (actualPageServerReadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualPageServerReads", actualPageServerReadsCounter));
                }

                if (actualReadAheadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualReadAheads", actualReadAheadsCounter));
                }

                if (actualPageServerReadAheadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualPageServerReadAheads", actualPageServerReadAheadsCounter));
                }

                if (actualLobLogicalReadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualLobLogicalReads", actualLobLogicalReadsCounter));
                }

                if (actualLobPhysicalReadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualLobPhysicalReads", actualLobPhysicalReadsCounter));
                }

                if (actualLobPageServerReadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualLobPageServerReads", actualLobPageServerReadsCounter));
                }

                if (actualLobReadAheadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualLobReadAheads", actualLobReadAheadsCounter));
                }

                if (actualLobPageServerReadAheadsCounter.NumOfCounters > 0)
                {
                    actualIOStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("ActualLobPageServerReadAheads", actualLobPageServerReadAheadsCounter));
                }

                if (actualIOStatsObjWrapper.Properties.Count > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualIOStatistics", actualIOStatsObjWrapper));
                }

                // Populate ActualMemoryGrantStats
                if (actualInputMemoryGrantCounter.NumOfCounters > 0)
                {
                    actualMemoryGrantStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("InputMemoryGrant", actualInputMemoryGrantCounter));
                }

                if (actualOutputMemoryGrantCounter.NumOfCounters > 0)
                {
                    actualMemoryGrantStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("OutputMemoryGrant", actualOutputMemoryGrantCounter));
                }

                if (actualUsedMemoryGrantCounter.NumOfCounters > 0)
                {
                    actualMemoryGrantStatsObjWrapper.Properties.Add(PropertyFactory.CreateProperty("UsedMemoryGrant", actualUsedMemoryGrantCounter));
                }

                if (actualMemoryGrantStatsObjWrapper.Properties.Count > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("ActualMemoryGrantStats", actualMemoryGrantStatsObjWrapper));
                }
            }

            // Decompose RunTimePartitionSummary and add them individually.
            // Otherwise, the properties will show up as nested in the property window.
            if (item.RunTimePartitionSummary != null && item.RunTimePartitionSummary.PartitionsAccessed != null)
            {
                RunTimePartitionSummaryTypePartitionsAccessed partitions = item.RunTimePartitionSummary.PartitionsAccessed;

                // Create localizable properties and add them to the property bag
                targetPropertyBag.Add(PropertyFactory.CreateProperty("PartitionCount", partitions.PartitionCount));

                if (partitions.PartitionRange != null && partitions.PartitionRange.Length > 0)
                {
                    targetPropertyBag.Add(PropertyFactory.CreateProperty("PartitionsAccessed", GetPartitionRangeString(partitions.PartitionRange)));
                }
            }
        }


        /// <summary>
        /// Helper method to format partition range string.
        /// </summary>
        /// <param name="ranges">Partition ranges</param>
        /// <returns>property string</returns>
        private static string GetPartitionRangeString(RunTimePartitionSummaryTypePartitionsAccessedPartitionRange[] ranges)
        {
            Debug.Assert(ranges != null);
            StringBuilder stringBuilder = new StringBuilder();
            string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            for (int i = 0; i < ranges.Length; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append(separator);
                }

                RunTimePartitionSummaryTypePartitionsAccessedPartitionRange range = ranges[i];
                if (range.Start == range.End)
                {
                    // The range is a single number
                    stringBuilder.Append(range.Start);
                }
                else
                {
                    stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "{0}..{1}", range.Start, range.End);
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private RelOpTypeParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static RelOpTypeParser relOpTypeParser = null;
        public static RelOpTypeParser Instance
        {
            get
            {
                relOpTypeParser ??= new RelOpTypeParser();
                return relOpTypeParser;
            }
        }
    }
}
