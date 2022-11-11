//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent request to get objects from model
    /// </summary>
    public class GetObjectsFromTSqlModelOperation
    {
        private TSqlModel Model;
        public GetObjectsFromTSqlModelParams Parameters { get; }

        public GetObjectsFromTSqlModelOperation(GetObjectsFromTSqlModelParams parameters, TSqlModel model)
        {
            Validate.IsNotNull("parameters", parameters);
            Validate.IsNotNull("model", model);
            this.Parameters = parameters;
            this.Model = model;
        }

        /// <summary>
        /// Get user defined top level type objects from model
        /// </summary>
        public TSqlObjectInfo[] GetObjectsFromTSqlModel()
        {
            try
            {
                var filters = Parameters.ObjectTypes.Select(MapType).ToArray();
                var objects = Model.GetObjects(DacQueryScopes.UserDefined, filters).ToList();

                return objects.Select(o => new TSqlObjectInfo
                {
                    Name = o.Name.ToString(),
                    ObjectType = o.ObjectType.Name,
                    ReferencedObjects = o.GetReferencedRelationshipInstances().ToList().Select(r => new TSqlObjectRelationship
                    {
                        Name = r.ObjectName.ToString(),
                        ObjectType = r.Object.ObjectType.Name,
                        RelationshipType = r.Relationship.Type.ToString(),
                        FromObjectName = r.FromObject.Name.ToString(),
                        FromObjectType = r.FromObject.ObjectType.Name
                    }).ToArray()
                }).ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception(SR.GetUserDefinedObjectsFromModelFailed, ex));
                throw;
            }
        }

        /// <summary>
        /// Class to represent the type of objects to query within the sql model
        /// </summary>
        public static ModelTypeClass MapType(string type)
        {
            switch (type.ToLower())
            {
                case "table":
                    return ModelSchema.Table;
                case "view":
                    return ModelSchema.View;
                default:
                    throw new ArgumentException(SR.UnsupportedModelType(type));
            }
        }
    }
}