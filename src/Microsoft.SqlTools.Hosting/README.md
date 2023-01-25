# SqlTools.Hosting

## Description
This library contains the necessary classes for implementing an Azure Data Studio [language server](https://code.visualstudio.com/api/language-extensions/language-server-extension-guide). It provides a simple and easy-to-use API for handling JSON-RPC requests and responses, and supports adding LSP messages and notifications. 

## Usage
Example of using extension service host to implement a JSON rpc server and registering a service with it.

```cs
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;

namespace Microsoft.SqlTools.SampleService
{
    internal class Program
    {
        private const string ServiceName = "MicrosoftSqlToolsSampleService.exe";

        internal static void Main(string[] args)
        {
            try
            {
                // reading command-line arguments
                CommandOptions commandOptions = new CommandOptions(args, ServiceName);


                // Using the command-line arguments to initialize the logger included in the library
                string logFilePath = "MicrosoftSqlToolsSampleService";
                if (!string.IsNullOrWhiteSpace(commandOptions.LogFilePath))
                {
                    logFilePath = Path.Combine(commandOptions.LogFilePath, logFilePath);
                } else 
                {
                    logFilePath = Logger.GenerateLogFilePath(logFilePath);
                    
                }

                Logger.Initialize(
                    tracingLevel: SourceLevels.Verbose, 
                    logFilePath: logFilePath, "MicrosoftSqlToolsSampleService", 
                    commandOptions.AutoFlushLog
                );

                Logger.Verbose("Starting SqlTools Sample Services.....");

                // Setting up the options for the extension service host
                ExtensibleServiceHostOptions serverOptions = new ExtensibleServiceHostOptions()
                {
                    // Name of the server
                    HostName = "Sample Server", 
                    // Unique identifier for the server
                    HostProfileId = "Microsoft.SqlTools.SampleServer", 
                    // Version of the server
                    HostVersion = new Version(1, 0, 0, 0), 
                    // Directory where the service assemblies are located
                    ExtensionServiceAssemblyDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location), 
                    // Names of the service assemblies
                    ExtensionServiceAssemblyDllFileNames = new string[] { 
                        "Microsoft.SqlTools.SampleServer.Service1MEF.dll",
                    }
                };

                // Creating the extension service host
                ExtensionServiceHost serviceHost = new ExtensionServiceHost(serverOptions);

                // Registering the service with the extension service host
                serviceHost.RegisterAndInitializedServices(SampleService.Instance); 
                serviceHost.RegisterAndInitializeService(SampleService.Instance);

                // Adding more assemblies to the extension service host
                serviceHost.LoadAndIntializeServicesFromAssesmblies(
                    new string[] {
                        "Microsoft.SqlTools.SampleServer.Services2MEF.dll"
                    }
                );

                serviceHost.WaitForExit();

                Logger.Verbose("SqlTools Sample Services stopped.");

            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("An unhandled exception occurred: {0}", e));
                Environment.Exit(1);
            }
        }
    }
}

```

### Implementing a service for Extension Service Host for MEF based consumption.

```cs
namespace Microsoft.SqlTools.SampleServer.Service1MEF
{
    [Export(typeof(IHostedService))] // This is required for MEF to discover the service
    public class SampleServiceMEF1 : IHostedService
    {
    }
}
```

```cs
namespace Microsoft.SqlTools.SampleServer.Service2MEF
{
    [Export(typeof(IHostedService))] // This is required for MEF to discover the service
    public class SampleServiceMEF2 : IHostedService
    {
    }
}
```

### Implementing a service for direct consumption.
```cs
namespace Microsoft.SqlTools.SampleServer.Service
{
    public class SampleService : IHostedService
    {
    }
}
```

## Compatibility

The library has been tested with and is compatible with following other JSON-RPC library:

* [sqlops-dataprotocolclient](https://github.com/microsoft/sqlops-dataprotocolclient)


