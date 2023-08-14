//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//using System;
//using System.Data;
//using Microsoft.SqlServer.Management.Common;
//using SMO = Microsoft.SqlServer.Management.Smo;
//using Microsoft.SqlServer.Management.Smo;

//namespace Microsoft.SqlServer.Management.SqlManagerUI
//{
//    /// <summary>
//    /// class ServerPropSecurity.
//    /// 
//    /// implements dialog for (Server Properties) Security
//    /// </summary>
//    internal class ServerPropSecurity
//    {
//        #region Trace support
//        private const string componentName = "ServerPropSecurity";

//        public string ComponentName
//        {
//            get
//            {
//                return componentName;
//            }
//        }
//        #endregion

//        #region Constants
//        const int SERVERPROP_SECURITY_AUTH_WINDOWS = 1;
//        const int SERVERPROP_SECURITY_AUTH_MIXED = 2;
//        const int SERVERPROP_SECURITY_AUDIT_NONE = 0;
//        const int SERVERPROP_SECURITY_AUDIT_SUCCESS = 1;
//        const int SERVERPROP_SECURITY_AUDIT_FAILURE = 2;
//        const int SERVERPROP_SECURITY_AUDIT_BOTH = 3;
//        #endregion

//        #region Data Types
//        private enum ServiceType
//        {
//            SqlServer = 0,
//            SqlAgent = 1,
//            SqlSearch = 2
//        }
//        #endregion

//        #region Variables
//        private System.Windows.Forms.Panel panelSecurity;
//        private System.Windows.Forms.RadioButton radioButtonWindowsAuthentication;
//        private System.Windows.Forms.RadioButton radioButtonMixedAuthentication;
//        private System.Windows.Forms.RadioButton radioButtonAuditNone;
//        private System.Windows.Forms.RadioButton radioButtonAuditFailed;
//        private System.Windows.Forms.RadioButton radioButtonAuditSuccessful;
//        private System.Windows.Forms.RadioButton radioButtonAuditBoth;

//        private SMO.ServerLoginMode authenticationMode = SMO.ServerLoginMode.Integrated;
//        private SMO.AuditLevel loginAuditing = SMO.AuditLevel.None;

//        private SMO.ServerLoginMode authenticationModeInitial = SMO.ServerLoginMode.Integrated;
//        private SMO.AuditLevel loginAuditingInitial = SMO.AuditLevel.None;
//        #endregion

//        #region ISupportValidation Members

//        private bool flagSetSaPasswordDialogAlreadyOKed = false;
//        private ChangeSAPassword formSAPass = null;
//        bool ISupportValidation.Validate()
//        {

//            if (flagSetSaPasswordDialogAlreadyOKed == false) // if question never answered before (user didnt already OK-ed this "Set sa Password")
//            {
//                if (DataContainer.Server.Information.Version.Major < 9) // if pre-Yukon server (2000 or 7.0)
//                {
//                    if (
//                       (this.authenticationModeInitial == SMO.ServerLoginMode.Integrated) &&
//                       (this.authenticationMode == SMO.ServerLoginMode.Mixed) // if changed from NT Authentication to Mixed
//                       )
//                    {
//                        if (DataContainer.Server.Logins.Contains("sa") && IsNullSaPassword()) // if is null sa password
//                        {
//                            // ask user for a password for sa account
//                            DialogResult dr;
//                            using (formSAPass = new ChangeSAPassword(DataContainer.Server, ServiceProvider))
//                            {
//                                dr = formSAPass.ShowDialog(this);
//                            }

//                            if (dr == DialogResult.OK)
//                            {
//                                flagSetSaPasswordDialogAlreadyOKed = true;
//                            }
//                            else
//                            {
//                                // dialog canceled - so no page switching and no "run now" execution
//                                return false;
//                            }
//                        }
//                    }
//                }
//            }
//            return true;
//        }

//        private bool IsNullSaPassword()
//        {
//            System.Diagnostics.Debug.Assert(DataContainer != null);
//            System.Diagnostics.Debug.Assert(DataContainer.Server != null);
//            System.Diagnostics.Debug.Assert(DataContainer.Server.Information.Version.Major < 9); // we check this only on pre Yukon servers

//            System.Diagnostics.Debug.Assert(DataContainer.ServerConnection != null);
//            Request req = new Request("Server/Information", new string[] { "HasNullSaPassword" });
//            DataTable dt = Enumerator.GetData(DataContainer.ServerConnection, req);

//            System.Diagnostics.Debug.Assert(dt != null);
//            System.Diagnostics.Debug.Assert(dt.Rows.Count == 1);

//            object o = dt.Rows[0][0];
//            bool b = Convert.ToBoolean(o, System.Globalization.CultureInfo.InvariantCulture);

//            return b;
//        }

//        #endregion

//        #region Properties
//        // public eAuthenticationMode AuthenticationMode
//        public SMO.ServerLoginMode AuthenticationMode
//        {
//            get
//            {
//                return this.authenticationMode;
//            }
//            set
//            {
//                this.authenticationMode = value;
//            }
//        }

