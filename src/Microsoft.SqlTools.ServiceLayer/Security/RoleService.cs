//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
//using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Main class for Role Service functionality
    /// </summary>
    public sealed class RoleService : IDisposable
    {
        private bool disposed;

        private ConnectionService connectionService = null;

        private static readonly Lazy<RoleService> instance = new Lazy<RoleService>(() => new RoleService());

        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public RoleService()
        {
        }

        /// <summary>
        /// Disposes the scripting service and all active scripting operations.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static RoleService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                connectionService ??= ConnectionService.Instance;
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the Security Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Credential request handlers
            this.ServiceHost.SetRequestHandler(CreateCredentialRequest.Type, HandleCreateAppRoleRequest, true);
        }

        /// <summary>
        /// Handle request to create a credential
        /// </summary>
        internal async Task HandleCreateAppRoleRequest(CreateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {           
            await requestContext.SendResult(new CredentialResult()
            {
                Credential = null,
                Success = true,
                ErrorMessage =null
            });
        }

#if false

#region "App Role Helpers"

        private CDataContainer context;
        private string              serverName;
        private string              databaseName;
        private string              approleName;

        // initial values loaded from server
        private string initialDefaultSchema;
        private bool                isYukonOrLater;

        
        /// <summary>
        /// interface IPanelForm
        ///
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        public void AppRole_OnRunNow (object sender)
        {
            //base.OnRunNow(sender);

            // if (panelInitialized)
            // {
            //     try
            //     {
            //         SendDataToServer();
            //         this.ExecutionMode = ExecutionMode.Success;
            //     }
            //     catch (Exception e)
            //     {
            //         //DisplayExceptionMessage(e);

            //         this.ExecutionMode = ExecutionMode.Failure;
            //         // throw;
            //     }
            // }
        }

        private void AppRole_LoadData()
        {            
            // bStatus         = param.GetParam("servername", ref this.serverName);
            // bStatus         = param.GetParam("database", ref this.databaseName);
            // bStatus         = param.GetParam("applicationrole", ref this.approleName);
        }        

        private void AppRole_InitProp()
        {            
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.serverName), "serverName is empty");
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.databaseName), "databaseName is empty");

            //passwordChanged = false;

            if (this.IsPropertiesMode == true)
            {
                // initialize from enumerator in properties mode
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.approleName), "approleName is empty");

                //this.textBoxRoleName.Text = this.approleName;

                // get the default schema

                System.Diagnostics.Debug.Assert(this.DataContainer.ObjectUrn.Length != 0, "object urn is empty");

                Enumerator enumerator = new Enumerator();
                Request request = new Request();
                request.Urn = this.DataContainer.ObjectUrn;
                request.Fields = new String[] { AppRoleGeneral.defaultSchemaField};

                DataTable dataTable = enumerator.Process(ServerConnection, request);
                System.Diagnostics.Debug.Assert(dataTable != null, "dataTable is null");
                System.Diagnostics.Debug.Assert(dataTable.Rows.Count == 1, "unexpected number of rows in dataTable");

                if (dataTable.Rows.Count == 0)
                {
                    throw new Exception(AppRoleSR.ErrorAppRoleNotFound);
                }

                DataRow dataRow = dataTable.Rows[0];
                // this.initialDefaultSchema = Convert.ToString(dataRow[AppRoleGeneral.defaultSchemaField],System.Globalization.CultureInfo.InvariantCulture);

                // this.textBoxDefaultSchema.Text = this.initialDefaultSchema;
             
            }
            // else
            // {
            //     // initialize with empty values in create new mode
            //     this.textBoxRoleName.Text = String.Empty;
            //     this.textBoxDefaultSchema.Text = this.initialDefaultSchema;

            //     this.textBoxPasword.Text = String.Empty;
            //     this.textBoxConfirmPassword.Text = String.Empty;
            // }

            // LoadSchemas();
            // InitializeSchemasGridColumns();
            // FillSchemasGrid();

            // LoadMembership();
            // InitializeMembershipGridColumns();
            // FillMembershipGrid();

            // // dont display the membership controls - app roles dont support members
            // HideMembership();
        }

        /// <summary>
        /// SendDataToServer
        ///
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        private void AppRole_SendDataToServer()
        {            
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.databaseName), "databaseName is empty");

            Microsoft.SqlServer.Management.Smo.Server srv = this.DataContainer.Server;
            System.Diagnostics.Debug.Assert(srv!=null, "server object is null");

            Database db = srv.Databases[this.databaseName];
            System.Diagnostics.Debug.Assert(db!=null, "database object is null");

            if (this.IsPropertiesMode == true) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.approleName), "approleName is empty");

                ApplicationRole approle = db.ApplicationRoles[this.approleName];
                System.Diagnostics.Debug.Assert(approle!=null, "approle object is null");

                bool alterRequired = false;

                if (this.isYukonOrLater && _selectedDefaultSchema != this.initialDefaultSchema)
                {
                    approle.DefaultSchema = _selectedDefaultSchema;
                    alterRequired = true;
                }

                if (passwordChanged == true)
                {
                    approle.ChangePassword((string) _textBoxPaswordText);
                }

                if (alterRequired == true)
                {
                    approle.Alter();
                }

                // SendToServerSchemaOwnershipChanges(db, approle);
                // SendToServerMembershipChanges(db, approle);
            }
            else // not in properties mode -> create role
            {
                ApplicationRole approle = new ApplicationRole(db, _textBoxRoleNameText);
                if (this.isYukonOrLater && _selectedDefaultSchema.Length > 0)
                {
                    approle.DefaultSchema = _selectedDefaultSchema;
                }

                approle.Create((string) _textBoxPaswordText);

                // SendToServerSchemaOwnershipChanges(db,approle);
                // SendToServerMembershipChanges(db,approle);

                this.DataContainer.SqlDialogSubject = approle; // needed by extended properties page
            }
        }

        HybridDictionary dictSchemas = null;
        StringCollection schemaNames = null;
        /// <summary>
        /// loads initial schemas from server together with information about the schema owner
        /// </summary>
        private void AppRole_LoadSchemas()
        {
            this.dictSchemas = new HybridDictionary();
            this.schemaNames = new StringCollection();

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String[] { AppRoleGeneral.schemaNameField, AppRoleGeneral.schemaOwnerField};
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Schema";
            req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc)};

            DataTable dt = en.Process(ServerConnection, req);
            System.Diagnostics.Debug.Assert((dt != null) && (dt.Rows.Count > 0), "No rows returned from schema enumerator");

            foreach (DataRow dr in dt.Rows)
            {
                string name = Convert.ToString(dr[AppRoleGeneral.schemaNameField],System.Globalization.CultureInfo.InvariantCulture);
                string owner = Convert.ToString(dr[AppRoleGeneral.schemaOwnerField],System.Globalization.CultureInfo.InvariantCulture);

                dictSchemas.Add(name, owner);
                schemaNames.Add(name);
            }
        }

         /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void AppRole_SendToServerSchemaOwnershipChanges(Database db, ApplicationRole approle)
        {
            if (this.isYukonOrLater)
            {
                DlgGridControl grid = this.gridSchemasOwned;

                for (int i = 0; i < grid.RowsNumber; ++i)
                {
                    string name = grid.GetCellInfo(i, colSchemasOwnedSchemas).CellData.ToString();
                    object o = dictSchemas[name];

                    System.Diagnostics.Debug.Assert(o != null, "schema object is null");

                    bool currentlyOwned = IsEmbeededCheckboxChecked(grid, i, colSchemasChecked);

                    if (IsPropertiesMode == true)
                    {
                        bool wasOwned = (o.ToString() == approleName);

                        if (currentlyOwned != wasOwned)
                        {
                            if (currentlyOwned == true)
                            {
                                Schema schema = db.Schemas[name];
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
                            Schema schema = db.Schemas[name];
                            schema.Owner = approle.Name;
                            schema.Alter();
                        }
                    }
                }
            }
        }

        System.Collections.Specialized.HybridDictionary dictMembership = null;

        /// <summary>
        /// loads from server initial membership information
        /// </summary>
        private void AppRole_LoadMembership()
        {
            dictMembership = new System.Collections.Specialized.HybridDictionary();
            if (IsPropertiesMode == false)
            {
                return;
            }

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String [] {AppRoleGeneral.memberNameField, AppRoleGeneral.memberUrnField};
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/ApplicationRole[@Name='" + Urn.EscapeString(this.approleName) + "']/Member";

            try
            {
                DataTable dt = en.Process(ServerConnection,req);
                System.Diagnostics.Debug.Assert(dt!=null, "No results returned from membership query");

                foreach (DataRow dr in dt.Rows)
                {
                    string name = Convert.ToString(dr[AppRoleGeneral.memberNameField],System.Globalization.CultureInfo.InvariantCulture);
                    string urn  = Convert.ToString(dr[AppRoleGeneral.memberUrnField],System.Globalization.CultureInfo.InvariantCulture);

                    dictMembership.Add(name,urn);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void AppRole_SendToServerMembershipChanges(Database db, ApplicationRole approle)
        {
            DlgGridControl grid = this.gridRoleMembership;

            if (IsPropertiesMode == true)
            {
                // members to add
                for (int i=0; i<grid.RowsNumber; ++i)
                {
                    string name = grid.GetCellInfo(i, colMembershipRoleMembers).CellData.ToString();
                    bool nameExistedInitially = dictMembership.Contains(name);

                    if (nameExistedInitially == false)
                    {
                        // need SMO for: role.Members.Add();
                    }
                }
                // members to drop
                foreach (DictionaryEntry de in dictMembership)
                {
                    if (FoundInMembershipGrid(de.Key.ToString(), de.Value.ToString()) == false)
                    {
                        // need SMO for: role.Members.Remove();
                    }
                }
            }
            else
            {
                // add only
                for (int i=0; i<grid.RowsNumber; ++i)
                {
                    string name = grid.GetCellInfo(i, colMembershipRoleMembers).CellData.ToString();
                    // need SMO for: role.Members.Add();
                }
            }
        }
#endregion

#region "DB Role"

        private bool IsPropertiesMode
        {
            get
            {
                return(dbroleName!=null) && (dbroleName.Trim().Length != 0);
            }
        }

        // info extracted from context
        // private string              serverName;
        // private string              databaseName;
        private string              dbroleName;
        private string              dbroleUrn;

        // initial values loaded from server
        private string              initialOwner;

        private string              ownerName       = String.Empty;
        private string              roleName        = String.Empty;
        private HybridDictionary    schemaOwnership = null;
        private HybridDictionary    roleMembers     = null;


        private void DbRole_LoadData()
        {
            // bStatus         = param.GetParam("servername", ref this.serverName);
            // bStatus         = param.GetParam("database", ref this.databaseName);

            // bStatus         = param.GetParam("role", ref this.dbroleName);
            // bStatus         = param.GetParam("urn", ref this.dbroleUrn);
        }

        private void InitProp()
        {            
            System.Diagnostics.Debug.Assert(this.serverName!=null);
            System.Diagnostics.Debug.Assert((this.databaseName!=null) && (this.databaseName.Trim().Length!=0));

            // LoadSchemas();
            // LoadMembership();

            if (this.IsPropertiesMode == true)
            {
                // initialize from enumerator in properties mode
                System.Diagnostics.Debug.Assert(this.dbroleName!=null);
                System.Diagnostics.Debug.Assert(this.dbroleName.Trim().Length !=0);
                System.Diagnostics.Debug.Assert(this.dbroleUrn!=null);
                System.Diagnostics.Debug.Assert(this.dbroleUrn.Trim().Length != 0);

                this.textBoxDbRoleName.Text = this.dbroleName;

                Enumerator en = new Enumerator();
                Request req = new Request();
                req.Fields = new String [] {DatabaseRoleGeneral.ownerField};

                if ((this.dbroleUrn!=null) && (this.dbroleUrn.Trim().Length != 0))
                {
                    req.Urn = this.dbroleUrn;
                }
                else
                {
                    req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Role[@Name='" + Urn.EscapeString(this.dbroleName) + "]";
                }

                DataTable dt = en.Process(ServerConnection,req);
                System.Diagnostics.Debug.Assert(dt!=null);
                System.Diagnostics.Debug.Assert(dt.Rows.Count==1);

                if (dt.Rows.Count==0)
                {
                    throw new Exception(DatabaseRoleSR.ErrorDbRoleNotFound);
                }

                DataRow dr = dt.Rows[0];
                this.initialOwner = Convert.ToString(dr[DatabaseRoleGeneral.ownerField],System.Globalization.CultureInfo.InvariantCulture);
                this.textBoxOwner.Text = this.initialOwner;
            }
        }

        private void DbRole_SendDataToServer()
        {
            System.Diagnostics.Debug.Assert(this.databaseName != null && this.databaseName.Trim().Length != 0, "database name is empty");
            System.Diagnostics.Debug.Assert(this.DataContainer.Server != null, "server is null");

            Database database = this.DataContainer.Server.Databases[this.databaseName];
            System.Diagnostics.Debug.Assert(database!= null, "database is null");

            DatabaseRole role = null;

            if (this.IsPropertiesMode == true) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(this.dbroleName != null && this.dbroleName.Trim().Length != 0, "role name is empty");

                role = database.Roles[this.dbroleName];
                System.Diagnostics.Debug.Assert(role != null, "role is null");

                if (0 != String.Compare(this.ownerName, this.initialOwner, StringComparison.Ordinal))
                {
                    role.Owner = this.ownerName;
                    role.Alter();
                }
            }
            else // not in properties mode -> create role
            {
                role = new DatabaseRole(database, this.roleName);
                if (this.ownerName.Length != 0)
                {
                    role.Owner = this.ownerName;
                }

                role.Create();
            }

            SendToServerSchemaOwnershipChanges(database, role);
            SendToServerMembershipChanges(database, role);

            this.DataContainer.ObjectName       = role.Name;
            this.DataContainer.SqlDialogSubject = role; // needed by extended properties page
        }

        private void DbRole_LoadSchemas()
        {
            this.schemaOwnership = new HybridDictionary();

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String [] {DatabaseRoleGeneral.schemaNameField, DatabaseRoleGeneral.schemaOwnerField};
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Schema";

            DataTable dt = en.Process(ServerConnection,req);
            System.Diagnostics.Debug.Assert((dt != null) && (0 < dt.Rows.Count), "enumerator did not return schemas");
            System.Diagnostics.Debug.Assert(!this.IsPropertiesMode || (this.dbroleName.Length != 0), "role name is not known");

            foreach (DataRow dr in dt.Rows)
            {
                string  schemaName      = Convert.ToString(dr[DatabaseRoleGeneral.schemaNameField],System.Globalization.CultureInfo.InvariantCulture);
                string  schemaOwner     = Convert.ToString(dr[DatabaseRoleGeneral.schemaOwnerField],System.Globalization.CultureInfo.InvariantCulture);
                bool    roleOwnsSchema  = 
                    this.IsPropertiesMode &&
                    (0 == String.Compare(this.dbroleName, schemaOwner, StringComparison.Ordinal));

                this.schemaOwnership[schemaName] = new SchemaOwnership(roleOwnsSchema);
            }
        }

        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void DbRole_SendToServerSchemaOwnershipChanges(Database db, DatabaseRole dbrole)
        {
            if (9 <= this.DataContainer.Server.Information.Version.Major)
            {
                IDictionaryEnumerator enumerator = this.schemaOwnership.GetEnumerator();
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    DictionaryEntry de          = enumerator.Entry;
                    string          schemaName  = de.Key.ToString();
                    SchemaOwnership ownership   = (SchemaOwnership)de.Value;

                    // If we are creating a new role, then no schema will have been initially owned by this role.
                    // If we are modifying an existing role, we can only take ownership of roles.  (Ownership can't
                    // be renounced, it can only be positively assigned to a principal.)
                    if (ownership.currentlyOwned && !ownership.initiallyOwned)
                    {
                        Schema schema = db.Schemas[schemaName];
                        schema.Owner = dbrole.Name;
                        schema.Alter();
                    }
                }
            }
        }

        private void DbRole_LoadMembership()
        {
            this.roleMembers = new HybridDictionary();

            if (this.IsPropertiesMode)
            {
                Enumerator  enumerator  = new Enumerator();
                Urn         urn         = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                        "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member",
                                                        Urn.EscapeString(this.databaseName),
                                                        Urn.EscapeString(this.dbroleName));
                string[]    fields      = new string[] { DatabaseRoleGeneral.memberNameField};
                OrderBy[]   orderBy     = new OrderBy[] { new OrderBy(DatabaseRoleGeneral.memberNameField, OrderBy.Direction.Asc)};
                Request     request     = new Request(urn, fields, orderBy);
                DataTable   dt          = enumerator.Process(this.ServerConnection, request);

                foreach (DataRow dr in dt.Rows)
                {
                    string memberName = dr[DatabaseRoleGeneral.memberNameField].ToString();
                    this.roleMembers[memberName] = new RoleMembership(true);
                }
            }
        }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void DbRole_SendToServerMembershipChanges(Database db, DatabaseRole dbrole)
        {
            IDictionaryEnumerator enumerator = this.roleMembers.GetEnumerator();
            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                DictionaryEntry entry       = enumerator.Entry;
                string          memberName  = entry.Key.ToString();
                RoleMembership  membership  = (RoleMembership) entry.Value;

                if (!membership.initiallyAMember && membership.currentlyAMember)
                {
                    dbrole.AddMember(memberName);
                }
                else if (membership.initiallyAMember && !membership.currentlyAMember)
                {
                    dbrole.DropMember(memberName);
                }
            }
        }

#endregion
#endif
    }
}
