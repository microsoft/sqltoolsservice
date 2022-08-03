//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent creating a dacfx model
    /// </summary>
    class GetObjectsFromTSqlModelOperation
    {
        public TSqlModel Model;
        public GetObjectsFromTSqlModelParams Parameters { get; }

        public GetObjectsFromTSqlModelOperation(GetObjectsFromTSqlModelParams parameters, TSqlModel model)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
            this.Model = model;
        }

        /// <summary>
        /// Get user defined objects from model
        /// </summary>
        public IEnumerable<TSqlObject> GetObjectsFromTSqlModel()
        {
            try
            {
                List<ModelTypeClass> filters = new List<ModelTypeClass>();
                foreach (var type in Parameters.ObjectTypes)
                {
                    filters.Add(MapType(type));
                }
                return Model.GetObjects(DacQueryScopes.UserDefined, filters.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, $"Failed to generate model. Error: {ex.Message}");
                throw;
            }
        }

        public static ModelTypeClass MapType(string type)
        {
            switch (type)
            {
                case "table": return ModelSchema.Table;
                case "view": return ModelSchema.View;
                case "dataType": return ModelSchema.DataType;
                default:  throw new ArgumentException($@"Unsupported data source type ""{dataSourceType}""",
                        nameof(dataSourceType));
            }
        }
    }
}