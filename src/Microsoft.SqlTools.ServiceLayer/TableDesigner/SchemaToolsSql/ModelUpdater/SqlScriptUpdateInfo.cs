//------------------------------------------------------------------------------
// <copyright file="SqlScriptUpdateInfo.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed class SqlScriptUpdateInfo
    {
        private SortedList<SqlScriptUpdateItem, SqlScriptUpdateItem> _updates = new SortedList<SqlScriptUpdateItem, SqlScriptUpdateItem>();
        public String ScriptCacheIdentifier { get; private set; }
        
        public IEnumerable<SqlScriptUpdateItem> Updates
        {
            get { return _updates.Values; }
        }

        public SqlScriptUpdateInfo(String cacheId)
        {
            if (string.IsNullOrEmpty(cacheId))
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Null or empty argument: cacheId");
                throw new ArgumentNullException("cacheId");
            }

            this.ScriptCacheIdentifier = cacheId;
        }

        public void AddUpdate(Int32 startOffset, Int32 startLine, Int32 startColumn, Int32 length, String newText)
        {
            if (startOffset < 0)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: startOffset");
                throw new ArgumentOutOfRangeException("startOffset");
            }

            if (startLine < 1)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: startLine");
                throw new ArgumentOutOfRangeException("startLine");
            }

            if (startColumn < 1)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: startColumn");
                throw new ArgumentOutOfRangeException("startColumn");
            }

            if (length < 0)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Argument out of range: length");
                throw new ArgumentOutOfRangeException("length");
            }

            if (newText == null)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Null argument: newText");
                throw new ArgumentNullException("newText");
            }

            AddUpdate(new SqlScriptUpdateItem(startOffset, startLine, startColumn, length, newText));
        }

        public void AddUpdate(SqlScriptUpdateItem updateItem)
        {
            if (updateItem == null)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "Null argument: updateItem");
                throw new ArgumentNullException("updateItem");
            }

            updateItem.UpdateOrder = _updates.Count;
            _updates.Add(updateItem, updateItem);
        }

        public void AddUpdates(IEnumerable<SqlScriptUpdateItem> updateItems)
        {
            if (updateItems == null || !updateItems.Any())
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, SqlTraceId.CoreServices, "There should be at least one SqlScriptUpdateItem");
                throw new ArgumentException("There should be at least one SqlScriptUpdateItem", "updateItems");
            }

            foreach (SqlScriptUpdateItem updateItem in updateItems)
            {
                this.AddUpdate(updateItem);
            }
        }
    }
}