//        public SMO.AuditLevel LoginAuditing
//        {
//            get
//            {
//                return this.loginAuditing;
//            }
//            set
//            {
//                this.loginAuditing = value;
//            }
//        }

//        public bool CrossDbChaining
//        {
//            get
//            {
//                return this.crossDbChaining;
//            }
//            set
//            {
//                this.crossDbChaining = value;
//            }
//        }

//        private bool m_boolAllowUpdates_cfg = false;
//        private bool m_boolAllowUpdates_old = false;
//        private bool m_boolAllowUpdates_run = false;
//        private bool m_boolAllowUpdates_din = false;
//        public bool AllowUpdates
//        {
//            get
//            {
//                return m_boolAllowUpdates_cfg;
//            }
//            set
//            {
//                m_boolAllowUpdates_cfg = value;
//            }
//        }

//        private bool m_boolC2AuditTracing_cfg = false;
//        private bool m_boolC2AuditTracing_old = false;
//        private bool m_boolC2AuditTracing_run = false;
//        private bool m_boolC2AuditTracing_din = false;
//        public bool C2AuditTracing
//        {
//            get
//            {
//                return m_boolC2AuditTracing_cfg;
//            }
//            set
//            {
//                m_boolC2AuditTracing_cfg = value;
//            }
//        }

//        private bool enableCommonCriteriaCompliance = false;
//        private bool enableCommonCriteriaComplianceInitial = false;
//        private bool enableCommonCriteriaComplianceIsDynamic = false;
//        public bool EnableCommonCriteriaCompliance
//        {
//            get
//            {
//                return this.enableCommonCriteriaCompliance;
//            }
//            set
//            {
//                this.enableCommonCriteriaCompliance = value;
//            }
//        }
//        #endregion

//        #region Implementation - Constructor / Load Data / Init Prop / Send Data to Server / Update UI
//        /// <summary>
//        /// ServerPropSecurity
//        /// 
//        /// constructor
//        /// </summary>
//        /// <param name="doc"></param>
//        public ServerPropSecurity(CDataContainer context)
//        {


//            InitializeComponent();
//            DataContainer = context;
//        }

//        /// <summary>
//        ///  InitProp
//        ///  
//        ///  talks with enumerator and retrieves info
//        /// </summary>
//        private void InitProp()
//        {


//            Enumerator en = new Enumerator();

//            Request req = new Request();
//            req.Urn = "Server/Setting";
//            req.Fields = new string[] { "LoginMode", "AuditLevel" }; // , "ServerAccount"

//            DataSet ds = en.Process(ServerConnection, req);

//            DataRow drServerInfo = ds.Tables[0].Rows[0];

//            switch (Convert.ToInt16(drServerInfo["LoginMode"], System.Globalization.CultureInfo.InvariantCulture))
//            {
//                case SERVERPROP_SECURITY_AUTH_WINDOWS:
//                    this.authenticationMode = SMO.ServerLoginMode.Integrated; // eAuthenticationMode.Integrated;
//                    radioButtonWindowsAuthentication.Checked = true;
//                    break;
//                case SERVERPROP_SECURITY_AUTH_MIXED:
//                    this.authenticationMode = SMO.ServerLoginMode.Mixed; // eAuthenticationMode.Mixed;
//                    radioButtonMixedAuthentication.Checked = true;
//                    break;
//                default:
//                    System.Diagnostics.Debug.Assert(false, "unhandled login mode");
//                    panelServerAuthentication.Enabled = false;
//                    break;
//            }

//            // We can't change the auth mode on non-Windows instances
//            if (DataContainer.Server.HostPlatform != HostPlatformNames.Windows)
//            {
//                radioButtonWindowsAuthentication.Enabled = false;
//                radioButtonMixedAuthentication.Enabled = false;
//            }

//            switch (Convert.ToInt16(drServerInfo["AuditLevel"], System.Globalization.CultureInfo.InvariantCulture))
//            {
//                case SERVERPROP_SECURITY_AUDIT_NONE:
//                    this.loginAuditing = AuditLevel.None;
//                    radioButtonAuditNone.Checked = true;
//                    break;
//                case SERVERPROP_SECURITY_AUDIT_SUCCESS:
//                    this.loginAuditing = AuditLevel.Success;
//                    radioButtonAuditSuccessful.Checked = true;
//                    break;
//                case SERVERPROP_SECURITY_AUDIT_FAILURE:
//                    this.loginAuditing = AuditLevel.Failure;
//                    radioButtonAuditFailed.Checked = true;
//                    break;
//                case SERVERPROP_SECURITY_AUDIT_BOTH:
//                    this.loginAuditing = AuditLevel.All;
//                    radioButtonAuditBoth.Checked = true;
//                    break;
//                default:
//                    System.Diagnostics.Debug.Assert(false, "unhandled audit mode");
//                    panelLoginAuditing.Enabled = false;
//                    break;
//            }

//            // Special handling for Managed Instances.
//            //
//            if (this.ServerConnection.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
//            {
//                radioButtonWindowsAuthentication.Enabled = false;
//                radioButtonMixedAuthentication.Enabled = false;
//            }

