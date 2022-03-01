//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    public abstract class EnumUtil<T> where T : struct, IConvertible
    {
        protected Dictionary<string, T> Mapping = new Dictionary<string, T>();

        public List<string> DisplayNames
        {
            get
            {
                return this.Mapping.Keys.ToList();
            }
        }

        public string GetName(T enumValue)
        {
            foreach (var key in this.Mapping.Keys)
            {
                if (this.Mapping[key].Equals(enumValue))
                {
                    return key;
                }
            }
            throw new KeyNotFoundException(SR.UnknownEnumString(enumValue.ToString()));
        }

        public T GetValue(string displayName)
        {
            if (this.Mapping.ContainsKey(displayName))
            {
                return this.Mapping[displayName];
            }
            else
            {
                throw new KeyNotFoundException(SR.UnknownEnumString(displayName));
            }
        }
    }

    public class SqlForeignKeyActionUtil : EnumUtil<SqlForeignKeyAction>
    {
        public static SqlForeignKeyActionUtil Instance { get; } = new SqlForeignKeyActionUtil();

        public SqlForeignKeyActionUtil()
        {
            this.Mapping.Add(SR.SqlForeignKeyAction_NoAction, SqlForeignKeyAction.NoAction);
            this.Mapping.Add(SR.SqlForeignKeyAction_Cascade, SqlForeignKeyAction.Cascade);
            this.Mapping.Add(SR.SqlForeignKeyAction_SetNull, SqlForeignKeyAction.SetNull);
            this.Mapping.Add(SR.SqlForeignKeyAction_SetDefault, SqlForeignKeyAction.SetDefault);
        }

        public List<string> EdgeConstraintOnDeleteActionNames
        {
            get
            {
                return new List<string> { SR.SqlForeignKeyAction_NoAction, SR.SqlForeignKeyAction_Cascade };
            }
        }
    }

    public class SqlTableDurabilityUtil : EnumUtil<TableDurability>
    {
        public static SqlTableDurabilityUtil Instance { get; } = new SqlTableDurabilityUtil();

        public SqlTableDurabilityUtil()
        {
            this.Mapping.Add(SR.SqlTableDurability_SchemaAndData, TableDurability.SchemaAndData);
            this.Mapping.Add(SR.SqlTableDurability_SchemaOnly, TableDurability.SchemaOnly);
        }
    }

    public class SqlGeneratedAlwaysColumnTypeUtil : EnumUtil<GeneratedAlwaysColumnType>
    {
        public static SqlGeneratedAlwaysColumnTypeUtil Instance { get; } = new SqlGeneratedAlwaysColumnTypeUtil();

        public SqlGeneratedAlwaysColumnTypeUtil()
        {
            this.Mapping.Add(SR.GeneratedAlwaysColumnType_None, GeneratedAlwaysColumnType.None);
            this.Mapping.Add(SR.GeneratedAlwaysColumnType_RowStart, GeneratedAlwaysColumnType.GeneratedAlwaysAsRowStart);
            this.Mapping.Add(SR.GeneratedAlwaysColumnType_RowEnd, GeneratedAlwaysColumnType.GeneratedAlwaysAsRowEnd);
        }
    }
}