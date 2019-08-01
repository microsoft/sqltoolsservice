//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{


    public class ColumnInfo
    {
        /// <summary>
        /// Name of this column
        /// </summary>
        public string Name { get; set; }

        public string DataTypeName { get; set; } 

        public ColumnInfo()
        {
        }

        public ColumnInfo(string name, string dataTypeName)
        {
            this.Name = name;
            this.DataTypeName = dataTypeName;
        }
    }

    /// <summary>
    /// Class used for storing results and how the results are to be serialized
    /// </summary>
    public class SerializeDataRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// String representation of the type that service is supposed to serialize to
        ///  E.g. "json" or "csv"
        /// </summary>
        public string SaveFormat { get; set; }

        /// <summary>
        /// Path to file that the serialized results will be stored in
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Results that are to be serialized into 'SaveFormat' format
        /// </summary>
        public DbCellValue[][] Rows { get; set; }

        public ColumnInfo[] Columns { get; set; }

        /// <summary>
        /// Whether the current set of Rows passed in is the last for this file
        // </summary>
        public bool IsComplete { get; set; }

        public SerializeDataRequestParams()
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public SerializeDataRequestParams(string saveFormat, 
            string savePath, 
            DbCellValue[][] rows, 
            bool isLast)
        {
            this.SaveFormat = saveFormat;
            this.FilePath = savePath;
            this.Rows = Rows;
            this.IsComplete = isLast;
        }

        internal bool IncludeHeaders
        {
            get { return this.GetOptionValue<bool>(SerializationOptionsHelper.IncludeHeaders); }
            set { this.SetOptionValue<bool>(SerializationOptionsHelper.IncludeHeaders, value); }
        }

        internal string Delimiter
        {
            get { return this.GetOptionValue<string>(SerializationOptionsHelper.Delimiter); }
            set { this.SetOptionValue<string>(SerializationOptionsHelper.Delimiter, value); }
        }

        internal string LineSeparator
        {
            get { return this.GetOptionValue<string>(SerializationOptionsHelper.LineSeparator); }
            set { this.SetOptionValue<string>(SerializationOptionsHelper.LineSeparator, value); }
        }

        internal string TextIdentifier
        {
            get { return this.GetOptionValue<string>(SerializationOptionsHelper.TextIdentifier); }
            set { this.SetOptionValue<string>(SerializationOptionsHelper.TextIdentifier, value); }
        }

        internal string Encoding
        {
            get { return this.GetOptionValue<string>(SerializationOptionsHelper.Encoding); }
            set { this.SetOptionValue<string>(SerializationOptionsHelper.Encoding, value); }
        }

        internal bool Formatted
        {
            get { return this.GetOptionValue<bool>(SerializationOptionsHelper.Formatted); }
            set { this.SetOptionValue<bool>(SerializationOptionsHelper.Formatted, value); }
        }
    }

    public class SerializeDataResult
    {
        public string Messages { get; set; }

        public bool Succeeded { get; set; }
    }

    public class SerializeDataRequest
    {
        public static readonly
            RequestType<SerializeDataRequestParams, SerializeDataResult> Type =
            RequestType<SerializeDataRequestParams, SerializeDataResult>.Create("query/saveAs");
    }

    class SerializationOptionsHelper
    {
        internal const string IncludeHeaders = "includeHeaders";
        internal const string Delimiter = "delimiter";
        internal const string LineSeparator = "lineSeparator";
        internal const string TextIdentifier = "textIdentifier";
        internal const string Encoding = "encoding";
        internal const string Formatted = "formatted";
    }
}