//            req = new Request();
//            req.Urn = "Server/Configuration";

//            ds = en.Process(ServerConnection, req);

//            foreach (DataRow drConfigInfo in ds.Tables[0].Rows)
//            {
//                string s = Convert.ToString(drConfigInfo["Name"], System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
//                switch (s)
//                {
//                    case "allow updates":
//                        try
//                        {
//                            m_boolAllowUpdates_cfg = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
//                            m_boolAllowUpdates_run = Convert.ToBoolean(drConfigInfo["RunValue"], System.Globalization.CultureInfo.InvariantCulture);
//                            m_boolAllowUpdates_din = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
//                        }
//                        catch
//                        {
//                            this.checkBoxAllowDirectUpdates.Enabled = false;
//                        }
//                        break;
//                    case "c2 audit mode":
//                        try
//                        {
//                            m_boolC2AuditTracing_cfg = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
//                            m_boolC2AuditTracing_run = Convert.ToBoolean(drConfigInfo["RunValue"], System.Globalization.CultureInfo.InvariantCulture);
//                            m_boolC2AuditTracing_din = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
//                        }
//                        catch
//                        {
//                            this.checkBoxEnableC2AuditTracing.Enabled = false;
//                        }
//                        break;
//                    case "cross db ownership chaining":
//                        try
//                        {
//                            this.crossDbChaining = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
//                            this.crossDbChainingDynamic = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
//                        }
//                        catch
//                        {
//                            this.checkBoxCrossDbChaining.Enabled = false;
//                        }
//                        break;
//                    case "common criteria compliance enabled":
//                        try
//                        {
//                            this.enableCommonCriteriaCompliance = Convert.ToBoolean(drConfigInfo["ConfigValue"], System.Globalization.CultureInfo.InvariantCulture);
//                            this.enableCommonCriteriaComplianceIsDynamic = Convert.ToBoolean(drConfigInfo["Dynamic"], System.Globalization.CultureInfo.InvariantCulture);
//                        }
//                        catch
//                        {
//                            this.checkboxEnableCommonCriteria.Enabled = false;
//                        }
//                        break;
//                }

//            }


//            m_boolAllowUpdates_old = m_boolAllowUpdates_cfg;
//            m_boolC2AuditTracing_old = m_boolC2AuditTracing_cfg;
//            this.crossDbChainingInitial = this.crossDbChaining;

//            UpdateConfig();


//            if (DataContainer.Server.Version.Major < 9 || DataContainer.Server.HostPlatform != HostPlatformNames.Windows)
//            {
//                this.sqlProxyControlsEnabled = false;
//                ShowHideProxyAccountControls(false);
//            }
//            else
//            {
//                this.sqlProxyControlsEnabled = true;
//                this.sqlProxyAccountEnabled = DataContainer.Server.ProxyAccount.IsEnabled;
//                if (this.sqlProxyAccountEnabled)
//                {
//                    this.proxyAccountEnabledCheckBox.Checked = true;
//                    this.sqlProxyLogin = DataContainer.Server.ProxyAccount.WindowsAccount;

//                    this.proxyAccountTextBox.Text = this.sqlProxyLogin;
//                    this.proxyPasswordTextBox.Text = proxyPasswordTextBoxMaskText;
//                }
//                else
//                {
//                    this.proxyAccountEnabledCheckBox.Checked = false;
//                    EnableDisableProxyAccountControls(false);
//                }
//                this.sqlProxyPasswordChanged = false;


//            }

//            // sqlbu# 397315 - we display '[ ] Allow Direct Updates checkbox' only for pre-Yukon servers
//            // in Yukon the sp_configure option exists only for backward compatibility reasons, it is a noop
//            checkBoxAllowDirectUpdates.Visible = (DataContainer.Server.Information.Version.Major < 9);

//            // the enable common criteria checkbox should be visible only for Yukon SP2 or later (version > 9.0.3000)
//            // on enterprise servers
//            Version yukonSp2 = new Version(9, 0, 3000);

//            if ((yukonSp2 <= DataContainer.Server.Information.Version) &&
//                (DataContainer.Server.Information.EngineEdition == Edition.EnterpriseOrDeveloper))
//            {
//                this.checkboxEnableCommonCriteria.Visible = true;
//                this.checkboxEnableCommonCriteria.Enabled = true;
//            }
//            else
//            {
//                this.checkboxEnableCommonCriteria.Visible = false;
//            }

//            SetInitialValues();
//        }

//        /// <summary>
//        /// SetInitialValues
//        /// 
//        /// sets initial values that will be used OnReset and OnRunNow (to send only changed props to smo)
//        /// </summary>
//        private void SetInitialValues()
//        {
//            this.sqlProxyLoginInitial = this.sqlProxyLogin;
//            this.sqlProxyAccountEnabledInitial = this.sqlProxyAccountEnabled;

//            this.authenticationModeInitial = this.authenticationMode;
//            this.loginAuditingInitial = this.loginAuditing;

