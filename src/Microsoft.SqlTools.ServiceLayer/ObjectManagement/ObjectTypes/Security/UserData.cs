//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Defines the common behavior of all types of database user objects.
    /// </summary>
    internal interface IUserPrototype
    {
        string Name { get; set; }
        UserType UserType { get; set; }
        string AsymmetricKeyName { get; set; }
        string CertificateName { get; set; }
        bool IsSystemObject { get; }
        bool Exists { get; }
        List<string> SchemaNames { get; }
        bool IsSchemaOwner(string schemaName);
        void SetSchemaOwner(string schemaName, bool isOwner);
        List<string> DatabaseRoleNames { get; }
        bool IsRoleMember(string roleName);
        void SetRoleMembership(string roleName, bool isMember);
    }

    /// <summary>
    /// Defines the behavior of those database users which have default schema.
    /// </summary>
    internal interface IUserPrototypeWithDefaultSchema
    {
        bool IsDefaultSchemaSupported { get; }
        string DefaultSchema { get; set; }
    }

    /// <summary>
    /// Defines the behavior of those database users which are mapped to a login.
    /// </summary>
    internal interface IUserPrototypeWithMappedLogin
    {
        string LoginName { get; set; }
    }

    /// <summary>
    /// Defines the behavior of those database users which have password.
    /// </summary>
    internal interface IUserPrototypeWithPassword
    {
        SecureString Password { set; }
        SecureString PasswordConfirm { set; }
        SecureString OldPassword { set; }
        bool IsOldPasswordRequired { get; set; }
    }

    /// <summary>
    /// Defines the behavior of those database users which have default language.
    /// </summary>
    internal interface IUserPrototypeWithDefaultLanguage
    {
        bool IsDefaultLanguageSupported { get; }
        string DefaultLanguageAlias { get; set; }
    }

    /// <summary>
    /// Object have exhaustive list of data elements which are required for creating 
    /// any type of database user.
    /// </summary>
    public class UserPrototypeData
    {
        public string name = string.Empty;
        public UserType userType = UserType.SqlUser;
        public bool isSystemObject = false;
        public Dictionary<string, bool> isSchemaOwned;
        public Dictionary<string, bool> isMember;

        public AuthenticationType authenticationType = AuthenticationType.Instance;
        public string mappedLoginName = string.Empty;
        public string certificateName = string.Empty;
        public string asymmetricKeyName = string.Empty;
        public string defaultSchemaName = string.Empty;        
        public string defaultLanguageAlias = string.Empty;
        public SecureString password = new SecureString();
        public SecureString passwordConfirm = new SecureString();
        public SecureString oldPassword = new SecureString();
        public bool isOldPasswordRequired = false;

        /// <summary>
        /// Used for creating clone of a UserPrototypeData.
        /// </summary>
        private UserPrototypeData()
        {
            this.isSchemaOwned = new Dictionary<string, bool>();
            this.isMember = new Dictionary<string, bool>();
        }

        public UserPrototypeData(CDataContainer context, UserInfo? userInfo)
        {
            this.isSchemaOwned = new Dictionary<string, bool>();
            this.isMember = new Dictionary<string, bool>();

            // load user properties from SMO object
            if (!context.IsNewObject)
            {
                this.LoadUserData(context);
            }
     
            // apply user properties provided by client
            if (userInfo != null)
            {
                this.name = userInfo.Name;
                this.mappedLoginName = userInfo.LoginName;
                this.defaultSchemaName = userInfo.DefaultSchema;
                if (!string.IsNullOrEmpty(userInfo.Password))
                {                    
                    this.password = DatabaseUtils.GetReadOnlySecureString(userInfo.Password);
                }
                if (!string.IsNullOrEmpty(userInfo.DefaultLanguage)
                    && string.Compare(userInfo.DefaultLanguage, SR.DefaultLanguagePlaceholder, StringComparison.Ordinal) != 0)
                {
                    this.defaultLanguageAlias = LanguageUtils.GetLanguageAliasFromDisplayText(userInfo.DefaultLanguage);                        
                }
                this.userType = UserPrototypeData.GetUserTypeFromUserInfo(userInfo);
            }     

            this.LoadRoleMembership(context, userInfo);

            this.LoadSchemaData(context, userInfo);
        }

        public static UserType GetUserTypeFromUserInfo(UserInfo userInfo)
        {
            UserType userType = UserType.SqlLogin;
            switch (userInfo.Type)
            {
                case DatabaseUserType.NoConnectAccess:
                    userType = UserType.NoLogin;
                    break;
                case DatabaseUserType.Contained:
                    if (userInfo.AuthenticationType == ServerAuthenticationType.AzureActiveDirectory)
                    {
                        userType = UserType.External;
                    }
                    break;
                // all the other user types are using SqlLogin
            }
            return userType;
        }

        public UserPrototypeData Clone()
        {
            UserPrototypeData result = new UserPrototypeData();

            result.asymmetricKeyName = this.asymmetricKeyName;
            result.authenticationType = this.authenticationType;
            result.certificateName = this.certificateName;
            result.defaultLanguageAlias = this.defaultLanguageAlias;
            result.defaultSchemaName = this.defaultSchemaName;
            result.isSystemObject = this.isSystemObject;
            result.mappedLoginName = this.mappedLoginName;
            result.name = this.name;
            result.oldPassword = this.oldPassword;
            result.password = this.password;
            result.passwordConfirm = this.passwordConfirm;
            result.isOldPasswordRequired = this.isOldPasswordRequired;
            result.userType = this.userType;

            foreach (string key in this.isMember?.Keys ?? Enumerable.Empty<string>())
            {
                result.isMember[key] = this.isMember[key];
            }

            foreach (string key in this.isSchemaOwned?.Keys ?? Enumerable.Empty<string>())
            {
                 result.isSchemaOwned[key] = this.isSchemaOwned[key];
            }

            return result;
        }

        public bool HasSameValueAs(UserPrototypeData other)
        {
            bool result =
                (this.asymmetricKeyName == other.asymmetricKeyName) &&
                (this.authenticationType == other.authenticationType) &&
                (this.certificateName == other.certificateName) &&
                (this.defaultLanguageAlias == other.defaultLanguageAlias) &&
                (this.defaultSchemaName == other.defaultSchemaName) &&
                (this.isSystemObject == other.isSystemObject) &&
                (this.mappedLoginName == other.mappedLoginName) &&
                (this.name == other.name) &&
                (this.oldPassword == other.oldPassword) &&
                (this.password == other.password) &&
                (this.passwordConfirm == other.passwordConfirm) &&
                (this.isOldPasswordRequired == other.isOldPasswordRequired) &&
                (this.userType == other.userType);

            result = result && this.isMember?.Keys.Count == other.isMember?.Keys.Count;
            if (result)
            {
                foreach (string key in this.isMember?.Keys ?? Enumerable.Empty<string>())
                {
                    if (this.isMember?.ContainsKey(key) == true
                        && other.isMember?.ContainsKey(key) == true
                        && this.isMember[key] != other.isMember[key])
                    {
                        result = false;
                        break;
                    }
                }
            }

            result = result && this.isSchemaOwned.Keys.Count == other.isSchemaOwned.Keys.Count;
            if (result)
            {
                foreach (string key in this.isSchemaOwned.Keys)
                {
                    if (this.isSchemaOwned[key] != other.isSchemaOwned[key])
                    {
                        result = false;
                        break;
                    }
                }
            }     
            
            return result;
        }

        /// <summary>
        /// Initializes this object with values from the existing database user.
        /// </summary>
        /// <param name="context"></param>
        private void LoadUserData(CDataContainer context)
        {
            User? existingUser = context.Server.GetSmoObject(new Urn(context.ObjectUrn)) as User;
            if (existingUser == null)
            {
                return;
            }

            this.name = existingUser.Name;
            this.mappedLoginName = existingUser.Login;
            this.isSystemObject = existingUser.IsSystemObject;

            if (SqlMgmtUtils.IsYukonOrAbove(context.Server))
            {
                this.asymmetricKeyName = existingUser.AsymmetricKey;
                this.certificateName = existingUser.Certificate;
                this.defaultSchemaName = existingUser.DefaultSchema;
                this.userType = existingUser.UserType;
            }

            if (SqlMgmtUtils.IsSql11OrLater(context.Server.ServerVersion))
            {
                this.authenticationType = existingUser.AuthenticationType;

                if (context.Server.ServerType != DatabaseEngineType.SqlAzureDatabase)
                {
                    this.defaultLanguageAlias = LanguageUtils.GetLanguageAliasFromName(
                                                                    existingUser.Parent.Parent,
                                                                    existingUser.DefaultLanguage.Name);
                }
            }
        }

        /// <summary>
        /// Loads role membership of a database user.
        /// </summary>
        /// <param name="context"></param>
        private void LoadRoleMembership(CDataContainer context, UserInfo? userInfo)
        {
            Urn objUrn = new Urn(context.ObjectUrn);
            Urn databaseUrn = objUrn.Parent;

            Database? parentDb = context.Server.GetSmoObject(databaseUrn) as Database;
            if (parentDb == null)
            {
                return;
            }

            string userName = userInfo?.Name ?? objUrn.GetNameForType("User");
            User existingUser = context.Server.Databases[parentDb.Name].Users[userName];

            foreach (DatabaseRole dbRole in parentDb.Roles)
            {
                var comparer = parentDb.GetStringComparer();
                if (comparer.Compare(dbRole.Name, "public") != 0)
                {
                    if (userInfo != null && userInfo.DatabaseRoles != null)
                    {
                        this.isMember[dbRole.Name]  = userInfo.DatabaseRoles.Contains(dbRole.Name);
                    }
                    else if (existingUser != null)
                    {
                        this.isMember[dbRole.Name] = existingUser.IsMember(dbRole.Name);
                    }
                    else
                    {
                        this.isMember[dbRole.Name] = false;
                    }
                }
            }
        }

        /// <summary>
        /// Loads schema ownership related data.
        /// </summary>
        /// <param name="context"></param>
        private void LoadSchemaData(CDataContainer context, UserInfo? userInfo)
        {
            Urn objUrn = new Urn(context.ObjectUrn);
            Urn databaseUrn = objUrn.Parent;

            Database? parentDb = context.Server.GetSmoObject(databaseUrn) as Database;
            if (parentDb == null)
            {
                return;
            }

            string userName = userInfo?.Name ?? objUrn.GetNameForType("User");
            User existingUser = context.Server.Databases[parentDb.Name].Users[userName];

            if (!SqlMgmtUtils.IsYukonOrAbove(context.Server)
                || parentDb.CompatibilityLevel <= CompatibilityLevel.Version80)
            {
                return;
            }          

            foreach (Schema sch in parentDb.Schemas)
            {
                if (userInfo != null && userInfo.OwnedSchemas != null)
                {
                    this.isSchemaOwned[sch.Name]  = userInfo.OwnedSchemas.Contains(sch.Name);
                }
                else if (existingUser != null)
                {
                    var comparer = parentDb.GetStringComparer();
                    this.isSchemaOwned[sch.Name] = comparer.Compare(sch.Owner, existingUser.Name) == 0;
                }
                else
                {
                    this.isSchemaOwned[sch.Name] = false;
                }                
            }
        }
    }

    /// <summary>
	/// Prototype object for creating or altering users
	/// </summary>
    internal class UserPrototype : IUserPrototype
    {
        protected UserPrototypeData originalState;
        protected UserPrototypeData currentState;

        private List<string> schemaNames;
        private List<string> roleNames;
        private bool exists = false;
        private Database parent;
        private CDataContainer context;

        public bool IsRoleMembershipChangesApplied { get; set; } //default is false
        public bool IsSchemaOwnershipChangesApplied { get; set; } //default is false

        #region IUserPrototype Members

        public UserPrototypeData CurrentState
        {
            get
            {
                return this.currentState;
            }
        }

        public string Name
        {
            get
            {
                return this.currentState.name;
            }
            set
            {
                this.currentState.name = value;
            }
        }

        public string CertificateName
        {
            get
            {
                return this.currentState.certificateName;
            }
            set
            {
                this.currentState.certificateName = value;
            }
        }

        public string AsymmetricKeyName
        {
            get
            {
                return this.currentState.asymmetricKeyName;
            }
            set
            {
                this.currentState.asymmetricKeyName = value;
            }
        }

        public UserType UserType
        {
            get
            {
                return this.currentState.userType;
            }
            set
            {
                this.currentState.userType = value;
            }
        }

        public bool IsSystemObject
        {
            get
            {
                return this.currentState.isSystemObject;
            }
        }

        public bool Exists
        {
            get
            {
                return this.exists;
            }
        }

        public List<string> SchemaNames
        {
            get
            {
                return this.schemaNames;
            }
        }

        public List<string> DatabaseRoleNames
        {
            get
            {
                return this.roleNames;
            }
        }

        public bool IsSchemaOwner(string schemaName)
        {
            bool isSchemaOwner = false;
            this.currentState.isSchemaOwned.TryGetValue(schemaName, out isSchemaOwner);
            
            return isSchemaOwner;
        }

        public void SetSchemaOwner(string schemaName, bool isOwner)
        {
            this.currentState.isSchemaOwned[schemaName] = isOwner;
        }

        public bool IsRoleMember(string roleName)
        {
            bool isRoleMember = false;
            this.currentState.isMember.TryGetValue(roleName, out isRoleMember);
            
            return isRoleMember;
        }

        public void SetRoleMembership(string roleName, bool isMember)
        {
            this.currentState.isMember[roleName] = isMember;
        }

        #endregion

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">The context for the dialog</param>
		public UserPrototype(CDataContainer context, 
                                UserPrototypeData current,
                                UserPrototypeData original)
		{
            this.currentState = current;
            this.originalState = original;
            this.exists = !context.IsNewObject;
            this.context = context;

            Database? parent = context.Server.GetSmoObject(new Urn(context.ParentUrn)) as Database ?? throw new ArgumentException("Context ParentUrn is invalid");
            this.parent = parent;

            this.roleNames = this.PopulateRoles();
            this.schemaNames = this.PopulateSchemas();
        }

        private List<string> PopulateRoles()
        {
            var roleNames = new List<string>();

            foreach (DatabaseRole dbRole in this.parent.Roles)
            {
                var comparer = this.parent.GetStringComparer();
                if (comparer.Compare(dbRole.Name, "public") != 0)
                {
                    roleNames.Add(dbRole.Name);
                }
            }
            return roleNames;
        }

        private List<string> PopulateSchemas()
        {
            var schemaNames = new List<string>();

            if (!SqlMgmtUtils.IsYukonOrAbove(this.parent.Parent)
                || this.parent.CompatibilityLevel <= CompatibilityLevel.Version80)
            {
                throw new ArgumentException("Unsupported server version");
            }

            foreach (Schema sch in this.parent.Schemas)
            {
                schemaNames.Add(sch.Name);
            }
            return schemaNames;
        }

        public bool IsYukonOrLater
        {
            get
            {
                return SqlMgmtUtils.IsYukonOrAbove(this.parent.Parent);
            }
        }

        public User ApplyChanges(Database parentDb)
        {
            User user = this.GetUser(parentDb);

            if (this.ChangesExist())
            {
                this.SaveProperties(user);
                this.CreateOrAlterUser(user);
                
                //Extended Properties page also executes Alter() method on the same user object
                //in order to add extended properties. If at that time any property is dirty, 
                //it will again generate the script corresponding to that.
                user.Refresh();

                this.ApplySchemaOwnershipChanges(parentDb, user);
                this.IsSchemaOwnershipChangesApplied = true;

                this.ApplyRoleMembershipChanges(parentDb, user);
                this.IsRoleMembershipChangesApplied = true;
            }

            return user;
        }

        protected virtual void CreateOrAlterUser(User user)
        {
            if (!this.Exists)
            {
                user.Create();
            }
            else
            {
                user.Alter();
            }
        }

        private void ApplySchemaOwnershipChanges(Database parentDb, User user)
        {
            IEnumerator<KeyValuePair<string, bool>>? enumerator = this.currentState.isSchemaOwned?.GetEnumerator();
            if (enumerator != null)
            {
                enumerator.Reset();

                string? nullString = null;

                while (enumerator.MoveNext())
                {
                    string schemaName = enumerator.Current.Key.ToString();
                    bool userIsOwner = (bool)enumerator.Current.Value;

                    if (this.originalState.isSchemaOwned?[schemaName] != userIsOwner)
                    {
                        System.Diagnostics.Debug.Assert(!this.Exists || userIsOwner, "shouldn't have to unset ownership for new users");

                        Schema schema = parentDb.Schemas[schemaName];
                        schema.Owner = userIsOwner ? user.Name : nullString;
                        schema.Alter();
                    }
                }
            }
        }

        private void ApplyRoleMembershipChanges(Database parentDb, User user)
        {
            IEnumerator<KeyValuePair<string, bool>>? enumerator = this.currentState.isMember?.GetEnumerator();
            if (enumerator != null)
            {
                enumerator.Reset();

                while (enumerator.MoveNext())
                {
                    string roleName = enumerator.Current.Key;
                    bool userIsMember = enumerator.Current.Value;

                    if (this.originalState.isMember?[roleName] != userIsMember)
                    {
                        System.Diagnostics.Debug.Assert(this.Exists || userIsMember, "shouldn't have to unset membership for new users");

                        DatabaseRole role = parentDb.Roles[roleName];

                        if (userIsMember)
                        {
                            role.AddMember(user.Name);
                        }
                        else
                        {
                            role.DropMember(user.Name);
                        }
                    }
                }
            }
        }

        protected virtual void SaveProperties(User user)
        {
            if (!this.Exists || (user.UserType != this.currentState.userType))
            {
                user.UserType = this.currentState.userType;
            }

            if ((this.currentState.userType == UserType.Certificate)
                &&(!this.Exists || (user.Certificate != this.currentState.certificateName)))
            {
                user.Certificate = this.currentState.certificateName;
            }

            if ((this.currentState.userType == UserType.AsymmetricKey)
                && (!this.Exists || (user.AsymmetricKey != this.currentState.asymmetricKeyName)))
            {
                user.AsymmetricKey = this.currentState.asymmetricKeyName;
            }
        }

        public User GetUser(Database parentDb)
        {
            User result;

            // if we think we exist, get the SMO user object
            if (this.Exists)
            {
                result = parentDb.Users[this.originalState.name];
                result?.Refresh();

                System.Diagnostics.Debug.Assert(0 == string.Compare(this.originalState.name, this.currentState.name, StringComparison.Ordinal), "name of existing user has changed");
                if (result == null)
                {
                    throw new Exception();
                }
            }
            else
            {
                result = new User(parentDb, this.Name);
            }

            return result;
        }

        /// <summary>
        /// Will calling ApplyChanges do anything?
        /// </summary>
        /// <returns>True if there are changes to apply, false otherwise</returns>
        public bool ChangesExist()
        {
            bool result =
            !this.Exists ||
            !this.originalState.HasSameValueAs(this.currentState);

            return result;
        }

        public bool AADAuthSupported
        {
            get
            {
                return context?.Server?.ServerType == DatabaseEngineType.SqlAzureDatabase;
            }
        }

        public bool WindowsAuthSupported
        {
            get
            {
                return context?.Server?.ServerType != DatabaseEngineType.SqlAzureDatabase;
            }
        }
    }

    internal class UserPrototypeWithDefaultSchema : UserPrototype,
                                                    IUserPrototypeWithDefaultSchema
    {
        private CDataContainer context;

        #region IUserPrototypeWithDefaultSchema Members

        public virtual bool IsDefaultSchemaSupported
        {
            get
            {
                //Default Schema was not supported in Shiloh.
                return this.context.Server.ConnectionContext.ServerVersion.Major > 8 ;
            }
        }

        public string DefaultSchema
        {
            get
            {
                return this.currentState.defaultSchemaName;
            }
            set
            {
                this.currentState.defaultSchemaName = value;
            }
        }

        #endregion

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">The context for the dialog</param>
        public UserPrototypeWithDefaultSchema(CDataContainer context,
                                UserPrototypeData current,
                                UserPrototypeData original)
            : base(context, current, original)
        {
            this.context = context;
        }

        protected override void SaveProperties(User user)
        {
            base.SaveProperties(user);

            if (this.IsDefaultSchemaSupported
                && (!this.Exists || (user.DefaultSchema != this.currentState.defaultSchemaName)))
            {
                user.DefaultSchema = this.currentState.defaultSchemaName;
            }
        }
    }

    internal class UserPrototypeForSqlUserWithLogin : UserPrototypeWithDefaultSchema,
                                                        IUserPrototypeWithMappedLogin
    {

        #region IUserPrototypeWithMappedLogin Members

        public string LoginName
        {
            get
            {
                return this.currentState.mappedLoginName;
            }
            set
            {
                this.currentState.mappedLoginName = value;
            }
        }

        #endregion

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">The context for the dialog</param>
        public UserPrototypeForSqlUserWithLogin(CDataContainer context,
                                UserPrototypeData current,
                                UserPrototypeData original)
            : base(context, current, original)
        {
        }

        protected override void SaveProperties(User user)
        {
            base.SaveProperties(user);

            bool isValidLoginName = !string.IsNullOrWhiteSpace(this.currentState.mappedLoginName);
            bool isCreatingOrUpdatingLogin = !this.Exists || user.Login != this.currentState.mappedLoginName;
            if (isValidLoginName && isCreatingOrUpdatingLogin)
            {
                user.Login = this.currentState.mappedLoginName;
            }
        }
    }

    internal class UserPrototypeForWindowsUser : UserPrototypeForSqlUserWithLogin,
                                                    IUserPrototypeWithDefaultLanguage                                                    
    {
        private CDataContainer context;

        public override bool IsDefaultSchemaSupported
        {
            get
            {
                Database? parentDb = this.context.Server.GetSmoObject(this.context.ParentUrn) as Database;
                User user = this.GetUser(parentDb);

                // Default Schema was not supported before Denali for windows group.
                if (this.Exists && user.LoginType == Microsoft.SqlServer.Management.Smo.LoginType.WindowsGroup)
                {
                    return SqlMgmtUtils.IsSql11OrLater(this.context.Server.ConnectionContext.ServerVersion);
                }
                else
                {
                    return base.IsDefaultSchemaSupported;
                }
            }
        }

        #region IUserPrototypeWithDefaultLanguage Members

        public bool IsDefaultLanguageSupported
        {
            get
            {
                return LanguageUtils.IsDefaultLanguageSupported(this.context.Server);
            }
        }

        public string DefaultLanguageAlias
        {
            get
            {
                return this.currentState.defaultLanguageAlias;
            }
            set
            {
                this.currentState.defaultLanguageAlias = value;
            }
        }

        #endregion        

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">The context for the dialog</param>
        public UserPrototypeForWindowsUser(CDataContainer context,
                                UserPrototypeData current,
                                UserPrototypeData original)
            : base(context, current, original)
        {
            this.context = context;
        }

        protected override void SaveProperties(User user)
        {
            base.SaveProperties(user);

            if (this.IsDefaultLanguageSupported)
            {
                //If this.currentState.defaultLanguageAlias is <default>, we will get defaultLanguageName as string.Empty
                string defaultLanguageName = LanguageUtils.GetLanguageNameFromAlias(user.Parent.Parent,
                                                        this.currentState.defaultLanguageAlias);

                if (!this.Exists || (user.DefaultLanguage.Name != defaultLanguageName)) //comparing name of the language.
                {
                    //Default language is invalid inside an uncontained database.
                    if (user.Parent.ContainmentType != ContainmentType.None)
                    {
                        //Setting what user has set, i.e. the Alias of the language.
                        user.DefaultLanguage.Name = this.currentState.defaultLanguageAlias;
                    }
                }
            }            
        }
    }

    internal class UserPrototypeForSqlUserWithPassword : UserPrototypeWithDefaultSchema,
                                                            IUserPrototypeWithDefaultLanguage,
                                                            IUserPrototypeWithPassword
    {
        private CDataContainer context;

        #region IUserPrototypeWithDefaultLanguage Members

        public bool IsDefaultLanguageSupported
        {
            get
            {
                //Default Language was not supported before Denali or on SQL DB.
                bool isSqlAzure = this.context.ServerConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase;
                return !isSqlAzure && SqlMgmtUtils.IsSql11OrLater(this.context.Server.ConnectionContext.ServerVersion);
            }
        }

        public string DefaultLanguageAlias
        {
            get
            {
                return this.currentState.defaultLanguageAlias;
            }
            set
            {
                this.currentState.defaultLanguageAlias = value;
            }
        }

        #endregion

        #region IUserPrototypeWithPassword Members

        public SecureString Password
        {
            set
            {
                this.currentState.password = value;
            }
        }

        public SecureString PasswordConfirm
        {
            set
            {
                this.currentState.passwordConfirm = value;
            }
        }

        public SecureString OldPassword
        {
            set
            {
                this.currentState.oldPassword = value;
            }
        }

        public bool IsOldPasswordRequired
        {
            get
            {
                return this.currentState.isOldPasswordRequired;
            }
            set 
            {
                this.currentState.isOldPasswordRequired = value;
            }
        }

        #endregion

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">The context for the dialog</param>
        public UserPrototypeForSqlUserWithPassword(CDataContainer context,
                                UserPrototypeData current,
                                UserPrototypeData original)
            : base(context, current, original)
        {
            this.context = context;
        }

        protected override void SaveProperties(User user)
        {
            base.SaveProperties(user);

            if (this.IsDefaultLanguageSupported)
            {
                //If this.currentState.defaultLanguageAlias is <default>, we will get defaultLanguageName as string.Empty
                string defaultLanguageName = LanguageUtils.GetLanguageNameFromAlias(user.Parent.Parent,
                                                        this.currentState.defaultLanguageAlias);

                if (!this.Exists || (user.DefaultLanguage.Name != defaultLanguageName)) //comparing name of the language.
                {
                    //Default language is invalid inside an uncontained database.
                    if (user.Parent.ContainmentType != ContainmentType.None)
                    {
                        //Setting what user has set, i.e. the Alias of the language.
                        user.DefaultLanguage.Name = this.currentState.defaultLanguageAlias;
                    }
                }
            }
        }

        protected override void CreateOrAlterUser(User user)
        {
            if (!this.Exists) //New User
            {
                user.Create(this.currentState.password);                
            }
            else //Existing User
            {
                user.Alter();

                if (!DatabaseUtils.IsSecureStringsEqual(this.currentState.password, this.originalState.password))
                {
                    if (this.currentState.isOldPasswordRequired)
                    {
                        user.ChangePassword(this.currentState.oldPassword, this.currentState.password);
                    }
                    else
                    {
                        user.ChangePassword(this.currentState.password);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Used to create or return required UserPrototype objects for a user type.
    /// </summary>
    internal static class UserPrototypeFactory
    {
        public static UserPrototype GetUserPrototype(
            CDataContainer context, UserInfo? user, 
            UserPrototypeData? originalData, ExhaustiveUserTypes userType)
        {
            UserPrototype currentPrototype = null;
            UserPrototypeData currentData = new UserPrototypeData(context, user);
            switch (userType)
            {
                case ExhaustiveUserTypes.AsymmetricKeyMappedUser:
                    currentPrototype ??= new UserPrototype(context, currentData, originalData);
                    break;                    

                case ExhaustiveUserTypes.CertificateMappedUser:
                    currentPrototype ??= new UserPrototype(context, currentData, originalData);
                    break;

                case ExhaustiveUserTypes.LoginMappedUser:
                    currentPrototype ??= new UserPrototypeForSqlUserWithLogin(context, currentData, originalData);
                    break;

                case ExhaustiveUserTypes.SqlUserWithoutLogin:
                case ExhaustiveUserTypes.ExternalUser:
                    currentPrototype ??= new UserPrototypeWithDefaultSchema(context, currentData, originalData);
                    break;

                case ExhaustiveUserTypes.SqlUserWithPassword:
                    currentPrototype ??= new UserPrototypeForSqlUserWithPassword(context, currentData, originalData);
                    break;

                case ExhaustiveUserTypes.WindowsUser:
                    currentPrototype ??= new UserPrototypeForWindowsUser(context, currentData, originalData);
                    break;
                
                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown UserType provided.");
                    currentPrototype = null;
                    break;
            }
            return currentPrototype;
        }
    }

    /// <summary>
    /// Lists all types of possible database users.
    /// </summary>
    internal enum ExhaustiveUserTypes
    {
        Unknown,
        SqlUserWithoutLogin,
        SqlUserWithPassword,
        WindowsUser,
        LoginMappedUser,
        CertificateMappedUser,
        AsymmetricKeyMappedUser,
        ExternalUser,
    };
}
