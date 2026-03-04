//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Range = Microsoft.SqlTools.ServiceLayer.QueryExecution.QueryExecutionService.Range;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public static class CopyTextBuilder
    {
        public static SqlToolsSettings Settings
        {
            get
            {
                return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings;
            }
        }
        public static async Task<string> BuildCopyContentAsync(
            CopyResults2RequestParams requestParams,
            Query query,
            IReadOnlyList<DbColumnWrapper> selectedColumns,
            IReadOnlyList<int> columnIndexes,
            List<Range> rowRanges,
            long lastRowIndex,
            CancellationToken cancellationToken)
        {
            switch (requestParams.CopyType)
            {
                case CopyType.Text:
                    return await BuildTextCopyAsync(requestParams, selectedColumns, columnIndexes, rowRanges, lastRowIndex, cancellationToken);
                case CopyType.CSV:
                    return await BuildCsvCopyAsync(requestParams, selectedColumns, columnIndexes, rowRanges, cancellationToken);
                case CopyType.JSON:
                    return await BuildJsonCopyAsync(requestParams, selectedColumns, columnIndexes, rowRanges, cancellationToken);
                case CopyType.INSERT:
                    return await BuildInsertCopyAsync(requestParams, selectedColumns, columnIndexes, rowRanges, cancellationToken);
                case CopyType.IN:
                    return await BuildInCopyAsync(requestParams, columnIndexes, rowRanges, cancellationToken);
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestParams.CopyType), requestParams.CopyType, "Unsupported copy type.");
            }
        }

        public static async Task<string> BuildTextCopyAsync(
            CopyResults2RequestParams requestParams,
            IReadOnlyList<DbColumnWrapper> selectedColumns,
            IReadOnlyList<int> columnIndexes,
            List<Range> rowRanges,
            long lastRowIndex,
            CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            if (requestParams.IncludeHeaders)
            {
                builder.Append(string.Join('\t', selectedColumns.Select(c => c.ColumnName)));
                builder.Append(requestParams.LineSeparator);
            }

            await ProcessSelectedRowsAsync(
                requestParams,
                rowRanges,
                async (absoluteRowIndex, row) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var projectedRow = BuildRowProjection(row, columnIndexes, requestParams.Selections, absoluteRowIndex, treatUnselectedAsNull: false);
                    AppendTextRow(builder, projectedRow);

                    if (absoluteRowIndex != lastRowIndex && (!StringBuilderEndsWith(builder, Environment.NewLine) || (!Settings?.QueryEditorSettings?.Results?.SkipNewLineAfterTrailingLineBreak ?? true)))
                    {
                        builder.Append(requestParams.LineSeparator);
                    }

                    await Task.CompletedTask;
                },
                cancellationToken);

            return builder.ToString();
        }

        private static async Task<string> BuildCsvCopyAsync(
            CopyResults2RequestParams requestParams,
            IReadOnlyList<DbColumnWrapper> selectedColumns,
            IReadOnlyList<int> columnIndexes,
            List<Range> rowRanges,
            CancellationToken cancellationToken)
        {
            if (selectedColumns.Count == 0)
            {
                return string.Empty;
            }

            var csvParams = new SaveResultsAsCsvRequestParams
            {
                IncludeHeaders = requestParams.IncludeHeaders,
                Delimiter = requestParams.Delimiter,
                LineSeperator = requestParams.LineSeparator,
                TextIdentifier = requestParams.TextIdentifier,
                Encoding = requestParams.Encoding
            };

            var encoding = ResolveEncoding(csvParams.Encoding, Encoding.UTF8);

            using var memoryStream = new MemoryStream();
            using (var streamWrapper = new NonDisposingStream(memoryStream))
            using (var writer = new SaveAsCsvFileStreamWriter(streamWrapper, csvParams, selectedColumns))
            {
                await ProcessSelectedRowsAsync(
                    requestParams,
                    rowRanges,
                    async (absoluteRowIndex, row) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var projectedRow = BuildRowProjection(row, columnIndexes, requestParams.Selections, absoluteRowIndex, treatUnselectedAsNull: false);
                        writer.WriteRow(projectedRow, selectedColumns);
                        await Task.CompletedTask;
                    },
                    cancellationToken);

                writer.FlushBuffer();
            }

            return encoding.GetString(memoryStream.ToArray());
        }

        private static async Task<string> BuildJsonCopyAsync(
            CopyResults2RequestParams requestParams,
            IReadOnlyList<DbColumnWrapper> selectedColumns,
            IReadOnlyList<int> columnIndexes,
            List<Range> rowRanges,
            CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            using (var streamWrapper = new NonDisposingStream(memoryStream))
            using (var writer = new SaveAsJsonFileStreamWriter(streamWrapper, new SaveResultsAsJsonRequestParams(), selectedColumns, requestParams.LineSeparator))
            {
                await ProcessSelectedRowsAsync(
                    requestParams,
                    rowRanges,
                    async (absoluteRowIndex, row) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var projectedRow = BuildRowProjection(row, columnIndexes, requestParams.Selections, absoluteRowIndex, treatUnselectedAsNull: true);
                        writer.WriteRow(projectedRow, selectedColumns);
                        await Task.CompletedTask;
                    },
                    cancellationToken);
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        private static async Task<string> BuildInsertCopyAsync(
            CopyResults2RequestParams requestParams,
            IReadOnlyList<DbColumnWrapper> selectedColumns,
            IReadOnlyList<int> columnIndexes,
            List<Range> rowRanges,
            CancellationToken cancellationToken)
        {
            if (selectedColumns.Count == 0)
            {
                return string.Empty;
            }

            var insertParams = new SaveResultsAsInsertRequestParams
            {
                IncludeHeaders = requestParams.IncludeHeaders,
                TableName = null,
                Encoding = requestParams.Encoding
            };

            var encoding = ResolveEncoding(insertParams.Encoding, Encoding.UTF8);

            using var memoryStream = new MemoryStream();
            using (var streamWrapper = new NonDisposingStream(memoryStream))
            using (var writer = new SaveAsInsertFileStreamWriter(streamWrapper, insertParams, selectedColumns, requestParams.LineSeparator))
            {
                await ProcessSelectedRowsAsync(
                    requestParams,
                    rowRanges,
                    async (absoluteRowIndex, row) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var projectedRow = BuildRowProjection(row, columnIndexes, requestParams.Selections, absoluteRowIndex, treatUnselectedAsNull: true);
                        writer.WriteRow(projectedRow, selectedColumns);
                        await Task.CompletedTask;
                    },
                    cancellationToken);

                writer.FlushBuffer();
            }

            return encoding.GetString(memoryStream.ToArray());
        }

        private static async Task<string> BuildInCopyAsync(
            CopyResults2RequestParams requestParams,
            IReadOnlyList<int> columnIndexes,
            List<Range> rowRanges,
            CancellationToken cancellationToken)
        {
            if (columnIndexes.Count != 1)
            {
                throw new InvalidOperationException("Copying as an IN clause requires selecting exactly one column.");
            }

            var values = new List<string>();
            int columnIndex = columnIndexes[0];

            await ProcessSelectedRowsAsync(
                requestParams,
                rowRanges,
                (absoluteRowIndex, row) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!IsCellSelected(requestParams.Selections, absoluteRowIndex, columnIndex))
                    {
                        return Task.CompletedTask;
                    }

                    DbCellValue? cell = row != null && columnIndex < row.Length ? row[columnIndex] : null;
                    values.Add(SqlValueFormatter.FormatValue(cell));
                    return Task.CompletedTask;
                },
                cancellationToken);

            var builder = new StringBuilder();
            builder.Append("IN");
            builder.Append(requestParams.LineSeparator);
            builder.Append("(");
            builder.Append(requestParams.LineSeparator);
            for (int i = 0; i < values.Count; i++)
            {
                builder.Append("    ");
                builder.Append(values[i]);
                if (i < values.Count - 1)
                {
                    builder.Append(",");
                    builder.Append(requestParams.LineSeparator);
                }
                else
                {
                    builder.Append(requestParams.LineSeparator);
                }
            }
            builder.Append(")");
            builder.Append(requestParams.LineSeparator);

            return builder.ToString();
        }

        private static async Task ProcessSelectedRowsAsync(
            CopyResults2RequestParams requestParams,
            List<Range> rowRanges,
            Func<long, DbCellValue[], Task> onRow,
            CancellationToken cancellationToken)
        {
            QueryExecutionService queryExecutionService = QueryExecutionService.Instance;
            const int pageSize = 200;

            foreach (var rowRange in rowRanges)
            {
                var pageStartRowIndex = rowRange.Start;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowsToFetch = Math.Min(pageSize, rowRange.End - pageStartRowIndex + 1);
                    ResultSetSubset subset = await queryExecutionService.InterServiceResultSubset(new SubsetParams
                    {
                        OwnerUri = requestParams.OwnerUri,
                        ResultSetIndex = requestParams.ResultSetIndex,
                        BatchIndex = requestParams.BatchIndex,
                        RowsStartIndex = pageStartRowIndex,
                        RowsCount = rowsToFetch
                    });

                    for (int rowIndex = 0; rowIndex < subset.Rows.Length; rowIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        long absoluteRowIndex = pageStartRowIndex + rowIndex;
                        await onRow(absoluteRowIndex, subset.Rows[rowIndex]);
                    }

                    pageStartRowIndex += rowsToFetch;
                } while (pageStartRowIndex <= rowRange.End);
            }
        }

        private static List<DbCellValue> BuildRowProjection(
            DbCellValue[] sourceRow,
            IReadOnlyList<int> columnIndexes,
            TableSelectionRange[] selections,
            long absoluteRowIndex,
            bool treatUnselectedAsNull)
        {
            var result = new List<DbCellValue>(columnIndexes.Count);

            foreach (var columnIndex in columnIndexes)
            {
                if (IsCellSelected(selections, absoluteRowIndex, columnIndex))
                {
                    if (sourceRow != null && columnIndex < sourceRow.Length && sourceRow[columnIndex] != null)
                    {
                        result.Add(sourceRow[columnIndex]);
                    }
                    else
                    {
                        result.Add(CreateNullCell(absoluteRowIndex));
                    }
                }
                else
                {
                    result.Add(treatUnselectedAsNull ? CreateNullCell(absoluteRowIndex) : CreateEmptyCell(absoluteRowIndex));
                }
            }

            return result;
        }

        private static bool IsCellSelected(TableSelectionRange[] selections, long rowIndex, int columnIndex)
        {
            foreach (var selection in selections)
            {
                if (selection.FromRow <= rowIndex && selection.ToRow >= rowIndex &&
                    selection.FromColumn <= columnIndex && selection.ToColumn >= columnIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static DbCellValue CreateNullCell(long rowId)
        {
            return new DbCellValue
            {
                DisplayValue = null,
                InvariantCultureDisplayValue = null,
                IsNull = true,
                RawObject = null,
                RowId = rowId
            };
        }

        private static DbCellValue CreateEmptyCell(long rowId)
        {
            return new DbCellValue
            {
                DisplayValue = string.Empty,
                InvariantCultureDisplayValue = string.Empty,
                IsNull = true,
                RawObject = null,
                RowId = rowId
            };
        }

        private static void AppendTextRow(StringBuilder builder, IList<DbCellValue> rowValues)
        {
            bool removeNewLines = Settings?.GetCopyRemoveNewLineSetting() ?? true;

            for (int i = 0; i < rowValues.Count; i++)
            {
                var cell = rowValues[i];
                if (cell != null && cell.DisplayValue != null)
                {
                    var value = removeNewLines ? cell.DisplayValue.ReplaceLineEndings(" ") : cell.DisplayValue;
                    builder.Append(value);
                }

                if (i < rowValues.Count - 1)
                {
                    builder.Append('\t');
                }
            }
        }

        private sealed class NonDisposingStream : Stream
        {
            private readonly Stream innerStream;

            public NonDisposingStream(Stream innerStream)
            {
                this.innerStream = innerStream;
            }

            public override bool CanRead => innerStream.CanRead;

            public override bool CanSeek => innerStream.CanSeek;

            public override bool CanWrite => innerStream.CanWrite;

            public override long Length => innerStream.Length;

            public override long Position
            {
                get => innerStream.Position;
                set => innerStream.Position = value;
            }

            public override void Flush()
            {
                innerStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return innerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                innerStream.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                // Intentionally do not dispose the underlying stream so it can be read after the writer is disposed.
            }
        }

        private static Encoding ResolveEncoding(string encodingName, Encoding fallbackEncoding)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
            {
                return fallbackEncoding;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                return int.TryParse(encodingName, out int codePage)
                    ? Encoding.GetEncoding(codePage)
                    : Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return fallbackEncoding;
            }
        }

        public static bool StringBuilderEndsWith(StringBuilder sb, string target)
        {
            if (sb.Length < target.Length)
            {
                return false;
            }

            // Calling ToString like this only converts the last few characters of the StringBuilder to a string
            return sb.ToString(sb.Length - target.Length, target.Length).EndsWith(target);
        }


    }

}