//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FlatFile.Contracts
{
    public class Result
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class ProseDiscoveryParams
    {
        public string OperationId { get; set; }

        public string FilePath { get; set; }

        public string TableName { get; set; }

        public string SchemaName { get; set; }

        public string FileType { get; set; }

        public string FileContents { get; set; }
    }

    public class ColumnInfo
    {
        public string Name { get; set; }

        public string SqlType { get; set; }

        public bool IsNullable { get; set; }
    }

    public class ProseDiscoveryResponse
    {
        public List<string[]> DataPreview { get; set; }

        public ColumnInfo[] ColumnInfo { get; set; }

        public string ColumnDelimiter { get; set; }

        public int FirstRow { get; set; }

        public string QuoteCharacter { get; set; }
    }

    public class ProseDiscoveryRequest
    {
        public static readonly RequestType<ProseDiscoveryParams, ProseDiscoveryResponse> Type =
            RequestType<ProseDiscoveryParams, ProseDiscoveryResponse>.Create("flatfile/proseDiscovery");
    }

    public class InsertDataParams
    {
        public string OperationId { get; set; }

        public string OwnerUri { get; set; }

        public string DatabaseName { get; set; }

        public int BatchSize { get; set; }
    }

    public class InsertDataResponse
    {
        public Result Result { get; set; }
    }

    public class InsertDataRequest
    {
        public static readonly RequestType<InsertDataParams, InsertDataResponse> Type =
            RequestType<InsertDataParams, InsertDataResponse>.Create("flatfile/insertData");
    }
}
