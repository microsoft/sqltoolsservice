//------------------------------------------------------------------------------
// <copyright file="SqlScriptUpdateItem.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed class SqlScriptUpdateItem : IComparable<SqlScriptUpdateItem>
    {
        public int StartOffset { get; private set; }
        public int StartLine { get; private set; }
        public int StartColumn { get; private set; }
        public int Length { get; private set; }
        public string NewText { get; private set; }

        // order in which the update item was inserted in the list of updates;
        // used to distinguish updates with the same start offset
        internal int UpdateOrder { get; set; }

        public SqlScriptUpdateItem(int startOffset, int startLine, int startColumn, int length, string newText)
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

            StartOffset = startOffset;
            StartLine = startLine;
            StartColumn = startColumn;
            Length = length;
            NewText = newText;
        }

        public int CompareTo(SqlScriptUpdateItem other)
        {
            int comp = this.StartOffset - other.StartOffset;
            if (comp == 0)
            {
                comp = this.UpdateOrder - other.UpdateOrder;
            }
            return comp;
        }
    }
}
