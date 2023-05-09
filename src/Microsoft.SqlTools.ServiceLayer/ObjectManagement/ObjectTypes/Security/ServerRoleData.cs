//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.PermissionsData;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// ServerRoleGeneral - main app role page
    /// </summary>
    internal class ServerRolePrototype
    {
        #region Members

        /// <summary>
        /// data container member that contains data specific information like
        /// connection infor, SMO server object or an AMO server object as well
        /// as a hash table where one can manipulate custom data
        /// </summary>
        private CDataContainer dataContainer = null;
        private Principal principal = null;
        private SecurablePermissions[] securablePermissions = null;

        private bool exists;
        private ServerRolePrototypeData currentState;
        private ServerRolePrototypeData originalState;

        #endregion

        #region Trace support
        private const string componentName = "ServerRoleGeneral";

        public string ComponentName
        {
            get
            {
                return componentName;
            }
        }
        #endregion

        #region Properties: CreateNew/Properties mode
        public string Name
        {
            get
            {
                return this.currentState.ServerRoleName;
            }
            set
            {
                this.currentState.ServerRoleName = value;
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

        public List<string> Memberships
        {
            get
            {
                return this.currentState.Memberships;
            }
            set
            {
                this.currentState.Memberships = value;
            }
        }

        public bool IsYukonOrLater
        {
            get
            {
                return this.dataContainer.Server.VersionMajor >= 9;
            }
        }

        public bool IsFixedRole
        {
            get
            {
                return this.currentState.IsFixedRole;
            }
        }

        public SecurablePermissions[] SecurablePermissions
        {
            get
            {
                return securablePermissions;
            }
            set
            {
                securablePermissions = value;
            }
        }
        #endregion

        #region Constructors / Dispose
        public ServerRolePrototype(CDataContainer context)
        {
            this.exists = false;
            this.dataContainer = context;
            this.currentState = new ServerRolePrototypeData(context);
            this.originalState = (ServerRolePrototypeData)this.currentState.Clone();
            this.securablePermissions = new SecurablePermissions[0];
        }

        /// <summary>
        /// ServerRoleData for creating a new app role
        /// </summary>
        public ServerRolePrototype(CDataContainer context, ServerRoleInfo roleInfo)
        {
            this.exists = false;
            this.dataContainer = context;
            this.currentState = new ServerRolePrototypeData(context);
            this.originalState = (ServerRolePrototypeData)this.currentState.Clone();

            this.ApplyInfoToPrototype(roleInfo);
        }

        /// <summary>
        /// ServerRoleData for editing an existing app role
        /// </summary>
        public ServerRolePrototype(CDataContainer context, ServerRole role)
        {
            this.exists = true;
            this.dataContainer = context;
            this.currentState = new ServerRolePrototypeData(context, role);
            this.originalState = (ServerRolePrototypeData)this.currentState.Clone();
            this.principal = SecurableUtils.CreatePrincipal(true, PrincipalType.ServerRole, role, context);
            securablePermissions = SecurableUtils.GetSecurablePermissions(this.exists, PrincipalType.ServerRole, role, this.dataContainer);
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
            Microsoft.SqlServer.Management.Smo.Server srv = this.dataContainer.Server;
            System.Diagnostics.Debug.Assert(srv != null, "server object is null");

            ServerRole serverRole = null;
            if (this.exists) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.Name), "serverRoleName is empty");

                serverRole = srv.Roles[this.Name];
                System.Diagnostics.Debug.Assert(serverRole != null, "serverRole object is null");

                if (0 != String.Compare(this.currentState.Owner, this.originalState.Owner, StringComparison.Ordinal))
                {
                    serverRole.Owner = this.Owner;
                    serverRole.Alter();
                }
            }
            else // not in properties mode -> create role
            {
                serverRole = new ServerRole(srv, this.Name);
                if (this.Owner.Length != 0)
                {
                    serverRole.Owner = this.Owner;
                }
                serverRole.Create();
            }

            SendToServerMemberChanges(serverRole);
            SendToServerMembershipChanges(serverRole);
        }
        #endregion

        /// <summary>
        /// sends to server user changes related to members
        /// </summary>
        private void SendToServerMemberChanges(ServerRole serverRole)
        {
            if (!this.exists)
            {
                foreach (string member in this.Members)
                {
                    serverRole.AddMember(member);
                }
            }
            else
            {
                foreach (string member in this.Members)
                {
                    if (!this.originalState.Members.Contains(member))
                    {
                        serverRole.AddMember(member);
                    }
                }

                foreach (string member in this.originalState.Members)
                {
                    if (!this.Members.Contains(member))
                    {
                        serverRole.DropMember(member);
                    }
                }
            }
        }

        /// <summary>
        /// sends to server user changes related to memberships
        /// </summary>
        private void SendToServerMembershipChanges(ServerRole serverRole)
        {
            if (!this.exists)
            {
                foreach (string role in this.Memberships)
                {
                    serverRole.AddMembershipToRole(this.dataContainer.Server.Roles[role].Name);
                }
            }
            else
            {
                foreach (string role in this.Memberships)
                {
                    if (!this.originalState.Memberships.Contains(role))
                    {
                        serverRole.AddMembershipToRole(this.dataContainer.Server.Roles[role].Name);
                    }
                }

                foreach (string role in this.originalState.Memberships)
                {
                    if (!this.Memberships.Contains(role))
                    {
                        serverRole.DropMembershipFromRole(this.dataContainer.Server.Roles[role].Name);
                    }
                }
            }
        }


        public void ApplyInfoToPrototype(ServerRoleInfo roleInfo)
        {
            this.Name = roleInfo.Name;
            this.Owner = roleInfo.Owner;
            this.Members = roleInfo.Members.ToList();
            this.Memberships = roleInfo.Memberships.ToList();
        }

        private class ServerRolePrototypeData : ICloneable
        {
            #region data members
            private string serverRoleName = string.Empty;
            private string owner = String.Empty;
            private bool initialized = false;
            private List<string> members = new List<string>();
            private List<string> memberships = new List<string>();
            private ServerRole role = null;
            private ServerRoleExtender extender = null;
            private Server server = null;
            private CDataContainer context = null;
            private bool isYukonOrLater = false;
            #endregion

            #region Properties

            // General properties


            public string ServerRoleName
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }

                    return this.serverRoleName;
                }

                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.serverRoleName = value;
                }
            }

            public ServerRole ServerRole
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

            public List<string> Memberships
            {
                get
                {
                    if (!this.initialized)
                    {
                        LoadData();
                    }
                    return this.memberships;
                }
                set
                {
                    System.Diagnostics.Debug.Assert(this.initialized, "unexpected property set before initialization");
                    this.memberships = value;
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

            public bool IsFixedRole
            {
                get
                {
                    return this.role != null && this.role.IsFixedRole;
                }
            }

            #endregion

            /// <summary>
            /// private default constructor - used by Clone()
            /// </summary>
            private ServerRolePrototypeData()
            {
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="context">The context in which we are creating a new serverRole</param>
            public ServerRolePrototypeData(CDataContainer context)
            {
                this.server = context.Server;
                this.context = context;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
                LoadData();
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="context">The context in which we are modifying an existing serverRole</param>
            /// <param name="serverRole">The serverRole we are modifying</param>
            public ServerRolePrototypeData(CDataContainer context, ServerRole serverRole)
            {
                this.server = context.Server;
                this.context = context;
                this.isYukonOrLater = (this.server.Information.Version.Major >= 9);
                this.role = serverRole;
                this.extender = new ServerRoleExtender(this.role);
                LoadData();
            }

            /// <summary>
            /// Create a clone of this ServerRolePrototypeData object
            /// </summary>
            /// <returns>The clone ServerRolePrototypeData object</returns>
            public object Clone()
            {
                ServerRolePrototypeData result = new ServerRolePrototypeData();
                result.serverRoleName = this.serverRoleName;
                result.initialized = this.initialized;
                result.members = new List<string>(this.members);
                result.memberships = new List<string>(this.memberships);
                result.role = this.role;
                result.extender = this.extender;
                result.owner = this.owner;
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
                this.serverRoleName = role.Name;
                this.owner = role.Owner;
                LoadMembers();
                LoadMemberships();
            }

            private void LoadNew()
            {
            }

            private void LoadMembers()
            {
                if (this.Exists)
                {
                    foreach (string memberName in this.role.EnumMemberNames())
                    {
                        this.members.Add(memberName);
                    }
                }
            }

            private void LoadMemberships()
            {
                foreach (ServerRole srvRole in this.server.Roles)
                {
                    if (srvRole.EnumMemberNames().Contains(this.role.Name))
                    {
                        this.memberships.Add(srvRole.Name);
                    }
                }
            }
        }
    }
}
