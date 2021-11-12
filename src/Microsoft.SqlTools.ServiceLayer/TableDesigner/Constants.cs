//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        public const string Precision = "precision";
        public const string Scale = "scale";
        public const string IsIdentity = "isIdentity";
        public const string IdentityIncrement = "identityIncrement";
        public const string IdentitySeed = "identitySeed";
    }
}