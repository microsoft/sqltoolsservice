//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;

namespace Microsoft.SqlTools.ResourceProvider.Core.Extensibility
{
    /// <summary>
    /// Attribute defining a service export, and the metadata about that service. Implements IServiceMetadata,
    /// which should be used on the importer side to ensure type consistency. Services and providers have to add this property 
    /// in order to be found by the extension manager
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportableAttribute : ExportAttribute, IExportableMetadata
    {
        /// <summary>
        /// Default constructor to initialize the metadata of the exportable
        /// </summary>
        /// <param name="serverType">The server type supported by the exportable. If not set, exportable supports all server types</param>
        /// <param name="category">The category supported by the exportable. If not set, exportable supports all categories </param>
        /// <param name="type">The type of the exportable to be used by the extension manager to find the exportable</param>
        /// <param name="id">The unique id of the exportable. Used by the extension manager to pick only one from exportables with same id in the same assembly</param>
        /// <param name="priority">The priority of the exportable. The extension manager will pick the exportable with the highest priority if multiple found</param>
        /// <param name="displayName">The display name of the exportable. This field is optional</param>
        public ExportableAttribute(
            string serverType, 
            string category,
            Type type, 
            string id, 
            int priority = 0, 
            string displayName = null) : base(type)
        {
            Category = category;
            ServerType = serverType;
            Id = id;
            DisplayName = displayName;
            Priority = priority;
        }
        
        /// <summary>
        /// The constructor to define an exportable by type, id and priority only. To be used by the exportables that support all server types and categories.
        /// For example: the implementation of <see cref="ITrace" /> can be used for all server types and categories.
        /// </summary>
        /// <param name="type">The type of the exportable to be used by the extension manager to find the exportable</param>
        /// <param name="id">The unique id of the exportable. Used by the extension manager to pick only one from exportables with same id in the same assembly</param>
        /// <param name="priority">The priority of the exportable. The extension manager will pick the exportable with the highest priority if multiple found</param>
        /// <param name="displayName">The display name of the exportable. This field is optional</param>
        public ExportableAttribute(Type type, string id, int priority = 0,
            string displayName = null) :
            this(String.Empty, String.Empty, type, id, priority, displayName)
        {
        }

        /// <summary>
        /// Thye category of the service
        /// </summary>
        public string Category
        {
            get; 
            private set; 
        }

        /// <summary>
        /// The server type of that the service supports
        /// </summary>
        public string ServerType
        {
            get; 
            private set; 
        }

        /// <summary>
        ///  The version of this extension
        /// </summary>
        public string Version
        {
            get;
            set;
        }

        /// <summary>
        /// The id of the extension
        /// </summary>
        public string Id
        {
            get; 
            private set;
        }

        /// <summary>
        /// The display name for the extension
        /// </summary>
        public string DisplayName
        {
            get; 
            private set;
        }

        /// <summary>
        /// priority of the extension. Can be used to filter the extensions if multiple found
        /// </summary>
        public int Priority
        {
            get; 
            private set;
        }
    }
}
