// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Writer for writing rows of results to a XML file.
    /// </summary>
    /// <remarks>
    /// This implements its own IDisposable because the cleanup logic closes the element that was
    /// created when the writer was created. Since this behavior is different than the standard
    /// file stream cleanup, the extra Dispose method was added.
    /// </remarks>
    public class SaveAsXmlFileStreamWriter : SaveAsStreamWriter, IDisposable
    {
        // Root element name for the output XML
        private const string RootElementTag = "data";
        
        // Item element name which will be used for every row
        private const string ItemElementTag = "row";
        
        #region Member Variables

        private readonly XmlTextWriter xmlTextWriter;

        #endregion

        /// <summary>
        /// Constructor, writes the header to the file, chains into the base constructor
        /// </summary>
        /// <param name="stream">FileStream to access the JSON file output</param>
        /// <param name="requestParams">XML save as request parameters</param>
        public SaveAsXmlFileStreamWriter(Stream stream, SaveResultsAsXmlRequestParams requestParams)
            : base(stream, requestParams)
        {
            // Setup the internal state
            var encoding = GetEncoding(requestParams);
            xmlTextWriter = new XmlTextWriter(stream, encoding);
            xmlTextWriter.Formatting = requestParams.Formatted ? Formatting.Indented : Formatting.None;

            //Start the document and the root element
            xmlTextWriter.WriteStartDocument();
            xmlTextWriter.WriteStartElement(RootElementTag);
        }

        /// <summary>
        /// Writes a row of data as a XML object
        /// </summary>
        /// <param name="row">The data of the row to output to the file</param>
        /// <param name="columns">
        /// The entire list of columns for the result set. They will be filtered down as per the
        /// request params.
        /// </param>
        public override void WriteRow(IList<DbCellValue> row, IList<DbColumnWrapper> columns)
        {
            // Write the header for the object
            xmlTextWriter.WriteStartElement(ItemElementTag);

            // Write the items out as properties
            int columnStart = ColumnStartIndex ?? 0;
            int columnEnd = ColumnEndIndex + 1 ?? columns.Count;
            for (int i = columnStart; i < columnEnd; i++)
            {
                // Write the column name as item tag
                xmlTextWriter.WriteStartElement(columns[i].ColumnName);
                
                if (row[i].RawObject != null)
                {
                    xmlTextWriter.WriteString(row[i].DisplayValue);
                }

                // End the item tag
                xmlTextWriter.WriteEndElement();
            }

            // Write the footer for the object
            xmlTextWriter.WriteEndElement();
        }

        /// <summary>
        /// Get the encoding for the XML file according to <param name="requestParams"></param>
        /// </summary>
        /// <param name="requestParams">XML save as request parameters</param>
        /// <returns></returns>
        private Encoding GetEncoding(SaveResultsAsXmlRequestParams requestParams)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding;
            try
            {
                if (int.TryParse(requestParams.Encoding, out var codepage))
                {
                    encoding = Encoding.GetEncoding(codepage);
                }
                else
                {
                    encoding = Encoding.GetEncoding(requestParams.Encoding);
                }
            }
            catch
            {
                // Fallback encoding when specified codepage is invalid
                encoding = Encoding.GetEncoding("utf-8");
            }

            return encoding;
        }

        private bool disposed = false;

        /// <summary>
        /// Disposes the writer by closing up the element that contains the row objects
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Write the footer of the file
                xmlTextWriter.WriteEndElement();
                xmlTextWriter.WriteEndDocument();

                xmlTextWriter.Close();
                xmlTextWriter.Dispose();
            }

            disposed = true;
            base.Dispose(disposing);
        }
    }
}