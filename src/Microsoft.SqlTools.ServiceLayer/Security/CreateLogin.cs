//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Windows.Forms;
using System.Resources;
using System.Data;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.Smo;

using Microsoft.NetEnterpriseServers;
using Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginData;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// CreateLogin control.
    /// </summary>
    internal class CreateLogin
    {
        private ResourceManager resourceManager;
        private LoginPrototype  prototype;
        internal string loginName;
        
        /// <summary>
        /// contructor
        /// </summary>
        public CreateLogin()
        {
            //
            // TODO: Add constructor logic here
            //

            resourceManager = null;
            prototype       = null;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context">the database context for the control</param>
        public CreateLogin(CDataContainer context)
        {
            resourceManager = new ResourceManager(
                "Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
                typeof(CreateLogin).Assembly);

            this.DataContainer  = context;
    
            string loginName    = context.GetDocumentPropertyString("login");

            if (loginName.Length != 0)
            {
                this.CheckObjects(loginName);
                Login login = context.Server.Logins[loginName];
                this.prototype  = new LoginPrototype(context.Server, login);
            }
            else
            {
                this.prototype      = new LoginPrototype(context.Server);
            }


            InitializeNodeAssociations();
        }

        private void CheckObjects(string login)
        {
            Request     request     = new Request();
            Enumerator  enumerator  = new Enumerator();
            DataTable   dataTable   = null;

            System.Diagnostics.Debug.Assert(login.Length != 0, "unexpected empty login name");

            request.Urn     = "Server/Login[@Name='" + Urn.EscapeString(login) + "']";
            request.Fields  = new string[] {"Name"};
            dataTable       = enumerator.Process(ServerConnection, request);
            
            if(dataTable.Rows.Count == 0)
            {
                throw new ObjectNoLongerExistsException();
            }               
        }

        /// <summary>
        /// initialize the icon, title, and treeview nodes
        /// </summary>
        private void InitializeNodeAssociations()
        {
            PanelTreeNode       node            = null;
            PanelTreeNode       auxNode         = null;
            CUtils              util            = new CUtils();

            UserControl general                         = new CreateLoginGeneral(this.DataContainer, this.prototype);
            UserControl serverRoles                     = new CreateLoginServerRoles(this.DataContainer, this.prototype);
            CreateLoginDatabaseAccess   loginDbAccess   = new CreateLoginDatabaseAccess(this.DataContainer, this.prototype);

            AddView(general);
            AddView(serverRoles);
            AddView(loginDbAccess);
    

            // login permissions only valid for Yukon and later servers
            if (9 <= this.DataContainer.Server.ConnectionContext.ServerVersion.Major)
            {
                // show the permissions page for new logins and existing non-system logins
                if (this.DataContainer.IsNewObject || !((Login) this.DataContainer.SqlDialogSubject).IsSystemObject)
                {
                    UserControl permissions = new PermissionsServerPrincipal(this.DataContainer, PrincipalType.Login);
                    AddView(permissions);

                
                }

                UserControl status = new CreateLoginStatus(this.DataContainer, this.prototype);
                AddView(status);

            }

            AddNode(node);
        }


        /// <summary>
        /// called by IExecutionAwareSqlControlCollection.PreProcessExecution to enable derived
        /// classes to take over execution of the dialog and do entire execution in this method
        /// rather than having the framework to execute dialog views one by one.
        /// 
        /// NOTE: it might be called from non-UI thread
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="executionResult"></param>
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);
            
            try
            {
                executionResult = ExecutionMode.Success;
                if (this.prototype.LoginType == LoginType.SqlLogin)
                {
                    // check that there is a password
                    // this check is made for versions prior 9.0, or for 9.0 if policy enforcement is off
                    // for 9.0 with policy turned on we do not display this message, instead we let server
                    // return the error associated with null password (coming from policy) - see bug 124377
                    if ((this.prototype.SqlPassword.Length == 0) &&
                        ((this.DataContainer.Server.Information.Version.Major < 9) ||
                        (this.prototype.EnforcePolicy == false)))
                    {
                        string passwordBlank = resourceManager.GetString("general.error.passwordBlank");                                                                       
                        
                        if (DialogResult.Yes != this.MessageBoxProvider.ShowMessage
                            (new Exception(passwordBlank),
                            SRError.SQLWorkbench,ExceptionMessageBoxButtons.YesNo,
                            ExceptionMessageBoxSymbol.Exclamation,this as IWin32Window))
                        {
                            executionResult = ExecutionMode.Failure;
                            return false;
                        }                                                                   
                    }

                    // check that password and confirm password controls' text matches
                    if (0 != String.Compare(this.prototype.SqlPassword, this.prototype.SqlPasswordConfirm, StringComparison.Ordinal))
                    {
                        string passwordNotMatch = resourceManager.GetString("general.error.passwordMismatch");

                        executionResult = ExecutionMode.Failure;

                        throw new Exception(passwordNotMatch);
                    }
                }
                else if (this.prototype.LoginType == LoginType.Certificate)
                {
                    if(string.IsNullOrEmpty(this.prototype.CertificateName))
                    {
                        string noCertificateSelected = resourceManager.GetString("general.error.noCertificateSelected");

                        executionResult = ExecutionMode.Failure;

                        throw new Exception(noCertificateSelected);
                    }
                }
                else if (this.prototype.LoginType == LoginType.AsymmetricKey)
                {
                    if(string.IsNullOrEmpty(this.prototype.AsymmetricKeyName))
                    {
                        string noKeySelected = resourceManager.GetString("general.error.noKeySelected");

                        executionResult = ExecutionMode.Failure;

                        throw new Exception(noKeySelected);
                    }
                }

                this.prototype.ApplyGeneralChanges(this.DataContainer.Server);
                this.DataContainer.ObjectName = this.prototype.LoginName;
                this.loginName = this.prototype.LoginName;
                executionResult = ExecutionMode.Success;

                // allow pages to process themselves
                return true;
            }
            catch (Exception)
            {
                // whatever we tried to do failed at least partially.  Refresh
                // SMO's state to match the database, but only if we're not scripting,
                // because the base class will substitute server object with the original one
                // anyway
                if (runType == RunType.RunNow || runType == RunType.RunNowAndExit)
                {
                    this.DataContainer.Server = new Microsoft.SqlServer.Management.Smo.Server(this.DataContainer.Server.ConnectionContext);
                }

                executionResult = ExecutionMode.Failure;
                throw;
            }
        }
        
        /// <summary>
        /// called before dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// </returns>
        /// <returns></returns>
        protected override bool DoPreProcessReset()
        {
            base.DoPreProcessReset();
            
            this.prototype.Reset(this.DataContainer.Server);

            return true;
        }
    }
}
