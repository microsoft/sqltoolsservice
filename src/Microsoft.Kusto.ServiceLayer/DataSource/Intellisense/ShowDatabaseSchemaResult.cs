//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Intellisense
{
    public class ShowDatabaseSchemaResult
    {
        public string DatabaseName;
        public string TableName;
        public string ColumnName;
        public string ColumnType;
        public bool IsDefaultTable;
        public bool IsDefaultColumn;
        public string PrettyName;
        public string Version;
        public string Folder;
        public string DocName;
    }
}