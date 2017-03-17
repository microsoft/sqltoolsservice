//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Main class for Scripting Service functionality
    /// </summary>
    public sealed class ScriptingService
    {    
        private static readonly Lazy<ScriptingService> LazyInstance = new Lazy<ScriptingService>(() => new ScriptingService());

        public static ScriptingService Instance => LazyInstance.Value;

        private static ConnectionService connectionService = null;        

        private static LanguageService languageServices = null;   

         /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
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

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static LanguageService LanguageServiceInstance
        {
            get
            {
                if (languageServices == null)
                {
                    languageServices = LanguageService.Instance;
                }
                return languageServices;
            }
            set
            {
                languageServices = value;
            }
        }

        /// <summary>
        /// Initializes the Scripting Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ScriptingScriptAsRequest.Type, HandleScriptingScriptAsRequest);
        }

         /// <summary>
        /// Create a SqlConnection to use for querying metadata
        /// </summary>
        internal static SqlConnection OpenConnection(ConnectionInfo connInfo)
        {
            try
            {                 
                // increase the connection timeout to at least 30 seconds and and build connection string
                // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                connInfo.ConnectionDetails.PersistSecurityInfo = true;
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;

                // open a dedicated binding server connection
                SqlConnection sqlConn = new SqlConnection(connectionString); 
                sqlConn.Open();
                return sqlConn;
            }
            catch (Exception)
            {
            }
            
            return null;
        }

        /// <summary>
        /// Handle Script As Create requests
        /// </summary>
        private static string HandleScriptCreate(
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            // get or create the current parse info object
            ScriptParseInfo parseInfo = LanguageServiceInstance.GetScriptParseInfo(connInfo.OwnerUri);
            if (Monitor.TryEnter(parseInfo.BuildingMetadataLock, LanguageService.BindingTimeout))
            {
                try
                {
                    QueueItem queueItem = LanguageServiceInstance.BindingQueue.QueueBindingOperation(
                        key: parseInfo.ConnectionKey,
                        bindingTimeout: 60000,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            PeekDefinition peekDefinition = new PeekDefinition(bindingContext.ServerConnection, connInfo);
                            var results = peekDefinition.GetTableScripts(metadata.Name, metadata.Schema);
                            string script = string.Empty;
                            if (results != null) 
                            {
                                foreach (var result in results)
                                {
                                    script += result.ToString() + Environment.NewLine + Environment.NewLine;
                                }
                            }
                            return script;
                        });

                    queueItem.ItemProcessed.WaitOne();

                    return queueItem.GetResultAsT<string>();
                }
                finally
                {
                    Monitor.Exit(parseInfo.BuildingMetadataLock);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Handles script as request messages
        /// </summary>
        /// <param name="scriptingParams"></param>
        /// <param name="requestContext"></param>
        internal static async Task HandleScriptingScriptAsRequest(
            ScriptingScriptAsParams scriptingParams,
            RequestContext<ScriptingScriptAsResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ScriptingService.ConnectionServiceInstance.TryFindConnection(
                    scriptingParams.OwnerUri,
                    out connInfo);

                ObjectMetadata metadata = scriptingParams.Metadata;
                string script = string.Empty;

                if (connInfo != null) 
                {
                    if (scriptingParams.Operation == ScriptOperation.Select)
                    {                    
                        script = string.Format(
@"SELECT *
FROM {0}.{1}",
                            scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
                    }
                    else if (scriptingParams.Operation == ScriptOperation.Create)
                    {
                        script = HandleScriptCreate(connInfo, metadata);
                    }
                    else if (scriptingParams.Operation == ScriptOperation.Update)
                    {
                        script = string.Format(
                            @"UPDATE {0}.{1}",
                        scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
                    }
                    else if (scriptingParams.Operation == ScriptOperation.Insert)
                    {
                        script = string.Format(
                            @"INSERT {0}.{1}",
                        scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
                    }
                    else if (scriptingParams.Operation == ScriptOperation.Delete)
                    {
                        script = string.Format(
                            @"DELETE {0}.{1}",
                        scriptingParams.Metadata.Schema, scriptingParams.Metadata.Name);
                    }
                }

                await requestContext.SendResult(new ScriptingScriptAsResult
                {
                    OwnerUri = scriptingParams.OwnerUri,
                    Script = script
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
    }
}
