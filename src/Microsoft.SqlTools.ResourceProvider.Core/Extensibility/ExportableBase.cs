//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlTools.Extensibility;

namespace Microsoft.SqlTools.ResourceProvider.Core.Extensibility
{
    /// <summary>
    /// The base class for all exportable classes.
    /// </summary>
    public abstract class ExportableBase : TraceableBase, IExportable, IComposableService
    {
        private ITrace _trace;
        private ExportableStatus _exportableStatus = new ExportableStatus();

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            ServiceProvider = provider;
        }

        /// <summary>
        /// The exportable metadata
        /// </summary>
        public IExportableMetadata Metadata
        {
            get; 
            set;
        }


        /// <summary>
        /// Gets or sets the dependency manager to provider the dependencies of the class
        /// </summary>
        public IMultiServiceProvider ServiceProvider
        {
            get; 
            private set;
        }

        public virtual ExportableStatus Status
        {
            get { return _exportableStatus; }
        }

        /// <summary>
        /// Finds a service of specific type which has the same metadata as class using the dependency manager.
        /// If multiple services found, the one with the highest priority will be returned
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <returns>A service of type T or null if not found</returns>
        protected T GetService<T>()
            where T : IExportable
        {            
            return GetService<T>(Metadata);
        }

        /// <summary>
        /// Finds a service of specific type which has the same metadata as class using the dependency manager.
        /// If multiple services found, the one with the highest priority will be returned
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <returns>A service of type T or null if not found</returns>
        protected T GetService<T>(IExportableMetadata metadata)
            where T : IExportable
        {
            //Don't try to find the service if it's the same service as current one with same metadata
            if (ServiceProvider != null && (!(this is T) || metadata != Metadata))
            {
                return ServiceProvider.GetService<T>(metadata);
            }
            return default(T);
        }

        /// <summary>
        /// An instance of ITrace which is exported to the extension manager
        /// </summary>
        public override ITrace Trace
        {
            get
            {
                return (_trace = _trace ?? GetService<ITrace>());
            }
            set
            {
                _trace = value;
            }
        }

        /// <summary>
        /// ServerDefinition created from the metadata 
        /// </summary>
        protected ServerDefinition ServerDefinition
        {
            get
            {
                return Metadata != null ? new ServerDefinition(Metadata.ServerType, Metadata.Category) : null;
            }
        }
    }
}