//            this.crossDbChainingInitial = this.crossDbChaining;
//            this.enableCommonCriteriaComplianceInitial = this.enableCommonCriteriaCompliance;

//            this.sqlProxyPassword = null;
//        }


//        private void ResetToInitialData()
//        {
//            this.sqlProxyLogin = this.sqlProxyLoginInitial;

//            this.authenticationMode = this.authenticationModeInitial;
//            this.loginAuditing = this.loginAuditingInitial;

//            this.crossDbChaining = this.crossDbChainingInitial;

//            this.sqlProxyPassword = null;
//            this.sqlProxyPasswordChanged = false;

//            AllowUpdates = m_boolAllowUpdates_old;
//            C2AuditTracing = m_boolC2AuditTracing_old;
//            EnableCommonCriteriaCompliance = this.enableCommonCriteriaComplianceInitial;
//        }

//        /// <summary>
//        /// UpdateAuthentication
//        /// 
//        /// updates the radio controls dealing with Authentication
//        /// </summary>
//        private void UpdateAuthentication()
//        {
//            switch (this.authenticationMode)
//            {
//                case SMO.ServerLoginMode.Integrated: //eAuthenticationMode.Integrated:
//                    radioButtonWindowsAuthentication.Checked = true;
//                    break;
//                case SMO.ServerLoginMode.Mixed: //eAuthenticationMode.Mixed:
//                    radioButtonMixedAuthentication.Checked = true;
//                    break;
//                default:
//                    System.Diagnostics.Debug.Assert(false, "unhandled login mode");
//                    panelServerAuthentication.Enabled = false;
//                    break;
//            }
//        }


//        /// <summary>
//        /// UpdateAudit
//        /// 
//        /// updates the radio controls dealing with Audit level
//        /// </summary>
//        private void UpdateAudit()
//        {
//            switch (this.loginAuditing)
//            {
//                case AuditLevel.None:
//                    radioButtonAuditNone.Checked = true;
//                    break;
//                case AuditLevel.Failure:
//                    radioButtonAuditFailed.Checked = true;
//                    break;
//                case AuditLevel.Success:
//                    radioButtonAuditSuccessful.Checked = true;
//                    break;
//                case AuditLevel.All:
//                    radioButtonAuditBoth.Checked = true;
//                    break;
//                default:
//                    System.Diagnostics.Debug.Assert(false, "unhandled audit mode");
//                    panelLoginAuditing.Enabled = false;
//                    break;
//            }
//        }

//        /// <summary>
//        /// UpdateUI
//        /// 
//        /// resets all controls to old (initial) values
//        /// </summary>
//        private void UpdateUI()
//        {
//            UpdateAuthentication();
//            UpdateAudit();
//            UpdateConfig();
//        }

//        /// <summary>
//        /// Update Config options
//        /// </summary>
//        private void UpdateConfig()
//        {
//            checkBoxCrossDbChaining.Checked = CrossDbChaining;
//            checkBoxAllowDirectUpdates.Checked = AllowUpdates;
//            checkBoxEnableC2AuditTracing.Checked = C2AuditTracing;
//            checkboxEnableCommonCriteria.Checked = EnableCommonCriteriaCompliance;
//        }

//        /// <summary>
//        /// SendDataToServer
//        /// 
//        /// here we talk with server via smo and do the actual data changing
//        /// </summary>
//        private void SendDataToServer()
//        {

//            bool bAlterServer = false;
//            bool bNeedServerRestart = false;

//            SMO.Server smoServer = DataContainer.Server;

//            if (this.authenticationMode != this.authenticationModeInitial)
//            {
//                // when we switch to Mixed we have to ensure sa password is not null
//                if (this.authenticationMode == SMO.ServerLoginMode.Mixed)
//                {
//                    // check for sa password was made on ISupportValidation
//                    //
//                    // formSAPass was created only if we detected null sa password
//                    // here we only submit the password changes made in formSAPass
//                    // to server
//                    if (formSAPass != null)
//                    {
//                        System.Diagnostics.Debug.Assert(flagSetSaPasswordDialogAlreadyOKed, "sa password dialog has not been OK'd");

//                        // if a password was 'OK'-ed we send it to server
//                        // if empty password was 'OK'-ed nothing will be sent to server
//                        formSAPass.SendNewSaPasswordToServer();
//                    }
//                }

//                // set authentication
//                smoServer.Settings.LoginMode = this.authenticationMode;
//                bAlterServer = true;
//                bNeedServerRestart = true;
//            }

//            if (this.loginAuditing != this.loginAuditingInitial)
//            {
//                smoServer.Settings.AuditLevel = this.loginAuditing;
//                bAlterServer = true;
//            }

