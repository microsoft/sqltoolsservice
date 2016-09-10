#if false
//------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Data.Tools.Components.Common;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.Common.SqlClient;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.Data.Tools.Schema.ScriptDom.Sql;
using Microsoft.Data.Tools.Schema.Sql.Common;
using Microsoft.Data.Tools.Schema.Sql.Deployment;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common.SqlClient
{
    internal static class ConnectionHelperUtils
    {
        public readonly static int DefaultCommandTimeout = SqlRegistryManager.Instance.DatabaseQueryTimeout;
        public readonly static int DefaultLockTimeoutMs = SqlRegistryManager.Instance.DatabaseLockTimeoutMS;
        public readonly static SqlConnectionStringBuilder BuilderWithDefaultApplicationName = new SqlConnectionStringBuilder("server=(local);");

        private const string ContainedDbQuery = "sp_configure 'contained database authentication'";
        private const string RunValue = "run_value";

        // See MSDN documentation for "SERVERPROPERTY (SQL Azure Database)" for "EngineEdition" property:
        // http://msdn.microsoft.com/en-us/library/ee336261.aspx
        private const int SqlAzureEngineEditionId = 5;

        // See the PDW T-SQL Compat 2 Functional Spec for "SERVERPROPERTY('EngineEdition')"
        public readonly static int SqlPdwEngineEditionId = 6;

        /// <summary>
        /// We need a place to plumb through the SSDT defaults into DacFx. We actually want to avoid loading 
        /// DacFx binaries where possible, so avoid calling this in the package constructors. Instead we
        /// assume this is logically the first "Connection" related class that will be invoked, and encourage
        /// this by routing GetServerVersion calls through the class
        /// </summary>
        static ConnectionHelperUtils()
        {
            InitDefaultConnectionSettings();
        }

        public static void InitDefaultConnectionSettings()
        {
            AmbientSettings.DefaultSettings.QueryTimeoutSeconds = DefaultCommandTimeout;
            AmbientSettings.DefaultSettings.LockTimeoutMilliSeconds = DefaultLockTimeoutMs;
            AmbientSettings.DefaultSettings.LongRunningQueryTimeoutSeconds = SqlRegistryManager.Instance.DatabaseLongRunningQueryTimeout;
        }
        
        /// <summary>
        /// Note: Plumbs through a call to ReliableConnectionHelper to ensure that correct defaults for connections
        /// are set up in DacFx.
        /// 
        /// Returns the version of the server.  This routine will throw if an exception is encountered.
        /// </summary>
        public static ReliableConnectionHelper.ServerInfo GetServerVersion(IDbConnection connection)
        {
            return ReliableConnectionHelper.GetServerVersion(connection);
        }


        /// <summary>
        /// Note: Plumbs through a call to ReliableConnectionHelper to ensure that correct defaults for connections
        /// are set up in DacFx.
        /// 
        /// Returns the version of the server.  This routine will throw if an exception is encountered.
        /// </summary>
        public static ReliableConnectionHelper.ServerInfo GetServerVersion(SqlConnectionStringBuilder csb)
        {
            return ReliableConnectionHelper.GetServerVersion(csb);
        }

        /// <summary>
        /// Note: Plumbs through a call to ReliableConnectionHelper to ensure that correct defaults for connections
        /// are set up in DacFx.
        /// 
        /// Get the version of the server and database using
        /// the connection string provided.  This routine will 
        /// throw if an exception is encountered.
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="info">Basic information about the server</param>
        public static bool GetServerAndDatabaseVersion(string connectionString, out ReliableConnectionHelper.ServerAndDatabaseInfo info)
        {
            return ReliableConnectionHelper.GetServerAndDatabaseVersion(connectionString, out info);
        }
        /// <summary>
        /// Opens the connection and sets the lock/command timeout and pooling=false.
        /// </summary>
        /// <returns>The opened connection</returns>
        public static SqlConnection OpenSqlConnection(SqlConnectionStringBuilder csb)
        {
            csb.Pooling = false;
            return OpenSqlConnection(csb.ToString());
        }

        /// <summary>
        /// Opens the connection and sets the lock/command timeout.  This routine
        /// will assert if pooling!=false.
        /// </summary>
        /// <returns>The opened connection</returns>
        public static SqlConnection OpenSqlConnection(string connectionString)
        {
#if DEBUG
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                SqlTracer.AssertTraceEvent(builder.Pooling == false, TraceEventType.Warning, SqlTraceId.CoreServices, 
                    "Pooling should be false");
            }
            catch (Exception ex)
            {
                SqlTracer.AssertTraceEvent(false, TraceEventType.Warning, SqlTraceId.CoreServices, "Invalid connectionstring: " + ex.Message);
            }
#endif
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SetLockAndCommandTimeout(conn);
            return conn;
        }

        /// <summary>
        /// Opens the connection (if it is not already) and sets
        /// the lock/command timeout.
        /// </summary>
        /// <param name="conn">The connection to open</param>
        public static void OpenSqlConnection(SqlConnection conn)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
                SetLockAndCommandTimeout(conn);
            }
        }

        /// <summary>
        /// Opens a connection using 'csb' as the connection string.  Provide
        /// 'usingConnection' to execute T-SQL against the open connection and
        /// 'catchException' to handle errors.
        /// </summary>
        /// <param name="csb">The connection string used when opening the SqlConnection</param>
        /// <param name="usingConnection">delegate called when the SqlConnection has been successfully opened</param>
        /// <param name="catchException">delegate called when an exception has occurred.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        public static void OpenSqlConnection(SqlConnectionStringBuilder csb,
            Action<SqlConnection> usingConnection,
            Predicate<Exception> catchException)
        {
            SqlArgumentValidation.CheckForNullReference(csb, "csb");
            SqlArgumentValidation.CheckForNullReference(usingConnection, "usingConnection");

            try
            {
                // Always disable pooling
                DisablePooling(csb);

                using (SqlConnection conn = new SqlConnection(csb.ConnectionString))
                {
                    conn.Open();

                    SetLockAndCommandTimeout(conn);

                    usingConnection(conn);
                }
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                    throw;
            }
        }

        /// <summary>
        /// Opens a <see cref="SqlCommand"/> based on an <see cref="IDbConnection"/>. 
        /// </summary>
        /// <param name="commandText">string to set as the command text</param>
        /// <param name="connection">An <see cref="IDbConnection"/> - note that the actual 
        /// connection is expected to be either a <see cref="ReliableSqlConnection"/> or a <see cref="SqlConnection"/></param>
        /// <returns>SqlCommand</returns>
        /// <exception cref="InvalidArgumentException">Thrown if the connection is not supported - only 
        /// a <see cref="ReliableSqlConnection"/> or a <see cref="SqlConnection"/> will be supported</exception>
        public static SqlCommand CreateSqlCommand(string commandText, IDbConnection connection)
        {
            SqlCommand cmd = CreateSqlCommand(connection);
            cmd.CommandText = commandText;
            return cmd;
        }

        /// <summary>
        /// Opens a <see cref="SqlCommand"/> based on an <see cref="IDbConnection"/>. Note: this will not be a reliable command,
        /// based on the current implementation. 
        /// </summary>
        /// <param name="connection">An <see cref="IDbConnection"/> - note that the actual 
        /// connection is expected to be either a <see cref="ReliableSqlConnection"/> or a <see cref="SqlConnection"/></param>
        /// <returns>SqlCommand</returns>
        /// <exception cref="InvalidArgumentException">Thrown if the connection is not supported - only 
        /// a <see cref="ReliableSqlConnection"/> or a <see cref="SqlConnection"/> will be supported</exception>
        public static SqlCommand CreateSqlCommand(IDbConnection connection)
        {
            ReliableSqlConnection reliableConnection = connection as ReliableSqlConnection;
            if (reliableConnection != null)
            {
                return reliableConnection.CreateSqlCommand();
            }

            SqlConnection sqlConnection = connection as SqlConnection;
            if (sqlConnection != null)
            {
                return sqlConnection.CreateCommand();
            }
            // Else throw an error
            SqlTracer.TraceEvent(TraceEventType.Error, SqlTraceId.CoreServices, "ConnectionHelperUtils: A non-SQL connection was passed to CreateSqlCommand");
            throw new InvalidArgumentException("connection");
        }

        /// <summary>
        /// Opens an <see cref="IDbCommand"/> based on an <see cref="IDbConnection"/>. 
        /// </summary>
        public static IDbCommand CreateCommand(string commandText, IDbConnection connection)
        {
            SqlCommand cmd = CreateSqlCommand(connection);
            cmd.CommandText = commandText;
            return cmd;
        }

        /// <summary>
        /// This method provides the provides a connection string configured with the specified database name.
        /// This is also an opportunity to decrypt the connection string based on the encryption/decryption strategy.
        /// FormatException could be thrown since this routine attempts to restore the connection
        /// string if 'restoreConnectionString' is true.
        /// 
        /// Will only set DatabaseName/ApplicationName if the value is not null.
        /// </summary>
        ///         
        public static SqlConnectionStringBuilder ConfigureConnectionString(string connectionString,
                                                       string databaseName,
                                                       string applicationName,
                                                       bool restoreConnectionString = true)
        {
            if (restoreConnectionString)
            {
                // Read the connection string through the persistence layer
                connectionString = SqlConnectionStringSecurer.RestoreConnectionString(connectionString);
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

        /// <summary>
        /// Default 'builderInitializer' routine.  This disables pooling.
        /// </summary>
        /// <param name="csb"></param>
        public static void DisablePooling(SqlConnectionStringBuilder csb)
        {
            SqlArgumentValidation.CheckForNullReference(csb, "csb");

            csb.Pooling = false;
        }

        /// <summary>
        /// Optional 'initializeConnection' routine.  This sets the lock and command timeout for the connection.
        /// </summary>
        public static void SetLockAndCommandTimeout(IDbConnection conn)
        {
            SqlArgumentValidation.CheckForNullReference(conn, "conn");

            using (var ctx = AmbientSettings.CreateSettingsContext())
            {
                ctx.Settings.LockTimeoutMilliSeconds = DefaultLockTimeoutMs;
                ctx.Settings.QueryTimeoutSeconds = DefaultCommandTimeout;
                ReliableConnectionHelper.SetLockAndCommandTimeout(conn);
            }
        }

        /// <summary>
        /// Opens a SqlConnection, creates a SqlCommand and calls ExecuteNonQuery against the connection.
        /// </summary>
        /// <param name="csb">The connection string.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="initializeCommand">Optional delegate to initialize the SqlCommand before execution.  
        /// Default is ConnectionHelperUtils.SetCommandTimeout</param>
        /// <param name="catchException">delegate called when an exception has occurred.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The number of rows affected</returns>
        public static object ExecuteNonQuery(SqlConnectionStringBuilder csb,
                                             string commandText,
                                             Action<SqlCommand> initializeCommand,
                                             Predicate<Exception> catchException)
        {
            object retObject = null;
            OpenSqlConnection(csb,
                           usingConnection: (connection) =>
                           {
                               retObject = ExecuteNonQuery(connection, commandText, initializeCommand, catchException);
                           },
                           catchException: catchException);
            return retObject;
        }

        /// <summary>
        /// Creates a SqlCommand and calls ExecuteNonQuery against the connection.
        /// </summary>
        /// <param name="conn">The connection.  This must be opened.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="initializeCommand">Optional delegate to initialize the SqlCommand before execution.  
        /// Default is ConnectionHelperUtils.SetCommandTimeout</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The number of rows affected</returns>
        public static object ExecuteNonQuery(SqlConnection conn,
                                           string commandText,
                                           Action<SqlCommand> initializeCommand,
                                           Predicate<Exception> catchException)
        {
            SqlArgumentValidation.CheckForNullReference(conn, "conn");
            SqlArgumentValidation.CheckForEmptyString(commandText, "commandText");

            SqlCommand cmd = null;
            try
            {
                SqlTracer.AssertTraceEvent(conn.State == ConnectionState.Open, TraceEventType.Warning, SqlTraceId.CoreServices, 
                    "connection passed to ExecuteNonQuery should be open.");

                cmd = conn.CreateCommand();
                if (initializeCommand == null)
                    initializeCommand = SetCommandTimeout;
                initializeCommand(cmd);

                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;

                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                    throw;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
            return null;
        }

        /// <summary>
        /// Creates a SqlCommand and calls ExecuteScalar against the connection.
        /// </summary>
        /// <param name="conn">The connection.  This must be opened.</param>
        /// <param name="commandText">The scalar T-SQL command.</param>
        /// <param name="initializeCommand">Optional delegate to initialize the SqlCommand before execution.  
        /// Default is ConnectionHelperUtils.SetCommandTimeout</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        /// <returns>The scalar result</returns>
        public static object ExecuteScalar(SqlConnection conn, 
                                           string commandText, 
                                           Action<SqlCommand> initializeCommand = null,
                                           Predicate<Exception> catchException = null)
        {
            SqlArgumentValidation.CheckForNullReference(conn, "conn");
            SqlArgumentValidation.CheckForEmptyString(commandText, "commandText");

            SqlCommand cmd = null;

            try
            {
                SqlTracer.AssertTraceEvent(conn.State == ConnectionState.Open, TraceEventType.Warning, SqlTraceId.CoreServices, 
                    "connection passed to ExecuteScalar should be open.");

                cmd = conn.CreateCommand();
                if (initializeCommand == null)
                    initializeCommand = SetCommandTimeout;
                initializeCommand(cmd);

                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                    throw;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
            return null;
        }

        /// <summary>
        /// Creates a SqlCommand and calls ExecuteReader against the connection.
        /// </summary>
        /// <param name="conn">The connection to execute the reader on.  This must be opened. Note that the 
        /// connection is expected to be either a <see cref="ReliableSqlConnection"/> or a <see cref="SqlConnection"/>
        /// </param>
        /// <param name="commandText">The command text to execute</param>
        /// <param name="readResult">A delegate used to read from the reader</param>
        /// <param name="initializeCommand">Optional delegate to initialize the SqlCommand object</param>
        /// <param name="catchException">Optional exception handling.  Pass back 'true' to handle the 
        /// exception, 'false' to throw. If Null is passed in then all exceptions are thrown.</param>
        public static void ExecuteReader(IDbConnection conn,
                                           string commandText,
                                           Action<SqlDataReader> readResult,
                                           Action<SqlCommand> initializeCommand = null,
                                           Predicate<Exception> catchException = null)
        {
            SqlArgumentValidation.CheckForNullReference(conn, "conn");
            SqlArgumentValidation.CheckForEmptyString(commandText, "commandText");
            SqlArgumentValidation.CheckForNullReference(readResult, "readResult");

            SqlCommand cmd = null;
            try
            {
                SqlTracer.AssertTraceEvent(conn.State == ConnectionState.Open, TraceEventType.Warning, SqlTraceId.CoreServices, 
                    "connection passed to ExecuteReader should be open.");

                cmd = CreateSqlCommand(conn);
                if (initializeCommand == null)
                    initializeCommand = SetCommandTimeout;
                initializeCommand(cmd);

                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    readResult(reader);
                }
            }
            catch (Exception ex)
            {
                if (catchException == null || !catchException(ex))
                    throw;
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        /// <summary>
        /// optional 'initializeCommand' routine.  This initializes the SqlCommand
        /// </summary>
        /// <param name="cmd"></param>
        public static void SetCommandTimeout(IDbCommand cmd)
        {
            SqlArgumentValidation.CheckForNullReference(cmd, "cmd");

            using (var ctx = AmbientSettings.CreateSettingsContext())
            {
                ctx.Settings.LockTimeoutMilliSeconds = DefaultLockTimeoutMs;
                ctx.Settings.QueryTimeoutSeconds = DefaultCommandTimeout;
                ReliableConnectionHelper.SetCommandTimeout(cmd);
            }
        }

        /// <summary>
        /// Looks at the server defined by the connection string and determines the next, unique database name.
        /// </summary>
        public static string CreateUniqueDatabaseName(SqlConnectionStringBuilder builder, string proposedName)
        {
            const string selectStatement = @"SELECT name FROM sys.sysdatabases WHERE name LIKE @dbname";

            string newName = proposedName;
            if (string.IsNullOrEmpty(newName))
            {
                newName = "Database";
            }

            SqlTracer.TraceEvent(TraceEventType.Verbose, SqlTraceId.VSShell, String.Format(CultureInfo.CurrentCulture, "SqlConnectionHelper::CreateUniqueDatabaseName(): looking for unique database name like {0} on data source {1}", proposedName, builder.DataSource));

            HashSet<string> allDbs = new HashSet<string>();
            if (string.IsNullOrEmpty(builder.AttachDBFilename))
            {
                OpenSqlConnection(builder,
                    usingConnection: (connection) =>
                    {

                        using (SqlCommand versionCommand = connection.CreateCommand())
                        {
                            versionCommand.Parameters.AddWithValue("@dbname", string.Concat(newName, "%"));
                            versionCommand.CommandText = selectStatement;
                            versionCommand.CommandType = CommandType.Text;

                            using (SqlDataReader reader = versionCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    // do lookups in the hash table with upper-cased names.  This will work irrespective of DB collation
                                    allDbs.Add(reader[0].ToString().ToUpperInvariant());
                                }
                            }
                        }
                    },
                catchException: (ex) =>
                {
                    SqlTracer.TraceException(SqlTraceId.TSqlModel, ex);
                    return ReliableConnectionHelper.StandardExceptionHandler(ex); // handled
                });
            }


            string namePrefixWithCasing = newName;

            // do lookups in the hash table with upper-cased names.  This will work correctly irrespective of DB collation.  
            string namePrefixUpperCased = newName.ToUpperInvariant();
            string newNameUpperCased = namePrefixUpperCased;
            uint index = 0;

            while (allDbs.Contains(newNameUpperCased))
            {
                if (index == UInt32.MaxValue)
                {
                    // We've exhausted all of the uint values & still haven't found a unique name.  
                    // add an additional underscore, or we'll go into an infinite loop. (I'd be shocked if this ever happens)
                    namePrefixWithCasing = namePrefixWithCasing + "_";
                    namePrefixUpperCased = namePrefixUpperCased + "_";
                    index = 0;
                }
                ++index;
                newName = string.Concat(namePrefixWithCasing, "_", index.ToString(CultureInfo.InvariantCulture));
                newNameUpperCased = string.Concat(namePrefixUpperCased, "_", index.ToString(CultureInfo.InvariantCulture));
            }

            SqlTracer.AssertTraceEvent(allDbs.Contains(newName.ToUpperInvariant()) == false, TraceEventType.Warning, SqlTraceId.CoreServices, 
                "Unexpected allDbs hash map contains string.  Why is this?");

            SqlTracer.TraceEvent(TraceEventType.Verbose, SqlTraceId.VSShell, String.Format(CultureInfo.CurrentCulture, "SqlConnectionHelper::CreateUniqueDatabaseName(): found unique database name {0}", newName));

            return newName;
        }

        public static void CreateDatabase(SqlConnectionStringBuilder connection, SqlPlatforms serverType, string databaseName, string databaseFilePath = null, string databaseLogPath = null)
        {
            using (IDbConnection conn = ReliableConnectionHelper.OpenConnection(connection, useRetry: serverType.IsCloud()))
            {
                Sql110ScriptGenerator generator = new Sql110ScriptGenerator();
                CreateDatabaseStatement createDb = new CreateDatabaseStatement();

                if (serverType.IsCloud())
                {
                    // Azure can handle CREATE DATABASE [???]
                    createDb.DatabaseName = ScriptDomUtils.CreateIdentifier(databaseName, QuoteType.SquareBracket);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(databaseFilePath))
                        databaseFilePath = ReliableConnectionHelper.GetDefaultDatabaseFilePath(conn);
                    if (string.IsNullOrWhiteSpace(databaseLogPath))
                        databaseLogPath = ReliableConnectionHelper.GetDefaultDatabaseLogPath(conn);

                    // SQL Server must include the FILENAME statement in case the database name contains
                    // invalid characters[???]
                    createDb.DatabaseName = ScriptDomUtils.CreateIdentifier(databaseName, QuoteType.SquareBracket);
                    string safeFileName = SqlFileUtils.ReplaceIllegalNtfsCharacters(databaseName);
                    FileGroupDefinition primaryFg = new FileGroupDefinition();
                    createDb.FileGroups.Add(primaryFg);

                    FileDeclaration defaultFile = CreateFileDeclaration(databaseFilePath, safeFileName, logFile: false);
                    defaultFile.IsPrimary = true;
                    primaryFg.FileDeclarations.Add(defaultFile);

                    createDb.LogOn.Add(CreateFileDeclaration(databaseLogPath, safeFileName, logFile: true));
                }

                TSqlScript newScript = new TSqlScript();
                TSqlBatch batch = new TSqlBatch();
                batch.Statements.Add(createDb);
                newScript.Batches.Add(batch);

                string createCmd;
                generator.GenerateScript(newScript, out createCmd);

                // Create the database
                using (var cmd = conn.CreateCommand())
                {
                    ReliableConnectionHelper.SetCommandTimeout(cmd);
                    cmd.CommandText = createCmd;
                    cmd.ExecuteNonQuery();
                }

                // Set containment to partial for Sql11+
                string safeName = databaseName.Replace("]", "]]");
                if (serverType != SqlPlatforms.Sql90 &&
                    serverType != SqlPlatforms.Sql100 &&
                    IsConfiguredForContainedDatabase(connection.ToString()))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        const string partialContainmentCmd =
                            @"ALTER DATABASE [{0}] SET CONTAINMENT = PARTIAL";

                        cmd.CommandText = string.Format(CultureInfo.InvariantCulture, partialContainmentCmd, safeName);
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }

                if (!serverType.IsCloud())
                {
                    // Disable ServiceBroker so our database has the same default settings as SSMS
                    using (var cmd = conn.CreateCommand())
                    {
                        const string disableBrokerCmd =
                            @"ALTER DATABASE [{0}] SET DISABLE_BROKER";

                        cmd.CommandText = string.Format(CultureInfo.InvariantCulture, disableBrokerCmd, safeName);
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }

                    // Now set various options so they conform to best practices
                    using (var cmd = conn.CreateCommand())
                    {
                        const string defaultSettingsCmd =
                            @"ALTER DATABASE [{0}]
                                SET ANSI_NULLS ON,
                                    ANSI_PADDING ON,
                                    ANSI_WARNINGS ON,
                                    ARITHABORT ON,
                                    CONCAT_NULL_YIELDS_NULL ON,
                                    NUMERIC_ROUNDABORT OFF,
                                    QUOTED_IDENTIFIER ON,
                                    ANSI_NULL_DEFAULT ON,
                                    CURSOR_DEFAULT LOCAL,
                                    RECOVERY FULL,
                                    CURSOR_CLOSE_ON_COMMIT OFF,
                                    AUTO_CREATE_STATISTICS ON,
                                    AUTO_SHRINK OFF,
                                    AUTO_UPDATE_STATISTICS ON,
                                    RECURSIVE_TRIGGERS OFF,
                                    AUTO_CLOSE OFF,
                                    PAGE_VERIFY CHECKSUM
                                WITH ROLLBACK IMMEDIATE;";

                        cmd.CommandText = string.Format(CultureInfo.InvariantCulture, defaultSettingsCmd, safeName);
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the sql 11 or greater + server is configured for contained databases.
        /// </summary>
        public static bool IsConfiguredForContainedDatabase(string connectionString)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = ContainedDbQuery;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        string isContained = reader[RunValue].ToString();
                        if (!string.IsNullOrWhiteSpace(isContained))
                        {
                            int runValue;
                            if (int.TryParse(isContained, out runValue) == true &&
                                runValue == 1)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SqlTracer.TraceException(SqlTraceId.VSShell, ex);
            }

            return false;
        }

        public static FileDeclaration CreateFileDeclaration(string path, string namePrefix, bool logFile)
        {
            string logicalName;
            string fileFullPath;
            if (logFile)
            {
                logicalName = string.Format(CultureInfo.InvariantCulture,
                                            TSqlSnippets.DefaultLogLogicalNameFormat,
                                            namePrefix);
                fileFullPath = Path.Combine(path,
                                            string.Format(CultureInfo.InvariantCulture,
                                                          "{0}{1}",
                                                          namePrefix,
                                                          SqlFileExtensions.LogFileFileExtension));
            }
            else
            {
                logicalName = namePrefix;
                fileFullPath = Path.Combine(path,
                                            string.Format(CultureInfo.InvariantCulture,
                                                          "{0}{1}",
                                                          namePrefix,
                                                          SqlFileExtensions.MdfFileExtension));
            }
            
            return ScriptDomUtils.CreateFileDeclaration(logicalName, fileFullPath);
        }

        /// <summary>
        /// Returns the default path for the database specified in the connection string.
        /// </summary>
        public static bool GetDatabaseFilePaths(SqlConnectionStringBuilder csb, out string[] paths)
        {
            paths = null;
            try
            {
                using (SqlConnection connection = OpenSqlConnection(csb))
                {
                    const string sqlCommand = @"use [{0}];
  SELECT [filename] FROM dbo.sysfiles";

                    string unsafeName = !string.IsNullOrWhiteSpace(connection.Database) ? connection.Database : "master";
                    string safeName = unsafeName.Replace("]", "]]");
                    string finalCmd = string.Format(CultureInfo.InvariantCulture, sqlCommand, safeName);

                    List<string> temp = new List<string>();
                    Action<SqlDataReader> rdrAction = rdr =>
                    {
                        rdr.Read();
                        temp.Add(rdr["filename"].ToString());
                    };
                    ExecuteReader(connection, finalCmd, rdrAction);

                    paths = temp.ToArray();
                    return (paths.Length != 0);
                }
            }
            catch (Exception ex)
            {
                if (ReliableConnectionHelper.StandardExceptionHandler(ex))
                    return true;
                throw;
            }
        }

        /// <summary>
        /// Returns a displayable value for the connection string.
        /// </summary>
        /// <param name="csb"></param>
        /// <returns></returns>
        public static string StringForDisplay(this SqlConnectionStringBuilder csb)
        {
            csb = ReliableConnectionHelper.TrimConnectionStringBuilder(csb);
            csb.Remove("Application Name");
            csb.Remove("Packet Size");
            csb.Remove("MultipleActiveResultSets");
            csb.Remove("Pooling");
            if (!string.IsNullOrWhiteSpace(csb.Password))
            {
                csb.Password = "********";
            }
            return csb.ConnectionString;
        }

        /// <summary>
        /// Determine if the specified DataSource is a localdb
        /// </summary>
        internal static bool IsLocalDb(SqlConnectionStringBuilder csb)
        {
            return IsLocalDb(csb.DataSource);
        }

        /// <summary>
        /// Determine if the specified serverName is a localdb
        /// </summary>
        internal static bool IsLocalDb(string serverName)
        {
            if (!string.IsNullOrEmpty(serverName))
            {
                // If the url starts with '(localdb)' then it is a localdb URL.
                return (serverName.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase) ||
                         serverName.Contains(@"\\.\pipe\LOCALDB#"));
            }
            return false;
        }

        /// <summary>
        /// Attempts to delete the database identified by the connection string and database name.  This
        /// routine will throw on failure.
        /// </summary>
        internal static void DropDatabase(IDbConnection connection, string databaseName, bool isCloud, bool closeConnections, bool deleteBackupHistory)
        {
            IDbCommand command = null;
            string safeName = databaseName.Replace("]", "]]");
            SqlConnectionStringBuilder bldr = null;
            string origAccessLevel = null;
            try
            {
                bldr = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = string.Empty };
                string finalCmd;
                if (!isCloud)
                {
                    if (closeConnections)
                    {
                        command = CreateCommand(SqlConnectionHelperScripts.GetAccessLevel, connection);
                        command.Parameters.Add(new SqlParameter("@dbname", databaseName));
                        var obj = command.ExecuteScalar();
                        if (obj == null)
                        {
                            throw new InvalidOperationException(
                                string.Format(CultureInfo.CurrentCulture,
                                              SqlCommonResource.CouldNotRetrieveAccessLevel,
                                              databaseName));
                        }
                        origAccessLevel = obj.ToString();
                        command.Dispose();
                        command = null;

                        finalCmd = string.Format(CultureInfo.InvariantCulture,
                                                 SqlConnectionHelperScripts.CloseConnectionsCommand, safeName);
                        command = CreateCommand(finalCmd, connection);
                        command.CommandType = CommandType.Text;

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch(Exception ex)
                        {
                            // Eat this exception.  If the database is readonly we won't be
                            // able to close connections.  This is ok.
                            SqlTracer.TraceEvent(TraceEventType.Verbose, SqlTraceId.CoreServices, 
                                "Caught exception '{0}' when closing connections. This is OK as we assume DB is in readonly state", ex.Message);
                        }

                        command.Dispose();
                        command = null;
                    }
                    if (deleteBackupHistory)
                    {
                        command = CreateCommand(SqlConnectionHelperScripts.DeleteBackupHistoryCommand, connection);
                        command.Parameters.Add(new SqlParameter("@dbname", databaseName));
                        command.ExecuteNonQuery();
                        command.Dispose();
                        command = null;
                    }
                }

                finalCmd = string.Format(CultureInfo.InvariantCulture, SqlConnectionHelperScripts.DropCommand, safeName);
                command = CreateCommand(finalCmd, connection);
                ReliableConnectionHelper.SetCommandTimeout(command);
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
                command.Dispose();
                command = null;
            }
            catch (Exception ex)
            {
                if (ReliableConnectionHelper.StandardExceptionHandler(ex) ||
                    (ex is InvalidOperationException) ||
                    (ex is SqlServerManagementException) ||
                    (ex is SystemException))
                {
                    // Cleanup the failed delete
                    // Note: this will fail against SQL Auth connections with non-persisted passwords as
                    // we copied the bldr from a connection's connection string. This is a limitation for now
                    // since we had to switch to the master DB to do this operation
                    ResetAccessLevel(origAccessLevel, bldr, safeName);
                }

                if (command != null)
                    command.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Sets the access level for the database in the connection string builder.
        /// </summary>
        internal static void ResetAccessLevel(string origAccessLevel, SqlConnectionStringBuilder bldr, string safeName)
        {
            if (!string.IsNullOrWhiteSpace(origAccessLevel) &&
                bldr != null &&
                safeName != null)
            {
                const string restoreAccessLevelCommand =
                    @"ALTER DATABASE [{0}] SET {1} WITH ROLLBACK IMMEDIATE";

                SqlCommand command = null;
                try
                {
                    using (SqlConnection connection = OpenSqlConnection(bldr))
                    {
                        string finalCmd = string.Format(CultureInfo.InvariantCulture, restoreAccessLevelCommand, safeName, true);
                        command = new SqlCommand(finalCmd, connection);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    if (command != null)
                    {
                        command.Dispose();
                    }

                    if (!ReliableConnectionHelper.StandardExceptionHandler(ex))
                    {
                        throw;
                    }
                }
            }
        }
    }
}

#endif