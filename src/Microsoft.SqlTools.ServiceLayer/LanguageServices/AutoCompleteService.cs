//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Autocomplete functionality
    /// </summary>
    public class AutoCompleteService
    {
        #region Singleton Instance Implementation

        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<AutoCompleteService> instance 
            = new Lazy<AutoCompleteService>(() => new AutoCompleteService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static AutoCompleteService Instance 
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Default, parameterless constructor.
        /// TODO: Figure out how to make this truely singleton even with dependency injection for tests
        /// </summary>
        public AutoCompleteService()
        { 
        }

        #endregion

        // Dictionary of unique intellisense caches for each Connection
        private Dictionary<ConnectionSummary, IntellisenseCache> caches = 
            new Dictionary<ConnectionSummary, IntellisenseCache>(new ConnectionSummaryComparer());
        private Object cachesLock = new Object(); // Used when we insert/remove something from the cache dictionary

        private ISqlConnectionFactory factory;
        private Object factoryLock = new Object();

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ISqlConnectionFactory ConnectionFactory
        {
            get
            {
                lock(factoryLock)
                {
                    if(factory == null)
                    {
                        factory = new SqlConnectionFactory();
                    }
                }
                return factory;
            }
            set
            {
                lock(factoryLock)
                {
                    factory = value;
                }
            }
        }

        private ConnectionService connectionService = null;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if(connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        public void InitializeService(ServiceHost serviceHost)
        {
            // Register a callback for when a connection is created
            ConnectionServiceInstance.RegisterOnConnectionTask(UpdateAutoCompleteCache);

            // Register a callback for when a connection is closed
            ConnectionServiceInstance.RegisterOnDisconnectTask(RemoveAutoCompleteCacheUriReference);
        }

        private async Task UpdateAutoCompleteCache(ConnectionInfo connectionInfo)
        {
            if (connectionInfo != null)
            {
                await UpdateAutoCompleteCache(connectionInfo.ConnectionDetails);
            }
        }

        /// <summary>
        /// Intellisense cache count access for testing.
        /// </summary>
        internal int GetCacheCount()
        {
            return caches.Count;
        }

        /// <summary>
        /// Remove a reference to an autocomplete cache from a URI. If
        /// it is the last URI connected to a particular connection,
        /// then remove the cache.
        /// </summary>
        public async Task RemoveAutoCompleteCacheUriReference(ConnectionSummary summary)
        {
            await Task.Run( () => 
            {
                lock(cachesLock)
                {
                    IntellisenseCache cache;
                    if( caches.TryGetValue(summary, out cache) )
                    {
                        cache.ReferenceCount--;

                        // Remove unused caches
                        if( cache.ReferenceCount == 0 )
                        {
                            caches.Remove(summary);
                        }
                    }
                }
            });
        }


        /// <summary>
        /// Update the cached autocomplete candidate list when the user connects to a database
        /// </summary>
        /// <param name="details"></param>
        public async Task UpdateAutoCompleteCache(ConnectionDetails details)
        {
            IntellisenseCache cache;
            lock(cachesLock)
            {
                if(!caches.TryGetValue(details, out cache))
                {
                    cache = new IntellisenseCache(ConnectionFactory, details);
                    caches[cache.DatabaseInfo] = cache;
                }
                cache.ReferenceCount++;
            }
            
            await cache.UpdateCache();
        }

        /// <summary>
        /// Return the completion item list for the current text position.
        /// This method does not await cache builds since it expects to return quickly
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        public CompletionItem[] GetCompletionItems(TextDocumentPosition textDocumentPosition)
        {
            // Try to find a cache for the document's backing connection (if available)
            // If we have a connection but no cache, we don't care - assuming the OnConnect and OnDisconnect listeners
            // behave well, there should be a cache for any actively connected document. This also helps skip documents 
            // that are not backed by a SQL connection
            ConnectionInfo info;
            IntellisenseCache cache;
            if (ConnectionServiceInstance.TryFindConnection(textDocumentPosition.Uri, out info)
                && caches.TryGetValue((ConnectionSummary)info.ConnectionDetails, out cache))
            {
                return cache.GetAutoCompleteItems(textDocumentPosition).ToArray();
            }
            
            return new CompletionItem[0];
        }
        
    }
}
