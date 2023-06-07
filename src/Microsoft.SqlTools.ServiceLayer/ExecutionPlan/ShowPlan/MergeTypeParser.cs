//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Parses ShowPlan XML objects derived from RelOpBaseType type
    /// </summary>
    internal sealed class MergeTypeParser : RelOpBaseTypeParser
    {
        /// <summary>
        /// Private constructor prevents this object from being externally instantiated
        /// </summary>
        private MergeTypeParser()
        {
        }

        /// <summary>
        /// Updates node special properties.
        /// </summary>
        /// <param name="node">Node being parsed.</param>
        public override void ParseProperties(object parsedItem, PropertyDescriptorCollection targetPropertyBag, NodeBuilderContext context)
        {
            base.ParseProperties(parsedItem, targetPropertyBag, context);

            MergeType item = parsedItem as MergeType;
            Debug.Assert(item != null, "MergeType object expected");

            // Make a new property which combines "InnerSideJoinColumns" and "OuterSideJoinColumns"
            object mergeColumnsWrapper = ObjectWrapperTypeConverter.Convert(new MergeColumns(item));
            PropertyDescriptor wrapperProperty = PropertyFactory.CreateProperty("WhereJoinColumns", mergeColumnsWrapper);
            if (wrapperProperty != null)
            {
                targetPropertyBag.Add(wrapperProperty);
            }
        }

        /// <summary>
        /// Indicates if a property should be skipped from the target property bag
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected override bool ShouldSkipProperty(PropertyDescriptor property)
        {
            if (property.Name == "InnerSideJoinColumns" || property.Name == "OuterSideJoinColumns")
            {
                // These two properties are handled in a special way
                return true;
            }

            return base.ShouldSkipProperty(property);
        }


        /// <summary>
        /// Singelton instance
        /// </summary>
        private static MergeTypeParser mergeTypeParser = null;
        public static new MergeTypeParser Instance
        {
            get
            {
                mergeTypeParser ??= new MergeTypeParser();
                return mergeTypeParser;
            }
        }
    }

    /// <summary>
    /// This type is used for 2 purposes:
    /// 1) It creates additional level in the property hierarchy. Instead of including
    /// InnerSideJoinColumns and OuterSideJoinColumnsField properties in the Node, we
    /// create additional property which has these two properties as nested properties.
    /// 2) It allows to convert this to string the same way we convert other custom types
    /// See static Convert(MergeColumns) method in ObjectWrapperTypeConverter.cs
    /// </summary>
    public sealed class MergeColumns
    {
        public MergeColumns(MergeType mergeType)
        {
            this.innerSideJoinColumnsField = mergeType.InnerSideJoinColumns;
            this.outerSideJoinColumnsField = mergeType.OuterSideJoinColumns;
        }

        public ColumnReferenceType[] InnerSideJoinColumns
        {
            get { return this.innerSideJoinColumnsField; }
        }

        public ColumnReferenceType[] OuterSideJoinColumns
        {
            get { return this.outerSideJoinColumnsField; }
        }

        private ColumnReferenceType[] innerSideJoinColumnsField;
        private ColumnReferenceType[] outerSideJoinColumnsField;
    }
}
