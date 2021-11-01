//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    public static class TablePropertyNames
    {
        public const string Name = "name";
        public const string Schema = "schema";
        public const string Description = "description";
        public const string Columns = "columns";
    }

    public static class TableColumnPropertyNames
    {
        public const string Name = "name";
        public const string Type = "type";
        public const string DefaultValue = "defaultValue";
        public const string Length = "length";
        public const string AllowNulls = "allowNulls";
        public const string IsPrimaryKey = "isPrimaryKey";
    }
}