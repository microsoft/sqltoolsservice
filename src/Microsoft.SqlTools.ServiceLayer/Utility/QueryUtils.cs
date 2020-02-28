//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System.Collections.Generic;
using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public static class QueryUtils
    {
        public static void ExecuteNonQuery(IDbConnection connection, string script, Dictionary<string, object> parameters = null)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = script;
                if (parameters != null)
                {
                    foreach (var item in parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = item.Key;
                        parameter.Value = item.Value;
                        command.Parameters.Add(parameter);
                    }
                }

                command.ExecuteNonQuery();
            }
        }
    }
}
