﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using Microsoft.SqlTools.BatchParser.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    public static class ReliableConnectionHelper
    {
        private const int PCU1BuildNumber = 2816;
        public readonly static SqlConnectionStringBuilder BuilderWithDefaultApplicationName = new SqlConnectionStringBuilder("server=(local);");

        private const string ServerNameLocalhost = "localhost";
        private const string SqlProviderName = "System.Data.SqlClient";

        private const string ApplicationIntent = "ApplicationIntent";
        private const string MultiSubnetFailover = "MultiSubnetFailover";
        private const string DacFxApplicationName = "DacFx";

        private const int SqlDwEngineEditionId = (int)DatabaseEngineEdition.SqlDataWarehouse;

        // See MSDN documentation for "SERVERPROPERTY (SQL Azure Database)" for "EngineEdition" property:
        // http://msdn.microsoft.com/en-us/library/ee336261.aspx
        private const int SqlAzureEngineEditionId = 5;

        private static Lazy<HashSet<int>> cloudEditions = new Lazy<HashSet<int>>(() => new HashSet<int>()
        {
            (int)DatabaseEngineEdition.SqlDatabase,
            (int)DatabaseEngineEdition.SqlDataWarehouse,
            (int)DatabaseEngineEdition.SqlStretchDatabase,
            (int)DatabaseEngineEdition.SqlOnDemand,
            // Note: for now, ignoring managed instance as it should be treated just like on prem.
        });

        /// <summary>
        /// Opens the connection and sets the lock/command timeout and pooling=false.
        /// </summary>
        /// <returns>The opened connection</returns>
        public static IDbConnection OpenConnection(SqlConnectionStringBuilder csb, bool useRetry, string azureAccountToken)
        {
            csb.Pooling = false;
            return OpenConnection(csb.ToString(), useRetry, azureAccountToken);
        }

        /// <summary>
        /// Opens the connection and sets the lock/command timeout.  This routine
        /// will assert if pooling!=false.
        /// </summary>
        /// <returns>The opened connection</returns>
        public static IDbConnection OpenConnection(string connectionString, bool useRetry, string azureAccountToken)
        {
#if DEBUG
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                Debug.Assert(!builder.Pooling, "Pooling should be false");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, "Invalid connectionstring: " + ex.Message);
            }