//            //take care of proxy account changes
//            if (this.sqlProxyControlsEnabled)
//            {
//                bool shouldAlterProxyAccount = false;
//                if (this.sqlProxyAccountEnabledInitial != this.sqlProxyAccountEnabled)
//                {
//                    smoServer.ProxyAccount.IsEnabled = this.sqlProxyAccountEnabled;
//                    shouldAlterProxyAccount = true;
//                }
//                if (this.sqlProxyAccountEnabled)
//                {
//                    if (this.sqlProxyLoginInitial != this.sqlProxyLogin || this.sqlProxyPassword != null)
//                    {
//                        if (this.sqlProxyPassword == null)
//                        {
//                            this.sqlProxyPassword = string.Empty;
//                        }
//                        smoServer.ProxyAccount.SetAccount(this.sqlProxyLogin, (string)this.sqlProxyPassword);
//                        shouldAlterProxyAccount = true;
//                    }
//                }
//                if (shouldAlterProxyAccount)
//                {
//                    smoServer.ProxyAccount.Alter();
//                }
//            }

//            bool bAlterServerConfig = false;
//            if (m_boolAllowUpdates_cfg != m_boolAllowUpdates_old)
//            {
//                smoServer.Configuration.AllowUpdates.ConfigValue = m_boolAllowUpdates_cfg ? 1 : 0;
//                bAlterServerConfig = true;
//                bNeedServerRestart |= (m_boolAllowUpdates_din == false); // server requires restart if option is not dinamyc
//            }

//            if (m_boolC2AuditTracing_cfg != m_boolC2AuditTracing_old)
//            {
//                smoServer.Configuration.C2AuditMode.ConfigValue = m_boolC2AuditTracing_cfg ? 1 : 0;
//                bAlterServerConfig = true;
//                bNeedServerRestart |= (m_boolC2AuditTracing_din == false); // server requires restart if option is not dinamyc
//            }

//            if (this.crossDbChaining != this.crossDbChainingInitial)
//            {
//                smoServer.Configuration.CrossDBOwnershipChaining.ConfigValue = this.crossDbChaining ? 1 : 0;
//                bAlterServerConfig = true;
//                bNeedServerRestart |= (this.crossDbChainingDynamic == false); // server requires restart if option is not dinamyc
//            }

//            if (this.enableCommonCriteriaCompliance != this.enableCommonCriteriaComplianceInitial)
//            {
//                smoServer.Configuration.CommonCriteriaComplianceEnabled.ConfigValue = this.enableCommonCriteriaCompliance ? 1 : 0;
//                bAlterServerConfig = true;
//                bNeedServerRestart |= (this.enableCommonCriteriaComplianceIsDynamic == false);// server requires restart if option is not dynamic
//            }

//            if (bAlterServerConfig == true)
//            {
//                smoServer.Configuration.Alter(true);
//            }

//            if (bAlterServer == true)
//            {
//                smoServer.Alter();
//            }

//            if ((bNeedServerRestart == true) && !IsScripting(base.RunType))
//            {
//                // user is applying the changes right now (not scripting/scheduling)
//                // and changes require server restart in order to be effective so
//                // we warn the user about the required restart action (see also SQLBU# 355718)
//                DisplayExceptionInfoMessage(new Exception(ServerPropSecuritySR.ServerNeedsToBeRestarted));
//            }

//        }
//        #endregion

