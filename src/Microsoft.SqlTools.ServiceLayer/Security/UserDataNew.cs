using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections.Generic;
using System.Security;
using Microsoft.SqlServer.Management.SqlMgmt;


namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Defines the common behavior of all types of database user objects.
    /// </summary>
    internal interface IUserPrototype
    {
        string Name { get; set; }
        Smo.UserType UserType { get; set; }
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
    public class UserPrototypeDataNew
    {
        public string name = string.Empty;
        public Smo.UserType userType = Smo.UserType.SqlUser;
        public bool isSystemObject = false;
        public Dictionary<string, bool> isSchemaOwned = null;
        public Dictionary<string, bool> isMember = null;

        public Smo.AuthenticationType authenticationType = Smo.AuthenticationType.Instance;
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
        private UserPrototypeDataNew()
        {
            this.isSchemaOwned = new Dictionary<string, bool>();
            this.isMember = new Dictionary<string, bool>();
        }

        public UserPrototypeDataNew(CDataContainer context)
        {
            this.isSchemaOwned = new Dictionary<string, bool>();
            this.isMember = new Dictionary<string, bool>();

            if (!context.IsNewObject)
            {
                this.LoadUserData(context);
            }

            this.LoadRoleMembership(context);

            this.LoadSchemaData(context);            
        }

        public UserPrototypeDataNew Clone()
        {
            UserPrototypeDataNew result = new UserPrototypeDataNew();

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

            foreach (string key in this.isMember.Keys)
            {
                result.isMember[key] = this.isMember[key];
            }

            foreach (string key in this.isSchemaOwned.Keys)
            {
                result.isSchemaOwned[key] = this.isSchemaOwned[key];
            }

            return result;
        }

        public bool HasSameValueAs(UserPrototypeDataNew other)
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

            result = result && this.isMember.Keys.Count == other.isMember.Keys.Count;
            if (result)
            {
                foreach (string key in this.isMember.Keys)
                {
                    if (this.isMember[key] != other.isMember[key])
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
            Smo.User existingUser = context.Server.GetSmoObject(new Urn(context.ObjectUrn)) as Smo.User;

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
                this.defaultLanguageAlias = LanguageUtils.GetLanguageAliasFromName(existingUser.Parent.Parent,
                                                                existingUser.DefaultLanguage.Name);
            }
        }

        /// <summary>
        /// Loads role membership of a database user.
        /// </summary>
        /// <param name="context"></param>
        private void LoadRoleMembership(CDataContainer context)
        {
            Urn objUrn = new Urn(context.ObjectUrn);
            Urn databaseUrn = objUrn.Parent;

            Smo.Database parentDb = context.Server.GetSmoObject(databaseUrn) as Smo.Database;
            Smo.User existingUser = context.Server.Databases[parentDb.Name].Users[objUrn.GetNameForType("User")];

            foreach (Smo.DatabaseRole dbRole in parentDb.Roles)
            {
                var comparer = parentDb.GetStringComparer();
                if (comparer.Compare(dbRole.Name, "public") != 0)
                {
                    if (context.IsNewObject)
                    {
                        this.isMember[dbRole.Name] = false;
                    }
                    else
                    {
                        this.isMember[dbRole.Name] = existingUser.IsMember(dbRole.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Loads schema ownership related data.
        /// </summary>
        /// <param name="context"></param>
        private void LoadSchemaData(CDataContainer context)
        {
            Urn objUrn = new Urn(context.ObjectUrn);
            Urn databaseUrn = objUrn.Parent;

            Smo.Database parentDb = context.Server.GetSmoObject(databaseUrn) as Smo.Database;
            Smo.User existingUser = context.Server.Databases[parentDb.Name].Users[objUrn.GetNameForType("User")];

            if (!SqlMgmtUtils.IsYukonOrAbove(context.Server)
                || parentDb.CompatibilityLevel <= Smo.CompatibilityLevel.Version80)
            {
                return;
            }          

            foreach (Smo.Schema sch in parentDb.Schemas)
            {
                if (context.IsNewObject)
                {
                    this.isSchemaOwned[sch.Name] = false;
                }
                else
                {
                    var comparer = parentDb.GetStringComparer();
                    this.isSchemaOwned[sch.Name] = comparer.Compare(sch.Owner, existingUser.Name) == 0;
                }
            }
        }
    }

    /// <summary>
	/// Prototype object for creating or altering users
	/// </summary>
    internal class UserPrototypeNew : IUserPrototype
    {
        protected UserPrototypeDataNew originalState = null;
        protected UserPrototypeDataNew currentState = null;

        private List<string> schemaNames = null;
        private List<string> roleNames = null;
        private bool exists = false;
        private Smo.Database parent = null;

        public bool IsRoleMembershipChangesApplied { get; set; } //default is false
        public bool IsSchemaOwnershipChangesApplied { get; set; } //default is false

        #region IUserPrototype Members

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

        public Smo.UserType UserType
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
		public UserPrototypeNew(CDataContainer context, 
                                UserPrototypeDataNew current,
                                UserPrototypeDataNew original)
		{
            this.currentState = current;
            this.originalState = original;

            this.exists = !context.IsNewObject;
            this.parent = context.Server.GetSmoObject(new Urn(context.ParentUrn)) as Smo.Database;
            
            this.PopulateRoles();
            this.PopulateSchemas();
        }

        private void PopulateRoles()
        {
            this.roleNames = new List<string>();

            foreach (Smo.DatabaseRole dbRole in this.parent.Roles)
            {
                var comparer = this.parent.GetStringComparer();
                if (comparer.Compare(dbRole.Name, "public") != 0)
                {
                    this.roleNames.Add(dbRole.Name);
                }
            }
        }

        private void PopulateSchemas()
        {
            this.schemaNames = new List<string>();

            if (!SqlMgmtUtils.IsYukonOrAbove(this.parent.Parent)
                || this.parent.CompatibilityLevel <= Smo.CompatibilityLevel.Version80)
            {
                return;
            }

            foreach (Smo.Schema sch in this.parent.Schemas)
            {
                this.schemaNames.Add(sch.Name);
            }            
        }

        public bool IsYukonOrLater
        {
            get
            {
                return SqlMgmtUtils.IsYukonOrAbove(this.parent.Parent);
            }
        }

        public Smo.User ApplyChanges()
        {
            Smo.User user = null;

            user = this.GetUser();

            if (this.ChangesExist())
            {
                this.SaveProperties(user);
                this.CreateOrAlterUser(user);
                
                //Extended Properties page also executes Alter() method on the same user object
                //in order to add extended properties. If at that time any property is dirty, 
                //it will again generate the script corresponding to that.
                user.Refresh();

                this.ApplySchemaOwnershipChanges(user);
                this.IsSchemaOwnershipChangesApplied = true;

                this.ApplyRoleMembershipChanges(user);
                this.IsRoleMembershipChangesApplied = true;
            }

            return user;
        }

        protected virtual void CreateOrAlterUser(Smo.User user)
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

        private void ApplySchemaOwnershipChanges(Smo.User user)
        {
            IEnumerator<KeyValuePair<string, bool>> enumerator = this.currentState.isSchemaOwned.GetEnumerator();
            enumerator.Reset();

            String nullString = null;

            while (enumerator.MoveNext())
            {
                string schemaName = enumerator.Current.Key.ToString();
                bool userIsOwner = (bool)enumerator.Current.Value;

                if (((bool)this.originalState.isSchemaOwned[schemaName]) != userIsOwner)
                {
                    System.Diagnostics.Debug.Assert(!this.Exists || userIsOwner, "shouldn't have to unset ownership for new users");

                    Smo.Schema schema = this.parent.Schemas[schemaName];
                    schema.Owner = userIsOwner ? user.Name : nullString;
                    schema.Alter();
                }
            }
        }

        private void ApplyRoleMembershipChanges(Smo.User user)
        {
            IEnumerator<KeyValuePair<string, bool>> enumerator = this.currentState.isMember.GetEnumerator();
            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                string roleName = enumerator.Current.Key;
                bool userIsMember = (bool)enumerator.Current.Value;

                if (((bool)this.originalState.isMember[roleName]) != userIsMember)
                {
                    System.Diagnostics.Debug.Assert(this.Exists || userIsMember, "shouldn't have to unset membership for new users");

                    Smo.DatabaseRole role = this.parent.Roles[roleName];

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

        protected virtual void SaveProperties(Smo.User user)
        {
            if (!this.Exists || (user.UserType != this.currentState.userType))
            {
                user.UserType = this.currentState.userType;
            }

            if ((this.currentState.userType == Smo.UserType.Certificate)
                &&(!this.Exists || (user.Certificate != this.currentState.certificateName))
                )
            {
                user.Certificate = this.currentState.certificateName;
            }

            if ((this.currentState.userType == Smo.UserType.AsymmetricKey)
                && (!this.Exists || (user.AsymmetricKey != this.currentState.asymmetricKeyName))
                )
            {
                user.AsymmetricKey = this.currentState.asymmetricKeyName;
            }
        }

        public Smo.User GetUser()
        {
            Smo.User result = null;

            // if we think we exist, get the SMO user object
            if (this.Exists)
            {
                result = this.parent.Users[this.originalState.name];
                result.Refresh();

                System.Diagnostics.Debug.Assert(0 == String.Compare(this.originalState.name, this.currentState.name, StringComparison.Ordinal), "name of existing user has changed");
                if (result == null)
                {
                    throw new ObjectNoLongerExistsException();
                }
            }
            else
            {
                result = new Smo.User(this.parent, this.Name);
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
    }

    internal class UserPrototypeWithDefaultSchema : UserPrototypeNew,
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
                                UserPrototypeDataNew current,
                                UserPrototypeDataNew original)
            : base(context, current, original)
        {
            this.context = context;
        }

        protected override void SaveProperties(Smo.User user)
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
                                UserPrototypeDataNew current,
                                UserPrototypeDataNew original)
            : base(context, current, original)
        {
        }

        protected override void SaveProperties(Smo.User user)
        {
            base.SaveProperties(user);

            if (!this.Exists || (user.Login != this.currentState.mappedLoginName))
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
                //Default Schema was not supported before Denali for windows group.
                Smo.User user = null;

                user = this.GetUser();
                if (this.Exists && user.LoginType == Smo.LoginType.WindowsGroup)
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
                //Default Language was not supported before Denali.
                return SqlMgmtUtils.IsSql11OrLater(this.context.Server.ConnectionContext.ServerVersion);
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
                                UserPrototypeDataNew current,
                                UserPrototypeDataNew original)
            : base(context, current, original)
        {
            this.context = context;
        }

        protected override void SaveProperties(Smo.User user)
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
                    if (user.Parent.ContainmentType != Smo.ContainmentType.None)
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
                //Default Language was not supported before Denali.
                return SqlMgmtUtils.IsSql11OrLater(this.context.Server.ConnectionContext.ServerVersion);
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
                                UserPrototypeDataNew current,
                                UserPrototypeDataNew original)
            : base(context, current, original)
        {
            this.context = context;
        }

        protected override void SaveProperties(Smo.User user)
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
                    if (user.Parent.ContainmentType != Smo.ContainmentType.None)
                    {
                        //Setting what user has set, i.e. the Alias of the language.
                        user.DefaultLanguage.Name = this.currentState.defaultLanguageAlias;
                    }
                }
            }
        }

        protected override void CreateOrAlterUser(Smo.User user)
        {
            if (!this.Exists) //New User
            {
                user.Create(this.currentState.password);                
            }
            else //Existing User
            {
                user.Alter();

                if (this.currentState.password != this.originalState.password)
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
    /// This factory class also helps us in maintaining a single set of current data
    /// and original data mapped to all UserPrototypes.
    /// 
    /// Also this UserPrototypeFactory is a Singleton object for one datacontainer object.
    /// Making it Singleton helps us in using same factory object inside other pages too.
    /// </summary>
    internal class UserPrototypeFactory
    {
        private static UserPrototypeFactory singletonInstance;

        private UserPrototypeDataNew currentData;
        private UserPrototypeDataNew originalData;
        private CDataContainer context;

        private UserPrototypeNew asymmetricKeyMappedUser;
        private UserPrototypeNew certificateMappedUser;
        private UserPrototypeNew loginMappedUser;
        private UserPrototypeNew noLoginUser;
        private UserPrototypeNew sqlUserWithPassword;
        private UserPrototypeNew windowsUser;        

        private UserPrototypeNew currentPrototype;

        public UserPrototypeNew CurrentPrototype
        {
            get
            {
                if (currentPrototype == null)
                {
                    currentPrototype = new UserPrototypeNew(this.context,
                                                        this.currentData,
                                                        this.originalData);
                }

                return currentPrototype;
            }
        }

        private UserPrototypeFactory(CDataContainer context)
        {
            this.context = context;

            this.originalData = new UserData.UserPrototypeDataNew(this.context);
            this.currentData = this.originalData.Clone();
        }

        public static UserPrototypeFactory GetInstance(CDataContainer context)
        {
            if (singletonInstance != null
                && singletonInstance.context != context)
            {
                singletonInstance = null;
            }

            if (singletonInstance == null)
            {
                singletonInstance = new UserPrototypeFactory(context);
            }            

            return singletonInstance;
        }

        public UserPrototypeNew GetUserPrototype(ExhaustiveUserTypes userType)
        {
            switch (userType)
            {
                case ExhaustiveUserTypes.AsymmetricKeyMappedUser:
                    currentData.userType = Smo.UserType.AsymmetricKey;
                    if(this.asymmetricKeyMappedUser == null)
                    {
                        this.asymmetricKeyMappedUser = new UserPrototypeNew(this.context, this.currentData, this.originalData);
                    }
                    this.currentPrototype = asymmetricKeyMappedUser;
                    break;                    

                case ExhaustiveUserTypes.CertificateMappedUser:
                    currentData.userType = Smo.UserType.Certificate;
                    if (this.certificateMappedUser == null)
                    {
                        this.certificateMappedUser = new UserPrototypeNew(this.context, this.currentData, this.originalData);
                    }
                    this.currentPrototype = certificateMappedUser; 
                    break;

                case ExhaustiveUserTypes.LoginMappedUser:
                    currentData.userType = Smo.UserType.SqlUser;
                    if (this.loginMappedUser == null)
                    {
                        this.loginMappedUser = new UserPrototypeForSqlUserWithLogin(this.context, this.currentData, this.originalData);
                    }
                    this.currentPrototype = loginMappedUser;
                    break;

                case ExhaustiveUserTypes.SqlUserWithoutLogin:
                    currentData.userType = Smo.UserType.NoLogin;
                    if (this.noLoginUser == null)
                    {
                        this.noLoginUser = new UserPrototypeWithDefaultSchema(this.context, this.currentData, this.originalData);
                    }
                    this.currentPrototype = noLoginUser;
                    break;

                case ExhaustiveUserTypes.SqlUserWithPassword:
                    currentData.userType = Smo.UserType.SqlUser;
                    if (this.sqlUserWithPassword == null)
                    {
                        this.sqlUserWithPassword = new UserPrototypeForSqlUserWithPassword(this.context, this.currentData, this.originalData);
                    }
                    this.currentPrototype = sqlUserWithPassword;
                    break;

                case ExhaustiveUserTypes.WindowsUser:
                    currentData.userType = Smo.UserType.SqlUser;
                    if (this.windowsUser == null)
                    {
                        this.windowsUser = new UserPrototypeForWindowsUser(this.context, this.currentData, this.originalData);
                    }
                    this.currentPrototype = windowsUser;
                    break;
                
                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown UserType provided.");
                    this.currentPrototype = null;
                    break;
            }
            return this.currentPrototype;
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
        AsymmetricKeyMappedUser
    };
}








