//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    public class TableDesignerMessageEventArgs
    {
        public string SessionId { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public string MessageType { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int Number { get; set; }
        public string Prefix { get; set; } = null!;
        public double? Progress { get; set; }
        public string SchemaName { get; set; } = null!;
        public string TableName { get; set; } = null!;
    }
}