//        #region Windows Form Designer generated code
//        /// <summary>
//        /// Required method for Designer support - do not modify
//        /// the contents of this method with the code editor.
//        /// </summary>
//        private void InitializeComponent()
//        {
//            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerPropSecurity));
//            this.radioButtonAuditNone = new System.Windows.Forms.RadioButton();
//            this.radioButtonAuditBoth = new System.Windows.Forms.RadioButton();
//            this.radioButtonMixedAuthentication = new System.Windows.Forms.RadioButton();
//            this.radioButtonWindowsAuthentication = new System.Windows.Forms.RadioButton();
//            this.radioButtonAuditSuccessful = new System.Windows.Forms.RadioButton();
//            this.radioButtonAuditFailed = new System.Windows.Forms.RadioButton();
//            this.panelSecurity = new System.Windows.Forms.Panel();
//            this.checkboxEnableCommonCriteria = new System.Windows.Forms.CheckBox();
//            this.checkBoxCrossDbChaining = new System.Windows.Forms.CheckBox();
//            this.checkBoxEnableC2AuditTracing = new System.Windows.Forms.CheckBox();
//            this.checkBoxAllowDirectUpdates = new System.Windows.Forms.CheckBox();
//            this.separatorOptions = new Microsoft.SqlServer.Management.Controls.Separator();
//            this.proxyAccountEnabledCheckBox = new System.Windows.Forms.CheckBox();
//            this.panelLoginAuditing = new System.Windows.Forms.Panel();
//            this.separatorLoginAuditing = new Microsoft.SqlServer.Management.Controls.Separator();
//            this.panelServerAuthentication = new System.Windows.Forms.Panel();
//            this.separatorServerAuthentication = new Microsoft.SqlServer.Management.Controls.Separator();
//            this.proxyAccountPasswordLabel = new System.Windows.Forms.Label();
//            this.separatorServiceProxy = new Microsoft.SqlServer.Management.Controls.Separator();
//            this.proxyAccountTextBox = new System.Windows.Forms.TextBox();
//            this.proxyAccountBrowseButton = new System.Windows.Forms.Button();
//            this.proxyPasswordTextBox = new System.Windows.Forms.TextBox();
//            this.proxyAccoutLabel = new System.Windows.Forms.Label();
//            this.panelSecurity.SuspendLayout();
//            this.panelLoginAuditing.SuspendLayout();
//            this.panelServerAuthentication.SuspendLayout();
//            this.SuspendLayout();
//            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//            // 
//            // radioButtonAuditNone
//            // 
//            resources.ApplyResources(this.radioButtonAuditNone, "radioButtonAuditNone");
//            this.radioButtonAuditNone.Name = "radioButtonAuditNone";
//            this.radioButtonAuditNone.CheckedChanged += new System.EventHandler(this.radioButtonAuditNone_CheckedChanged);
//            // 
//            // radioButtonAuditBoth
//            // 
//            resources.ApplyResources(this.radioButtonAuditBoth, "radioButtonAuditBoth");
//            this.radioButtonAuditBoth.Name = "radioButtonAuditBoth";
//            this.radioButtonAuditBoth.CheckedChanged += new System.EventHandler(this.radioButtonAuditBoth_CheckedChanged);
//            // 
//            // radioButtonMixedAuthentication
//            // 
//            resources.ApplyResources(this.radioButtonMixedAuthentication, "radioButtonMixedAuthentication");
//            this.radioButtonMixedAuthentication.Name = "radioButtonMixedAuthentication";
//            this.radioButtonMixedAuthentication.CheckedChanged += new System.EventHandler(this.radioButtonMixedAuthentication_CheckedChanged);
//            // 
//            // radioButtonWindowsAuthentication
//            // 
//            resources.ApplyResources(this.radioButtonWindowsAuthentication, "radioButtonWindowsAuthentication");
//            this.radioButtonWindowsAuthentication.Name = "radioButtonWindowsAuthentication";
//            this.radioButtonWindowsAuthentication.CheckedChanged += new System.EventHandler(this.radioButtonWindowsAuthentication_CheckedChanged);
//            // 
//            // radioButtonAuditSuccessful
//            // 
//            resources.ApplyResources(this.radioButtonAuditSuccessful, "radioButtonAuditSuccessful");
//            this.radioButtonAuditSuccessful.Name = "radioButtonAuditSuccessful";
//            this.radioButtonAuditSuccessful.CheckedChanged += new System.EventHandler(this.radioButtonAuditSuccessful_CheckedChanged);
//            // 
//            // radioButtonAuditFailed
//            // 
//            resources.ApplyResources(this.radioButtonAuditFailed, "radioButtonAuditFailed");
//            this.radioButtonAuditFailed.Name = "radioButtonAuditFailed";
//            this.radioButtonAuditFailed.CheckedChanged += new System.EventHandler(this.radioButtonAuditFailed_CheckedChanged);
//            // 
//            // panelSecurity
//            // 
//            resources.ApplyResources(this.panelSecurity, "panelSecurity");
//            this.panelSecurity.Controls.Add(this.panelServerAuthentication);
//            this.panelSecurity.Controls.Add(this.panelLoginAuditing);
//            this.panelSecurity.Controls.Add(this.separatorServiceProxy);
//            this.panelSecurity.Controls.Add(this.proxyAccountEnabledCheckBox);
//            this.panelSecurity.Controls.Add(this.proxyAccoutLabel);
//            this.panelSecurity.Controls.Add(this.proxyAccountTextBox);
//            this.panelSecurity.Controls.Add(this.proxyAccountBrowseButton);
//            this.panelSecurity.Controls.Add(this.proxyAccountPasswordLabel);
//            this.panelSecurity.Controls.Add(this.proxyPasswordTextBox);
//            this.panelSecurity.Controls.Add(this.separatorOptions);
//            this.panelSecurity.Controls.Add(this.checkboxEnableCommonCriteria);
//            this.panelSecurity.Controls.Add(this.checkBoxEnableC2AuditTracing);
//            this.panelSecurity.Controls.Add(this.checkBoxCrossDbChaining);
//            this.panelSecurity.Controls.Add(this.checkBoxAllowDirectUpdates);
//            this.panelSecurity.Name = "panelSecurity";
//            // 
//            // checkboxEnableCommonCriteria
//            // 
//            resources.ApplyResources(this.checkboxEnableCommonCriteria, "checkboxEnableCommonCriteria");
//            this.checkboxEnableCommonCriteria.Name = "checkboxEnableCommonCriteria";
//            this.checkboxEnableCommonCriteria.UseVisualStyleBackColor = true;
//            // 
//            // checkBoxCrossDbChaining
//            // 
//            resources.ApplyResources(this.checkBoxCrossDbChaining, "checkBoxCrossDbChaining");
//            this.checkBoxCrossDbChaining.Name = "checkBoxCrossDbChaining";
//            this.checkBoxCrossDbChaining.UseVisualStyleBackColor = true;
//            // 
//            // checkBoxEnableC2AuditTracing
//            // 
//            resources.ApplyResources(this.checkBoxEnableC2AuditTracing, "checkBoxEnableC2AuditTracing");
//            this.checkBoxEnableC2AuditTracing.Name = "checkBoxEnableC2AuditTracing";
//            // 
//            // checkBoxAllowDirectUpdates
//            // 
//            resources.ApplyResources(this.checkBoxAllowDirectUpdates, "checkBoxAllowDirectUpdates");
//            this.checkBoxAllowDirectUpdates.Name = "checkBoxAllowDirectUpdates";
//            // 
//            // separatorOptions
//            // 
//            resources.ApplyResources(this.separatorOptions, "separatorOptions");
//            this.separatorOptions.Name = "separatorOptions";
//            // 
//            // proxyAccountEnabledCheckBox
//            // 
//            resources.ApplyResources(this.proxyAccountEnabledCheckBox, "proxyAccountEnabledCheckBox");
//            this.proxyAccountEnabledCheckBox.Name = "proxyAccountEnabledCheckBox";
//            this.proxyAccountEnabledCheckBox.CheckedChanged += new System.EventHandler(this.proxyAccountEnabledCheckBox_CheckedChanged);
//            // 
//            // panelLoginAuditing
//            // 
//            resources.ApplyResources(this.panelLoginAuditing, "panelLoginAuditing");
//            this.panelLoginAuditing.Controls.Add(this.separatorLoginAuditing);
//            this.panelLoginAuditing.Controls.Add(this.radioButtonAuditNone);
//            this.panelLoginAuditing.Controls.Add(this.radioButtonAuditFailed);
//            this.panelLoginAuditing.Controls.Add(this.radioButtonAuditSuccessful);
//            this.panelLoginAuditing.Controls.Add(this.radioButtonAuditBoth);
//            this.panelLoginAuditing.Name = "panelLoginAuditing";
//            // 
//            // separatorLoginAuditing
//            // 
//            resources.ApplyResources(this.separatorLoginAuditing, "separatorLoginAuditing");
//            this.separatorLoginAuditing.Name = "separatorLoginAuditing";
//            // 
//            // panelServerAuthentication
//            // 
//            resources.ApplyResources(this.panelServerAuthentication, "panelServerAuthentication");
//            this.panelServerAuthentication.Controls.Add(this.separatorServerAuthentication);
//            this.panelServerAuthentication.Controls.Add(this.radioButtonWindowsAuthentication);
//            this.panelServerAuthentication.Controls.Add(this.radioButtonMixedAuthentication);
//            this.panelServerAuthentication.Name = "panelServerAuthentication";
//            // 
//            // separatorServerAuthentication
//            // 
//            resources.ApplyResources(this.separatorServerAuthentication, "separatorServerAuthentication");
//            this.separatorServerAuthentication.Name = "separatorServerAuthentication";
//            // 
//            // proxyAccountPasswordLabel
//            // 
//            resources.ApplyResources(this.proxyAccountPasswordLabel, "proxyAccountPasswordLabel");
//            this.proxyAccountPasswordLabel.Name = "proxyAccountPasswordLabel";
//            // 
//            // separatorServiceProxy
//            // 
//            resources.ApplyResources(this.separatorServiceProxy, "separatorServiceProxy");
//            this.separatorServiceProxy.Name = "separatorServiceProxy";
//            // 
//            // proxyAccountTextBox
//            // 
//            resources.ApplyResources(this.proxyAccountTextBox, "proxyAccountTextBox");
//            this.proxyAccountTextBox.Name = "proxyAccountTextBox";
//            // 
//            // proxyAccountBrowseButton
//            // 
//            resources.ApplyResources(this.proxyAccountBrowseButton, "proxyAccountBrowseButton");
//            this.proxyAccountBrowseButton.Name = "proxyAccountBrowseButton";
//            this.proxyAccountBrowseButton.Click += new System.EventHandler(this.proxyAccountBrowseButton_Click);
//            // 
//            // proxyPasswordTextBox
//            // 
//            resources.ApplyResources(this.proxyPasswordTextBox, "proxyPasswordTextBox");
//            this.proxyPasswordTextBox.Name = "proxyPasswordTextBox";
//            this.proxyPasswordTextBox.Enter += new System.EventHandler(this.proxyPasswordTextBox_Enter);
//            this.proxyPasswordTextBox.Leave += new System.EventHandler(this.proxyPasswordTextBox_Leave);
//            this.proxyPasswordTextBox.TextChanged += new System.EventHandler(this.proxyPasswordTextBox_TextChanged);
//            // 
//            // proxyAccoutLabel
//            // 
//            resources.ApplyResources(this.proxyAccoutLabel, "proxyAccoutLabel");
//            this.proxyAccoutLabel.Name = "proxyAccoutLabel";
//            // 
//            // ServerPropSecurity
//            // 
//            this.Controls.Add(this.panelSecurity);
//            this.Name = "ServerPropSecurity";
//            resources.ApplyResources(this, "$this");
//            this.panelSecurity.ResumeLayout(false);
//            this.panelSecurity.PerformLayout();
//            this.panelLoginAuditing.ResumeLayout(false);
//            this.panelServerAuthentication.ResumeLayout(false);
//            this.ResumeLayout(false);

