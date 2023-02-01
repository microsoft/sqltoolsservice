//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Encapsulates database roles, access and default schema
    /// </summary>
    internal class DatabaseRoles : ICloneable
    {
        private Microsoft.SqlServer.Management.Smo.Server server;

        private string              databaseName;
        private string              loginName;
        private bool                permit;
        private bool                isAccessible;
        private string              defaultSchema;
        private string              userName;
        private HybridDictionary    databaseRoles;
        private StringCollection    schemaNames = null;
        private bool                loginExists;
        private bool                roleMembershipChanged;
        private bool                guestStatus;

        private bool                initializedDatabaseAccess;
        private bool                initializedRoleMembership;

        /// <summary>
        /// The name of the database
        /// </summary>
        public string   DatabaseName
        {
            get
            {
                return databaseName;
            }
        }

        /// <summary>
        /// The default schema (user name) for the login in the database
        /// </summary>
        public string   DefaultSchema
        {
            get
            {
                if (!this.initializedDatabaseAccess)
                {
                    this.InitializeDatabaseAccess();
                }

                return defaultSchema;
            }

            set
            {
                System.Diagnostics.Debug.Assert(this.initializedDatabaseAccess, "unexpected property set for unitialized DatabaseRoles");
                defaultSchema = value;
            }
        }

        /// <summary>
        /// Whether the login has access to the database
        /// </summary>
        public bool     PermitDatabaseAccess
        {
            get
            {
                if (!this.initializedDatabaseAccess)
                {
                    this.InitializeDatabaseAccess();
                }

                return permit;
            }

            set
            {
                System.Diagnostics.Debug.Assert(this.initializedDatabaseAccess, "unexpected property set for unitialized DatabaseRoles");
                permit = value;
            }
        }

        /// <summary>
        /// Whether the database is accessible
        /// </summary>
        public bool     DatabaseIsAccessible
        {
            get
            {
                if (!this.initializedDatabaseAccess)
                {
                    this.InitializeDatabaseAccess();
                }

                return this.isAccessible;
            }
        }

        /// <summary>
        /// Guest account enables or disabled in the database
        /// </summary>
        public bool     GuestStatus
        {
            get
            {
                if (!this.initializedRoleMembership)
                {
                    this.InitializeRoleMembership();
                }
                return this.guestStatus;

            }
        }

        /// <summary>
        /// The names of all the database roles defined for the database
        /// </summary>
        public string[] DatabaseRoleNames
        {
            get
            {
                if (!this.initializedRoleMembership)
                {
                    this.InitializeRoleMembership();
                }

                SortedList  sortedRoles = new SortedList(databaseRoles, Comparer.Default);
                string[]    result      = new string[sortedRoles.Count];

                sortedRoles.Keys.CopyTo(result, 0);

                return result;
            }
        }

        /// <summary>
        /// The names of the schemas in the database sorted alphabetically
        /// </summary>
        public StringCollection SchemaNames
        {
            get
            {
                if (this.schemaNames == null)
                {
                    this.schemaNames                = new StringCollection();
                    Enumerator  enumerator          = new Enumerator();
                    Urn         urn                 = new Urn(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server/Database[@Name='{0}']/Schema", Urn.EscapeString(databaseName)));
                    string[]    fields              = new string[] { "Name"};
                    OrderBy[]   orderBy             = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc)};
                    Request     request             = new Request(urn, fields, orderBy);
                    DataTable   enumeratorResults   = enumerator.Process(this.server.ConnectionContext, request);

                    System.Diagnostics.Debug.Assert(enumeratorResults.Rows.Count != 0, "couldn't enumerate schemas in the database");

                    for (int i = 0; i < enumeratorResults.Rows.Count; ++i)
                    {
                        this.schemaNames.Add(enumeratorResults.Rows[i]["Name"].ToString());
                    }
                }

                return this.schemaNames;
            }
        }

        /// <summary>
        /// Has the user changed membership in any role?
        /// </summary>
        public bool     RoleMembershipChanged
        {
            get
            {
                return this.roleMembershipChanged;
            }
        }
        /// <summary>
        /// Whether the login already exists
        /// </summary>
        private bool    LoginExists
        {
            get
            {
                return this.loginExists;
            }
        }


        /// <summary>
        /// constructor
        /// </summary>
        private DatabaseRoles()
        {
            this.DefaultInitialize();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="server">The server we are working with</param>
        /// <param name="databaseName">The name of the database for which we are encapsulating data</param>
        public DatabaseRoles(Microsoft.SqlServer.Management.Smo.Server server, string databaseName)
        {
            this.DefaultInitialize();

            this.server         = server;
            this.databaseName   = databaseName;

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="server">The server we are working with</param>
        /// <param name="databaseName">The name of the database for which we are encapsulating data</param>
        /// <param name="loginName">The name of the login we are modifying</param>
        public DatabaseRoles(Microsoft.SqlServer.Management.Smo.Server server, string databaseName, string loginName)
        {
            this.DefaultInitialize();

            this.server             = server;
            this.loginName          = loginName;
            this.databaseName       = databaseName;
            this.loginExists        = true;

        }


        /// <summary>
        /// Gets whether the associated login is a member of a particular role
        /// </summary>
        /// <param name="databaseRoleName">The name of the database role</param>
        /// <returns>True if the login is a member of the role, false otherwise</returns>
        public bool     IsMember(string databaseRoleName)
        {
            if (!this.initializedRoleMembership)
            {
                this.InitializeRoleMembership();
            }

            System.Diagnostics.Debug.Assert(databaseRoles.Contains(databaseRoleName), "databaseRoleName is not the name of a role in the database");

            bool isPublic   = (0 == String.Compare(databaseRoleName, "public", StringComparison.Ordinal));
            bool result     = isPublic || (bool) databaseRoles[databaseRoleName];

            return result; 
        }

        /// <summary>
        /// Sets whether the associated login is a member of a particular role
        /// </summary>
        /// <param name="databaseRoleName">The name of the database role</param>
        /// <param name="isMember">Whether the login is a member of the role</param>
        public void     SetMember(string databaseRoleName, bool isMember)
        {
            System.Diagnostics.Debug.Assert(databaseRoles.Contains(databaseRoleName), "databaseRoleName is not the name of a role in the database");

            if (0 != String.Compare(databaseRoleName, "public", StringComparison.Ordinal))
            {
                databaseRoles[databaseRoleName] = isMember;
                this.roleMembershipChanged      = true;
            }
        }

        /// <summary>
        /// Create a clone of this DatabaseRoles object
        /// </summary>
        /// <returns>The clone DatabaseRoles</returns>
        public object   Clone()
        {
            DatabaseRoles result = new DatabaseRoles();

            result.server           = this.server;
            result.loginName        = this.loginName;
            result.databaseName     = this.databaseName;
            result.loginExists      = this.loginExists;
            result.permit           = this.permit;
            result.isAccessible     = this.isAccessible;
            result.defaultSchema    = this.defaultSchema;
            result.userName         = this.userName;
            result.guestStatus      = this.guestStatus;

            result.initializedDatabaseAccess    = this.initializedDatabaseAccess;
            result.initializedRoleMembership    = this.initializedRoleMembership;

            foreach (string key in this.databaseRoles.Keys)
            {
                result.databaseRoles[key] = this.databaseRoles[key];
            }

            return result;
        }

        /// <summary>
        /// Determine whether the login has access to the database
        /// </summary>
        /// <param name="userName">the name of the schema associated with the login in the database</param>
        /// <param name="hasDBAccess">whether the user has database access</param>
        /// <returns>True if there is a user associated with the login in the database, false otherwise</returns>
        private bool    GetDatabaseUserInfo(out string userName, out bool hasDBAccess, out string defaultSchema)
        {
            bool result     = false;

            userName        = String.Empty;
            hasDBAccess     = false;
            defaultSchema   = String.Empty;

            if (this.isAccessible)
            {
                try
                {
                    Request request = new Request();

                    request.Urn         = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                        "Server/Database[@Name='{0}']/User[@Login='{1}']", 
                                                        Urn.EscapeString(databaseName), 
                                                        Urn.EscapeString(this.loginName));

                    if (server.Information.Version.Major >= 9)
                    {
                        request.Fields      = new string[3] { "Name", "HasDBAccess", "DefaultSchema"};
                    }
                    else
                    {
                        request.Fields      = new string[2] { "Name", "HasDBAccess"};
                    }

                    DataTable users     = new Enumerator().Process(server.ConnectionContext, request);

                    if (0 != users.Rows.Count)
                    {
                        System.Diagnostics.Debug.Assert(1 == users.Rows.Count, "unexpected number of users for the the login");

                        result  = true;

                        userName    = Convert.ToString (users.Rows[0][0], System.Globalization.CultureInfo.InvariantCulture);
                        hasDBAccess = Convert.ToBoolean(users.Rows[0][1], System.Globalization.CultureInfo.InvariantCulture);
                        if (server.Information.Version.Major >= 9)
                        {
                            defaultSchema = Convert.ToString(users.Rows[0][2], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            defaultSchema = String.Empty;
                        }
                    }
                }
                catch (EnumeratorException ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.Message);

                    // if we got an exception determining whether the user has access,
                    // then the database is effectively inaccessible
                    this.isAccessible = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Determine whether a particular user is a member of a role
        /// </summary>
        /// <param name="roleName">The name of the role</param>
        /// <param name="userName">The name of the user</param>
        /// <returns>True if the user is a member of the role, false otherwise</returns>
        private bool    DatabaseRoleContainsUser(string roleName, string userName)
        {
            bool result = false;

            if (this.isAccessible && (0 != userName.Length))
            {
                Request request = new Request();

                request.Urn         = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                    "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member[@Name='{2}']", 
                                                    Urn.EscapeString(databaseName), 
                                                    Urn.EscapeString(roleName),
                                                    Urn.EscapeString(userName));

                request.Fields      = new string[1] { "Name"};

                DataTable members   = new Enumerator().Process(server.ConnectionContext, request);

                if (0 != members.Rows.Count)
                {
                    System.Diagnostics.Debug.Assert(1 == members.Rows.Count, "unexpected number of members for the user name");
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Initialize member variables to default values
        /// </summary>
        private void    DefaultInitialize()
        {
            this.server         = null;
            this.loginName      = String.Empty;
            this.databaseName   = String.Empty;
            this.loginExists    = false;
            this.permit         = false;
            this.isAccessible   = false;
            this.defaultSchema  = String.Empty;
            this.userName       = String.Empty;
            this.databaseRoles  = new HybridDictionary();
            this.guestStatus    = false;

            this.initializedDatabaseAccess  = false;
            this.initializedRoleMembership  = false;
            this.roleMembershipChanged      = false;
        }

        /// <summary>
        /// Determines status of the guest account in the database
        /// </summary>
        /// <param name="database"></param>
        private void    GetGuestStatus(Database database)
        {
            System.Diagnostics.Debug.Assert(database != null, "we need a valid database to determine guest access!");
            if (database != null)
            {
                User guest = database.Users["guest"];

                // if guest doesn't exist, then guest doesn't have access.
                this.guestStatus = (guest != null) ? guest.HasDBAccess : false;
            }
        }

        /// <summary>
        /// Initialize database access information
        /// </summary>
        private void    InitializeDatabaseAccess()
        {
            this.initializedDatabaseAccess = true;

            if ((server != null) && (databaseName.Length != 0))
            {
                // determine whether the database is accessible to the user
                try
                {
                    DataTable dt = new Enumerator().Process(this.server.ConnectionContext, 
                                                            new Request(new Urn(string.Format("Server/Database[@Name='{0}']", Urn.EscapeString(databaseName))), 
                                                                        new string[] { "Status" }));
                    if (dt != null &&
                        dt.Rows.Count > 0 &&
                        dt.Rows[0]["Status"] != DBNull.Value)
                    {
                        this.isAccessible = (Convert.ToInt32(dt.Rows[0]["Status"]) & (int)DatabaseStatus.Normal) != 0;
                    }
                    else
                    {
                        this.isAccessible = false;
                    }                                       
                }
                catch (Exception)
                {
                    // if we got an exception checking accessibility, the database
                    // is inaccessible to the user at the very least
                    this.isAccessible   = false;
                }

                if (this.isAccessible && (0 != this.loginName.Length))
                {
                    this.GetDatabaseUserInfo(out this.userName, out this.permit, out this.defaultSchema);
                }
            }
        }

        /// <summary>
        /// Initialize role membership information for the database
        /// </summary>
        private void    InitializeRoleMembership()
        {
            this.initializedRoleMembership = true;

            if (this.DatabaseIsAccessible)
            {
                // Get user information
                Database    database    = server.Databases[this.databaseName];

                if (database != null)
                {
                    GetGuestStatus(database);
                }
                
                string      userName    = String.Empty;
                bool        hasAccess   = false;
                string      defaultSchema = String.Empty;
                bool        userExists  = this.GetDatabaseUserInfo(out userName, out hasAccess, out defaultSchema);
                

                // get database role names
                Request     request     = new Request();

                request.Urn             = String.Format(System.Globalization.CultureInfo.InvariantCulture,"Server/Database[@Name='{0}']/Role", Urn.EscapeString(databaseName));
                request.Fields          = new string[1] { "Name"};

                DataTable   roles       = new Enumerator().Process(server.ConnectionContext, request);
                int         roleCount   = roles.Rows.Count;

                // determine which roles the user is a member of
                for (int roleIndex = 0; roleIndex < roleCount; ++roleIndex)
                {
                    string  roleName        = roles.Rows[roleIndex][0].ToString();
                    bool    isRoleMember    = (userExists) ? this.DatabaseRoleContainsUser(roleName, userName) : false;

                    this.databaseRoles.Add(roleName, isRoleMember);
                }
            }
        }

        /// <summary>
        /// gets/sets the name of the user for our login in this database
        /// </summary>
        /// <param name="databaseName"></param>
        public string UserName
        {
            get
            {
                if (!this.initializedDatabaseAccess)
                {
                    this.InitializeDatabaseAccess();
                }

                return userName;
            }
            set
            {
                userName = value;
            }
        }
    }

    /// <summary>
    /// Encapsulates server roles of which a particular login is a member
    /// </summary>
    internal class ServerRoles : ICloneable
    {
        private Microsoft.SqlServer.Management.Smo.Server              server;
        private string              loginName;
        private bool                loginExists;
        private HybridDictionary    serverRoles;

        private bool                initialized;

        /// <summary>
        /// Simple description, isMember pair - used as the value in the serverRoles map
        /// </summary>
        private class ServerRoleInfo : ICloneable
        {
            public string   roleDescription;
            public bool     isMember;

            public ServerRoleInfo(string roleDescription)
            {
                this.roleDescription    = roleDescription;
                this.isMember           = false;
            }

            public ServerRoleInfo(string roleDescription, bool isMember)
            {
                this.roleDescription    = roleDescription;
                this.isMember           = isMember;
            }

            public object Clone()
            {
                ServerRoleInfo result = new ServerRoleInfo(this.roleDescription, this.isMember);
                return result;
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        private ServerRoles()
        {
            this.server         = null;
            this.loginName      = String.Empty;
            this.loginExists    = false;
            this.serverRoles    = new HybridDictionary();
            this.initialized    = false;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="server">The server with which we are working</param>
        public ServerRoles(Microsoft.SqlServer.Management.Smo.Server server)
        {
            this.server         = server;
            this.loginName      = String.Empty;
            this.loginExists    = false;
            this.serverRoles    = new HybridDictionary();
            this.initialized    = false;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="server">The server with which we are working</param>
        /// <param name="loginName">The name of the login we are modifying</param>
        public ServerRoles(Microsoft.SqlServer.Management.Smo.Server server, string loginName)
        {
            System.Diagnostics.Debug.Assert(server.Logins[loginName] != null, "loginName does not refer to an actual login on the server");

            this.server         = server;
            this.loginName      = loginName;
            this.loginExists    = true;
            this.serverRoles    = new HybridDictionary();
            this.initialized    = false;
        }


        /// <summary>
        /// Get the names of the roles defined on the server
        /// </summary>
        /// <returns>Array of role names</returns>
        public string[] ServerRoleNames
        {
            get
            {
                if (!this.initialized)
                {
                    PopulateServerRoles();
                }

                SortedList  sortedRoles = new SortedList(serverRoles, Comparer.Default);
                string[]    result      = new string[sortedRoles.Count];

                sortedRoles.Keys.CopyTo(result, 0);

                return result;
            }
        }

        /// <summary>
        /// Get whether the associated login is a member of a particular role
        /// </summary>
        /// <param name="serverRoleName">The name of the role for which we are checking membership</param>
        /// <returns>True if the login is a member of the role, false otherwise</returns>
        public bool     IsMember(string serverRoleName)
        {
            if (!this.initialized)
            {
                this.PopulateServerRoles();
            }

            bool result = false;

            if (serverRoleName == "public")
            {
                result = true;
            }
            else if (serverRoles.Contains(serverRoleName))
            {
                result = ((ServerRoleInfo) serverRoles[serverRoleName]).isMember;
            }

            return result;
        }

        /// <summary>
        /// Set whether the associated login is a member of a particular role
        /// </summary>
        /// <param name="serverRoleName">The name of the role whose membership we wish to modify</param>
        /// <param name="isMember">True if the login should be a member of the role, false otherwise</param>
        public void     SetMember(string serverRoleName, bool isMember)
        {
            System.Diagnostics.Debug.Assert(serverRoles.Contains(serverRoleName), "serverRoleName is not the name of a role in the server");

            if (0 != String.Compare(serverRoleName, "public", StringComparison.Ordinal))
            {
                ((ServerRoleInfo) serverRoles[serverRoleName]).isMember = isMember;
            }
        }

        /// <summary>
        /// Get the role description for a particular role
        /// </summary>
        /// <param name="serverRoleName">The name of the role for which we are getting a description</param>
        /// <returns>The role description</returns>
        public string   GetDescription(string serverRoleName)
        {
            System.Diagnostics.Debug.Assert(serverRoles.Contains(serverRoleName), "serverRoleName is not the name of a role in the server");
            return((ServerRoleInfo) serverRoles[serverRoleName]).roleDescription;
        }
        /// <summary>
        /// Create a clone of this ServerRoles object
        /// </summary>
        /// <returns>The clone ServerRoles object</returns>
        public object   Clone()
        {
            ServerRoles result = new ServerRoles();

            result.server           = this.server;
            result.loginName        = this.loginName;
            result.loginExists      = this.loginExists;
            result.initialized      = this.initialized;

            foreach (string key in this.serverRoles.Keys)
            {
                ServerRoleInfo roleInfo = (ServerRoleInfo) this.serverRoles[key];
                result.serverRoles[key] = roleInfo.Clone();
            }

            return result;
        }

        /// <summary>
        /// Populate the server roles map
        /// </summary>
        private void    PopulateServerRoles()
        {
            this.initialized = true;
            serverRoles.Clear();

            try
            {
                foreach (ServerRole role in server.Roles)
                {
                    bool isRoleMember = false;

                    if (this.loginExists)
                    {
                        StringCollection roleMembers = new StringCollection();
                        roleMembers = role.EnumMemberNames();
                        isRoleMember = roleMembers.Contains(this.loginName);
                    }

                    string roleDescription = String.Empty; // role.Description;
                    this.serverRoles.Add(role.Name, new ServerRoleInfo(roleDescription, isRoleMember));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);

                // swallow the exception - this method gets called before the dialog is fully created, so there
                // is no way to display the error message
            }
        }

    }

    /// <summary>
    /// Adapter between the CreateLogin dbCommanderPages and the 
    /// </summary>
    internal class LoginPrototype
    {
        private SqlCollationSensitiveStringComparer comparer = null;

        /// <summary>
        /// string of asterisks to display in lieu of the actual password
        /// </summary>
        public static string    fakePassword    = "***************";

        /// <summary>
        /// Private class encapsulating the data that is changed by the UI.
        /// </summary>
        /// <remarks>
        /// Isolating this data allows for an easy implementation of Reset() and
        /// simplifies difference detection when committing changes to the server.
        /// </remarks>
        private class LoginPrototypeData : ICloneable
        {
            #region data members
            private string              loginName = string.Empty;
            private SqlServer.Management.Smo.LoginType loginType = SqlServer.Management.Smo.LoginType.WindowsUser;

            // General data
            private string              defaultDatabase         = "master";
            private string              defaultLanguage         = String.Empty;
            private ServerRoles         serverRoles             = null;
            private HybridDictionary    databaseRolesCollection = null;

            // Windows Authentication data
            private bool                windowsGrantAccess      = true;

            // SQL Authentication data
            private string     sqlPassword             = string.Empty;
            private string     sqlPasswordConfirm      = string.Empty;
            private string     oldPassword             = string.Empty;
            private bool showOldPassword = false;

            // yukon only
            private bool                mustChange          = true;
            private bool                isDisabled          = false;
            private bool                isLockedOut         = false;
            private bool                enforcePolicy       = true;
            private bool                enforceExpiration   = true;

            // Certificate and Asymmetric Key based
            private string              certificateName     = String.Empty;
            private string              asymmetricKeyName   = String.Empty;

            private bool                initialized         = false;
            private Login               login               = null;
            private Microsoft.SqlServer.Management.Smo.Server server;
            private static string       defaultLanguageDisplay;
            private bool                windowsAuthSupported = true;

            private StringCollection credentials = null;
            #endregion

            #region Properties

            // General properties

            public SqlServer.Management.Smo.LoginType LoginType
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.loginType;
                }

                set
                {
                    this.loginType = value;
                }
            }

            public string           LoginName
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.loginName;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.loginName = value;
                }
            }

            public string           DefaultDatabase
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.defaultDatabase;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.defaultDatabase = value;
                }
            }

            public string           DefaultLanguage
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.defaultLanguage;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.defaultLanguage = value;
                }
            }

            public ServerRoles      ServerRoles
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.serverRoles;
                }
            }

            public HybridDictionary DatabaseRolesCollection
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.databaseRolesCollection;
                }
            }            

            public bool             Exists
            {
                get
                {
                    return(this.login != null);
                }
            }

            public Microsoft.SqlServer.Management.Smo.Server Server
            {
                get
                {
                    return this.server;
                }
            }

            public Login            Login
            {
                get
                {
                    return this.login;
                }
            }

            public bool WindowsAuthSupported
            {
                get
                {
                    if (this.server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                    {
                        this.windowsAuthSupported = false;
                    }

                    return this.windowsAuthSupported;
                }
            }

            public static string    DefaultLanguageDisplay
            {
                get
                {
                    return defaultLanguageDisplay;
                }
            }

            // Windows Authentication properties

            public bool             WindowsGrantAccess
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.windowsGrantAccess;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.windowsGrantAccess = value;
                }
            }

            // SQL Authentication properties

            public string  SqlPassword
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.sqlPassword;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.sqlPassword = value;
                }
            }

            public string  SqlPasswordConfirm
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.sqlPasswordConfirm;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.sqlPasswordConfirm = value;
                }
            }

            public string  OldPassword
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.oldPassword;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.oldPassword = value;
                }
            }

            public bool ShowOldPassword
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }
                    return this.showOldPassword;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialzation");
                    this.showOldPassword = value;
                }
            }


            public bool             MustChange
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.mustChange;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    System.Diagnostics.Debug.Assert(Server.Information.Version.Major>=9);
                    this.mustChange = value;
                }
            }

            public bool             IsDisabled
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.isDisabled;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    System.Diagnostics.Debug.Assert(Server.Information.Version.Major>=9);
                    this.isDisabled = value;
                }
            }

            public bool             IsLockedOut
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.isLockedOut;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    System.Diagnostics.Debug.Assert(Server.Information.Version.Major >= 9);
                    this.isLockedOut = value;
                }
            }

            public bool             EnforcePolicy
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.enforcePolicy;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    System.Diagnostics.Debug.Assert(Server.Information.Version.Major>=9);
                    this.enforcePolicy = value;
                }
            }

            public bool             EnforceExpiration
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.enforceExpiration;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    System.Diagnostics.Debug.Assert(Server.Information.Version.Major>=9);
                    this.enforceExpiration = value;
                }
            }

            // Certificate and Asymmtric Key properties
            public string           CertificateName
            {
                get
                {
                    return this.certificateName;
                }
                set
                {
                    this.certificateName = value;
                }
            }

            public string           AsymmetricKeyName
            {
                get
                {
                    return this.asymmetricKeyName;
                }
                set
                {
                    this.asymmetricKeyName = value;
                }
            }

            public StringCollection Credentials
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }
                    return this.credentials;
                }
                set
                {
                    if (Server.Information.Version.Major < 10)
                        System.Diagnostics.Debug.Assert(value.Count <= 1, "Max one credential can be mapped to a login in server < Katmai");
                    this.credentials.Clear();
                    foreach (string str in value)
                    {
                        this.credentials.Add(str);
                    }
                }
            }

            #endregion

            static LoginPrototypeData()
            {
            //     ResourceManager resourceManager = new ResourceManager(
            //                                                          "Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
            //                                                          typeof(LoginPrototype).Assembly);

            //     defaultLanguageDisplay = resourceManager.GetString("prototype.defaultLanguage");
            }

            /// <summary>
            /// private default contructor - used by Clone()
            /// </summary>
            private LoginPrototypeData()
            {
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="server">The server on which we are creating a new login</param>
            public LoginPrototypeData(Microsoft.SqlServer.Management.Smo.Server server)
            {
                this.server = server;
                if (server.HostPlatform != HostPlatformNames.Windows)
                {
                    LoginType = SqlServer.Management.Smo.LoginType.SqlLogin;
                }
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="server">The server on which we are modifying a login</param>
            /// <param name="login">The login we are modifying</param>
            public LoginPrototypeData(Microsoft.SqlServer.Management.Smo.Server server, Login login)
            {
                this.server = server;
                this.login  = login;
            }          

            /// <summary>
            /// Create a clone of this LoginPrototypeData object
            /// </summary>
            /// <returns>The clone LoginPrototypeData object</returns>
            public object   Clone()
            {
                LoginPrototypeData result = new LoginPrototypeData();

                result.loginName        = this.loginName;   
                result.loginType        = this.loginType;

                result.defaultDatabase  = this.defaultDatabase;
                result.defaultLanguage  = this.defaultLanguage;
                result.serverRoles      = (this.serverRoles != null) ? (ServerRoles) this.serverRoles.Clone() : null;

                if (this.credentials != null)
                {
                    result.credentials = new StringCollection();
                    foreach (string credential in this.credentials)
                    {
                        result.credentials.Add(credential);
                    }
                }

                if (this.databaseRolesCollection != null)
                {
                    result.databaseRolesCollection = new HybridDictionary();

                    foreach (string databaseName in this.databaseRolesCollection.Keys)
                    {
                        DatabaseRoles roles = (DatabaseRoles) this.databaseRolesCollection[databaseName];

                        result.databaseRolesCollection[databaseName] = roles.Clone();
                    }
                }
                else
                {
                    result.databaseRolesCollection = null;
                }
         
                result.windowsGrantAccess       = this.windowsGrantAccess;  
        
                result.sqlPassword              = this.sqlPassword;             
                result.sqlPasswordConfirm       = this.sqlPasswordConfirm;   
                result.oldPassword              = this.oldPassword;
                result.showOldPassword          = this.showOldPassword;

                result.mustChange               = this.mustChange;
                result.isDisabled               = this.isDisabled;
                result.enforcePolicy            = this.enforcePolicy;
                result.enforceExpiration        = this.enforceExpiration;

                result.certificateName          = this.certificateName;
                result.asymmetricKeyName        = this.asymmetricKeyName;

                result.initialized              = this.initialized;
                result.server                   = this.server;
                result.login                    = this.login;

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
                System.Diagnostics.Debug.Assert(login  != null, "login is null");

                this.loginName  = login.Name;
                this.loginType  = login.LoginType;
            
                bool useWindowsAuthentication   = 
                    (login.LoginType == SqlServer.Management.Smo.LoginType.WindowsUser) ||
                    (login.LoginType == SqlServer.Management.Smo.LoginType.WindowsGroup);

                bool useSqlAuthentication = (login.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin);

                this.windowsGrantAccess = !login.DenyWindowsLogin;

                this.sqlPassword        = useSqlAuthentication ? LoginPrototype.fakePassword : string.Empty;
                this.sqlPasswordConfirm = useSqlAuthentication ? LoginPrototype.fakePassword : string.Empty;

                this.defaultDatabase    = login.DefaultDatabase;
                this.credentials = new StringCollection();

                if ((login.Language != null) && (login.Language.Length != 0))
                {
                    this.defaultLanguage = login.Language;
                }
                else
                {
                    this.defaultLanguage = LoginPrototypeData.DefaultLanguageDisplay;
                }

                bool isYukon = (9 <= server.Information.Version.Major);

                if (isYukon)
                {
                    if (login.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin)
                    {
                        // these properties make sense only for Yukon+ with SQL Authentication
                        this.mustChange         = login.MustChangePassword;
                        this.enforcePolicy      = login.PasswordPolicyEnforced;
                        this.enforceExpiration  = login.PasswordExpirationEnabled;
                        this.isLockedOut           = login.IsLocked;
                    }
                    else
                    {
                        this.mustChange         = false;
                        this.enforcePolicy      = false;
                        this.enforceExpiration  = false;
                        this.isDisabled         = false;
                        this.isLockedOut           = false;
                    }
                    
                    this.isDisabled = login.IsDisabled;  
                 
                    if (login.LoginType == SqlServer.Management.Smo.LoginType.Certificate)
                    {
                        this.certificateName = login.Certificate;
                    }
                    else if (login.LoginType == SqlServer.Management.Smo.LoginType.AsymmetricKey)
                    {
                        this.asymmetricKeyName = login.AsymmetricKey;
                    }
                }

                this.serverRoles                = new ServerRoles(server, login.Name);
                this.databaseRolesCollection    = new HybridDictionary();
                if (server.Information.Version.Major == 9 && !string.IsNullOrEmpty(login.Credential))
                {
                    this.credentials.Add(login.Credential);
                }
                else if (server.Information.Version.Major >= 10)
                {
                    this.credentials.Clear();
                    foreach (string str in login.EnumCredentials())
                    {
                        this.credentials.Add(str);
                    }
                }
            }

            private void LoadNew()
            {
                if (!WindowsAuthSupported)
                {
                    this.loginType = SqlServer.Management.Smo.LoginType.SqlLogin;
                }

                this.defaultLanguage            = LoginPrototypeData.DefaultLanguageDisplay;

                this.serverRoles                = new ServerRoles(server);
                this.databaseRolesCollection    = new HybridDictionary();
                this.credentials                = new StringCollection();
            }

        }

        private bool                exists;
        private string              machineName;
        private LoginPrototypeData  currentState;
        private LoginPrototypeData  originalState;

        private bool mapToCredential;
        public bool MapToCredential
        {
            set
            {
                this.mapToCredential = value;
            }
        }

        /// <summary>
        /// Whether the login already exists on the server
        /// </summary>
        public  bool            Exists
        {
            get
            {
                return this.exists;
            }
        }

        /// <summary>
        /// Whether to use NT Authentication.  (e.g. true == NT Authentication, false == SQL Authentication)
        /// </summary>
        public SqlServer.Management.Smo.LoginType LoginType
        {
            get
            {
                return this.currentState.LoginType;
            }

            set
            {
                System.Diagnostics.Debug.Assert(!this.Exists, "we shouldn't be changing the login type for existing logins");
                this.currentState.LoginType = value; 
            }
        }

        /// <summary>
        /// The Windows account name for the login (e.g. MYDOMAIN\mysuser)
        /// </summary>
        public  string          LoginName
        {
            get
            {
                if (!this.Exists && this.UseWindowsAuthentication && this.currentState.LoginName.Length != 0)
                {
                    return CUtils.CanonicalizeWindowsLoginName(this.currentState.LoginName);
                }

                return this.currentState.LoginName;
            }

            set
            {
                System.Diagnostics.Debug.Assert(!this.Exists, "shouldn't be renaming existing logins");
                
                this.UpdateDefaultSchemaAndUserNames(value);
                this.currentState.LoginName = value;
            }
        }

        /// <summary>
        /// If the login is a windows login, returns the windows login name with domain name capitalized;
        /// otherwise returns the login name as-is
        /// </summary>
        public string           CanonicalizedLoginName
        {
            get
            {
                string result = String.Empty;

                if (this.LoginName.Length != 0)
                {
                    result = this.UseWindowsAuthentication ?
                        CUtils.CanonicalizeWindowsLoginName(this.LoginName) :
                        this.LoginName;
                }

                return result;
            }
        }

        public bool WindowsAuthSupported
        {
            get
            {
                return this.currentState.WindowsAuthSupported;
            }
        }

        /// <summary>
        /// Whether the Windows account is granted server access (e.g. true == grant access, false == deny access)
        /// </summary>
        public  bool            WindowsGrantAccess
        {
            get
            {
                return this.currentState.WindowsGrantAccess;
            }

            set
            {
                this.currentState.WindowsGrantAccess = value;
            }
        }

        /// <summary>
        /// The password associated with the SQL login
        /// </summary>
        public  string          SqlPassword
        {
            get
            {
                return this.currentState.SqlPassword.ToString();
            }

            set
            {
                this.currentState.SqlPassword = value;
            }
        }

        /// <summary>
        /// The password confirmation, which should be the same as the password
        /// </summary>
        public  string          SqlPasswordConfirm
        {
            get
            {
                return this.currentState.SqlPasswordConfirm.ToString();
            }

            set
            {
                this.currentState.SqlPasswordConfirm = value;
            }
        }

        /// <summary>
        /// The login's current password
        /// </summary>
        public string           OldPassword
        {
            get
            {
                return this.currentState.OldPassword.ToString();
            }

            set
            {
                this.currentState.OldPassword = value;
            }
        }

        /// <summary>
        /// If the old password is to be considered or not
        /// </summary>
        public bool ShowOldPassword
        {
            get
            {
                return this.currentState.ShowOldPassword;
            }
            set
            {
                this.currentState.ShowOldPassword = value;
            }
        }


        /// <summary>
        /// The default database for the login
        /// </summary>
        public  string          DefaultDatabase
        {
            get
            {
                return this.currentState.DefaultDatabase;
            }

            set
            {
                this.currentState.DefaultDatabase = value;
            }
        }

        /// <summary>
        /// The default language for the login
        /// </summary>
        public  string          DefaultLanguage
        {
            get
            {
                return this.currentState.DefaultLanguage;
            }

            set
            {
                this.currentState.DefaultLanguage = value;
            }
        }

        /// <summary>
        /// Yukon+ only
        /// 
        /// Password must change at next login
        /// </summary>
        public  bool            MustChange
        {
            get
            {
                return this.currentState.MustChange;
            }
            set
            {
                this.currentState.MustChange = value;
            }
        }

        /// <summary>
        /// Yukon+ only
        /// 
        /// Is login disabled?
        /// </summary>
        public  bool            IsDisabled
        {
            get
            {
                return this.currentState.IsDisabled;
            }
            set
            {
                this.currentState.IsDisabled = value;
            }
        }

        /// <summary>
        /// Yukon+ only
        /// 
        /// Is login locked out?
        /// </summary>
        public bool             IsLockedOut
        {
            get
            {
                return this.currentState.IsLockedOut;
            }
            set
            {
                this.currentState.IsLockedOut = value;
            }
        }

        /// <summary>
        /// Yukon+ only
        /// 
        /// enforce system policy on sql login's password
        /// </summary>
        public  bool            EnforcePolicy
        {
            get
            {
                return this.currentState.EnforcePolicy;
            }
            set
            {
                this.currentState.EnforcePolicy = value;
            }
        }

        /// <summary>
        /// Yukon+ only
        /// 
        /// expiration of sql login password
        /// </summary>
        public  bool            EnforceExpiration
        {
            get
            {
                return this.currentState.EnforceExpiration;
            }
            set
            {
                this.currentState.EnforceExpiration = value;
            }
        }

        /// <summary>
        /// If this is a certificate based login, returns the name of the certificate;
        /// otherwise, returns an empty string
        /// </summary>
        public  string          CertificateName
        {
            get
            {
                return this.currentState.CertificateName;
            }
            set
            {
                this.currentState.CertificateName = value;
            }
        }

        /// <summary>
        /// If this is an asymmetric key based login, returns the name of the key;
        /// otherwise, returns an empty string
        /// </summary>
        public string           AsymmetricKeyName
        {
            get
            {
                return this.currentState.AsymmetricKeyName;
            }
            set
            {
                this.currentState.AsymmetricKeyName = value;
            }
        }

        /// <summary>
        /// The server roles collection for the login
        /// </summary>
        public  ServerRoles     ServerRoles
        {
            get
            {
                return this.currentState.ServerRoles;
            }
        }

        public StringCollection Credentials
        {
            get
            {
                return this.currentState.Credentials;
            }
            set
            {
                this.currentState.Credentials.Clear();
                foreach (string str in value)
                {
                    this.currentState.Credentials.Add(str);
                }
            }
        }

        /// <summary>
        /// Get the database roles collection for the user in a particular database
        /// </summary>
        /// <param name="databaseName">The name of the database</param>
        /// <returns>The database roles collection</returns>
        public  DatabaseRoles   GetDatabaseRoles(string databaseName)
        {
            DatabaseRoles result = null;

            if (!this.currentState.DatabaseRolesCollection.Contains(databaseName))
            {
                if (this.Exists)
                {
                    result = new DatabaseRoles(this.currentState.Server, databaseName, this.LoginName);
                }
                else
                {
                    result = new DatabaseRoles(this.currentState.Server, databaseName);
                }
                this.currentState.DatabaseRolesCollection.Add(databaseName, result);
                this.originalState.DatabaseRolesCollection.Add(databaseName, result.Clone());
            }
            else
            {
                result = (DatabaseRoles) this.currentState.DatabaseRolesCollection[databaseName];
            }

            return result;
        }

        /// <summary>
        /// Get the names of the schemas in the indicated database in alphabetical order
        /// </summary>
        /// <param name="databaseName">The name of the database</param>
        /// <returns>The names of the database's schemas</returns>
        public StringCollection GetDatabaseSchemaNames(string databaseName)
        {   
            return this.GetDatabaseRoles(databaseName).SchemaNames;
        }


        /// <summary>
        /// A list of names of the databases on the server
        /// </summary>
        public string[]         DatabaseNames
        {
            get
            {
                Request request             = new Request();

                request.Urn                 = "Server/Database";
                request.Fields              = new string[1] {"Name"};
                request.OrderByList         = new OrderBy[1] { new OrderBy("Name", OrderBy.Direction.Asc)};

                DataTable   databases       = new Enumerator().Process(this.currentState.Server.ConnectionContext, request);
                int         databaseCount   = databases.Rows.Count;
                string[]    result          = new string[databaseCount];

                for (int databaseIndex = 0; databaseIndex < databaseCount; ++databaseIndex)
                {
                    result[databaseIndex] = databases.Rows[databaseIndex][0].ToString();
                }

                return result;
            }
        }

        /// <summary>
        /// A list of names of the certificates in master database.
        /// </summary>
        public DataTable CertificateNames
        {
            get
            {
                Request request = new Request();

                request.Urn = "Server/Database[@Name='master']/Certificate";
                request.Fields = new string[1] { "Name" };
                request.OrderByList = new OrderBy[1] { new OrderBy("Name", OrderBy.Direction.Asc) };

                DataTable certificates = new Enumerator().Process(this.currentState.Server.ConnectionContext, request);

                DataView dv = certificates.DefaultView;
                dv.RowFilter = "Name NOT LIKE '##MS%'";
                                
                return dv.ToTable();
            }
        }

        /// <summary>
        /// A list of names of the certificates in master database.
        /// </summary>
        public DataTable AsymmetricKeyNames
        {
            get
            {
                Request request = new Request();

                request.Urn = "Server/Database[@Name='master']/AsymmetricKey";
                request.Fields = new string[1] { "Name" };
                request.OrderByList = new OrderBy[1] { new OrderBy("Name", OrderBy.Direction.Asc) };

                DataTable asymmetricKeys = new Enumerator().Process(this.currentState.Server.ConnectionContext, request);

                return asymmetricKeys;
            }
        }


        public Hashtable credProviderMap;
        public StringCollection CredentialNames
        {
            get
            {
                if (this.currentState.Server.Information.Version.Major < 9)
                    return null;

                bool isKatmai = (this.currentState.Server.Version.Major >= 10);
                Request request = new Request();
                request.Urn = "Server/Credential";
                if (isKatmai)
                {
                    request.Fields = new string[2] { "Name", "ProviderName" };
                }
                else
                {
                    request.Fields = new string[1] { "Name" };
                }
                DataTable dt = new Enumerator().Process(this.currentState.Server.ConnectionContext, request);
                StringCollection result = new StringCollection();
                credProviderMap = new Hashtable();
                foreach (DataRow dr in dt.Rows)
                {
                    result.Add(dr["Name"].ToString());
                    if (isKatmai)
                    {
                        credProviderMap.Add(dr["Name"].ToString(), dr["ProviderName"].ToString());
                    }
                    else
                    {
                        credProviderMap.Add(dr["Name"].ToString(), string.Empty);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="server">The server on which we are creating a login</param>
        public LoginPrototype(Microsoft.SqlServer.Management.Smo.Server server)
        {
            this.exists         = false;
            this.machineName    = server.ConnectionContext.TrueName.ToUpperInvariant();
            this.currentState   = new LoginPrototypeData(server);
            this.originalState  = (LoginPrototypeData) this.currentState.Clone();
            this.comparer       = new SqlCollationSensitiveStringComparer(server.Information.Collation);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="server">The server on which we are modifying a login</param>
        /// <param name="login">The login we are modifying</param>
        public LoginPrototype(Microsoft.SqlServer.Management.Smo.Server server, Login login)
        {
            this.exists         = true;
            this.machineName    = server.ConnectionContext.TrueName.ToUpperInvariant();
            this.currentState   = new LoginPrototypeData(server, login);
            this.originalState  = (LoginPrototypeData) this.currentState.Clone();
            this.comparer       = new SqlCollationSensitiveStringComparer(server.Information.Collation);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="server">The server on which we are creating a login</param>
        public LoginPrototype(Microsoft.SqlServer.Management.Smo.Server server, LoginInfo login)
        {
            this.exists         = false;
            this.machineName    = server.ConnectionContext.TrueName.ToUpperInvariant();
            this.currentState   = new LoginPrototypeData(server);
            this.originalState  = (LoginPrototypeData) this.currentState.Clone();
            this.comparer       = new SqlCollationSensitiveStringComparer(server.Information.Collation);

            this.LoginName = login.LoginName;
            this.SqlPassword = login.Password;
            this.OldPassword = login.OldPassword;
            this.LoginType = SqlServer.Management.Smo.LoginType.SqlLogin;
            this.DefaultLanguage = login.DefaultLanguage;
            this.DefaultDatabase = login.DefaultDatabase;
        }

        /// <summary>
        /// Reset the prototype state to its initial state
        /// </summary>
        public void     Reset(Microsoft.SqlServer.Management.Smo.Server server)
        {
            if (this.Exists)
            {
                this.Reload(server);
            }
            else
            {
                this.currentState = (LoginPrototypeData) this.originalState.Clone();
            }
        }

        public void     Reload(Microsoft.SqlServer.Management.Smo.Server server)
        {
            System.Diagnostics.Debug.Assert(this.Exists, "trying to load the state for a login that doesn't exist");

            Login login = server.Logins[this.LoginName];
            System.Diagnostics.Debug.Assert(login != null, "login does not exist on the server");

            this.originalState  = new LoginPrototypeData(server, login);
            this.currentState   = (LoginPrototypeData) this.originalState.Clone();

        }

        /// <summary>
        /// Create the login or modify the login's access type, default database, default language,
        /// and password
        /// map the login to credentials
        /// </summary>
        public void     ApplyGeneralChanges(Microsoft.SqlServer.Management.Smo.Server server)
        {
            bool    changesMade = false;
            Login   login       = null;

            // if the login exists, get the login, otherwise make a new login
            if (this.Exists)
            {
                login = server.Logins[this.LoginName];
                if (login == null)
                {
                    throw new ApplicationException("CreateLoginSR.LoginMissing(this.LoginName)");
                }
            }
            else
            {
                login = new Login(server, this.CanonicalizedLoginName);
                login.LoginType = this.LoginType;
            }

            // set whether to grant SQL CONNECT to the login and whether to disable the login
            if (this.currentState.WindowsGrantAccess != this.originalState.WindowsGrantAccess)
            {
                login.DenyWindowsLogin = !this.WindowsGrantAccess;
                changesMade = true;
            }

            if (9 <= server.Information.Version.Major)
            {
                if (!this.Exists || (this.currentState.CertificateName != this.originalState.CertificateName))
                {
                    login.Certificate = this.CertificateName;
                    changesMade = true;
                }

                if (!this.Exists || (this.currentState.AsymmetricKeyName != this.originalState.AsymmetricKeyName))
                {
                    login.AsymmetricKey = this.AsymmetricKeyName;
                    changesMade = true;
                }
            }

            // set the default database and language
            if (!this.Exists || (this.currentState.DefaultDatabase != this.originalState.DefaultDatabase))
            {
                login.DefaultDatabase   = this.DefaultDatabase;
                changesMade = true;
            }

            if (!this.Exists || (this.currentState.DefaultLanguage != this.originalState.DefaultLanguage))
            {
                if (this.DefaultLanguage == LoginPrototypeData.DefaultLanguageDisplay)
                {
                    login.Language      = String.Empty;
                }
                else
                {
                    login.Language      = this.DefaultLanguage;
                }

                changesMade = true;
            }

            // enforcing password policy, enforcing password expiration, and mapping a credential are supported only on Yukon+
            if ((this.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin) && (server.Information.Version.Major >= 9))
            {
                if (!this.Exists || (this.currentState.EnforcePolicy != this.originalState.EnforcePolicy))
                {
                    login.PasswordPolicyEnforced = this.currentState.EnforcePolicy;
                    changesMade = true;
                }

                if (!this.Exists || (this.currentState.EnforceExpiration != this.originalState.EnforceExpiration))
                {
                    login.PasswordExpirationEnabled = this.currentState.EnforceExpiration;
                    changesMade = true;
                }
            }

            if (server.Information.Version.Major == 9)
            {
                System.Diagnostics.Debug.Assert(this.currentState.Credentials.Count <= 1, "Yukon can have max one credential mapped to a login");
                if (this.currentState.Credentials.Count == 1 //One credential is currently present in the grid
                    && mapToCredential) //Credential in the grid should be honored.
                {
                    if (!this.originalState.Credentials.Contains(this.currentState.Credentials[0])) //Ensuring that credential is either added or changed.
                    {
                        // new credential is added
                        login.Credential = this.currentState.Credentials[0];
                        changesMade = true;
                    }
                }
                else if (this.originalState.Credentials.Count == 1 //Originally atleast one credential was present.
                    && (!mapToCredential //Credential in the grid should be ignored.
                        || this.currentState.Credentials.Count == 0)) //Grid is empty now.
                {
                    // credential is dropped
                    login.Credential = string.Empty;
                    changesMade = true;
                }
            }

            // create or alter the login
            if (this.Exists)
            {
                if (changesMade)
                {
                    login.Alter();
                }
            }
            else
            {
                // if login is a SQL Login and the login is being created, specify the password
                if (this.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin)
                {
                    login.Create(this.SqlPassword, this.MustChange ? LoginCreateOptions.MustChange : LoginCreateOptions.None);
                }
                else
                {
                    login.Create();  
                }
            }

            // If the login already exists and uses SQL authentication and either
            // the password or one of the password options has changed, change
            // the password to match.  Note that this should be delayed until after
            // the "enforce policy" and "enforce expiration" options have been set
            // in the call to Login.Alter() above.
            if ((this.LoginType == SqlServer.Management.Smo.LoginType.SqlLogin) && this.Exists)
            {
                if (this.currentState.SqlPassword != this.originalState.SqlPassword) //Password is changed
                {
                    if (server.Information.Version.Major >= 9
                        && (this.currentState.IsLockedOut != this.originalState.IsLockedOut
                        || this.currentState.MustChange != this.originalState.MustChange))
                    {
                        if (this.currentState.IsLockedOut != this.originalState.IsLockedOut //IsLockedOut value has been changed
                            && !this.currentState.IsLockedOut) //IsLockedOut is false, which means we have to unlock the login now
                        {
                            login.ChangePassword(this.SqlPassword, !this.currentState.IsLockedOut, this.MustChange);
                        }
                        else //no change in isLockedOut means we don't need to include UNBLOCK Option in the Alter Login query
                        {
                            login.ChangePassword(this.SqlPassword, false, this.MustChange);
                        }
                    }
                    else //For server versions < 9, UNLOCK(IsLockedOut) and MUST_CHANGE(MustChange) are not provided
                    {     //For server version >= 9, if we don't change UNLOCK(IsLockedOut) and MUST_CHANGE(MustChange), the T-Sql remains the same as the previous versions
                        if (!this.currentState.ShowOldPassword)
                        {
                            login.ChangePassword(this.SqlPassword);
                        }
                        else
                        {
                            login.ChangePassword(this.OldPassword, this.SqlPassword);
                        }
                    }
                }
                else //Password should be reset while unlocking
                {
                    if (server.Information.Version.Major >= 9
                        && (this.currentState.IsLockedOut != this.originalState.IsLockedOut)
                        )
                    {
                        throw new ArgumentException("CreateLoginSR.ResetPasswordWhileUnlocking");
                    }
                }
            }            

            // enable or disable the login as needed
            if (this.currentState.IsDisabled != this.originalState.IsDisabled)
            {
                if (this.currentState.IsDisabled)
                {
                    login.Disable();
                }
                else
                {
                    login.Enable();
                }
            }
            if (server.Information.Version.Major >= 10)
            {
                foreach (string dropCredential in this.originalState.Credentials)
                {
                    if (!mapToCredential || !this.currentState.Credentials.Contains(dropCredential))
                    {
                        login.DropCredential(dropCredential);
                    }
                }
                foreach (string newCredential in this.currentState.Credentials)
                {
                    if (mapToCredential && !this.originalState.Credentials.Contains(newCredential))
                    {
                        login.AddCredential(newCredential);
                    }
                }
            }
        }

        /// <summary>
        /// Set the login's server role membership
        /// </summary>
        public void ApplyServerRoleChanges(Microsoft.SqlServer.Management.Smo.Server server)
        {
            // get the login
            Login login = server.Logins[this.LoginName];

            // foreach server role
            foreach (ServerRole role in server.Roles)
            {
                // all users are always members of the public role, so skip processing for public
                if (0 != String.Compare(role.Name, "public", StringComparison.Ordinal))
                {
                    bool wasOriginallyARoleMember   = this.originalState.ServerRoles.IsMember(role.Name);
                    bool isCurrentlyARoleMember     = this.currentState.ServerRoles.IsMember(role.Name);


                    // if the login is currently a member of the role, but wasn't originally a member, add the login to the role
                    if (isCurrentlyARoleMember && !wasOriginallyARoleMember)
                    {
                        role.AddMember(this.LoginName);
                    }
                    // if the login is not currently a member of the role, but originally was a member, remove the login from the role
                    else if (!isCurrentlyARoleMember && wasOriginallyARoleMember)
                    {
                        role.DropMember(this.LoginName);
                    }
                }
            }
        }

        /// <summary>
        /// Set the login's database role membership
        /// </summary>
        public void     ApplyDatabaseRoleChanges(Microsoft.SqlServer.Management.Smo.Server server)
        {
            System.Diagnostics.Debug.Assert(server != null, "the server is null");

            // get the login
            Login login = server.Logins[this.LoginName];

            // foreach database
            foreach (Database database in server.Databases)
            {
                // if anything has been changed, then the currentState will include the database
                if (this.currentState.DatabaseRolesCollection.Contains(database.Name))
                {
                    DatabaseRoles currentDatabaseRoles  = (DatabaseRoles) this.currentState.DatabaseRolesCollection[database.Name];
                    DatabaseRoles originalDatabaseRoles = (DatabaseRoles) this.originalState.DatabaseRolesCollection[database.Name];

                    // if the login is currently permitted in the database
                    if (currentDatabaseRoles.PermitDatabaseAccess)
                    {
                        // get the existing user for the login in the database
                        bool    userRecreated       = false;
                        string  existingUserName    = originalDatabaseRoles.UserName;
                        User    user = 
                            ((existingUserName != null) && (existingUserName.Length != 0)) ?
                            database.Users[existingUserName] :
                            null;
                        
                        // If the user doesn't exist, create the user in the database.
                        if (user == null)
                        {
                            // Note that if the user name already exists and is mapped to
                            // another login, SMO will emit the appropriate error message.

                            user        = new User(database, currentDatabaseRoles.UserName);
                            user.Login  = this.LoginName;
                            user.Create();
                        }
                        // if a user does exist in the database for the login and
                        // the user name has been changed
                        else if (user.Name != currentDatabaseRoles.UserName)
                        {
                            // rename the user to the new name
                            user.Rename(currentDatabaseRoles.UserName);
                        }

                        System.Diagnostics.Debug.Assert(user != null, "user has not been created");

                        if (currentDatabaseRoles.DefaultSchema != originalDatabaseRoles.DefaultSchema)
                        {
                            user.DefaultSchema = currentDatabaseRoles.DefaultSchema;
                            user.Alter();

                            // if the schema doesn't exist, create it
                            if ((user.DefaultSchema != null) && (user.DefaultSchema.Length != 0) &&
                            !database.Schemas.Contains(user.DefaultSchema))
                            {
                                Schema schema   = new Schema(database, user.DefaultSchema);
                                schema.Owner    = user.Name;

                                schema.Create();
                            }
                         }

                        // if any of the roles have been changed
                        if (currentDatabaseRoles.RoleMembershipChanged || userRecreated || !this.Exists)
                        {
                            // commit the role changes to the server
                            foreach (DatabaseRole role in database.Roles)
                            {
                                // all users are always members of the public role, so skip processing for public
                                if (0 != String.Compare(role.Name, "public", StringComparison.Ordinal))
                                {
                                    // If the login is new, it is not a member of any role
                                    //
                                    bool wasOriginallyARoleMember = (this.Exists && originalDatabaseRoles.IsMember(role.Name));
                                    bool isCurrentlyARoleMember     = currentDatabaseRoles.IsMember(role.Name);
                                    bool defaultSchemaChanged       = (originalDatabaseRoles.DefaultSchema != currentDatabaseRoles.DefaultSchema);

                                     

                                    // if the login is currently in the role, but wasn't originally, add the login to the role
                                    if (isCurrentlyARoleMember && (!wasOriginallyARoleMember || userRecreated))
                                    {
                                        role.AddMember(currentDatabaseRoles.UserName);
                                    }
                                    // if the login is currently not in the role, but originally was, remove the login from the role
                                    if (!isCurrentlyARoleMember && (wasOriginallyARoleMember && !userRecreated))
                                    {
                                        role.DropMember(originalDatabaseRoles.UserName);
                                    }
                                }
                            }
                        }
                    }
                    // else if the login was originally permitted in the database
                    else if (originalDatabaseRoles.PermitDatabaseAccess)
                    {
                        // remove the associated user
                        string existingUserName = login.GetDatabaseUser(database.Name);
                        if ((existingUserName != null) && (existingUserName.Length != 0))
                        {
                            System.Diagnostics.Debug.Assert(database.Users[existingUserName] != null, "user not found in collection");
                            database.Users[existingUserName].Drop();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove the login from all database roles in the named database
        /// </summary>
        /// <param name="databaseName">The name of the database from which we are removing login</param>
        internal void   DisjoinAllDatabaseRoles(string databaseName)
        {
            DatabaseRoles roles = this.GetDatabaseRoles(databaseName);

            foreach (string roleName in roles.DatabaseRoleNames)
            {
                roles.SetMember(roleName, false);
            }
        }

        /// <summary>
        /// Update any default schemas or user names in the databaseRolesCollection that match
        /// the old login name to match a new login name.
        /// </summary>
        /// <param name="newLoginName">The new login name</param>
        private void    UpdateDefaultSchemaAndUserNames(string newLoginName)
        {
            foreach (DatabaseRoles databaseRoles in this.currentState.DatabaseRolesCollection.Values)
            {
                if (databaseRoles.PermitDatabaseAccess)
                {  
                    // if the default schema name is the same as the login name,
                    // then set the default schema name to the new login name
                    if (this.NameMatchesCurrentLoginName(databaseRoles.DefaultSchema))
                    {
                        databaseRoles.DefaultSchema = newLoginName;
                    }

                    // if the user name is the same as the login name, 
                    // then set the user name to the new login name
                    if (this.NameMatchesCurrentLoginName(databaseRoles.UserName))
                    {
                        databaseRoles.UserName = newLoginName;
                    }

                }
            }
        }

        /// <summary>
        /// Does the input name match the current login name
        /// </summary>
        /// <param name="otherName">The name to compare</param>
        /// <returns>True if they match, otherwise false</returns>
        private bool NameMatchesCurrentLoginName(string otherName)
        {
            // if the login is not a windows login, do a collation-sensitive comparison.
            // 
            // if the login is a windows login, do a case-insensitive comparison
            // because domain\user is equivalent to the canonicalized form of DOMAIN\user.

            bool matchesSqlLogin = 
                !this.UseWindowsAuthentication &&
                (0 == this.comparer.Compare(this.CanonicalizedLoginName, otherName));

            bool matchesWindowsLogin =
                this.UseWindowsAuthentication &&
                (0 == string.Compare(this.CanonicalizedLoginName, otherName, StringComparison.OrdinalIgnoreCase));

            return (matchesSqlLogin || matchesWindowsLogin);
        }

        /// <summary>
        /// tells us if user changed the password (password initial state different then current state)
        /// this is required by ui since it needs to enable disable some checkboxes based on this info
        /// </summary>
        internal bool PasswordWasChanged
        {
            get
            {
                if ( (!this.Exists) || (this.currentState.SqlPassword != this.originalState.SqlPassword))
                {
                    return true;
                }
                return false;
            }
        }

        public bool UseWindowsAuthentication
        {
            get
            {
                return ((this.LoginType == SqlServer.Management.Smo.LoginType.WindowsUser) || (this.LoginType == SqlServer.Management.Smo.LoginType.WindowsGroup));
            }
        }

        public bool UseAadAuthentication
        {
            get
            {
                return ((this.LoginType == SqlServer.Management.Smo.LoginType.ExternalUser) || (this.LoginType == SqlServer.Management.Smo.LoginType.ExternalGroup));
            }
        }
    }

    /// <summary>
    /// Case-sensitive, culture-invariant string comparer
    /// </summary>
    internal class CaseSensitiveCultureInvariantComparer : IComparer
    {
        public CaseSensitiveCultureInvariantComparer() {}

        /// <summary>
        /// Compare strings a and b
        /// </summary>
        /// <param name="a">First string</param>
        /// <param name="b">Second string</param>
        /// <returns>less than zero if a is less than b, 0 if a and b are equal, and greater than zero if a is greater than b</returns>
        public int Compare(object a, object b)
        {
            if ((a == null) || (b == null) || !(a is String) || !(b is String))
            {
                throw new ArgumentException();
            }

            string string_a = (string) a;
            string string_b = (string) b;

            return String.Compare(string_a, string_b, StringComparison.Ordinal);
        }
    }
}
