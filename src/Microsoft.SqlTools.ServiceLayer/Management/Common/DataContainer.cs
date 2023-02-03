//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// CDataContainer
    /// </summary>
    public class CDataContainer : IDisposable
    {
        #region Nested types

        public enum ServerType
        {
            SQL,
            OLAP, //This type is used only for non-express sku
            SQLCE,
            UNKNOWN
        }

        #endregion

        #region Fields

        private ServerConnection serverConnection;
        private Server m_server;   
        protected XmlDocument m_doc;
        private XmlDocument originalDocument;
        private SqlOlapConnectionInfoBase connectionInfo;
        private SqlConnectionInfoWithConnection sqlCiWithConnection;
        private bool ownConnection = true;
        private IManagedConnection managedConnection;
        protected string serverName;

        //This member is used for non-express sku only
        protected string olapServerName;

        protected string sqlceFilename;

        private ServerType serverType = ServerType.UNKNOWN;

        private Hashtable m_hashTable;

        private string objectNameKey = "object-name-9524b5c1-e996-4119-a433-b5b947985566";
        private string objectSchemaKey = "object-schema-ccaf2efe-8fa3-4f62-be79-62ef3cbe7390";

        private SqlSmoObject sqlDialogSubject;

        private int sqlServerVersion = 0;


        #endregion

        #region Public properties

        /// <summary>
        /// gets/sets XmlDocument with parameters
        /// </summary>
        public XmlDocument Document
        {
            get
            {
                return this.m_doc;
            }
            set
            {
                this.m_doc = value;

                if (value != null)
                {
                    this.originalDocument = (XmlDocument) value.Clone();
                }
                else
                {
                    this.originalDocument = null;
                }
            }
        }


        /// <summary>
        /// returns the Hashtable that can be used to store generic information
        /// </summary>
        public Hashtable HashTable
        {
            get
            {
                m_hashTable ??= new Hashtable();
                return m_hashTable;
            }
        }

        /// <summary>
        /// gets/sets SMO server object
        /// </summary>
        public Server Server
        {
            get
            {
                return m_server;
            }
            set
            {
                m_server = value;
            }
        }


        /// <summary>
        /// connection info that should be used by the dialogs
        /// </summary>
        public SqlOlapConnectionInfoBase ConnectionInfo
        {
            get
            {
                // update the database name in the serverconnection object to set the correct database context when connected to Azure
                var conn = this.connectionInfo as SqlConnectionInfoWithConnection;

                // Don't update the database name if this is a Gen3 connection since Gen3 supports USE from the server connection.
                if (conn != null &&
                    conn.ServerConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase &&
                    !(conn.ServerConnection.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse &&
                    conn.ServerConnection.ProductVersion.Major >= 12))
                {
                    if (this.RelevantDatabaseName != null)
                    {
                        IComparer<string> dbNamesComparer = new ServerComparer(conn.ServerConnection, "master");
                        if (dbNamesComparer.Compare(this.RelevantDatabaseName, conn.DatabaseName) != 0)
                        {
                            ServerConnection databaseConnection = conn.ServerConnection.GetDatabaseConnection(this.RelevantDatabaseName, true, conn.AccessToken);
                            ((SqlConnectionInfoWithConnection)this.connectionInfo).ServerConnection = databaseConnection;
                        }
                    }
                }
                return this.connectionInfo;
            }
        }

        /// <summary>
        /// returns SMO server connection object constructed off the connectionInfo.
        /// This method cannot work until ConnectionInfo property has been set
        /// </summary>
        public ServerConnection ServerConnection
        {
            get
            {
                if (this.serverConnection == null)
                {
                    if (this.serverType != ServerType.SQL)
                    {
                        System.Diagnostics.Debug.Assert(false, "CDataContainer.ServerConnection can be used only for SQL connection");

                        throw new InvalidOperationException();
                    }

                    if (this.connectionInfo == null)
                    {
                        System.Diagnostics.Debug.Assert(false, "CDataContainer.ServerConnection can be used only after ConnectionInfo property has been set");

                        throw new InvalidOperationException();
                    }

                    if (this.sqlCiWithConnection != null)
                    {
                        this.serverConnection = this.sqlCiWithConnection.ServerConnection;
                    }
                    else
                    {
                        SqlConnectionInfo sci = this.connectionInfo as SqlConnectionInfo;
                        System.Diagnostics.Debug.Assert(sci != null, "CDataContainer.ServerConnection: connection info MUST be SqlConnectionInfo");
                        this.serverConnection = new ServerConnection(sci);
                    }
                }

                System.Diagnostics.Debug.Assert(this.serverConnection != null);
                return this.serverConnection;
            }
        }

        /// <summary>
        /// returns SMO server connection object constructed off the connectionInfo.
        /// This method cannot work until ConnectionInfo property has been set
        /// </summary>
        public SqlConnectionInfoWithConnection SqlInfoWithConnection
        {
            get
            {
                if (this.serverConnection == null)
                {
                    if (this.serverType != ServerType.SQL)
                    {
                        System.Diagnostics.Debug.Assert(false, "CDataContainer.ServerConnection can be used only for SQL connection");

                        throw new InvalidOperationException();
                    }

                    if (this.connectionInfo == null)
                    {
                        System.Diagnostics.Debug.Assert(false, "CDataContainer.ServerConnection can be used only after ConnectionInfo property has been set");

                        throw new InvalidOperationException();
                    }


                    if (this.sqlCiWithConnection != null)
                    {
                        this.serverConnection = this.sqlCiWithConnection.ServerConnection;
                    }
                    else
                    {
                        SqlConnectionInfo sci = this.connectionInfo as SqlConnectionInfo;
                        System.Diagnostics.Debug.Assert(sci != null, "CDataContainer.ServerConnection: connection info MUST be SqlConnectionInfo");
                        this.serverConnection = new ServerConnection(sci);
                    }
                }

                System.Diagnostics.Debug.Assert(this.serverConnection != null);
                return this.sqlCiWithConnection;
            }
        }

        public string ServerName
        {
            get
            {
                return this.serverName;
            }
            set
            {
                this.serverName = value;
            }
        }

        public ServerType ContainerServerType
        {
            get
            {
                return this.serverType;
            }
            set
            {
                this.serverType = value;
            }
        }

        public string SqlCeFileName
        {
            get
            {
                return this.sqlceFilename;
            }
            set
            {
                this.sqlceFilename = value;
            }
        }

        //This member is used for non-express sku only
        public string OlapServerName
        {
            get
            {
                return this.olapServerName;
            }
            set
            {
                this.olapServerName = value;
            }
        }

        /// <summary>
        /// Whether we are creating a new object
        /// </summary>
        public bool IsNewObject
        {
            get
            {
                string itemType = this.GetDocumentPropertyString("itemtype");
                return (itemType.Length != 0);
            }
        }

        /// <summary>
        /// The URN to the parent of the object we are creating/modifying
        /// </summary>
        public string ParentUrn
        {
            get
            {
                string result = String.Empty;
                string documentUrn = this.GetDocumentPropertyString("urn");

                if (this.IsNewObject)
                {
                    result = documentUrn;
                }
                else
                {
                    Urn urn = new Urn(documentUrn);
                    result = urn.Parent.ToString();
                }

                return result;
            }
        }

        /// <summary>
        /// The URN to the object we are creating/modifying
        /// </summary>
        public string ObjectUrn
        {
            get
            {
                string result = string.Empty;

                if (this.IsNewObject)
                {
                    string objectName = this.ObjectName;
                    string objectSchema = this.ObjectSchema;

                    if (0 == objectName.Length)
                    {
                        throw new InvalidOperationException("object name is not known, so URN for object can't be formed");
                    }

                    if (0 == objectSchema.Length)
                    {
                        result = String.Format(CultureInfo.InvariantCulture,
                            "{0}/{1}[@Name='{2}']",
                            this.ParentUrn,
                            this.ObjectType,
                            Urn.EscapeString(objectName));
                    }
                    else
                    {
                        result = String.Format(CultureInfo.InvariantCulture,
                            "{0}/{1}[@Schema='{2}' and @Name='{3}']",
                            this.ParentUrn,
                            this.ObjectType,
                            Urn.EscapeString(objectSchema),
                            Urn.EscapeString(objectName));
                    }
                }
                else
                {
                    result = this.GetDocumentPropertyString("urn");
                }

                return result;
            }
        }

        /// <summary>
        /// The name of the object we are modifying
        /// </summary>
        public string ObjectName
        {
            get
            {
                return this.GetDocumentPropertyString(objectNameKey);
            }

            set
            {
                this.SetDocumentPropertyValue(objectNameKey, value);
            }
        }

        /// <summary>
        /// The schema of the object we are modifying
        /// </summary>
        public string ObjectSchema
        {
            get
            {
                return this.GetDocumentPropertyString(objectSchemaKey);
            }

            set
            {
                this.SetDocumentPropertyValue(objectSchemaKey, value);
            }
        }

        /// <summary>
        /// The type of the object we are creating (as it appears in URNs)
        /// </summary>
        public string ObjectType
        {
            get
            {
                // note that the itemtype property is only set for new objects

                string result = String.Empty;
                string itemtype = this.GetDocumentPropertyString("itemtype");

                // if this is not a new object
                if (0 == itemtype.Length)
                {
                    string documentUrn = this.GetDocumentPropertyString("urn");
                    Urn urn = new Urn(documentUrn);

                    result = urn.Type;
                }
                else
                {
                    result = itemtype;
                }

                return result;
            }
        }

        /// <summary>
        /// The SQL SMO object that is the subject of the dialog.
        /// </summary>
        public SqlSmoObject SqlDialogSubject
        {
            get
            {
                SqlSmoObject result = null;

                if (this.sqlDialogSubject != null)
                {
                    result = this.sqlDialogSubject;
                }
                else
                {
                    result = this.Server.GetSmoObject(this.ObjectUrn);
                }

                return result;
            }

            set
            {
                this.sqlDialogSubject = value;
            }
        }

        /// <summary>
        /// Whether the logged in user is a system administrator
        /// </summary>
        public bool LoggedInUserIsSysadmin
        {
            get
            {
                bool result = false;

                System.Diagnostics.Debug.Assert(this.Server != null, "SMO Server object is null!");
                System.Diagnostics.Debug.Assert(this.Server.ConnectionContext != null, "SMO Server Connection object is null!");

                if (this.Server != null && this.Server.ConnectionContext != null)
                {
                    result = this.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);
                }

                return result;
            }
        }

        /// <summary>
        /// Get the name of the Database that contains (or is) the subject of the dialog.
        /// If no there is no relevant database, then an empty string is returned.
        /// </summary>
        public string RelevantDatabaseName
        {
            get
            {
                string result = String.Empty;
                string urnText = this.GetDocumentPropertyString("urn");

                System.Diagnostics.Debug.Assert(urnText.Length != 0, "couldn't get relevant URN");

                if (urnText.Length != 0)
                {
                    Urn urn = new Urn(urnText);

                    while ((urn != null) && (urn.Type != "Database"))
                    {
                        urn = urn.Parent;
                    }

                    if ((urn != null) && (urn.Type == "Database"))
                    {
                        result = urn.GetAttribute("Name");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The server major version number
        /// </summary>
        public int SqlServerVersion
        {
            get
            {
                if (this.sqlServerVersion == 0)
                {
                    this.sqlServerVersion = 9;

                    System.Diagnostics.Debug.Assert(this.ConnectionInfo != null, "ConnectionInfo is null!");
                    System.Diagnostics.Debug.Assert(ServerType.SQL == this.ContainerServerType, "unexpected server type");

                    if ((this.ConnectionInfo != null) && (ServerType.SQL == this.ContainerServerType))
                    {
                        Enumerator enumerator = new Enumerator();
                        Urn urn = "Server/Information";
                        string[] fields = new string[] { "VersionMajor" };
                        DataTable dataTable = enumerator.Process(this.ConnectionInfo, new Request(urn, fields));

                        if (dataTable.Rows.Count != 0)
                        {
                            this.sqlServerVersion = (int)dataTable.Rows[0][0];
                        }
                    }
                }

                return this.sqlServerVersion;
            }

        }

        #endregion

        #region Constructors, finalizer

        public CDataContainer()
        {
        }

        /// <summary>
        /// contructs the object and initializes its SQL ConnectionInfo and ServerConnection properties
        /// using the specified connection info containing live connection. 
        /// </summary>
        /// <param name="ciObj">connection info containing live connection</param>
        public CDataContainer(object ciObj, bool ownConnection)
        {
            SqlConnectionInfoWithConnection ci = (SqlConnectionInfoWithConnection)ciObj;
            if (ci == null)
            {
                System.Diagnostics.Debug.Assert(false, "CDataContainer.CDataContainer(SqlConnectionInfoWithConnection): specified connection info is null");

                throw new ArgumentNullException("ci");
            }
            ApplyConnectionInfo(ci, ownConnection);
        }

        /// <summary>
        /// contructs the object and initializes its SQL ConnectionInfo and ServerConnection properties
        /// using the specified connection info containing live connection. 
        /// 
        /// in addition creates a server of the given server type
        /// </summary>
        /// <param name="ci">connection info containing live connection</param>
        public CDataContainer(ServerType serverType, object ciObj, bool ownConnection)
        {
            SqlConnectionInfoWithConnection ci = (SqlConnectionInfoWithConnection)ciObj;
            if (ci == null)
            {
                System.Diagnostics.Debug.Assert(false, "CDataContainer.CDataContainer(SqlConnectionInfoWithConnection): specified connection info is null");

                throw new ArgumentNullException("ci");
            }

            this.serverType = serverType;
            ApplyConnectionInfo(ci, ownConnection);

            if (serverType == ServerType.SQL)
            {
                //NOTE: ServerConnection property will constuct the object if needed
                    m_server = new Server(ServerConnection);
            }
            else
            {
                    throw new ArgumentException(SR.UnknownServerType(serverType.ToString()));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverType">Server type</param>
        /// <param name="serverName">Server name</param>
        /// <param name="trusted">true if connection is trused. If true user name and password are ignored</param>
        /// <param name="userName">User name for not trusted connections</param>
        /// <param name="password">Password for not trusted connections</param>
        /// <param name="xmlParameters">XML string with parameters</param>
        public CDataContainer(ServerType serverType, string serverName, bool trusted, string userName, SecureString password, string databaseName, string xmlParameters, string azureAccountToken = null)
        {
            this.serverType = serverType;
            this.serverName = serverName;

            if (serverType == ServerType.SQL)
            {
                // does some extra initialization
                ApplyConnectionInfo(GetTempSqlConnectionInfoWithConnection(serverName, trusted, userName, password, databaseName, azureAccountToken), true);

                // NOTE: ServerConnection property will constuct the object if needed
                m_server = new Server(ServerConnection);
            }          
            else
            {
                throw new ArgumentException(SR.UnknownServerType(serverType.ToString()));
            }

            if (xmlParameters != null)
            {
                this.Document = GenerateXmlDocumentFromString(xmlParameters);
            }

            if (ServerType.SQL == serverType)
            {
                this.InitializeObjectNameAndSchema();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataContainer">Data container</param>
        /// <param name="xmlParameters">XML string with parameters</param>
        public CDataContainer(CDataContainer dataContainer, string xmlParameters)
        {
            //BUGBUG - should we be reusing same SqlConnectionInfoWithConnection if it is available?

            System.Diagnostics.Debug.Assert(dataContainer.Server != null, "DataContainer.Server can not be null.");
            Server = dataContainer.Server;
            this.serverName = dataContainer.serverName;
            this.serverType = dataContainer.serverType;
            this.connectionInfo = dataContainer.connectionInfo;
            this.ownConnection = dataContainer.ownConnection;

            this.sqlCiWithConnection = dataContainer.connectionInfo as SqlConnectionInfoWithConnection;
            if (this.sqlCiWithConnection != null)
            {
                //we want to be notified if it is closed
                this.sqlCiWithConnection.ConnectionClosed += new EventHandler(OnSqlConnectionClosed);
            }

            if (this.connectionInfo is SqlConnectionInfo)
            {
                System.Diagnostics.Debug.Assert(this.sqlCiWithConnection != null, "CDataContainer.ConnectionInfo setter: for SQL connection info you MUST use SqlConnectionInfoWithConnection derived class!");
            }

            if (xmlParameters != null)
            {
                XmlDocument doc = GenerateXmlDocumentFromString(xmlParameters);
                this.Init(doc);
            }
        }

        ~CDataContainer()
        {
            Dispose(false);
        }

        #endregion

        #region Public virtual methods

        /// <summary>
        /// Initialization routine that is a convience fuction for clients with the data in a string
        /// </summary>
        /// <param name="xmlText">The string that contains the xml data</param>
        public virtual void Init(string xmlText)
        {
            XmlDocument xmlDoc = GenerateXmlDocumentFromString(xmlText);

            this.Init(xmlDoc);
        }

        /// <summary>
        /// Overload of basic Init which takes a IServiceProvider and initializes 
        /// what it can of the container with elements provided by IServcieProvider
        /// 
        /// Today this is only the IManagedProvider if available but this function
        /// could be modified to init other things provided by the ServiceProvider
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="site"></param>
        public virtual void Init(XmlDocument doc, IServiceProvider site)
        {

            if (site != null)
            {
                Trace.TraceInformation("CDataContainer.Init has non-null IServiceProvider");
                //see if service provider supports IManagedConnection interface from the object explorer

                //NOTE: we're trying to forcefully set connection information on the data container.
                //If this code doesn't execute, then dc.Init call below will result in CDataContainer
                //initializing its ConnectionInfo member with a new object contructed off the parameters
                //in the XML doc [server name, user name etc]
                IManagedConnection managedConnection =
                    site.GetService(typeof (IManagedConnection)) as IManagedConnection;
                if (managedConnection != null)
                {
                    Trace.TraceInformation("CDataContainer.Init has non-null IManagedConnection");
                    this.SetManagedConnection(managedConnection);
                }
            }

            this.Document = doc;
            LoadData();

            // finish the initialization
            this.Init(doc);
        }


        /// <summary>
        /// main initialization method - the object is unusable until this method is called
        /// NOTE: it will ensure the ConnectionInfo and ServerConnetion objects are constructed
        /// for the appropriate server types
        /// </summary>
        /// <param name="doc"></param>
        public virtual void Init(XmlDocument doc)
        {
            // First, we read the data from XML by calling LoadData
            this.Document = doc;

            LoadData();

            // Second, create the rignt server and connection objects
            if (this.serverType == ServerType.SQL)
            {
                // ensure that we have a valid ConnectionInfo
                if (ConnectionInfo == null)
                {
                    throw new InvalidOperationException();
                }

                // NOTE: ServerConnection property will constuct the object if needed
                m_server ??= new Server(ServerConnection);
            }
            else if (this.serverType == ServerType.SQLCE)
            {
                // do nothing; originally we were only distinguishing between two
                // types of servers (OLAP/SQL); as a result for SQLCE we were 
                // executing the same codepath as for OLAP server which was
                // resulting in an exception;
            }
        }

        /// <summary>
        /// loads data into internal members from the XML document and detects the server type
        /// [SQL, OLAP etc] based on the info in the XML doc
        /// </summary>
        public virtual void LoadData()
        {
            STParameters param;
            bool bStatus;

            param = new STParameters();

            param.SetDocument(m_doc);

            // This is an ugly way to distinguish between different server types
            // Maybe we should pass server type as one of the parameters?
            bStatus = param.GetParam("servername", ref this.serverName);

            if (!bStatus || this.serverName.Length == 0)
            {
                {
                    bStatus = param.GetParam("database", ref this.sqlceFilename);
				    if (bStatus && !string.IsNullOrEmpty(this.sqlceFilename))
                    {
                        this.serverType = ServerType.SQLCE;
                    }
				    else if (this.sqlCiWithConnection != null)
                            {
                                this.serverType = ServerType.SQL;
                            }
                            else
                            {
                                this.serverType = ServerType.UNKNOWN;
                            }
                        }
                    }
            else
            {
                // OK, let's see if <servertype> was specified in the parameters. It it was, use
                // it to double check that it is SQL
                string specifiedServerType = "";
                bStatus = param.GetParam("servertype", ref specifiedServerType);
                if (bStatus)
                {
                    if (specifiedServerType != null && "sql" != specifiedServerType.ToLowerInvariant())
                    {
                        this.serverType = ServerType.UNKNOWN; // we know only about 3 types, and 2 of them were excluded by if branch above
                    }
                    else
                    {
                        this.serverType = ServerType.SQL;
                    }
                }
                else
                {
                    this.serverType = ServerType.SQL;
                }
            }

            // Ensure there is no password in the XML document
            string temp = string.Empty;
            if (param.GetParam("password", ref temp))
            {
                temp = null;
                System.Diagnostics.Debug.Assert(false, "Plaintext password found in XML document!  This must be fixed!");

                throw new SecurityException();
            }

            if (ServerType.SQL == this.serverType)
            {
                this.InitializeObjectNameAndSchema();
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// we need to store it as context from the OE
        /// </summary>
        /// <param name="managedConnection"></param>
        internal void SetManagedConnection(IManagedConnection managedConnection)
        {
            System.Diagnostics.Debug.Assert(this.managedConnection == null, "CDataContainer.SetManagedConnection: overwriting the previous value");
            this.managedConnection = managedConnection;

            ApplyConnectionInfo(managedConnection.Connection, true);//it will do some extra initialization
        }

        /// <summary>
        /// Get the named property value from the XML document
        /// </summary>
        /// <param name="propertyName">The name of the property to get</param>
        /// <returns>The property value</returns>
        public object GetDocumentPropertyValue(string propertyName)
        {
            object result = null;
            STParameters param = new STParameters(this.Document);

            param.GetBaseParam(propertyName, ref result);

            return result;
        }

        /// <summary>
        /// Get the named property value from the XML document
        /// </summary>
        /// <param name="propertyName">The name of the property to get</param>
        /// <returns>The property value</returns>
        public string GetDocumentPropertyString(string propertyName)
        {
            object result = GetDocumentPropertyValue(propertyName);
            result ??= string.Empty;

            return (string)result;
        }

        /// <summary>
        /// Set the named property value in the XML document
        /// </summary>
        /// <param name="propertyName">The name of the property to set</param>
        /// <param name="propertyValue">The property value</param>
        public void SetDocumentPropertyValue(string propertyName, string propertyValue)
        {
            STParameters param = new STParameters(this.Document);

            param.SetParam(propertyName, propertyValue);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Reset the data container to its state from just after it was last initialized or reset
        /// </summary>
        internal void Reset()
        {
            if (this.originalDocument != null)
            {
                this.Init(this.originalDocument);
            }

            if (this.m_hashTable != null)
            {
                this.m_hashTable = new Hashtable();
            }
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Get the name and schema (if applicable) for the object we are referring to
        /// </summary>
        private void InitializeObjectNameAndSchema()
        {
            System.Diagnostics.Debug.Assert(ServerType.SQL == this.serverType, "This method only valid for SQL Servers");

            string documentUrn = this.GetDocumentPropertyString("urn");
            if (documentUrn.Length != 0)
            {
                Urn urn = new Urn(documentUrn);
                string name = urn.GetAttribute("Name");
                string schema = urn.GetAttribute("Schema");

                if ((name != null) && (name.Length != 0))
                {
                    this.ObjectName = name;
                }

                if ((schema != null) && (schema.Length != 0))
                {
                    this.ObjectSchema = schema;
                }
            }
        }

        /// <summary>
        /// returns SqlConnectionInfoWithConnection object constructed using our internal vars
        /// </summary>
        /// <returns></returns>
        private SqlConnectionInfoWithConnection GetTempSqlConnectionInfoWithConnection(
            string serverName,
            bool trusted,
            string userName,
            SecureString password,
            string databaseName,
            string azureAccountToken)
        {
            System.Diagnostics.Debug.Assert(this.serverType == ServerType.SQL, "GetTempSqlConnectionInfoWithConnection should only be called for SQL Server type");

            SqlConnectionInfoWithConnection tempCI = new SqlConnectionInfoWithConnection(serverName);
            tempCI.SingleConnection = false;
            tempCI.Pooled = false;
            //BUGBUG - set the right application name?
            if (trusted)
            {
                tempCI.UseIntegratedSecurity = true;
            }
            else
            {
                tempCI.UseIntegratedSecurity = false;
                tempCI.UserName = userName;
                tempCI.SecurePassword = password;
            }

            tempCI.DatabaseName = databaseName;

            return tempCI;
        }


        /// <summary>
        /// our handler of sqlCiWithConnection.ConnectionClosed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSqlConnectionClosed(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// stores specified connection info and performs some extra initialization steps
        /// that can only be done after we have the connection information
        /// </summary>
        /// <param name="ci"></param>
        private void ApplyConnectionInfo(SqlOlapConnectionInfoBase ci, bool ownConnection)
        {
            System.Diagnostics.Debug.Assert(this.connectionInfo == null, "CDataContainer.ApplyConnectionInfo: overwriting non-null connection info!");
            System.Diagnostics.Debug.Assert(ci != null, "CDataContainer.ApplyConnectionInfo: ci is null!");

            this.connectionInfo = ci;
            this.ownConnection = ownConnection;

            // cache the cast value. It is OK that it is null for non SQL types
            this.sqlCiWithConnection = ci as SqlConnectionInfoWithConnection;

            if (this.sqlCiWithConnection != null)
            {
                // we want to be notified if it is closed
                this.sqlCiWithConnection.ConnectionClosed += new EventHandler(OnSqlConnectionClosed);
            }
        }

        private static bool MustRethrow(Exception exception)
        {
            bool result = false;

            switch (exception.GetType().Name)
            {
                case "ExecutionEngineException":
                case "OutOfMemoryException":
                case "AccessViolationException":
                case "BadImageFormatException":
                case "InvalidProgramException":

                    result = true;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Generates an XmlDocument from a string, avoiding exploits available through
        /// DTDs
        /// </summary>
        /// <param name="sourceXml"></param>
        /// <returns></returns>
        private XmlDocument GenerateXmlDocumentFromString(string sourceXml)
        {
            if (sourceXml == null)
            {
                throw new ArgumentNullException("sourceXml");
            }
            if (sourceXml.Length == 0)
            {
                throw new ArgumentException("sourceXml");
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                {
                    //  Writes the xml to the memory stream
                    streamWriter.Write(sourceXml);
                    streamWriter.Flush();

                    //  Resets the stream to the beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    //  Creates the XML reader from the stream 
                    //  and moves it to the correct node
                    XmlReader xmlReader = XmlReader.Create(memoryStream);
                    xmlReader.MoveToContent();

                    // generate the xml document
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.PreserveWhitespace = true;
                    xmlDocument.LoadXml(xmlReader.ReadOuterXml());

                    return xmlDocument;
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// MUST be called, as we'll be closing SQL connection inside this call
        /// </summary>
        private void Dispose(bool disposing)
        {
            try
            {
                //take care of live SQL connection
                if (this.sqlCiWithConnection != null)
                {
                    this.sqlCiWithConnection.ConnectionClosed -= new EventHandler(OnSqlConnectionClosed);

                    if (disposing)
                    {
                        //if we have the managed connection interface, then use it to disconnect.
                        //Otherwise, Dispose on SqlConnectionInfoWithConnection should disconnect
                        if (this.managedConnection != null)
                        {
                            //in this case we have gotten sqlCiWithConnection as this.managedConnection.Connection
                            if (this.ownConnection)
                            {
                                this.managedConnection.Close();
                            }
                            this.managedConnection = null;
                        }
                        else
                        {
                            if (this.ownConnection)
                            {
                                this.sqlCiWithConnection.Dispose();//internally will decide whether to disconnect or not
                            }
                        }
                        this.sqlCiWithConnection = null;
                    }
                    else
                    {
                        this.managedConnection = null;
                        this.sqlCiWithConnection = null;
                    }
                }
                else if (this.managedConnection != null)
                {
                    if (disposing && this.ownConnection)
                    {
                        this.managedConnection.Close();
                    }
                    this.managedConnection = null;
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        
        /// <summary>
        /// Create a data container object
        /// </summary>
        /// <param name="connInfo">connection info</param>
        /// <param name="databaseExists">flag indicating whether to create taskhelper for existing database or not</param>
        internal static CDataContainer CreateDataContainer(
            ConnectionInfo connInfo,            
            bool databaseExists = false,
            XmlDocument? containerDoc = null)
        {
            containerDoc ??= CreateDataContainerDocument(connInfo, databaseExists);

            var serverConnection = ConnectionService.OpenServerConnection(connInfo, "DataContainer");

            var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
            connectionInfoWithConnection.ServerConnection = serverConnection;
            CDataContainer dataContainer = new CDataContainer(ServerType.SQL, connectionInfoWithConnection, true);
            dataContainer.Init(containerDoc);

            return dataContainer;
        }

        internal static System.Security.SecureString BuildSecureStringFromPassword(string password) 
        {
            var passwordSecureString = new System.Security.SecureString();
            if (password != null) 
            {
                foreach (char c in password) 
                {
                    passwordSecureString.AppendChar(c);
                }
            }
            return passwordSecureString;
        }

        /// <summary>
        /// Create data container document
        /// </summary>
        /// <param name="connInfo">connection info</param>
        /// <param name="databaseExists">flag indicating whether to create document for existing database or not</param>
        /// <returns></returns>
        internal static XmlDocument CreateDataContainerDocument(ConnectionInfo connInfo, bool objectExists, string? itemType = null)
        {
            string xml = string.Empty;

            if (!objectExists)
            {
                xml =
                string.Format(@"<?xml version=""1.0""?>
                <formdescription><params>
                <servername>{0}</servername>
                <connectionmoniker>{0} (SQLServer, user = {1})</connectionmoniker>
                <servertype>sql</servertype>
                <urn>Server[@Name='{0}']</urn>
                <itemtype>{2}</itemtype>
                {3}                
                </params></formdescription> ",
                connInfo.ConnectionDetails.ServerName.ToUpper(),
                connInfo.ConnectionDetails.UserName,
                itemType ?? "Database",
                !string.IsNullOrEmpty(itemType) ? "<database>" + connInfo.ConnectionDetails.DatabaseName + "</database>" : string.Empty);
            }
            else
            {
                xml =
                string.Format(@"<?xml version=""1.0""?>
                <formdescription><params>
                <servername>{0}</servername>
                <connectionmoniker>{0} (SQLServer, user = {1})</connectionmoniker>
                <servertype>sql</servertype>
                <urn>Server[@Name='{0}']</urn>
                <database>{2}</database>
                </params></formdescription> ",
                connInfo.ConnectionDetails.ServerName.ToUpper(),
                connInfo.ConnectionDetails.UserName,
                connInfo.ConnectionDetails.DatabaseName);
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            return xmlDoc;
        }
    }
}
