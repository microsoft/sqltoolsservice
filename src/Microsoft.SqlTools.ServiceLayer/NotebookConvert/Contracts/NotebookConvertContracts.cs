//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.NotebookConvert.Contracts
{
    public class ConvertNotebookToSqlParams : GeneralRequestDetails
    {
        /// <summary>
        /// The raw Notebook JSON content to convert
        /// </summary>
        public string NotebookJson { get; set; }

    }

    public class ConvertNotebookToSqlResult : ResultStatus
    {
        /// <summary>
        /// The raw SQL query content to display
        /// </summary>
        public string content;
    }

    public class ConvertNotebookToSqlRequest
    {
        public static readonly
            RequestType<ConvertNotebookToSqlParams, ConvertNotebookToSqlResult> Type =
            RequestType<ConvertNotebookToSqlParams, ConvertNotebookToSqlResult>.Create("notebookconvert/convertnotebooktosql");
    }

    public class ConvertSqlToNotebookParams : GeneralRequestDetails
    {
        /// <summary>
        /// The ClientUri of the SQL Query file we're converting
        /// </summary>
        public string ClientUri { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class ConvertSqlToNotebookResult : ResultStatus
    {
        /// <summary>
        /// The raw Notebook JSON content to display
        /// </summary>
        public string content;
    }

    public class ConvertSqlToNotebookRequest
    {
        public static readonly
            RequestType<ConvertSqlToNotebookParams, ConvertSqlToNotebookResult> Type =
            RequestType<ConvertSqlToNotebookParams, ConvertSqlToNotebookResult>.Create("notebookconvert/convertsqltonotebook");
    }
}
