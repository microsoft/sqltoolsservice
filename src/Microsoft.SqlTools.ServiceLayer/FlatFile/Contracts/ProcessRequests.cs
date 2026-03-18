//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FlatFile.Contracts
{
    public class GetColumnInfoParams
    {
        public string OperationId { get; set; }
    }

    public class GetColumnInfoResponse
    {
        public ColumnInfo[] ColumnInfo { get; set; }
    }

    public class GetColumnInfoRequest
    {
        public static readonly RequestType<GetColumnInfoParams, GetColumnInfoResponse> Type =
            RequestType<GetColumnInfoParams, GetColumnInfoResponse>.Create("flatfile/getColumnInfo");
    }

    public class ChangeColumnSettingsParams
    {
        public string OperationId { get; set; }

        public int Index { get; set; }

        public string NewName { get; set; }

        public string NewDataType { get; set; }

        public bool? NewNullable { get; set; }

        public bool? NewInPrimaryKey { get; set; }
    }

    public class ChangeColumnSettingsResponse
    {
        public Result Result { get; set; }
    }

    public class ChangeColumnSettingsRequest
    {
        public static readonly RequestType<ChangeColumnSettingsParams, ChangeColumnSettingsResponse> Type =
            RequestType<ChangeColumnSettingsParams, ChangeColumnSettingsResponse>.Create("flatfile/changeColumnSettings");
    }

    public class DisposeSessionParams
    {
        public string OperationId { get; set; }
    }

    public class DisposeSessionResponse
    {
        public Result Result { get; set; }
    }

    public class DisposeSessionRequest
    {
        public static readonly RequestType<DisposeSessionParams, DisposeSessionResponse> Type =
            RequestType<DisposeSessionParams, DisposeSessionResponse>.Create("flatfile/disposeSession");
    }
}
