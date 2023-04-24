//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// AppRoleGeneral - main app role page
    /// </summary>
    internal class AppRolePrototype
    {
        #region Members

        /// <summary>
        /// data container member that contains data specific information like
        /// connection infor, SMO server object or an AMO server object as well
        /// as a hash table where one can manipulate custom data
        /// </summary>
        private CDataContainer dataContainer = null;

        private bool exists;
        private AppRolePrototypeData currentState;
        private AppRolePrototypeData originalState;

        #endregion

        #region Trace support
        private const string componentName = "AppRoleGeneral";

        public string ComponentName
        {
            get
            {
                return componentName;
            }
        }
        #endregion

        #region Constants - urn fields, etc...
        private const string ownerField = "Owner";
        private const string defaultSchemaField = "DefaultSchema";
        private const string schemaNameField = "Name";
        private const string schemaOwnerField = "Owner";
        private const string memberNameField = "Name";
        private const string memberUrnField = "Urn";
        #endregion

        #region Non-UI variables

        // info extracted from context
        private string databaseName;
        #endregion


        #region Properties: CreateNew/Properties mode
        public string Name
        {
            get
            {
                return this.currentState.AppRoleName;
            }
            set
            {
                this.currentState.AppRoleName = value;
            }
        }

        public string DefaultSchema
        {
            get
            {
                return this.currentState.DefaultSchema;
            }
            set
            {
                this.currentState.DefaultSchema = value;
            }
        }

        public string[] Schemas
        {
            get
            {
                return this.currentState.Schemas;
            }
        }

        public string[] SchemasOwned
        {
            get
            {
                return this.currentState.SchemasOwned;
            }
            set
            {
                this.currentState.SchemasOwned = value;
            }
        }

        public string Password
        {
            get
            {
                return this.currentState.Password.ToString();
            }
            set
            {
                this.currentState.Password = value;
            }
        }

        public bool IsYukonOrLater
        {
            get
            {
                return this.currentState.IsYukonOrLater;
            }
        }

        public Dictionary<string, string> ExtendedProperties
        {
            get
            {
                return this.currentState.ExtendedProperties;
            }
            set
            {
                this.currentState.ExtendedProperties = value;
            }
        }
        #endregion

        #region Constructors / Dispose
        public AppRolePrototype(CDataContainer context, string database)
        {
            this.exists = false;
            this.dataContainer = context;
            this.currentState = new AppRolePrototypeData(context, database);
            this.originalState = (AppRolePrototypeData)this.currentState.Clone();
        }

        /// <summary>
        /// AppRoleData for creating a new app role
        /// </summary>
        public AppRolePrototype(CDataContainer context, string database, AppRoleInfo roleInfo)
        {
            this.exists = false;
            this.dataContainer = context;
            this.currentState = new AppRolePrototypeData(context, database);
            this.originalState = (AppRolePrototypeData)this.currentState.Clone();

            this.ApplyInfoToPrototype(roleInfo);
        }

        /// <summary>
        /// AppRoleData for editing an existing app role
        /// </summary>
        public AppRolePrototype(CDataContainer context, string database, ApplicationRole role)
        {
            this.exists = true;
            this.dataContainer = context;
            this.currentState = new AppRolePrototypeData(context, database, role);
            this.originalState = (AppRolePrototypeData)this.currentState.Clone();
        }

        #endregion

        #region Implementation: SendDataToServer()
        /// <summary>
        /// SendDataToServer
        ///
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        public void SendDataToServer()
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.databaseName), "databaseName is empty");

            Microsoft.SqlServer.Management.Smo.Server srv = this.dataContainer.Server;
            System.Diagnostics.Debug.Assert(srv != null, "server object is null");

            Database db = srv.Databases[this.databaseName];
            System.Diagnostics.Debug.Assert(db != null, "database object is null");

            if (this.exists) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.Name), "approleName is empty");

                ApplicationRole approle = db.ApplicationRoles[this.Name];
                System.Diagnostics.Debug.Assert(approle != null, "approle object is null");

                bool alterRequired = false;

                if (this.IsYukonOrLater && this.originalState.DefaultSchema != this.currentState.DefaultSchema)
                {
                    approle.DefaultSchema = this.currentState.DefaultSchema;
                    alterRequired = true;
                }

                if (this.originalState.Password != this.currentState.Password)
                {
                    approle.ChangePassword(this.Password);
                    alterRequired = true;
                }

                if (alterRequired == true)
                {
                    approle.Alter();
                }

                SendToServerSchemaOwnershipChanges(db, approle);
                SendToServerExtendedPropertiesChange();
            }
            else // not in properties mode -> create role
            {
                ApplicationRole approle = new ApplicationRole(db, this.Name);
                if (this.IsYukonOrLater && this.currentState.DefaultSchema.Length > 0)
                {
                    approle.DefaultSchema = this.currentState.DefaultSchema;
                }

                approle.Create(this.Password);

                SendToServerSchemaOwnershipChanges(db, approle);
                SendToServerExtendedPropertiesChange();
            }

        }
        #endregion

        #region Schemas - general operations with ...
        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void SendToServerSchemaOwnershipChanges(Database db, ApplicationRole approle)
        {
            if (this.IsYukonOrLater)
            {
                foreach (string schemaName in this.Schemas)
                {
                    bool currentlyOwned = this.currentState.SchemasOwned.Contains(schemaName);

                    if (this.exists)
                    {
                        bool wasOwned = this.originalState.SchemasOwned.Contains(schemaName);

                        if (currentlyOwned != wasOwned)
                        {
                            if (currentlyOwned == true)
                            {
                                Schema schema = db.Schemas[schemaName];
                                schema.Owner = approle.Name;
                                schema.Alter();
                            }
                            else
                            {
                                /* we cannot not renounce ownership
                            Schema schema = db.Schemas[name];
                            schema.Owner = null;
                            schema.Alter();
                            */
                            }
                        }
                    }
                    else
                    {
                        if (currentlyOwned == true)
                        {
                            Schema schema = db.Schemas[schemaName];
                            schema.Owner = approle.Name;
                            schema.Alter();
                        }
                    }
                }
            }
        }
        #endregion

        private void SendToServerExtendedPropertiesChange()
        {
            // add or alter the extended properties
            foreach (var item in this.ExtendedProperties)
            {
                if (this.originalState.ExtendedProperties.ContainsKey(item.Key))
                {
                    // alter the existing extended property
                    ExtendedProperty ep = this.originalState.ApplicationRole.ExtendedProperties[item.Key];
                    ep.Value = item.Value;
                    ep.Alter();
                }
                else
                {
                    // create the extended property
                    ExtendedProperty ep = new ExtendedProperty(this.originalState.ApplicationRole, item.Key, item.Value);
                    ep.Create();
                }
            }

            // remove the extended properties that are not in the current list
            foreach (var item in this.originalState.ExtendedProperties)
            {
                if (!this.ExtendedProperties.ContainsKey(item.Key))
                {
                    ExtendedProperty ep = this.originalState.ApplicationRole.ExtendedProperties[item.Key];
                    ep.Drop();
                }
            }
        }

        public void ApplyInfoToPrototype(AppRoleInfo roleInfo)
        {
            this.Name = roleInfo.Name;
            this.DefaultSchema = roleInfo.DefaultSchema;
            this.SchemasOwned = roleInfo.SchemasOwned.ToArray();
            this.ExtendedProperties = roleInfo.ExtendedProperties.Select(ep => new KeyValuePair<string, string>(ep.Name, ep.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private class AppRolePrototypeData : ICloneable
        {
            #region data members
            private string appRoleName = string.Empty;
            private string defaultSchema = String.Empty;
            private string password = String.Empty;
            private bool initialized = false;
            private List<string> schemaNames = null;
            private Dictionary<string, string> dictSchemas = null;
            private Dictionary<string, string> dictExtendedProperties = null;
            private ApplicationRole role = null;
            private Server server = null;
            private string database = string.Empty;
            private CDataContainer context = null;
            private bool isYukonOrLater = false;
            #endregion

            #region Properties

            // General properties


            public string AppRoleName
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.appRoleName;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.appRoleName = value;
                }
            }

            public ApplicationRole ApplicationRole
            {
                get
                {
                    return this.role;
                }
            }

            public string DefaultSchema
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.defaultSchema;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.defaultSchema = value;
                }
            }
            public string[] SchemasOwned
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }
                    return this.dictSchemas.Keys.Where(s => dictSchemas[s] == this.appRoleName).ToArray();
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    foreach (string schema in value)
                    {
                        // cannot renounce ownership
                        this.dictSchemas[schema] = this.AppRoleName;
                    }
                }
            }

            public string[] Schemas
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.schemaNames.ToArray();
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.schemaNames = value.ToList();
                }
            }

            public Dictionary<string, string> ExtendedProperties
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }
                    return this.dictExtendedProperties;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.dictExtendedProperties = value;
                }
            }

            public bool Exists
            {
                get
                {
                    return (this.role != null);
                }
            }

            public bool IsYukonOrLater
            {
                get
                {
                    return this.isYukonOrLater;
                }
            }

            public Microsoft.SqlServer.Management.Smo.Server Server
            {
                get
                {
                    return this.server;
                }
            }

            public ApplicationRole AppRole
            {
                get
                {
                    return this.role;
                }
            }

            public string Password
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.password;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.password = value;
                }
            }

            #endregion

            /// <summary>
            /// private default constructor - used by Clone()
            /// </summary>
            private AppRolePrototypeData()
            {
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="server">The server on which we are creating a new login</param>
            public AppRolePrototypeData(CDataContainer context, string databaseName)
            {
                this.server = context.Server;
                this.database = databaseName;
                this.context = context;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
                LoadData();
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="server">The server on which we are modifying a login</param>
            /// <param name="login">The login we are modifying</param>
            public AppRolePrototypeData(CDataContainer context, string databaseName, ApplicationRole appRole)
            {
                this.server = context.Server;
                this.database = databaseName;
                this.context = context;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
                this.role = appRole;
                LoadData();
            }

            /// <summary>
            /// Create a clone of this AppRolePrototypeData object
            /// </summary>
            /// <returns>The clone AppRolePrototypeData object</returns>
            public object Clone()
            {
                AppRolePrototypeData result = new AppRolePrototypeData();
                result.appRoleName = this.appRoleName;
                result.defaultSchema = this.defaultSchema;
                result.password = this.password;
                result.initialized = this.initialized;
                result.schemaNames = new List<string>(this.schemaNames);
                result.dictSchemas = new Dictionary<string, string>(this.dictSchemas);
                result.dictExtendedProperties = new Dictionary<string, string>(this.dictExtendedProperties);
                result.role = this.role;
                result.server = this.server;
                return result;
            }

            private void LoadData()
            {
                this.initialized = true;

                if (this.Exists)
                {
                    LoadExisting();
                }
                else
                {
                    LoadNew();
                }
            }

            private void LoadExisting()
            {
                System.Diagnostics.Debug.Assert(server != null, "server is null");
                System.Diagnostics.Debug.Assert(role != null, "app role is null");
                this.appRoleName = role.Name;
                // this.defaultSchema = role.DefaultSchema; // load via query
                this.password = LoginPrototype.fakePassword;
                LoadSchemas();
                LoadDefaultSchema();
                LoadExtendProperties();
            }

            private void LoadNew()
            {
                LoadSchemas();
            }

            /// <summary>
            /// loads initial schemas from server together with information about the schema owner
            /// </summary>
            private void LoadSchemas()
            {
                if (this.isYukonOrLater)
                {
                    this.dictSchemas = new Dictionary<string, string>();
                    this.schemaNames = new List<string>();

                    Enumerator en = new Enumerator();
                    Request req = new Request();
                    req.Fields = new String[] { AppRolePrototype.schemaNameField, AppRolePrototype.schemaOwnerField };
                    req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.database) + "']/Schema";
                    req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };

                    DataTable dt = en.Process(server, req);
                    System.Diagnostics.Debug.Assert((dt != null) && (dt.Rows.Count > 0), "No rows returned from schema enumerator");

                    foreach (DataRow dr in dt.Rows)
                    {
                        string name = Convert.ToString(dr[AppRolePrototype.schemaNameField], System.Globalization.CultureInfo.InvariantCulture);
                        string owner = Convert.ToString(dr[AppRolePrototype.schemaOwnerField], System.Globalization.CultureInfo.InvariantCulture);

                        dictSchemas.Add(name, owner);
                        schemaNames.Add(name);
                    }
                }
            }

            private void LoadDefaultSchema()
            {
                if (this.isYukonOrLater)
                {
                    // get the default schema

                    System.Diagnostics.Debug.Assert(this.context.ObjectUrn.Length != 0, "object urn is empty");

                    Enumerator enumerator = new Enumerator();
                    Request request = new Request();
                    request.Urn = this.context.ObjectUrn;
                    request.Fields = new String[] { AppRolePrototype.defaultSchemaField };

                    DataTable dataTable = enumerator.Process(server, request);
                    System.Diagnostics.Debug.Assert(dataTable != null, "dataTable is null");
                    System.Diagnostics.Debug.Assert(dataTable.Rows.Count == 1, "unexpected number of rows in dataTable");

                    if (dataTable.Rows.Count == 0)
                    {
                        throw new Exception("AppRoleSR.ErrorAppRoleNotFound");
                    }

                    DataRow dataRow = dataTable.Rows[0];
                    this.defaultSchema = Convert.ToString(dataRow[AppRolePrototype.defaultSchemaField], System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            private void LoadExtendProperties()
            {
                if (this.isYukonOrLater)
                {
                    foreach (ExtendedProperty ep in this.role.ExtendedProperties)
                    {
                        this.dictExtendedProperties.Add(ep.Name, (string)ep.Value);
                    }
                }
            }
        }
    }
}
