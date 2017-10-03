//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;

namespace Microsoft.SqlTools.Azure.Core.Extensibility
{
    /// <summary>
    /// The metadata describing an extension
    /// </summary>
    public interface IExportableMetadata : IStandardMetadata, IServerDefinition
    {
        /// <summary>
        /// Exportable priority tobe used when multiple of same type found
        /// </summary>
        int Priority
        {
            get;
        }
    }

    public class ExportableMetadata : IExportableMetadata
    {
        /// <summary>
        /// Default constructor to initialize the metadata of the exportable
        /// </summary>
        /// <param name="serverType">The server type supported by the exportable. If not set, exportable supports all server types</param>
        /// <param name="category">The category supported by the exportable. If not set, exportable supports all categories </param>
        /// <param name="id">The unique id of the exportable. Used by the extension manager to pick only one from exportables with same id in the same assembly</param>
        /// <param name="priority">The priority of the exportable. The extension manager will pick the exportable with the highest priority if multiple found</param>
        /// <param name="displayName">The display name of the exportable. This field is optional</param>
        public ExportableMetadata(
            string serverType,
            string category,
            string id,
            int priority = 0,
            string displayName = null)
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
        /// <param name="id">The unique id of the exportable. Used by the extension manager to pick only one from exportables with same id in the same assembly</param>
        /// <param name="priority">The priority of the exportable. The extension manager will pick the exportable with the highest priority if multiple found</param>
        /// <param name="displayName">The display name of the exportable. This field is optional</param>
        public ExportableMetadata(string id, int priority = 0,
            string displayName = null) :
            this(string.Empty, string.Empty, id, priority, displayName)
        {
        }

        public int Priority
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string DisplayName
        {
            get;
            set;
        }

        public string Category
        {
            get;
            set;
        }

        public string ServerType
        {
            get;
            set;
        }
    }
}
