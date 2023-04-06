//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System.ComponentModel;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    public class ServerRoleManageTaskFormComponent
    {
        protected ServerRole serverRole;
        ServerRoleExtender extender;

        ServerRole instance;
        Server server;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        public ServerRoleManageTaskFormComponent()
        {
            InitializeComponent();
        }

        public ServerRoleManageTaskFormComponent(IContainer container)
        {
            // container.Add(this);
            InitializeComponent();
        }

        protected string MethodName
        {
            get
            {
                return "Alter";
            }
        }

        protected ServerRole Instance
        {
            get
            {
#pragma warning disable IDE0074 // Use compound assignment
                if (this.instance == null)
                {
                    this.instance = CreateSmoObjectInstance();
                }
#pragma warning restore IDE0074 // Use compound assignment
                return this.instance;
            }
        }

        protected Server Server
        {
            get
            {
                return this.server;
            }
        }

        protected Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertyProvider CreatePropertyProvider()
        {
#pragma warning disable IDE0074 // Use compound assignment
            if (extender == null)
            {
                extender = new ServerRoleExtender(this.Instance);
            }
#pragma warning restore IDE0074 // Use compound assignment
            return extender;            
        }        

        protected ServerRole CreateSmoObjectInstance()
        {
            if (this.serverRole == null)
            {
                Urn urn;
                // if (!SmoTaskHelper.TryGetUrn(this.TaskManager.Context, out urn))
                if (true) // TODO new server role
                {
                    this.serverRole = new ServerRole(this.Server, GetServerRoleName());
                }
                else
                {
                    this.serverRole = this.Server.GetSmoObject(urn) as ServerRole;
                }
            }
            return this.serverRole;
        }

        protected void PerformTask()
        {
            this.CreateOrManage();

            //General Page Permissions Related actions
            EventHandler tempEventHandler = (EventHandler)this.extender.GeneralPageOnRunNow;
            if (tempEventHandler != null)
            {
                tempEventHandler(this, new EventArgs());
            }

            //Disposing DataContainer object as it has a dedicated server connection.
            if (this.extender.GeneralPageDataContainer != null)
            {
                ((CDataContainer)this.extender.GeneralPageDataContainer).Dispose();
            }
        }

        private void CreateOrManage()
        {
            //General Page Actions
            if (Utils.IsSql11OrLater(this.Instance.ServerVersion.Major))
            {
                if (this.Instance.State == SqlSmoState.Creating)
                {
                    if (this.extender.OwnerForUI == string.Empty) //In order to avoid scripting Authorization part of ddl.
                    {
                        this.extender.OwnerForUI = null;
                    }
                    this.Instance.Create();
                }
                else
                {
                    this.Instance.Alter();
                }
            }

            //Members Page Related actions
            this.extender.RefreshRoleMembersHash();
            Dictionary<string, bool> memberNameIsMemberHash = this.extender.MemberNameIsMemberHash;

            StringCollection membersToBeDropped = new StringCollection();
            StringCollection membersToBeAdded = new StringCollection();

            StringCollection membershipsToBeDropped = new StringCollection();
            StringCollection membershipsToBeAdded = new StringCollection();

            foreach (string memberName in memberNameIsMemberHash.Keys)
            {
                if (memberNameIsMemberHash[memberName]) //if added as member
                {
                    membersToBeAdded.Add(memberName);
                }
                else //if dropped from members
                {
                    membersToBeDropped.Add(memberName);
                }
            }

            //Membership page Related actions
            this.extender.RefreshServerRoleNameHasMembershipHash();
            Dictionary<string, bool> membershipInfoHash = this.extender.ServerRoleNameHasMembershipHash;

            foreach (string serverRoleName in membershipInfoHash.Keys)
            {
                if (membershipInfoHash[serverRoleName]) //If new membership added
                {
                    membershipsToBeAdded.Add(serverRoleName);
                }
                else //If now not a member of
                {
                    membershipsToBeDropped.Add(serverRoleName);
                }
            }

            //First dropping members and memberships
            foreach (string member in membersToBeDropped)
            {
                this.serverRole.DropMember(member);
            }

            foreach (string containingRole in membershipsToBeDropped)
            {
                this.serverRole.DropMembershipFromRole(containingRole);
            }

            //Now adding members and memberships.
            foreach (string member in membersToBeAdded)
            {
                this.serverRole.AddMember(member);
            }

            foreach (string containingRole in membershipsToBeAdded)
            {
                this.serverRole.AddMembershipToRole(containingRole);
            }
        }

        // protected System.Collections.Specialized.StringCollection GetScriptStrings(ITaskExecutionContext context)
        // {
        //     StringCollection script = this.GetScriptForCreateOrManage();

        //     StringCollection permissionScript = this.GetPermissionRelatedScripts();

        //     foreach (string str in permissionScript)
        //     {
        //         script.Add(str);
        //     }

        //     if (script.Count == 0)
        //     {
        //         //When the user tries to script and no changes have been made.                
        //         // throw new SsmsException(SR.NoActionToBeScripted);
        //     }
        //     return script;
        // }

        private StringCollection GetPermissionRelatedScripts()
        {
            StringCollection script = new StringCollection();

            if (this.extender.GeneralPageDataContainer != null) //Permission controls have been initialized.
            {
                //For General Page permissions
                ServerConnection permControlConn = ((CDataContainer)this.extender.GeneralPageDataContainer).ServerConnection;
                SqlExecutionModes em = permControlConn.SqlExecutionModes;

                //PermissionUI has a cloned connection.                        
                permControlConn.CapturedSql.Clear();
                permControlConn.SqlExecutionModes = SqlExecutionModes.CaptureSql;

                //This will run General page's permission related actions.
                EventHandler tempEventHandler = (EventHandler)this.extender.GeneralPageOnRunNow;
                if (tempEventHandler != null)
                {
                    tempEventHandler(this, new EventArgs());
                }

                script = permControlConn.CapturedSql.Text;
                permControlConn.SqlExecutionModes = em;
            }

            return script;
        }

        /// <summary>
        /// Generates script for Create and Properties.
        /// </summary>
        /// <returns></returns>
        private StringCollection GetScriptForCreateOrManage()
        {
            StringCollection script = new StringCollection();

            Server svr = this.Instance.Parent;
            svr.ConnectionContext.CapturedSql.Clear();
            SqlExecutionModes em = svr.ConnectionContext.SqlExecutionModes;

            svr.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;

            //T-SQL Capturing starts.

            this.CreateOrManage();

            //T-SQL capturing ends
            script = svr.ConnectionContext.CapturedSql.Text;
            svr.ConnectionContext.SqlExecutionModes = em;

            return script;
        }

        protected string GetServerRoleName()
        {
            return "ServerRole-" + DateTime.Now.ToString("yyyyMMdd-HHmmss",SmoApplication.DefaultCulture);
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected void Dispose(bool disposing)
        {
            // if (disposing && (components != null))
            // {
            //     components.Dispose();
            // }
            // base.Dispose(disposing);
        }
    }

    public class ServerRoleCreateTaskFormComponent : ServerRoleManageTaskFormComponent
    {
        public ServerRoleCreateTaskFormComponent()
            : base()
        {
        }

        public ServerRoleCreateTaskFormComponent(IContainer container)
            : base(container)
        {
        }

        protected ServerRole CreateSmoObjectInstance()
        {
#pragma warning disable IDE0074 // Use compound assignment
            if (this.serverRole == null)
            {
                this.serverRole = new ServerRole(this.Server, GetServerRoleName());
            }
#pragma warning restore IDE0074 // Use compound assignment
            return this.serverRole;
        }

        protected string MethodName
        {
            get
            {
                return "Create";
            }
        }
    }
}
