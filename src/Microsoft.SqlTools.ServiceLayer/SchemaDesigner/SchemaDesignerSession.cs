//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession
    {
        private readonly string connectionString;
        private DacServices dacServices;
        private TSqlModel originalModel;
        public SchemaModel schema;

        public SchemaDesignerSession(string connectionString, string? accessToken, string databaseName)
        {
            this.connectionString = connectionString;
            if (accessToken != null)
            {
                dacServices = new DacServices(connectionString, new AccessTokenProvider(accessToken));
                originalModel = TSqlModel.LoadFromDatabaseWithAuthProvider(connectionString, new AccessTokenProvider(accessToken));
            }
            else
            {
                dacServices = new DacServices(connectionString);
                originalModel = TSqlModel.LoadFromDatabase(connectionString);
            }
            var tables = new List<ITable>();

            foreach (TSqlObject table in originalModel.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass))
            {
                TSqlObject schema = table.GetReferenced(Table.Schema).ToList()[0];
                List<TSqlObject> columns = table.GetReferenced(Table.Columns).ToList();
                IEnumerable<TSqlObject> foreignKeys = table.GetReferencing(ForeignKeyConstraint.Host, DacQueryScopes.UserDefined);
                tables.Add(new ITable()
                {
                    Name = table.Name.Parts[1],
                    Schema = schema.Name.Parts[0],
                    Columns = columns.Select(c =>
                    {
                        string dataType = "";
                        if (c.GetReferenced(SqlServer.Dac.Model.Column.DataType).ToList().Count != 0)
                        {
                            dataType = c.GetReferenced(SqlServer.Dac.Model.Column.DataType).ToList()[0].Name.Parts[0];
                        }
                        return new IColumn()
                        {
                            Name = c.Name.Parts[2],
                            DataType = dataType,
                            IsIdentity = c.GetProperty<bool>(SqlServer.Dac.Model.Column.IsIdentity),
                            IsPrimaryKey = c.GetReferencing(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined).ToList().Count != 0
                        };
                    }).ToList(),
                    ForeignKeys = foreignKeys.Select(fk =>
                    {
                        var foreignKey = new IForeignKey()
                        {
                            Name = fk.Name.Parts.Count != 0 ? fk.Name.Parts[1] : "",
                            Columns = fk.GetReferenced(ForeignKeyConstraint.Columns).ToList().Select(f => f.Name.Parts[2]).ToList(),
                            ReferencedColumns = fk.GetReferenced(ForeignKeyConstraint.ForeignColumns).ToList().Select(f => f.Name.Parts[2]).ToList(),
                            ReferencedTableName = fk.GetReferenced(ForeignKeyConstraint.ForeignTable).ToList()[0].Name.Parts[1],
                            ReferencedSchemaName = fk.GetReferenced(ForeignKeyConstraint.ForeignTable).ToList()[0].GetReferenced(Table.Schema).ToList()[0].Name.Parts[0],
                            OnDeleteAction = ConvertForeingKeyActionToOnAction(fk.GetProperty<ForeignKeyAction>(ForeignKeyConstraint.DeleteAction)),
                            OnUpdateAction = ConvertForeingKeyActionToOnAction(fk.GetProperty<ForeignKeyAction>(ForeignKeyConstraint.UpdateAction))
                        };
                        return foreignKey;
                    }).ToList()
                });
            }

            schema = new SchemaModel()
            {
                Tables = tables,
            };
        }

        private OnAction ConvertForeingKeyActionToOnAction(ForeignKeyAction action)
        {
            switch (action)
            {
                case ForeignKeyAction.NoAction:
                    return OnAction.NO_ACTION;
                case ForeignKeyAction.Cascade:
                    return OnAction.CASCADE;
                case ForeignKeyAction.SetDefault:
                    return OnAction.SET_DEFAULT;
                case ForeignKeyAction.SetNull:
                    return OnAction.SET_NULL;
                default:
                    return OnAction.NO_ACTION;
            }
        }
    }
}