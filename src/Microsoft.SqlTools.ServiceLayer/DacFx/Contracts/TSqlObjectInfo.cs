//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Describes TSqlObject information
    /// </summary>
    public class TSqlObjectInfo
    {
        public string Name { get; set; }
        public string ObjectType { get; set; }

        public TSqlObjectRelationship[] ReferencedObjects {get; set;}
    }

    public class TSqlObjectRelationship 
    {
        public string Name {get; set;}
        public string ObjectType { get; set; }

        public string RelationshipType {get; set;}

        public string  FromObjectName {get; set;}

         public string  FromObjectType {get; set;}
    }

}