#endif

            if (AmbientSettings.AlwaysRetryOnTransientFailure)
            {
                useRetry = true;
            }

            RetryPolicy commandRetryPolicy, connectionRetryPolicy;
            if (useRetry)
            {
                commandRetryPolicy = RetryPolicyFactory.CreateDefaultSchemaCommandRetryPolicy(useRetry: true);
                connectionRetryPolicy = RetryPolicyFactory.CreateDefaultSchemaConnectionRetryPolicy();
            }
            else
            {
                commandRetryPolicy = RetryPolicyFactory.CreateNoRetryPolicy();
                connectionRetryPolicy = RetryPolicyFactory.CreateNoRetryPolicy();
            }

            ReliableSqlConnection connection = new ReliableSqlConnection(connectionString, connectionRetryPolicy, commandRetryPolicy, azureAccountToken);

            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {

                string debugMessage = String.Format(CultureInfo.CurrentCulture,
                    "Opening connection using connection string '{0}' failed with exception: {1}", connectionString, ex.Message);
#if DEBUG
                Debug.WriteLine(debugMessage);
#endif
                connection.Dispose();
                throw;
            }

            return connection;
        }

        /// <summary>
        /// Opens the connection (if it is not already) and sets
        /// the lock/command timeout.
        /// </summary>
        /// <param name="conn">The connection to open</param>
        public static void OpenConnection(IDbConnection conn)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
        }

        /// <summary>
        /// Opens a connection using 'csb' as the connection string.  Provide
        /// 'usingConnection' to execute T-SQL against the open connection and
        /// 'catchException' to handle errors.
        /// </summary>
        /// <param name="csb">The connection string used when opening the IDbConnection</param>
        /// <param name="usingConnection">delegate called when the IDbConnection has been successfully opened</param>
        /// <param name="catchException">delegate called when an exception has occurred.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <param name="useRetry">Should retry logic be used when opening the connection</param>
        public static void OpenConnection(
            SqlConnectionStringBuilder csb,
            Action<IDbConnection> usingConnection,
            Predicate<Exception> catchException,
            bool useRetry,
            string azureAccountToken)
        {
            Validate.IsNotNull(nameof(csb), csb);
            Validate.IsNotNull(nameof(usingConnection), usingConnection);

            try
            {
                // Always disable pooling
                csb.Pooling = false;
                using (IDbConnection conn = OpenConnection(csb.ConnectionString, useRetry, azureAccountToken))
                {
                    usingConnection(conn);
                }
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                {
                    throw;
                }
            }
        }

        /*
        TODO - re-enable if we port ConnectionStringSecurer
        /// <summary>
        /// This method provides the provides a connection string configured with the specified database name.
        /// This is also an opportunity to decrypt the connection string based on the encryption/decryption strategy.
        /// InvalidConnectionStringException could be thrown since this routine attempts to restore the connection
        /// string if 'restoreConnectionString' is true.
        /// 
        /// Will only set DatabaseName/ApplicationName if the value is not null.
        /// </summary>
        ///         
        public static SqlConnectionStringBuilder ConfigureConnectionString(
            string connectionString,
            string databaseName,
            string applicationName,
            bool restoreConnectionString = true)
        {
            if (restoreConnectionString)
            {
                // Read the connection string through the persistence layer
                connectionString = ConnectionStringSecurer.RestoreConnectionString(
                    connectionString,
                    SqlProviderName);
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            builder.Pooling = false;
            builder.MultipleActiveResultSets = false;

            // Cannot set the applicationName/initialCatalog to null but empty string is valid
            if (databaseName != null)
            {
                builder.InitialCatalog = databaseName;
            }

            if (applicationName != null)
            {
                builder.ApplicationName = applicationName;
            }

            return builder;
        }
        */

        /// <summary>
        /// Optional 'initializeConnection' routine.  This sets the lock and command timeout for the connection.
        /// </summary>
        public static void SetLockAndCommandTimeout(IDbConnection conn)
        {
            ReliableSqlConnection.SetLockAndCommandTimeout(conn);
        }

        /// <summary>
        /// Opens a IDbConnection, creates a IDbCommand and calls ExecuteNonQuery against the connection.
        /// </summary>
        /// <param name="csb">The connection string.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="initializeCommand">Optional delegate to initialize the IDbCommand before execution.  
        /// Default is SqlConnectionHelper.SetCommandTimeout</param>
        /// <param name="catchException">delegate called when an exception has occurred.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <param name="useRetry">Should a retry policy be used when calling ExecuteNonQuery</param>
        /// <returns>The number of rows affected</returns>
        public static object ExecuteNonQuery(
            SqlConnectionStringBuilder csb,
            string commandText,
            Action<IDbCommand> initializeCommand,
            Predicate<Exception> catchException,
            bool useRetry,
            string azureAccountToken)
        {
            object retObject = null;
            OpenConnection(
                csb,
                (connection) =>
                {
                    retObject = ExecuteNonQuery(connection, commandText, initializeCommand, catchException);
                },
                catchException,
                useRetry,
                azureAccountToken);

            return retObject;
        }

        /// <summary>
        /// Creates a IDbCommand and calls ExecuteNonQuery against the connection.
        /// </summary>
        /// <param name="conn">The connection.  This must be opened.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="initializeCommand">Optional delegate to initialize the IDbCommand before execution.  
        /// Default is SqlConnectionHelper.SetCommandTimeout</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The number of rows affected</returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static object ExecuteNonQuery(
            IDbConnection conn,
            string commandText,
            Action<IDbCommand> initializeCommand,
            Predicate<Exception> catchException)
        {
            Validate.IsNotNull(nameof(conn), conn);
            Validate.IsNotNullOrEmptyString(nameof(commandText), commandText);

            IDbCommand cmd = null;
            try
            {

                Debug.Assert(conn.State == ConnectionState.Open, "connection passed to ExecuteNonQuery should be open.");

                cmd = conn.CreateCommand();

                initializeCommand ??= SetCommandTimeout;
                initializeCommand(cmd);

                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;

                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                {
                    throw;
                }
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a IDbCommand and calls ExecuteScalar against the connection.
        /// </summary>
        /// <param name="conn">The connection.  This must be opened.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="initializeCommand">Optional delegate to initialize the IDbCommand before execution.  
        /// Default is SqlConnectionHelper.SetCommandTimeout</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The scalar result</returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static object ExecuteScalar(
            IDbConnection conn,
            string commandText,
            Action<IDbCommand> initializeCommand = null,
            Predicate<Exception> catchException = null)
        {
            Validate.IsNotNull(nameof(conn), conn);
            Validate.IsNotNullOrEmptyString(nameof(commandText), commandText);

            IDbCommand cmd = null;

            try
            {
                Debug.Assert(conn.State == ConnectionState.Open, "connection passed to ExecuteScalar should be open.");

                cmd = conn.CreateCommand();

                initializeCommand ??= SetCommandTimeout;
                initializeCommand(cmd);

                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                {
                    throw;
                }
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a IDbCommand and calls ExecuteReader against the connection.
        /// </summary>
        /// <param name="conn">The connection to execute the reader on.  This must be opened.</param>
        /// <param name="commandText">The command text to execute</param>
        /// <param name="readResult">A delegate used to read from the reader</param>
        /// <param name="initializeCommand">Optional delegate to initialize the IDbCommand object</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static void ExecuteReader(
            IDbConnection conn,
            string commandText,
            Action<IDataReader> readResult,
            Action<IDbCommand> initializeCommand = null,
            Predicate<Exception> catchException = null)
        {
            Validate.IsNotNull(nameof(conn), conn);
            Validate.IsNotNullOrEmptyString(nameof(commandText), commandText);
            Validate.IsNotNull(nameof(readResult), readResult);

            IDbCommand cmd = null;
            try
            {
                cmd = conn.CreateCommand();

                initializeCommand ??= SetCommandTimeout;
                initializeCommand(cmd);

                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    readResult(reader);
                }
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                {
                    throw;
                }
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
        }
        /// <summary>
        /// Creates a IDbCommand and calls ExecuteReader using the provided connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="commandText">The command text to execute</param>
        /// <param name="readResult">A delegate used to read from the reader</param>
        /// <param name="initializeCommand">Optional delegate to initialize the IDbCommand object</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        public static void ExecuteReader(
            string connectionString,
            string commandText,
            Action<IDataReader> readResult,
            Action<IDbCommand> initializeCommand = null,
            Predicate<Exception> catchException = null)
        {
            Validate.IsNotNull(nameof(connectionString), connectionString);
            Validate.IsNotNullOrEmptyString(nameof(commandText), commandText);
            Validate.IsNotNull(nameof(readResult), readResult);
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                ExecuteReader(sqlConnection, commandText, readResult, initializeCommand, catchException);
            }
        }

        /// <summary>
        /// optional 'initializeCommand' routine.  This initializes the IDbCommand
        /// </summary>
        /// <param name="cmd"></param>
        public static void SetCommandTimeout(IDbCommand cmd)
        {
            Validate.IsNotNull(nameof(cmd), cmd);
            cmd.CommandTimeout = CachedServerInfo.Instance.GetQueryTimeoutSeconds(cmd.Connection);
        }

        public static DatabaseEngineEdition GetEngineEdition(IDbConnection connection)
        {
            Validate.IsNotNull(nameof(connection), connection);
            if (!(connection.State == ConnectionState.Open))
            {
                Logger.Write(TraceEventType.Warning, Resources.ConnectionPassedToIsCloudShouldBeOpen);
            }

            Func<string, DatabaseEngineEdition> executeCommand = commandText =>
            {
                DatabaseEngineEdition result = DatabaseEngineEdition.Unknown;
                ExecuteReader(connection,
                    commandText,
                    readResult: (reader) =>
                    {
                        reader.Read();
                        result = (DatabaseEngineEdition)int.Parse(reader[0].ToString(), CultureInfo.InvariantCulture);
                    }
                );
                return result;
            };

            DatabaseEngineEdition engineEdition = DatabaseEngineEdition.Unknown;
            try
            {
                engineEdition = executeCommand(SqlConnectionHelperScripts.EngineEdition);
            }
            catch (SqlException)
            {
                // The default query contains a WITH (NOLOCK).  This doesn't work for Azure DW or SqlOnDemand, so when things don't work out, 
                // we'll fall back to a version without NOLOCK and try again.
                engineEdition = executeCommand(SqlConnectionHelperScripts.EngineEditionWithLock);
            }

            return engineEdition;
        }

        /// <summary>
        /// Return true if the database is an Azure database
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static bool IsCloud(IDbConnection connection)
        {
            return IsCloudEngineId((int)GetEngineEdition(connection));
        }
        private static bool IsCloudEngineId(int engineEditionId)
        {
            return cloudEditions.Value.Contains(engineEditionId);
        }

        /// <summary>
        /// Handles the exceptions typically thrown when a SQLConnection is being opened
        /// </summary>
        /// <returns>True if the exception was handled</returns>
        public static bool StandardExceptionHandler(Exception ex)
        {
            Validate.IsNotNull(nameof(ex), ex);

            if (ex is SqlException ||
                ex is RetryLimitExceededException)
            {
                return true;
            }
            if (ex is InvalidCastException ||
                ex is ArgumentException ||         // Thrown when a particular connection string property is invalid (i.e. failover parner = "yes")
                ex is InvalidOperationException || // thrown when the connection pool is empty and SQL is down
                ex is TimeoutException ||
                ex is SecurityException)
            {
                return true;
            }

            Logger.Write(TraceEventType.Error, ex.ToString());
            return false;
        }

        /// <summary>
        /// Returns the default database path.
        /// </summary>
        /// <param name="conn">The connection</param>
        /// <param name="initializeCommand">The delegate used to initialize the command</param>
        /// <param name="catchException">The exception handler delegate. If Null is passed in then all exceptions are thrown</param>
        public static string GetDefaultDatabaseFilePath(
            IDbConnection conn,
            Action<IDbCommand> initializeCommand = null,
            Predicate<Exception> catchException = null)
        {
            Validate.IsNotNull(nameof(conn), conn);

            string filePath = null;
            ServerInfo info = GetServerVersion(conn);

            if (!info.IsCloud)
            {
                filePath = GetDefaultDatabasePath(conn, SqlConnectionHelperScripts.GetDatabaseFilePathAndName, initializeCommand, catchException);
            }

            return filePath;
        }

        /// <summary>
        /// Returns the log path or null
        /// </summary>
        /// <param name="conn">The connection</param>
        /// <param name="initializeCommand">The delegate used to initialize the command</param>
        /// <param name="catchException">The exception handler delegate. If Null is passed in then all exceptions are thrown</param>
        public static string GetDefaultDatabaseLogPath(
            IDbConnection conn,
            Action<IDbCommand> initializeCommand = null,
            Predicate<Exception> catchException = null)
        {
            Validate.IsNotNull(nameof(conn), conn);

            string logPath = null;
            ServerInfo info = GetServerVersion(conn);

            if (!info.IsCloud)
            {
                logPath = GetDefaultDatabasePath(conn, SqlConnectionHelperScripts.GetDatabaseLogPathAndName, initializeCommand, catchException);
            }

            return logPath;
        }

        /// <summary>
        /// Returns the database path or null
        /// </summary>
        /// <param name="conn">The connection</param>
        /// <param name="commandText">The command to issue</param>
        /// <param name="initializeCommand">The delegate used to initialize the command</param>
        /// <param name="catchException">The exception handler delegate. If Null is passed in then all exceptions are thrown</param>
        private static string GetDefaultDatabasePath(
            IDbConnection conn,
            string commandText,
            Action<IDbCommand> initializeCommand = null,
            Predicate<Exception> catchException = null)
        {
            Validate.IsNotNull(nameof(conn), conn);
            Validate.IsNotNullOrEmptyString(nameof(commandText), commandText);

            string filePath = ExecuteScalar(conn, commandText, initializeCommand, catchException) as string;
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                // Remove filename from the filePath
                if (!Uri.IsWellFormedUriString(filePath, UriKind.Absolute))
                {
                    // In linux "file://" is required otehrwise the Uri cannot parse the path
                    //this should be fixed in dotenet core 2.0
                    filePath = $"file://{filePath}";
                }
                if (!Uri.TryCreate(filePath, UriKind.Absolute, out Uri pathUri))
                {
                    // Invalid Uri
                    return null;
                }

                // Get a current directory path relative to the pathUri
                // This will remove filename from the uri.
                Uri filePathUri = new Uri(pathUri, ".");
                // For file uri we need to get LocalPath instead of file:// url
                filePath = filePathUri.IsFile ? filePathUri.LocalPath : filePathUri.OriginalString;
            }
            return filePath;
        }

        /// <summary>
        /// Returns true if the database is readonly.  This routine will swallow the exceptions you might expect from SQL using StandardExceptionHandler.
        /// </summary>
        public static bool IsDatabaseReadonly(SqlConnectionStringBuilder builder, string azureAccountToken)
        {
            Validate.IsNotNull(nameof(builder), builder);

            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            bool isDatabaseReadOnly = false;

            OpenConnection(
                builder,
                (connection) =>
                {
                    string commandText = String.Format(CultureInfo.InvariantCulture, SqlConnectionHelperScripts.CheckDatabaseReadonly, builder.InitialCatalog);
                    ExecuteReader(connection,
                        commandText,
                        readResult: (reader) =>
                        {
                            if (reader.Read())
                            {
                                string currentSetting = reader.GetString(1);
                                if (String.Compare(currentSetting, "ON", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    isDatabaseReadOnly = true;
                                }
                            }
                        });
                },
                (ex) =>
                {
                    Logger.Write(TraceEventType.Error, ex.ToString());
                    return StandardExceptionHandler(ex); // handled
                },
                useRetry: true,
                azureAccountToken: azureAccountToken);

            return isDatabaseReadOnly;
        }

        public class ServerInfo
        {
            public int ServerMajorVersion;
            public int ServerMinorVersion;
            public int ServerReleaseVersion;
            public int EngineEditionId;
            public string ServerVersion;
            public string ServerLevel;
            public string ServerEdition;
            public bool IsCloud;
            public int AzureVersion;

            // In SQL 2012 SP1 Selective XML indexes were added. There is bug where upgraded databases from previous versions
            // of SQL Server do not have their metadata upgraded to include the xml_index_type column in the sys.xml_indexes view. Because
            // of this, we must detect the presence of the column to determine if we can query for Selective Xml Indexes
            public bool IsSelectiveXmlIndexMetadataPresent;
            public string OsVersion;
            public string MachineName;
            public string ServerName;
            public Nullable<int> CpuCount;
            public Nullable<int> PhysicalMemoryInMB;
            public Dictionary<string, object> Options { get; set; }
        }

        public class ServerHostInfo
        {
            public string Platform;
            public string Distribution;
            public string Release;
            public string ServicePackLevel;
        }

        public class ServerSystemInfo
        {
            public Nullable<int> CpuCount;
            public Nullable<int> PhysicalMemoryInMB;
        }

        public static bool TryGetServerVersion(string connectionString, out ServerInfo serverInfo, string azureAccountToken)
        {
            serverInfo = null;
            if (!TryGetConnectionStringBuilder(connectionString, out SqlConnectionStringBuilder builder))
            {
                return false;
            }

            serverInfo = GetServerVersion(builder, azureAccountToken);
            return true;
        }

        /// <summary>
        /// Returns the version of the server.  This routine will throw if an exception is encountered.
        /// </summary>
        public static ServerInfo GetServerVersion(SqlConnectionStringBuilder csb, string azureAccountToken)
        {
            Validate.IsNotNull(nameof(csb), csb);
            ServerInfo serverInfo = null;

            OpenConnection(
                csb,
                (connection) =>
                {
                    serverInfo = GetServerVersion(connection);
                },
                catchException: null, // Always throw
                useRetry: true,
                azureAccountToken: azureAccountToken);

            return serverInfo;
        }

        /// <summary>
        /// Gets the server host information from sys.dm_os_host_info view
        /// </summary>
        /// <param name="connection">The connection</param>
        public static ServerHostInfo GetServerHostInfo(IDbConnection connection)
        {
            var hostInfo = new ServerHostInfo();
            // SQL Server 2016 and earlier versions does not provide sys.dm_os_host_info and we know the host OS can only be Windows.
            if (!Version.TryParse(ReadServerVersion(connection), out var hostVersion) || hostVersion.Major <= 13)
            {
                try
                {
                    hostInfo.Platform = "Windows";
                    ExecuteReader(
                        connection,
                        SqlConnectionHelperScripts.GetHostWindowsVersion,
                        reader =>
                        {
                            reader.Read();
                            hostInfo.Release = reader[0].ToString();
                        });
                }
                catch
                {
                    // Ignore the error and only set the Platform to Windows by default
                }
            }
            else
            {
                ExecuteReader(
                    connection,
                    SqlConnectionHelperScripts.GetHostInfo,
                    reader =>
                    {
                        reader.Read();
                        hostInfo.Platform = reader[0].ToString();
                        hostInfo.Distribution = reader[1].ToString();
                        hostInfo.Release = reader[2].ToString();
                        hostInfo.ServicePackLevel = reader[3].ToString();
                    });
            }
            return hostInfo;
        }

        /// <summary>
        /// Gets the server host cpu count and memory from sys.dm_os_sys_info view
        /// </summary>
        /// <param name="connection">The connection</param>
        public static ServerSystemInfo GetServerCpuAndMemoryInfo(IDbConnection connection)
        {
            var sysInfo = new ServerSystemInfo();
            try
            {
                SqlConnection sqlConnection = GetAsSqlConnection(connection);
                var server = new Server(new ServerConnection(sqlConnection));
                var defaultFields = new List<string>();
                var isProcessorsSupported = server.IsSupportedProperty(nameof(server.Processors));
                if (isProcessorsSupported)
                {
                    defaultFields.Add(nameof(server.Processors));
                }
                var isPhysicalMemorySupported = server.IsSupportedProperty(nameof(server.PhysicalMemory));
                if (isPhysicalMemorySupported)
                {
                    defaultFields.Add(nameof(server.PhysicalMemory));
                }
                if (defaultFields.Any())
                {
                    server.SetDefaultInitFields(server.GetType(), defaultFields.ToArray());
                }

                sysInfo.CpuCount = isProcessorsSupported ? server.Processors as int? : null;
                sysInfo.PhysicalMemoryInMB = isPhysicalMemorySupported ? server.PhysicalMemory as int? : null;
            }
            catch (Exception ex)
            {
                // We don't want to fail the normal flow if any unexpected thing happens
                // since these properties are not available for types of sql servers and users 
                // and it is not essential to always include them
                // just logging the errors here and moving on with the workflow. 
                Logger.Write(TraceEventType.Error, ex.ToString());
            }
            return sysInfo;
        }

        /// <summary>
        /// Returns the version of the server.  This routine will throw if an exception is encountered.
        /// </summary>
        public static ServerInfo GetServerVersion(IDbConnection connection)
        {
            Validate.IsNotNull(nameof(connection), connection);
            if (!(connection.State == ConnectionState.Open))
            {
                Logger.Write(TraceEventType.Error, "connection passed to GetServerVersion should be open.");
            }

            Func<string, ServerInfo> getServerInfo = commandText =>
            {
                ServerInfo serverInfo = new ServerInfo();
                ExecuteReader(
                connection,
                commandText,
                delegate (IDataReader reader)
                {
                    reader.Read();

                    int engineEditionId = Int32.Parse(reader[0].ToString(), CultureInfo.InvariantCulture);

                    serverInfo.EngineEditionId = engineEditionId;
                    serverInfo.IsCloud = IsCloudEngineId(engineEditionId);

                    serverInfo.ServerVersion = reader[1].ToString();
                    serverInfo.ServerLevel = reader[2].ToString();
                    serverInfo.ServerEdition = reader[3].ToString();
                    serverInfo.MachineName = reader[4].ToString();
                    serverInfo.ServerName = reader[5].ToString();

                    if (reader.FieldCount > 6)
                    {
                        // Detect the presence of SXI
                        serverInfo.IsSelectiveXmlIndexMetadataPresent = reader.GetInt32(6) == 1;
                    }

                    // The 'ProductVersion' server property is of the form ##.#[#].####.#,
                    Version serverVersion = new Version(serverInfo.ServerVersion);

                    // The server version is of the form ##.##.####,
                    serverInfo.ServerMajorVersion = serverVersion.Major;
                    serverInfo.ServerMinorVersion = serverVersion.Minor;
                    serverInfo.ServerReleaseVersion = serverVersion.Build;

                    if (serverInfo.IsCloud)
                    {
                        serverInfo.AzureVersion = serverVersion.Major > 11 ? 2 : 1;
                    }

                    try
                    {
                        CachedServerInfo.Instance.AddOrUpdateIsCloud(connection, serverInfo.IsCloud);
                    }
                    catch (Exception ex)
                    {
                        //we don't want to fail the normal flow if any unexpected thing happens
                        //during caching although it's unlikely. So we just log the exception and ignore it
                        Logger.Write(TraceEventType.Error, Resources.FailedToCacheIsCloud);
                        Logger.Write(TraceEventType.Error, ex.ToString());
                    }
                });

                // Also get the OS Version
                var hostInfo = GetServerHostInfo(connection);

                // Examples:
                // SQL Server on Linux : Ubuntu 16.04
                // SQL Server on Windows:
                //  major version <= 13 (SQL Server 2016) - Windows 6.5
                //  otherwise - Windows Server 2019 Standard 10.0
                serverInfo.OsVersion = hostInfo.Distribution != null ? string.Format("{0} {1}", hostInfo.Distribution, hostInfo.Release) : string.Format("{0} {1}", hostInfo.Platform, hostInfo.Release);

                var sysInfo = GetServerCpuAndMemoryInfo(connection);

                serverInfo.CpuCount = sysInfo.CpuCount;
                serverInfo.PhysicalMemoryInMB = sysInfo.PhysicalMemoryInMB;

                serverInfo.Options = new Dictionary<string, object>();

                return serverInfo;
            };

            ServerInfo result = null;
            try
            {
                result = getServerInfo(SqlConnectionHelperScripts.EngineEdition);
            }
            catch (SqlException)
            {
                // The default query contains a WITH (NOLOCK).  This doesn't work for Azure DW, so when things don't work out, 
                // we'll fall back to a version without NOLOCK and try again.
                result = getServerInfo(SqlConnectionHelperScripts.EngineEditionWithLock);
            }

            return result;
        }

        public static string GetServerName(IDbConnection connection)
        {
            return new DbConnectionWrapper(connection).DataSource;
        }

        public static string ReadServerVersion(IDbConnection connection)
        {
            return new DbConnectionWrapper(connection).ServerVersion;
        }

        /// <summary>
        /// Converts to a SqlConnection by casting (if we know it is actually a SqlConnection)
        /// or by getting the underlying connection (if it's a ReliableSqlConnection)
        /// </summary>
        public static SqlConnection GetAsSqlConnection(IDbConnection connection)
        {
            return new DbConnectionWrapper(connection).GetAsSqlConnection();
        }

        /* TODO - CloneAndOpenConnection() requires IClonable, which doesn't exist in .NET Core
        /// <summary>
        /// Clones a connection and ensures it's opened. 
        /// If it's a SqlConnection it will clone it,
        /// and for ReliableSqlConnection it will clone the underling connection.
        /// The reason the entire ReliableSqlConnection is not cloned is that it includes
        /// several callbacks and we don't want to try and handle deciding how to clone these
        /// yet.
        /// </summary>
        public static SqlConnection CloneAndOpenConnection(IDbConnection connection)
        {
            return new DbConnectionWrapper(connection).CloneAndOpenConnection();
        }
        */

        public class ServerAndDatabaseInfo : ServerInfo
        {
            public int DbCompatibilityLevel;
            public string DatabaseName;
        }

        public static bool TryGetConnectionStringBuilder(string connectionString, out SqlConnectionStringBuilder builder)
        {
            builder = null;

            if (String.IsNullOrEmpty(connectionString))
            {
                // Connection string is not valid
                return false;
            }

            // Attempt to initialize the builder
            Exception handledEx = null;
            try
            {
                builder = new SqlConnectionStringBuilder(connectionString);
            }
            catch (KeyNotFoundException ex)
            {
                handledEx = ex;
            }
            catch (FormatException ex)
            {
                handledEx = ex;
            }
            catch (ArgumentException ex)
            {
                handledEx = ex;
            }

            if (handledEx != null)
            {
                Logger.Write(TraceEventType.Error, String.Format(Resources.ErrorParsingConnectionString, handledEx));
                return false;
            }

            return true;
        }

        /*
        /// <summary>
        /// Get the version of the server and database using
        /// the connection string provided.  This routine will 
        /// throw if an exception is encountered.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="info">Basic information about the server</param>
        public static bool GetServerAndDatabaseVersion(string connectionString, out ServerAndDatabaseInfo info)
        {
            bool foundVersion = false;
            info = new ServerAndDatabaseInfo { IsCloud = false, ServerMajorVersion = -1, DbCompatibilityLevel = -1, DatabaseName = String.Empty };

            SqlConnectionStringBuilder builder;
            if (!TryGetConnectionStringBuilder(connectionString, out builder))
            {
                return false;
            }

            // The database name is either the InitialCatalog or the AttachDBFilename.  The
            // AttachDBFilename is used if an mdf file is specified in the connections dialog.
            if (String.IsNullOrEmpty(builder.InitialCatalog) ||
                String.IsNullOrEmpty(builder.AttachDBFilename))
            {
                builder.Pooling = false;

                string tempDatabaseName = String.Empty;
                int tempDbCompatibilityLevel = 0;
                ServerInfo serverInfo = null;

                OpenConnection(
                    builder,
                    (connection) =>
                    {
                        // Set the lock timeout to 3 seconds
                        SetLockAndCommandTimeout(connection);

                        serverInfo = GetServerVersion(connection);

                        tempDatabaseName = (!string.IsNullOrEmpty(builder.InitialCatalog)) ?
                            builder.InitialCatalog : builder.AttachDBFilename;

                        // If at this point the dbName remained an empty string then
                        // we should get the database name from the open IDbConnection
                        if (String.IsNullOrEmpty(tempDatabaseName))
                        {
                            tempDatabaseName = connection.Database;
                        }

                        // SQL Azure does not support custom DBCompat values.
                        SqlParameter databaseNameParameter = new SqlParameter(
                            "@dbname",
                            SqlDbType.NChar,
                            128,
                            ParameterDirection.Input,
                            false,
                            0,
                            0,
                            null,
                            DataRowVersion.Default,
                            tempDatabaseName);

                        object compatibilityLevel;

                        using (IDbCommand versionCommand = connection.CreateCommand())
                        {
                            versionCommand.CommandText = "SELECT compatibility_level FROM sys.databases WITH (NOLOCK) WHERE name = @dbname";
                            versionCommand.CommandType = CommandType.Text;
                            versionCommand.Parameters.Add(databaseNameParameter);
                            compatibilityLevel = versionCommand.ExecuteScalar();
                        }

                        // value is null if db is not online
                        foundVersion = compatibilityLevel != null && !(compatibilityLevel is DBNull);
                        if(foundVersion)
                        {
                            tempDbCompatibilityLevel = (byte)compatibilityLevel;
                        }
                        else
                        {
                            string conString = connection.ConnectionString == null ? "null" : connection.ConnectionString;
                            string dbName = tempDatabaseName == null ? "null" : tempDatabaseName;
                            string message = string.Format(CultureInfo.CurrentCulture, 
                                "Querying database compatibility level failed. Connection string: '{0}'. dbname: '{1}'.",
                                conString, dbName);
                            Tracer.TraceEvent(TraceEventType.Error, TraceId.CoreServices, message);
                        }
                    },
                 catchException: null, // Always throw
                 useRetry: true);

                info.IsCloud = serverInfo.IsCloud;
                info.ServerMajorVersion = serverInfo.ServerMajorVersion;
                info.ServerMinorVersion = serverInfo.ServerMinorVersion;
                info.ServerReleaseVersion = serverInfo.ServerReleaseVersion;
                info.ServerVersion = serverInfo.ServerVersion;
                info.ServerLevel = serverInfo.ServerLevel;
                info.ServerEdition = serverInfo.ServerEdition;
                info.AzureVersion = serverInfo.AzureVersion;
                info.DatabaseName = tempDatabaseName;
                info.DbCompatibilityLevel = tempDbCompatibilityLevel;
            }

            return foundVersion;
        }
        */

        /// <summary>
        /// Returns true if the authenticating database is master, otherwise false.  An example of
        /// false is when the user is a contained user connecting to a contained database.
        /// </summary>
        public static bool IsAuthenticatingDatabaseMaster(IDbConnection connection)
        {
            try
            {
                const string sqlCommand =
                    @"use [{0}];
    if (db_id() = 1)
    begin
    -- contained auth is 0 when connected to master
    select 0
    end
    else
    begin
    -- need dynamic sql so that we compile this query only when we know resource db is available
    exec('select case when authenticating_database_id = 1 then 0 else 1 end from sys.dm_exec_sessions where session_id = @@SPID')
    end";

                string finalCmd = null;
                if (!String.IsNullOrWhiteSpace(connection.Database))
                {
                    finalCmd = String.Format(CultureInfo.InvariantCulture, sqlCommand, connection.Database);
                }
                else
                {
                    finalCmd = String.Format(CultureInfo.InvariantCulture, sqlCommand, "master");
                }

                object retValue = ExecuteScalar(connection, finalCmd);
                if (retValue != null && retValue.ToString() == "1")
                {
                    // contained auth is 0 when connected to non-master 
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                if (StandardExceptionHandler(ex))
                {
                    return true;
                }
                throw;
            }
        }

        /// <summary>
        /// Returns true if the authenticating database is master, otherwise false.  An example of
        /// false is when the user is a contained user connecting to a contained database.
        /// </summary>
        public static bool IsAuthenticatingDatabaseMaster(SqlConnectionStringBuilder builder, string azureAccountToken)
        {
            bool authIsMaster = true;
            OpenConnection(
                builder,
                usingConnection: (connection) =>
                {
                    authIsMaster = IsAuthenticatingDatabaseMaster(connection);
                },
                catchException: StandardExceptionHandler, // Don't throw unless it's an unexpected exception
                useRetry: true,
                azureAccountToken: azureAccountToken);
            return authIsMaster;
        }

        /// <summary>
        /// Returns the form of the server as a it's name - replaces . and (localhost)
        /// </summary>
        public static string GetCompleteServerName(string server)
        {
            if (String.IsNullOrEmpty(server))
            {
                return server;
            }

            int nlen = 0;
            if (server[0] == '.')
            {
                nlen = 1;
            }
            else if (String.Compare(server, Constants.Local, StringComparison.OrdinalIgnoreCase) == 0)
            {
                nlen = Constants.Local.Length;
            }
            else if (String.Compare(server, 0, ServerNameLocalhost, 0, ServerNameLocalhost.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                nlen = ServerNameLocalhost.Length;
            }

            if (nlen > 0)
            {
                string strMachine = Environment.MachineName;
                if (server.Length == nlen)
                    return strMachine;
                if (server.Length > (nlen + 1) && server[nlen] == '\\') // instance
                {
                    string strRet = strMachine + server.Substring(nlen);
                    return strRet;
                }
            }

            return server;
        }

        /*
        /// <summary>
        /// Processes a user-supplied connection string and provides a trimmed connection string
        /// that eliminates everything except for DataSource, InitialCatalog, UserId, Password,
        /// ConnectTimeout, Encrypt, TrustServerCertificate and IntegratedSecurity.
        /// </summary>
        /// <exception cref="InvalidConnectionStringException">When connection string is invalid</exception>
        public static string TrimConnectionString(string connectionString)
        {
            Exception handledException;

            try
            {
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connectionString);
                return TrimConnectionStringBuilder(scsb).ConnectionString;
            }
            catch (ArgumentException exception)
            {
                handledException = exception;
            }
            catch (KeyNotFoundException exception)
            {
                handledException = exception;
            }
            catch (FormatException exception)
            {
                handledException = exception;
            }

            throw new InvalidConnectionStringException(handledException);
        }
        */

        ///<summary>
        /// Sql 2012 PCU1 introduces breaking changes to metadata queries and adds new Selective XML Index support. 
        /// This method allows components to detect if the <see cref="ServerInfo"/> represents a build of SQL 2012 after RTM.
        ///</summary> 
        public static bool IsVersionGreaterThan2012RTM(ServerInfo _serverInfo)
        {
            return _serverInfo.ServerMajorVersion > 11 ||
                // Use the presence of SXI metadata rather than build number as upgrade bugs leave out the SXI metadata for some upgraded databases.
                _serverInfo.ServerMajorVersion == 11 && _serverInfo.IsSelectiveXmlIndexMetadataPresent;
        }


        // SQL Server: Defect 1122301: ReliableConnectionHelper does not maintain ApplicationIntent
        // The ApplicationIntent and MultiSubnetFailover property is not introduced to .NET until .NET 4.0 update 2
        // However, DacFx is officially depends on .NET 4.0 RTM
        // So here we want to support both senarios, on machine with 4.0 RTM installed, it will ignore these 2 properties
        // On machine with higher .NET version which included those properties, it will pick them up.
        public static void TryAddAlwaysOnConnectionProperties(SqlConnectionStringBuilder userBuilder, SqlConnectionStringBuilder trimBuilder)
        {
            if (userBuilder.ContainsKey(ApplicationIntent))
            {
                trimBuilder[ApplicationIntent] = userBuilder[ApplicationIntent];
            }

            if (userBuilder.ContainsKey(MultiSubnetFailover))
            {
                trimBuilder[MultiSubnetFailover] = userBuilder[MultiSubnetFailover];
            }
        }

        /* TODO - this relies on porting SqlAuthenticationMethodUtils
        /// <summary>
        /// Processes a user-supplied connection string and provides a trimmed connection string
        /// that eliminates everything except for DataSource, InitialCatalog, UserId, Password,
        /// ConnectTimeout, Encrypt, TrustServerCertificate, IntegratedSecurity and Pooling.
        /// </summary>
        /// <remarks>
        /// Pooling is always set to false to avoid connections remaining open.
        /// </remarks>
        /// <exception cref="InvalidConnectionStringException">When connection string is invalid</exception>
        public static SqlConnectionStringBuilder TrimConnectionStringBuilder(SqlConnectionStringBuilder userBuilder, Action<string> throwException = null)
        {

            Exception handledException;

            if (throwException == null)
            {
                throwException = (propertyName) =>
                {
                    throw new InvalidConnectionStringException(String.Format(CultureInfo.CurrentCulture, Resources.UnsupportedConnectionStringArgument, propertyName));
                };
            }
            if (!String.IsNullOrEmpty(userBuilder.AttachDBFilename))
            {
                throwException("AttachDBFilename");
            }
            if (userBuilder.UserInstance)
            {
                throwException("User Instance");
            }

            try
            {
                SqlConnectionStringBuilder trimBuilder = new SqlConnectionStringBuilder();

                if (String.IsNullOrWhiteSpace(userBuilder.DataSource))
                {
                    throw new InvalidConnectionStringException();
                }

                trimBuilder.ConnectTimeout = userBuilder.ConnectTimeout;
                trimBuilder.DataSource = userBuilder.DataSource;

                if (false == String.IsNullOrWhiteSpace(userBuilder.InitialCatalog))
                {
                    trimBuilder.InitialCatalog = userBuilder.InitialCatalog;
                }

                trimBuilder.IntegratedSecurity = userBuilder.IntegratedSecurity;

                if (!String.IsNullOrWhiteSpace(userBuilder.UserID))
                {
                        trimBuilder.UserID = userBuilder.UserID;
                }

                if (!String.IsNullOrWhiteSpace(userBuilder.Password))                    
                {
                    trimBuilder.Password = userBuilder.Password;
                }

                trimBuilder.TrustServerCertificate = userBuilder.TrustServerCertificate;
                trimBuilder.Encrypt = userBuilder.Encrypt;

                if (String.IsNullOrWhiteSpace(userBuilder.ApplicationName) ||
                    String.Equals(BuilderWithDefaultApplicationName.ApplicationName, userBuilder.ApplicationName, StringComparison.Ordinal))
                {
                    trimBuilder.ApplicationName = DacFxApplicationName;
                }
                else
                {
                    trimBuilder.ApplicationName = userBuilder.ApplicationName;
                }

                TryAddAlwaysOnConnectionProperties(userBuilder, trimBuilder);

                if (SqlAuthenticationMethodUtils.IsAuthenticationSupported())
                {
                    SqlAuthenticationMethodUtils.SetAuthentication(userBuilder, trimBuilder);
                }

                if (SqlAuthenticationMethodUtils.IsCertificateSupported())
                {
                    SqlAuthenticationMethodUtils.SetCertificate(userBuilder, trimBuilder);
                }

                trimBuilder.Pooling = false;
                return trimBuilder;
            }
            catch (ArgumentException exception)
            {
                handledException = exception;
            }
            catch (KeyNotFoundException exception)
            {
                handledException = exception;
            }
            catch (FormatException exception)
            {
                handledException = exception;
            }

            throw new InvalidConnectionStringException(handledException);
        }

        public static bool TryCreateConnectionStringBuilder(string connectionString, out SqlConnectionStringBuilder builder, out Exception handledException)
        {
            bool success = false;
            builder = null;
            handledException = null;
            try
            {
                builder = TrimConnectionStringBuilder(new SqlConnectionStringBuilder(connectionString));

                success = true;
            }
            catch (InvalidConnectionStringException e)
            {
                handledException = e;
            }
            catch (ArgumentException exception)
            {
                handledException = exception;
            }
            catch (KeyNotFoundException exception)
            {
                handledException = exception;
            }
            catch (FormatException exception)
            {
                handledException = exception;
            }
            finally
            {
                if (handledException != null)
                {
                    success = false;
                }
            }
            return success;
        }
        */
    }
}