//        }
//        #endregion

//        #region Events
//        /// <summary>
//        /// helper property that returns name of the target machine that Object Picker should 
//        /// be pointed to
//        /// </summary>
//        private string MachineNameForObjectPicker
//        {
//            get
//            {
//                if (this.mnameForOP == null)
//                {
//                    string sqlServerName = DataContainer.Server.Name;
//                    this.mnameForOP = sqlServerName;

//                    // Determine if we work with the default instance
//                    if (0 != DataContainer.Server.InstanceName.Length)
//                    {
//                        /// we work with a named instance 
//                        this.mnameForOP = (sqlServerName.Split('\\'))[0];
//                    }
//                }
//                System.Diagnostics.Debug.Assert(this.mnameForOP != null);
//                return this.mnameForOP;
//            }
//        }

//        private void proxyAccountBrowseButton_Click(object sender, System.EventArgs e)
//        {
//            try
//            {
//                this.proxyAccountTextBox.Focus();
//                proxyAccountBrowseButton.Enabled = false;

//                ObjectPickerWrapper.TargetMachine = MachineNameForObjectPicker;
//                ObjectPickerWrapper.SingleObjectSelection = true;
//                ObjectPickerWrapper.GetUsersList(this);

//                this.sqlProxyLogin = this.proxyAccountTextBox.Text;
//                string strOldProxyLogin = this.sqlProxyLogin;
//                if (ObjectPickerWrapper.UsersList.Count != 0)
//                {
//                    this.sqlProxyLogin = Convert.ToString(ObjectPickerWrapper.UsersList[0], System.Globalization.CultureInfo.CurrentCulture); // pick only 1st selection
//                    this.proxyAccountTextBox.Text = this.sqlProxyLogin;
//                    if (strOldProxyLogin != this.sqlProxyLogin)
//                    {
//                        this.proxyPasswordTextBox.Focus(); // give user a change to enter passoword for this account
//                    }
//                }

