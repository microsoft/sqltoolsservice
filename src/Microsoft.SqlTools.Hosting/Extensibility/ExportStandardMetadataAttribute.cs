//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;

namespace Microsoft.SqlTools.Extensibility
{
    /// <summary>
    /// Base attribute class for TracingLevel export definitions. 
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class ExportStandardMetadataAttribute : ExportAttribute, IStandardMetadata
    {
        /// <summary>
        /// Base class for DAC extensibility exports
        /// </summary>
        protected ExportStandardMetadataAttribute(Type contractType, string id, string displayName = null)
            : base(contractType)
        {
            Id = id;
            DisplayName = displayName; 
        }
        

        /// <summary>
        /// The version of this extension
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The id of the extension
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The display name for the extension
        /// </summary>
        public virtual string DisplayName { get; private set; }
    }
}
