//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecutionGraph
{
    /// <summary>
    /// Parses ShowPlan XML objects derived from RelOpBaseType type
    /// </summary>
    internal sealed class IndexOpTypeParser : RelOpBaseTypeParser
    {
        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private IndexOpTypeParser()
        {
        }

        /// <summary>
        /// Singelton instance
        /// </summary>
        private static IndexOpTypeParser indexOpTypeParser = null;
        public static new IndexOpTypeParser Instance
        {
            get
            {
                if (indexOpTypeParser == null)
                {
                    indexOpTypeParser = new IndexOpTypeParser();
                }
                return indexOpTypeParser;
            }
        }


        /// <summary>
        /// Retrieves the ObjectType that the Index operation references.
        /// </summary>
        /// <param name="indexScanType">The current Index operation node being parsed</param>
        private ObjectType GetObjectTypeFromProperties(object parsedItem)
        {
            ObjectType objectType = null;

            // The index operators operate on an object, get that object.
            PropertyDescriptor objectProperty = TypeDescriptor.GetProperties(parsedItem)["Object"];
            Debug.Assert(objectProperty != null, "Object expected");
            if (objectProperty != null)
            {
                object objectItem = objectProperty.GetValue(parsedItem);
                if (objectItem != null)
                {
                    ObjectType[] objectTypeArray = objectItem as ObjectType[];
                    Debug.Assert(objectTypeArray != null && objectTypeArray.Length == 1, "ObjectTypeArray is null or more than one object found for IndexScan");

                    // Only handle the index operations operate on one object
                    if (objectTypeArray != null && objectTypeArray.Length == 1)
                    {
                        objectType = objectTypeArray[0];
                    }
                }
            }

            return objectType;
        }

        /// <summary>
        /// Adds the indexKind attribute, if it exists, from the ObjectType as a property in the targetPropertyBag.
        /// </summary>
        /// <param name="objectType">The objectType for the indexScan.</param>
        /// <param name="targetPropertyBag">The target the property bag where we will put the PhysicalOperationKind element.</param>
        private void AddIndexKindAsPhysicalOperatorKind(ObjectType objectType, PropertyDescriptorCollection targetPropertyBag)
        {
            if (objectType.IndexKindSpecified)
            {
                if (0 < objectType.IndexKind.ToString().Length)
                {
                    PropertyDescriptor wrapperProperty = PropertyFactory.CreateProperty("PhysicalOperationKind", objectType.IndexKind.ToString());
                    if (wrapperProperty != null)
                    {
                        targetPropertyBag.Add(wrapperProperty);
                    }
                }
            }
        }


        /// <summary>
        /// Updates node special properties.
        /// </summary>
        /// <param name="node">Node being parsed.  The node should be IndexScanType or CreateIndexType</param>
        public override void ParseProperties(object parsedItem, PropertyDescriptorCollection targetPropertyBag, NodeBuilderContext context)
        {
            Debug.Assert((parsedItem is IndexScanType) || (parsedItem is CreateIndexType), "IndexScanType or CreateIndexType object expected");

            // Parse the item as usual with RelOpBaseTypeParser first
            base.ParseProperties(parsedItem, targetPropertyBag, context);

            // Now look for the object and get the indexKind
            if ((parsedItem is IndexScanType) || (parsedItem is CreateIndexType))
            {
                ObjectType objectType = this.GetObjectTypeFromProperties(parsedItem);
                if (objectType != null)
                {
                    this.AddIndexKindAsPhysicalOperatorKind(objectType, targetPropertyBag);
                }
            }
        }
    }
}
