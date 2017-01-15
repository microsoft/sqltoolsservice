//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class Session
    {

        #region Member Variables

        private long nextRowId;
        private readonly ConcurrentDictionary<long, RowUpdateBase> updateCache;
        private readonly ResultSet associatedResultSet;

        #endregion

        public Session(Query query)
        {
            // Determine if the query is valid for editing
            // @TODO: Refine this criteria once we replace the batch parser
            // Criterion 1) Query has finished executing
            if (!query.HasExecuted)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Query has not completed execution");
            }

            // Criterion 2) Query only has a single result set
            ResultSet[] queryResultSets = query.Batches.SelectMany(b => b.ResultSets).ToArray();
            if (queryResultSets.Length == 0)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Query did not generate any result sets");
            }
            if (queryResultSets.Length > 1)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Query has generated more than one result sets");
            }

            // Setup the internal state
            AssociatedQuery = query;
            associatedResultSet = queryResultSets[0];
            nextRowId = associatedResultSet.RowCount;
            updateCache = new List<RowUpdateBase>();
        }

        #region Public Methods

        /// <summary>
        /// Creates a new row update and adds it to the update cache
        /// </summary>
        /// <exception cref="Exception">If inserting into cache fails</exception>
        /// <returns>The internal ID of the newly created row</returns>
        public long CreateRow()
        {
            // Create a new row ID (atomically, since this could be accesses concurrently)
            long newRowId = nextRowId++;

            // Create a new row create update and add to the update cache
            RowCreate newRow = new RowCreate();
            if (!updateCache.TryAdd(newRowId, newRow))
            {
                // Revert the next row ID
                nextRowId--;

                // @TODO: Move to constants file
                throw new Exception("Failed to add new row to update cache.");
            }

            return newRowId;
        }

        /// <summary>
        /// Creates a delete row update and adds it to the update cache
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If row requested to delete already has a pending change in the cache
        /// </exception>
        /// <param name="rowId">The internal ID of the row to delete</param>
        public void DeleteRow(long rowId)
        {
            // Create a new row delete update and add to cache
            RowDelete deleteRow = new RowDelete();
            if (!updateCache.TryAdd(rowId, deleteRow))
            {
                // @TODO: Move to constants file
                throw new InvalidOperationException("An update is already pending for this row and must be reverted first.");
            }
        }

        /// <summary>
        /// Removes a pending row update from the update cache.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If a pending row update with the given row ID does not exist.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to reset</param>
        public void RevertRow(long rowId)
        {
            // Attempt to remove the row with the given ID
            RowUpdateBase removedUpdate;
            if (!updateCache.TryRemove(rowId, out removedUpdate))
            {
                // @TODO Move to constants file
                throw new ArgumentOutOfRangeException(nameof(rowId), "Given row ID does not have pending updated");
            }
        }

        /// <summary>
        /// Performs an update to a specific cell in a row. If the row has not already been
        /// initialized with a record in the update cache, one is created.
        /// </summary>
        /// <exception cref="Exception">If adding a new update row fails</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the row that is requested to be edited is beyond the rows in the results and the
        /// rows that are being added.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to edit</param>
        /// <param name="columnId">The ordinal of the column to edit in the row</param>
        /// <param name="newValue">The new string value of the cell to update</param>
        public void UpdateCell(long rowId, int columnId, string newValue)
        {
            // Sanity check to make sure that the row ID is in the range of possible values
            if (rowId >= nextRowId)
            {
                // @TODO: Move to constants file
                throw new ArgumentOutOfRangeException(nameof(rowId), "Give row ID is outside the range of rows in the cache");
            }

            // Attempt to get the row that is being edited, create a new update object if one
            // doesn't exist
            RowUpdateBase updateRow;
            if (!updateCache.TryGetValue(rowId, out updateRow))
            {
                updateRow = new RowUpdate();
                if (!updateCache.TryAdd(rowId, updateRow))
                {
                    // @TODO: Move to constants file
                    throw new Exception("Failed to add row update to cache");
                }
            }

            // Pass the call to the row update
            updateRow.UpdateCell(columnId, newValue);
        }

        #endregion

    }
}
