//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession
    {
        private readonly string _connectionString;
        private DacServices _dacServices;
        private TSqlModel _originalModel;

        public SchemaDesignerSession(string connectionString, string? accessToken, string databaseName)
        {
            _connectionString = connectionString;
            if (accessToken != null)
            {
                _dacServices = new DacServices(connectionString, new AccessTokenProvider(accessToken));
                _originalModel = TSqlModel.LoadFromDatabaseWithAuthProvider(connectionString, new AccessTokenProvider(accessToken));
            }
            else
            {
                _dacServices = new DacServices(connectionString);
                _originalModel = TSqlModel.LoadFromDatabase(connectionString);
            }
            var tables = new List<object>();

            foreach (var table in _originalModel.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass))
            {
                tables.Add(new
                {
                    Name = table.Name.ToString(),
                    Columns = table.GetReferenced(Table.Columns)
                });
            }
        }
    }
}