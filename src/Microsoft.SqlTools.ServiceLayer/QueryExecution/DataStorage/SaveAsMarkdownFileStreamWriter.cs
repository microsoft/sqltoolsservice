//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class SaveAsMarkdownFileStreamWriter : SaveAsStreamWriter
    {
        private const string Delimiter = "|";
        private static readonly Regex NewlineRegex = new Regex("(\r\n|\n|\r)", RegexOptions.Compiled);

        private readonly Encoding encoding;
        private readonly string lineSeparator;

        public SaveAsMarkdownFileStreamWriter(
            Stream stream,
            SaveResultsAsMarkdownRequestParams requestParams,
            IReadOnlyList<DbColumnWrapper> columns)
            : base(stream, requestParams, columns)
        {
            // Parse the request params
            this.lineSeparator = string.IsNullOrEmpty(requestParams.LineSeperator)
                ? Environment.NewLine
                : requestParams.LineSeperator;
            this.encoding = ParseEncoding(requestParams.Encoding, Encoding.UTF8);

            // Output the header if requested
            if (requestParams.IncludeHeaders)
            {
                // Write the column header
                IEnumerable<string> selectedColumnNames = columns.Skip(this.ColumnStartIndex)
                    .Take(this.ColumnCount)
                    .Select(c => EncodeMarkdownField(c.ColumnName));
                string headerLine = string.Join(Delimiter, selectedColumnNames);

                this.WriteLine($"{Delimiter}{headerLine}{Delimiter}");

                // Write the separator row
                var separatorBuilder = new StringBuilder(Delimiter);
                for (int i = 0; i < this.ColumnCount; i++)
                {
                    separatorBuilder.Append($"---{Delimiter}");
                }

                this.WriteLine(separatorBuilder.ToString());
            }
        }

        /// <inheritdoc />
        public override void WriteRow(IList<DbCellValue> row, IReadOnlyList<DbColumnWrapper> columns)
        {
            IEnumerable<string> selectedCells = row.Skip(this.ColumnStartIndex)
                .Take(this.ColumnCount)
                .Select(c => EncodeMarkdownField(c.DisplayValue));
            string rowLine = string.Join(Delimiter, selectedCells);

            this.WriteLine($"{Delimiter}{rowLine}{Delimiter}");
        }

        internal static string EncodeMarkdownField(string? field)
        {
            // Special case for nulls
            if (field == null)
            {
                return "NULL";
            }

            // Escape HTML entities, since Markdown supports inline HTML
            field = SecurityElement.Escape(field);

            // Escape pipe delimiters
            field = field.Replace(@"|", @"\|");

            // Replace newlines with br tags, since cell values must be single line
            field = NewlineRegex.Replace(field, @"<br />");

            return field;
        }

        internal void WriteLine(string line)
        {
            byte[] bytes = this.encoding.GetBytes(line + this.lineSeparator);
            this.FileStream.Write(bytes);
        }
    }
}