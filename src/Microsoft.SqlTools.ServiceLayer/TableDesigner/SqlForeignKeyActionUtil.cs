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
    public static class SqlForeignKeyActionUtil
    {
        private static Dictionary<string, SqlForeignKeyAction> mapping = new Dictionary<string, SqlForeignKeyAction>();

        static SqlForeignKeyActionUtil()
        {
            mapping.Add(SR.SqlForeignKeyAction_NoAction, SqlForeignKeyAction.NoAction);
            mapping.Add(SR.SqlForeignKeyAction_Cascade, SqlForeignKeyAction.Cascade);
            mapping.Add(SR.SqlForeignKeyAction_SetNull, SqlForeignKeyAction.SetNull);
            mapping.Add(SR.SqlForeignKeyAction_SetDefault, SqlForeignKeyAction.SetDefault);
        }
        public static List<string> ActionNames
        {
            get
            {
                return mapping.Keys.ToList();
            }
        }

        public static string GetName(SqlForeignKeyAction action)
        {
            foreach (var key in mapping.Keys)
            {
                if (mapping[key] == action)
                {
                    return key;
                }
            }
            throw new NotSupportedException(SR.UnKnownSqlForeignKeyAction(action.ToString()));
        }

        public static SqlForeignKeyAction GetValue(string displayName)
        {
            if (mapping.ContainsKey(displayName))
            {
                return mapping[displayName];
            }
            else
            {
                throw new KeyNotFoundException(SR.UnKnownSqlForeignKeyAction(displayName));
            }
        }
    }
}