//            }
//            finally
//            {
//                proxyAccountBrowseButton.Enabled = true;
//            }
//        }

//        private void radioButtonWindowsAuthentication_CheckedChanged(object sender, System.EventArgs e)
//        {
//            if (radioButtonWindowsAuthentication.Checked == true)
//            {
//                this.authenticationMode = SMO.ServerLoginMode.Integrated; // eAuthenticationMode.Integrated;
//            }
//        }

//        private void radioButtonMixedAuthentication_CheckedChanged(object sender, System.EventArgs e)
//        {
//            if (radioButtonMixedAuthentication.Checked == true)
//            {
//                this.authenticationMode = SMO.ServerLoginMode.Mixed; //eAuthenticationMode.Mixed;
//            }
//        }

//        private void radioButtonAuditNone_CheckedChanged(object sender, System.EventArgs e)
//        {
//            if (radioButtonAuditNone.Checked == true)
//            {
//                this.loginAuditing = AuditLevel.None;
//            }
//        }

//        private void radioButtonAuditFailed_CheckedChanged(object sender, System.EventArgs e)
//        {
//            if (radioButtonAuditFailed.Checked == true)
//            {
//                this.loginAuditing = AuditLevel.Failure;
//            }
//        }

//        private void radioButtonAuditSuccessful_CheckedChanged(object sender, System.EventArgs e)
//        {
//            if (radioButtonAuditSuccessful.Checked == true)
//            {
//                this.loginAuditing = AuditLevel.Success;
//            }
//        }

//        private void radioButtonAuditBoth_CheckedChanged(object sender, System.EventArgs e)
//        {
//            if (radioButtonAuditBoth.Checked == true)
//            {
//                this.loginAuditing = AuditLevel.All;
//            }
//        }

//        private void proxyPasswordTextBox_TextChanged(object sender, System.EventArgs e)
//        {
//            this.sqlProxyPasswordChanged = true;
//        }

//        private void proxyPasswordTextBox_Enter(object sender, System.EventArgs e)
//        {
//            if (!this.sqlProxyPasswordChanged)
//            {
//                this.proxyPasswordTextBox.Text = string.Empty;
//                this.sqlProxyPasswordChanged = false;
//            }
//        }

//        private void proxyPasswordTextBox_Leave(object sender, System.EventArgs e)
//        {
//            if (!this.sqlProxyPasswordChanged)
//            {
//                this.proxyPasswordTextBox.Text = proxyPasswordTextBoxMaskText;
//                this.sqlProxyPasswordChanged = false;
//            }
//        }

//        private void proxyAccountEnabledCheckBox_CheckedChanged(object sender, System.EventArgs e)
//        {
//            //save it right away
//            this.sqlProxyAccountEnabled = this.proxyAccountEnabledCheckBox.Checked;
//            //enable/disable the rest of the proxy controls. NOTE: we don't erase
//            //user's input while disabling controls
//            EnableDisableProxyAccountControls(this.sqlProxyAccountEnabled);
//        }

//        #endregion
//    }
//}