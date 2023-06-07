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
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.PermissionsData;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// DatabaseRoleGeneral - main app role page
    /// </summary>
    internal class DatabaseRolePrototype
    {
        #region Members

        /// <summary>
        /// data container member that contains data specific information like
        /// connection infor, SMO server object or an AMO server object as well
        /// as a hash table where one can manipulate custom data
        /// </summary>
        private CDataContainer dataContainer = null;

        private bool exists;
        private DatabaseRolePrototypeData currentState;
        private DatabaseRolePrototypeData originalState;
        private SecurablePermissions[] securablePermissions = null;
        private Principal principal = null;
        #endregion

        #region Trace support
        private const string componentName = "DatabaseRoleGeneral";

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
        private const string schemaOwnerField = "Owner";
        private const string schemaNameField = "Name";
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
                return this.currentState.DatabaseRoleName;
            }
            set
            {
                this.currentState.DatabaseRoleName = value;
            }
        }

        public string Owner
        {
            get
            {
                return this.currentState.Owner;
            }
            set
            {
                this.currentState.Owner = value;
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

        public List<string> Members
        {
            get
            {
                return this.currentState.Members;
            }
            set
            {
                this.currentState.Members = value;
            }
        }

        public bool IsYukonOrLater
        {
            get
            {
                return this.dataContainer.Server.VersionMajor >= 9;
            }
        }

        public SecurablePermissions[] SecurablePermissions
        {
            get
            {
                return this.securablePermissions;
            }
            set
            {
                this.securablePermissions = value;
            }
        }
        #endregion

        #region Constructors / Dispose
        public DatabaseRolePrototype(CDataContainer context, string database)
        {
            this.exists = false;
            this.databaseName = database;
            this.dataContainer = context;
            this.currentState = new DatabaseRolePrototypeData(context, database);
            this.originalState = (DatabaseRolePrototypeData)this.currentState.Clone();
            this.securablePermissions = new SecurablePermissions[0];
        }

        /// <summary>
        /// DatabaseRoleData for creating a new app role
        /// </summary>
        public DatabaseRolePrototype(CDataContainer context, string database, DatabaseRoleInfo roleInfo)
        {
            this.exists = false;
            this.databaseName = database;
            this.dataContainer = context;
            this.currentState = new DatabaseRolePrototypeData(context, database);
            this.originalState = (DatabaseRolePrototypeData)this.currentState.Clone();
            this.principal = SecurableUtils.CreatePrincipal(false, PrincipalType.DatabaseRole, null, roleInfo.Name, context, database);

            this.ApplyInfoToPrototype(roleInfo);
        }

        /// <summary>
        /// DatabaseRoleData for editing an existing app role
        /// </summary>
        public DatabaseRolePrototype(CDataContainer context, string database, DatabaseRole role)
        {
            this.exists = true;
            this.databaseName = database;
            this.dataContainer = context;
            this.currentState = new DatabaseRolePrototypeData(context, database, role);
            this.originalState = (DatabaseRolePrototypeData)this.currentState.Clone();
            this.principal = SecurableUtils.CreatePrincipal(true, PrincipalType.DatabaseRole, role, null, context, database);
            this.principal.AddExistingSecurables();
            this.securablePermissions = SecurableUtils.GetSecurablePermissions(true, PrincipalType.DatabaseRole, role, context);
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

            DatabaseRole databaseRole = null;
            if (this.exists) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.Name), "databaseRoleName is empty");

                databaseRole = db.Roles[this.Name];
                System.Diagnostics.Debug.Assert(databaseRole != null, "databaseRole object is null");

                if (0 != String.Compare(this.currentState.Owner, this.originalState.Owner, StringComparison.Ordinal))
                {
                    databaseRole.Owner = this.Owner;
                    databaseRole.Alter();
                }
            }
            else // not in properties mode -> create role
            {
                databaseRole = new DatabaseRole(db, this.Name);
                if (this.Owner.Length != 0)
                {
                    databaseRole.Owner = this.Owner;
                }
                databaseRole.Create();
            }
            SendToServerSchemaOwnershipChanges(db, databaseRole);
            SendToServerMembershipChanges(db, databaseRole);
            SendToServerExtendedPropertiesChange();
            SecurableUtils.SendToServerPermissionChanges(this.exists, this.Name, this.SecurablePermissions, this.principal, this.dataContainer, this.databaseName);
        }
        #endregion

        #region Schemas - general operations with ...
        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void SendToServerSchemaOwnershipChanges(Database db, DatabaseRole databaseRole)
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
                                schema.Owner = databaseRole.Name;
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
                            schema.Owner = databaseRole.Name;
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
                    ExtendedProperty ep = this.originalState.DatabaseRole.ExtendedProperties[item.Key];
                    ep.Value = item.Value;
                    ep.Alter();
                }
                else
                {
                    // create the extended property
                    ExtendedProperty ep = new ExtendedProperty(this.originalState.DatabaseRole, item.Key, item.Value);
                    ep.Create();
                }
            }

            // remove the extended properties that are not in the current list
            foreach (var item in this.originalState.ExtendedProperties)
            {
                if (!this.ExtendedProperties.ContainsKey(item.Key))
                {
                    ExtendedProperty ep = this.originalState.DatabaseRole.ExtendedProperties[item.Key];
                    ep.Drop();
                }
            }
        }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void SendToServerMembershipChanges(Database db, DatabaseRole dbrole)
        {
            if (!this.exists)
            {
                foreach (string member in this.Members)
                {
                    dbrole.AddMember(member);
                }
            }
            else
            {
                foreach (string member in this.Members)
                {
                    if (!this.originalState.Members.Contains(member))
                    {
                        dbrole.AddMember(member);
                    }
                }

                foreach (string member in this.originalState.Members)
                {
                    if (!this.Members.Contains(member))
                    {
                        dbrole.DropMember(member);
                    }
                }
            }
        }


        public void ApplyInfoToPrototype(DatabaseRoleInfo roleInfo)
        {
            this.Name = roleInfo.Name;
            this.Owner = roleInfo.Owner;
            this.Members = roleInfo.Members.ToList();
            this.SchemasOwned = roleInfo.OwnedSchemas.ToArray();
            this.ExtendedProperties = roleInfo.ExtendedProperties.Select(ep => new KeyValuePair<string, string>(ep.Name, ep.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            this.securablePermissions = roleInfo.SecurablePermissions;
        }

        private class DatabaseRolePrototypeData : ICloneable
        {
            #region data members
            private string databaseRoleName = string.Empty;
            private string owner = String.Empty;
            private bool initialized = false;
            private List<string> schemaNames = null;
            private Dictionary<string, string> dictSchemas = new Dictionary<string, string>();
            private Dictionary<string, string> dictExtendedProperties = new Dictionary<string, string>();
            private List<string> members = new List<string>();
            private DatabaseRole role = null;
            private Server server = null;
            private string database = string.Empty;
            private CDataContainer context = null;
            private bool isYukonOrLater = false;
            #endregion

            #region Properties

            // General properties


            public string DatabaseRoleName
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.databaseRoleName;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.databaseRoleName = value;
                }
            }

            public DatabaseRole DatabaseRole
            {
                get
                {
                    return this.role;
                }
            }

            public string Owner
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.owner;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.owner = value;
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
                    return this.dictSchemas.Keys.Where(s => dictSchemas[s] == this.databaseRoleName).ToArray();
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    foreach (string schema in value)
                    {
                        // cannot renounce ownership
                        this.dictSchemas[schema] = this.DatabaseRoleName;
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

            public List<string> Members
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }
                    return this.members;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.members = value;
                }
            }

            public bool Exists
            {
                get
                {
                    return (this.role != null);
                }
            }

            public Microsoft.SqlServer.Management.Smo.Server Server
            {
                get
                {
                    return this.server;
                }
            }

            public bool IsYukonOrLater
            {
                get
                {
                    return this.isYukonOrLater;
                }
            }

            #endregion

            /// <summary>
            /// private default constructor - used by Clone()
            /// </summary>
            private DatabaseRolePrototypeData()
            {
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="server">The server on which we are creating a new databaseRole</param>
            /// <param name="databaseName">The database on which we are creating a new databaseRole</param>
            public DatabaseRolePrototypeData(CDataContainer context, string databaseName)
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
            /// <param name="server">The server on which we are modifying a databaseRole</param>
            /// <param name="databaseName">The database on which we are modifying a databaseRole</param>
            /// <param name="databaseRole">The databaseRole we are modifying</param>
            public DatabaseRolePrototypeData(CDataContainer context, string databaseName, DatabaseRole databaseRole)
            {
                this.server = context.Server;
                this.database = databaseName;
                this.context = context;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
                this.role = databaseRole;
                LoadData();
            }

            /// <summary>
            /// Create a clone of this DatabaseRolePrototypeData object
            /// </summary>
            /// <returns>The clone DatabaseRolePrototypeData object</returns>
            public object Clone()
            {
                DatabaseRolePrototypeData result = new DatabaseRolePrototypeData();
                result.databaseRoleName = this.databaseRoleName;
                result.initialized = this.initialized;
                result.schemaNames = new List<string>(this.schemaNames);
                result.dictSchemas = new Dictionary<string, string>(this.dictSchemas);
                result.dictExtendedProperties = new Dictionary<string, string>(this.dictExtendedProperties);
                result.members = new List<string>(this.members);
                result.owner = this.owner;
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
                this.databaseRoleName = role.Name;
                this.owner = role.Owner;
                LoadSchemas();
                LoadMembership();
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
                    req.Fields = new String[] { DatabaseRolePrototype.schemaNameField, DatabaseRolePrototype.schemaOwnerField };
                    req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.database) + "']/Schema";
                    req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };

                    DataTable dt = en.Process(server.ConnectionContext, req);
                    System.Diagnostics.Debug.Assert((dt != null) && (dt.Rows.Count > 0), "No rows returned from schema enumerator");

                    foreach (DataRow dr in dt.Rows)
                    {
                        string name = Convert.ToString(dr[DatabaseRolePrototype.schemaNameField], System.Globalization.CultureInfo.InvariantCulture);
                        string owner = Convert.ToString(dr[DatabaseRolePrototype.schemaOwnerField], System.Globalization.CultureInfo.InvariantCulture);

                        dictSchemas.Add(name, owner);
                        schemaNames.Add(name);
                    }
                }
            }

            private void LoadMembership()
            {
                if (this.Exists)
                {
                    this.members = new List<string>();
                    Enumerator enumerator = new Enumerator();
                    Urn urn = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                            "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member",
                                                            Urn.EscapeString(this.database),
                                                            Urn.EscapeString(this.databaseRoleName));
                    string[] fields = new string[] { DatabaseRolePrototype.memberNameField };
                    OrderBy[] orderBy = new OrderBy[] { new OrderBy(DatabaseRolePrototype.memberNameField, OrderBy.Direction.Asc) };
                    Request request = new Request(urn, fields, orderBy);
                    DataTable dt = enumerator.Process(this.server.ConnectionContext, request);
                    foreach (DataRow dr in dt.Rows)
                    {
                        string memberName = dr[DatabaseRolePrototype.memberNameField].ToString();
                        this.members.Add(memberName);
                    }
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
