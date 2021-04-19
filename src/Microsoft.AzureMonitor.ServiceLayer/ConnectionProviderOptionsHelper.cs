using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.AzureMonitor.ServiceLayer
{
    public static class ConnectionProviderOptionsHelper
    {
        internal static ConnectionProviderOptions BuildConnectionProviderOptions()
        {
            return new ConnectionProviderOptions
            {
                Options = new[]
                {
                    new ConnectionOption
                    {
                        Name = "server",
                        DisplayName = "Server name",
                        Description = "Name of the SQL Server instance",
                        ValueType = ServiceOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueServerName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "database",
                        DisplayName = "Database name",
                        Description = "The name of the initial catalog or database int the data source",
                        ValueType = ServiceOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueDatabaseName,
                        IsIdentity = true,
                        IsRequired = false,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "authenticationType",
                        DisplayName = "Authentication type",
                        Description = "Specifies the method of authenticating with SQL Server",
                        ValueType = ServiceOption.ValueTypeCategory,
                        SpecialValueType = ConnectionOption.SpecialValueAuthType,
                        CategoryValues = new[]
                        {
                            new CategoryValue {DisplayName = "SQL Login", Name = "SqlLogin"},
                            new CategoryValue {DisplayName = "Windows Authentication", Name = "Integrated"},
                            new CategoryValue {DisplayName = "Azure Active Directory - Universal with MFA support", Name = "AzureMFA"}
                        },
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "user",
                        DisplayName = "User name",
                        Description = "Indicates the user ID to be used when connecting to the data source",
                        ValueType = ServiceOption.ValueTypeString,
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
                        ValueType = ServiceOption.ValueTypePassword,
                        SpecialValueType = ConnectionOption.SpecialValuePasswordName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "applicationIntent",
                        DisplayName = "Application intent",
                        Description = "Declares the application workload type when connecting to a server",
                        ValueType = ServiceOption.ValueTypeCategory,
                        CategoryValues = new[]
                        {
                            new CategoryValue {Name = "ReadWrite", DisplayName = "ReadWrite"},
                            new CategoryValue {Name = "ReadOnly", DisplayName = "ReadOnly"}
                        },
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "asynchronousProcessing",
                        DisplayName = "Asynchronous processing enabled",
                        Description = "When true, enables usage of the Asynchronous functionality in the .Net Framework Data Provider",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "connectTimeout",
                        DisplayName = "Connect timeout",
                        Description =
                            "The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error",
                        ValueType = ServiceOption.ValueTypeNumber,
                        DefaultValue = "15",
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "currentLanguage",
                        DisplayName = "Current language",
                        Description = "The SQL Server language record name",
                        ValueType = ServiceOption.ValueTypeString,
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "columnEncryptionSetting",
                        DisplayName = "Column encryption setting",
                        Description = "Default column encryption setting for all the commands on the connection",
                        ValueType = ServiceOption.ValueTypeCategory,
                        GroupName = "Security",
                        CategoryValues = new[]
                        {
                            new CategoryValue {Name = "Disabled"},
                            new CategoryValue {Name = "Enabled"}
                        }
                    },
                    new ConnectionOption
                    {
                        Name = "encrypt",
                        DisplayName = "Encrypt",
                        Description =
                            "When true, SQL Server uses SSL encryption for all data sent between the client and server if the servers has a certificate installed",
                        GroupName = "Security",
                        ValueType = ServiceOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "persistSecurityInfo",
                        DisplayName = "Persist security info",
                        Description =
                            "When false, security-sensitive information, such as the password, is not returned as part of the connection",
                        GroupName = "Security",
                        ValueType = ServiceOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "trustServerCertificate",
                        DisplayName = "Trust server certificate",
                        Description =
                            "When true (and encrypt=true), SQL Server uses SSL encryption for all data sent between the client and server without validating the server certificate",
                        GroupName = "Security",
                        ValueType = ServiceOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "attachedDBFileName",
                        DisplayName = "Attached DB file name",
                        Description = "The name of the primary file, including the full path name, of an attachable database",
                        ValueType = ServiceOption.ValueTypeString,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "contextConnection",
                        DisplayName = "Context connection",
                        Description =
                            "When true, indicates the connection should be from the SQL server context. Available only when running in the SQL Server process",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "port",
                        DisplayName = "Port",
                        ValueType = ServiceOption.ValueTypeNumber
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryCount",
                        DisplayName = "Connect retry count",
                        Description = "Number of attempts to restore connection",
                        ValueType = ServiceOption.ValueTypeNumber,
                        DefaultValue = "1",
                        GroupName = "Connection Resiliency"
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryInterval",
                        DisplayName = "Connect retry interval",
                        Description = "Delay between attempts to restore connection",
                        ValueType = ServiceOption.ValueTypeNumber,
                        DefaultValue = "10",
                        GroupName = "Connection Resiliency"
                    },
                    new ConnectionOption
                    {
                        Name = "applicationName",
                        DisplayName = "Application name",
                        Description = "The name of the application",
                        ValueType = ServiceOption.ValueTypeString,
                        GroupName = "Context",
                        SpecialValueType = ConnectionOption.SpecialValueAppName
                    },
                    new ConnectionOption
                    {
                        Name = "workstationId",
                        DisplayName = "Workstation Id",
                        Description = "The name of the workstation connecting to SQL Server",
                        ValueType = ServiceOption.ValueTypeString,
                        GroupName = "Context"
                    },
                    new ConnectionOption
                    {
                        Name = "pooling",
                        DisplayName = "Pooling",
                        Description =
                            "When true, the connection object is drawn from the appropriate pool, or if necessary, is created and added to the appropriate pool",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "maxPoolSize",
                        DisplayName = "Max pool size",
                        Description = "The maximum number of connections allowed in the pool",
                        ValueType = ServiceOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "minPoolSize",
                        DisplayName = "Min pool size",
                        Description = "The minimum number of connections allowed in the pool",
                        ValueType = ServiceOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "loadBalanceTimeout",
                        DisplayName = "Load balance timeout",
                        Description =
                            "The minimum amount of time (in seconds) for this connection to live in the pool before being destroyed",
                        ValueType = ServiceOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "replication",
                        DisplayName = "Replication",
                        Description = "Used by SQL Server in Replication",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        GroupName = "Replication"
                    },
                    new ConnectionOption
                    {
                        Name = "attachDbFilename",
                        DisplayName = "Attach DB filename",
                        ValueType = ServiceOption.ValueTypeString
                    },
                    new ConnectionOption
                    {
                        Name = "failoverPartner",
                        DisplayName = "Failover partner",
                        Description = "the name or network address of the instance of SQL Server that acts as a failover partner",
                        ValueType = ServiceOption.ValueTypeString,
                        GroupName = " Source"
                    },
                    new ConnectionOption
                    {
                        Name = "multiSubnetFailover",
                        DisplayName = "Multi subnet failover",
                        ValueType = ServiceOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "multipleActiveResultSets",
                        DisplayName = "Multiple active result sets",
                        Description = "When true, multiple result sets can be returned and read from one connection",
                        ValueType = ServiceOption.ValueTypeBoolean,
                        GroupName = "Advanced"
                    },
                    new ConnectionOption
                    {
                        Name = "packetSize",
                        DisplayName = "Packet size",
                        Description = "Size in bytes of the network packets used to communicate with an instance of SQL Server",
                        ValueType = ServiceOption.ValueTypeNumber,
                        GroupName = "Advanced"
                    },
                    new ConnectionOption
                    {
                        Name = "typeSystemVersion",
                        DisplayName = "Type system version",
                        Description = "Indicates which server type system then provider will expose through the DataReader",
                        ValueType = ServiceOption.ValueTypeString,
                        GroupName = "Advanced"
                    }
                }
            };
        }
    }
}