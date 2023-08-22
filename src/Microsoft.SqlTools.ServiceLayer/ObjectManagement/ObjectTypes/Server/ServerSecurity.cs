//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.SqlManagerUI
{
    /// <summary>
    /// class ServerPropSecurity.
    /// 
    /// implements dialog for (Server Properties) Security
    /// </summary>
    internal class ServerPropSecurity
    {
        #region Trace support
        private const string componentName = "ServerPropSecurity";

        public string ComponentName
        {
            get
            {
                return componentName;
            }
        }
        #endregion

        #region Constants
        const int SERVERPROP_SECURITY_AUTH_WINDOWS = 1;
        const int SERVERPROP_SECURITY_AUTH_MIXED = 2;
        const int SERVERPROP_SECURITY_AUDIT_NONE = 0;
        const int SERVERPROP_SECURITY_AUDIT_SUCCESS = 1;
        const int SERVERPROP_SECURITY_AUDIT_FAILURE = 2;
        const int SERVERPROP_SECURITY_AUDIT_BOTH = 3;
        #endregion

        #region Data Types
        private enum ServiceType
        {
            SqlServer = 0,
            SqlAgent = 1,
            SqlSearch = 2
        }
        #endregion

        #region Variables
        private System.Windows.Forms.Panel panelSecurity;
        private System.Windows.Forms.RadioButton radioButtonWindowsAuthentication;
        private System.Windows.Forms.RadioButton radioButtonMixedAuthentication;
        private System.Windows.Forms.RadioButton radioButtonAuditNone;
        private System.Windows.Forms.RadioButton radioButtonAuditFailed;
        private System.Windows.Forms.RadioButton radioButtonAuditSuccessful;
        private System.Windows.Forms.RadioButton radioButtonAuditBoth;

        private SMO.ServerLoginMode authenticationMode = SMO.ServerLoginMode.Integrated;
        private SMO.AuditLevel loginAuditing = SMO.AuditLevel.None;

        private SMO.ServerLoginMode authenticationModeInitial = SMO.ServerLoginMode.Integrated;
        private SMO.AuditLevel loginAuditingInitial = SMO.AuditLevel.None;
        #endregion

        #region ISupportValidation Members

        private bool flagSetSaPasswordDialogAlreadyOKed = false;
        private ChangeSAPassword formSAPass = null;
        bool ISupportValidation.Validate()
        {

            if (flagSetSaPasswordDialogAlreadyOKed == false) // if question never answered before (user didnt already OK-ed this "Set sa Password")
            {
                if (DataContainer.Server.Information.Version.Major < 9) // if pre-Yukon server (2000 or 7.0)
                {
                    if (
                       (this.authenticationModeInitial == SMO.ServerLoginMode.Integrated) &&
                       (this.authenticationMode == SMO.ServerLoginMode.Mixed) // if changed from NT Authentication to Mixed
                       )
                    {
                        if (DataContainer.Server.Logins.Contains("sa") && IsNullSaPassword()) // if is null sa password
                        {
                            // ask user for a password for sa account
                            DialogResult dr;
                            using (formSAPass = new ChangeSAPassword(DataContainer.Server, ServiceProvider))
                            {
                                dr = formSAPass.ShowDialog(this);
                            }

                            if (dr == DialogResult.OK)
                            {
                                flagSetSaPasswordDialogAlreadyOKed = true;
                            }
                            else
                            {
                                // dialog canceled - so no page switching and no "run now" execution
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool IsNullSaPassword()
        {
            System.Diagnostics.Debug.Assert(DataContainer != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server != null);
            System.Diagnostics.Debug.Assert(DataContainer.Server.Information.Version.Major < 9); // we check this only on pre Yukon servers

            System.Diagnostics.Debug.Assert(DataContainer.ServerConnection != null);
            Request req = new Request("Server/Information", new string[] { "HasNullSaPassword" });
            DataTable dt = Enumerator.GetData(DataContainer.ServerConnection, req);

            System.Diagnostics.Debug.Assert(dt != null);
            System.Diagnostics.Debug.Assert(dt.Rows.Count == 1);

            object o = dt.Rows[0][0];
            bool b = Convert.ToBoolean(o, System.Globalization.CultureInfo.InvariantCulture);

            return b;
        }

        #endregion

        #region Properties
        // public eAuthenticationMode AuthenticationMode
        public SMO.ServerLoginMode AuthenticationMode
        {
            get
            {
                return this.authenticationMode;
            }
            set
            {
                this.authenticationMode = value;
            }
        }

        public SMO.AuditLevel LoginAuditing
        {
            get
            {
                return this.loginAuditing;
            }
            set
            {
                this.loginAuditing = value;
            }
        }

        private bool m_boolAllowUpdates_cfg = false;
        private bool m_boolAllowUpdates_old = false;
        private bool m_boolAllowUpdates_run = false;
        private bool m_boolAllowUpdates_din = false;
        public bool AllowUpdates
        {
            get
            {
                return m_boolAllowUpdates_cfg;
            }
            set
            {
                m_boolAllowUpdates_cfg = value;
            }
        }

       
        }
        #endregion

        #region Implementation - Constructor / Load Data / Init Prop / Send Data to Server / Update UI
        /// <summary>
        /// ServerPropSecurity
        /// 
        /// constructor
        /// </summary>
        /// <param name="doc"></param>
        public ServerPropSecurity(CDataContainer context)
        {
            InitializeComponent();
            DataContainer = context;
        }

        /// <summary>
        ///  InitProp
        ///  
        ///  talks with enumerator and retrieves info
        /// </summary>
        private void InitProp()
        {
            Enumerator en = new Enumerator();

            Request req = new Request();
            req.Urn = "Server/Setting";
            req.Fields = new string[] { "LoginMode", "AuditLevel" }; // , "ServerAccount"

            DataSet ds = en.Process(ServerConnection, req);

            DataRow drServerInfo = ds.Tables[0].Rows[0];

            switch (Convert.ToInt16(drServerInfo["LoginMode"], System.Globalization.CultureInfo.InvariantCulture))
            {
                case SERVERPROP_SECURITY_AUTH_WINDOWS:
                    this.authenticationMode = SMO.ServerLoginMode.Integrated; // eAuthenticationMode.Integrated;
                    radioButtonWindowsAuthentication.Checked = true;
                    break;
                case SERVERPROP_SECURITY_AUTH_MIXED:
                    this.authenticationMode = SMO.ServerLoginMode.Mixed; // eAuthenticationMode.Mixed;
                    radioButtonMixedAuthentication.Checked = true;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "unhandled login mode");
                    panelServerAuthentication.Enabled = false;
                    break;
            }

            switch (Convert.ToInt16(drServerInfo["AuditLevel"], System.Globalization.CultureInfo.InvariantCulture))
            {
                case SERVERPROP_SECURITY_AUDIT_NONE:
                    this.loginAuditing = AuditLevel.None;
                    radioButtonAuditNone.Checked = true;
                    break;
                case SERVERPROP_SECURITY_AUDIT_SUCCESS:
                    this.loginAuditing = AuditLevel.Success;
                    radioButtonAuditSuccessful.Checked = true;
                    break;
                case SERVERPROP_SECURITY_AUDIT_FAILURE:
                    this.loginAuditing = AuditLevel.Failure;
                    radioButtonAuditFailed.Checked = true;
                    break;
                case SERVERPROP_SECURITY_AUDIT_BOTH:
                    this.loginAuditing = AuditLevel.All;
                    radioButtonAuditBoth.Checked = true;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "unhandled audit mode");
                    panelLoginAuditing.Enabled = false;
                    break;
            }

            req = new Request();
            req.Urn = "Server/Configuration";

            ds = en.Process(ServerConnection, req);

            foreach (DataRow drConfigInfo in ds.Tables[0].Rows)
            {
                string s = Convert.ToString(drConfigInfo["Name"], System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
                switch (s)
                {
                    case "allow updates":
                        try
                        {
                            m_boolAllowUpdates_cfg = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
                            m_boolAllowUpdates_run = Convert.ToBoolean(drConfigInfo["RunValue"], System.Globalization.CultureInfo.InvariantCulture);
                            m_boolAllowUpdates_din = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            this.checkBoxAllowDirectUpdates.Enabled = false;
                        }
                        break;
                    case "c2 audit mode":
                        try
                        {
                            m_boolC2AuditTracing_cfg = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
                            m_boolC2AuditTracing_run = Convert.ToBoolean(drConfigInfo["RunValue"], System.Globalization.CultureInfo.InvariantCulture);
                            m_boolC2AuditTracing_din = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            this.checkBoxEnableC2AuditTracing.Enabled = false;
                        }
                        break;
                    case "cross db ownership chaining":
                        try
                        {
                            this.crossDbChaining = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
                            this.crossDbChainingDynamic = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            this.checkBoxCrossDbChaining.Enabled = false;
                        }
                        break;
                    case "common criteria compliance enabled":
                        try
                        {
                            this.enableCommonCriteriaCompliance = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
                            this.enableCommonCriteriaComplianceIsDynamic = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            this.checkboxEnableCommonCriteria.Enabled = false;
                        }
                        break;
                }

            }

            UpdateConfig();


            // sqlbu# 397315 - we display '[ ] Allow Direct Updates checkbox' only for pre-Yukon servers
            // in Yukon the sp_configure option exists only for backward compatibility reasons, it is a noop
            checkBoxAllowDirectUpdates.Visible = (DataContainer.Server.Information.Version.Major < 9);

            // the enable common criteria checkbox should be visible only for Yukon SP2 or later (version > 9.0.3000)
            // on enterprise servers
            Version yukonSp2 = new Version(9, 0, 3000);

            if ((yukonSp2 <= DataContainer.Server.Information.Version) &&
                (DataContainer.Server.Information.EngineEdition == Edition.EnterpriseOrDeveloper))
            {
                this.checkboxEnableCommonCriteria.Visible = true;
                this.checkboxEnableCommonCriteria.Enabled = true;
            }
            else
            {
                this.checkboxEnableCommonCriteria.Visible = false;
            }

            SetInitialValues();
        }

        /// <summary>
        /// SetInitialValues
        /// 
        /// sets initial values that will be used OnReset and OnRunNow (to send only changed props to smo)
        /// </summary>
        private void SetInitialValues()
        {
            this.sqlProxyLoginInitial = this.sqlProxyLogin;
            this.sqlProxyAccountEnabledInitial = this.sqlProxyAccountEnabled;

            this.authenticationModeInitial = this.authenticationMode;
            this.loginAuditingInitial = this.loginAuditing;

            this.crossDbChainingInitial = this.crossDbChaining;
            this.enableCommonCriteriaComplianceInitial = this.enableCommonCriteriaCompliance;

            this.sqlProxyPassword = null;
        }


        private void ResetToInitialData()
        {
            this.sqlProxyLogin = this.sqlProxyLoginInitial;

            this.authenticationMode = this.authenticationModeInitial;
            this.loginAuditing = this.loginAuditingInitial;

            this.crossDbChaining = this.crossDbChainingInitial;

            this.sqlProxyPassword = null;
            this.sqlProxyPasswordChanged = false;

            AllowUpdates = m_boolAllowUpdates_old;
            C2AuditTracing = m_boolC2AuditTracing_old;
            EnableCommonCriteriaCompliance = this.enableCommonCriteriaComplianceInitial;
        }

        /// <summary>
        /// UpdateAuthentication
        /// 
        /// updates the radio controls dealing with Authentication
        /// </summary>
        private void UpdateAuthentication()
        {
            switch (this.authenticationMode)
            {
                case SMO.ServerLoginMode.Integrated: //eAuthenticationMode.Integrated:
                    radioButtonWindowsAuthentication.Checked = true;
                    break;
                case SMO.ServerLoginMode.Mixed: //eAuthenticationMode.Mixed:
                    radioButtonMixedAuthentication.Checked = true;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "unhandled login mode");
                    panelServerAuthentication.Enabled = false;
                    break;
            }
        }


        /// <summary>
        /// UpdateAudit
        /// 
        /// updates the radio controls dealing with Audit level
        /// </summary>
        private void UpdateAudit()
        {
            switch (this.loginAuditing)
            {
                case AuditLevel.None:
                    radioButtonAuditNone.Checked = true;
                    break;
                case AuditLevel.Failure:
                    radioButtonAuditFailed.Checked = true;
                    break;
                case AuditLevel.Success:
                    radioButtonAuditSuccessful.Checked = true;
                    break;
                case AuditLevel.All:
                    radioButtonAuditBoth.Checked = true;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "unhandled audit mode");
                    panelLoginAuditing.Enabled = false;
                    break;
            }
        }

        /// <summary>
        /// UpdateUI
        /// 
        /// resets all controls to old (initial) values
        /// </summary>
        private void UpdateUI()
        {
            UpdateAuthentication();
            UpdateAudit();
            UpdateConfig();
        }

        /// <summary>
        /// Update Config options
        /// </summary>
        private void UpdateConfig()
        {
            checkBoxCrossDbChaining.Checked = CrossDbChaining;
            checkBoxAllowDirectUpdates.Checked = AllowUpdates;
            checkBoxEnableC2AuditTracing.Checked = C2AuditTracing;
            checkboxEnableCommonCriteria.Checked = EnableCommonCriteriaCompliance;
        }

        /// <summary>
        /// SendDataToServer
        /// 
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        private void SendDataToServer()
        {

            bool bAlterServer = false;
            bool bNeedServerRestart = false;

            SMO.Server smoServer = DataContainer.Server;

            if (this.authenticationMode != this.authenticationModeInitial)
            {
                // when we switch to Mixed we have to ensure sa password is not null
                if (this.authenticationMode == SMO.ServerLoginMode.Mixed)
                {
                    // check for sa password was made on ISupportValidation
                    //
                    // formSAPass was created only if we detected null sa password
                    // here we only submit the password changes made in formSAPass
                    // to server
                    if (formSAPass != null)
                    {
                        System.Diagnostics.Debug.Assert(flagSetSaPasswordDialogAlreadyOKed, "sa password dialog has not been OK'd");

                        // if a password was 'OK'-ed we send it to server
                        // if empty password was 'OK'-ed nothing will be sent to server
                        formSAPass.SendNewSaPasswordToServer();
                    }
                }

                // set authentication
                smoServer.Settings.LoginMode = this.authenticationMode;
                bAlterServer = true;
                bNeedServerRestart = true;
            }

            if (this.loginAuditing != this.loginAuditingInitial)
            {
                smoServer.Settings.AuditLevel = this.loginAuditing;
                bAlterServer = true;
            }

            //take care of proxy account changes
            if (this.sqlProxyControlsEnabled)
            {
                bool shouldAlterProxyAccount = false;
                if (this.sqlProxyAccountEnabledInitial != this.sqlProxyAccountEnabled)
                {
                    smoServer.ProxyAccount.IsEnabled = this.sqlProxyAccountEnabled;
                    shouldAlterProxyAccount = true;
                }
                if (this.sqlProxyAccountEnabled)
                {
                    if (this.sqlProxyLoginInitial != this.sqlProxyLogin || this.sqlProxyPassword != null)
                    {
                        if (this.sqlProxyPassword == null)
                        {
                            this.sqlProxyPassword = string.Empty;
                        }
                        smoServer.ProxyAccount.SetAccount(this.sqlProxyLogin, (string)this.sqlProxyPassword);
                        shouldAlterProxyAccount = true;
                    }
                }
                if (shouldAlterProxyAccount)
                {
                    smoServer.ProxyAccount.Alter();
                }
            }

            bool bAlterServerConfig = false;
            if (m_boolAllowUpdates_cfg != m_boolAllowUpdates_old)
            {
                smoServer.Configuration.AllowUpdates.ConfigValue = m_boolAllowUpdates_cfg ? 1 : 0;
                bAlterServerConfig = true;
                bNeedServerRestart |= (m_boolAllowUpdates_din == false); // server requires restart if option is not dinamyc
            }

            if (bAlterServerConfig == true)
            {
                smoServer.Configuration.Alter(true);
            }

            if (bAlterServer == true)
            {
                smoServer.Alter();
            }

            if ((bNeedServerRestart == true) && !IsScripting(base.RunType))
            {
                // user is applying the changes right now (not scripting/scheduling)
                // and changes require server restart in order to be effective so
                // we warn the user about the required restart action (see also SQLBU# 355718)
                DisplayExceptionInfoMessage(new Exception(ServerPropSecuritySR.ServerNeedsToBeRestarted));
            }

        }
        #endregion

        #region Events

        private void radioButtonWindowsAuthentication_CheckedChanged(object sender, System.EventArgs e)
        {
            if (radioButtonWindowsAuthentication.Checked == true)
            {
                this.authenticationMode = SMO.ServerLoginMode.Integrated; // eAuthenticationMode.Integrated;
            }
        }

        private void radioButtonMixedAuthentication_CheckedChanged(object sender, System.EventArgs e)
        {
            if (radioButtonMixedAuthentication.Checked == true)
            {
                this.authenticationMode = SMO.ServerLoginMode.Mixed; //eAuthenticationMode.Mixed;
            }
        }

        private void radioButtonAuditNone_CheckedChanged(object sender, System.EventArgs e)
        {
            if (radioButtonAuditNone.Checked == true)
            {
                this.loginAuditing = AuditLevel.None;
            }
        }

        private void radioButtonAuditFailed_CheckedChanged(object sender, System.EventArgs e)
        {
            if (radioButtonAuditFailed.Checked == true)
            {
                this.loginAuditing = AuditLevel.Failure;
            }
        }

        private void radioButtonAuditSuccessful_CheckedChanged(object sender, System.EventArgs e)
        {
            if (radioButtonAuditSuccessful.Checked == true)
            {
                this.loginAuditing = AuditLevel.Success;
            }
        }

        private void radioButtonAuditBoth_CheckedChanged(object sender, System.EventArgs e)
        {
            if (radioButtonAuditBoth.Checked == true)
            {
                this.loginAuditing = AuditLevel.All;
            }
        }
        #endregion
    }
}