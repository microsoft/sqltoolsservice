//****************************************************************************
//      Copyright (c) Microsoft Corporation.
//
// @File: createlogingeneral.cs
//
// Purpose:
//      SQL Workbench Create Login (General) Form code.
//
// Notes:
//
// History:
//
//   @Version: Yukon
//   120625 MRS  10/01/02 Integrate SMO changes
//   118268 SNT  09/11/02 Management Dialogs: Create Login displays an error
//                        when you don't select a windows login name
//
// @EndHeader@
//****************************************************************************

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Resources;
using System.Data;
using System.Linq;

using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.SqlServer.Management.Common;

using Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginData;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// The general page of the Create Login/Login Properties dbCommander
    /// </summary>
    internal class CreateLoginGeneral : SqlManagementUserControl, IPanelForm
    {
        #region Member WinForms Controls

        private System.Windows.Forms.RadioButton windowsOrAadAuthentication;
        private System.Windows.Forms.Label loginNameLabel;
        private System.Windows.Forms.TextBox loginName;
        private System.Windows.Forms.Button browseWindowsLogins;
        private System.Windows.Forms.RadioButton sqlServerAuthentication;
        private System.Windows.Forms.Label sqlPasswordLabel;
        private System.Windows.Forms.TextBox sqlPassword;
        private System.Windows.Forms.Label confirmPasswordLabel;
        private System.Windows.Forms.Label defaultDatabaseLabel;
        private System.Windows.Forms.ComboBox defaultDatabase;
        private System.Windows.Forms.ComboBox defaultLanguage;
        private System.Windows.Forms.Label defaultLanguageLabel;
        private System.Windows.Forms.TextBox sqlConfirmPassword;
        private System.Windows.Forms.CheckBox checkBoxMustChange;
        private System.Windows.Forms.CheckBox checkBoxEnforcePasswordPolicy;
        private System.Windows.Forms.CheckBox checkBoxEnforcePasswordExpiration;
        private Label oldPasswordLabel;
        private TextBox oldPassword;
        private RadioButton mappedToCertificate;
        private RadioButton mappedToKey;
        #endregion

        private LoginPrototype              prototype                   = null;
        private ResourceManager             resourceManager             = null;
        private string                      defaultLanguagePlaceholder  = "";
        private string                      defaultLanguageCBItemFormat;

        private bool initializing = false;
        private CheckBox mapToCredCheckBox;
        private Button addCredentialButton;
        private Button removeCredentialButton;
        private ComboBox credentialComboBox;
        private SqlManagerUIDlgGrid credentialGrid;
        private Label credentialNameLabel;
        private ComboBox certificateName;
        private ComboBox keyName;
        private Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager sqlStudioLayoutManager1;
        private IContainer components;
        private TableLayoutPanel tableLayoutPanel1;
        private CheckBox checkBoxOldPassword;

        //  private System.ComponentModel.IContainer components;

        /// <summary>
        /// Constructor
        /// </summary>
        public CreateLoginGeneral()
        {
            this.InitializeComponent();
            this.InitializeGrid();
            this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.general.f1";
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataContainer"></param>
        public CreateLoginGeneral(CDataContainer dataContainer, LoginPrototype prototype)
        {
            this.resourceManager = new ResourceManager(
                "Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings",
                typeof(CreateLoginGeneral).Assembly);

            DataContainer                   = dataContainer;
            this.prototype                  = prototype;
            this.defaultLanguagePlaceholder = resourceManager.GetString("prototype.defaultLanguage"); // "(default)"
            this.HelpF1Keyword              = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.general.f1";
            this.defaultLanguageCBItemFormat = resourceManager.GetString("prototype.languageCBItemFormat");

            InitializeComponent();
            this.InitializeGrid();
        }


        /// <summary>
        /// Enable or disable child controls as appropriate
        /// </summary>
        private void            EnableControls()
        {
            bool useAADAuthentication = (this.prototype.LoginType == LoginType.ExternalUser
                || this.prototype.LoginType == LoginType.ExternalGroup);

            EnableDisableSqlAuthenticationData((this.prototype.LoginType == LoginType.SqlLogin));
            EnableDisableAadAuthenticationData(useAADAuthentication);

            if (this.prototype.Exists)
            {
                // For existing logins, the user cannot change the login type, so
                // disable the windows and sql login type radio buttons
                this.windowsOrAadAuthentication.Enabled = false;
                this.sqlServerAuthentication.Enabled    = false;
                this.mappedToCertificate.Enabled        = false;
                this.mappedToKey.Enabled                = false;

                // for existing logins, the login name can't be changed.
                this.loginName.ReadOnly          = true;
                this.browseWindowsLogins.Enabled = false;
                FillCredentialInformation();

                this.certificateName.Enabled = false;
                this.keyName.Enabled = false;
            }
            else
            {
                if (this.ServerConnection != null && this.ServerConnection.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                {
                    this.browseWindowsLogins.Enabled = false;
                }

                // enable the browse button only for new Windows logins
                this.browseWindowsLogins.Enabled = (this.prototype.LoginType == LoginType.WindowsUser);
                this.certificateName.Enabled = (this.prototype.LoginType == LoginType.Certificate);
                this.keyName.Enabled = (this.prototype.LoginType == LoginType.AsymmetricKey);
            }

                // sql authentication controls
                bool showOldPassword             = this.prototype.Exists && !useAADAuthentication;
                this.checkBoxOldPassword.Enabled = showOldPassword;
                this.oldPassword.Enabled         = showOldPassword && this.checkBoxOldPassword.Checked;
                this.oldPasswordLabel.Enabled    = showOldPassword && this.checkBoxOldPassword.Checked;
                this.checkBoxMustChange.Visible  = true; //show the must change checkbox always

            // yukon features
            if (this.DataContainer.Server.Information.Version.Major >= 9)
            {
                if (this.prototype.LoginType == LoginType.SqlLogin)
                {
                    EnableDisableSqlAuthenticationData(true);
                    this.checkBoxEnforcePasswordExpiration.Enabled = this.checkBoxEnforcePasswordPolicy.Checked;
                    this.checkBoxMustChange.Enabled =
                        this.checkBoxEnforcePasswordExpiration.Checked &&
                        this.prototype.PasswordWasChanged;
                }
                else
                {
                    EnableDisableSqlAuthenticationData(false);
                }

                bool isCertificateOrKey =
                    (this.prototype.LoginType == LoginType.Certificate) ||
                    (this.prototype.LoginType == LoginType.AsymmetricKey);

                this.defaultDatabase.Enabled = !isCertificateOrKey;
                this.defaultLanguage.Enabled = !isCertificateOrKey;
            }
            else
            {
                this.checkBoxEnforcePasswordExpiration.Visible  = false;
                this.checkBoxEnforcePasswordPolicy.Visible      = false;
                this.checkBoxMustChange.Visible                 = false;

                this.checkBoxEnforcePasswordExpiration.Checked  = false;
                this.checkBoxEnforcePasswordPolicy.Checked      = false;
                this.checkBoxMustChange.Checked                 = false;

                this.mappedToCertificate.Enabled = false;
                this.mappedToKey.Enabled = false;
                this.certificateName.Enabled = false;
                this.keyName.Enabled = false;
            }
        }

        /// <summary>
        /// Initialize the controls on the form
        /// </summary>
        private void            InitializeControlData()
        {
            this.initializing               = true;

            this.loginName.Text             = this.prototype.LoginName;
            this.sqlPassword.Text           = this.prototype.SqlPassword;
            this.sqlConfirmPassword.Text    = this.prototype.SqlPasswordConfirm;

            if(this.DataContainer.Server.Information.Version.Major >= 9)
            {
                //Filling combobox for Asymmetric keys
                this.keyName.DataSource = this.prototype.AsymmetricKeyNames;
                this.keyName.DisplayMember = "Name";
                this.keyName.SelectedIndex = this.keyName.FindStringExact(this.prototype.AsymmetricKeyName);
                //Filling combobox for certificates
                this.certificateName.DataSource = this.prototype.CertificateNames;
                this.certificateName.DisplayMember = "Name";
                this.certificateName.SelectedIndex = this.certificateName.FindStringExact(this.prototype.CertificateName);
            }


            switch (this.prototype.LoginType)
            {
                case LoginType.WindowsGroup:
                case LoginType.WindowsUser:
                case LoginType.ExternalGroup:
                case LoginType.ExternalUser:

                    this.windowsOrAadAuthentication.Checked = true;
                    break;

                case LoginType.Certificate:

                    this.mappedToCertificate.Checked    = true;
                    break;

                case LoginType.AsymmetricKey:

                    this.mappedToKey.Checked    = true;
                    break;

                default:

                    System.Diagnostics.Debug.Assert(this.prototype.LoginType == LoginType.SqlLogin, "unexpected login type");
                    this.sqlServerAuthentication.Checked = true;
                    break;

            }

            FillDBCombo();
            FillLanguageCombo();
            SetLanguageComboSelection();

            this.defaultDatabase.SelectedIndex  = this.defaultDatabase.FindStringExact(this.prototype.DefaultDatabase);

            if (this.DataContainer.Server.Information.Version.Major >= 9)
            {
                this.checkBoxMustChange.Checked                 = this.prototype.MustChange;
                this.checkBoxEnforcePasswordPolicy.Checked      = this.prototype.EnforcePolicy;
                this.checkBoxEnforcePasswordExpiration.Checked  = this.prototype.EnforceExpiration;
            }
            else
            {
                this.checkBoxMustChange.Checked                 = false;
                this.checkBoxEnforcePasswordPolicy.Checked      = false;
                this.checkBoxEnforcePasswordExpiration.Checked  = false;
            }

            EnableControls();

            this.initializing = false;
        }

        /// <summary>
        /// Populate the database dropdown with the databases on the server
        /// </summary>
        private void            FillDBCombo()
        {
            this.defaultDatabase.Items.Clear();

            foreach (string databaseName in this.prototype.DatabaseNames)
            {
                this.defaultDatabase.Items.Add(databaseName);
            }
        }

        /// <summary>
        /// Populate the language dropdown
        /// </summary>
        private void            FillLanguageCombo()
        {
            this.defaultLanguage.Items.Clear();

            if (!this.prototype.Exists)
            {
                this.defaultLanguage.Items.Add(defaultLanguagePlaceholder);
            }

            // sort the languages alphabetically by alias
            SortedList sortedLanguages = new SortedList(Comparer.Default);

            LanguageUtils.SetLanguageDefaultInitFieldsForDefaultLanguages(DataContainer.Server);
            foreach (Language language in DataContainer.Server.Languages)
            {
                LanguageDisplay listValue = new LanguageDisplay(language);
                sortedLanguages.Add(language.Alias, listValue);
            }

            // add the language display objects to the combo box
            foreach (LanguageDisplay languageDisplay in sortedLanguages.Values)
            {
                this.defaultLanguage.Items.Add(languageDisplay);
            }
        }

        /// <summary>
        /// Set the default language combo box selection to match the prototype's default language
        /// </summary>
        private void SetLanguageComboSelection()
        {
            object selectedLanguageInComboBox = null;

            // Note that the defaultLanguage drop down contains LanguageDisplay items, which
            // use the language alias as their string representation.  Prototype.DefaultLanguage
            // may be the language name or the language alias for the login.
            if (0 != String.Compare(prototype.DefaultLanguage, defaultLanguagePlaceholder, StringComparison.Ordinal))
            {
                int langId = 1033;
                Language language = this.DataContainer.Server.Languages.Cast<Language>().FirstOrDefault(lang => lang.Alias == prototype.DefaultLanguage || lang.Name == prototype.DefaultLanguage);
                if (language != null)
                {
                    langId = language.LangID;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, prototype.DefaultLanguage + " is not in the list of server languages");
                }

                selectedLanguageInComboBox = this.defaultLanguage.Items.OfType<LanguageDisplay>().FirstOrDefault(languageDisplay => languageDisplay.Language.LangID == langId);
            }

            if (selectedLanguageInComboBox != null)
            {
                this.defaultLanguage.SelectedItem = selectedLanguageInComboBox;
            }
            else
            {
                this.defaultLanguage.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Handle changes to the authentication type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAuthenticationTypeChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                if (this.windowsOrAadAuthentication.Checked)
                {
                    // This radio button for SQL Managed Instance represents AAD authentication instead of Windows
                    //
                    this.prototype.LoginType = (this.prototype.WindowsAuthSupported)
                        ? LoginType.WindowsUser
                        : LoginType.ExternalUser;
                }
                else if (this.sqlServerAuthentication.Checked)
                {
                    this.prototype.LoginType = LoginType.SqlLogin;
                }
                else if (this.mappedToCertificate.Checked)
                {
                    this.prototype.LoginType = LoginType.Certificate;
                }
                else if (this.mappedToKey.Checked)
                {
                    this.prototype.LoginType = LoginType.AsymmetricKey;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "unexpected windows authentication type selected in UI");
                }

                this.EnableControls();
            }
        }

        /// <summary>
        /// Handle changes to the Windows account name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnLoginNameChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.LoginName = this.loginName.Text;
                this.EnableControls();
            }
        }


        /// <summary>
        /// Handle changes to the SQL password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnSqlPasswordChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.SqlPassword = this.sqlPassword.Text;
                this.EnableControls();
            }
        }

        /// <summary>
        /// Handle changes to the SQL password confirm text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnSqlPasswordConfirmChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.SqlPasswordConfirm = this.sqlConfirmPassword.Text;
            }
        }

        /// <summary>
        /// Handle changes to the default database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnDefaultDatabaseChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.DefaultDatabase = this.defaultDatabase.Text;
            }
        }

        /// <summary>
        /// Handles changes to asymmetric key combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.AsymmetricKeyName = this.keyName.Text;
            }
        }

        /// <summary>
        /// Handles changes to certificate combo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCertificateChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.CertificateName = this.certificateName.Text;
            }
        }

        /// <summary>
        /// Format display of the combobox to show each language name in their respective language
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DefaultLanguageFormat(object sender, ListControlConvertEventArgs e)
        {
            var item = e.ListItem as LanguageDisplay;

            if (item != null)
            {
                // A string that looks like "Italian - Italiano"
                e.Value = string.Format(defaultLanguageCBItemFormat, item.Language.Alias, item.Language.Name);
            }
        }

        /// <summary>
        /// Handle changes to the default language
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnDefaultLanguageChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                if (this.prototype.Exists || this.defaultLanguage.SelectedIndex != 0)
                {

                    this.prototype.DefaultLanguage = ((LanguageDisplay)this.defaultLanguage.SelectedItem).Language.Name;
                }
                else
                {
                    this.prototype.DefaultLanguage = defaultLanguagePlaceholder;
                }
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

        #region IPanel implementation
        UserControl IPanelForm.Panel
        {
            get
            {
                return this;
            }

        }

        /// <summary>
        /// IPanelForm.OnInitialization
        ///
        /// TODO - in order to reduce IPanelForm container load time
        /// and to improve performance, IPanelForm-s should be able
        /// to lazy-initialize themself when IPanelForm.OnInitialization
        /// is called (a continer like TreePanelForm calls the
        /// OnInitialization() method before first OnSelection())
        /// </summary>
        void IPanelForm.OnInitialization()
        {
        }

        void IPanelForm.OnSelection(TreeNode node)
        {
            InitializeControlData();
        }

        void IPanelForm.OnPanelLoseSelection(TreeNode node)
        {
            this.DataContainer.ObjectName = prototype.LoginName;
        }


        public override void OnReset(object sender)
        {
            base.OnReset(sender);

            InitializeControlData();
        }

        #endregion

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateLoginGeneral));
            this.windowsOrAadAuthentication = new System.Windows.Forms.RadioButton();
            this.browseWindowsLogins = new System.Windows.Forms.Button();
            this.loginName = new System.Windows.Forms.TextBox();
            this.loginNameLabel = new System.Windows.Forms.Label();
            this.sqlServerAuthentication = new System.Windows.Forms.RadioButton();
            this.checkBoxOldPassword = new System.Windows.Forms.CheckBox();
            this.oldPassword = new System.Windows.Forms.TextBox();
            this.oldPasswordLabel = new System.Windows.Forms.Label();
            this.checkBoxEnforcePasswordExpiration = new System.Windows.Forms.CheckBox();
            this.checkBoxEnforcePasswordPolicy = new System.Windows.Forms.CheckBox();
            this.checkBoxMustChange = new System.Windows.Forms.CheckBox();
            this.sqlConfirmPassword = new System.Windows.Forms.TextBox();
            this.confirmPasswordLabel = new System.Windows.Forms.Label();
            this.sqlPassword = new System.Windows.Forms.TextBox();
            this.sqlPasswordLabel = new System.Windows.Forms.Label();
            this.defaultDatabaseLabel = new System.Windows.Forms.Label();
            this.defaultDatabase = new System.Windows.Forms.ComboBox();
            this.defaultLanguageLabel = new System.Windows.Forms.Label();
            this.defaultLanguage = new System.Windows.Forms.ComboBox();
            this.mappedToCertificate = new System.Windows.Forms.RadioButton();
            this.mappedToKey = new System.Windows.Forms.RadioButton();
            this.mapToCredCheckBox = new System.Windows.Forms.CheckBox();
            this.addCredentialButton = new System.Windows.Forms.Button();
            this.removeCredentialButton = new System.Windows.Forms.Button();
            this.credentialComboBox = new System.Windows.Forms.ComboBox();
            this.credentialGrid = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            this.credentialNameLabel = new System.Windows.Forms.Label();
            this.certificateName = new System.Windows.Forms.ComboBox();
            this.keyName = new System.Windows.Forms.ComboBox();
            this.sqlStudioLayoutManager1 = new Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.credentialGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sqlStudioLayoutManager1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //
            // windowsAuthentication or aadAuthentication
            //
            if (this.prototype.WindowsAuthSupported)
            {
                resources.ApplyResources(this.windowsOrAadAuthentication, "windowsAuthentication");
                this.windowsOrAadAuthentication.Name = "windowsAuthentication";
                this.windowsOrAadAuthentication.CheckedChanged += new System.EventHandler(this.OnAuthenticationTypeChanged);
            }
            else
            {
                resources.ApplyResources(this.windowsOrAadAuthentication, "aadAuthentication");
                this.windowsOrAadAuthentication.Name = "aadAuthentication";
                this.windowsOrAadAuthentication.Click += new System.EventHandler(this.OnAuthenticationTypeChanged);
            }
            this.tableLayoutPanel1.SetColumnSpan(this.windowsOrAadAuthentication, 3);
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.windowsOrAadAuthentication, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level1);
            //
            // browseWindowsLogins
            //
            resources.ApplyResources(this.browseWindowsLogins, "browseWindowsLogins");
            this.browseWindowsLogins.MinimumSize = new System.Drawing.Size(75, 23);
            this.browseWindowsLogins.Name = "browseWindowsLogins";
            this.browseWindowsLogins.Click += new System.EventHandler(this.OnSelectNTLogin);
            //
            // loginName
            //
            resources.ApplyResources(this.loginName, "loginName");
            this.loginName.Name = "loginName";
            this.loginName.TextChanged += new System.EventHandler(this.OnLoginNameChanged);
            //
            // loginNameLabel
            //
            resources.ApplyResources(this.loginNameLabel, "loginNameLabel");
            this.loginNameLabel.Name = "loginNameLabel";
            //
            // sqlServerAuthentication
            //
            resources.ApplyResources(this.sqlServerAuthentication, "sqlServerAuthentication");
            this.sqlServerAuthentication.Checked = true;
            this.tableLayoutPanel1.SetColumnSpan(this.sqlServerAuthentication, 3);
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.sqlServerAuthentication, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level1);
            this.sqlServerAuthentication.Name = "sqlServerAuthentication";
            this.sqlServerAuthentication.TabStop = true;
            this.sqlServerAuthentication.Click += new System.EventHandler(this.OnAuthenticationTypeChanged);
            this.sqlServerAuthentication.CheckedChanged += new System.EventHandler(this.sqlServerAuthentication_CheckedChanged);
            //
            // checkBoxOldPassword
            //
            resources.ApplyResources(this.checkBoxOldPassword, "checkBoxOldPassword");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.checkBoxOldPassword, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.checkBoxOldPassword.Name = "checkBoxOldPassword";
            this.checkBoxOldPassword.UseVisualStyleBackColor = true;
            this.checkBoxOldPassword.CheckedChanged += new System.EventHandler(this.checkBoxOldPassword_CheckedChanged);
            //
            // oldPassword
            //
            resources.ApplyResources(this.oldPassword, "oldPassword");
            this.oldPassword.Name = "oldPassword";
            this.oldPassword.UseSystemPasswordChar = true;
            this.oldPassword.TextChanged += new System.EventHandler(this.OnOldPasswordChanged);
            //
            // oldPasswordLabel
            //
            resources.ApplyResources(this.oldPasswordLabel, "oldPasswordLabel");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.oldPasswordLabel, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level3);
            this.oldPasswordLabel.Name = "oldPasswordLabel";
            //
            // checkBoxEnforcePasswordExpiration
            //
            resources.ApplyResources(this.checkBoxEnforcePasswordExpiration, "checkBoxEnforcePasswordExpiration");
            this.tableLayoutPanel1.SetColumnSpan(this.checkBoxEnforcePasswordExpiration, 3);
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.checkBoxEnforcePasswordExpiration, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.checkBoxEnforcePasswordExpiration.Name = "checkBoxEnforcePasswordExpiration";
            this.checkBoxEnforcePasswordExpiration.CheckedChanged += new System.EventHandler(this.checkBoxEnforcePasswordExpiration_CheckedChanged);
            //
            // checkBoxEnforcePasswordPolicy
            //
            resources.ApplyResources(this.checkBoxEnforcePasswordPolicy, "checkBoxEnforcePasswordPolicy");
            this.tableLayoutPanel1.SetColumnSpan(this.checkBoxEnforcePasswordPolicy, 3);
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.checkBoxEnforcePasswordPolicy, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.checkBoxEnforcePasswordPolicy.Name = "checkBoxEnforcePasswordPolicy";
            this.checkBoxEnforcePasswordPolicy.CheckedChanged += new System.EventHandler(this.checkBoxEnforcePasswordPolicy_CheckedChanged);
            //
            // checkBoxMustChange
            //
            resources.ApplyResources(this.checkBoxMustChange, "checkBoxMustChange");
            this.tableLayoutPanel1.SetColumnSpan(this.checkBoxMustChange, 3);
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.checkBoxMustChange, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.checkBoxMustChange.Name = "checkBoxMustChange";
            this.checkBoxMustChange.CheckedChanged += new System.EventHandler(this.checkBoxMustChange_CheckedChanged);
            //
            // sqlConfirmPassword
            //
            resources.ApplyResources(this.sqlConfirmPassword, "sqlConfirmPassword");
            this.sqlConfirmPassword.Name = "sqlConfirmPassword";
            this.sqlConfirmPassword.UseSystemPasswordChar = true;
            this.sqlConfirmPassword.TextChanged += new System.EventHandler(this.OnSqlPasswordConfirmChanged);
            //
            // confirmPasswordLabel
            //
            resources.ApplyResources(this.confirmPasswordLabel, "confirmPasswordLabel");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.confirmPasswordLabel, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.confirmPasswordLabel.Name = "confirmPasswordLabel";
            //
            // sqlPassword
            //
            resources.ApplyResources(this.sqlPassword, "sqlPassword");
            this.sqlPassword.Name = "sqlPassword";
            this.sqlPassword.UseSystemPasswordChar = true;
            this.sqlPassword.TextChanged += new System.EventHandler(this.OnSqlPasswordChanged);
            //
            // sqlPasswordLabel
            //
            resources.ApplyResources(this.sqlPasswordLabel, "sqlPasswordLabel");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.sqlPasswordLabel, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.sqlPasswordLabel.Name = "sqlPasswordLabel";
            //
            // defaultDatabaseLabel
            //
            resources.ApplyResources(this.defaultDatabaseLabel, "defaultDatabaseLabel");
            this.defaultDatabaseLabel.Name = "defaultDatabaseLabel";
            //
            // defaultDatabase
            //
            resources.ApplyResources(this.defaultDatabase, "defaultDatabase");
            this.defaultDatabase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.defaultDatabase.FormattingEnabled = true;
            this.defaultDatabase.Name = "defaultDatabase";
            this.defaultDatabase.SelectedIndexChanged += new System.EventHandler(this.OnDefaultDatabaseChanged);
            //
            // defaultLanguageLabel
            //
            resources.ApplyResources(this.defaultLanguageLabel, "defaultLanguageLabel");
            this.defaultLanguageLabel.Name = "defaultLanguageLabel";
            //
            // defaultLanguage
            //
            resources.ApplyResources(this.defaultLanguage, "defaultLanguage");
            this.defaultLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.defaultLanguage.FormattingEnabled = true;
            this.defaultLanguage.Name = "defaultLanguage";
            this.defaultLanguage.SelectedIndexChanged += new System.EventHandler(this.OnDefaultLanguageChanged);
            this.defaultLanguage.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.DefaultLanguageFormat);
            //
            // mappedToCertificate
            //
            resources.ApplyResources(this.mappedToCertificate, "mappedToCertificate");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.mappedToCertificate, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level1);
            this.mappedToCertificate.Name = "mappedToCertificate";
            this.mappedToCertificate.Click += new System.EventHandler(this.OnAuthenticationTypeChanged);
            //
            // mappedToKey
            //
            resources.ApplyResources(this.mappedToKey, "mappedToKey");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.mappedToKey, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level1);
            this.mappedToKey.Name = "mappedToKey";
            this.mappedToKey.Click += new System.EventHandler(this.OnAuthenticationTypeChanged);
            this.mappedToKey.CheckedChanged += new System.EventHandler(this.mappedToKey_CheckedChanged);
            //
            // mapToCredCheckBox
            //
            resources.ApplyResources(this.mapToCredCheckBox, "mapToCredCheckBox");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.mapToCredCheckBox, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level1);
            this.mapToCredCheckBox.Name = "mapToCredCheckBox";
            this.mapToCredCheckBox.UseVisualStyleBackColor = true;
            this.mapToCredCheckBox.CheckedChanged += new System.EventHandler(this.mapToCredCheckBox_CheckedChanged);
            //
            // addCredentialButton
            //
            resources.ApplyResources(this.addCredentialButton, "addCredentialButton");
            this.addCredentialButton.MinimumSize = new System.Drawing.Size(75, 23);
            this.addCredentialButton.Name = "addCredentialButton";
            this.addCredentialButton.UseVisualStyleBackColor = true;
            this.addCredentialButton.Click += new System.EventHandler(this.addCredentialButton_Click);
            //
            // removeCredentialButton
            //
            resources.ApplyResources(this.removeCredentialButton, "removeCredentialButton");
            this.removeCredentialButton.MinimumSize = new System.Drawing.Size(75, 23);
            this.removeCredentialButton.Name = "removeCredentialButton";
            this.removeCredentialButton.UseVisualStyleBackColor = true;
            this.removeCredentialButton.Click += new System.EventHandler(this.removeCredentialButton_Click);
            //
            // credentialComboBox
            //
            resources.ApplyResources(this.credentialComboBox, "credentialComboBox");
            this.credentialComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.credentialComboBox.FormattingEnabled = true;
            this.credentialComboBox.Name = "credentialComboBox";
            //
            // credentialGrid
            //
            resources.ApplyResources(this.credentialGrid, "credentialGrid");
            this.credentialGrid.AccessibleRole = System.Windows.Forms.AccessibleRole.Table;
            this.credentialGrid.BackColor = System.Drawing.SystemColors.Window;
            this.credentialGrid.ForceEnabled = false;
            this.credentialGrid.Name = "credentialGrid";
            this.credentialGrid.SelectionType = Microsoft.SqlServer.Management.UI.Grid.GridSelectionType.SingleRow;
            //
            // credentialNameLabel
            //
            resources.ApplyResources(this.credentialNameLabel, "credentialNameLabel");
            this.sqlStudioLayoutManager1.SetHorizontalIndent(this.credentialNameLabel, Microsoft.SqlServer.Management.Controls.SqlStudioLayoutManager.SqlStudioLayoutIndent.Level2);
            this.credentialNameLabel.Name = "credentialNameLabel";
            //
            // certificateName
            //
            resources.ApplyResources(this.certificateName, "certificateName");
            this.certificateName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.certificateName.FormattingEnabled = true;
            this.certificateName.Name = "certificateName";
            this.certificateName.SelectedIndexChanged += new System.EventHandler(this.OnCertificateChanged);
            //
            // keyName
            //
            resources.ApplyResources(this.keyName, "keyName");
            this.keyName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.keyName.FormattingEnabled = true;
            this.keyName.Name = "keyName";
            this.keyName.SelectedIndexChanged += new System.EventHandler(this.OnKeyChanged);
            //
            // tableLayoutPanel1
            //
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");

            if (this.prototype.WindowsAuthSupported)
            {
                this.tableLayoutPanel1.Controls.Add(this.windowsOrAadAuthentication, 0, 1);
                this.tableLayoutPanel1.Controls.Add(this.sqlServerAuthentication, 0, 2);
            }
            else
            {
                // For SQL Managed Instance, SQL authentication is default, thus this radio button should be at the top.
                //
                this.tableLayoutPanel1.Controls.Add(this.sqlServerAuthentication, 0, 1);
                this.tableLayoutPanel1.Controls.Add(this.windowsOrAadAuthentication, 0, 2);
            }

            this.tableLayoutPanel1.Controls.Add(this.checkBoxMustChange, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.defaultLanguageLabel, 0, 15);
            this.tableLayoutPanel1.Controls.Add(this.defaultDatabaseLabel, 0, 14);
            this.tableLayoutPanel1.Controls.Add(this.removeCredentialButton, 2, 13);
            this.tableLayoutPanel1.Controls.Add(this.defaultLanguage, 1, 15);
            this.tableLayoutPanel1.Controls.Add(this.credentialNameLabel, 0, 13);
            this.tableLayoutPanel1.Controls.Add(this.defaultDatabase, 1, 14);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxEnforcePasswordExpiration, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.certificateName, 1, 10);
            this.tableLayoutPanel1.Controls.Add(this.addCredentialButton, 2, 12);
            this.tableLayoutPanel1.Controls.Add(this.oldPassword, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxEnforcePasswordPolicy, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.mapToCredCheckBox, 0, 12);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxOldPassword, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.oldPasswordLabel, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.loginNameLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.loginName, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.browseWindowsLogins, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.mappedToKey, 0, 11);
            this.tableLayoutPanel1.Controls.Add(this.sqlConfirmPassword, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.confirmPasswordLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.mappedToCertificate, 0, 10);
            this.tableLayoutPanel1.Controls.Add(this.sqlPasswordLabel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.sqlPassword, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.keyName, 1, 11);
            this.tableLayoutPanel1.Controls.Add(this.credentialComboBox, 1, 12);
            this.tableLayoutPanel1.Controls.Add(this.credentialGrid, 1, 13);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            //
            // CreateLoginGeneral
            //
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tableLayoutPanel1);
            this.sqlStudioLayoutManager1.SetIsRootControl(this, true);
            this.Name = "CreateLoginGeneral";
            ((System.ComponentModel.ISupportInitialize)(this.credentialGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sqlStudioLayoutManager1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion


        private void checkBoxMustChange_CheckedChanged(object sender, System.EventArgs e)
        {
            if (!this.initializing)
            {
                System.Diagnostics.Debug.Assert(this.DataContainer.Server.Information.Version.Major>=9, "supported only on Yukon");

                this.prototype.MustChange = checkBoxMustChange.Checked;
            }
        }

        private void checkBoxEnforcePasswordPolicy_CheckedChanged(object sender, System.EventArgs e)
        {
            if (!this.initializing)
            {
                System.Diagnostics.Debug.Assert(this.DataContainer.Server.Information.Version.Major>=9, "supported only on Yukon");

                this.prototype.EnforcePolicy = checkBoxEnforcePasswordPolicy.Checked;

                // enforce password expiration supported only when enforce password policy is turned on
                if (this.prototype.EnforcePolicy == true)
                {
                    // enable expiration and autocheck it
                    checkBoxEnforcePasswordExpiration.Enabled = true;
                    checkBoxEnforcePasswordExpiration.Checked = true;
                }
                else
                {
                    // disable expiration and uncheck it
                    checkBoxEnforcePasswordExpiration.Enabled = false;
                    checkBoxEnforcePasswordExpiration.Checked = false;
                }
            }
        }

        private void checkBoxEnforcePasswordExpiration_CheckedChanged(object sender, System.EventArgs e)
        {
            if (!this.initializing)
            {
                System.Diagnostics.Debug.Assert(this.DataContainer.Server.Information.Version.Major>=9, "supported only on Yukon");

                this.prototype.EnforceExpiration = checkBoxEnforcePasswordExpiration.Checked;

                // must change supported only when enforce expiration policy is turned on _and_ password was changed
                if (this.prototype.EnforceExpiration == true)
                {
                    // enable 'must change' checkbox and check it
                    checkBoxMustChange.Enabled = true && this.prototype.PasswordWasChanged;
                    checkBoxMustChange.Checked = true && this.prototype.PasswordWasChanged;
                }
                else
                {
                    // disable 'must change' checkbox and uncheck it
                    checkBoxMustChange.Enabled = false;
                    checkBoxMustChange.Checked = false;
                }
            }
        }

        private void OnOldPasswordChanged(object sender, EventArgs e)
        {
            this.prototype.OldPassword = this.oldPassword.Text;
        }

        private void mapToCredCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.credentialComboBox.Enabled = mapToCredCheckBox.Checked;
            this.credentialGrid.Enabled = mapToCredCheckBox.Checked;

            PopulateCredentialComboBox();
            this.prototype.MapToCredential = mapToCredCheckBox.Checked;
        }

        // A collection of all credentials to which login can map
        private System.Collections.Specialized.StringCollection allCredentials;
        private bool isInitialized = false;
        private void PopulateCredentialComboBox()
        {
            if (!isInitialized)
            {
                allCredentials = this.prototype.CredentialNames;
            }
            credentialComboBox.Text = resourceManager.GetString("general.selectCredential");
            credentialComboBox.Items.Clear();
            foreach (string credential in allCredentials)
            {
                if (!this.prototype.Credentials.Contains(credential))
                {
                    credentialComboBox.Items.Add(credential);
                }
            }

            this.addCredentialButton.Enabled = this.mapToCredCheckBox.Checked && this.credentialComboBox.Items.Count > 0;

            this.credentialComboBox.SelectedIndex = this.credentialComboBox.Items.Count > 0 ? 0 : -1;
        }

        private void addCredentialButton_Click(object sender, EventArgs e)
        {
            GridCellCollection row = new GridCellCollection();
            string credentialName = credentialComboBox.SelectedItem.ToString();
            GridCell cell = new GridCell(EditableCellType.ReadOnly, credentialName);
            row.Add(cell);

            cell = new GridCell(EditableCellType.ReadOnly, this.prototype.credProviderMap[credentialName].ToString());
            row.Add(cell);

            credentialGrid.AddRow(row);

            BlockOfCells[] blocks = new BlockOfCells[1];
            blocks[0] = new BlockOfCells(0, 0);
            BlockOfCellsCollection collection = new BlockOfCellsCollection(blocks);
            credentialGrid.SelectedCells = collection;

            allCredentials.Remove(credentialName);
            this.prototype.Credentials.Add(credentialName);
            if (!removeCredentialButton.Enabled)
                removeCredentialButton.Enabled = true;
            if (this.ServerConnection.ServerVersion.Major >= 10)
            {
                PopulateCredentialComboBox();
            }
            else
            {
                // else it is Yukon
                System.Diagnostics.Debug.Assert(this.ServerConnection.ServerVersion.Major == 9, "Server should be Yukon");
                addCredentialButton.Enabled = false;
                credentialComboBox.Enabled = false;
            }
        }

        private void removeCredentialButton_Click(object sender, EventArgs e)
        {
            int row = -1;
            BlockOfCellsCollection cellsCollection = credentialGrid.SelectedCells;
            if ((cellsCollection != null) && (0 < cellsCollection.Count))
            {
                row = (int)cellsCollection[0].Y;
            }

            string credentialName = string.Empty;
            if (0 <= row && row < this.prototype.Credentials.Count)
            {
                credentialName = this.prototype.Credentials[row];
                allCredentials.Add(credentialName);
                this.prototype.Credentials.Remove(credentialName);
                credentialGrid.DeleteRow(row);

                if (this.prototype.Credentials.Count == 0)
                {
                    removeCredentialButton.Enabled = false;
                }

                PopulateCredentialComboBox();
                if (this.ServerConnection.ServerVersion.Major == 9)
                {
                    // when Yukon, then only the add button is disabled, else it is enabled
                    addCredentialButton.Enabled = true;
                    credentialComboBox.Enabled = true;
                }
            }
        }

        private void FillCredentialInformation()
        {
            if (this.ServerConnection.ServerVersion.Major < 9)
                return;
            credentialGrid.DeleteAllRows();
            if (this.prototype.Credentials.Count > 0)
            {
                // check box should be ticked
                this.mapToCredCheckBox.Enabled = true;
                this.mapToCredCheckBox.Checked = true;
                this.credentialGrid.Enabled = true;
                this.credentialNameLabel.Enabled = true;
                this.credentialComboBox.Enabled = true;
                if (this.ServerConnection.ServerVersion.Major >= 10)
                {
                    PopulateCredentialComboBox();
                }
                else
                {
                    // else it is Yukon
                    addCredentialButton.Enabled = false;
                    credentialComboBox.Enabled = false;
                }
                // The grid should have information about the added credentials
                foreach (string credential in this.prototype.Credentials)
                {
                    GridCellCollection row = new GridCellCollection();
                    GridCell cell = new GridCell(EditableCellType.ReadOnly, credential);
                    row.Add(cell);
                    cell = new GridCell(EditableCellType.ReadOnly, this.prototype.credProviderMap[credential].ToString());
                    row.Add(cell);
                    credentialGrid.AddRow(row);
                    allCredentials.Remove(credential);
                }
                this.removeCredentialButton.Enabled = true;

                BlockOfCells[] blocks = new BlockOfCells[1];
                blocks[0] = new BlockOfCells(0, 0);
                BlockOfCellsCollection collection = new BlockOfCellsCollection(blocks);
                credentialGrid.SelectedCells = collection;
            }
            else
            {
                // No mapped credentials so disable button and box until checkbox is checked
                this.mapToCredCheckBox.Checked = false;
                this.addCredentialButton.Enabled = false;
                this.credentialComboBox.Enabled = false;
            }
        }

        private void InitializeGrid()
        {
            GridColumnInfo column = new GridColumnInfo
            {
                WidthType = GridColumnWidthType.InPixels,
                ColumnWidth = credentialGrid.Width/2,
                IsUserResizable = true
            };
            credentialGrid.AddColumn(column);
            credentialGrid.SetHeaderInfo(0, resourceManager.GetString("general.credentialGrid.Credential"), null);

            column = new GridColumnInfo
            {
                WidthType = GridColumnWidthType.InPixels,
                ColumnWidth = credentialGrid.Width/2,
                IsUserResizable = true
            };
            credentialGrid.AddColumn(column);
            credentialGrid.SetHeaderInfo(1, resourceManager.GetString("general.credentialGrid.Provider"), null);
        }

        private void checkBoxOldPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBoxOldPassword.Checked)
            {
                this.oldPasswordLabel.Enabled = true;
                this.oldPassword.Enabled = true;
                this.prototype.ShowOldPassword = true;
            }
            else
            {
                this.oldPasswordLabel.Enabled = false;
                this.oldPassword.Enabled = false;
                this.prototype.ShowOldPassword = false;
            }
        }

        private void mappedToKey_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void sqlServerAuthentication_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void EnableDisableSqlAuthenticationData(bool enable)
        {
            this.sqlPasswordLabel.Enabled = enable;
            this.sqlPassword.Enabled = enable;
            this.confirmPasswordLabel.Enabled = enable;
            this.sqlConfirmPassword.Enabled = enable;
            this.checkBoxEnforcePasswordExpiration.Enabled = enable;
            this.checkBoxEnforcePasswordPolicy.Enabled = enable;
            this.checkBoxMustChange.Enabled = enable;
        }

        private void EnableDisableAadAuthenticationData(bool enable)
        {
            // In case of AAD logins, disable all controls except default database and default language on General login page.
            //
            bool disable = !enable;
            this.sqlPasswordLabel.Enabled = disable;
            this.sqlPassword.Enabled = disable;
            this.confirmPasswordLabel.Enabled = disable;
            this.sqlConfirmPassword.Enabled = disable;
            this.checkBoxEnforcePasswordExpiration.Enabled = disable;
            this.checkBoxEnforcePasswordPolicy.Enabled = disable;
            this.checkBoxMustChange.Enabled = disable;
            this.mappedToCertificate.Enabled = disable;
            this.mappedToKey.Enabled = disable;
            this.mapToCredCheckBox.Enabled = disable;
            this.checkBoxOldPassword.Enabled = disable;
            this.oldPassword.Enabled = disable;
            this.oldPasswordLabel.Enabled = disable;
            this.addCredentialButton.Enabled = disable;
            this.removeCredentialButton.Enabled = disable;
            this.credentialComboBox.Enabled = disable;
            this.credentialNameLabel.Enabled = disable;
            this.certificateName.Enabled = disable;
            this.keyName.Enabled = disable;
            this.credentialGrid.Enabled = disable;
        }
    }
}








