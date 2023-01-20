Hosting project contains the necessary classes for implementing a JSON - RPC server. It also contains the Utility classes for Logging, Parsing command line arguments from the RPC client. 

## Example of using the Extension Service Host.
Example of using extension service host to implement a JSON rpc server and registering a service with it.
```cs
ExtensibleServiceHostOptions<IHostedService> serverOptions = new ExtensibleServiceHostOptions<IHostedService>()
                {
                    HostName = "<host name>",
                    HostProfileId = "<host profile id>",
                    HostVersion = new Version(1, 0, 0, 0),
                    ExtensionServiceAssemblyDirectory = "<path to directory containing service dll>",
                    ExtensionServiceAssemblyDllFileNames = new string[] {
                        "Microsoft.SqlTools.Service.dll",
                    },
                    InitializeServiceCallback = (serviceHost, service) => service.InitializeService(serviceHost)
                };
ExtensionServiceHost<IHostedService> serviceHost = new ExtensionServiceHost<IHostedService>(serverOptions);

// More assembly dlls can be added later during runtime: 
serviceHost.LoadAndIntializeServicesFromAssesmblies(
                    new string[] {
                        "Microsoft.SqlTools.Service2.dll"
                    }
                );

// To directly register a service class with the host:
serviceHost.RegisterAndInitializeService(ServiceClass);

// To register a set of services with the host:
serviceHost.RegisterAndInitializeServices(serviceList);

// Waiting while the host is running
serviceHost.WaitForExit();
```

### Implementing a service for Extension Service Host for MEF based consumption.

```cs
namespace Microsoft.SqlTools.FlatFileImport.Services
{
    [Export(typeof(IHostedService))]
    public class FlatFileImportService : IHostedService
    {
    }
}
```

### Implementing a service for direct consumption.
```cs
namespace Microsoft.SqlTools.FlatFileImport.Services
{
    public class FlatFileImportService2 : IHostedService
    {
    }
}
```

## Using the logger class

This project also provides implmentation of logging class. It provides a simple way to log messages to a file. The logger class can be initialized with the following parameters:

```cs
 Logger.Initialize(tracingLevel: SourceLevels.Verbose, logFilePath: "<path-to-log-file>", "<traceSource>", true);
 Logger.Write(TraceEventType.Verbose, "<verbose message>");
```


