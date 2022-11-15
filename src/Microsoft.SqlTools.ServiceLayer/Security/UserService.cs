//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// using System;
// using System.Collections;
// using System.Security;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    public partial class UserGeneralNew
    {
        private UserPrototypeFactory userPrototypeFactory;
        private ExhaustiveUserTypes currentUserType;
        private Urn parentDbUrn;
        private Urn objectUrn;
        private const string FAKE_PASSWORD = "***************";
        private string defaultLanguagePlaceholder = string.Empty;
        private CDataContainer dataContainer = null;

        private UserPrototypeNew currentUserPrototype;

        public UserGeneralNew(CDataContainer context)
        {
            this.DataContainer = context;          
            this.parentDbUrn = new Urn(this.DataContainer.ParentUrn); 
            this.objectUrn = new Urn(this.DataContainer.ObjectUrn);

            this.userPrototypeFactory = UserPrototypeFactory.GetInstance(this.DataContainer);

            if (this.DataContainer.IsNewObject)
            {
                if (this.IsParentDatabaseContained())
                {
                    this.currentUserType = ExhaustiveUserTypes.SqlUserWithPassword;
                }
                else
                {
                    this.currentUserType = ExhaustiveUserTypes.LoginMappedUser;
                }
            }
            else
            {
                this.currentUserType = this.GetCurrentUserTypeForExistingUser(
                    this.DataContainer.Server.GetSmoObject(this.objectUrn) as User);
            }

            this.currentUserPrototype = this.userPrototypeFactory.GetUserPrototype(this.currentUserType);
        }

         /// <summary>
        /// CDataContainer accessor
        /// </summary>
        protected CDataContainer DataContainer
        {
            get
            {
                return this.dataContainer;
            }
            set
            {
                this.dataContainer = value;
            }
        }

        // private void InitializeValuesInUiControls()
        // {
        //     this.userNameTextBox.Text = this.currentUserPrototype.Name;

        //     if(this.currentUserPrototype.UserType == UserType.Certificate)
        //     {
        //         this.mappedObjTextbox.Text = this.currentUserPrototype.CertificateName;
        //     }
        //     if (this.currentUserPrototype.UserType == UserType.AsymmetricKey)
        //     {
        //         this.mappedObjTextbox.Text = this.currentUserPrototype.AsymmetricKeyName;
        //     }
        //     IUserPrototypeWithMappedLogin mappedLoginPrototype = this.currentUserPrototype
        //                                                                     as IUserPrototypeWithMappedLogin;
        //     if (mappedLoginPrototype != null)
        //     {
        //         this.mappedObjTextbox.Text = mappedLoginPrototype.LoginName;
        //     }

        //     IUserPrototypeWithDefaultLanguage defaultLanguagePrototype = this.currentUserPrototype
        //                                                                             as IUserPrototypeWithDefaultLanguage;
        //     if (defaultLanguagePrototype != null
        //         && defaultLanguagePrototype.IsDefaultLanguageSupported)
        //     {
        //         string defaultLanguageAlias = defaultLanguagePrototype.DefaultLanguageAlias;

        //         //If engine returns default language as empty or null, that means the default language of  
        //         //database will be used.
        //         //Default language is not applicable for users inside an uncontained authentication.
        //         if (string.IsNullOrEmpty(defaultLanguageAlias)
        //             && (this.DataContainer.Server.GetSmoObject(this.parentDbUrn) as Database).ContainmentType != ContainmentType.None)
        //         {
        //             defaultLanguageAlias = this.defaultLanguagePlaceholder;
        //         }
        //         this.defaultLanguageComboBox.Text = defaultLanguageAlias;
        //     }

        //     IUserPrototypeWithDefaultSchema defaultSchemaPrototype = this.currentUserPrototype
        //                                                                         as IUserPrototypeWithDefaultSchema;
        //     if (defaultSchemaPrototype != null
        //         && defaultSchemaPrototype.IsDefaultSchemaSupported)
        //     {
        //         this.defaultSchemaTextBox.Text = defaultSchemaPrototype.DefaultSchema;
        //     }

        //     IUserPrototypeWithPassword userWithPwdPrototype = this.currentUserPrototype
        //                                                                 as IUserPrototypeWithPassword;
        //     if (userWithPwdPrototype != null
        //         && !this.DataContainer.IsNewObject)
        //     {
        //         this.passwordTextBox.Text = FAKE_PASSWORD;
        //         this.confirmPwdTextBox.Text = FAKE_PASSWORD;                
        //     }
        // }        

        // private void UpdateUiControlsOnLoad()
        // {
        //     if (!this.DataContainer.IsNewObject)
        //     {
        //         this.userNameTextBox.ReadOnly = true; //Rename is not allowed from the dialog.
        //         this.userSearchButton.Enabled = false;
        //         this.mappedObjTextbox.ReadOnly = true; //Changing mapped login, certificate and asymmetric key is not allowed
        //         this.mappedObjSearchButton.Enabled = false;
        //         //from SMO also.
        //         this.userTypeComboBox.Enabled = false;
        //         this.oldPasswordTextBox.ReadOnly = true;
        //     }
        //     else
        //     {
        //         //Old password is only useful for changing the password.
        //         this.specifyOldPwdCheckBox.Enabled = false;
        //         this.oldPasswordLabel.Enabled = false;
        //         this.oldPasswordTextBox.Enabled = false;
        //     }
        // }

        // private void UpdateUiControlsWithUserTypeChanges()
        // {
        //     IUserPrototype prototype = this.currentUserPrototype as IUserPrototype;
        //     IUserPrototypeWithMappedLogin mappedLoginPrototype = this.currentUserPrototype
        //                                                                     as IUserPrototypeWithMappedLogin;

        //     if (prototype != null) //can be any type of user prototype.
        //     {
        //         if (prototype.UserType == UserType.AsymmetricKey)
        //         {
        //             this.mappedObjectLabel.Text = UserSR.AsymmetricKeyNameLabel;
        //             this.mappedObjTextbox.Text = this.currentUserPrototype.AsymmetricKeyName;
        //             this.mappedObjTextbox.Show();
        //             this.mappedObjectLabel.Show();
        //             this.mappedObjSearchButton.Show();
        //         }
        //         else if (prototype.UserType == UserType.Certificate)
        //         {
        //             this.mappedObjectLabel.Text = UserSR.CertificateNameLabel;
        //             this.mappedObjTextbox.Text = this.currentUserPrototype.CertificateName;
        //             this.mappedObjTextbox.Show();
        //             this.mappedObjectLabel.Show();
        //             this.mappedObjSearchButton.Show();
        //         }
        //         else if (mappedLoginPrototype != null) //is a SQL user mapped with a login.
        //         {
        //             this.mappedObjectLabel.Text = UserSR.LoginNameLabel;
        //             this.mappedObjTextbox.Text = mappedLoginPrototype.LoginName;
        //             this.mappedObjTextbox.Show();
        //             this.mappedObjectLabel.Show();
        //             this.mappedObjSearchButton.Show();
        //         }
        //         else
        //         {
        //             this.mappedObjTextbox.Hide();
        //             this.mappedObjectLabel.Hide();
        //             this.mappedObjSearchButton.Hide();
        //         }
        //     }

        //     IUserPrototypeWithPassword userWithPwdPrototype = this.currentUserPrototype
        //                                                                 as IUserPrototypeWithPassword;
        //     if (userWithPwdPrototype != null)
        //     {
        //         this.passwordLabel.Show();
        //         this.passwordTextBox.Show();
        //         this.confirmPwdLabel.Show();
        //         this.confirmPwdTextBox.Show();
        //         this.specifyOldPwdCheckBox.Show();
        //         this.oldPasswordLabel.Show();
        //         this.oldPasswordTextBox.Show();
        //     }
        //     else
        //     {
        //         this.passwordLabel.Hide();
        //         this.passwordTextBox.Hide();
        //         this.confirmPwdLabel.Hide();
        //         this.confirmPwdTextBox.Hide();
        //         this.specifyOldPwdCheckBox.Hide();
        //         this.oldPasswordLabel.Hide();
        //         this.oldPasswordTextBox.Hide();
        //     }

        //     IUserPrototypeWithDefaultLanguage userWithDefaultLanguagePrototype = this.currentUserPrototype
        //                                                                                 as IUserPrototypeWithDefaultLanguage;
        //     if (userWithDefaultLanguagePrototype != null
        //         && userWithDefaultLanguagePrototype.IsDefaultLanguageSupported)
        //     {
        //         this.defaultLanguageLabel.Show();
        //         this.defaultLanguageComboBox.Show();
        //     }
        //     else
        //     {
        //         this.defaultLanguageLabel.Hide();
        //         this.defaultLanguageComboBox.Hide();
        //     }

        //     IUserPrototypeWithDefaultSchema userWithDefaultSchemaPrototype = this.currentUserPrototype
        //                                                                                 as IUserPrototypeWithDefaultSchema;
        //     if (userWithDefaultSchemaPrototype != null
        //         && userWithDefaultSchemaPrototype.IsDefaultSchemaSupported)
        //     {
        //         this.defaultSchemaLabel.Show();
        //         this.defaultSchemaTextBox.Show();
        //         this.defaultSchSearchButton.Show();
        //     }
        //     else
        //     {
        //         this.defaultSchemaLabel.Hide();
        //         this.defaultSchemaTextBox.Hide();
        //         this.defaultSchSearchButton.Hide();
        //     }

        //     if (this.currentUserType == ExhaustiveUserTypes.WindowsUser)
        //     {
        //         this.userSearchButton.Enabled = true;
        //         if (!this.IsParentDatabaseContained())
        //         {
        //             this.defaultLanguageLabel.Enabled = false;
        //             this.defaultLanguageComboBox.Enabled = false;
        //         }
        //     }
        //     else
        //     {
        //         this.userSearchButton.Enabled = false;
        //         if (!this.IsParentDatabaseContained())
        //         {
        //             this.defaultLanguageLabel.Enabled = true;
        //             this.defaultLanguageComboBox.Enabled = true;
        //         }
        //     }
        // }

        // private void PopulateUserTypeComboBox()
        // {
        //     if (SqlMgmtUtils.IsSql11OrLater(this.DataContainer.Server.ServerVersion)
        //         && this.IsParentDatabaseContained())
        //     {
        //         this.userTypeComboBox.Items.AddRange(
        //         new string[]{
        //             UserSR.SqlUserWithPasswordUserTypeText
        //             }
        //         );
        //     }
        //     if (SqlMgmtUtils.IsYukonOrAbove(this.DataContainer.Server))
        //     {
        //         this.userTypeComboBox.Items.AddRange(
        //         new string[]{
        //             UserSR.AsymmetricKeyUserTypeText,
        //             UserSR.CertificateUserTypeText,
        //             UserSR.WithoutLoginSqlUserTypeText,               
        //             UserSR.WindowsUserTypeText
        //             }
        //         );
        //     }
        //     this.userTypeComboBox.Items.AddRange(
        //         new string[]{
        //             UserSR.LoginMappedSqlUserTypeText
        //             }
        //         );
        // }

        // private void PopulateDefaultLanguageComboBox()
        // {
        //     this.defaultLanguageComboBox.Items.Clear();            
        //     this.defaultLanguageComboBox.Items.Add(defaultLanguagePlaceholder);            

        //     // sort the languages alphabetically by alias
        //     SortedList sortedLanguages = new SortedList(Comparer.Default);

        //     LanguageUtils.SetLanguageDefaultInitFieldsForDefaultLanguages(DataContainer.Server);
        //     foreach (Language language in this.DataContainer.Server.Languages)
        //     {
        //         LanguageDisplay listValue = new LanguageDisplay(language);
        //         sortedLanguages.Add(language.Alias, listValue);
        //     }

        //     // add the language display objects to the combo box
        //     foreach (LanguageDisplay languageDisplay in sortedLanguages.Values)
        //     {
        //         this.defaultLanguageComboBox.Items.Add(languageDisplay);
        //     }
        // }

        // private ExhaustiveUserTypes GetUserTypeEnumFromText(string userTypeText)
        // {
        //     if (0 == string.Compare(userTypeText, UserSR.AsymmetricKeyUserTypeText, StringComparison.Ordinal))
        //     {
        //         return ExhaustiveUserTypes.AsymmetricKeyMappedUser;
        //     }
        //     else if (0 == string.Compare(userTypeText, UserSR.CertificateUserTypeText, StringComparison.Ordinal))
        //     {
        //         return ExhaustiveUserTypes.CertificateMappedUser;
        //     }
        //     else if (0 == string.Compare(userTypeText, UserSR.LoginMappedSqlUserTypeText, StringComparison.Ordinal))
        //     {
        //         return ExhaustiveUserTypes.LoginMappedUser;
        //     }
        //     else if (0 == string.Compare(userTypeText, UserSR.WithoutLoginSqlUserTypeText, StringComparison.Ordinal))
        //     {
        //         return ExhaustiveUserTypes.SqlUserWithoutLogin;
        //     }
        //     else if (0 == string.Compare(userTypeText, UserSR.SqlUserWithPasswordUserTypeText, StringComparison.Ordinal))
        //     {
        //         return ExhaustiveUserTypes.SqlUserWithPassword;
        //     }
        //     else if (0 == string.Compare(userTypeText, UserSR.WindowsUserTypeText, StringComparison.Ordinal))
        //     {
        //         return ExhaustiveUserTypes.WindowsUser;
        //     }
        //     else
        //     {
        //         System.Diagnostics.Debug.Assert(false, "Unknown UserType Text provided");
        //         return ExhaustiveUserTypes.Unknown;
        //     }
        // }

        // private string GetUserTypeTextFromEnum(ExhaustiveUserTypes userTypeEnum)
        // {
        //     switch (userTypeEnum)
        //     {
        //         case ExhaustiveUserTypes.AsymmetricKeyMappedUser:
        //             return UserSR.AsymmetricKeyUserTypeText;
                    
        //         case ExhaustiveUserTypes.CertificateMappedUser:
        //             return UserSR.CertificateUserTypeText;
                    
        //         case ExhaustiveUserTypes.LoginMappedUser:
        //             return UserSR.LoginMappedSqlUserTypeText;
                    
        //         case ExhaustiveUserTypes.SqlUserWithoutLogin:
        //             return UserSR.WithoutLoginSqlUserTypeText;
                    
        //         case ExhaustiveUserTypes.SqlUserWithPassword:
        //             return UserSR.SqlUserWithPasswordUserTypeText;
                    
        //         case ExhaustiveUserTypes.WindowsUser:
        //             return UserSR.WindowsUserTypeText;
                    
        //         default:
                    
        //             return null;                    
        //     }
        // }

        private ExhaustiveUserTypes GetCurrentUserTypeForExistingUser(User user)
        {
            switch (user.UserType)
            {
                case UserType.SqlUser:
                    if (user.IsSupportedProperty("AuthenticationType"))
                    {
                        if (user.AuthenticationType == AuthenticationType.Windows)
                        {
                            return ExhaustiveUserTypes.WindowsUser;                            
                        }
                        else if (user.AuthenticationType == AuthenticationType.Database)
                        {
                            return ExhaustiveUserTypes.SqlUserWithPassword;
                        }
                    }

                    return ExhaustiveUserTypes.LoginMappedUser;
                    
                case UserType.NoLogin:
                    return ExhaustiveUserTypes.SqlUserWithoutLogin;
                    
                case UserType.Certificate:
                    return ExhaustiveUserTypes.CertificateMappedUser;
                    
                case UserType.AsymmetricKey:
                    return ExhaustiveUserTypes.AsymmetricKeyMappedUser;
                    
                default:
                    return ExhaustiveUserTypes.Unknown;
            }
        }

        private bool IsParentDatabaseContained()
        {
            string parentDbName = parentDbUrn.GetNameForType("Database");
            Database parentDatabase = this.DataContainer.Server.Databases[parentDbName];

            if (parentDatabase.IsSupportedProperty("ContainmentType")
                && parentDatabase.ContainmentType == ContainmentType.Partial)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Perform UI tasks that need to happen in the UI thread before OnRunNow is called
        /// </summary>
        // public override void OnGatherUiInformation(RunType runType)
        // {
        //     base.OnGatherUiInformation(runType);

        //     // check whether the user has a name
        //     if (0 == this.currentUserPrototype.Name.Trim().Length)
        //     {
        //         this.LaunchForm.SelectView(0);
        //         this.userNameTextBox.Focus();
        //         throw new EmptyNameException();
        //     }

        //     this.VerifyMappedObjectName();

        //     if (this.currentUserType == ExhaustiveUserTypes.SqlUserWithPassword
        //         && this.passwordTextBox.Text != this.confirmPwdTextBox.Text)
        //     {
        //         this.LaunchForm.SelectView(0);
        //         this.passwordTextBox.Focus();
        //         throw new InvalidOperationException(UserSR.Error_DifferentPasswordAndConfirmPassword);
        //     }
        // }

        // private void VerifyMappedObjectName()
        // {
        //     if ((this.currentUserType == ExhaustiveUserTypes.AsymmetricKeyMappedUser
        //             || this.currentUserType == ExhaustiveUserTypes.CertificateMappedUser
        //             || this.currentUserType == ExhaustiveUserTypes.LoginMappedUser)
        //         && 0 == this.mappedObjTextbox.Text.Trim().Length)
        //     {
        //         this.LaunchForm.SelectView(0);
        //         this.mappedObjTextbox.Focus();

        //         string errorMsg = string.Empty;
        //         if (this.currentUserType == ExhaustiveUserTypes.AsymmetricKeyMappedUser)
        //         {
        //             errorMsg = UserSR.Error_SpecifyAnAsymmetricKey;
        //         }
        //         else if (this.currentUserType == ExhaustiveUserTypes.CertificateMappedUser)
        //         {
        //             errorMsg = UserSR.Error_SpecifyACertificate;
        //         }
        //         else if (this.currentUserType == ExhaustiveUserTypes.LoginMappedUser)
        //         {
        //             errorMsg = UserSR.Error_SpecifyALogin;
        //         }

        //         throw new InvalidOperationException(errorMsg);
        //     }
        // }

        /// <summary>
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        // public void OnRunNow(object sender)
        // {
        //     User user = this.currentUserPrototype.ApplyChanges();
            
        //     this.ExecutionMode = ExecutionMode.Success;
        //     this.DataContainer.ObjectName = this.currentUserPrototype.Name;
        //     this.DataContainer.SqlDialogSubject = user;
        // }

        // #region IPanelForm Members

        // void IPanelForm.OnInitialization()
        // {            
        // }

        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {
        //     if (!this.currentUserPrototype.Exists)
        //     {
        //         this.DataContainer.ObjectName = this.currentUserPrototype.Name;
        //     }
        // }

        // void IPanelForm.OnSelection(TreeNode node)
        // {            
        // }

        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {
        //         return this;
        //     }
        // }        

        // #endregion

        // private void OnUserTypeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        // {
        //     this.currentUserType = this.GetUserTypeEnumFromText(this.userTypeComboBox.Text);
        //     this.currentUserPrototype = this.userPrototypeFactory.GetUserPrototype(this.currentUserType);

        //     this.UpdateUiControlsWithUserTypeChanges();
        // }

        // private void OnSpecifyOldPasswordCheckedChanged(object sender, EventArgs e)
        // {
        //     this.oldPasswordTextBox.ReadOnly = !this.specifyOldPwdCheckBox.Checked;

        //     (this.currentUserPrototype as IUserPrototypeWithPassword).IsOldPasswordRequired 
        //         = this.specifyOldPwdCheckBox.Checked;
        // }

        // private void OnUserNameTextBoxTextChanged(object sender, EventArgs e)
        // {
        //     this.currentUserPrototype.Name = this.userNameTextBox.Text;
        // }

        // private void OnMappedObjectTextBoxTextChanged(object sender, EventArgs e)
        // {
        //     IUserPrototypeWithMappedLogin mappedLoginPrototype = this.currentUserPrototype
        //                                                                         as IUserPrototypeWithMappedLogin;

        //     if (this.currentUserType == ExhaustiveUserTypes.AsymmetricKeyMappedUser)
        //     {
        //         this.currentUserPrototype.AsymmetricKeyName = this.mappedObjTextbox.Text;
        //     }
        //     else if (this.currentUserType == ExhaustiveUserTypes.CertificateMappedUser)
        //     {
        //         this.currentUserPrototype.CertificateName = this.mappedObjTextbox.Text;
        //     }
        //     else if (mappedLoginPrototype != null) //Either be a Sql user with login, Windows user or Windows Group
        //     {
        //         mappedLoginPrototype.LoginName = this.mappedObjTextbox.Text;
        //     }
        // }

        // private void OnPasswordTextBoxTextChanged(object sender, EventArgs e)
        // {
        //     if (!this.isInitializing)
        //     {
        //         IUserPrototypeWithPassword passwordUserPrototype = this.currentUserPrototype
        //                                                                     as IUserPrototypeWithPassword;
        //         if (passwordUserPrototype != null)
        //         {
        //             passwordUserPrototype.Password = this.GetReadOnlySecureString(this.passwordTextBox.Text);
        //         }
        //     }
        // }

        // private SecureString GetReadOnlySecureString(string secret)
        // {
        //     SecureString ss = new SecureString();
        //     foreach (char c in secret.ToCharArray())
        //     {
        //         ss.AppendChar(c);
        //     }
        //     ss.MakeReadOnly();

        //     return ss;
        // }

        // private void OnConfirmPwdTextBoxTextChanged(object sender, EventArgs e)
        // {
        //     if (!this.isInitializing)
        //     {
        //         IUserPrototypeWithPassword passwordUserPrototype = this.currentUserPrototype
        //                                                                     as IUserPrototypeWithPassword;
        //         if (passwordUserPrototype != null)
        //         {
        //             passwordUserPrototype.PasswordConfirm = this.GetReadOnlySecureString(this.confirmPwdTextBox.Text);
        //         }
        //     }
        // }

        // private void OnOldPasswordTextBoxTextChanged(object sender, EventArgs e)
        // {
        //     IUserPrototypeWithPassword passwordUserPrototype = this.currentUserPrototype
        //                                                                 as IUserPrototypeWithPassword;
        //     if (passwordUserPrototype != null)
        //     {
        //         passwordUserPrototype.OldPassword = this.GetReadOnlySecureString(this.oldPasswordTextBox.Text);
        //     }
        // }

        // private void OnDefaultLanguageSelectedIndexChanged(object sender, EventArgs e)
        // {
        //     if (!this.isInitializing)
        //     {
        //         IUserPrototypeWithDefaultLanguage defaultLanguagePrototype = this.currentUserPrototype
        //                                                                                 as IUserPrototypeWithDefaultLanguage;
        //         if (defaultLanguagePrototype != null)
        //         {
        //             defaultLanguagePrototype.DefaultLanguageAlias = this.defaultLanguageComboBox.Text;
        //         }
        //     }
        // }

        // private void OnDefaultSchemaTextBoxTextChanged(object sender, EventArgs e)
        // {
        //     if (!this.isInitializing)
        //     {
        //         IUserPrototypeWithDefaultSchema defaultSchemaPrototype = this.currentUserPrototype
        //                                                                             as IUserPrototypeWithDefaultSchema;
        //         if (defaultSchemaPrototype != null)
        //         {
        //             defaultSchemaPrototype.DefaultSchema = this.defaultSchemaTextBox.Text;
        //         }
        //     }
        // }

        // private void OnWindowsUserOrGroupSearch(object sender, EventArgs e)
        // {
        //     try
        //     {
        //         this.userSearchButton.Enabled = false;                

        //         string windowsLoginName = CUtils.GetWindowsLoginNameFromObjectPicker(this,
        //                                                     this.DataContainer.Server,
        //                                                     UserSR.Error_TooManyLogins);

        //         if(windowsLoginName != null)
        //         {
        //             this.userNameTextBox.Text = windowsLoginName;
        //         }

        //     }
        //     finally
        //     {
        //         this.userSearchButton.Enabled = true;
        //     }
        // }

        // private void OnMappedObjSearchButtonClicked(object sender, EventArgs e)
        // {
        //     SearchableObjectTypeCollection types = null;
        //     string title = null;
            
        //     if (this.currentUserType == ExhaustiveUserTypes.AsymmetricKeyMappedUser)
        //     {
        //         types = new SearchableObjectTypeCollection(SearchableObjectType.AsymmetricKey);
        //         title = UserSR.TitleSearchAsymmetricKey;
        //     }
        //     else if (this.currentUserType == ExhaustiveUserTypes.CertificateMappedUser)
        //     {
        //         types = new SearchableObjectTypeCollection(SearchableObjectType.Certificate);
        //         title = UserSR.TitleSearchCertificate;
        //     }
        //     else
        //     {
        //         IUserPrototypeWithMappedLogin mappedLoginPrototype
        //             = this.currentUserPrototype as IUserPrototypeWithMappedLogin;

        //         if (mappedLoginPrototype != null)
        //         {
        //             types = new SearchableObjectTypeCollection(SearchableObjectType.Login);
        //             title = UserSR.TitleSearchLogin;
        //         }
        //         else
        //         {
        //             System.Diagnostics.Debug.Assert(false, "Mapped Object button should not be enabled in this case.");
        //             return;
        //         }
        //     }

        //     using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
        //                                                      null,
        //                                                      this.HelpProvider,
        //                                                      title,
        //                                                      this.DataContainer.ConnectionInfo,
        //                                                      this.DataContainer.GetDocumentPropertyString("database"),
        //                                                      types,
        //                                                      types))
        //     {
        //         if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
        //         {
        //             SearchableObject mappedObject = dlg.SearchResults[0];
        //             this.mappedObjTextbox.Text = mappedObject.Name;
        //         }
        //     }
        // }

        // private void OnDefaultSchSearchButtonClicked(object sender, EventArgs e)
        // {
        //     // pop up the object picker to select a schema.
        //     using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
        //                                                      null,
        //                                                      this.HelpProvider,
        //                                                      UserSR.TitleSearchSchema,
        //                                                      this.DataContainer.ConnectionInfo,
        //                                                      this.DataContainer.GetDocumentPropertyString("database"),
        //                                                      new SearchableObjectTypeCollection(SearchableObjectType.Schema),
        //                                                      new SearchableObjectTypeCollection(SearchableObjectType.Schema)))
        //     {
        //         if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
        //         {
        //             SearchableObject schemaname = dlg.SearchResults[0];
        //             this.defaultSchemaTextBox.Text = schemaname.Name;
        //         }
        //     }
        // }
    }
}
