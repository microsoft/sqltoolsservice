//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.NotebookConvert.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Notebook Convert Service
    /// </summary>
    public class NotebookConvertService
    {
        private static readonly Lazy<NotebookConvertService> instance = new Lazy<NotebookConvertService>(() => new NotebookConvertService());

        public NotebookConvertService()
        {
        }

        public static NotebookConvertService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service by doing tasks such as setting up request handlers. 
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            this.ServiceHost.SetRequestHandler(ConvertNotebookToSqlRequest.Type, HandleConvertNotebookToSqlRequest);
            this.ServiceHost.SetRequestHandler(ConvertSqlToNotebookRequest.Type, HandleConvertSqlToNotebookRequest);
           

        }

        #region Convert Handlers

        internal async Task HandleConvertNotebookToSqlRequest(ConvertNotebookToSqlParams parameters, RequestContext<ConvertNotebookToSqlResult> requestContext)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var result = new ConvertNotebookToSqlResult();
                    result.content = parameters.NotebookJson;
                    await requestContext.SendResult(result);
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }

        internal async Task HandleConvertSqlToNotebookRequest(ConvertSqlToNotebookParams parameters, RequestContext<ConvertSqlToNotebookResult> requestContext)
        {
            await Task.Run(async () =>
            {
               
                try
                {
                    var file = WorkspaceService<SqlToolsSettings>.Instance.Workspace.GetFile(parameters.ClientUri);
                    // Temporary notebook that we just fill in with the sql until the parsing logic is added
                    var result = new ConvertSqlToNotebookResult
                    {
                        content = $@"{{
    ""metadata"": {{
        ""kernelspec"": {{
                    ""name"": ""SQL"",
            ""display_name"": ""SQL"",
            ""language"": ""sql""
        }},
        ""language_info"": {{
                    ""name"": ""sql"",
            ""version"": """"
        }}
            }},
    ""nbformat_minor"": 2,
    ""nbformat"": 4,
    ""cells"": [
        {{
                ""cell_type"": ""code"",
            ""source"": [
                ""{file.Contents}""
            ],
            ""metadata"": {{
                ""azdata_cell_guid"": ""477da394-51fd-45ab-8a37-387b47b2b692""
            }},
            ""outputs"": [],
            ""execution_count"": null
        }}
    ]
}}"
                    };
                    await requestContext.SendResult(result);
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }

        #endregion // Convert Handlers

    }
}
