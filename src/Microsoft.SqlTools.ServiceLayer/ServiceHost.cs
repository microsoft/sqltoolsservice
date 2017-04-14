//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Hosting
{
    /// <summary>
    /// SQL Tools VS Code Language Server request handler. Provides the entire JSON RPC
    /// implementation for sending/receiving JSON requests and dispatching the requests to
    /// handlers that are registered prior to startup.
    /// </summary>
    public sealed class ServiceHost : ServiceHostBase
    {
        /// <summary>
        /// This timeout limits the amount of time that shutdown tasks can take to complete
        /// prior to the process shutting down.
        /// </summary>
        private const int ShutdownTimeoutInSeconds = 120;
        public static readonly string[] CompletionTriggerCharacters = new string[] { ".", "-", ":", "\\", "[", "\"" };
        private IMultiServiceProvider serviceProvider;

        #region Singleton Instance Code

        /// <summary>
        /// Singleton instance of the service host for internal storage
        /// </summary>
        private static readonly Lazy<ServiceHost> instance = new Lazy<ServiceHost>(() => new ServiceHost());

        /// <summary>
        /// Current instance of the ServiceHost
        /// </summary>
        public static ServiceHost Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Constructs new instance of ServiceHost using the host and profile details provided.
        /// Access is private to ensure only one instance exists at a time.
        /// </summary>
        private ServiceHost() : base(new StdioServerChannel())
        {
            // Initialize the shutdown activities
            shutdownCallbacks = new List<ShutdownCallback>();
            initializeCallbacks = new List<InitializeCallback>();
        }

        public IMultiServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }
            internal set
            {
                serviceProvider = value;
            }
        }

        /// <summary>
        /// Provide initialization that must occur after the service host is started
        /// </summary>
        public void InitializeRequestHandlers()
        {
            // Register the requests that this service host will handle
            this.SetRequestHandler(InitializeRequest.Type, HandleInitializeRequest);
            this.SetRequestHandler(CapabilitiesRequest.Type, HandleCapabilitiesRequest);
            this.SetRequestHandler(ShutdownRequest.Type, HandleShutdownRequest);
            this.SetRequestHandler(VersionRequest.Type, HandleVersionRequest);
        }

        #endregion

        #region Member Variables

        /// <summary>
        /// Delegate definition for the host shutdown event
        /// </summary>
        /// <param name="shutdownParams"></param>
        /// <param name="shutdownRequestContext"></param>
        public delegate Task ShutdownCallback(object shutdownParams, RequestContext<object> shutdownRequestContext);

        /// <summary>
        /// Delegate definition for the host initialization event
        /// </summary>
        /// <param name="startupParams"></param>
        /// <param name="requestContext"></param>
        public delegate Task InitializeCallback(InitializeRequest startupParams, RequestContext<InitializeResult> requestContext);

        private readonly List<ShutdownCallback> shutdownCallbacks;

        private readonly List<InitializeCallback> initializeCallbacks;

        private static readonly Version serviceVersion = Assembly.GetEntryAssembly().GetName().Version;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new callback to be called when the shutdown request is submitted
        /// </summary>
        /// <param name="callback">Callback to perform when a shutdown request is submitted</param>
        public void RegisterShutdownTask(ShutdownCallback callback)
        {
            shutdownCallbacks.Add(callback);
        }

        /// <summary>
        /// Add a new method to be called when the initialize request is submitted
        /// </summary>
        /// <param name="callback">Callback to perform when an initialize request is submitted</param>
        public void RegisterInitializeTask(InitializeCallback callback)
        {
            initializeCallbacks.Add(callback);
        }

        #endregion

        #region Request Handlers

        /// <summary>
        /// Handles the shutdown event for the Language Server
        /// </summary>
        private async Task HandleShutdownRequest(object shutdownParams, RequestContext<object> requestContext)
        {
            Logger.Write(LogLevel.Normal, "Service host is shutting down...");

            // Call all the shutdown methods provided by the service components
            Task[] shutdownTasks = shutdownCallbacks.Select(t => t(shutdownParams, requestContext)).ToArray();
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(ShutdownTimeoutInSeconds);
            // shut down once all tasks are completed, or after the timeout expires, whichever comes first.
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout)).ContinueWith(t => Environment.Exit(0));
        }

        /// <summary>
        /// Handles the initialization request
        /// </summary>
        /// <param name="initializeParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        internal async Task HandleInitializeRequest(InitializeRequest initializeParams, RequestContext<InitializeResult> requestContext)
        {
            // Call all tasks that registered on the initialize request
            var initializeTasks = initializeCallbacks.Select(t => t(initializeParams, requestContext));
            await Task.WhenAll(initializeTasks);

            // TODO: Figure out where this needs to go to be agnostic of the language

            // Send back what this server can do
            await requestContext.SendResult(
                new InitializeResult
                {
                    Capabilities = new ServerCapabilities
                    {
                        TextDocumentSync = TextDocumentSyncKind.Incremental,
                        DefinitionProvider = true,
                        ReferencesProvider = false,
                        DocumentFormattingProvider = true,
                        DocumentRangeFormattingProvider = true,
                        DocumentHighlightProvider = false,
                        HoverProvider = true,
                        CompletionProvider = new CompletionOptions
                        {
                            ResolveProvider = true,
                            TriggerCharacters = CompletionTriggerCharacters
                        },
                        SignatureHelpProvider = new SignatureHelpOptions
                        {
                            TriggerCharacters = new string[] { " ", "," }
                        }
                    }
                });
        }

        internal async Task HandleCapabilitiesRequest(
            CapabilitiesRequest initializeParams, 
            RequestContext<CapabilitiesResult> requestContext)            
        {
            await requestContext.SendResult(
                new CapabilitiesResult
                {
                    Capabilities = new DmpServerCapabilities
                    {
                        ProtocolVersion = "1.0",
                        ProviderName = "MSSQL",
                        ProviderDisplayName = "Microsoft SQL Server",
                        ConnectionProvider = ServiceHost.BuildConnectionProviderOptions()                      
                    }
                }
            );            
        }

        internal static ConnectionProviderOptions BuildConnectionProviderOptions()
        {
            return new ConnectionProviderOptions
            {
                Options = new ConnectionOption[]
                {
                    new ConnectionOption
                    {
                        Name = "server",
                        DisplayName = "Server Name",
                        Description = "Name of the SQL Server instance",
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueServerName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "database",
                        DisplayName = "Database Name",
                        Description = "The name of the initial catalog or database int the data source",
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueDatabaseName,
                        IsIdentity = true,
                        IsRequired = false,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "authenticationType",
                        DisplayName = "Authentication Type",
                        Description = "Specifies the method of authenticating with SQL Server",
                        ValueType = ConnectionOption.ValueTypeCategory,
                        SpecialValueType = ConnectionOption.SpecialValueAuthType,
                        CategoryValues = new CategoryValue[] 
                        { new CategoryValue {DisplayName = "SQL Login", Name = "SqlLogin" },
                          new CategoryValue {DisplayName =  "Integrated Auth", Name= "Integrated" }
                        },
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "user",
                        DisplayName = "User Name",
                        Description = "Indicates the user ID to be used when connecting to the data source",
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueUserName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "password",
                        DisplayName = "Password",
                        Description = "Indicates the password to be used when connecting to the data source",
                        ValueType = ConnectionOption.ValueTypePassword,
                        SpecialValueType = ConnectionOption.SpecialValuePasswordName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "applicationIntent",
                        DisplayName = "Application Intent",
                        Description = "Declares the application workload type when connecting to a server",
                        ValueType = ConnectionOption.ValueTypeCategory,
                        CategoryValues = new CategoryValue[] {
                            new CategoryValue { Name = "ReadWrite", DisplayName = "ReadWrite" },
                            new CategoryValue {Name = "ReadOnly", DisplayName = "ReadOnly" }
                        },
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "asynchronousProcessing",
                        DisplayName = "Asynchronous processing enabled",
                        Description = "When true, enables usage of the Asynchronous functionality in the .Net Framework Data Provider",
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "connectTimeout",
                        DisplayName = "Connect Timeout",
                        Description = 
                        "The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "15",
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "currentLanguage",
                        DisplayName = "Current Language",
                        Description = "The SQL Server language record name",
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "columnEncryptionSetting",
                        DisplayName = "Column Encryption Setting",
                        Description = "Default column encryption setting for all the commands on the connection",
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = "Security",
                        CategoryValues = new CategoryValue[] {
                            new CategoryValue { Name = "Disabled" }, 
                            new CategoryValue {Name = "Enabled" }
                        }
                    },
                    new ConnectionOption
                    {
                        Name = "encrypt",
                        DisplayName = "Encrypt",
                        Description = 
                        "When true, SQL Server uses SSL encryption for all data sent between the client and server if the servers has a certificate installed",
                        GroupName = "Security",
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "persistSecurityInfo",
                        DisplayName = "Persist Security Info",
                        Description = "When false, security-sensitive information, such as the password, is not returned as part of the connection",
                        GroupName = "Security",
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "trustServerCertificate",
                        DisplayName = "Trust Server Certificate",
                        Description = "When true (and encrypt=true), SQL Server uses SSL encryption for all data sent between the client and server without validating the server certificate",
                        GroupName = "Security",
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "attachedDBFileName",
                        DisplayName = "Attached DB File Name",
                        Description = "The name of the primary file, including the full path name, of an attachable database",
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "contextConnection",
                        DisplayName = "Context Connection",
                        Description = "When true, indicates the connection should be from the SQL server context. Available only when running in the SQL Server process",
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "port",
                        DisplayName = "Port",
                        ValueType = ConnectionOption.ValueTypeNumber
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryCount",
                        DisplayName = "Connect Retry Count",
                        Description = "Number of attempts to restore connection",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "1",
                        GroupName = "Connection Resiliency"
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryInterval",
                        DisplayName = "Connect Retry Interval",
                        Description = "Delay between attempts to restore connection",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "10",
                        GroupName = "Connection Resiliency"

                    },
                    new ConnectionOption
                    {
                        Name = "applicationName",
                        DisplayName = "Application Name",
                        Description = "The name of the application",
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Context"
                    },
                    new ConnectionOption
                    {
                        Name = "workstationId",
                        DisplayName = "Workstation Id",
                        Description = "The name of the workstation connecting to SQL Server",
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Context"
                    },
                    new ConnectionOption
                    {
                        Name = "pooling",
                        DisplayName = "Pooling",
                        Description = "When true, the connection object is drawn from the appropriate pool, or if necessary, is created and added to the appropriate pool",
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "maxPoolSize",
                        DisplayName = "Max Pool Size",
                        Description = "The maximum number of connections allowed in the pool",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "minPoolSize",
                        DisplayName = "Min Pool Size",
                        Description = "The minimum number of connections allowed in the pool",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "loadBalanceTimeout",
                        DisplayName = "Load Balance Timeout",
                        Description = "The minimum amount of time (in seconds) for this connection to live in the pool before being destroyed",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "replication",
                        DisplayName = "Replication",
                        Description = "Used by SQL Server in Replication",
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Replication"
                    },
                    new ConnectionOption
                    {
                        Name = "attachDbFilename",
                        DisplayName = "Attach Db Filename",
                        ValueType = ConnectionOption.ValueTypeString
                    },
                    new ConnectionOption
                    {
                        Name = "failoverPartner",
                        DisplayName = "Failover Partner",
                        Description = "the name or network address of the instance of SQL Server that acts as a failover partner",
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = " Source"
                    },
                    new ConnectionOption
                    {
                        Name = "multiSubnetFailover",
                        DisplayName = "Multi Subnet Failover",
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "multipleActiveResultSets",
                        DisplayName = "Multiple Active ResultSets",
                        Description = "When true, multiple result sets can be returned and read from one connection",
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Advanced"
                    },
                    new ConnectionOption
                    {
                        Name = "packetSize",
                        DisplayName = "Packet Size",
                        Description = "Size in bytes of the network packets used to communicate with an instance of SQL Server",
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Advanced"
                    },
                    new ConnectionOption
                    {
                        Name = "typeSystemVersion",
                        DisplayName = "Type System Version",
                        Description = "Indicates which server type system then provider will expose through the DataReader",
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Advanced"
                    }
                }
            };
        }

        /// <summary>
        /// Handles the version request. Sends back the server version as result.
        /// </summary>
        private static async Task HandleVersionRequest(
          object versionRequestParams,
          RequestContext<string> requestContext)
        {
            await requestContext.SendResult(serviceVersion.ToString());
        }

        #endregion
    }
}
