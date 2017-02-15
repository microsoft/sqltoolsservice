//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class Session
    {

        #region Member Variables

        internal long NextRowId;
        internal readonly ConcurrentDictionary<long, RowEditBase> EditCache;
        private readonly ResultSet associatedResultSet;
        private readonly IEditTableMetadata objectMetadata;

        #endregion

        public Session(Query query, IEditTableMetadata objMetadata)
        {
            Validate.IsNotNull(nameof(query), query);
            Validate.IsNotNull(nameof(objMetadata), objMetadata);

            // Determine if the query is valid for editing
            // Criterion 1) Query has finished executing
            if (!query.HasExecuted)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Query has not completed execution");
            }

            // Criterion 2) Query only has a single result set
            ResultSet[] queryResultSets = query.Batches.SelectMany(b => b.ResultSets).ToArray();
            if (queryResultSets.Length != 1)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Query did not generate exactly one result set");
            }

            // Setup the internal state
            associatedResultSet = queryResultSets[0];
            objectMetadata = objMetadata;
            NextRowId = associatedResultSet.RowCount;
            EditCache = new ConcurrentDictionary<long, RowEditBase>();
        }

        #region Public Methods

        /// <summary>
        /// Creates a new row update and adds it to the update cache
        /// </summary>
        /// <exception cref="InvalidOperationException">If inserting into cache fails</exception>
        /// <returns>The internal ID of the newly created row</returns>
        public long CreateRow()
        {
            // Create a new row ID (atomically, since this could be accesses concurrently)
            long newRowId = NextRowId++;

            // Create a new row create update and add to the update cache
            RowCreate newRow = new RowCreate(newRowId, associatedResultSet, objectMetadata);
            if (!EditCache.TryAdd(newRowId, newRow))
            {
                // Revert the next row ID
                NextRowId--;

                // @TODO: Move to constants file
                throw new InvalidOperationException("Failed to add new row to update cache.");
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
            // Sanity check the row ID
            if (rowId >= NextRowId || rowId < 0)
            {
                // @TODO: Move to constants file
                throw new ArgumentOutOfRangeException(nameof(rowId), "Give row ID is outside the range of rows in the cache");
            }

            // Create a new row delete update and add to cache
            RowDelete deleteRow = new RowDelete(rowId, associatedResultSet, objectMetadata);
            if (!EditCache.TryAdd(rowId, deleteRow))
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
            RowEditBase removedEdit;
            if (!EditCache.TryRemove(rowId, out removedEdit))
            {
                // @TODO Move to constants file
                throw new ArgumentOutOfRangeException(nameof(rowId), "Given row ID does not have pending updated");
            }
        }

        public string ScriptEdits(string outputPath)
        {
            // Validate the output path
            // @TODO: Reinstate this code once we have an interface around file generation
            //if (outputPath == null)
            //{
            //    // If output path isn't provided, we'll use a temporary location
            //    outputPath = Path.GetTempFileName();
            //}
            //else 
            if (outputPath == null || outputPath.Trim() == string.Empty)
            {
                // If output path is empty, that's an error
                // @TODO: Move to constants file
                throw new ArgumentNullException(nameof(outputPath), "An output filename must be provided");
            }

            // Open a handle to the output file
            using (FileStream outputStream = File.OpenWrite(outputPath))
            using (TextWriter outputWriter = new StreamWriter(outputStream))
            {

                // Convert each update in the cache into an insert/update/delete statement
                foreach (RowEditBase rowEdit in EditCache.Values)
                {
                    outputWriter.WriteLine(rowEdit.GetScript());
                }
            }

            // Return the location of the generated script
            return outputPath;
        }

        /// <summary>
        /// Performs an update to a specific cell in a row. If the row has not already been
        /// initialized with a record in the update cache, one is created.
        /// </summary>
        /// <exception cref="InvalidOperationException">If adding a new update row fails</exception>
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
            if (rowId >= NextRowId || rowId < 0)
            {
                // @TODO: Move to constants file
                throw new ArgumentOutOfRangeException(nameof(rowId), "Give row ID is outside the range of rows in the cache");
            }

            // Attempt to get the row that is being edited, create a new update object if one
            // doesn't exist
            RowEditBase editRow = EditCache.GetOrAdd(rowId, new RowUpdate(rowId, associatedResultSet, objectMetadata));

            // Pass the call to the row update
            editRow.SetCell(columnId, newValue);
        }

        #endregion

    }
}
