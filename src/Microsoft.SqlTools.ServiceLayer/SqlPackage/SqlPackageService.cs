//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage
{
    public class SqlPackageService
    {
        private static readonly Lazy<SqlPackageService> instance = new Lazy<SqlPackageService>(() => new SqlPackageService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SqlPackageService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the SqlPackage service by registering request handlers with the service host.
        /// This method sets up the handler for generating SqlPackage CLI command strings.
        /// </summary>
        /// <param name="serviceHost">The service host that manages RPC communication with clients</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GenerateSqlPackageCommandRequest.Type, this.HandleGenerateSqlPackageCommandRequest, true);
        }

        /// <summary>
        /// Handles requests to generate SqlPackage CLI command strings for various database operations.
        /// Delegates to the SqlPackage API which routes to the appropriate command generator based on 
        /// the specified action and builds properly formatted command strings with all necessary parameters and options.
        /// </summary>
        /// <param name="parameters">Parameters containing the action type and operation-specific settings (connection strings, file paths, deployment options, etc.)</param>
        /// <param name="requestContext">The request context for sending the command result back to the client</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task HandleGenerateSqlPackageCommandRequest(GenerateSqlPackageCommandParams parameters, RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                // Delegate to unified SqlPackage API method
                string command = SqlPackageCommandBuilder.GenerateSqlPackageCommand(parameters);

                await requestContext.SendResult(new SqlPackageCommandResult()
                {
                    Command = command,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (Exception e)
            {
                Logger.Error($"SqlPackage GenerateCommand failed: {e.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult()
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }
    }
}