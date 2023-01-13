//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
//using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
// using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Main class for Login Service functionality
    /// </summary>
    public sealed class LoginService
    {
        private bool disposed;

        private ConnectionService connectionService = null;

        private static readonly Lazy<LoginService> instance = new Lazy<LoginService>(() => new LoginService());

        /// <summary>
        /// Construct a new LoginService instance with default parameters
        /// </summary>
        public LoginService()
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
        public static LoginService Instance
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
                ErrorMessage = null
            });
        }

#if false
        #region "Create Login"

        private LoginPrototype  prototype;
        internal string loginName;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context">the database context for the control</param>
        public void CreateLogin(CDataContainer context)
        {      
            string loginName    = context.GetDocumentPropertyString("login");

            if (loginName.Length != 0)
            {
                //this.CheckObjects(loginName);
                Login login = context.Server.Logins[loginName];
                this.prototype  = new LoginPrototype(context.Server, login);
            }
            else
            {
                this.prototype      = new LoginPrototype(context.Server);
            }
            //InitializeNodeAssociations();    
        }

        private void CreateLogin_CheckObjects(string login)
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
        protected bool CreateLogin_DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            //base.DoPreProcessExecution(runType, out executionResult);
            
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
        /// constructor
        /// </summary>
        /// <param name="context">The database context for the dialog</param>
        /// <param name="prototype">The prototype login we are working with</param>
        internal void CreateLoginDatabaseAccess(CDataContainer context, LoginPrototype prototype)
        {
            // this.databaseRolesLabelFormat = resourceManager.GetString("databaseAccess.rolesLabel");
            // this.databaseGuestStatusFormat = resourceManager.GetString("databaseAccess.guestEnabled");
        }

        // void CreateLogin_OnSelection(TreeNode node)
        // {
        //     if (!this.checkedForInaccessibleDatabases)
        //     {
        //         this.checkedForInaccessibleDatabases = true;

        //         foreach (string databaseName in this.prototype.DatabaseNames)
        //         {
        //             // get the information for the database
        //             DatabaseRoles databaseRoles = this.prototype.GetDatabaseRoles(databaseName);

        //             // if the database is accessible, add a row for it in the grid
        //             if (!databaseRoles.DatabaseIsAccessible)
        //             {
        //                 // if the database is inaccessible and we haven't warned already, warn the user
        //                 ResourceManager resourceManager = new ResourceManager(
        //                                                                      "Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
        //                                                                      typeof(CreateLoginDatabaseAccess).Assembly);

        //                 string message = resourceManager.GetString("databaseAccess.error.inaccessibleDatabase");

        //                 this.DisplayExceptionInfoMessage(new Exception(message));
        //                 break;
        //             }
        //         }
        //     }

        //     this.RefreshDatabasesGrid();
        // }

        /// <summary>
		/// Set server roles for the login
		/// </summary>
		/// <param name="sender"></param>
		public void CreateLoginServerRoles_OnRunNow(object sender)
		{
			try
			{
				this.ExecutionMode	= ExecutionMode.Success;
				string loginName	= this.prototype.LoginName;
				
				if ((loginName == null) || (loginName.Length == 0))
				{
					ResourceManager resourceManager = new ResourceManager(
						"Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
						typeof(CreateLoginServerRoles).Assembly);

					string blankLogin = resourceManager.GetString("error.blankLogin");

					throw new SmoException(blankLogin);
				}

				this.prototype.ApplyServerRoleChanges(DataContainer.Server);
			}
			catch(Exception e)
			{
				DisplayExceptionMessage(e);

				this.ExecutionMode = ExecutionMode.Failure;
			}
		}

        /// <summary>
        /// Event handler for the "..." button that launches the NT login picker dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectNTLogin(object sender, System.EventArgs e)
        {
            try
            {
                this.browseWindowsLogins.Enabled = false;

                string windowsLoginName = CUtils.GetWindowsLoginNameFromObjectPicker(this,
                    this.DataContainer.Server,
                    resourceManager.GetString("general.error.tooManyNtLogins"));

                if (windowsLoginName != null)
                {
                    this.loginName.Text = windowsLoginName;
                }

            }
            finally
            {
                this.browseWindowsLogins.Enabled = true;
            }
        }

        #endregion
#endif        
    }
}