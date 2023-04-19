//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Text;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Broker;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Enumeration of sql object types that can have permissions
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
 enum SecurableType
    {
        //              !!IMPORTANT!!
        // If adding a new type that is schema-scoped
        // make sure to also add the SchemaScopedSecurable attribute
        // so Principal!AddAllSecurables(schemaName) will correctly
        // add securables of that type to its list
        //
        //Note there doesn't currently exist for Server or Database-scoped
        // securables as the work was only initially done for schema-scoped
        // (but there's no reason db or server-scoped attributes couldn't be
        // added later). This DOES mean you need to manually update the
        // methods below for those securable types though - e.g. in
        //Principal::RelevantSecurableTypes

        [SchemaScopedSecurable(typeof(UserDefinedAggregate))]
        AggregateFunction,
        ApplicationRole,
        Assembly,
        AsymmetricKey,
        AvailabilityGroup,
        Certificate,
        Column,
        ColumnFunctionTable,
        //Contract,             // NYI in SMO
        Database,
        DatabaseRole,
        Endpoint,
        [SchemaScopedSecurable(typeof(ExtendedStoredProcedure))]
        ExtendedStoredProcedure,
        ExternalDataSource,
        ExternalFileFormat,
        FullTextCatalog,
        [SchemaScopedSecurable(typeof(UserDefinedFunction), "FunctionType", (int)UserDefinedFunctionType.Inline)]
        FunctionInline,
        [SchemaScopedSecurable(typeof(UserDefinedFunction), "FunctionType", (int)UserDefinedFunctionType.Scalar)]
        FunctionScalar,
        [SchemaScopedSecurable(typeof(UserDefinedFunction), "FunctionType", (int)UserDefinedFunctionType.Table)]
        FunctionTable,
        Login,
        //MessageType,              // Assuming there is SMO support for this object type
        //RemoteBindingService,     // NYI in SMO
        //Route,                    // NYI in SMO
        Schema,
        [SchemaScopedSecurable(typeof(SecurityPolicy))]
        SecurityPolicy,
        Server,
        ServerRole,
        //Service,                  // Assuming there is SMO support for this object type
        //ServiceContract,          // Assuming there is SMO support for this object type
        [SchemaScopedSecurable(typeof(StoredProcedure))]
        StoredProcedure,
        [SchemaScopedSecurable(typeof(Synonym))]
        Synonym,
        [SchemaScopedSecurable(typeof(Sequence))]
        Sequence,
        SymmetricKey,
        [SchemaScopedSecurable(typeof(Table))]
        Table,
        //Trigger,
        User,
        [SchemaScopedSecurable(typeof(View))]
        View,
        [SchemaScopedSecurable(typeof(UserDefinedDataType))]
        UserDefinedDataType,
        [SchemaScopedSecurable(typeof(UserDefinedTableType))]
        UserDefinedTableType,
        [SchemaScopedSecurable(typeof(XmlSchemaCollection))]
        XmlSchemaCollection,
        ServiceQueue,
        Unknown
    }

    namespace PermissionsData
    {

        /// <summary>
        /// The values a grant-state can take
        /// </summary>
        internal enum PermissionStatus
        {
            Revoke,             // neither granted nor denied
            Grant,              // privilege granted
            WithGrant,          // with grant option
            Deny                // privilege denied
        }

        /// <summary>
        /// The display states a PermissionState can have in the UI
        /// </summary>
        internal enum PermissionDisplayStatus
        {
            Revoke,             // neither granted nor denied, no checks
            Grant,              // privilege granted, checked "Grant" box, unchecked "With Grant" and "Deny"
            WithGrant,          // privelege granted, with grant option.  grant is also checked.  deny unchecked.
            Deny,               // privilege denied, unchecked "Grant" box, checked "Deny" box
            PartialWithGrant,   // with grant on some, but not all child objects - indeterminate "Grant" and "With Grant", unchecked "Deny"
            PartialGrant,       // privilege granted on some, but not all child objects - indeterminate "Grant", unchecked "Deny"
            PartialDeny,        // privilege denied on some, but not all child objects - unchecked "Grant", indeterminate "Deny"
            PartialGrantDeny,   // privilege granted on some, denied on others
            Indeterminate       // privilege granted to some child objects, with grant and denied to others - indeterminate "Grant", indeterminate "Deny"
        }

        /// <summary>
        /// A SQL Server object that can have permissions associated with it
        /// </summary>
        internal class Securable
        {
            /// <summary>
            /// This class captures enumerated data about principals that are relevant to this securable
            /// </summary>
            private class EnumeratedPrincipalData
            {
                public string Name;
                public PrincipalType PrincipalType;
                public EnumeratedPrincipalData(string name, PrincipalType type)
                {
                    this.Name = name;
                    this.PrincipalType = type;
                }

                public override bool Equals(object obj)
                {
                    bool result = false;

                    EnumeratedPrincipalData other = obj as EnumeratedPrincipalData;
                    if (other != null)
                    {
                        result = ((this.PrincipalType == other.PrincipalType) && (this.Name == other.Name));
                    }

                    return result;
                }

                public static bool operator ==(EnumeratedPrincipalData a, EnumeratedPrincipalData b)
                {
                    bool result = false;

                    if (((object)a == null) && ((object)b == null))
                    {
                        result = true;
                    }
                    else if (((object)a != null) && ((object)b != null))
                    {
                        result = a.Equals(b);
                    }

                    return result;
                }

                public static bool operator !=(EnumeratedPrincipalData a, EnumeratedPrincipalData b)
                {
                    return !(a == b);
                }

                public override int GetHashCode()
                {
                    return this.Name.GetHashCode();
                }
            }

            /// <summary>
            /// This class contains the names of system principals
            /// </summary>
            private class SystemPrincipalDirectory
            {
                private Dictionary<PrincipalType, List<string>> systemPrincipalsMap;
                private object connectionInfo;
                private string databaseName;

                public SystemPrincipalDirectory(object connectionInfo, string databaseName)
                {
                    this.connectionInfo = connectionInfo;
                    this.databaseName = databaseName;
                    this.systemPrincipalsMap = new Dictionary<PrincipalType, List<string>>();
                }

                /// <summary>
                /// Get a list of the system principals of a particular type
                /// </summary>
                /// <param name="principalType">The type to look for</param>
                /// <returns>The names of the system principals</returns>
                public List<string> GetSystemPrincipalNames(PrincipalType principalType)
                {
                    if (!this.systemPrincipalsMap.ContainsKey(principalType))
                    {
                        Urn urn = null;

                        switch (principalType)
                        {
                            case PrincipalType.User:

                                urn = new Urn(String.Format(
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    "Server/Database[@Name='{0}']/User[@IsSystemObject=true() and @Name!='guest']",
                                    Urn.EscapeString(this.databaseName)));
                                break;

                            case PrincipalType.DatabaseRole:

                                urn = new Urn(String.Format(
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    "Server/Database[@Name='{0}']/Role[@IsFixedRole=true()]",
                                    Urn.EscapeString(this.databaseName)));
                                break;

                            case PrincipalType.ApplicationRole:

                                // do nothing - there are no system application roles
                                break;

                            case PrincipalType.Login:

                                urn = new Urn("Server/Login[@IsSystemObject=true()]");
                                break;

                            case PrincipalType.ServerRole:

                                Version version = Securable.GetServerVersion(this.connectionInfo);
                                if (Utils.IsSql11OrLater(version.Major))
                                {
                                    urn = new Urn("Server/Role[@IsFixedRole=true()]");
                                }
                                else
                                {
                                    urn = new Urn("Server/Role");
                                }
                                break;

                            default:

                                // STrace.Assert(false, "unexpected principal type");
                                break;
                        }

                        List<string> systemPrincipalNames = new List<string>();

                        if (urn != null)
                        {
                            Request request = new Request(
                                urn,
                                new string[] { "Name" },
                                new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) });

                            DataTable systemPrincipalsTable = (new Enumerator()).Process(this.connectionInfo, request);

                            foreach (DataRow systemPrincipalRow in systemPrincipalsTable.Rows)
                            {
                                systemPrincipalNames.Add(systemPrincipalRow[0].ToString());
                            }
                        }

                        this.systemPrincipalsMap[principalType] = systemPrincipalNames;
                    }

                    return this.systemPrincipalsMap[principalType];
                }
            }


            private string name = String.Empty;
            private string schema = String.Empty;
            private string expectedGrantor = string.Empty;
            private Urn urn = null;

            private SecurableType securableType;
            private SqlSmoObject smoObject = null;
            private bool exists = false;

            private SecurableList children = null;
            private Securable parent = null;

            private PrincipalCollection principals = null;
            private PrincipalCollection removedPrincipals = null;

            private event EventHandler observableChanged;

            /// <summary>
            /// Dictionary mapping Principal to PermissionStateCollection
            /// </summary>
            private HybridDictionary principalToPermissionStates = null;
            private object connectionInfo = null;
            private Version serverVersion;
            private DatabaseEngineType databaseEngineType;
            private DatabaseEngineEdition databaseEngineEdition;

            private ArrayList relevantPermissions = null;
            private bool isRemoved = false;
            private const int DENALI = 11;
            private const int KATMAI = 10;
            private const int YUKON = 9;
            private const int SHILOH = 8;

            public string ExpectedGrantor
            {
                get { return expectedGrantor; }
                set { expectedGrantor = value; }
            }

            /// <summary>
            /// The object's name
            /// </summary>
            public string Name
            {
                get
                {
                    string result = String.Empty;

                    if (this.Exists)
                    {
                        string objectName = this.urn.GetAttribute("Name");
                        result = (objectName != null) ? objectName : String.Empty;
                    }
                    else
                    {
                        result = this.name;
                    }

                    return result;
                }

                set
                {
                    // STrace.Assert(!this.Exists, "Shouldn't be setting names for existing objects");
                    this.name = value;
                }
            }

            /// <summary>
            /// The object's schema or owner
            /// </summary>
            public string Schema
            {
                get
                {
                    string result = String.Empty;

                    if (this.Exists)
                    {
                        string schemaName = this.urn.GetAttribute("Schema");
                        result = (schemaName != null) ? schemaName : String.Empty;
                    }
                    else
                    {
                        result = this.schema;
                    }

                    return result;
                }

                set
                {
                    // STrace.Assert(!this.Exists, "Shouldn't be setting schema name for existing objects");
                    this.schema = value;
                }
            }

            /// <summary>
            /// The two-part name of the object
            /// </summary>
            public string FullName
            {
                get
                {
                    string result = String.Empty;

                    if (this.Name.Length != 0)
                    {
                        if (this.Schema.Length != 0)
                        {
                            result = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", this.Schema, this.Name);
                        }
                        else
                        {
                            result = this.Name;
                        }
                    }

                    return result;
                }
            }

            /// <summary>
            /// Get the name of the database containing this object, or String.Empty if no database contains this object
            /// </summary>
            public string DatabaseName
            {
                get
                {
                    string result = String.Empty;

                    Urn currentUrn = new Urn(this.urn.ToString());

                    if (currentUrn != null)
                    {
                        while (0 != String.Compare(currentUrn.Type, "Server", StringComparison.OrdinalIgnoreCase))
                        {
                            if (0 == String.Compare(currentUrn.Type, "Database", StringComparison.OrdinalIgnoreCase))
                            {
                                result = currentUrn.GetAttribute("Name");
                                break;
                            }
                            else
                            {
                                currentUrn = currentUrn.Parent;
                            }
                        }
                    }

                    return result;
                }
            }

            /// <summary>
            /// This object's type
            /// </summary>
            public SecurableType SecurableType
            {
                get
                {
                    return this.securableType;
                }
            }

            /// <summary>
            /// The localized display name of the type of the securable
            /// </summary>
            public string TypeName
            {
                get
                {
                    SearchableObjectType seachableType = Securable.GetSearchableObjectType(this.SecurableType);
                    SearchableObjectTypeDescription description = SearchableObjectTypeDescription.GetDescription(seachableType);

                    return description.DisplayTypeNameSingular;
                }
            }

            /// <summary>
            /// The URN representing this securable object
            /// </summary>
            public Urn Urn
            {
                get
                {
                    return this.urn;
                }
            }

            /// <summary>
            /// The collection of child objects
            /// </summary>
            public SecurableList Children
            {
                get
                {
                    if (null == this.children)
                    {
                        this.children = this.GetChildren();
                    }

                    return this.children;
                }

            }

            /// <summary>
            /// Indicate whether this.children is initialized
            /// </summary>
            internal bool IsChildrenInitialized
            {
                get
                {
                    return null != this.children;
                }
            }

            /// <summary>
            /// The securable object that is the parent of this object
            /// </summary>
            internal Securable Parent
            {
                get
                {
                    return this.parent;
                }

                set
                {
                    this.parent = value;
                }
            }

            /// <summary>
            /// Are there permissions changes to persist to the server
            /// </summary>
            public bool ChangesExist
            {
                get
                {
                    bool result = (this.removedPrincipals.Count != 0);

                    if (!result)
                    {
                        ICollection permissionStateCollections = this.principalToPermissionStates.Values;
                        IEnumerator permissionStateCollectionsEnumerator = permissionStateCollections.GetEnumerator();

                        permissionStateCollectionsEnumerator.Reset();

                        while (!result && permissionStateCollectionsEnumerator.MoveNext())
                        {
                            PermissionStateCollection permissionStates = (PermissionStateCollection)permissionStateCollectionsEnumerator.Current;
                            IEnumerator permissionStateEnumerator = permissionStates.GetEnumerator();

                            permissionStateEnumerator.Reset();

                            while (!result && permissionStateEnumerator.MoveNext())
                            {
                                PermissionState permissionState = (PermissionState)permissionStateEnumerator.Current;
                                result = permissionState.StateChanged;
                            }
                        }
                    }

                    // no changes in children if children were not initialized
                    if (!result && this.IsChildrenInitialized)
                    {
                        foreach (Securable child in this.Children)
                        {
                            if (child.ChangesExist)
                            {
                                result = true;
                                break;
                            }
                        }
                    }

                    return result;
                }
            }

            /// <summary>
            /// Whether the object exists on the server
            /// </summary>
            public bool Exists
            {
                get
                {
                    return this.exists;
                }
            }

            /// <summary>
            /// Has this securable been removed?
            /// </summary>
            public bool IsRemoved
            {
                get
                {
                    return this.isRemoved;
                }

                set
                {
                    this.isRemoved = value;
                }
            }

            /// <summary>
            /// The collection of security principals with interest in this object
            /// </summary>
            public PrincipalCollection Principals
            {
                get
                {
                    if (null == this.principals)
                    {
                        this.principals = this.GetExistingPrincipals();
                    }

                    return this.principals;
                }
            }

            /// <summary>
            /// The set of permissions relevant to this object's type
            /// </summary>
            public ArrayList RelevantPermissions
            {
                get
                {
#pragma warning disable IDE0074 // Use compound assignment
                    if (this.relevantPermissions == null)
                    {
                        this.relevantPermissions = Securable.GetRelevantPermissions(this.SecurableType, this.serverVersion, this.DatabaseName, this.databaseEngineType, this.databaseEngineEdition);
                    }
#pragma warning restore IDE0074 // Use compound assignment

                    return this.relevantPermissions;
                }
            }

            //          /// <summary>
            //          /// The SMO object associated with this object
            //          /// </summary>
            //            protected SqlSmoObject                    SmoObject
            //            {
            //              get
            //              {
            //                  return this.smoObject;
            //              }
            //          }

            /// <summary>
            /// The connection info object used to interact with the SMO enumerator
            /// </summary>
            internal object ConnectionInfo
            {
                get
                {
                    return this.connectionInfo;
                }
            }

            /// <summary>
            /// The server version
            /// </summary>
            protected Version ServerVersion
            {
                get
                {
                    return this.serverVersion;
                }
            }

            /// <summary>
            /// The database engine type.
            /// </summary>
            protected DatabaseEngineType DatabaseEngineType
            {
                get
                {
                    return this.databaseEngineType;
                }
            }

            /// <summary>
            /// The database engine edition.
            /// </summary>
            protected DatabaseEngineEdition DatabaseEngineEdition
            {
                get
                {
                    return this.databaseEngineEdition;
                }
            }

            /// <summary>
            /// Constructor for existing objects.  Used by the Permissions on Objects controls.
            /// </summary>
            /// <param name="smoObject">The SMO object we are representing</param>
            /// <param name="type">The SecurableType for the SMO object</param>
            /// <param name="connectionInfo">Connection information for enumerating permissions data</param>
            /// <param name="serverVersion">The version of the server</param>
            protected Securable(SqlSmoObject smoObject, SecurableType type, object connectionInfo, Version serverVersion)
            {
                this.smoObject = smoObject;
                this.urn = smoObject.Urn;
                this.securableType = type;
                this.connectionInfo = connectionInfo;
                this.exists = (SqlSmoState.Existing == smoObject.State);
                this.principalToPermissionStates = new HybridDictionary();
                this.removedPrincipals = new PrincipalCollection();
                this.serverVersion = serverVersion;
                this.databaseEngineType = GetDatabaseEngineType(this.connectionInfo);
                this.databaseEngineEdition = GetDatabaseEngineEdition(this.connectionInfo);
            }

            /// <summary>
            /// Constructor for new objects or existing objects that have been enumerated or searched for.
            /// </summary>
            /// <param name="type">The securable type of the object</param>
            /// <param name="urn">The URN of the object</param>
            /// <param name="connectionInfo">Connection information for enumerating permissions data</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <param name="exists">True if the securable already exists on the server; otherwise, false</param>
            protected Securable(SecurableType type, string urn, object connectionInfo, Version serverVersion, bool exists)
            {
                this.urn = new Urn(urn);
                this.securableType = type;
                this.connectionInfo = connectionInfo;
                this.exists = exists;
                this.principalToPermissionStates = new HybridDictionary();
                this.removedPrincipals = new PrincipalCollection();
                this.serverVersion = serverVersion;
                this.databaseEngineType = GetDatabaseEngineType(this.connectionInfo);
                this.databaseEngineEdition = GetDatabaseEngineEdition(this.connectionInfo);
            }

            /// <summary>
            /// Factory method for Securables
            /// </summary>
            /// <param name="type">The type of the securable to create</param>
            /// <param name="urn">The URN of the securable</param>
            /// <param name="connectionInfo">Connection info used to enumerate permissions information</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <returns>The securable that was created</returns>
            protected static Securable AllocateSecurable(SecurableType type, string urn, object connectionInfo, Version serverVersion)
            {
                return new Securable(type, urn, connectionInfo, serverVersion, true);
            }

            /// <summary>
            /// Are all permissions for the principal revoked?
            /// </summary>
            /// <param name="principal">The principal</param>
            /// <returns>True if all permissions are revoked, false if there are any permissions granted or denied</returns>
            public bool AllPermissionsRevoked(Principal principal)
            {
                bool result = true;

                foreach (PermissionState ps in this.GetPermissionStates(principal))
                {
                    if (ps.State != PermissionStatus.Revoke)
                    {
                        result = false;
                        break;
                    }
                }

                return result;
            }

            /// <summary>
            /// Apply changes to the permission states
            /// </summary>
            /// <param name="obj">The object to modify</param>
            public void ApplyChanges(SqlSmoObject obj)
            {
                this.ApplyRevokes(obj);
                this.ApplyGrantsAndDenies(obj);
            }

            /// <summary>
            /// Reset back to just loaded state
            /// </summary>
            public virtual void Reset()
            {
                this.principals = null;
                this.children = null;
                this.removedPrincipals = new PrincipalCollection();
                this.principalToPermissionStates = new HybridDictionary();
                this.isRemoved = false;
            }

            /// <summary>
            /// Reload permission states for the securable.  This should be called after committing permission changes to the server.
            /// </summary>
            /// <param name="smoSecurable">The smo securable that was committed to the server</param>
            public void ReloadPermissionStates(SqlSmoObject smoSecurable)
            {
                // STrace.Assert(smoSecurable != null, "smoSecurable is null");
                // STrace.Assert((this.SecurableType == GetSecurableType(smoSecurable)), "smoSecurable is not the same type as the original type");

                if ((smoSecurable == null) || (this.SecurableType != GetSecurableType(smoSecurable)))
                {
                    throw new ArgumentException();
                }

                this.principalToPermissionStates = new HybridDictionary();
                this.removedPrincipals = new PrincipalCollection();
                this.children = null;
                this.isRemoved = false;
                this.exists = ((smoSecurable != null) && (SqlSmoState.Existing == smoSecurable.State));
                this.smoObject = smoSecurable;
                this.urn = smoSecurable.Urn;

                if (this.principals != null)
                {
                    foreach (Principal principal in this.principals)
                    {
                        principal.Reset();
                    }
                }

            }

            /// <summary>
            /// Factory method for PermissionStates
            /// </summary>
            /// <param name="principal">The principal for which we are getting PermissionStates</param>
            /// <returns>The PermissionStates for this object and the principal</returns>
            public PermissionStateCollection GetPermissionStates(Principal principal)
            {
                if (!this.principalToPermissionStates.Contains(principal))
                {
                    PermissionState.PopulatePermissionStates(this, principal);
                    PermissionState.AddChildrenToEmptyParents((PermissionStateCollection)this.principalToPermissionStates[principal]);
                }

                return (PermissionStateCollection)this.principalToPermissionStates[principal];
            }

            /// <summary>
            /// Add a principal to the set of principals
            /// </summary>
            /// <param name="principal">The searchable object describing the principal to add</param>
            /// <returns>The Principal that was added</returns>
            public Principal AddPrincipal(SearchableObject principal)
            {
                Principal result = null;

                if (!this.Principals.Contains(principal))
                {
                    // if the principal is removed, fetch it from the removed collection,
                    // otherwise create a new principal
                    if (this.removedPrincipals.Contains(principal))
                    {
                        result = this.removedPrincipals[new PrincipalKey(principal)];
                        result.IsRemoved = false;
                        this.removedPrincipals.Remove(principal);
                    }
                    else
                    {
                        result = new Principal(principal, this.connectionInfo, this.serverVersion);
                    }

                    // add the principal to the principals collection
                    this.Principals.Add(result);

                    // add the principal to the child columns as well
                    if (this.Children.Count != 0)
                    {
                        IEnumerator childEnumerator = this.Children.GetEnumerator();
                        childEnumerator.Reset();

                        while (childEnumerator.MoveNext())
                        {
                            Securable child = (Securable)childEnumerator.Current;
                            child.AddPrincipal(result);
                        }
                    }
                }
                else
                {
                    result = this.Principals[new PrincipalKey(principal)];
                }

                return result;
            }

            /// <summary>
            /// Add a principal to the set of principals
            /// </summary>
            /// <param name="principal">The principal to add</param>
            /// <returns>The Principal that was added</returns>
            public Principal AddPrincipal(Principal principal)
            {
                // note:  this method is called when child colums are being created.
                //        principal should never be in the removedPrincipals collection.
                // STrace.Assert(
                            //  !principal.IsRemoved && !this.removedPrincipals.Contains(principal),
                            //  "principal shouldn't be in the removed collection when columns are being populated");

                // if the principals collection does not exist, create it
#pragma warning disable IDE0074 // Use compound assignment
                if (this.principals == null)
                {
                    this.principals = new PrincipalCollection();
                }
#pragma warning restore IDE0074 // Use compound assignment

                // add the principal to the collection if it isn't already in the collection
                if (!this.principals.Contains(principal))
                {
                    this.principals.Add(principal);
                }

                return principal;
            }

            /// <summary>
            /// Remove a principal from the set of active principals
            /// </summary>
            /// <param name="principal">The principal to remove</param>
            public void RemovePrincipal(Principal principal)
            {
                // STrace.Assert(this.principals.Contains(principal), "principal is not in the principals collection");

                if (principal.HasExistingGrants(this))
                {
                    // STrace.Assert(!this.removedPrincipals.Contains(principal), "removedPrincipals already contains the principal");
                    this.removedPrincipals.Add(principal);
                    principal.IsRemoved = true;
                }

                this.principals.Remove(principal);
                this.principalToPermissionStates.Remove(principal);

                this.NotifyObservers();
            }

            /// <summary>
            /// Revoke all permissions that have been displayed in the UI.
            /// </summary>
            /// <remarks>
            /// This is called when one of the permissions on objects controls needs to
            /// revoke all permissions on this securable for the principal.  Any child
            /// permissions are revoked as well.
            /// </remarks>
            public void RevokeAll()
            {
                IEnumerator permissionStateCollectionEnumerator = this.principalToPermissionStates.Values.GetEnumerator();
                permissionStateCollectionEnumerator.Reset();

                while (permissionStateCollectionEnumerator.MoveNext())
                {
                    PermissionStateCollection permissionStateCollection = (PermissionStateCollection)permissionStateCollectionEnumerator.Current;
                    IEnumerator permissionStateEnumerator = permissionStateCollection.GetEnumerator();
                    permissionStateEnumerator.Reset();

                    while (permissionStateEnumerator.MoveNext())
                    {
                        PermissionState permissionState = (PermissionState)permissionStateEnumerator.Current;
                        permissionState.Revoke();
                    }
                }
            }

            /// <summary>
            /// Get the collection of child securables
            /// </summary>
            /// <returns>The set of child securables</returns>
            protected virtual SecurableList GetChildren()
            {
                return new SecurableList();
            }

            /// <summary>
            /// Apply permission grants and denies in the permission states
            /// </summary>
            /// <remarks>
            /// This method should only be called by the Securable class or its derived classes.
            /// It isn't protected because parent objects need to call Apply* on their children.
            /// </remarks>
            /// <param name="obj">The object to modify</param>
            internal virtual void ApplyGrantsAndDenies(SqlSmoObject obj)
            {
                SmoPermissionsAdapter adapter = SmoPermissionsAdapter.CreateAdapter(obj);

                // Apply changes for principals that have not been removed.
                // The only principals that could have been changed are the ones that have been selected in the UI
                // at some point, which are the ones that are keys in the principalToPermissionStates collection.
                ICollection principalsCollection = this.principalToPermissionStates.Keys;
                IEnumerator principalEnumerator = principalsCollection.GetEnumerator();
                principalEnumerator.Reset();

                while (principalEnumerator.MoveNext())
                {
                    Principal principal = (Principal)principalEnumerator.Current;
                    this.ApplyGrantsAndDenies(adapter, principal);
                }

                // apply changes for removed principals
                principalEnumerator = this.removedPrincipals.GetEnumerator();
                principalEnumerator.Reset();

                while (principalEnumerator.MoveNext())
                {
                    Principal removedPrincipal = (Principal)principalEnumerator.Current;
                    this.ApplyGrantsAndDenies(adapter, removedPrincipal);
                }
            }

            /// <summary>
            /// Apply permission grants and denies in the permission states
            /// </summary>
            /// <remarks>
            /// This method should only be called by the Securable class or its derived classes.
            /// It isn't protected because parent objects need to call Apply* on their children.
            /// </remarks>
            /// <param name="obj">The object to modify</param>
            internal virtual void ApplyRevokes(SqlSmoObject obj)
            {
                SmoPermissionsAdapter adapter = SmoPermissionsAdapter.CreateAdapter(obj);

                // Apply changes for principals that have not been removed.
                // The only principals that could have been changed are the ones that have been selected in the UI
                // at some point, which are the ones that are keys in the principalToPermissionStates collection.
                ICollection principalsCollection = this.principalToPermissionStates.Keys;
                IEnumerator principalEnumerator = principalsCollection.GetEnumerator();
                principalEnumerator.Reset();

                while (principalEnumerator.MoveNext())
                {
                    Principal principal = (Principal)principalEnumerator.Current;
                    this.ApplyRevokes(adapter, principal);
                }

                // apply changes for removed principals
                principalEnumerator = this.removedPrincipals.GetEnumerator();
                principalEnumerator.Reset();

                while (principalEnumerator.MoveNext())
                {
                    Principal removedPrincipal = (Principal)principalEnumerator.Current;
                    this.ApplyRevokes(adapter, removedPrincipal);
                }
            }

            /// <summary>
            /// Actually commit permission grants and denies for a particular principal
            /// </summary>
            /// <param name="adapter">The SMO Permissions Adapter through which to save</param>
            /// <param name="principal">The principal whose permissions changes are to be commited</param>
            private void ApplyGrantsAndDenies(SmoPermissionsAdapter adapter, Principal principal)
            {
                // If the principal is removed, then there won't be any PermissionState instances for the principal
                // in this securable's principalToPermissionStates collection, but the principal will have the
                // the relationship instances in it's counterpart collection.
                PermissionStateCollection permissionStates = principal.IsRemoved ?
                                                              principal.GetPermissionStates(this) :
                                                              this.GetPermissionStates(principal);

                IEnumerator permissionStateEnumerator = permissionStates.GetEnumerator();
                List<PermissionState> grants = new List<PermissionState>();
                List<PermissionState> denies = new List<PermissionState>();
                List<PermissionState> revokes = new List<PermissionState>();
                List<PermissionState> withGrants = new List<PermissionState>();

                permissionStateEnumerator.Reset();

                while (permissionStateEnumerator.MoveNext())
                {
                    PermissionState permissionState = (PermissionState)permissionStateEnumerator.Current;
                    permissionState.AssignChange(grants, withGrants, denies, revokes);
                }

                adapter.Grant(withGrants, principal, true);
                adapter.Grant(grants, principal, false);
                adapter.Deny(denies, principal);
            }

            /// <summary>
            /// Actually commit permission revokes for a particular principal
            /// </summary>
            /// <param name="adapter">The SMO Permissions Adapter through which to save</param>
            /// <param name="principal">The principal whose permissions changes are to be commited</param>
            private void ApplyRevokes(SmoPermissionsAdapter adapter, Principal principal)
            {
                // If the principal is removed, then there won't be any PermissionState instances for the principal
                // in this securable's principalToPermissionStates collection, but the principal will have the
                // the relationship instances in it's counterpart collection.
                PermissionStateCollection permissionStates =
                    principal.IsRemoved ?
                    principal.GetPermissionStates(this) :
                    this.GetPermissionStates(principal);

                IEnumerator permissionStateEnumerator = permissionStates.GetEnumerator();
                List<PermissionState> grants = new List<PermissionState>();
                List<PermissionState> denies = new List<PermissionState>();
                List<PermissionState> revokes = new List<PermissionState>();
                List<PermissionState> withGrants = new List<PermissionState>();

                permissionStateEnumerator.Reset();

                while (permissionStateEnumerator.MoveNext())
                {
                    PermissionState permissionState = (PermissionState)permissionStateEnumerator.Current;
                    permissionState.AssignChange(grants, withGrants, denies, revokes);
                }

                adapter.Revoke(revokes, principal);
            }



            /// <summary>
            /// Populate the principals collection
            /// </summary>
            private PrincipalCollection GetExistingPrincipals()
            {
                // STrace.Assert(null == this.principals, "overwriting existing principals collection");

                this.principals = new PrincipalCollection();

                if (this.Exists)
                {
                    Principal principal = null;
                    bool isDatabasePrincipal = (this.DatabaseName.Length != 0);
                    SystemPrincipalDirectory systemPrincipalDirectory = new SystemPrincipalDirectory(this.connectionInfo, this.DatabaseName);
                    List<EnumeratedPrincipalData> principalData = this.EnumerateExistingPrincipals(systemPrincipalDirectory);

                    foreach (EnumeratedPrincipalData principalDatum in principalData)
                    {
                        // STrace.Assert(!this.principals.Contains(new PrincipalKey(principalDatum.Name, principalDatum.PrincipalType)), "the principal is already in the collection");

                        if (isDatabasePrincipal)
                        {
                            principal = new Principal(
                                principalDatum.Name,
                                this.DatabaseName,
                                principalDatum.PrincipalType,
                                true,
                                this.connectionInfo,
                                this.serverVersion);
                        }
                        else
                        {
                            principal = new Principal(
                                principalDatum.Name,
                                principalDatum.PrincipalType,
                                true,
                                this.connectionInfo,
                                this.serverVersion);
                        }

                        this.principals.Add(principal);
                    }

                    // principals with permissions on child objects (e.g. columns) should be treated
                    // as principals relevant to the parent too
                    foreach (Securable child in this.Children)
                    {
                        List<EnumeratedPrincipalData> childPrincipalData = child.EnumerateExistingPrincipals(systemPrincipalDirectory);

                        foreach (EnumeratedPrincipalData childPrincipalDatum in childPrincipalData)
                        {
                            if (!this.principals.Contains(new PrincipalKey(childPrincipalDatum.Name, childPrincipalDatum.PrincipalType)))
                            {
                                if (isDatabasePrincipal)
                                {
                                    principal = new Principal(
                                        childPrincipalDatum.Name,
                                        this.DatabaseName,
                                        childPrincipalDatum.PrincipalType,
                                        true,
                                        this.connectionInfo,
                                        this.serverVersion);
                                }
                                else
                                {
                                    principal = new Principal(
                                        childPrincipalDatum.Name,
                                        childPrincipalDatum.PrincipalType,
                                        true,
                                        this.connectionInfo,
                                        this.serverVersion);
                                }

                                this.principals.Add(principal);
                            }
                        }
                    }
                }

                return this.principals;
            }

            /// <summary>
            /// Get a list of the names and types of the principals that have permissions granted or denied
            /// for the securable
            /// </summary>
            /// <param name="systemPrincipals">The directory from which to get the names of system principals</param>
            /// <returns>The list of principal names and types</returns>
            private List<EnumeratedPrincipalData> EnumerateExistingPrincipals(SystemPrincipalDirectory systemPrincipals)
            {
                List<EnumeratedPrincipalData> result = new List<EnumeratedPrincipalData>();

                if (this.Exists)
                {
                    // STrace.Assert(systemPrincipals != null, "systemPrincipals is null");

                    string permissionsUrn = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/Permission", this.urn.ToString());
                    string[] fields = new string[] { "Grantee", "GranteeType" };
                    Request request = new Request(new Urn(permissionsUrn), fields);
                    Enumerator enumerator = new Enumerator();
                    DataTable grantees = enumerator.Process(this.connectionInfo, request);
                    string databaseName = this.DatabaseName;
                    bool isDatabasePrincipal = (databaseName.Length != 0);

                    // map from principal type to a list of system principals of that type
                    Dictionary<PrincipalType, List<string>> systemPrincipalsMap = new Dictionary<PrincipalType, List<string>>();

                    for (int rowIndex = 0; rowIndex < grantees.Rows.Count; ++rowIndex)
                    {
                        DataRow row = grantees.Rows[rowIndex];
                        string principalName = row["Grantee"].ToString();
                        PrincipalType principalType = (PrincipalType)row["GranteeType"];

                        EnumeratedPrincipalData principalData = new EnumeratedPrincipalData(principalName, principalType);

                        // permissions can't be granted or denied to system principals, so we need to filter system principals out
                        if (!systemPrincipals.GetSystemPrincipalNames(principalType).Contains(principalName) && !result.Contains(principalData))
                        {
                            result.Add(principalData);
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// This will update the PrincipalKey with the right ChangesExist value
            /// </summary>
            public void UpdatePrincipalKeyStatus(Principal principal)
            {
                this.principals.UpdateKey(principal);
            }

            /// <summary>
            /// Add a permission state to the collection
            /// </summary>
            /// <param name="permissionState">The permission state to add</param>
            internal void AddPermissionState(PermissionState permissionState)
            {
                PermissionStateCollection permissionStates = null;

                if (this.principalToPermissionStates.Contains(permissionState.Principal))
                {
                    permissionStates = (PermissionStateCollection)this.principalToPermissionStates[permissionState.Principal];
                }
                else
                {
                    permissionStates = new PermissionStateCollection();
                    this.principalToPermissionStates.Add(permissionState.Principal, permissionStates);
                }

                permissionStates.Add(permissionState);

                permissionState.Changed += new EventHandler(this.OnChanged);

                // in the case of the permissions of principals controls, the principals collection will be
                // initially null because no control will have asked for the principals.  In that case
                // we need to create the collection without fetching the collection of other principals
                // with permissions on the securable before adding the new principal to the collection.
#pragma warning disable IDE0074 // Use compound assignment
                if (this.principals == null)
                {
                    this.principals = new PrincipalCollection();
                }
#pragma warning restore IDE0074 // Use compound assignment

                if (!this.principals.Contains(permissionState.Principal))
                {
                    this.principals.Add(permissionState.Principal);
                }
            }

            /// <summary>
            /// Handle permission selection change events
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnChanged(object sender, EventArgs e)
            {
                this.NotifyObservers(sender, e);
            }

            /// <summary>
            /// Property to access the observable event.
            /// </summary>
            internal event EventHandler Changed
            {
                add { this.observableChanged += value; }
                remove { this.observableChanged -= value; }
            }

            /// <summary>
            /// Notify all observers that this object has changed.
            /// </summary>
            /// <param name="sender">The object that changed</param>
            /// <param name="e">Hint for the notification, usually null</param>
            private void NotifyObservers(object sender, EventArgs e)
            {
                if (this.observableChanged != null)
                {
                    this.observableChanged(sender, e);
                }
            }

            /// <summary>
            /// Notify all observers that this object or one of its children has changed.
            /// </summary>
            private void NotifyObservers()
            {
                this.NotifyObservers(this, new EventArgs());
            }


            /// <summary>
            /// Static constructor
            /// </summary>
            static Securable()
            {
            }

            /// <summary>
            /// Get the set of permissions relevent to a particular object type
            /// </summary>
            /// <param name="type">The object type</param>
            /// <param name="serverVersion"></param>
            /// <param name="databaseName"></param>
            /// <param name="databaseEngineType"></param>
            /// <param name="engineEdition"></param>
            /// <returns>The relevant permissions</returns>
            public static ArrayList GetRelevantPermissions(SecurableType type, Version serverVersion, string databaseName,DatabaseEngineType databaseEngineType, DatabaseEngineEdition engineEdition)
            {
                // $CONSIDER caching relevent permission sets
                ArrayList result = new ArrayList();

                switch (type)
                {
                    case SecurableType.AggregateFunction:

                        // STrace.Assert(YUKON <= serverVersion.Major, "Aggregate Function's permissions don't exist for pre-yukon servers");

                        result.Add(Permission.References);
                        result.Add(Permission.Execute);
                        result.Add(Permission.ViewDefinition);
                        result.Add(Permission.Alter);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.Control);

                        break;

                    case SecurableType.ApplicationRole:

                        // STrace.Assert(YUKON <= serverVersion.Major, "application role permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.Assembly:

                        // STrace.Assert(YUKON <= serverVersion.Major, "assembly permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        // Permission Execute has been removed by engine from katmai. This was because deny was not honoured by engine silently earlier
                        // on assemblies. see vsts:30530 for details.-anchals
                        if (serverVersion.Major < KATMAI)
                        {
                            result.Add(Permission.Execute);
                        }
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.AsymmetricKey:

                        // STrace.Assert(YUKON <= serverVersion.Major, "asymmetric key permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.Certificate:

                        // STrace.Assert(YUKON <= serverVersion.Major, "certificate permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.Column:

                        // permissions for columns of tables, views, and inline functions

                        result.Add(Permission.Update);
                        result.Add(Permission.Select);

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.References);
                        }

                        break;

                    case SecurableType.ColumnFunctionTable:

                        // permissions for columns of table-valued functions

                        result.Add(Permission.Select);

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.References);
                        }

                        break;

                    //                  case SecurableType.Contract:
                    //
                    //                      // STrace.Assert(YUKON <= serverVersion.Major, "contract permissions don't exist for pre-yukon servers");
                    //
                    //                      result.Add(Permission.Alter);
                    //                      result.Add(Permission.Control);
                    //                      result.Add(Permission.References);
                    //                      result.Add(Permission.TakeOwnership);
                    //                      result.Add(Permission.ViewDefinition);
                    //
                    //                      break;

                    case SecurableType.Database:

                        result.Add(Permission.BackupDatabase);
                        if (databaseName == "master")
                        {
                            // create database permission only valid in master
                            result.Add(Permission.CreateDatabase);
                        }

                        if (engineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                        {
                            result.Add(Permission.BackupLog);
                            result.Add(Permission.CreateDefault);
                            result.Add(Permission.CreateRule);
                        }

                        result.Add(Permission.CreateProcedure);
                        result.Add(Permission.CreateTable);
                        result.Add(Permission.CreateView);

                        if (SHILOH <= serverVersion.Major)
                        {
                            result.Add(Permission.CreateFunction);
                        }

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.Alter);

                            if (engineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                            {
                                result.Add(Permission.AlterAnyApplicationRole);
                                result.Add(Permission.AlterAnyAssembly);
                                result.Add(Permission.AlterAnyAsymmetricKey);
                                result.Add(Permission.AlterAnyCertificate);
                            }

                            if (engineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                            {
                                if (KATMAI <= serverVersion.Major)
                                {
                                    result.Add(Permission.AlterAnyDatabaseAudit);
                                }
                                result.Add(Permission.AlterAnyContract);
                                result.Add(Permission.AlterAnyDatabaseDdlTrigger);
                                result.Add(Permission.AlterAnyDatabaseEventNotification);
                                result.Add(Permission.AlterAnyDataspace);
                                result.Add(Permission.AlterAnyFulltextCatalog);
                                result.Add(Permission.AlterAnyMessageType);
                                result.Add(Permission.AlterAnyRemoteServiceBinding);
                                result.Add(Permission.AlterAnyRoute);
                                result.Add(Permission.AlterAnyService);
                                result.Add(Permission.AlterAnySymmetricKey);
                                result.Add(Permission.Authenticate);
                                result.Add(Permission.Checkpoint);
                                result.Add(Permission.ConnectReplication);
                                result.Add(Permission.CreateAggregate);
                                result.Add(Permission.CreateAssembly);
                                result.Add(Permission.CreateAsymmetricKey);
                                result.Add(Permission.CreateCertificate);
                                result.Add(Permission.CreateContract);
                                result.Add(Permission.CreateDatabaseDdlEventNotification);

                                result.Add(Permission.CreateFulltextCatalog);
                                result.Add(Permission.CreateService);
                                result.Add(Permission.CreateSymmetricKey);
                                result.Add(Permission.CreateSynonym);
                                result.Add(Permission.CreateType);
                                result.Add(Permission.CreateXmlSchemaCollection);
                                result.Add(Permission.CreateMessageType);
                                result.Add(Permission.CreateQueue);
                                result.Add(Permission.CreateRemoteServiceBinding);
                                result.Add(Permission.CreateRoute);
                                result.Add(Permission.SubscribeQueryNotifications);
                            }

                            result.Add(Permission.AlterAnyRole);
                            result.Add(Permission.AlterAnySchema);

                            result.Add(Permission.AlterAnyUser);
                            result.Add(Permission.Connect);

                            result.Add(Permission.Control);

                            result.Add(Permission.CreateRole);
                            result.Add(Permission.CreateSchema);


                            result.Add(Permission.Delete);
                            result.Add(Permission.Execute);
                            result.Add(Permission.Insert);
                            result.Add(Permission.References);
                            result.Add(Permission.Select);
                            result.Add(Permission.ShowPlan);

                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Update);
                            result.Add(Permission.ViewDatabaseState);
                            result.Add(Permission.ViewDefinition);
                            if (Utils.IsSql13OrLater(serverVersion.Major))
                            {
                                if (engineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                                {
                                    result.Add(Permission.AlterAnySecurityPolicy);
                                }

                                result.Add(Permission.AlterAnyExternalDataSource);
                                result.Add(Permission.AlterAnyExternalFileFormat);
                                result.Add(Permission.AlterAnyMask);
                                result.Add(Permission.Unmask);
                                result.Add(Permission.ViewAnyColumnEncryptionKeyDefinition);
                                result.Add(Permission.ViewAnyColumnMasterKeyDefinition);
                            }

                            if (Utils.IsSql15OrLater(serverVersion.Major))
                            {
                                // The condition will be removed once Data classification feature will be deployed on DW
                                if (engineEdition != DatabaseEngineEdition.SqlDataWarehouse)
                                {
                                    result.Add(Permission.AlterAnySensitivityClassification);
                                    result.Add(Permission.ViewAnySensitivityClassification);
                                }

                            }
                        }
                        break;

                    case SecurableType.DatabaseRole:

                        // STrace.Assert(YUKON <= serverVersion.Major, "role permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.Endpoint:

                        // STrace.Assert(YUKON <= serverVersion.Major, "endpoint permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Connect);
                        result.Add(Permission.Control);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.FullTextCatalog:

                        // STrace.Assert(YUKON <= serverVersion.Major, "full-text catalog permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.ExtendedStoredProcedure:
                    case SecurableType.StoredProcedure:

                        result.Add(Permission.Execute);

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.ViewDefinition);
                            result.Add(Permission.Alter);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Control);
                        }

                        break;

                    case SecurableType.FunctionInline:

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.Select);
                            result.Add(Permission.References);
                            result.Add(Permission.ViewDefinition);
                            result.Add(Permission.Alter);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Control);
                            result.Add(Permission.Insert);
                            result.Add(Permission.Delete);
                            result.Add(Permission.Update);
                        }
                        else
                        {
                            result.Add(Permission.Insert);
                            result.Add(Permission.Update);
                            result.Add(Permission.Delete);
                            result.Add(Permission.Select);
                            result.Add(Permission.References);
                        }

                        break;

                    case SecurableType.FunctionTable:

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.Select);
                            result.Add(Permission.References);
                            result.Add(Permission.ViewDefinition);
                            result.Add(Permission.Alter);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Control);
                        }
                        else
                        {
                            result.Add(Permission.Insert);
                            result.Add(Permission.Update);
                            result.Add(Permission.Delete);
                            result.Add(Permission.Select);
                            result.Add(Permission.References);
                        }

                        break;

                    case SecurableType.FunctionScalar:

                        result.Add(Permission.References);
                        result.Add(Permission.Execute);

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.ViewDefinition);
                            result.Add(Permission.Alter);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Control);
                        }

                        break;

                    case SecurableType.Login:

                        // STrace.Assert(YUKON <= serverVersion.Major, "login permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.Impersonate);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.Schema:

                        // STrace.Assert(YUKON <= serverVersion.Major, "schema permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.Delete);
                        result.Add(Permission.Execute);
                        result.Add(Permission.Insert);
                        result.Add(Permission.References);
                        result.Add(Permission.Select);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.Update);
                        result.Add(Permission.ViewDefinition);
                        if (KATMAI <= serverVersion.Major)
                        {
                            result.Add(Permission.ViewChangeTracking);
                        }
                        if (Utils.IsSql11OrLater(serverVersion.Major))
                        {
                            result.Add(Permission.CreateSequence);
                        }
                        break;

                    case SecurableType.Server:

                        // STrace.Assert(YUKON <= serverVersion.Major, "server permissions don't exist for pre-yukon servers");

                        result.Add(Permission.AdministerBulkOperations);

                        if (Utils.IsSql11OrLater(serverVersion.Major))
                        {
                            result.Add(Permission.AlterAnyServerRole);
                            result.Add(Permission.CreateServerRole);
                            result.Add(Permission.AlterAnyAvailabilityGroup);
                            result.Add(Permission.CreateAvailabilityGroup);
                        }

                        if (Utils.IsSql11OrLater(serverVersion.Major))
                        {
                            result.Add(Permission.AlterAnyEventSession);
                        }
                        if (KATMAI <= serverVersion.Major)
                        {
                            result.Add(Permission.AlterAnyServerAudit);
                        }
                        result.Add(Permission.AlterAnyLinkedServer);

                        if (Utils.IsSql12OrLater(serverVersion.Major))
                        {
                            result.Add(Permission.SelectAllUserSecurables);
                            result.Add(Permission.ConnectAnyDatabase);
                            result.Add(Permission.ImpersonateAnyLogin);
                        }

                        result.Add(Permission.AlterAnyConnection);
                        result.Add(Permission.AlterAnyCredential);
                        result.Add(Permission.AlterAnyDatabase);
                        result.Add(Permission.AlterAnyEndpoint);
                        result.Add(Permission.AlterAnyEventNotification);
                        result.Add(Permission.AlterAnyLogin);
                        result.Add(Permission.AlterResources);
                        result.Add(Permission.AlterServerState);
                        result.Add(Permission.AlterSettings);
                        result.Add(Permission.AlterTrace);
                        result.Add(Permission.AuthenticateServer);
                        result.Add(Permission.ConnectSql);
                        result.Add(Permission.ControlServer);
                        result.Add(Permission.CreateAnyDatabase);
                        result.Add(Permission.CreateDdlEventNotification);
                        result.Add(Permission.CreateEndpoint);
                        result.Add(Permission.CreateTraceEventNotification);
                        result.Add(Permission.ExternalAccessAssembly);
                        result.Add(Permission.Shutdown);
                        result.Add(Permission.UnsafeAssembly);
                        result.Add(Permission.ViewAnyDatabase);
                        result.Add(Permission.ViewAnyDefinition);
                        result.Add(Permission.ViewServerState);
                        break;

                    case SecurableType.ServerRole:

                        // STrace.Assert(Utils.IsSql11OrLater(serverVersion.Major), "Server Role permissions don't exist for pre-sql11 servers");

                        if (Utils.IsSql11OrLater(serverVersion.Major))
                        {
                            result.Add(Permission.Alter);
                            result.Add(Permission.Control);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.ViewDefinition);
                        }
                        break;

                    case SecurableType.ServiceQueue:

                        // STrace.Assert(YUKON <= serverVersion.Major, "Service Queue permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.References);
                        result.Add(Permission.Receive);
                        result.Add(Permission.Select);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);

                        break;

                    case SecurableType.SymmetricKey:

                        // STrace.Assert(YUKON <= serverVersion.Major, "symmetric key permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);

                        break;

                    case SecurableType.Synonym:

                        // STrace.Assert(YUKON <= serverVersion.Major, "user permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Control);
                        result.Add(Permission.Delete);
                        result.Add(Permission.Execute);
                        result.Add(Permission.Insert);
                        result.Add(Permission.Select);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.Update);
                        result.Add(Permission.ViewDefinition);

                        break;
                    case SecurableType.Sequence:

                        if (Utils.IsSql11OrLater(serverVersion.Major))
                        {
                            result.Add(Permission.Control);
                            result.Add(Permission.Alter);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Update);
                            result.Add(Permission.ViewDefinition);
                            result.Add(Permission.References);
                        }
                        break;

                    case SecurableType.Table:
                    case SecurableType.View:

                        result.Add(Permission.Insert);
                        result.Add(Permission.Update);
                        result.Add(Permission.Delete);
                        result.Add(Permission.Select);
                        result.Add(Permission.References);

                        if (YUKON <= serverVersion.Major)
                        {
                            result.Add(Permission.ViewDefinition);
                            result.Add(Permission.Alter);
                            result.Add(Permission.TakeOwnership);
                            result.Add(Permission.Control);
                        }

                        if (KATMAI <= serverVersion.Major)
                        {
                            result.Add(Permission.ViewChangeTracking);
                        }

                        break;

                    case SecurableType.User:

                        // STrace.Assert(YUKON <= serverVersion.Major, "user permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.Impersonate);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.UserDefinedDataType:

                        // STrace.Assert(YUKON <= serverVersion.Major, "UDDT permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Control);
                        result.Add(Permission.Execute);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.UserDefinedTableType:

                        // STrace.Assert(KATMAI <= serverVersion.Major, "UDTT permissions don't exist for pre-Katmai servers");

                        result.Add(Permission.Control);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.XmlSchemaCollection:

                        // STrace.Assert(YUKON <= serverVersion.Major, "XML schema collection permissions don't exist for pre-yukon servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.Execute);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.AvailabilityGroup:

                        // STrace.Assert(DENALI <= serverVersion.Major, "Availability Group permissions do not exists pre-Denali servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.TakeOwnership);
                        result.Add(Permission.ViewDefinition);
                        break;

                    case SecurableType.SecurityPolicy:

                        // STrace.Assert(Utils.IsSql13OrLater(serverVersion.Major), "Security polices do not exist for pre-SQL15 servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.ViewDefinition);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        break;

                    case SecurableType.ExternalDataSource:

                        // STrace.Assert(Utils.IsSql13OrLater(serverVersion.Major), "External Data Sources do not exist for pre-SQL15 servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.ViewDefinition);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        break;

                    case SecurableType.ExternalFileFormat:

                        // STrace.Assert(Utils.IsSql13OrLater(serverVersion.Major), "External File Formats do not exist for pre-SQL15 servers");

                        result.Add(Permission.Alter);
                        result.Add(Permission.Control);
                        result.Add(Permission.ViewDefinition);
                        result.Add(Permission.References);
                        result.Add(Permission.TakeOwnership);
                        break;

                    default:

                        // STrace.Assert(false, "need to add relevent permissions for permissible object type");
                        break;
                }


                return result;
            }


            /// <summary>
            /// Factory method for existing Securables.  This is used by the Permissions on Objects controls.
            /// </summary>
            /// <param name="smoObject">The SMO object for which to create a Securable</param>
            /// <param name="connectionInfo">The connection object</param>
            /// <returns>A corresponding Securable</returns>
            public static Securable Create(SqlSmoObject smoObject, object connectionInfo)
            {
                Securable result = null;
                SecurableType type = Securable.GetSecurableType(smoObject);

                switch (type)
                {

                    case SecurableType.Table:
                    case SecurableType.View:
                    case SecurableType.FunctionInline:
                    case SecurableType.FunctionTable:

                        result = new SecurableColumnParent(smoObject, type, connectionInfo, GetServerVersion(connectionInfo));
                        break;

                    default:

                        result = new Securable(smoObject, type, connectionInfo, GetServerVersion(connectionInfo));
                        break;
                }

                return result;
            }

            /// <summary>
            /// Factory method for new Securables.  This is used by the Permissions on Objects controls.
            /// </summary>
            /// <param name="type">The object type</param>
            /// <param name="parentUrn">The URN string for the parent of the securable</param>
            /// <param name="connectionInfo">Connection information for the server</param>
            /// <returns>A new Securable</returns>
            public static Securable Create(SecurableType type, string parentUrn, object connectionInfo)
            {
                Securable result = null;

                switch (type)
                {
                    case SecurableType.Table:
                    case SecurableType.View:
                    case SecurableType.FunctionInline:
                    case SecurableType.FunctionTable:

                        result = new SecurableColumnParent(type, parentUrn, connectionInfo, GetServerVersion(connectionInfo), false);
                        break;

                    default:

                        result = new Securable(type, parentUrn, connectionInfo, GetServerVersion(connectionInfo), false);
                        break;

                }

                return result;
            }

            /// <summary>
            /// Factory method for existing securables that have been enumerated.  This is used by the Permission of Principals controls.
            /// </summary>
            /// <param name="type">The type of the object that is to be created</param>
            /// <param name="urn">The URN describing the object on the server</param>
            /// <param name="connectionInfo">The connection information that should be used with the enumerator</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <returns>The securable that is created </returns>
            public static Securable Create(SecurableType type, string urn, object connectionInfo, Version serverVersion)
            {
                Securable result = null;

                switch (type)
                {
                    case SecurableType.Table:
                    case SecurableType.View:
                    case SecurableType.FunctionInline:
                    case SecurableType.FunctionTable:

                        result = new SecurableColumnParent(type, urn, connectionInfo, serverVersion, true);
                        break;

                    default:

                        result = new Securable(type, urn, connectionInfo, serverVersion, true);
                        break;

                }

                return result;
            }

            /// <summary>
            /// Factory method for existing securables that have been enumerated.  This is used by the Permission of Principals controls.
            /// </summary>
            /// <param name="objectClass">The SMO object class for the object</param>
            /// <param name="objectType">The sys.objects or sys.server_principals type string for the object</param>
            /// <param name="name">The name of the object</param>
            /// <param name="schemaName">The schema containing the object, or String.Empty if the object is not contained by a schema</param>
            /// <param name="isTableType">for user-defined types, indicate whether it is a data type or a table type</param>
            /// <param name="databaseName">The database containing the object, or String.Empty if the object is not contained by a database</param>
            /// <param name="connectionInfo">The connection information that should be used with the enumerator</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <returns>The securable that is created</returns>
            public static Securable Create(
                                                  ObjectClass objectClass,
                                                  string objectType,
                                                  string name,
                                                  string schemaName,
                                                  bool isTableType,
                                                  string databaseName,
                                                  object connectionInfo,
                                                  Version serverVersion)
            {

                Securable result = null;
                string urn = String.Empty;

                if (FormUrn(objectClass, objectType, databaseName, schemaName, name, isTableType, out urn))
                {
                    SecurableType type = GetSecurableType(objectClass, objectType, isTableType);
                    result = Create(type, urn, connectionInfo, serverVersion);
                }
                else
                {
                    throw new ArgumentException();
                }

                return result;
            }

            /// <summary>
            /// Factory method for existing securables that have been returned by the SQL Object Search dialog.
            /// </summary>
            /// <param name="searchableObject">The search dialog result</param>
            /// <param name="connectionInfo">The connection information that should be used with the enumerator</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <returns>The securable that is created</returns>
            public static Securable Create(SearchableObject searchableObject, object connectionInfo, Version serverVersion)
            {
                SecurableType type = GetSecurableType(searchableObject.SearchableObjectType);
                return Create(type, searchableObject.Urn.ToString(), connectionInfo, serverVersion);
            }


            /// <summary>
            /// Get a URN string for all server Objects of a particular securable type
            /// </summary>
            /// <param name="type">The type of the securable to enumerate</param>
            /// <returns>The URN string</returns>
            public static string GetUrnForAll(SecurableType type)
            {
                string result = String.Empty;

                switch (type)
                {
                    case SecurableType.Login:

                        result = "Server/Login";
                        break;

                    case SecurableType.Endpoint:

                        result = "Server/Endpoint";
                        break;

                    case SecurableType.ServerRole:

                        result = "Server/Role";
                        break;

                    default:

                        // STrace.Assert(false, "Unexpected securable type.  Is this a server object?");
                        throw new ArgumentException();
                }

                return result;
            }

            /// <summary>
            /// Get a URN string for all database Objects of a particular securable type
            /// </summary>
            /// <param name="type">The securable type</param>
            /// <param name="databaseName">The name of the database</param>
            /// <returns>The URN string</returns>
            public static string GetUrnForAll(SecurableType type, string databaseName)
            {
                // STrace.Assert((databaseName != null) && (databaseName.Length != 0), "database name is empty");

                StringBuilder urn = new StringBuilder();
                urn.AppendFormat("Server/Database[@Name='{0}']/", Urn.EscapeString(databaseName));

                switch (type)
                {
                    case SecurableType.AggregateFunction:

                        urn.Append("UserDefinedAggregate");
                        break;

                    case SecurableType.ApplicationRole:

                        urn.Append("ApplicationRole");
                        break;

                    case SecurableType.Assembly:

                        urn.Append("SqlAssembly");
                        break;

                    case SecurableType.AsymmetricKey:

                        urn.Append("AsymmetricKey");
                        break;

                    case SecurableType.Certificate:

                        urn.Append("Certificate");
                        break;

                    case SecurableType.DatabaseRole:

                        urn.Append("Role");
                        break;

                    case SecurableType.ExtendedStoredProcedure:

                        urn.Append("ExtendedStoredProcedure");
                        break;

                    case SecurableType.ExternalDataSource:

                        urn.Append("ExternalDataSource");
                        break;

                    case SecurableType.ExternalFileFormat:

                        urn.Append("ExternalFileFormat");
                        break;

                    case SecurableType.FullTextCatalog:

                        urn.Append("FullTextCatalog");
                        break;

                    case SecurableType.FunctionInline:
                    case SecurableType.FunctionScalar:
                    case SecurableType.FunctionTable:

                        urn.Append("UserDefinedFunction");
                        break;

                    case SecurableType.Schema:

                        urn.Append("Schema");
                        break;

                    case SecurableType.SecurityPolicy:

                        urn.Append("SecurityPolicy");
                        break;

                    case SecurableType.ServiceQueue:

                        urn.Append("ServiceBroker/ServiceQueue");
                        break;

                    case SecurableType.StoredProcedure:

                        urn.Append("StoredProcedure");
                        break;

                    case SecurableType.SymmetricKey:

                        urn.Append("SymmetricKey");
                        break;

                    case SecurableType.Synonym:

                        urn.Append("Synonym");
                        break;

                    case SecurableType.Sequence:

                        urn.Append("Sequence");
                        break;

                    case SecurableType.Table:

                        urn.Append("Table");
                        break;

                    case SecurableType.User:

                        urn.Append("User");
                        break;

                    case SecurableType.UserDefinedDataType:

                        urn.Append("UserDefinedDataType");
                        break;

                    case SecurableType.UserDefinedTableType:

                        urn.Append("UserDefinedTableType");
                        break;

                    case SecurableType.View:

                        urn.Append("View");
                        break;

                    case SecurableType.XmlSchemaCollection:

                        urn.Append("XmlSchemaCollection");
                        break;

                    case SecurableType.AvailabilityGroup:

                        urn.Append("AvailabilityGroup");
                        break;

                    default:

                        // STrace.Assert(false, "Unexpected securable type.  Is this a database object?");
                        throw new ArgumentException();
                }

                return urn.ToString();
            }

            /// <summary>
            /// Get a URN string for all database Objects of a particular securable type in a particular schema
            /// </summary>
            /// <param name="type">The securable type</param>
            /// <param name="databaseName">The name of the database</param>
            /// <param name="schemaName">The name of the schema</param>
            /// <returns>The URN string</returns>
            public static string GetUrnForAll(SecurableType type, string databaseName, string schemaName)
            {
                // STrace.Assert((databaseName != null) && (databaseName.Length != 0), "database name is empty");
                // STrace.Assert((schemaName != null) && (schemaName.Length != 0), "schema name is empty");

                StringBuilder urn = new StringBuilder();
                urn.AppendFormat("Server/Database[@Name='{0}']/", Urn.EscapeString(databaseName));

                switch (type)
                {
                    case SecurableType.AggregateFunction:

                        urn.Append("UserDefinedAggregate");
                        break;

                    case SecurableType.ExtendedStoredProcedure:

                        urn.Append("ExtendedStoredProcedure");
                        break;

                    case SecurableType.FunctionInline:
                    case SecurableType.FunctionScalar:
                    case SecurableType.FunctionTable:

                        urn.Append("UserDefinedFunction");
                        break;

                    case SecurableType.StoredProcedure:

                        urn.Append("StoredProcedure");
                        break;

                    case SecurableType.Synonym:

                        urn.Append("Synonym");
                        break;

                    case SecurableType.Sequence:

                        urn.Append("Sequence");
                        break;

                    case SecurableType.Table:

                        urn.Append("Table");
                        break;

                    case SecurableType.UserDefinedDataType:

                        urn.Append("UserDefinedDataType");
                        break;

                    case SecurableType.UserDefinedTableType:

                        urn.Append("UserDefinedTableType");
                        break;

                    case SecurableType.View:

                        urn.Append("View");
                        break;

                    case SecurableType.XmlSchemaCollection:

                        urn.Append("XmlSchemaCollection");
                        break;

                    default:

                        // STrace.Assert(false, "Unexpected securable type.  Is this a schema object?");
                        throw new ArgumentException();
                }

                urn.AppendFormat("[@Schema='{0}']", Urn.EscapeString(schemaName));
                return urn.ToString();
            }


            /// <summary>
            /// Get the SecurableType for a particular SMO object
            /// </summary>
            /// <param name="smoObject">The SMO object</param>
            /// <returns>The type for the SMO object</returns>
            public static SecurableType GetSecurableType(SqlSmoObject smoObject)
            {
                SecurableType result = SecurableType.Unknown;
                Type smoObjectType = smoObject.GetType();

                // $CONSIDER this switch statement is ugly and almost certainly has abysmal performance, but it
                // should be called only once in the lifetime of the dialog.  On the other hand, it's called
                // during start-up, so we might want to try to think of a better way to map from Type to
                // SecurableType.

                if (typeof(UserDefinedAggregate).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.AggregateFunction;
                }
                else if (typeof(ApplicationRole).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.ApplicationRole;
                }
                else if (typeof(SqlAssembly).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Assembly;
                }
                else if (typeof(AsymmetricKey).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.AsymmetricKey;
                }
                else if (typeof(Certificate).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Certificate;
                }
                else if (typeof(Column).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Column;
                }
                else if (typeof(Database).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Database;
                }
                else if (typeof(DatabaseRole).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.DatabaseRole;
                }
                else if (typeof(Endpoint).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Endpoint;
                }
                else if (typeof(ExtendedStoredProcedure).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.ExtendedStoredProcedure;
                }
                else if (typeof(ExternalDataSource).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.ExternalDataSource;
                }
                else if (typeof(ExternalFileFormat).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.ExternalFileFormat;
                }
                else if (typeof(FullTextCatalog).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.FullTextCatalog;
                }
                else if (typeof(UserDefinedFunction).IsAssignableFrom(smoObjectType))
                {
                    UserDefinedFunction function = (UserDefinedFunction)smoObject;

                    switch (function.FunctionType)
                    {
                        case UserDefinedFunctionType.Inline:

                            result = SecurableType.FunctionInline;
                            break;

                        case UserDefinedFunctionType.Scalar:

                            result = SecurableType.FunctionScalar;
                            break;

                        default:

                            // STrace.Assert(UserDefinedFunctionType.Table == function.FunctionType, "unexpected UDF type");
                            result = SecurableType.FunctionTable;
                            break;
                    }
                }
                else if (typeof(Login).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Login;
                }
                else if (typeof(ServerRole).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.ServerRole;
                }
                else if (typeof(Schema).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Schema;
                }
                else if (typeof(SecurityPolicy).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.SecurityPolicy;
                }
                else if (typeof(Microsoft.SqlServer.Management.Smo.Server).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Server;
                }
                else if (typeof(ServiceQueue).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.ServiceQueue;
                }
                else if (typeof(StoredProcedure).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.StoredProcedure;
                }
                else if (typeof(SymmetricKey).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.SymmetricKey;
                }
                else if (typeof(Synonym).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Synonym;
                }
                else if (typeof(Sequence).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Sequence;
                }
                else if (typeof(Table).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.Table;
                }
                else if (typeof(User).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.User;
                }
                else if (typeof(UserDefinedDataType).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.UserDefinedDataType;
                }
                else if (typeof(UserDefinedTableType).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.UserDefinedTableType;
                }
                else if (typeof(View).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.View;
                }
                else if (typeof(XmlSchemaCollection).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.XmlSchemaCollection;
                }
                else if (typeof(AvailabilityGroup).IsAssignableFrom(smoObjectType))
                {
                    result = SecurableType.AvailabilityGroup;
                }
                else
                {
                    // STrace.Assert(false, "don't know which permissible object type corresponds to smo object");
                    throw new InvalidArgumentException();
                }

                // STrace.Assert(SecurableType.Unknown != result, "result is still unknown!");
                return result;
            }

            /// <summary>
            /// Get the SecurableType corresponding to a particular SearchableType
            /// </summary>
            /// <param name="searchableType">The searchable type</param>
            /// <returns>The corresponding securable type, or SecurableType.Unknown if there is no corresponding securable type</returns>
            public static SecurableType GetSecurableType(SearchableObjectType searchableType)
            {
                SecurableType result = SecurableType.Unknown;

                switch (searchableType)
                {
                    case SearchableObjectType.AggregateFunction:

                        result = SecurableType.AggregateFunction;
                        break;

                    case SearchableObjectType.ApplicationRole:

                        result = SecurableType.ApplicationRole;
                        break;

                    case SearchableObjectType.Assembly:

                        result = SecurableType.Assembly;
                        break;

                    case SearchableObjectType.AsymmetricKey:

                        result = SecurableType.AsymmetricKey;
                        break;

                    case SearchableObjectType.Certificate:

                        result = SecurableType.Certificate;
                        break;

                    case SearchableObjectType.Database:

                        result = SecurableType.Database;
                        break;

                    case SearchableObjectType.DatabaseRole:

                        result = SecurableType.DatabaseRole;
                        break;

                    case SearchableObjectType.Endpoint:

                        result = SecurableType.Endpoint;
                        break;

                    case SearchableObjectType.ExtendedStoredProcedure:

                        result = SecurableType.ExtendedStoredProcedure;
                        break;

                    case SearchableObjectType.ExternalDataSource:

                        result = SecurableType.ExternalDataSource;
                        break;

                    case SearchableObjectType.ExternalFileFormat:

                        result = SecurableType.ExternalFileFormat;
                        break;

                    case SearchableObjectType.FullTextCatalog:

                        result = SecurableType.FullTextCatalog;
                        break;

                    case SearchableObjectType.FunctionInline:

                        result = SecurableType.FunctionInline;
                        break;

                    case SearchableObjectType.FunctionScalar:

                        result = SecurableType.FunctionScalar;
                        break;

                    case SearchableObjectType.FunctionTable:

                        result = SecurableType.FunctionTable;
                        break;

                    case SearchableObjectType.Login:

                        result = SecurableType.Login;
                        break;

                    case SearchableObjectType.ServerRole:

                        result = SecurableType.ServerRole;
                        break;

                    case SearchableObjectType.Schema:

                        result = SecurableType.Schema;
                        break;

                    case SearchableObjectType.Server:

                        result = SecurableType.Server;
                        break;

                    case SearchableObjectType.SecurityPolicy:

                        result = SecurableType.SecurityPolicy;
                        break;

                    case SearchableObjectType.ServiceQueue:

                        result = SecurableType.ServiceQueue;
                        break;

                    case SearchableObjectType.StoredProcedure:

                        result = SecurableType.StoredProcedure;
                        break;

                    case SearchableObjectType.SymmetricKey:

                        result = SecurableType.SymmetricKey;
                        break;

                    case SearchableObjectType.Synonym:

                        result = SecurableType.Synonym;
                        break;

                    case SearchableObjectType.Sequence:

                        result = SecurableType.Sequence;
                        break;

                    case SearchableObjectType.Table:

                        result = SecurableType.Table;
                        break;

                    case SearchableObjectType.User:

                        result = SecurableType.User;
                        break;

                    case SearchableObjectType.UserDefinedDataType:

                        result = SecurableType.UserDefinedDataType;
                        break;

                    case SearchableObjectType.UserDefinedTableType:

                        result = SecurableType.UserDefinedTableType;
                        break;

                    case SearchableObjectType.View:

                        result = SecurableType.View;
                        break;

                    case SearchableObjectType.XmlSchemaCollection:

                        result = SecurableType.XmlSchemaCollection;
                        break;

                    case SearchableObjectType.AvailabilityGroup:

                        result = SecurableType.AvailabilityGroup;
                        break;

                    default:

                        // STrace.Assert(false, "unexpected SeachableObjectType - we need to add a new case here.");
                        break;
                }

                return result;
            }

            /// <summary>
            /// Get the Securable type corresponding to a particular ObjectClass and sys.objects type
            /// </summary>
            /// <param name="smoObjectClass">The SMO ObjectClass</param>
            /// <param name="objectTypeString">The sys.objects type string</param>
            /// <param name="isTableType">for user-defined types, indicate whether it is a data type or a table type</param>
            /// <returns>The corresponding securable type, or SecurableType.Unknown if the ObjectClass/string isn't recognized</returns>
            public static SecurableType GetSecurableType(ObjectClass smoObjectClass, string objectTypeString, bool isTableType)
            {
                SecurableType result = SecurableType.Unknown;

                if (objectTypeString.Length != 0)
                {
                    result = GetSecurableType(objectTypeString);
                }
                else
                {
                    switch (smoObjectClass)
                    {
                        case ObjectClass.ApplicationRole:

                            result = SecurableType.ApplicationRole;
                            break;

                        case ObjectClass.Certificate:

                            result = SecurableType.Certificate;
                            break;

                        case ObjectClass.Database:

                            result = SecurableType.Database;
                            break;

                        case ObjectClass.DatabaseRole:

                            result = SecurableType.DatabaseRole;
                            break;

                        case ObjectClass.Endpoint:

                            result = SecurableType.Endpoint;
                            break;

                        case ObjectClass.ExternalDataSource:

                            result = SecurableType.ExternalDataSource;
                            break;

                        case ObjectClass.ExternalFileFormat:

                            result = SecurableType.ExternalFileFormat;
                            break;

                        case ObjectClass.FullTextCatalog:

                            result = SecurableType.FullTextCatalog;
                            break;

                        case ObjectClass.Login:

                            result = SecurableType.Login;
                            break;

                        case ObjectClass.ServerRole:

                            result = SecurableType.ServerRole;
                            break;

                        case ObjectClass.Schema:

                            result = SecurableType.Schema;
                            break;

                        case ObjectClass.Server:

                            result = SecurableType.Server;
                            break;

                        case ObjectClass.Service:
                        case ObjectClass.SqlAssembly:

                            result = SecurableType.Assembly;
                            break;

                        case ObjectClass.SymmetricKey:

                            result = SecurableType.SymmetricKey;
                            break;

                        case ObjectClass.User:

                            result = SecurableType.User;
                            break;

                        case ObjectClass.ObjectOrColumn:

                            result = GetSecurableType(objectTypeString);
                            break;

                        case ObjectClass.UserDefinedType:

                            if (isTableType)
                            {
                                result = SecurableType.UserDefinedTableType;
                            }
                            else
                            {
                                result = SecurableType.UserDefinedDataType;
                            }
                            break;

                        case ObjectClass.AvailabilityGroup:

                            result = SecurableType.AvailabilityGroup;
                            break;

                        default:
                            // STrace.Assert(result != SecurableType.Unknown, "unknown ObjectClass: " + smoObjectClass.ToString());
                            break;
                    }
                }

                return result;
            }

            /// <summary>
            /// Get the SecurableType corresponding to a particular sys.objects.type value
            /// </summary>
            /// <param name="objectTypeString">The sys.objects.type value</param>
            /// <returns>The corresponding securable type, or SecurableType.Unknown if the string isn't recognized</returns>
            public static SecurableType GetSecurableType(string objectTypeString)
            {
                SecurableType result = SecurableType.Unknown;

                switch (objectTypeString)
                {
                    case "AF":

                        result = SecurableType.AggregateFunction;
                        break;

                    case "P":
                    case "PC":
                    case "RF":

                        result = SecurableType.StoredProcedure;
                        break;

                    case "FN":
                    case "FS":

                        result = SecurableType.FunctionScalar;
                        break;

                    case "IF":

                        result = SecurableType.FunctionInline;
                        break;

                    case "TF":
                    case "FT":

                        result = SecurableType.FunctionTable;
                        break;

                    case "SQ":

                        result = SecurableType.ServiceQueue;
                        break;

                    case "SN":

                        result = SecurableType.Synonym;
                        break;

                    case "SO":

                        result = SecurableType.Sequence;
                        break;

                    case "SP":

                        result = SecurableType.SecurityPolicy;
                        break;

                    case "U":

                        result = SecurableType.Table;
                        break;

                    case "V":

                        result = SecurableType.View;
                        break;

                    case "X":

                        result = SecurableType.ExtendedStoredProcedure;
                        break;

                    default:

                        // object must be one of the types that we aren't showing in the UI
                        result = SecurableType.Unknown;
                        break;
                }

                return result;
            }


            /// <summary>
            /// Get the SearchableObjectType equivalent to the input SecurableType
            /// </summary>
            /// <param name="type">The Securable type to convert</param>
            /// <returns>The equivalent SearchableObjectType</returns>
            public static SearchableObjectType GetSearchableObjectType(SecurableType type)
            {
                SearchableObjectType result = SearchableObjectType.LastType;

                switch (type)
                {
                    case SecurableType.AggregateFunction:

                        result = SearchableObjectType.AggregateFunction;
                        break;

                    case SecurableType.ApplicationRole:

                        result = SearchableObjectType.ApplicationRole;
                        break;

                    case SecurableType.Assembly:

                        result = SearchableObjectType.Assembly;
                        break;

                    case SecurableType.AsymmetricKey:

                        result = SearchableObjectType.AsymmetricKey;
                        break;

                    case SecurableType.Certificate:

                        result = SearchableObjectType.Certificate;
                        break;

                    case SecurableType.Database:

                        result = SearchableObjectType.Database;
                        break;

                    case SecurableType.DatabaseRole:

                        result = SearchableObjectType.DatabaseRole;
                        break;

                    case SecurableType.Endpoint:

                        result = SearchableObjectType.Endpoint;
                        break;

                    case SecurableType.ExtendedStoredProcedure:

                        result = SearchableObjectType.ExtendedStoredProcedure;
                        break;

                    case SecurableType.ExternalDataSource:

                        result = SearchableObjectType.ExternalDataSource;
                        break;

                    case SecurableType.ExternalFileFormat:

                        result = SearchableObjectType.ExternalFileFormat;
                        break;

                    case SecurableType.FullTextCatalog:

                        result = SearchableObjectType.FullTextCatalog;
                        break;

                    case SecurableType.FunctionInline:

                        result = SearchableObjectType.FunctionInline;
                        break;

                    case SecurableType.FunctionScalar:

                        result = SearchableObjectType.FunctionScalar;
                        break;

                    case SecurableType.FunctionTable:

                        result = SearchableObjectType.FunctionTable;
                        break;

                    case SecurableType.Login:

                        result = SearchableObjectType.Login;
                        break;

                    case SecurableType.ServerRole:

                        result = SearchableObjectType.ServerRole;
                        break;

                    case SecurableType.Schema:

                        result = SearchableObjectType.Schema;
                        break;

                    case SecurableType.SecurityPolicy:

                        result = SearchableObjectType.SecurityPolicy;
                        break;

                    case SecurableType.Server:

                        result = SearchableObjectType.Server;
                        break;

                    case SecurableType.ServiceQueue:

                        result = SearchableObjectType.ServiceQueue;
                        break;

                    case SecurableType.StoredProcedure:

                        result = SearchableObjectType.StoredProcedure;
                        break;

                    case SecurableType.SymmetricKey:

                        result = SearchableObjectType.SymmetricKey;
                        break;

                    case SecurableType.Synonym:

                        result = SearchableObjectType.Synonym;
                        break;

                    case SecurableType.Sequence:

                        result = SearchableObjectType.Sequence;
                        break;

                    case SecurableType.Table:

                        result = SearchableObjectType.Table;
                        break;

                    case SecurableType.User:

                        result = SearchableObjectType.User;
                        break;

                    case SecurableType.UserDefinedDataType:

                        result = SearchableObjectType.UserDefinedDataType;
                        break;

                    case SecurableType.UserDefinedTableType:

                        result = SearchableObjectType.UserDefinedTableType;
                        break;

                    case SecurableType.View:

                        result = SearchableObjectType.View;
                        break;

                    case SecurableType.XmlSchemaCollection:

                        result = SearchableObjectType.XmlSchemaCollection;
                        break;

                    case SecurableType.AvailabilityGroup:

                        result = SearchableObjectType.AvailabilityGroup;
                        break;

                    default:

                        // STrace.Assert(false, "Can't convert securable type to searchable object type");
                        throw new ArgumentException();
                }

                return result;

            }

            /// <summary>
            /// Get an image for display in the securable selector for the securable type
            /// </summary>
            /// <param name="type">The type </param>
            /// <returns>The image</returns>
            // public static System.Drawing.Image GetImage(SecurableType type)
            // {
            //     SearchableObjectType searchableType = GetSearchableObjectType(type);
            //     SearchableObjectTypeDescription description = SearchableObjectTypeDescription.GetDescription(searchableType);
            //     return description.Image;
            // }

            /// <summary>
            /// Form a URN string for the object
            /// </summary>
            /// <param name="objectClass">The SMO ObjectClass of the object</param>
            /// <param name="objectType">The sys.objects type of the object if applicable, String.Empty if not applicable</param>
            /// <param name="databaseName">The name of the database containing the object if applicable, String.Empty if not applicable</param>
            /// <param name="schemaName">The name of the schema containing the object if applicable, String.Empty if not applicable</param>
            /// <param name="name">The name of the object</param>
            /// <param name="isTableType">for user-defined types, indicate whether it is a data type or a table type</param>
            /// <param name="urn">The urn that is formed for the object</param>
            /// <returns>True if the URN could be formed, false otherwise</returns>
            public static bool FormUrn(
                                                   ObjectClass objectClass,
                                                   string objectType,
                                                   string databaseName,
                                                   string schemaName,
                                                   string name,
                                                   bool isTableType,
                                                   out string urn)
            {
                string objectTypeString = String.Empty;
                bool result = GetUrnTypeString(objectClass, objectType, isTableType, out objectTypeString);

                if (result)
                {
                    StringBuilder urnBuilder;

                    if (objectTypeString != "Server")
                    {
                        urnBuilder = new StringBuilder("Server/");

                        if (databaseName.Length != 0)
                        {
                            urnBuilder.AppendFormat("Database[@Name='{0}']/", Urn.EscapeString(databaseName));
                        }

                        urnBuilder.AppendFormat("{0}[", objectTypeString);

                        if (schemaName.Length != 0)
                        {
                            urnBuilder.AppendFormat("@Schema='{0}' and ", Urn.EscapeString(schemaName));
                        }
                    }
                    else
                    {
                        urnBuilder = new StringBuilder("Server[");
                    }

                    urnBuilder.AppendFormat("@Name='{0}']", Urn.EscapeString(name));

                    urn = urnBuilder.ToString();
                }
                else
                {
                    urn = String.Empty;
                }

                return result;
            }

            /// <summary>
            /// Get the URN substring filtering for object type
            /// </summary>
            /// <param name="objectClass">The SMO ObjectClass of the object</param>
            /// <param name="objectType">The sys.objects type string of the object</param>
            /// <param name="isTableType">for user-defined types, indicate whether it is a data type or a table type</param>
            /// <param name="urnTypeString">The substring that is formed</param>
            /// <returns>True if the substring could be formed, false otherwise</returns>
            private static bool GetUrnTypeString(ObjectClass objectClass, string objectType, bool isTableType, out string urnTypeString)
            {
                bool result = false;
                urnTypeString = String.Empty;

                if (objectType.Length != 0)
                {
                    result = GetUrnTypeString(objectType, out urnTypeString);
                }
                else
                {
                    result = GetUrnTypeString(objectClass, isTableType, out urnTypeString);
                }

                return result;

            }

            /// <summary>
            /// Get the URN substring filtering for object type
            /// </summary>
            /// <param name="objectClass">The SMO ObjectClass for the object</param>
            /// <param name="isTableType">for user-defined types, indicate whether it is a data type or a table type</param>
            /// <param name="urnTypeString">The substring that is formed</param>
            /// <returns>True if the substring could be formed, false otherwise</returns>
            private static bool GetUrnTypeString(ObjectClass objectClass, bool isTableType, out string urnTypeString)
            {
                bool result = false;

                switch (objectClass)
                {
                    case ObjectClass.ApplicationRole:

                        urnTypeString = "ApplicationRole";
                        result = true;
                        break;

                    case ObjectClass.Certificate:

                        urnTypeString = "Certificate";
                        result = true;
                        break;

                    case ObjectClass.Database:

                        urnTypeString = "Database";
                        result = true;
                        break;

                    case ObjectClass.DatabaseRole:

                        urnTypeString = "Role";
                        result = true;
                        break;

                    case ObjectClass.Endpoint:

                        urnTypeString = "Endpoint";
                        result = true;
                        break;

                    case ObjectClass.ExternalDataSource:
                        urnTypeString = "ExternalDataSource";
                        result = true;
                        break;

                    case ObjectClass.ExternalFileFormat:
                        urnTypeString = "ExternalFileFormat";
                        result = true;
                        break;

                    case ObjectClass.FullTextCatalog:

                        urnTypeString = "FullTextCatalog";
                        result = true;
                        break;

                    case ObjectClass.Schema:

                        urnTypeString = "Schema";
                        result = true;
                        break;

                    case ObjectClass.Server:

                        urnTypeString = "Server";
                        result = true;
                        break;

                    case ObjectClass.SqlAssembly:

                        urnTypeString = "SqlAssembly";
                        result = true;
                        break;

                    case ObjectClass.SymmetricKey:

                        urnTypeString = "SymmetricKey";
                        result = true;
                        break;

                    case ObjectClass.User:

                        urnTypeString = "User";
                        result = true;
                        break;

                    case ObjectClass.UserDefinedType:

                        if (isTableType)
                        {
                            urnTypeString = "UserDefinedTableType";
                        }
                        else
                        {
                            urnTypeString = "UserDefinedDataType";
                        }
                        result = true;
                        break;

                    case ObjectClass.XmlNamespace:

                        urnTypeString = "XmlSchemaCollection";
                        result = true;
                        break;

                    case ObjectClass.Login:

                        urnTypeString = "Login";
                        result = true;
                        break;

                    case ObjectClass.ServerRole:

                        urnTypeString = "Role";
                        result = true;
                        break;

                    case ObjectClass.AvailabilityGroup:

                        urnTypeString = "AvailabilityGroup";
                        result = true;
                        break;

                    default:

                        urnTypeString = String.Empty;
                        result = false;
                        break;
                }

                return result;
            }

            /// <summary>
            /// Get the URN substring filtering for object type
            /// </summary>
            /// <param name="objectType">The sys.objects type string of the object</param>
            /// <param name="urnTypeString">The substring that is formed</param>
            /// <returns>True if the substring could be formed, false otherwise</returns>
            private static bool GetUrnTypeString(string objectType, out string urnTypeString)
            {
                bool result = false;

                switch (objectType)
                {
                    case "AF":

                        urnTypeString = "UserDefinedAggregate";
                        result = true;
                        break;

                    case "P":
                    case "PC":
                    case "RF":

                        urnTypeString = "StoredProcedure";
                        result = true;
                        break;

                    case "FN":
                    case "IF":
                    case "TF":
                    case "FS":
                    case "FT":

                        urnTypeString = "UserDefinedFunction";
                        result = true;
                        break;

                    case "SQ":

                        urnTypeString = "ServiceBroker/ServiceQueue";
                        result = true;
                        break;

                    case "SN":

                        urnTypeString = "Synonym";
                        result = true;
                        break;

                    case "SO":

                        urnTypeString = "Sequence";
                        result = true;
                        break;

                    case "U":

                        urnTypeString = "Table";
                        result = true;
                        break;

                    case "V":

                        urnTypeString = "View";
                        result = true;
                        break;

                    case "X":

                        urnTypeString = "ExtendedStoredProcedure";
                        result = true;
                        break;

                    default:

                        // object must be one of the types that we aren't showing in the UI
                        urnTypeString = String.Empty;
                        result = false;
                        break;
                }

                return result;
            }

            /// <summary>
            /// Get the version of the server on the other end of the connection
            /// </summary>
            /// <param name="connectionInfo">Connection information to connect to the server</param>
            /// <returns>The server's version</returns>
            public static Version GetServerVersion(object connectionInfo)
            {
                Version result;

                // enumerate granted/denied permissions for the principal
                string urn = "Server/Information";
                string[] fields = new string[] { "VersionMajor", "VersionMinor", "BuildNumber" };
                Request request = new Request(new Urn(urn), fields);
                DataTable table = new Enumerator().Process(connectionInfo, request);

                if (1 == table.Rows.Count)
                {
                    int major = (int)table.Rows[0][0];
                    int minor = (int)table.Rows[0][1];
                    int build = (int)table.Rows[0][2];

                    result = new Version(major, minor, build);
                }
                else
                {
                    // STrace.Assert(false, "couldn't get server version, defaulting to YUKON");

                    result = new Version(9, 0, 0);
                }

                return result;
            }

            public static Microsoft.SqlServer.Management.Common.DatabaseEngineType GetDatabaseEngineType(object connectionInfo)
            {
                Microsoft.SqlServer.Management.Common.DatabaseEngineType result;

                // enumerate granted/denied permissions for the principal
                string urn = "Server";
                string[] fields = new string[] { "ServerType" };
                Request request = new Request(new Urn(urn), fields);
                DataTable table = new Enumerator().Process(connectionInfo, request);

                if (1 == table.Rows.Count)
                {
                    result = (Microsoft.SqlServer.Management.Common.DatabaseEngineType)table.Rows[0][0];
                }
                else
                {
                    // STrace.Assert(false, "couldn't get server type, defaulting to Standalone");

            result = Microsoft.SqlServer.Management.Common.DatabaseEngineType.Standalone;
                }

                return result;
            }

            /// <summary>
            /// Get the database engine edition value.
            /// </summary>
            /// <param name="connectionInfo">The connection information.</param>
            /// <returns>The database engine edition value.</returns>
            public static Microsoft.SqlServer.Management.Common.DatabaseEngineEdition GetDatabaseEngineEdition(object connectionInfo)
            {
                return Microsoft.SqlServer.Management.Sdk.Sfc.ExecuteSql.GetDatabaseEngineEdition(connectionInfo);
            }
        }

        /// <summary>
        /// delegate for Securable.OnPermissionStateChanged events
        /// </summary>
        internal delegate void SecurableChangedEventHandler(bool permissionsChangesExist);

        #region Securable Collections

        /// <summary>
        /// Key class for SecurableList and SecurableDictionary
        /// </summary>
        internal class SecurableKey : IComparable
        {
            private string name;
            private string schema;
            private int securableType;
            private bool changesExist;

            internal string Name
            {
                get
                {
                    return this.name;
                }
            }

            internal string Schema
            {
                get
                {
                    return this.schema;
                }
            }

            internal int SecurableType
            {
                get
                {
                    return this.securableType;
                }
            }

            internal bool ChangesExist
            {
                get
                {
                    return this.changesExist;
                }
            }

            internal SecurableKey(Securable securable)
            {
                this.name = securable.Name;
                this.schema = securable.Schema;
                this.securableType = (int)securable.SecurableType;
                this.changesExist = securable.ChangesExist;
            }

            internal SecurableKey(SearchableObject searchable)
            {
                this.name = searchable.Name;
                this.schema = searchable.Schema;
                this.securableType = (int)Securable.GetSecurableType(searchable.SearchableObjectType);
                this.changesExist = false;
            }

            internal SecurableKey(string schema, string name, SecurableType type)
            {
                this.name = name;
                this.schema = schema;
                this.securableType = (int)type;
                this.changesExist = false;
            }

            internal SecurableKey(string schema, string name, SecurableType type, bool changesExist)
            {
                this.name = name;
                this.schema = schema;
                this.securableType = (int)type;
                this.changesExist = changesExist;
            }


            public override int GetHashCode()
            {
                string fullname = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}.{2}", this.schema, this.name, this.securableType);
                return fullname.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                bool result = false;

                SecurableKey other = obj as SecurableKey;

                if (other != null)
                {
                    result = (this.CompareTo(other) == 0);
                }

                return result;
            }


            #region IComparable Members

            public int CompareTo(object obj)
            {
                int result = 0;
                SecurableKey other = (SecurableKey)obj;

                result = String.Compare(this.schema, other.schema, StringComparison.Ordinal);

                if (0 == result)
                {
                    result = String.Compare(this.name, other.name, StringComparison.Ordinal);
                }

                if (0 == result)
                {
                    if (this.securableType < other.securableType)
                    {
                        result = -1;
                    }
                    else if (other.securableType < this.securableType)
                    {
                        result = 1;
                    }
                }

                return result;
            }

            #endregion
        }

        /// <summary>
        /// A collection of Securables, with sorted list-like behavior.  Inserts, Deletes, and Contains are O(lg N).
        /// </summary>
        internal class SecurableList : ICollection
        {
            private SortedList data;

            public SecurableList()
            {
                this.data = new SortedList(new SecurableComparer(SecurableComparer.DefaultSortingOrder, true));
            }

            public SecurableList(SecurableComparer comparer)
            {
                this.data = new SortedList(comparer);
            }

            public SecurableList(SecurableList original, SecurableComparer comparer)
            {
                if (original.Count != 0)
                {
                    this.data = new SortedList(original.data, comparer);
                }
                else
                {
                    this.data = new SortedList(comparer);
                }
            }

            public SecurableList(SecurableDictionary dictionary, SecurableComparer comparer)
            {
                if (dictionary.Count != 0)
                {
                    this.data = new SortedList(dictionary.GetData(), comparer);
                }
                else
                {
                    this.data = new SortedList(comparer);
                }
            }


            internal IDictionary GetData()
            {
                return this.data;
            }


            #region IList Members

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public Securable this[int index]
            {
                get
                {
                    return (Securable)this.data.GetByIndex(index);
                }
                set
                {
                    this.data.SetByIndex(index, value);
                }
            }

            internal Securable this[string columnName]
            {
                get
                {
                    SecurableKey key = new SecurableKey(String.Empty, columnName, SecurableType.Column);
                    return (Securable)this.data[key];
                }

                set
                {
                    SecurableKey key = new SecurableKey(String.Empty, columnName, SecurableType.Column);
                    this.data[key] = value;
                }
            }

            internal Securable this[SecurableKey key]
            {
                get
                {
                    return (Securable)this.data[key];
                }

                set
                {
                    this.data[key] = value;
                }
            }

            public void RemoveAt(int index)
            {
                this.data.RemoveAt(index);
            }

            public void Add(Securable value)
            {
                this.data.Add(new SecurableKey(value), value);
            }

            public void Remove(Securable value)
            {
                this.data.Remove(new SecurableKey(value));
            }

            public void Remove(SearchableObject value)
            {
                this.data.Remove(new SecurableKey(value));
            }

            public bool Contains(Securable value)
            {
                return this.data.Contains(new SecurableKey(value));
            }

            public bool Contains(SearchableObject value)
            {
                return this.data.Contains(new SecurableKey(value));
            }

            public void Clear()
            {
                this.data.Clear();
            }

            public int IndexOf(Securable value)
            {
                return this.data.IndexOfKey(new SecurableKey(value));
            }

            public int IndexOf(SearchableObject value)
            {
                return this.data.IndexOfKey(new SecurableKey(value));
            }

            public bool IsFixedSize
            {
                get
                {
                    return false;
                }
            }


            #endregion

            #region ICollection Members

            public bool IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            public int Count
            {
                get
                {
                    return this.data.Count;
                }
            }

            public void CopyTo(Array array, int index)
            {
                for (int i = 0; i < this.data.Count; ++i)
                {
                    array.SetValue(this.data.GetByIndex(i), index + i);
                }
            }

            public object SyncRoot
            {
                get
                {
                    return this;
                }
            }


            #endregion

            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                return this.data.Values.GetEnumerator();
            }


            #endregion
        }

        enum SecurableSortType
        {
            Schema,
            Name,
            Type,
            Status
        }



        /// <summary>
        /// Comparer for securable keys
        /// </summary>
        internal class SecurableComparer : IComparer
        {
            internal static SecurableSortType[] DefaultSortingOrder =
            {
                SecurableSortType.Schema,
                SecurableSortType.Name,
                SecurableSortType.Type,
                SecurableSortType.Status
            };

            private SecurableSortType[] sortOrder;
            private bool ascending;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="sortOrder">Indicate which column to sort</param>
            /// <param name="ascending">Whether to sort in ascending order</param>
            public SecurableComparer(SecurableSortType[] sortOrder, bool ascending)
            {
                this.sortOrder = sortOrder;
                this.ascending = ascending;
            }

            /// <summary>
            /// Compare two securable keys
            /// </summary>
            /// <param name="x">The first securable key</param>
            /// <param name="y">The second securable key</param>
            /// <returns>-1 if first is less than second, 0 if first equals second, 1 if second is less than first</returns>
            public int Compare(object x, object y)
            {
                int result = 0;

                if (x == null)
                {
                    result = -1;
                }
                else if (y == null)
                {
                    result = 1;
                }
                else
                {
                    SecurableKey securableKey1;
                    SecurableKey securableKey2;

                    // if not ascending sort, reverse the roles of x and y
                    if (this.ascending)
                    {
                        securableKey1 = (SecurableKey)x;
                        securableKey2 = (SecurableKey)y;
                    }
                    else
                    {
                        securableKey1 = (SecurableKey)y;
                        securableKey2 = (SecurableKey)x;
                    }

                    int i = 0;

                    while (0 == result && i < this.sortOrder.Length)
                    {
                        switch (this.sortOrder[i])
                        {
                            case SecurableSortType.Name:

                                result = String.Compare(
                                    securableKey1.Name,
                                    securableKey2.Name,
                                    StringComparison.CurrentCulture);
                                break;

                            case SecurableSortType.Schema:

                                result = String.Compare(
                                    securableKey1.Schema,
                                    securableKey2.Schema,
                                    StringComparison.CurrentCulture);
                                break;

                            case SecurableSortType.Type:

                                result = securableKey1.SecurableType - securableKey2.SecurableType;
                                break;

                            case SecurableSortType.Status:

                                if (!securableKey1.ChangesExist && securableKey2.ChangesExist)
                                {
                                    result = -1;
                                }
                                else if (securableKey1.ChangesExist && !securableKey2.ChangesExist)
                                {
                                    result = 1;
                                }
                                break;

                            default:
                                // STrace.Assert(true, "Unknown SecurableSortType");
                                result = 0;
                                break;
                        }

                        ++i;
                    }
                }

                return result;
            }

        }

        /// <summary>
        /// A collection of Securables, with dictionary-like behavior.  Inserts, Deletes, and Contains are O(1).
        /// </summary>
        internal class SecurableDictionary : ICollection
        {
            private Hashtable data;

            public SecurableDictionary()
            {
                this.data = new Hashtable();
            }

            public SecurableDictionary(SecurableList list)
            {
                if (list.Count != 0)
                {
                    this.data = new Hashtable(list.GetData());
                }
                else
                {
                    this.data = new Hashtable();
                }
            }

            internal IDictionary GetData()
            {
                return this.data;
            }

            internal void UpdateKey(Securable securable)
            {
                this.data.Remove(new SecurableKey(securable.Schema, securable.Name, securable.SecurableType));
                this.data.Add(new SecurableKey(securable), securable);
            }


            #region IDictionary Members

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public IDictionaryEnumerator GetDictionaryEnumerator()
            {
                return this.data.GetEnumerator();
            }

            public Securable this[SecurableKey key]
            {
                get
                {
                    return (Securable)this.data[key];
                }

                set
                {
                    this.data[key] = value;
                }
            }

            public Securable this[Securable key]
            {
                get
                {
                    SecurableKey securableKey = new SecurableKey(key);
                    return (Securable)this.data[securableKey];
                }

                set
                {
                    SecurableKey securableKey = new SecurableKey(key);
                    this.data[securableKey] = value;
                }
            }

            public Securable this[SearchableObject key]
            {
                get
                {
                    SecurableKey securableKey = new SecurableKey(key);
                    return (Securable)this.data[securableKey];
                }

                set
                {
                    SecurableKey securableKey = new SecurableKey(key);
                    this.data[securableKey] = value;
                }
            }

            public void Remove(Securable key)
            {
                this.data.Remove(new SecurableKey(key));
            }

            public bool Contains(SecurableKey key)
            {
                return this.data.Contains(key);
            }

            public bool Contains(Securable key)
            {
                return this.data.Contains(new SecurableKey(key));
            }

            public bool Contains(SearchableObject key)
            {
                return this.data.Contains(new SecurableKey(key));
            }

            public void Clear()
            {
                this.data.Clear();
            }

            public ICollection Values
            {
                get
                {
                    return this.data.Values;
                }
            }

            public void Add(Securable securable)
            {
                this.data.Add(new SecurableKey(securable), securable);
            }

            public ICollection Keys
            {
                get
                {
                    return this.data.Keys;
                }
            }

            public bool IsFixedSize
            {
                get
                {
                    return false;
                }
            }

            #endregion

            #region ICollection Members

            public bool IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            public int Count
            {
                get
                {
                    return this.data.Count;
                }
            }

            public void CopyTo(Array array, int index)
            {
                this.data.Values.CopyTo(array, index);
            }

            public object SyncRoot
            {
                get
                {
                    return this;
                }
            }

            #endregion

            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                return this.data.Values.GetEnumerator();
            }


            #endregion

        }

        #endregion

        #region Securable Subclasses


        /// <summary>
        /// Securable class for tables, views, inline UDF's, and table-valued UDF's
        /// </summary>
        internal class SecurableColumnParent : Securable
        {
            private Principal currentPrincipal;
            /// <summary>
            /// Constructor for existing tables, views, or table valued functions
            /// </summary>
            /// <param name="smoObject">The SMO object we are representing</param>
            /// <param name="type">The SecurableType for the SMO object</param>
            /// <param name="connectionInfo">Connection information for enumerating permissions data</param>
            /// <param name="serverVersion">The version of the server</param>
            public SecurableColumnParent(SqlSmoObject smoObject, SecurableType type, object connectionInfo, Version serverVersion)
                : base(smoObject, type, connectionInfo, serverVersion)
            {
                // STrace.Assert((smoObject is TableViewBase) || (smoObject is UserDefinedFunction), "smoObject is not a table, view, or UDF");
                // STrace.Assert(
                            //  (SecurableType.Table == type) ||
                            //  (SecurableType.View == type) ||
                            //  (SecurableType.FunctionInline == type) ||
                            //  (SecurableType.FunctionTable == type),
                            //  "type is not table, view, or table-valued UDF");
                CurrentPrincipal = null;
            }

            /// <summary>
            /// Constructor for new tables, views, or table valued functions, or existing tables, views, or
            /// table valued functions that have been enumerated or searched for.
            /// </summary>
            /// <param name="type">The securable type of the object</param>
            /// <param name="urn">The URN of the object</param>
            /// <param name="connectionInfo">Connection information for enumerating permissions data</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <param name="exists"></param>
            public SecurableColumnParent(SecurableType type, string urn, object connectionInfo, Version serverVersion, bool exists)
                : base(type, urn, connectionInfo, serverVersion, exists)
            {
                // STrace.Assert(
                            //  (SecurableType.Table == type) ||
                            //  (SecurableType.View == type) ||
                            //  (SecurableType.FunctionInline == type) ||
                            //  (SecurableType.FunctionTable == type),
                            //  "type is not table, view, or table-valued UDF");
                CurrentPrincipal = null;
            }


            /// <summary>
            /// Apply grants and denies to the table/view/function and its columns
            /// </summary>
            /// <param name="obj"></param>
            internal override void ApplyGrantsAndDenies(SqlSmoObject obj)
            {
                base.ApplyGrantsAndDenies(obj);

                if (obj is TableViewBase)
                {
                    TableViewBase table = (TableViewBase)obj;

                    for (int columnIndex = 0; columnIndex < table.Columns.Count; ++columnIndex)
                    {
                        Column column = table.Columns[columnIndex];
                        Securable child = this.Children[column.Name];

                        // STrace.Assert(child != null, "child not found for column, permissions control is out of sync with SMO object state");

                        // VSTS 1458762 - avoid duplicate if this child is already in current principal's list.
                        if (CurrentPrincipal != null && CurrentPrincipal.SecurableToPermissionStatesContains(child))
                        {
                            continue;
                        }

                        if (child != null)
                        {
                            child.ApplyGrantsAndDenies(column);
                        }
                    }
                }
                else if (obj is UserDefinedFunction)
                {
                    UserDefinedFunction function = (UserDefinedFunction)obj;

                    for (int columnIndex = 0; columnIndex < function.Columns.Count; ++columnIndex)
                    {
                        Column column = function.Columns[columnIndex];
                        Securable child = this.Children[column.Name];

                        // STrace.Assert(child != null, "child not found for column, permissions control is out of sync with SMO object state");

                        // VSTS 1458762 - avoid duplicate if this child is already in current principal's list.
                        if (CurrentPrincipal != null && CurrentPrincipal.SecurableToPermissionStatesContains(child))
                        {
                            continue;
                        }

                        if (child != null)
                        {
                            child.ApplyGrantsAndDenies(column);
                        }
                    }
                }
#if DEBUG
                else
                {
                    // STrace.Assert(false, "unexpected object type - obj is not a table, view, or UDF");
                }
#endif
            }

            /// <summary>
            /// Apply revokes to the table/view/function and its columns
            /// </summary>
            /// <param name="obj"></param>
            internal override void ApplyRevokes(SqlSmoObject obj)
            {
                base.ApplyRevokes(obj);

                if (obj is TableViewBase)
                {
                    TableViewBase table = (TableViewBase)obj;

                    for (int columnIndex = 0; columnIndex < table.Columns.Count; ++columnIndex)
                    {
                        Column column = table.Columns[columnIndex];
                        Securable child = this.Children[column.Name];

                        // STrace.Assert(child != null, "child not found for column, permissions control is out of sync with SMO object state");

                        // VSTS 1458762 - avoid duplicate if this child is already in current principal's list.
                        if (CurrentPrincipal != null && CurrentPrincipal.SecurableToPermissionStatesContains(child))
                        {
                            continue;
                        }

                        if (child != null)
                        {
                            child.ApplyRevokes(column);
                        }
                    }
                }
                else if (obj is UserDefinedFunction)
                {
                    UserDefinedFunction function = (UserDefinedFunction)obj;

                    for (int columnIndex = 0; columnIndex < function.Columns.Count; ++columnIndex)
                    {
                        Column column = function.Columns[columnIndex];
                        Securable child = this.Children[column.Name];

                        // STrace.Assert(child != null, "child not found for column, permissions control is out of sync with SMO object state");

                        // VSTS 1458762 - avoid duplicate if this child is already in current principal's list.
                        if (CurrentPrincipal != null && CurrentPrincipal.SecurableToPermissionStatesContains(child))
                        {
                            continue;
                        }

                        if (child != null)
                        {
                            child.ApplyRevokes(column);
                        }
                    }
                }
#if DEBUG
                else
                {
                    // STrace.Assert(false, "unexpected object type - obj is not a table, view, or UDF");
                }
#endif
            }

            /// <summary>
            /// Get a collection of child columns
            /// </summary>
            /// <returns>The columns collection</returns>
            protected override SecurableList GetChildren()
            {
                SecurableList result = new SecurableList();

                if (this.Exists)
                {
                    SecurableType childType = (SecurableType.FunctionTable == this.SecurableType) ?
                                              SecurableType.ColumnFunctionTable :
                                              SecurableType.Column;

                    string columnsUrn = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                      "{0}/Column",
                                                      this.Urn);
                    Enumerator enumerator = new Enumerator();
                    Request request = new Request();
                    request.Urn = new Urn(columnsUrn);
                    request.Fields = new string[] { "Urn" };

                    DataTable columns = enumerator.Process(this.ConnectionInfo, request);

                    for (int columnIndex = 0; columnIndex < columns.Rows.Count; ++columnIndex)
                    {
                        DataRow columnData = columns.Rows[columnIndex];
                        string columnUrn = columnData["Urn"].ToString();
                        Securable column = Securable.AllocateSecurable(childType,
                                                                       columnUrn,
                                                                       this.ConnectionInfo,
                                                                       this.ServerVersion);

                        // STrace.Assert(!result.Contains(column), "column is already in the collection");

                        // set the column's parent
                        column.Parent = this;

                        // the column should have all the same principals as its parent
                        IEnumerator principalEnumerator = this.Principals.GetEnumerator();
                        principalEnumerator.Reset();

                        while (principalEnumerator.MoveNext())
                        {
                            Principal principal = (Principal)principalEnumerator.Current;
                            column.AddPrincipal(principal);
                        }

                        // add the column to the result collection
                        result.Add(column);
                    }
                }

                return result;
            }

            internal Principal CurrentPrincipal
            {
                get
                {
                    return this.currentPrincipal;
                }
                set
                {
                    this.currentPrincipal = value;
                }
            }
        }


        #endregion


        /// <summary>
        /// A security principal
        /// </summary>
        internal class Principal
        {
            private string name = String.Empty;
            private string databaseName = String.Empty;
            private string serverName = String.Empty;
            private PrincipalType principalType;
            private object connectionInfo = null;
            /// <summary>
            /// Map from Securable to PermissionStateCollection
            /// </summary>
            private HybridDictionary securableToPermissionStates = null;
            private SecurableDictionary securables = null;
            private SecurableDictionary removedSecurables = null;
            private bool isRemoved = false;
            private bool exists = false;
            private Version serverVersion;
            private DatabaseEngineType databaseEngineType;
            private DatabaseEngineEdition databaseEngineEdition;
            private event EventHandler observableChanged;

            private const int YUKON = 9;
            private const int SHILOH = 8;

            /// <summary>
            /// The name of the principal
            /// </summary>
            public string Name
            {
                get
                {
                    return this.name;
                }

                set
                {
                    this.name = value;
                }
            }

            /// <summary>
            /// Get the type of the principal
            /// </summary>
            public PrincipalType PrincipalType
            {
                get
                {
                    return this.principalType;
                }
            }

            /// <summary>
            /// Get the searchable object type of the principal
            /// </summary>
            public SearchableObjectType SearchableObjectType
            {
                get
                {
                    return Principal.GetSearchableObjectType(this.principalType);
                }
            }

            /// <summary>
            /// The localized display name of the type of the securable
            /// </summary>
            public string TypeName
            {
                get
                {
                    SearchableObjectTypeDescription description =
                        SearchableObjectTypeDescription.GetDescription(this.SearchableObjectType);

                    return description.DisplayTypeNameSingular;
                }
            }

            /// <summary>
            /// The name of the database containing the principal, or String.Empty if the principal is a Login or Server Role
            /// </summary>
            public string DatabaseName
            {
                get
                {
                    return this.databaseName;
                }
            }

            /// <summary>
            /// The name of the server containing the principal
            /// </summary>
            public string ServerName
            {
                get
                {
                    if (0 == this.serverName.Length)
                    {
                        // STrace.Assert(this.connectionInfo != null, "connection info is null");

                        Enumerator enumerator = new Enumerator();
                        Urn urn = new Urn("Server");
                        string[] fields = new string[] { "Name" };
                        Request request = new Request(urn, fields);
                        DataTable results = enumerator.Process(this.connectionInfo, request);

                        // STrace.Assert(0 < results.Rows.Count, "Enumerator for server name did not return any rows");

                        if (0 < results.Rows.Count)
                        {
                            this.serverName = results.Rows[0]["Name"].ToString();
                        }
                    }

                    return this.serverName;
                }
            }

            /// <summary>
            /// Whether the principal has been removed
            /// </summary>
            public bool IsRemoved
            {
                get
                {
                    return this.isRemoved;
                }

                set
                {
                    this.isRemoved = true;
                }
            }

            /// <summary>
            /// Whether the principal exists on the server
            /// </summary>
            public bool Exists
            {
                get
                {
                    return this.exists;
                }
            }

            /// <summary>
            /// Are there permissions changes to persist to the server
            /// </summary>
            public bool ChangesExist
            {
                get
                {
                    bool result = (this.removedSecurables.Count != 0);

                    if (!result)
                    {
                        ICollection permissionStateCollections = this.securableToPermissionStates.Values;
                        IEnumerator permissionStateCollectionsEnumerator = permissionStateCollections.GetEnumerator();

                        permissionStateCollectionsEnumerator.Reset();

                        while (!result && permissionStateCollectionsEnumerator.MoveNext())
                        {
                            PermissionStateCollection permissionStates = (PermissionStateCollection)permissionStateCollectionsEnumerator.Current;
                            IEnumerator permissionStateEnumerator = permissionStates.GetEnumerator();

                            permissionStateEnumerator.Reset();

                            while (!result && permissionStateEnumerator.MoveNext())
                            {
                                PermissionState permissionState = (PermissionState)permissionStateEnumerator.Current;
                                result = permissionState.StateChanged;
                            }
                        }
                    }

                    return result;
                }
            }

            /// <summary>
            /// Whether the principal is a database principal
            /// </summary>
            public bool IsDatabasePrincipal
            {
                get
                {
                    bool result =
                    (PrincipalType.User == this.principalType) ||
                    (PrincipalType.DatabaseRole == this.principalType) ||
                    (PrincipalType.ApplicationRole == this.principalType);

                    return result;
                }
            }

            /// <summary>
            /// The version of the server
            /// </summary>
            public Version ServerVersion
            {
                get
                {
                    return this.serverVersion;
                }
            }

            /// <summary>
            /// The <see cref="DatabaseEngineType"/> of the current database
            /// </summary>
            public DatabaseEngineType DatabaseEngineType
            {
                get
                {
                    return this.databaseEngineType;
                }
            }

            /// <summary>
            /// The <see cref="DatabaseEngineEdition"/> of the current database
            /// </summary>
            public DatabaseEngineEdition DatabaseEngineEdition
            {
                get
                {
                    return this.databaseEngineEdition;
                }
            }

            /// <summary>
            /// The types of objects on which this principal can have permissions
            /// </summary>
            public ArrayList RelevantSecurableTypes
            {
                get
                {
                    ArrayList result = new ArrayList();

                    if (this.IsDatabasePrincipal)
                    {
                        result.Add(SecurableType.Database);
                        result.Add(SecurableType.StoredProcedure);
                        result.Add(SecurableType.Table);
                        result.Add(SecurableType.View);

                        if (this.databaseName == "master")
                        {
                            result.Add(SecurableType.ExtendedStoredProcedure);
                        }

                        if (SHILOH <= this.serverVersion.Major)
                        {
                            result.Add(SecurableType.FunctionInline);
                            result.Add(SecurableType.FunctionScalar);
                            result.Add(SecurableType.FunctionTable);
                        }

                        if (YUKON <= this.serverVersion.Major)
                        {
                            result.Add(SecurableType.ApplicationRole);
                            result.Add(SecurableType.Assembly);
                            result.Add(SecurableType.AsymmetricKey);
                            result.Add(SecurableType.Certificate);
                            result.Add(SecurableType.DatabaseRole);
                            result.Add(SecurableType.AggregateFunction);
                            result.Add(SecurableType.FullTextCatalog);
                            result.Add(SecurableType.ServiceQueue); //Display name is Queue.
                            result.Add(SecurableType.Schema);
                            result.Add(SecurableType.SymmetricKey);
                            result.Add(SecurableType.Synonym);
                            result.Add(SecurableType.User);
                            result.Add(SecurableType.UserDefinedDataType);
                            result.Add(SecurableType.XmlSchemaCollection);
                        }

                        if (Utils.IsKatmaiOrLater(this.ServerVersion.Major))
                        {
                            result.Add(SecurableType.UserDefinedTableType);
                        }
                        if (Utils.IsSql11OrLater(this.ServerVersion.Major))
                        {
                            result.Add(SecurableType.Sequence);
                        }
                        if (Utils.IsSql13OrLater(this.serverVersion.Major))
                        {
                            result.Add(SecurableType.SecurityPolicy);
                            result.Add(SecurableType.ExternalDataSource);
                            result.Add(SecurableType.ExternalFileFormat);
                        }
                    }
                    else
                    {
                        if (YUKON <= this.serverVersion.Major)
                        {
                            result.Add(SecurableType.Endpoint);
                            result.Add(SecurableType.Login);
                        }
                        result.Add(SecurableType.Server);

                        if (Utils.IsSql11OrLater(this.serverVersion.Major))
                        {
                            result.Add(SecurableType.AvailabilityGroup);
                            result.Add(SecurableType.ServerRole);
                        }
                    }

                    return result;
                }
            }


            /// <summary>
            /// Constructor for existing principals.
            /// </summary>
            /// <param name="principal">The SMO principal we are abstracting</param>
            /// <param name="connectionInfo">Connection info that the enumerator is to use</param>
            public Principal(SqlSmoObject principal, object connectionInfo)
            {
                NamedSmoObject principalAsNamedSmoObject = principal as NamedSmoObject;
                if (principalAsNamedSmoObject != null)
                {
                    this.name = principalAsNamedSmoObject.Name;
                }
                else
                {
                    // STrace.Assert(false, "principal must be a NamedSmoObject!");
                    // STrace.LogExThrow();
                    throw new ArgumentException("principal");
                }
                this.principalType = Principal.GetPrincipalType(principal);
                this.connectionInfo = connectionInfo;
                this.exists = (SqlSmoState.Existing == principal.State);
                this.serverVersion = Securable.GetServerVersion(connectionInfo);
                this.databaseEngineType = Securable.GetDatabaseEngineType(connectionInfo);
                this.databaseEngineEdition = Securable.GetDatabaseEngineEdition(connectionInfo);
                if (this.IsDatabasePrincipal)
                {
                    this.databaseName = principal.Urn.Parent.GetAttribute("Name");
                }

                this.Initialize();
            }

            /// <summary>
            /// Constructor for server principals that are being created or existing principals that have been enumerated.
            /// <param name="name">The name of the principal</param>
            /// <param name="principalType">The type of the principal</param>
            /// <param name="exists">Whether the principal exists on the server</param>
            /// <param name="connectionInfo">Connection info that the enumerator is to use</param>
            /// <param name="serverVersion">The version of the server</param>
            /// </summary>
            public Principal(string name,
                             PrincipalType principalType,
                             bool exists,
                             object connectionInfo,
                             Version serverVersion)
            {
                this.name = name;
                this.principalType = principalType;
                this.connectionInfo = connectionInfo;
                this.exists = exists;
                this.serverVersion = serverVersion;

                this.Initialize();
            }

            /// <summary>
            /// Constructor for database principals that are being created or existing principals that have been enumerated.
            /// </summary>
            /// <param name="name">The name of the principal</param>
            /// <param name="database">The name of the database containing the principal</param>
            /// <param name="principalType">The type of the principal</param>
            /// <param name="exists">Whether the principal exists on the server</param>
            /// <param name="connectionInfo">Connection info that the enumerator is to use</param>
            /// <param name="serverVersion">The version of the server</param>
            public Principal(string name, string database, PrincipalType principalType, bool exists, object connectionInfo, Version serverVersion)
            {
                this.name = name;
                this.databaseName = database;
                this.principalType = principalType;
                this.connectionInfo = connectionInfo;
                this.exists = exists;
                this.serverVersion = serverVersion;

                // STrace.Assert(this.IsDatabasePrincipal, "wrong constructor for a server principal");

                this.Initialize();
            }

            /// <summary>
            /// Constructor for database principals that are being created or existing principals that have been enumerated.
            /// </summary>
            /// <param name="name">The name of the principal</param>
            /// <param name="database">The name of the database containing the principal</param>
            /// <param name="principalType">The type of the principal</param>
            /// <param name="exists">Whether the principal exists on the server</param>
            /// <param name="connectionInfo">Connection info that the enumerator is to use</param>
            /// <param name="serverVersion">The version of the server</param>
            /// <param name="databaseEngineType"></param>
            /// <param name="databaseEngineEdition"></param>
            internal Principal(string name, string database, PrincipalType principalType, bool exists, object connectionInfo, Version serverVersion, DatabaseEngineType databaseEngineType, DatabaseEngineEdition databaseEngineEdition)
            {
                this.name = name;
                this.databaseName = database;
                this.principalType = principalType;
                this.connectionInfo = connectionInfo;
                this.exists = exists;
                this.serverVersion = serverVersion;
                this.databaseEngineType = databaseEngineType;
                this.databaseEngineEdition = databaseEngineEdition;
                // STrace.Assert(this.IsDatabasePrincipal, "wrong constructor for a server principal");

                this.Initialize();
            }

            /// <summary>
            /// Constructor for principals that have been searched for and added.
            /// </summary>
            /// <param name="searchableObject">The search result identifying the principal</param>
            /// <param name="connectionInfo">Connection info that the enumerator is to use</param>
            /// <param name="serverVersion">The version of the server</param>
            internal Principal(SearchableObject searchableObject, object connectionInfo, Version serverVersion)
            {
                this.name = searchableObject.Name;
                this.principalType = Principal.GetPrincipalType(searchableObject.SearchableObjectType);
                this.connectionInfo = connectionInfo;
                this.exists = true;
                this.serverVersion = serverVersion;

                if (this.IsDatabasePrincipal)
                {
                    this.databaseName = searchableObject.Urn.Parent.GetAttribute("Name");
                }

                this.Initialize();
            }

            /// <summary>
            /// The list of Schema-Scoped SecurableTypes
            /// </summary>
            internal IEnumerable<SecurableType> SchemaTypes
            {
                get
                {
                    return Enum.GetValues(typeof(SecurableType))
                        .Cast<SecurableType>()
                        .Where(
                            t =>
                                t.IsValidSchemaBoundSecurable(
                                new ServerVersion(ServerVersion.Major, ServerVersion.Minor, ServerVersion.Build),
                                    this.DatabaseEngineEdition,
                                    this.DatabaseEngineType));
                }
            }

            internal bool SecurableToPermissionStatesContains(Securable securable)
            {
                return this.securableToPermissionStates.Contains(securable);
            }

            /// <summary>
            /// Commit permissions changes to the server
            /// </summary>
            /// <param name="principalName">The name of the principal for the changes</param>
            /// <param name="server">The SMO Server object on which permissions changes are to be committed</param>
            public void ApplyChanges(string principalName, Microsoft.SqlServer.Management.Smo.Server server)
            {
                this.name = principalName;

                // commit changes to non-removed securables
                // The only securables that could have been changed are the ones
                // that have been selected in the UI at some point, which are the
                // ones that are keys in the securableToPermissionStates collection.
                IDictionaryEnumerator securableCollectionEnumerator = this.securableToPermissionStates.GetEnumerator();
                securableCollectionEnumerator.Reset();

                while (securableCollectionEnumerator.MoveNext())
                {
                    Securable securable = (Securable)securableCollectionEnumerator.Key;

                    // STrace.Assert(securable.Urn != null, "Unexpected null URN. Securable should exist and existing securables should have a URN.");

                    SqlSmoObject smoSecurable = (SqlSmoObject)server.GetSmoObject(securable.Urn);
                    if (securable is SecurableColumnParent)
                    {
                        // VSTS 1458762 - Set the current principal for this SecurableColumnParent, will check the children
                        // against current principal's list later.
                        SecurableColumnParent securableColumnParent = (SecurableColumnParent)securable;
                        securableColumnParent.CurrentPrincipal = this;
                        securable.ApplyChanges(smoSecurable);
                        securableColumnParent.CurrentPrincipal = null; // clean up
                    }
                    else
                    {
                        securable.ApplyChanges(smoSecurable);
                    }
                }

                // commit changes to removed securables
                IEnumerator removedSecurablesEnumerator = this.removedSecurables.GetEnumerator();
                removedSecurablesEnumerator.Reset();

                while (removedSecurablesEnumerator.MoveNext())
                {
                    Securable removed = (Securable)removedSecurablesEnumerator.Current;
                    // STrace.Assert(removed.Urn != null, "Unexpected null URN. Removed securable should exist and existing securables should have a URN.");

                    SqlSmoObject smoSecurable = (SqlSmoObject)server.GetSmoObject(removed.Urn);
                    removed.ApplyChanges(smoSecurable);
                }
            }

            /// <summary>
            /// Abandon all changes and get new state data from the server
            /// </summary>
            public void Reset()
            {
                this.Initialize();
            }

            /// <summary>
            /// Get an ordered list of the securables that have been "added" to the Principal in the UI
            /// </summary>
            /// <param name="comparer">The SecurableComparer to use to sort the securables</param>
            /// <returns>The list of securables</returns>
            public SecurableList GetSecurables(SecurableComparer comparer)
            {
                return new SecurableList(this.securables, comparer);
            }

            /// <summary>
            /// This will update the SecurableKey with the right ChangesExist value
            /// </summary>
            /// <param name="securable"></param>
            public void UpdateSecurableKeyStatus(Securable securable)
            {
                this.securables.UpdateKey(securable);
            }

            /// <summary>
            /// Get the permission states relating this principal to the input securable
            /// </summary>
            /// <param name="securable">The securable for which we are getting permission states</param>
            /// <returns>The collection of permission states</returns>
            public PermissionStateCollection GetPermissionStates(Securable securable)
            {
                if (!this.securableToPermissionStates.Contains(securable))
                {
                    PermissionState.PopulatePermissionStates(securable, this);
                    PermissionState.AddChildrenToEmptyParents((PermissionStateCollection)this.securableToPermissionStates[securable]);
                }

                return (PermissionStateCollection)this.securableToPermissionStates[securable];
            }


            /// <summary>
            /// Add the set of securables related to this principal
            /// </summary>
            /// <returns>The set of securables</returns>
            public void AddExistingSecurables()
            {
                if (this.exists)
                {
                    // STrace.Assert(this.connectionInfo != null, "connectionInfo is not set");

                    string urn = this.IsDatabasePrincipal ?
                                 new Urn(
                                    String.Format(
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        "Server/Database[@Name='{0}']/LevelPermission[@Grantee='{1}']",
                                        Urn.EscapeString(this.databaseName),
                                        Urn.EscapeString(this.Name))) :
                                 new Urn(
                                    String.Format(
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        "Server/LevelPermission[@Grantee='{0}']",
                                        Urn.EscapeString(this.Name)));

                    this.AddRelatedSecurables(urn, this.securables, this.IsDatabasePrincipal);
                }
            }

            /// <summary>
            /// Add the securables in the input schema to the securables collection
            /// </summary>
            /// <param name="schemaName">The name of the schema that contains the securables</param>
            /// <returns>The set of securables</returns>
            public void AddAllSecurables(string schemaName)
            {
                // STrace.Assert(this.IsDatabasePrincipal, "Server objects don't have schemas");

                //Schema-bound Securables should always be under a Database - so there's
                //nothing to add if we aren't a database principal
                if (!this.IsDatabasePrincipal)
                {
                    return;
                }

                foreach (SecurableType securableType in this.SchemaTypes)
                {
                    this.AddSecurables(securableType, securableType.GetSchemaScopedUrn(schemaName, this.databaseName), this.securables);
                }
            }


            /// <summary>
            /// Add the set of securables whose types are in the list of securable types
            /// </summary>
            /// <param name="securableTypes">The securable types to return</param>
            /// <returns>The set of securables</returns>
            public void AddAllSecurables(ArrayList securableTypes)
            {
                foreach (SecurableType securableType in securableTypes)
                {
                    this.AddSecurables(securableType, this.GetSecurablesUrn(securableType), this.securables);
                }

            }

            /// <summary>
            /// Add a securable to the collection of related securables
            /// </summary>
            /// <param name="securable">The searchable object for the securable</param>
            /// <returns>The securable that was added</returns>
            public Securable AddSecurable(SearchableObject securable)
            {
                Securable result = null;

                if (!this.securables.Contains(securable))
                {
                    if (this.removedSecurables.Contains(securable))
                    {
                        result = this.removedSecurables[securable];
                        result.IsRemoved = false;
                        this.removedSecurables.Remove(result);
                    }
                    else
                    {
                        result = Securable.Create(securable, this.connectionInfo, this.serverVersion);
                    }

                    this.securables.Add(result);
                    result.Changed += this.OnChanged;
                }
                else
                {
                    result = this.securables[securable];
                }

                return result;
            }

            /// <summary>
            /// Remove a securable from the collection of related securables
            /// </summary>
            /// <param name="securable">The securable to remove</param>
            public void RemoveSecurable(Securable securable)
            {
                // STrace.Assert(this.securables.Contains(securable), "securable is not in the securables collection");

                if (this.HasExistingGrants(securable))
                {
                    // STrace.Assert(!this.removedSecurables.Contains(securable), "removedSecurables already contains the securable");
                    this.removedSecurables.Add(securable);
                    securable.IsRemoved = true;
                }

                this.securables.Remove(securable);
                this.securableToPermissionStates.Remove(securable);
            }

            /// <summary>
            /// Revoke all permissions that have been displayed in the UI.
            /// </summary>
            /// <remarks>
            /// This is called when one of the permissions on objects controls needs to
            /// revoke all permissions on the securable for this principal.  Any child
            /// permissions are revoked as well.
            /// </remarks>
            public void RevokeAll()
            {
                IEnumerator permissionStateCollectionEnumerator = this.securableToPermissionStates.Values.GetEnumerator();
                permissionStateCollectionEnumerator.Reset();

                while (permissionStateCollectionEnumerator.MoveNext())
                {
                    PermissionStateCollection permissionStateCollection = (PermissionStateCollection)permissionStateCollectionEnumerator.Current;
                    IEnumerator permissionStateEnumerator = permissionStateCollection.GetEnumerator();
                    permissionStateEnumerator.Reset();

                    while (permissionStateEnumerator.MoveNext())
                    {
                        PermissionState permissionState = (PermissionState)permissionStateEnumerator.Current;
                        permissionState.Revoke();
                    }
                }
            }

            /// <summary>
            /// Handle permission selection change events
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnChanged(object sender, EventArgs e)
            {
                this.NotifyObservers(sender, e);
            }

            /// <summary>
            /// Property to access the observable event.
            /// </summary>
            internal event EventHandler Changed
            {
                add { this.observableChanged += value; }
                remove { this.observableChanged -= value; }
            }

            /// <summary>
            /// Notify all observers that this object has changed.
            /// </summary>
            /// <param name="sender">The object that changed</param>
            /// <param name="e">Hint for the notification, usually null</param>
            private void NotifyObservers(object sender, EventArgs e)
            {
                if (this.observableChanged != null)
                {
                    this.observableChanged(sender, e);
                }
            }

            /// <summary>
            /// Notify all observers that this object or one of its children has changed.
            /// </summary>
            private void NotifyObservers()
            {
                this.NotifyObservers(this, new EventArgs());
            }


            /// <summary>
            /// Does this principal have any granted or denied permissions on the securable?
            /// </summary>
            /// <param name="securable">The securable we are checking for existing grants/denies</param>
            /// <returns>True if there are existing grants or denies, false if all all permissions are revoked</returns>
            internal bool HasExistingGrants(Securable securable)
            {
                bool result = false;
                PermissionStateCollection permissionStates = this.GetPermissionStates(securable);

                for (int i = 0; i < permissionStates.Count; ++i)
                {
                    PermissionState permissionState = permissionStates[i];

                    if (permissionState.OriginalState != PermissionStatus.Revoke)
                    {
                        result = true;
                        break;
                    }
                }

                return result;
            }

            /// <summary>
            /// Add a permission state to the collection of permission states
            /// </summary>
            /// <param name="permissionState">The permission state to add</param>
            internal void AddPermissionState(PermissionState permissionState)
            {
                PermissionStateCollection permissionStates = null;

                if (this.securableToPermissionStates.Contains(permissionState.Securable))
                {
                    permissionStates = (PermissionStateCollection)this.securableToPermissionStates[permissionState.Securable];
                }
                else
                {
                    permissionStates = new PermissionStateCollection();
                    this.securableToPermissionStates.Add(permissionState.Securable, permissionStates);
                }

                permissionStates.Add(permissionState);
            }

            /// <summary>
            /// Perform common initialization tasks
            /// </summary>
            private void Initialize()
            {
                this.securableToPermissionStates = new HybridDictionary();
                this.securables = new SecurableDictionary();
                this.removedSecurables = new SecurableDictionary();

                //If we don't have a DatabaseEngineType try to fetch it from our connection now
                if(this.DatabaseEngineType == DatabaseEngineType.Unknown)
                {
                    var conn = this.connectionInfo as SqlConnectionInfoWithConnection;
                    if(conn != null)
                    {
                        this.databaseEngineType = conn.ServerConnection.DatabaseEngineType;
                    }
                }
            }

            /// <summary>
            /// Get a urn to enumerate all objects of a particular type
            /// </summary>
            /// <param name="type">The type of objects to enumerate</param>
            /// <returns>The URN to enumerate the objects</returns>
            private string GetSecurablesUrn(SecurableType type)
            {
                StringBuilder result = new StringBuilder("Server");

                if (this.IsDatabasePrincipal)
                {
                    result.AppendFormat("/Database[@Name='{0}']", Urn.EscapeString(this.databaseName));
                }

                switch (type)
                {
                    case SecurableType.AggregateFunction:

                        result.Append("/UserDefinedAggregate");
                        break;

                    case SecurableType.ApplicationRole:

                        result.Append("/ApplicationRole");
                        break;

                    case SecurableType.Assembly:

                        result.Append("/SqlAssembly");
                        break;

                    case SecurableType.AsymmetricKey:

                        result.Append("/AsymmetricKey");
                        break;

                    case SecurableType.Certificate:

                        result.Append("/Certificate");
                        break;

                    case SecurableType.Database:

                        // no additional text needed
                        break;

                    case SecurableType.DatabaseRole:

                        result.Append("/Role");
                        break;

                    case SecurableType.Endpoint:

                        result.Append("/Endpoint");
                        break;

                    case SecurableType.ExtendedStoredProcedure:

                        result.Append("/ExtendedStoredProcedure");
                        break;

                    case SecurableType.ExternalDataSource:

                        result.Append("/ExternalDataSource");
                        break;

                    case SecurableType.ExternalFileFormat:

                        result.Append("/ExternalFileFormat");
                        break;

                    case SecurableType.FullTextCatalog:

                        result.Append("/FullTextCatalog");
                        break;

                    case SecurableType.FunctionInline:

                        result.AppendFormat("/UserDefinedFunction[@FunctionType='{0}']", ((int)UserDefinedFunctionType.Inline));
                        break;

                    case SecurableType.FunctionScalar:

                        result.AppendFormat("/UserDefinedFunction[@FunctionType='{0}']", ((int)UserDefinedFunctionType.Scalar));
                        break;

                    case SecurableType.FunctionTable:

                        result.AppendFormat("/UserDefinedFunction[@FunctionType='{0}']", ((int)UserDefinedFunctionType.Table));
                        break;

                    case SecurableType.Login:

                        result.Append("/Login");
                        break;

                    case SecurableType.ServerRole:

                        result.Append("/Role");
                        break;

                    case SecurableType.Schema:

                        result.Append("/Schema");
                        break;

                    case SecurableType.SecurityPolicy:

                        result.Append("/SecurityPolicy");
                        break;

                    case SecurableType.Server:

                        // no additional text needed
                        break;

                    case SecurableType.ServiceQueue:

                        result.Append("/ServiceBroker/ServiceQueue");
                        break;

                    case SecurableType.StoredProcedure:

                        result.Append("/StoredProcedure");
                        break;

                    case SecurableType.SymmetricKey:

                        result.Append("/SymmetricKey");
                        break;

                    case SecurableType.Synonym:

                        result.Append("/Synonym");
                        break;

                    case SecurableType.Sequence:

                        result.Append("/Sequence");
                        break;

                    case SecurableType.Table:

                        result.Append("/Table");
                        break;

                    case SecurableType.User:

                        result.Append("/User");
                        break;

                    case SecurableType.UserDefinedDataType:

                        result.Append("/UserDefinedDataType");
                        break;

                    case SecurableType.UserDefinedTableType:

                        result.Append("/UserDefinedTableType");
                        break;

                    case SecurableType.View:

                        result.Append("/View");
                        break;

                    case SecurableType.XmlSchemaCollection:

                        result.Append("/XmlSchemaCollection");
                        break;

                    case SecurableType.AvailabilityGroup:

                        result.Append("/AvailabilityGroup");
                        break;

                    default:

                        // STrace.Assert(false, "Unexpected securable type for this principal type");
                        throw new ArgumentException();
                }


                return result.ToString();
            }

            /// <summary>
            /// Enumerate related securables and put the securables into the input collection
            /// </summary>
            /// <param name="urn">The URN for enumerating the securables</param>
            /// <param name="relatedSecurables">The collection into which the securables are to be put</param>
            /// <param name="isDatabasePrincipal">for database principals, include isTableType in the query fields</param>
            private void AddRelatedSecurables(string urn, SecurableDictionary relatedSecurables, bool isDatabasePrincipal)
            {
                // STrace.Assert(this.exists, "new principals don't have related securables");
                // STrace.Assert(this.connectionInfo != null, "connectionInfo is not set");
                // STrace.Assert(urn.Length != 0, "urn is empty");

                String[] fields = null;
                if (isDatabasePrincipal)
                {
                    fields = new string[] { "ObjectName", "ObjectSchema", "ObjectClass", "ObjectType", "IsTableType" };
                }
                else
                {
                    fields = new string[] { "ObjectName", "ObjectSchema", "ObjectClass", "ObjectType" };
                }

                Enumerator enumerator = new Enumerator();
                Request request = new Request(urn, fields);
                DataTable securableTable = enumerator.Process(this.connectionInfo, request);

                for (int securableIndex = 0; securableIndex < securableTable.Rows.Count; ++securableIndex)
                {
                    DataRow securableRow = securableTable.Rows[securableIndex];

                    string objectName = securableRow["ObjectName"].ToString();
                    object schemaData = securableRow["ObjectSchema"];
                    string objectSchema = (schemaData is DBNull) ? String.Empty : schemaData.ToString();
                    ObjectClass objectClass = (ObjectClass)securableRow["ObjectClass"];
                    object objectTypeData = securableRow["ObjectType"];
                    string objectType = (objectTypeData is DBNull) ? String.Empty : objectTypeData.ToString().Trim();

                    // SMO and SSMS integration not yet complete for security policies, so do not add them to the view
                    //
                    if (objectType.Equals("SP", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    bool isTableType = false;
                    if (isDatabasePrincipal)
                    {
                        isTableType = (bool)securableRow["IsTableType"];
                    }

                    Securable securable = Securable.Create(objectClass,
                                                           objectType,
                                                           objectName,
                                                           objectSchema,
                                                           isTableType,
                                                           this.databaseName,
                                                           this.connectionInfo,
                                                           this.serverVersion);

                    if (!relatedSecurables.Contains(securable))
                    {
                        relatedSecurables.Add(securable);
                        securable.Changed += this.OnChanged;
                    }
                }
            }

            /// <summary>
            /// Enumerate securables and put the securables into the input collection
            /// </summary>
            /// <param name="type">The type of securable we are enumerating</param>
            /// <param name="urn">The URN to enumerate the securables</param>
            /// <param name="securablesCollection">The collection to which new securables are to be added</param>
            private void AddSecurables(SecurableType type, string urn, SecurableDictionary securablesCollection)
            {
                // STrace.Assert(this.connectionInfo != null, "connectionInfo is not set");
                // STrace.Assert(urn.Length != 0, "urn is empty");

                Enumerator enumerator = new Enumerator();
                String[] fields = new string[] { "Urn" };
                Request request = new Request(urn, fields);
                DataTable securableTable = enumerator.Process(this.connectionInfo, request);

                for (int securableIndex = 0; securableIndex < securableTable.Rows.Count; ++securableIndex)
                {
                    DataRow securableRow = securableTable.Rows[securableIndex];

                    Urn securableUrn = new Urn(securableRow["Urn"].ToString());
                    string securableName = Urn.UnEscapeString(securableUrn.GetAttribute("Name"));
                    string securableSchema = securableUrn.GetAttribute("Schema");
                    securableSchema = (securableSchema != null) ? Urn.UnEscapeString(securableSchema) : String.Empty;
                    SecurableKey key = new SecurableKey(securableSchema, securableName, type);

                    if (!securablesCollection.Contains(key))
                    {
                        Securable securable = Securable.Create(type, securableUrn.ToString(), this.connectionInfo, this.serverVersion);
                        securablesCollection.Add(securable);
                        securable.Changed += this.OnChanged;
                    }
                }
            }


            /// <summary>
            /// Get the principal type for the input object
            /// </summary>
            /// <param name="smoPrincipal">The SMO object representing the principal</param>
            /// <returns>>The corresponding PrincipalType</returns>
            public static PrincipalType GetPrincipalType(SqlSmoObject smoPrincipal)
            {
                PrincipalType result;

                Type smoPrincipalType = smoPrincipal.GetType();

                if (typeof(User) == smoPrincipalType)
                {
                    result = PrincipalType.User;
                }
                else if (typeof(DatabaseRole) == smoPrincipalType)
                {
                    result = PrincipalType.DatabaseRole;
                }
                else if (typeof(Login) == smoPrincipalType)
                {
                    result = PrincipalType.Login;
                }
                else if (typeof(ApplicationRole) == smoPrincipalType)
                {
                    result = PrincipalType.ApplicationRole;
                }
                else if (typeof(ServerRole) == smoPrincipalType)
                {
                    result = PrincipalType.ServerRole;
                }
                else
                {

                    // STrace.Assert(false, "smoPrincipal is not a supported principal type");
                    throw new ArgumentException();
                }

                return result;
            }

            /// <summary>
            /// Get the principal type for the input object
            /// </summary>
            /// <param name="searchableType">The searchable type for the principal</param>
            /// <returns>The corresponding PrincipalType</returns>
            public static PrincipalType GetPrincipalType(SearchableObjectType searchableType)
            {
                PrincipalType result;

                switch (searchableType)
                {
                    case SearchableObjectType.Login:

                        result = PrincipalType.Login;
                        break;

                    case SearchableObjectType.ServerRole:

                        result = PrincipalType.ServerRole;
                        break;

                    case SearchableObjectType.User:

                        result = PrincipalType.User;
                        break;

                    case SearchableObjectType.DatabaseRole:

                        result = PrincipalType.DatabaseRole;
                        break;

                    case SearchableObjectType.ApplicationRole:

                        result = PrincipalType.ApplicationRole;
                        break;

                    default:

                        // STrace.Assert(false, "searchableType is not a supported principal type");
                        throw new ArgumentException();
                }

                return result;
            }

            /// <summary>
            /// Get the searchable object type equivalent to the input principal type
            /// </summary>
            /// <param name="principalType"></param>
            /// <returns></returns>
            public static SearchableObjectType GetSearchableObjectType(PrincipalType principalType)
            {
                // TypeCount is an invalid pseudo type
                SearchableObjectType result;

                switch (principalType)
                {
                    case PrincipalType.ApplicationRole:

                        result = SearchableObjectType.ApplicationRole;
                        break;

                    case PrincipalType.DatabaseRole:

                        result = SearchableObjectType.DatabaseRole;
                        break;

                    case PrincipalType.Login:

                        result = SearchableObjectType.Login;
                        break;

                    case PrincipalType.ServerRole:

                        result = SearchableObjectType.ServerRole;
                        break;

                    case PrincipalType.User:

                        result = SearchableObjectType.User;
                        break;

                    default:

                        // STrace.Assert(false, "unexpected principal type");
                        throw new ArgumentException();
                }

                return result;
            }
        }

        internal class PrincipalKey : IComparable
        {
            private string name;
            private int principalType;
            private bool changesExist;

            internal string Name
            {
                get
                {
                    return this.name;
                }
            }

            internal int PrincipalType
            {
                get
                {
                    return this.principalType;
                }
            }

            internal bool ChangesExist
            {
                get
                {
                    return this.changesExist;
                }
            }

            internal PrincipalKey(Principal principal)
            {
                this.name = principal.Name;
                this.principalType = (int)principal.PrincipalType;
                this.changesExist = principal.ChangesExist;
            }

            internal PrincipalKey(SearchableObject searchable)
            {
                this.name = searchable.Name;
                this.principalType = (int)Principal.GetPrincipalType(searchable.SearchableObjectType);
                this.changesExist = false;
            }

            internal PrincipalKey(string name, PrincipalType type)
            {
                this.name = name;
                this.principalType = (int)type;
                this.changesExist = false;
            }

            internal PrincipalKey(string name, SearchableObjectType searchableType)
            {
                this.name = name;
                this.principalType = (int)Principal.GetPrincipalType(searchableType);
                this.changesExist = false;
            }

            public override int GetHashCode()
            {
                string fullname = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", this.name, this.principalType);
                return fullname.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                bool result = false;

                PrincipalKey other = obj as PrincipalKey;

                if (other != null)
                {
                    result = (this.CompareTo(other) == 0);
                }

                return result;
            }

            public int CompareTo(object obj)
            {
                int result = 0;
                PrincipalKey other = (PrincipalKey)obj;

                result = String.Compare(this.name, other.name, StringComparison.Ordinal);

                if (0 == result)
                {
                    result = this.principalType - other.principalType;
                }

                return result;
            }
        }

        /// <summary>
        /// A sorted collection of Principals
        /// </summary>
        internal class PrincipalCollection : ICollection
        {
            private SortedList data;

            /// <summary>
            /// Constructor
            /// </summary>
            public PrincipalCollection()
            {
                data = new SortedList(StringComparer.Ordinal);
            }

            public PrincipalCollection(PrincipalComparer comparer)
            {
                this.data = new SortedList(comparer);
            }

            public PrincipalCollection(PrincipalCollection original, PrincipalComparer comparer)
            {
                if (original.Count != 0)
                {
                    this.data = new SortedList(original.data, comparer);
                }
                else
                {
                    this.data = new SortedList(comparer);
                }
            }

            /// <summary>
            /// Indexer, by numeric index
            /// </summary>
#pragma warning disable IDE0026 // Use expression body for indexer
            public Principal this[int index]
            {
                get
                {
                    return ((Principal)this.data.GetByIndex(index));
                }
            }
#pragma warning restore IDE0026 // Use expression body for indexer

            /// <summary>
            /// Indexer, by name
            /// </summary>
#pragma warning disable IDE0026 // Use expression body for indexer
            public Principal this[PrincipalKey principalKey]
            {
                get
                {
                    return ((Principal)this.data[principalKey]);
                }
            }
#pragma warning restore IDE0026 // Use expression body for indexer

            /// <summary>
            /// Update the PrincipalKey associated with this principal
            /// </summary>
            /// <param name="principal">The principal to update the key of</param>
            internal void UpdateKey(Principal principal)
            {
                this.data.Remove(new PrincipalKey(principal.Name, principal.PrincipalType));
                this.data.Add(new PrincipalKey(principal), principal);
            }

            /// <summary>
            /// Add an object to the collection
            /// </summary>
            /// <param name="value">the object to add</param>
            public void Add(Principal value)
            {
                this.data.Add(new PrincipalKey(value), value);
                this.NotifyObservers();
            }

            /// <summary>
            /// Get the index of the object in the collection
            /// </summary>
            /// <param name="principal">The object to search for</param>
            /// <returns>The object's index</returns>
            public int IndexOf(Principal principal)
            {
                return this.data.IndexOfKey(new PrincipalKey(principal));
            }

            /// <summary>
            /// Remove an object from the collection
            /// </summary>
            /// <param name="principal">The object to remove</param>
            public void Remove(Principal principal)
            {
                if (this.Contains(principal))
                {
                    this.data.Remove(new PrincipalKey(principal));
                }

                this.NotifyObservers();
            }

            public void Remove(SearchableObject searchable)
            {
                if (this.Contains(searchable))
                {
                    this.data.Remove(new PrincipalKey(searchable));
                }

                this.NotifyObservers();
            }

            /// <summary>
            /// Does the collection contain a particular object
            /// </summary>
            /// <param name="principal">The name of the object to check for</param>
            /// <returns>true if the object is in the collection, false otherwise</returns>
            public bool Contains(Principal principal)
            {
                return this.data.Contains(new PrincipalKey(principal));
            }

            public bool Contains(SearchableObject principal)
            {
                return this.data.Contains(new PrincipalKey(principal));
            }

            public bool Contains(PrincipalKey principalKey)
            {
                return this.data.Contains(principalKey);
            }

            /// <summary>
            /// Notify iterators that the collection has changed
            /// </summary>
            private void NotifyObservers()
            {
                if (null != this.OnInvalidateEnumerator)
                {
                    this.OnInvalidateEnumerator();
                }
            }


            /// <summary>
            /// Delegate declaration for delegates that will be called when the collection changes
            /// </summary>
            internal delegate void InvalidateEnumerator();
            /// <summary>
            /// Event that is fired when the collection changes
            /// </summary>
            internal event InvalidateEnumerator OnInvalidateEnumerator;

            #region ICollection Members

            /// <summary>
            /// Is access to collection thread-safe?
            /// </summary>
            public bool IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            /// <summary>
            /// How many objects are in the collection?
            /// </summary>
            public int Count
            {
                get
                {
                    return this.data.Count;
                }
            }

            /// <summary>
            /// Copy the collection to an array
            /// </summary>
            /// <param name="array">The target array</param>
            /// <param name="index">The array index where copying is to begin</param>
            public void CopyTo(Array array, int index)
            {
                for (int i = 0; i < this.data.Count; ++i)
                {
                    array.SetValue(this.data.GetByIndex(i), index + i);
                }
            }

            /// <summary>
            /// The object to be used to lock the collection
            /// </summary>
            public object SyncRoot
            {
                get
                {
                    return this;
                }
            }


            #endregion

            #region IEnumerable Members

            /// <summary>
            /// Get an enumerator for the collection
            /// </summary>
            /// <returns>An enumerator</returns>
            public IEnumerator GetEnumerator()
            {
                return new PrincipalCollectionEnumerator(this);
            }


            #endregion
        }

        enum PrincipalSortType
        {
            Name,
            Type,
            Status
        }

        /// <summary>
        /// Comparer for principals
        /// </summary>
        internal class PrincipalComparer : IComparer
        {
            internal static PrincipalSortType[] DefaultSortingOrder =
            {
                PrincipalSortType.Name,
                PrincipalSortType.Type,
                PrincipalSortType.Status
            };

            private PrincipalSortType[] sortOrder;
            private bool ascending;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="sortOrder">Indicate which column to sort</param>
            /// <param name="ascending">Whether to sort in ascending order</param>
            public PrincipalComparer(PrincipalSortType[] sortOrder, bool ascending)
            {
                this.sortOrder = sortOrder;
                this.ascending = ascending;
            }

            /// <summary>
            /// Compare two securable keys
            /// </summary>
            /// <param name="x">The first principal</param>
            /// <param name="y">The second principal</param>
            /// <returns>-1 if first is less than second, 0 if first equals second, 1 if second is less than first</returns>
            public int Compare(object x, object y)
            {
                int result = 0;

                if (x == null)
                {
                    result = -1;
                }
                else if (y == null)
                {
                    result = 1;
                }
                else
                {
                    PrincipalKey principalKey1;
                    PrincipalKey principalKey2;

                    // if not ascending sort, reverse the roles of x and y
                    if (this.ascending)
                    {
                        principalKey1 = (PrincipalKey)x;
                        principalKey2 = (PrincipalKey)y;
                    }
                    else
                    {
                        principalKey1 = (PrincipalKey)y;
                        principalKey2 = (PrincipalKey)x;
                    }

                    int i = 0;

                    while (0 == result && i < this.sortOrder.Length)
                    {
                        switch (this.sortOrder[i])
                        {
                            case PrincipalSortType.Name:

                                result = String.Compare(
                                    principalKey1.Name,
                                    principalKey2.Name,
                                    StringComparison.CurrentCulture);
                                break;

                            case PrincipalSortType.Type:

                                result = principalKey1.PrincipalType - principalKey2.PrincipalType;
                                break;

                            case PrincipalSortType.Status:

                                if (!principalKey1.ChangesExist && principalKey2.ChangesExist)
                                {
                                    result = -1;
                                }
                                else if (principalKey1.ChangesExist && !principalKey2.ChangesExist)
                                {
                                    result = 1;
                                }
                                break;

                            default:
                                // STrace.Assert(true, "Unknown PrincipalSortType");
                                result = 0;
                                break;
                        }

                        ++i;
                    }
                }

                return result;
            }

        }

        /// <summary>
        /// An enumerator for PrincipalCollections
        /// </summary>
        internal class PrincipalCollectionEnumerator : IEnumerator
        {
            private PrincipalCollection collection;
            private int currentIndex;
            private Principal currentObject;
            private bool isValid;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="collection">The collection to enumerate</param>
            internal PrincipalCollectionEnumerator(PrincipalCollection collection)
            {
                this.collection = collection;
                this.currentIndex = -1;
                this.currentObject = null;
                this.isValid = true;

                this.collection.OnInvalidateEnumerator += new PrincipalCollection.InvalidateEnumerator(this.Invalidate);
            }


            /// <summary>
            /// The method the collection should call when the collection's state changes
            /// </summary>
            internal void Invalidate()
            {
                this.isValid = false;
            }


            #region IEnumerator Members

            /// <summary>
            /// Set the enumerator index to "before the start"
            /// </summary>
            public void Reset()
            {
                if (!this.isValid)
                {
                    // STrace.Assert(false, "The enumerator has been invalidated.");
                    throw new InvalidOperationException();
                }

                this.currentIndex = -1;
                this.currentObject = null;
            }

            /// <summary>
            /// The current SqlObject
            /// </summary>
            public object Current
            {
                get
                {
                    if (this.currentIndex < 0)
                    {
                        // STrace.Assert(false, "The enumerator is positioned before start of collection.  Did you forget to call MoveNext()?");
                        throw new InvalidOperationException();
                    }

                    if (null == this.currentObject)
                    {
                        // STrace.Assert(false, "There is no current object.  Did you forget to check the result of MoveNext()?");
                        throw new InvalidOperationException();
                    }

                    return this.currentObject;
                }
            }

            /// <summary>
            /// Move to the next object in the colection
            /// </summary>
            /// <returns>True if there was a next object, false otherwise</returns>
            public bool MoveNext()
            {
                if (!this.isValid)
                {
                    // STrace.Assert(false, "The enumerator has been invalidated.");
                    throw new InvalidOperationException();
                }

                bool result = false;

                if (this.currentIndex < (this.collection.Count - 1))
                {
                    ++(this.currentIndex);
                    this.currentObject = this.collection[this.currentIndex];
                    result = true;
                }

                return result;
            }

            #endregion
        }

        /// <summary>
        /// The relationship between a securable, a principal, and a grant state.
        /// </summary>
        internal class PermissionState : IComparable
        {
            private Permission permission;
            private Securable securable;
            private Principal principal;
            private string grantor;

            private PermissionStatus originalState;
            private PermissionStatus currentState;
            private PermissionDisplayStatus displayState;
            private ArrayList children = null;
            private PermissionState parent = null;
            private bool settingState = false;

            private event EventHandler observableChanged;
            private event PermissionStateChangedEventHandler permissionStateChanged;

            /// <summary>
            /// The permission role-player
            /// </summary>
            public Permission Permission
            {
                get
                {
                    return this.permission;
                }
            }

            public string Grantor
            {
                get { return grantor; }
            }

            /// <summary>
            /// The securable role-player
            /// </summary>
            public Securable Securable
            {
                get
                {
                    return this.securable;
                }
            }

            /// <summary>
            /// The principal role-player
            /// </summary>
            public Principal Principal
            {
                get
                {
                    return this.principal;
                }
            }

            /// <summary>
            /// determines if we need to issue CASCADE with a deny or revoke
            /// </summary>
            public bool CascadeNeeded
            {
                get
                {
                    bool cascade = this.OriginalState == PermissionStatus.WithGrant;
                    if (!cascade && this.HasChildren)
                    {
                        IEnumerator e = this.children.GetEnumerator();
                        e.Reset();

                        while (e.MoveNext())
                        {
                            PermissionState ps = (PermissionState)e.Current;
                            if (ps.OriginalState == PermissionStatus.WithGrant)
                            {
                                cascade = true;
                                break;
                            }
                        }
                    }
                    return cascade;
                }
            }

            /// <summary>
            /// The grant-state of the permission relationship
            /// </summary>
            public PermissionStatus State
            {
                get
                {
                    return this.currentState;
                }
                set
                {
                    if (value != this.currentState)
                    {
                        this.settingState = true;
                        PermissionStatus oldState = this.currentState;
                        PermissionDisplayStatus oldDisplayState = this.displayState;
                        this.currentState = value;

                        switch (this.currentState)
                        {
                            case PermissionStatus.Grant:

                                this.displayState = PermissionDisplayStatus.Grant;
                                break;

                            case PermissionStatus.WithGrant:

                                this.displayState = PermissionDisplayStatus.WithGrant;
                                break;

                            case PermissionStatus.Deny:

                                this.displayState = PermissionDisplayStatus.Deny;
                                break;

                            case PermissionStatus.Revoke:

                                this.displayState = PermissionDisplayStatus.Revoke;
                                break;
                        }

                        this.NotifyObservers(oldState, oldDisplayState);
                        this.settingState = false;
                    }
                }
            }

            /// <summary>
            /// The original grant-state of the permission relationship
            /// </summary>
            public PermissionStatus OriginalState
            {
                get
                {
                    return this.originalState;
                }
            }

            /// <summary>
            /// The display state for the permission relationship in the UI
            /// </summary>
            public PermissionDisplayStatus DisplayState
            {
                get
                {
                    return this.displayState;
                }
            }

            /// <summary>
            /// Has the user changed the grant-state of this permission?
            /// </summary>
            public bool StateChanged
            {
                get
                {
                    return (this.originalState != this.currentState);
                }
            }

            /// <summary>
            /// Whether the user has removed the Permission state in the UI
            /// </summary>
            public bool IsRemoved
            {
                get
                {
                    return (this.principal.IsRemoved || this.securable.IsRemoved);
                }
            }

            /// <summary>
            /// Does this PermissionState have child states
            /// </summary>
            public bool HasChildren
            {
                get
                {
                    return ((this.children != null) && (this.children.Count != 0));
                }
            }

            /// <summary>
            /// The parent PermissionState (e.g. if this permission state is for a column, Parent is the permission state of the parent table)
            /// </summary>
            public PermissionState Parent
            {
                get
                {
                    return this.parent;
                }

                set
                {
                    // STrace.Assert(null == this.parent, "parent has already been set");
                    this.parent = value;
                    this.parent.PermissionStateChanged += new PermissionStateChangedEventHandler(this.OnParentStateChanged);
                }
            }


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="securable">The securable in the relationship</param>
            /// <param name="principal">The principal in the relationship</param>
            /// <param name="permission">The permission in the relationship</param>
            /// <param name="state">The permission status of the relationship</param>
            /// <param name="grantor"></param>
            public PermissionState(Securable securable,
                                   Principal principal,
                                   Permission permission,
                                   PermissionStatus state,
                                   string grantor)
            {
                this.permission = permission;
                this.principal = principal;
                this.securable = securable;
                this.currentState = state;
                this.originalState = state;
                this.grantor = grantor;
                this.DetermineDisplayState();
            }


            /// <summary>
            /// Put the permission into the right category
            /// </summary>
            /// <param name="grants">Set of granted permissions</param>
            /// <param name="withGrants"></param>
            /// <param name="denies">Set of denied permissions</param>
            /// <param name="revokes">Set of revoked permissions</param>
            public void AssignChange(List<PermissionState> grants,
                                     List<PermissionState> withGrants,
                                     List<PermissionState> denies,
                                     List<PermissionState> revokes)
            {
                if (this.StateChanged)
                {
                    PermissionStatus effectiveCurrentState = this.currentState;

                    switch (effectiveCurrentState)
                    {
                        case PermissionStatus.Grant:
                            grants.Add(this);
                            break;

                        case PermissionStatus.WithGrant:
                            withGrants.Add(this);
                            break;

                        case PermissionStatus.Deny:
                            denies.Add(this);
                            break;

                        default:
                            // STrace.Assert((PermissionStatus.Revoke == this.State), "unexpected permission state");
                            revokes.Add(this);
                            break;
                    }
                }
            }

            /// <summary>
            /// Revoke all this permission and all child permissions
            /// </summary>
            public void Revoke()
            {
                if (PermissionDisplayStatus.Revoke != this.displayState)
                {
                    this.settingState = true;

                    PermissionStatus oldState = this.currentState;
                    PermissionDisplayStatus oldDisplayState = this.displayState;

                    this.currentState = PermissionStatus.Revoke;
                    this.displayState = PermissionDisplayStatus.Revoke;

                    // there are two kinds of observers:  those interested in modifying there own state
                    // to reflect changes in this object's state, and those who only need to know that
                    // something has changed.  Notify both kinds of observers of the state change.
                    this.NotifyObservers(oldState, oldDisplayState);
                    this.NotifyObservers();

                    this.settingState = false;
                }
            }

            /// <summary>
            /// Toggle the grant-state between Grant and not-Grant
            /// </summary>
            /// <remarks>
            /// This supposed to handle click events on the "Allow" check-box in the UI.
            ///
            /// If the current state is Grant (or effectively Grant), transition to Revoke
            /// If the current state is Revoke or Deny (or effectively Deny), transition to Grant
            /// If the current state is indeterminate, transition to Grant
            /// </remarks>
            public void ToggleGrant(bool withGrant)
            {
                this.settingState = true;

                PermissionStatus oldState = this.currentState;
                PermissionDisplayStatus oldDisplayState = this.displayState;

                PermissionStatus effectiveState = this.GetEffectiveState();

                PermissionStatus stateToToggle = withGrant ?
                                                 PermissionStatus.WithGrant :
                                                 PermissionStatus.Grant;

                if (stateToToggle == effectiveState && withGrant)
                {
                    this.currentState = PermissionStatus.Grant;
                    this.displayState = PermissionDisplayStatus.Grant;
                }
                else if (stateToToggle == effectiveState ||
                         (!withGrant && effectiveState == PermissionStatus.WithGrant))
                {
                    this.currentState = PermissionStatus.Revoke;
                    this.displayState = PermissionDisplayStatus.Revoke;
                }
                else
                {
                    this.currentState = stateToToggle;
                    this.displayState = withGrant ?
                                        PermissionDisplayStatus.WithGrant :
                                        PermissionDisplayStatus.Grant;
                }

                // there are two kinds of observers:  those interested in modifying there own state
                // to reflect changes in this object's state, and those who only need to know that
                // something has changed.  Notify both kinds of observers of the state change.
                this.NotifyObservers(oldState, oldDisplayState);
                this.NotifyObservers();

                this.settingState = false;
            }

            /// <summary>
            /// Toggle the grant-state between Deny and not-Deny
            /// </summary>
            /// <remarks>
            /// This supposed to handle click events on the "Deny" check-box in the UI.
            ///
            /// If the current state is Deny (or effectively Deny), transition to Revoke
            /// If the current state is Revoke or Grant (or effectively Grant), transition to Deny
            /// If the current state is indeterminate, transition to Deny
            /// </remarks>
            public void ToggleDeny()
            {
                this.settingState = true;

                PermissionStatus oldState = this.currentState;
                PermissionDisplayStatus oldDisplayState = this.displayState;

                PermissionStatus effectiveState = this.GetEffectiveState();

                if (PermissionStatus.Deny == effectiveState)
                {
                    this.currentState = PermissionStatus.Revoke;
                    this.displayState = PermissionDisplayStatus.Revoke;
                }
                else
                {
                    this.currentState = PermissionStatus.Deny;
                    this.displayState = PermissionDisplayStatus.Deny;
                }

                // there are two kinds of observers:  those interested in modifying there own state
                // to reflect changes in this object's state, and those who only need to know that
                // something has changed.  Notify both kinds of observers of the state change.
                this.NotifyObservers(oldState, oldDisplayState);
                this.NotifyObservers();

                this.settingState = false;
            }

            /// <summary>
            /// Add a child permission state to the child collection and hook up notifications
            /// </summary>
            /// <param name="child">The child to add</param>
            internal void AddChild(PermissionState child)
            {
                if (null == this.children)
                {
                    this.children = new ArrayList();
                }

                child.Parent = this;
                this.children.Add(child);
                this.DetermineDisplayState();
                child.PermissionStateChanged += new PermissionStateChangedEventHandler(this.OnChildStateChanged);
            }

            /// <summary>
            /// Set the display state to whatever it should be for this object's grant-state
            /// and the grant-state of all its children
            /// </summary>
            private void DetermineDisplayState()
            {
                // if there are no children, display state is the same as state
                if (!this.HasChildren)
                {
                    switch (this.currentState)
                    {
                        case PermissionStatus.Revoke:
                            this.displayState = PermissionDisplayStatus.Revoke;
                            break;

                        case PermissionStatus.Grant:
                            this.displayState = PermissionDisplayStatus.Grant;
                            break;

                        case PermissionStatus.WithGrant:
                            this.displayState = PermissionDisplayStatus.WithGrant;
                            break;

                        default:
                            // STrace.Assert(PermissionStatus.Deny == this.currentState, "unexpected permission status");
                            this.displayState = PermissionDisplayStatus.Deny;
                            break;
                    }
                }
                else
                {
                    // if there are children
                    //   - display state is INDETERMINATE if some children are GRANTED and some are DENIED
                    //   - display state is PARTIALLY GRANTED if some children are GRANTED and the rest are REVOKED
                    //   - display state is PARTIALLY DENIED if some children are DENIED and the rest are REVOKED
                    //   - display state is GRANTED if the parent is GRANTED or if ALL the children are GRANTED
                    //   - display state is DENIED if the parent is DENIED or if ALL the children are DENIED
                    //   - display state is REVOKED if the parent is REVOKED and ALL the children are REVOKED as well

                    bool anyChildrenRevoked = false;
                    bool anyChildrenGranted = false;
                    bool anyChildrenWithGrant = false;
                    bool anyChildrenDenied = false;

                    IEnumerator childEnumerator = this.children.GetEnumerator();
                    childEnumerator.Reset();

                    while (childEnumerator.MoveNext())
                    {
                        PermissionState childState = (PermissionState)childEnumerator.Current;

                        if (PermissionStatus.Deny == childState.State)
                        {
                            anyChildrenDenied = true;
                        }
                        else if (PermissionStatus.WithGrant == childState.State)
                        {
                            anyChildrenWithGrant = true;
                        }
                        else if (PermissionStatus.Grant == childState.State)
                        {
                            anyChildrenGranted = true;
                        }
                        else
                        {
                            anyChildrenRevoked = true;
                        }

                        if (anyChildrenWithGrant && anyChildrenDenied && anyChildrenGranted && anyChildrenRevoked)
                        {
                            break;
                        }
                    }

                    bool allChildrenDenied = (!anyChildrenGranted && !anyChildrenRevoked && !anyChildrenWithGrant);
                    bool allChildrenGranted = (!anyChildrenDenied && !anyChildrenRevoked && !anyChildrenWithGrant);
                    bool allChildrenRevoked = (!anyChildrenGranted && !anyChildrenDenied && !anyChildrenWithGrant);
                    bool allChildrenWithGrant = (!anyChildrenGranted && !anyChildrenRevoked && !anyChildrenDenied);

                    // if (this state is DENY) then ((all children are denied) or (all children are revoked))
                    // STrace.Assert((PermissionStatus.Deny != this.currentState) || (allChildrenDenied || allChildrenRevoked), "conflicting parent and child object permissions detected");

                    // if (this state is GRANT) then ((all children are granted) or (all children are revoked))
                    // STrace.Assert((PermissionStatus.Grant != this.currentState) || (allChildrenGranted || allChildrenRevoked), "conflicting parent and child object permissions detected");

                    if (anyChildrenWithGrant && anyChildrenDenied)
                    {
                        this.displayState = PermissionDisplayStatus.Indeterminate;
                    }
                    else if (anyChildrenWithGrant && anyChildrenRevoked)
                    {
                        this.displayState = PermissionDisplayStatus.PartialWithGrant;
                    }
                    else if (anyChildrenDenied && anyChildrenGranted && anyChildrenRevoked)
                    {
                        this.displayState = PermissionDisplayStatus.PartialGrantDeny;
                    }
                    else if (anyChildrenGranted && anyChildrenRevoked)
                    {
                        this.displayState = PermissionDisplayStatus.PartialGrant;
                    }
                    else if (anyChildrenDenied && anyChildrenRevoked)
                    {
                        this.displayState = PermissionDisplayStatus.PartialDeny;
                    }
                    else if (anyChildrenWithGrant || (PermissionStatus.WithGrant == this.currentState))
                    {
                        this.displayState = PermissionDisplayStatus.WithGrant;
                    }
                    else if (anyChildrenGranted || (PermissionStatus.Grant == this.currentState))
                    {
                        this.displayState = PermissionDisplayStatus.Grant;
                    }
                    else if (anyChildrenDenied || (PermissionStatus.Deny == this.currentState))
                    {
                        this.displayState = PermissionDisplayStatus.Deny;
                    }
                    else
                    {
                        this.displayState = PermissionDisplayStatus.Revoke;
                    }
                }
            }

            /// <summary>
            /// Get the effective state (e.g. GRANT if state is GRANT or if all the children are GRANTED)
            /// </summary>
            /// <returns>The effective state</returns>
            private PermissionStatus GetEffectiveState()
            {
                // if there are no child objects, the effective state is just the current state
                // if there are children and the display state is not "partial", then effective state is display state
                // if there are children and the display state is "partial", then effective state is "REVOKE"
                PermissionStatus result = PermissionStatus.Revoke;

                if (!this.HasChildren)
                {
                    result = this.currentState;
                }
                else
                {
                    switch (this.displayState)
                    {
                        case PermissionDisplayStatus.Grant:
                            result = PermissionStatus.Grant;
                            break;

                        case PermissionDisplayStatus.WithGrant:
                            result = PermissionStatus.WithGrant;
                            break;

                        case PermissionDisplayStatus.Deny:
                            result = PermissionStatus.Deny;
                            break;

                        default:
                            result = PermissionStatus.Revoke;
                            break;
                    }
                }

                return result;
            }

            /// <summary>
            /// Handle change events on child PermissionStates
            /// </summary>
            /// <param name="sender">The child that changed</param>
            /// <param name="childNewStatus">The new state of the child</param>
            /// <param name="childOldStatus">The old state of the child</param>
            /// <param name="childNewDisplayStatus">The new display state of the child</param>
            /// <param name="childOldDisplayStatus">The old display state of the child</param>
            internal void OnChildStateChanged(PermissionState sender,
                                             PermissionStatus childNewStatus,
                                             PermissionStatus childOldStatus,
                                             PermissionDisplayStatus childNewDisplayStatus,
                                             PermissionDisplayStatus childOldDisplayStatus)
            {
                if (!this.settingState && (childNewStatus != childOldStatus))
                {
                    PermissionStatus oldStatus = this.currentState;
                    PermissionDisplayStatus oldDisplayStatus = this.displayState;
                    this.currentState = PermissionStatus.Revoke;

                    this.DetermineDisplayState();

                    if ((oldStatus != this.currentState) || (oldDisplayStatus != this.displayState))
                    {
                        this.settingState = true;
                        this.NotifyObservers(oldStatus, oldDisplayStatus);
                        this.settingState = false;
                    }

                    this.NotifyObservers();
                }
            }

            /// <summary>
            /// Handle change events on parent PermissionStates
            /// </summary>
            /// <param name="sender">The parent that changed</param>
            /// <param name="parentNewStatus">The new state of the parent</param>
            /// <param name="parentOldStatus">The old state of the parent</param>
            /// <param name="parentNewDisplayStatus">The new display state of the parent</param>
            /// <param name="parentOldDisplayStatus">The old display state of the parent</param>
            private void OnParentStateChanged(PermissionState sender,
                                              PermissionStatus parentNewStatus,
                                              PermissionStatus parentOldStatus,
                                              PermissionDisplayStatus parentNewDisplayStatus,
                                              PermissionDisplayStatus parentOldDisplayStatus)
            {
                // revoke permission if parent changes were not caused by changes to this object, parent state
                // actually changed and wasn't just a display changed, and if this permission isn't already revoked

                if (!this.settingState &&
                    (parentNewStatus != parentOldStatus) &&
                    (PermissionDisplayStatus.Revoke != this.displayState))
                {
                    this.settingState = true;

                    // parent status has changed, so all children are to be revoked
                    PermissionStatus oldStatus = this.currentState;
                    PermissionDisplayStatus oldDisplayStatus = this.displayState;
                    this.currentState = PermissionStatus.Revoke;

                    this.DetermineDisplayState();

                    this.NotifyObservers(oldStatus, oldDisplayStatus);
                    this.settingState = false;
                }
            }



            #region IComparable Members
            /// <summary>
            /// Determine whether this object is less than, equal to, or greater than another object
            /// </summary>
            /// <param name="obj">The object to compare</param>
            /// <returns>Less than 0 if this is less, 0 if equal, greater than 0 if this is greater</returns>
            public int CompareTo(object obj)
            {
                // STrace.Assert(typeof(PermissionState) == obj.GetType(), "unexpected object type");

                PermissionState other = (PermissionState)obj;
                return this.permission.Name.CompareTo(other.permission.Name);
            }

            #endregion


            public static void AddChildrenToEmptyParents(PermissionStateCollection states)
            {
                for (int i = 0; i < states.Count; ++i)
                {
                    //
                    // loop through all states that have children
                    //
                    if (states[i].HasChildren)
                    {
                        //
                        // find all other states that:
                        // 1. map to the same permission
                        // 2. have a grantor
                        //
                        for (int j = 0; j < states.Count; ++j)
                        {
                            if (i != j &&
                                !string.IsNullOrEmpty(states[j].Grantor) &&
                                states[i].Permission.Name == states[j].Permission.Name)
                            {
                                //
                                // copy any children present in i that are not in j,
                                // all with Revoked status
                                //
                                foreach (PermissionState ps in states[i].children)
                                {
                                    // see if j already has this child
                                    bool hasThisChild = false;
                                    if (states[j].HasChildren)
                                    {
                                        foreach (PermissionState ps2 in states[j].children)
                                        {
                                            if (ps.Securable.Name == ps2.Securable.Name &&
                                                ps.Permission.Name == ps2.Permission.Name)
                                            {
                                                hasThisChild = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!hasThisChild)
                                    {
                                        PermissionState child = new PermissionState(ps.Securable,
                                                                                    ps.Principal,
                                                                                    ps.Permission,
                                                                                    PermissionStatus.Revoke,
                                                                                    states[j].Grantor);
                                        states[j].AddChild(child);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Create the set of PermissionStates relating securable to principal.  Child permission states, if any,
            /// are created as well.  The permission state relationship instances are added to the securable's and principal's
            /// PermissionState collections.
            /// </summary>
            /// <param name="securable">The securable in the relationships</param>
            /// <param name="principal">The principal in the relationships</param>
            static public void PopulatePermissionStates(Securable securable, Principal principal)
            {
                PermissionStateCollection permissionStates = GetPermissionStates(securable, principal);

                for (int permissionStateIndex = 0;
                     permissionStateIndex < permissionStates.Count;
                     ++permissionStateIndex)
                {
                    PermissionState permissionState = permissionStates[permissionStateIndex];

                    securable.AddPermissionState(permissionState);
                    principal.AddPermissionState(permissionState);

                    AddPermissionStateToParent(securable, principal, permissionState);
                }

                PopulateChildPermissionStates(securable, principal);
            }

            /// <summary>
            /// If the permission state should be a child of some other permission state, add the child to the parent's collection
            /// </summary>
            /// <param name="securable">The securable in the relationships</param>
            /// <param name="principal">The principal in the relationships</param>
            /// <param name="child">The (possible) child permission state</param>
            static private void AddPermissionStateToParent(Securable securable,
                                                           Principal principal,
                                                           PermissionState child)
            {
                if (securable.Parent != null)
                {
                    PermissionStateCollection parentStates = securable.Parent.GetPermissionStates(principal);
                    // STrace.Assert(parentStates != null, "parent securable did not have permissions populated for principal.");

                    PermissionState parentState = parentStates[child.Permission.Name + child.Grantor];
                    if (parentState == null)
                    {
                        // parent doesn't exist, create it.
                        parentState = new PermissionState(securable.Parent,
                                                          child.Principal,
                                                          child.Permission,
                                                          PermissionStatus.Revoke,
                                                          child.Grantor);

                        securable.Parent.AddPermissionState(parentState);
                        principal.AddPermissionState(parentState);
                    }
                    parentState.AddChild(child);
                }
            }

            /// <summary>
            /// If the securable has child columns, create the child permission states relating the columns to the principal
            /// </summary>
            /// <param name="securable">The securable whose children we are hooking up</param>
            /// <param name="principal">The principal to whom the children are to be hooked</param>
            static private void PopulateChildPermissionStates(Securable securable, Principal principal)
            {
                if (securable.Children.Count != 0)
                {
                    IEnumerator childEnumerator = securable.Children.GetEnumerator();
                    childEnumerator.Reset();

                    while (childEnumerator.MoveNext())
                    {
                        Securable child = (Securable)childEnumerator.Current;
                        PopulatePermissionStates(child, principal);
                    }
                }
            }

            /// <summary>
            /// Create the set of permission states relating securable to principal
            /// </summary>
            /// <param name="securable">The securable in the relationships</param>
            /// <param name="principal">The principal in the relationships</param>
            /// <returns>The set of permission state relationship objects that was created</returns>
            static private PermissionStateCollection GetPermissionStates(Securable securable,
                                                                         Principal principal)
            {
                PermissionStateCollection result = new PermissionStateCollection();

                GetGrantedOrDeniedPermissions(securable, principal, result);
                GetRevokedPermissions(securable, principal, result);

                return result;
            }

            /// <summary>
            /// Enumerate granted and denied permissions on the securable for the principal and create permission states for them
            /// </summary>
            /// <param name="securable">The securable in the relationships</param>
            /// <param name="principal">The principal in the relationships</param>
            /// <param name="permissionStates">The collection into which the new PermissionStates are to be added</param>
            static private void GetGrantedOrDeniedPermissions(Securable securable,
                                                              Principal principal,
                                                              PermissionStateCollection permissionStates)
            {
                // add granted/denied permissions to the result
                if (securable.Exists && principal.Exists)
                {
                    // enumerate granted/denied permissions for the principal
                    string permissionsUrn = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                         "{0}/Permission[@Grantee='{1}']",
                                                         securable.Urn.ToString(),
                                                         Urn.EscapeString(principal.Name));

                    string[] fields = new string[] { "PermissionState", "Code", "Grantor" };
                    Request request = new Request(new Urn(permissionsUrn), fields);
                    DataTable table = new Enumerator().Process(securable.ConnectionInfo, request);

                    // process enumeration results
                    for (int rowIndex = 0; rowIndex < table.Rows.Count; ++rowIndex)
                    {
                        DataRow row = table.Rows[rowIndex];

                        // get result data
                        PermissionStatus state = GetGrantStatusFromEnumeratorResult(row);
                        Permission permission = GetPermissionFromEnumeratorResult(securable, row);

                        string grantor = string.Empty;
                        object o = row["Grantor"];
                        if (o != null)
                        {
                            grantor = o.ToString();
                        }

                        if ((permission != null))
                        {
                            // create the PermissionState
                            PermissionState permissionState = new PermissionState(securable,
                                                                                  principal,
                                                                                  permission,
                                                                                  state,
                                                                                  grantor);

                            // add the PermissionState to the collection
                            permissionStates.Add(permissionState);
                        }
                    }
                }
            }

            /// <summary>
            /// Add permissions that are revoked on the securable for the principal to the collection
            /// </summary>
            /// <param name="securable">The securable in the relationships</param>
            /// <param name="principal">The principal in the relationships</param>
            /// <param name="permissionStates">The collection into which the new PermissionStates are to be added</param>
            static private void GetRevokedPermissions(Securable securable,
                                                      Principal principal,
                                                      PermissionStateCollection permissionStates)
            {
                // add the revoked permissions to the result
                for (int permissionIndex = 0;
                     permissionIndex < securable.RelevantPermissions.Count;
                     ++permissionIndex)
                {
                    Permission permission = (Permission)securable.RelevantPermissions[permissionIndex];

                    if ((permission != null))
                    {
                        string expectedGrantor = string.Empty;
                        if (!string.IsNullOrEmpty(securable.ExpectedGrantor))
                        {
                            expectedGrantor = securable.ExpectedGrantor;
                        }
                        else if (securable.Parent != null && !string.IsNullOrEmpty(securable.Parent.ExpectedGrantor))
                        {
                            expectedGrantor = securable.Parent.ExpectedGrantor;
                        }

                        if (!permissionStates.Contains(permission.Name + expectedGrantor))
                        {
                            PermissionState permissionState = new PermissionState(securable,
                                                                                  principal,
                                                                                  permission,
                                                                                  PermissionStatus.Revoke,
                                                                                  expectedGrantor);
                            permissionStates.Add(permissionState);
                        }
                    }
                }
            }

            /// <summary>
            /// Extract the permission from the data row
            /// </summary>
            /// <param name="securable">The securable whose permissions are being enumerated</param>
            /// <param name="permissionRow">The data row returned by the enumerator</param>
            /// <returns>The Permission corresponding to the enumerator result</returns>
            static private Permission GetPermissionFromEnumeratorResult(Securable securable, DataRow permissionRow)
            {
                Permission result = null;

                switch (securable.SecurableType)
                {
                    case SecurableType.Server:

                        result = Permission.GetPermission((Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue)permissionRow["Code"]);
                        break;

                    case SecurableType.Database:

                        result = Permission.GetPermission((Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue)permissionRow["Code"]);
                        break;

                    default:

                        result = Permission.GetPermission((Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue)permissionRow["Code"]);
                        break;
                }

                return result;
            }

            /// <summary>
            /// Extract the grant status (Granted or Denied) from the data row
            /// </summary>
            /// <param name="permissionRow">The data row returned by the enumerator</param>
            /// <returns>The grant status corresponding to the enumerator result</returns>
            static private PermissionStatus GetGrantStatusFromEnumeratorResult(DataRow permissionRow)
            {
                Microsoft.SqlServer.Management.Smo.PermissionState smoState = (Microsoft.SqlServer.Management.Smo.PermissionState)permissionRow["PermissionState"];
                PermissionStatus result;

                switch (smoState)
                {
                    case Microsoft.SqlServer.Management.Smo.PermissionState.Grant:
                        result = PermissionStatus.Grant;
                        break;

                    case Microsoft.SqlServer.Management.Smo.PermissionState.GrantWithGrant:
                        result = PermissionStatus.WithGrant;
                        break;

                    case Microsoft.SqlServer.Management.Smo.PermissionState.Deny:
                        result = PermissionStatus.Deny;
                        break;

                    default:
                        // STrace.Assert(smoState == Microsoft.SqlServer.Management.Smo.PermissionState.Revoke, "unexpected smo permission state");
                        result = PermissionStatus.Revoke;
                        break;
                }

                return result;
            }


            /// <summary>
            /// Property to access the observable event.
            /// </summary>
            internal event EventHandler Changed
            {
                add { this.observableChanged += value; }
                remove { this.observableChanged -= value; }
            }

            /// <summary>
            /// Event for detailed state change notifications, informs listeners of what changed
            /// </summary>
            internal event PermissionStateChangedEventHandler PermissionStateChanged
            {
                add { this.permissionStateChanged += value; }
                remove { this.permissionStateChanged -= value; }
            }

            /// <summary>
            /// Notify all observers that this object has changed.
            /// </summary>
            /// <param name="sender">The object that changed</param>
            /// <param name="e">Hint for the notification, usually null</param>
            private void NotifyObservers(object sender, EventArgs e)
            {
                if (this.observableChanged != null)
                {
                    this.observableChanged(sender, e);
                }
            }

            /// <summary>
            /// Notify all observers that this object or one of its children has changed.
            /// </summary>
            private void NotifyObservers()
            {
                this.NotifyObservers(this, new EventArgs());
            }


            /// <summary>
            /// Inform observers of state changes on this object
            /// </summary>
            /// <param name="oldState">The state before the change</param>
            /// <param name="oldDisplayState">The display state before the change</param>
            private void NotifyObservers(PermissionStatus oldState, PermissionDisplayStatus oldDisplayState)
            {
                if (this.permissionStateChanged != null)
                {
                    this.permissionStateChanged(this, this.currentState, oldState, this.displayState, oldDisplayState);
                }
            }

        }

        /// <summary>
        /// Permission state changed event delegate
        /// </summary>
        internal delegate void PermissionStateChangedEventHandler(
                                                                 PermissionState sender,
                                                                 PermissionStatus newStatus,
                                                                 PermissionStatus oldStatus,
                                                                 PermissionDisplayStatus newDisplayStatus,
                                                                 PermissionDisplayStatus oldDisplayStatus);


        /// <summary>
        /// A sorted collection of PermissionState objects
        /// </summary>
        internal class PermissionStateCollection : ICollection
        {
            private SortedList data;

            /// <summary>
            /// Constructor
            /// </summary>
            public PermissionStateCollection()
            {
                data = new SortedList();
            }

            /// <summary>
            /// Indexer, by numeric index
            /// </summary>
#pragma warning disable IDE0026 // Use expression body for indexer
            public PermissionState this[int index]
            {
                get
                {
                    return ((PermissionState)this.data.GetByIndex(index));
                }
            }
#pragma warning restore IDE0026 // Use expression body for indexer

            /// <summary>
            /// Indexer, by permission
            /// </summary>
#pragma warning disable IDE0026 // Use expression body for indexer
            public PermissionState this[string key]
            {
                get
                {
                    return ((PermissionState)this.data[key]);
                }
            }
#pragma warning restore IDE0026 // Use expression body for indexer

            /// <summary>
            /// Add an object to the collection
            /// </summary>
            /// <param name="value">the object to add</param>
            public void Add(PermissionState value)
            {
                this.data.Add(value.Permission.Name + value.Grantor, value);
                this.NotifyObservers();
            }

            /// <summary>
            /// Get the index of the object in the collection
            /// </summary>
            /// <param name="key">The object for which to find the index</param>
            /// <returns>The object's index</returns>
            public int IndexOf(string key)
            {
                return this.data.IndexOfKey(key);
            }

            /// <summary>
            /// Remove an object from the collection
            /// </summary>
            /// <param name="key">The object to remove</param>
            public void Remove(string key)
            {
                if (this.data.Contains(key))
                {
                    this.data.Remove(key);
                }

                this.NotifyObservers();
            }

            /// <summary>
            /// Does the collection contain a particular object
            /// </summary>
            /// <param name="key">The object for which to check</param>
            /// <returns>true if the object is in the collection, false otherwise</returns>
            public bool Contains(string key)
            {
                return this.data.Contains(key);
            }

            /// <summary>
            /// Notify iterators that the collection has changed
            /// </summary>
            private void NotifyObservers()
            {
                if (null != this.OnInvalidateEnumerator)
                {
                    this.OnInvalidateEnumerator();
                }
            }


            /// <summary>
            /// Delegate declaration for delegates that will be called when the collection changes
            /// </summary>
            internal delegate void InvalidateEnumerator();
            /// <summary>
            /// Event that is fired when the collection changes
            /// </summary>
            internal event InvalidateEnumerator OnInvalidateEnumerator;

            #region ICollection Members

            /// <summary>
            /// Is access to collection thread-safe?
            /// </summary>
            public bool IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            /// <summary>
            /// How many objects are in the collection?
            /// </summary>
            public int Count
            {
                get
                {
                    return this.data.Count;
                }
            }

            /// <summary>
            /// Copy the collection to an array
            /// </summary>
            /// <param name="array">The target array</param>
            /// <param name="index">The array index where copying is to begin</param>
            public void CopyTo(Array array, int index)
            {
                for (int i = 0; i < this.data.Count; ++i)
                {
                    array.SetValue(this.data.GetByIndex(i), index + i);
                }
            }

            /// <summary>
            /// The object to be used to lock the collection
            /// </summary>
            public object SyncRoot
            {
                get
                {
                    return this;
                }
            }


            #endregion

            #region IEnumerable Members

            /// <summary>
            /// Get an enumerator for the collection
            /// </summary>
            /// <returns>An enumerator</returns>
            public IEnumerator GetEnumerator()
            {
                return new PermissionStateCollectionEnumerator(this);
            }


            #endregion
        }

        /// <summary>
        /// An enumerator for PermissionStateCollections
        /// </summary>
        internal class PermissionStateCollectionEnumerator : IEnumerator
        {
            private PermissionStateCollection collection;
            private int currentIndex;
            private PermissionState currentObject;
            private bool isValid;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="collection">The collection to enumerate</param>
            internal PermissionStateCollectionEnumerator(PermissionStateCollection collection)
            {
                this.collection = collection;
                this.currentIndex = -1;
                this.currentObject = null;
                this.isValid = true;

                this.collection.OnInvalidateEnumerator += new PermissionStateCollection.InvalidateEnumerator(this.Invalidate);
            }


            /// <summary>
            /// The method the collection should call when the collection's state changes
            /// </summary>
            internal void Invalidate()
            {
                this.isValid = false;
            }


            #region IEnumerator Members

            /// <summary>
            /// Set the enumerator index to "before the start"
            /// </summary>
            public void Reset()
            {
                if (!this.isValid)
                {
                    // STrace.Assert(false, "The enumerator has been invalidated.");
                    throw new InvalidOperationException();
                }

                this.currentIndex = -1;
                this.currentObject = null;
            }

            /// <summary>
            /// The current SqlObject
            /// </summary>
            public object Current
            {
                get
                {
                    if (this.currentIndex < 0)
                    {
                        // STrace.Assert(false, "The enumerator is positioned before start of collection.  Did you forget to call MoveNext()?");
                        throw new InvalidOperationException();
                    }

                    if (null == this.currentObject)
                    {
                        // STrace.Assert(false, "There is no current object.  Did you forget to check the result of MoveNext()?");
                        throw new InvalidOperationException();
                    }

                    return this.currentObject;
                }
            }

            /// <summary>
            /// Move to the next object in the colection
            /// </summary>
            /// <returns>True if there was a next object, false otherwise</returns>
            public bool MoveNext()
            {
                if (!this.isValid)
                {
                    // STrace.Assert(false, "The enumerator has been invalidated.");
                    throw new InvalidOperationException();
                }

                bool result = false;

                if (this.currentIndex < (this.collection.Count - 1))
                {
                    ++(this.currentIndex);
                    this.currentObject = this.collection[this.currentIndex];
                    result = true;
                }

                return result;
            }

            #endregion
        }

        /// <summary>
        /// A SQL Server permission.  Use one of the static properties (e.g. Permission.Alter) to acquire a permission instance.
        /// </summary>
        /// <remarks>
        /// Permissions are inherently immutable - how do you change the "EXECUTE" permission in SQL?
        /// </remarks>
        internal class Permission : IComparable
        {
            // TODO:  create subclasses as needed to handle disjoint SMO permission classes

            private string name;
            private ObjectPermission smoObjectPermission;
            private DatabasePermission smoDatabasePermission;
            private ServerPermission smoServerPermission;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">The name of the permission</param>
            /// <param name="objectPermission">The SMO object permission equivalent</param>
            /// <param name="databasePermission">The SMO database permission equivalent</param>
            private Permission(
                string name,
                ObjectPermission objectPermission,
                DatabasePermission databasePermission)
            {
                this.name = name;
                this.smoObjectPermission = objectPermission;
                this.smoDatabasePermission = databasePermission;
                this.smoServerPermission = null;
            }


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">The name of the permission</param>
            /// <param name="databasePermission">The SMO database permission equivalent</param>
            private Permission(
                string name,
                DatabasePermission databasePermission)
            {
                this.name = name;
                this.smoObjectPermission = null;
                this.smoDatabasePermission = databasePermission;
                this.smoServerPermission = null;
            }


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">The name of the permission</param>
            /// <param name="serverPermission">The SMO server permission equivalent</param>
            private Permission(
                              string name,
                              ServerPermission serverPermission)
            {
                this.name = name;
                this.smoObjectPermission = null;
                this.smoDatabasePermission = null;
                this.smoServerPermission = serverPermission;
            }



            /// <summary>
            /// The name of the permission
            /// </summary>
            public string Name
            {
                get
                {
                    return this.name;
                }
            }

            /// <summary>
            /// The equivalent SMO object permission
            /// </summary>
            public ObjectPermission SmoObjectPermission
            {
                get
                {
                    return this.smoObjectPermission;
                }
            }

            /// <summary>
            /// The equivalent SMO database permission
            /// </summary>
            public DatabasePermission SmoDatabasePermission
            {
                get
                {
                    return this.smoDatabasePermission;
                }
            }

            /// <summary>
            /// The equivaletn SMO server permission
            /// </summary>
            public ServerPermission SmoServerPermission
            {
                get
                {
                    return this.smoServerPermission;
                }
            }


            #region IComparable Members

            public int CompareTo(object obj)
            {
                return this.Name.CompareTo(((Permission)obj).Name);
            }


            #endregion

            #region static fields and methods

            // initialization flags
            private static bool objectPermissionsPopulated = false;
            private static bool databasePermissionsPopulated = false;
            private static bool serverPermissionsPopulated = false;

            /// <summary>
            /// Get the Permission equivalent to the SMO ObjectPermissionSetValue
            /// </summary>
            /// <param name="permissionValue">The permission to find</param>
            /// <returns>The corresponding Permission instance</returns>
            public static Permission GetPermission(Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue permissionValue)
            {
                Permission result = null;

                switch (permissionValue)
                {
                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Alter:

                        result = Permission.Alter;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Connect:

                        result = Permission.Connect;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Control:

                        result = Permission.Control;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Delete:

                        result = Permission.Delete;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Execute:

                        result = Permission.Execute;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Impersonate:

                        result = Permission.Impersonate;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Insert:

                        result = Permission.Insert;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Receive:

                        result = Permission.Receive;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.References:

                        result = Permission.References;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Select:

                        result = Permission.Select;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Send:

                        result = Permission.Send;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.TakeOwnership:

                        result = Permission.TakeOwnership;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.Update:

                        result = Permission.Update;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.ViewDefinition:

                        result = Permission.ViewDefinition;
                        break;
                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.ViewChangeTracking:

                        result = Permission.ViewChangeTracking;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ObjectPermissionSetValue.CreateSequence:

                        result = Permission.createSequence;
                        break;


                    default:

                        string message = String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unexpected ObjectPermissionSetValue: {0}", permissionValue);
                        // STrace.Assert(false, message);
                        break;

                }

                return result;

            }

            /// <summary>
            /// Get the Permission equivalent to the SMO DatabasePermissionSetValue
            /// </summary>
            /// <param name="permissionValue">The permission to find</param>
            /// <returns>The corresponding Permission instance</returns>
            public static Permission GetPermission(Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue permissionValue)
            {
                Permission result = null;

                switch (permissionValue)
                {
                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Alter:

                        result = Permission.Alter;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyApplicationRole:

                        result = Permission.AlterAnyApplicationRole;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyAssembly:

                        result = Permission.AlterAnyAssembly;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyAsymmetricKey:

                        result = Permission.AlterAnyAsymmetricKey;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyCertificate:

                        result = Permission.AlterAnyCertificate;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyDatabaseAudit:

                        result = Permission.AlterAnyDatabaseAudit;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyContract:
                        result = Permission.AlterAnyContract;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyDatabaseEventNotification:

                        result = Permission.AlterAnyDatabaseEventNotification;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyDataspace:

                        result = Permission.AlterAnyDataspace;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyExternalDataSource:

                        result = Permission.AlterAnyExternalDataSource;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyExternalFileFormat:

                        result = Permission.AlterAnyExternalFileFormat;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyFulltextCatalog:

                        result = Permission.AlterAnyFulltextCatalog;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyMask:

                        result = Permission.AlterAnyMask;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyMessageType:

                        result = Permission.AlterAnyMessageType;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyRemoteServiceBinding:

                        result = Permission.AlterAnyRemoteServiceBinding;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyRole:

                        result = Permission.AlterAnyRole;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyRoute:

                        result = Permission.AlterAnyRoute;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnySchema:

                        result = Permission.AlterAnySchema;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnySecurityPolicy:

                        result = Permission.AlterAnySecurityPolicy;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyService:

                        result = Permission.AlterAnyService;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnySymmetricKey:

                        result = Permission.AlterAnySymmetricKey;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyDatabaseDdlTrigger:

                        result = Permission.AlterAnyDatabaseDdlTrigger;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnyUser:

                        result = Permission.AlterAnyUser;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Authenticate:

                        result = Permission.Authenticate;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.BackupDatabase:

                        result = Permission.BackupDatabase;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.BackupLog:

                        result = Permission.BackupLog;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Checkpoint:

                        result = Permission.Checkpoint;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Connect:

                        result = Permission.Connect;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.ConnectReplication:

                        result = Permission.ConnectReplication;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Control:

                        result = Permission.Control;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateAggregate:

                        result = Permission.CreateAggregate;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateAssembly:

                        result = Permission.CreateAssembly;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateAsymmetricKey:

                        result = Permission.CreateAsymmetricKey;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateCertificate:

                        result = Permission.CreateCertificate;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateContract:

                        result = Permission.CreateContract;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateDatabase:

                        result = Permission.CreateDatabase;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateDatabaseDdlEventNotification:

                        result = Permission.CreateDatabaseDdlEventNotification;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateDefault:

                        result = Permission.CreateDefault;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateFunction:

                        result = Permission.CreateFunction;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateFulltextCatalog:

                        result = Permission.CreateFulltextCatalog;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateMessageType:

                        result = Permission.CreateMessageType;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateProcedure:

                        result = Permission.CreateProcedure;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateQueue:

                        result = Permission.CreateQueue;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateRemoteServiceBinding:

                        result = Permission.CreateRemoteServiceBinding;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateRole:

                        result = Permission.CreateRole;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateRoute:

                        result = Permission.CreateRoute;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateRule:

                        result = Permission.CreateRule;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateSchema:

                        result = Permission.CreateSchema;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateService:

                        result = Permission.CreateService;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateSymmetricKey:

                        result = Permission.CreateSymmetricKey;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateSynonym:

                        result = Permission.CreateSynonym;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateTable:

                        result = Permission.CreateTable;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateType:

                        result = Permission.CreateType;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateView:

                        result = Permission.CreateView;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.CreateXmlSchemaCollection:

                        result = Permission.CreateXmlSchemaCollection;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Delete:

                        result = Permission.Delete;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Execute:

                        result = Permission.Execute;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Insert:

                        result = Permission.Insert;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.References:

                        result = Permission.References;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Select:

                        result = Permission.Select;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Showplan:

                        result = Permission.ShowPlan;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.SubscribeQueryNotifications:

                        result = Permission.SubscribeQueryNotifications;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.TakeOwnership:

                        result = Permission.TakeOwnership;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Unmask:

                        result = Permission.Unmask;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.Update:

                        result = Permission.Update;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.ViewAnyColumnEncryptionKeyDefinition:

                        result = Permission.ViewAnyColumnEncryptionKeyDefinition;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.ViewAnyColumnMasterKeyDefinition:

                        result = Permission.ViewAnyColumnMasterKeyDefinition;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.ViewDefinition:

                        result = Permission.ViewDefinition;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.ViewDatabaseState:

                        result = Permission.ViewDatabaseState;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.AlterAnySensitivityClassification:

                        result = Permission.AlterAnySensitivityClassification;
                        break;

                    case Microsoft.SqlServer.Management.Smo.DatabasePermissionSetValue.ViewAnySensitivityClassification:

                        result = Permission.ViewAnySensitivityClassification;
                        break;

                    default:

                        string message = String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unexpected DatabasePermissionSetValue: {0}", permissionValue);
                        // STrace.Assert(false, message);
                        break;
                }

                return result;
            }

            /// <summary>
            /// Get the Permission equivalent to the SMO Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue
            /// </summary>
            /// <param name="permissionValue">The permission to find</param>
            /// <returns>The corresponding Permission instance</returns>
            public static Permission GetPermission(Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue permissionValue)
            {
                Permission result = null;

                switch (permissionValue)
                {
                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AdministerBulkOperations:

                        result = Permission.AdministerBulkOperations;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyServerAudit:

                        result = Permission.AlterAnyServerAudit;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyConnection:

                        result = Permission.AlterAnyConnection;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyCredential:

                        result = Permission.AlterAnyCredential;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyDatabase:

                        result = Permission.AlterAnyDatabase;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyEndpoint:

                        result = Permission.AlterAnyEndpoint;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyEventNotification:

                        result = Permission.AlterAnyEventNotification;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyEventSession:

                        result = Permission.AlterAnyEventSession;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyLinkedServer:

                        result = Permission.AlterAnyLinkedServer;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyLogin:

                        result = Permission.AlterAnyLogin;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyServerRole:

                        result = Permission.AlterAnyServerRole;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterResources:

                        result = Permission.AlterResources;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterServerState:

                        result = Permission.AlterServerState;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterSettings:

                        result = Permission.AlterSettings;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterTrace:

                        result = Permission.AlterTrace;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AuthenticateServer:

                        result = Permission.AuthenticateServer;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ConnectSql:

                        result = Permission.ConnectSql;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ControlServer:

                        result = Permission.ControlServer;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.CreateAnyDatabase:

                        result = Permission.CreateAnyDatabase;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.CreateDdlEventNotification:

                        result = Permission.CreateDdlEventNotification;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.CreateEndpoint:

                        result = Permission.CreateEndpoint;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.CreateTraceEventNotification:

                        result = Permission.CreateTraceEventNotification;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.CreateServerRole:

                        result = Permission.CreateServerRole;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ExternalAccessAssembly:

                        result = Permission.ExternalAccessAssembly;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.Shutdown:

                        result = Permission.Shutdown;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.UnsafeAssembly:

                        result = Permission.UnsafeAssembly;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ViewAnyDatabase:

                        result = Permission.ViewAnyDatabase;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ViewAnyDefinition:

                        result = Permission.ViewAnyDefinition;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ViewServerState:

                        result = Permission.ViewServerState;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.AlterAnyAvailabilityGroup:

                        result = Permission.AlterAnyAvailabilityGroup;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.CreateAvailabilityGroup:

                        result = Permission.CreateAvailabilityGroup;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.SelectAllUserSecurables:

                        result = Permission.SelectAllUserSecurables;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ConnectAnyDatabase:

                        result = Permission.ConnectAnyDatabase;
                        break;

                    case Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue.ImpersonateAnyLogin:

                        result = Permission.ImpersonateAnyLogin;
                        break;

                    default:

#if DEBUG
                        string message = String.Format(System.Globalization.CultureInfo.CurrentCulture, "Unexpected Microsoft.SqlServer.Management.Smo.ServerPermissionSetValue: {0}", permissionValue);
                        // STrace.Assert(false, message);
#endif
                        break;
                }

                return result;
            }


            #region object permission instances

            private static Permission alter = null;
            private static Permission connect = null;
            private static Permission control = null;
            private static Permission delete = null;
            private static Permission execute = null;
            private static Permission impersonate = null;
            private static Permission insert = null;
            private static Permission receive = null;
            private static Permission references = null;
            private static Permission select = null;
            private static Permission send = null;
            private static Permission takeOwnership = null;
            private static Permission update = null;
            private static Permission viewDefinition = null;
            #endregion

            #region database permission instances

            private static Permission alterAnyApplicationRole = null;
            private static Permission alterAnyAssembly = null;
            private static Permission alterAnyAsymmetricKey = null;
            private static Permission alterAnyCertificate = null;
            private static Permission alterAnyDatabaseAudit = null;
            private static Permission alterAnyContract = null;
            private static Permission alterAnyDatabaseDdlTrigger = null;
            private static Permission alterAnyDatabaseEventNotification = null;
            private static Permission alterAnyDataspace = null;
            private static Permission alterAnyExternalDataSource = null;
            private static Permission alterAnyExternalFileFormat = null;
            private static Permission alterAnyFulltextCatalog = null;
            private static Permission alterAnyMask = null;
            private static Permission alterAnyMessageType = null;
            private static Permission alterAnyRemoteServiceBinding = null;
            private static Permission alterAnyRole = null;
            private static Permission alterAnyRoute = null;
            private static Permission alterAnySchema = null;
            private static Permission alterAnySensitivityClassification = null;
            private static Permission alterAnyService = null;
            private static Permission alterAnySecurityPolicy = null;
            private static Permission alterAnySymmetricKey = null;
            private static Permission alterAnyUser = null;
            private static Permission authenticate = null;
            private static Permission backupDatabase = null;
            private static Permission backupLog = null;
            private static Permission checkpoint = null;
            private static Permission connectReplication = null;
            private static Permission createAggregate = null;
            private static Permission createAssembly = null;
            private static Permission createAsymmetricKey = null;
            private static Permission createCertificate = null;
            private static Permission createContract = null;
            private static Permission createDatabase = null;
            private static Permission createDatabaseDdlEventNotification = null;
            private static Permission createDefault = null;
            private static Permission createFulltextCatalog = null;
            private static Permission createFunction = null;
            private static Permission createMessageType = null;
            private static Permission createProcedure = null;
            private static Permission createQueue = null;
            private static Permission createRemoteServiceBinding = null;
            private static Permission createRole = null;
            private static Permission createRoute = null;
            private static Permission createRule = null;
            private static Permission createSchema = null;
            private static Permission createService = null;
            private static Permission createSymmetricKey = null;
            private static Permission createSynonym = null;
            private static Permission createSequence = null;
            private static Permission createTable = null;
            private static Permission createType = null;
            private static Permission createView = null;
            private static Permission createXmlSchemaCollection = null;
            private static Permission showPlan = null;
            private static Permission subscribeQueryNotifications = null;
            private static Permission unmask = null;
            private static Permission viewAnyColumnEncryptionKeyDefinition = null;
            private static Permission viewAnyColumnMasterKeyDefinition = null;
            private static Permission viewDatabaseState = null;
            private static Permission viewChangeTracking = null;            
            private static Permission viewAnySensitivityClassification = null;

            #endregion

            #region server permission instances

            private static Permission administerBulkOperations = null;
            private static Permission alterAnyServerAudit = null;
            private static Permission alterAnyConnection = null;
            private static Permission alterAnyCredential = null;
            private static Permission alterAnyDatabase = null;
            private static Permission alterAnyEndpoint = null;
            private static Permission alterAnyEventNotification = null;
            private static Permission alterAnyEventSession = null;
            private static Permission alterAnyLinkedServer = null;
            private static Permission alterAnyLogin = null;
            private static Permission alterAnyServerRole = null;
            private static Permission alterResources = null;
            private static Permission alterServerState = null;
            private static Permission alterSettings = null;
            private static Permission alterTrace = null;
            private static Permission authenticateServer = null;
            private static Permission connectSql = null;
            private static Permission controlServer = null;
            private static Permission createAnyDatabase = null;
            private static Permission createDdlEventNotification = null;
            private static Permission createEndpoint = null;
            private static Permission createTraceEventNotification = null;
            private static Permission createServerRole = null;
            private static Permission externalAccessAssembly = null;
            private static Permission shutdown = null;
            private static Permission viewAnyDatabase = null;
            private static Permission viewAnyDefinition = null;
            private static Permission viewServerState = null;
            private static Permission unsafeAssembly = null;
            private static Permission alterAnyAvailabilityGroup = null;
            private static Permission createAvailabilityGroup = null;
            private static Permission selectAllUserSecurables = null;
            private static Permission connectAnyDatabase = null;
            private static Permission impersonateAnyLogin = null;
            #endregion

            #region interface-based permission initialization methods

            private static void PopulateObjectPermissions()
            {
                alter = new Permission(SR.Permission_Alter, ObjectPermission.Alter, DatabasePermission.Alter);
                connect = new Permission(SR.Permission_Connect, ObjectPermission.Connect, DatabasePermission.Connect);
                control = new Permission(SR.Permission_Control, ObjectPermission.Control, DatabasePermission.Control);
                delete = new Permission(SR.Permission_Delete, ObjectPermission.Delete, DatabasePermission.Delete);
                execute = new Permission(SR.Permission_Execute, ObjectPermission.Execute, DatabasePermission.Execute);
                impersonate = new Permission(SR.Permission_Impersonate, ObjectPermission.Impersonate, null);
                insert = new Permission(SR.Permission_Insert, ObjectPermission.Insert, DatabasePermission.Insert);
                receive = new Permission(SR.Permission_Receive, ObjectPermission.Receive, null);
                references = new Permission(SR.Permission_References, ObjectPermission.References, DatabasePermission.References);
                select = new Permission(SR.Permission_Select, ObjectPermission.Select, DatabasePermission.Select);
                send = new Permission(SR.Permission_Send, ObjectPermission.Send, null);
                takeOwnership = new Permission(SR.Permission_TakeOwnership, ObjectPermission.TakeOwnership, DatabasePermission.TakeOwnership);
                update = new Permission(SR.Permission_Update, ObjectPermission.Update, DatabasePermission.Update);
                viewDefinition = new Permission(SR.Permission_ViewDefinition, ObjectPermission.ViewDefinition, DatabasePermission.ViewDefinition);
                viewChangeTracking = new Permission(SR.Permission_ViewChangeTracking, ObjectPermission.ViewChangeTracking, null);
                createSequence = new Permission(SR.Permission_CreateSequence, ObjectPermission.CreateSequence, null);
                objectPermissionsPopulated = true;
            }

            private static void PopulateDatabasePermissions()
            {
                alterAnyApplicationRole = new Permission(SR.Permission_AlterAnyApplicationRole, DatabasePermission.AlterAnyApplicationRole);
                alterAnyAssembly = new Permission(SR.Permission_AlterAnyAssembly, DatabasePermission.AlterAnyAssembly);
                alterAnyAsymmetricKey = new Permission(SR.Permission_AlterAnyAsymmetricKey, DatabasePermission.AlterAnyAsymmetricKey);
                alterAnyCertificate = new Permission(SR.Permission_AlterAnyCertificate, DatabasePermission.AlterAnyCertificate);
                alterAnyDatabaseAudit = new Permission(SR.Permission_AlterAnyDatabaseAudit, DatabasePermission.AlterAnyDatabaseAudit);
                alterAnyContract = new Permission(SR.Permission_AlterAnyContract, DatabasePermission.AlterAnyContract);
                alterAnyDatabaseDdlTrigger = new Permission(SR.Permission_AlterAnyDatabaseDdlTrigger, DatabasePermission.AlterAnyDatabaseDdlTrigger);
                alterAnyDatabaseEventNotification = new Permission(SR.Permission_AlterAnyDatabaseEventNotification, DatabasePermission.AlterAnyDatabaseEventNotification);
                alterAnyDataspace = new Permission(SR.Permission_AlterAnyDataspace, DatabasePermission.AlterAnyDataspace);
                alterAnyExternalDataSource = new Permission(SR.Permission_AlterAnyExternalDataSource, DatabasePermission.AlterAnyExternalDataSource);
                alterAnyExternalFileFormat = new Permission(SR.Permission_AlterAnyExternalFileFormat, DatabasePermission.AlterAnyExternalFileFormat);
                alterAnyFulltextCatalog = new Permission(SR.Permission_AlterAnyFulltextCatalog, DatabasePermission.AlterAnyFulltextCatalog);
                alterAnyMask = new Permission(SR.Permission_AlterAnyMask, DatabasePermission.AlterAnyMask);
                alterAnyMessageType = new Permission(SR.Permission_AlterAnyMessageType, DatabasePermission.AlterAnyMessageType);
                alterAnyRemoteServiceBinding = new Permission(SR.Permission_AlterAnyRemoteServiceBinding, DatabasePermission.AlterAnyRemoteServiceBinding);
                alterAnyRole = new Permission(SR.Permission_AlterAnyRole, DatabasePermission.AlterAnyRole);
                alterAnyRoute = new Permission(SR.Permission_AlterAnyRoute, DatabasePermission.AlterAnyRoute);
                alterAnySchema = new Permission(SR.Permission_AlterAnySchema, DatabasePermission.AlterAnySchema);
                alterAnySecurityPolicy = new Permission(SR.Permission_AlterAnySecurityPolicy, DatabasePermission.AlterAnySecurityPolicy);
                alterAnySensitivityClassification = new Permission(SR.Permission_AlterAnySensitivityClassification, DatabasePermission.AlterAnySensitivityClassification);
                alterAnyService = new Permission(SR.Permission_AlterAnyService, DatabasePermission.AlterAnyService);
                alterAnyUser = new Permission(SR.Permission_AlterAnyUser, DatabasePermission.AlterAnyUser);
                alterAnySymmetricKey = new Permission(SR.Permission_AlterAnySymmetricKey, DatabasePermission.AlterAnySymmetricKey);
                authenticate = new Permission(SR.Permission_Authenticate, DatabasePermission.Authenticate);
                backupDatabase = new Permission(SR.Permission_BackupDatabase, DatabasePermission.BackupDatabase);
                backupLog = new Permission(SR.Permission_BackupLog, DatabasePermission.BackupLog);
                checkpoint = new Permission(SR.Permission_Checkpoint, DatabasePermission.Checkpoint);
                connectReplication = new Permission(SR.Permission_ConnectReplication, DatabasePermission.ConnectReplication);
                createAggregate = new Permission(SR.Permission_CreateAggregate, DatabasePermission.CreateAggregate);
                createAssembly = new Permission(SR.Permission_CreateAssembly, DatabasePermission.CreateAssembly);
                createAsymmetricKey = new Permission(SR.Permission_CreateAsymmetricKey, DatabasePermission.CreateAsymmetricKey);
                createCertificate = new Permission(SR.Permission_CreateCertificate, DatabasePermission.CreateCertificate);
                createContract = new Permission(SR.Permission_CreateContract, DatabasePermission.CreateContract);
                createDatabase = new Permission(SR.Permission_CreateDatabase, DatabasePermission.CreateDatabase);
                createDatabaseDdlEventNotification = new Permission(SR.Permission_CreateDatabaseDdlEventNotification, DatabasePermission.CreateDatabaseDdlEventNotification);
                createDefault = new Permission(SR.Permission_CreateDefault, DatabasePermission.CreateDefault);
                createFulltextCatalog = new Permission(SR.Permission_CreateFulltextCatalog, DatabasePermission.CreateFulltextCatalog);
                createFunction = new Permission(SR.Permission_CreateFunction, DatabasePermission.CreateFunction);
                createMessageType = new Permission(SR.Permission_CreateMessageType, DatabasePermission.CreateMessageType);
                createProcedure = new Permission(SR.Permission_CreateProcedure, DatabasePermission.CreateProcedure);
                createQueue = new Permission(SR.Permission_CreateQueue, DatabasePermission.CreateQueue);
                createRemoteServiceBinding = new Permission(SR.Permission_CreateRemoteServiceBinding, DatabasePermission.CreateRemoteServiceBinding);
                createRole = new Permission(SR.Permission_CreateRole, DatabasePermission.CreateRole);
                createRoute = new Permission(SR.Permission_CreateRoute, DatabasePermission.CreateRoute);
                createRule = new Permission(SR.Permission_CreateRule, DatabasePermission.CreateRule);
                createSchema = new Permission(SR.Permission_CreateSchema, DatabasePermission.CreateSchema);
                createService = new Permission(SR.Permission_CreateService, DatabasePermission.CreateService);
                createSymmetricKey = new Permission(SR.Permission_CreateSymmetricKey, DatabasePermission.CreateSymmetricKey);
                createSynonym = new Permission(SR.Permission_CreateSynonym, DatabasePermission.CreateSynonym);
                createTable = new Permission(SR.Permission_CreateTable, DatabasePermission.CreateTable);
                createType = new Permission(SR.Permission_CreateType, DatabasePermission.CreateType);
                createView = new Permission(SR.Permission_CreateView, DatabasePermission.CreateView);
                createXmlSchemaCollection = new Permission(SR.Permission_CreateXmlSchemaCollection, DatabasePermission.CreateXmlSchemaCollection);
                showPlan = new Permission(SR.Permission_Showplan, DatabasePermission.Showplan);
                subscribeQueryNotifications = new Permission(SR.Permission_SubscribeQueryNotifications, DatabasePermission.SubscribeQueryNotifications);
                unmask = new Permission(SR.Permission_Unmask, DatabasePermission.Unmask);
                viewAnyColumnEncryptionKeyDefinition = new Permission(SR.Permission_ViewAnyColumnEncryptionKeyDefinition, DatabasePermission.ViewAnyColumnEncryptionKeyDefinition);
                viewAnyColumnMasterKeyDefinition = new Permission(SR.Permission_ViewAnyColumnMasterKeyDefinition, DatabasePermission.ViewAnyColumnMasterKeyDefinition);
                viewDatabaseState = new Permission(SR.Permission_ViewDatabaseState, DatabasePermission.ViewDatabaseState);                
                viewAnySensitivityClassification = new Permission(SR.Permission_ViewAnySensitivityClassification, DatabasePermission.ViewAnySensitivityClassification);

                databasePermissionsPopulated = true;
            }

            private static void PopulateServerPermissions()
            {
                administerBulkOperations = new Permission(SR.Permission_AdministerBulkOperations, ServerPermission.AdministerBulkOperations);
                alterAnyServerAudit = new Permission(SR.Permission_AlterAnyServerAudit, ServerPermission.AlterAnyServerAudit);
                alterAnyConnection = new Permission(SR.Permission_AlterAnyConnection, ServerPermission.AlterAnyConnection);
                alterAnyCredential = new Permission(SR.Permission_AlterAnyCredential, ServerPermission.AlterAnyCredential);
                alterAnyDatabase = new Permission(SR.Permission_AlterAnyDatabase, ServerPermission.AlterAnyDatabase);
                alterAnyEndpoint = new Permission(SR.Permission_AlterAnyEndpoint, ServerPermission.AlterAnyEndpoint);
                alterAnyEventNotification = new Permission(SR.Permission_AlterAnyEventNotification, ServerPermission.AlterAnyEventNotification);
                alterAnyEventSession = new Permission(SR.Permission_AlterAnyEventSession, ServerPermission.AlterAnyEventSession);
                alterAnyLinkedServer = new Permission(SR.Permission_AlterAnyLinkedServer, ServerPermission.AlterAnyLinkedServer);
                alterAnyLogin = new Permission(SR.Permission_AlterAnyLogin, ServerPermission.AlterAnyLogin);
                alterAnyServerRole = new Permission(SR.Permission_AlterAnyServerRole, ServerPermission.AlterAnyServerRole);
                alterResources = new Permission(SR.Permission_AlterResources, ServerPermission.AlterResources);
                alterServerState = new Permission(SR.Permission_AlterServerState, ServerPermission.AlterServerState);
                alterSettings = new Permission(SR.Permission_AlterSettings, ServerPermission.AlterSettings);
                alterTrace = new Permission(SR.Permission_AlterTrace, ServerPermission.AlterTrace);
                authenticateServer = new Permission(SR.Permission_AuthenticateServer, ServerPermission.AuthenticateServer);
                connectSql = new Permission(SR.Permission_ConnectSql, ServerPermission.ConnectSql);
                controlServer = new Permission(SR.Permission_ControlServer, ServerPermission.ControlServer);
                createAnyDatabase = new Permission(SR.Permission_CreateAnyDatabase, ServerPermission.CreateAnyDatabase);
                createDdlEventNotification = new Permission(SR.Permission_CreateDdlEventNotification, ServerPermission.CreateDdlEventNotification);
                createEndpoint = new Permission(SR.Permission_CreateEndpoint, ServerPermission.CreateEndpoint);
                createTraceEventNotification = new Permission(SR.Permission_CreateTraceEventNotification, ServerPermission.CreateTraceEventNotification);
                createServerRole = new Permission(SR.Permission_CreateServerRole, ServerPermission.CreateServerRole);
                externalAccessAssembly = new Permission(SR.Permission_ExternalAccessAssembly, ServerPermission.ExternalAccessAssembly);
                shutdown = new Permission(SR.Permission_Shutdown, ServerPermission.Shutdown);
                unsafeAssembly = new Permission(SR.Permission_UnsafeAssembly, ServerPermission.UnsafeAssembly);
                viewAnyDatabase = new Permission(SR.Permission_ViewAnyDatabase, ServerPermission.ViewAnyDatabase);
                viewAnyDefinition = new Permission(SR.Permission_ViewAnyDefinition, ServerPermission.ViewAnyDefinition);
                viewServerState = new Permission(SR.Permission_ViewServerState, ServerPermission.ViewServerState);
                alterAnyAvailabilityGroup = new Permission(SR.Permission_AlterAnyAvailabilityGroup, ServerPermission.AlterAnyAvailabilityGroup);
                createAvailabilityGroup = new Permission(SR.Permission_CreateAvailabilityGroup, ServerPermission.CreateAvailabilityGroup);
                selectAllUserSecurables = new Permission(SR.Permission_SelectAllUserSecurables, ServerPermission.SelectAllUserSecurables);
                connectAnyDatabase = new Permission(SR.Permission_ConnectAnyDatabase, ServerPermission.ConnectAnyDatabase);
                impersonateAnyLogin = new Permission(SR.Permission_ImpersonateAnyLogin, ServerPermission.ImpersonateAnyLogin);

                serverPermissionsPopulated = true;
            }

            #endregion

            #region object permission properties
            /// <summary>
            /// Gets the one and only Alter permission object
            /// </summary>
            public static Permission Alter
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alter;
                }
            }

            /// <summary>
            /// Gets the one and only Connect permission object
            /// </summary>
            public static Permission Connect
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return connect;
                }
            }
            /// <summary>
            /// Gets the one and only Control permission object
            /// </summary>
            public static Permission Control
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return control;
                }
            }
            /// <summary>
            /// Gets the one and only Delete permission object
            /// </summary>
            public static Permission Delete
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return delete;
                }
            }
            /// <summary>
            /// Gets the one and only Execute permission object
            /// </summary>
            public static Permission Execute
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return execute;
                }
            }
            /// <summary>
            /// Gets the one and only Impersonate permission object
            /// </summary>
            public static Permission Impersonate
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return impersonate;
                }
            }
            /// <summary>
            /// Gets the one and only Insert permission object
            /// </summary>
            public static Permission Insert
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return insert;
                }
            }
            /// <summary>
            /// Gets the one and only Receive permission object
            /// </summary>
            public static Permission Receive
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    return receive;
                }
            }
            /// <summary>
            /// Gets the one and only References permission object
            /// </summary>
            public static Permission References
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return references;
                }
            }
            /// <summary>
            /// Gets the one and only Select permission object
            /// </summary>
            public static Permission Select
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return select;
                }
            }
            /// <summary>
            /// Gets the one and only Send permission object
            /// </summary>
            public static Permission Send
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    return send;
                }
            }
            /// <summary>
            /// Gets the one and only TakeOwnership permission object
            /// </summary>
            public static Permission TakeOwnership
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return takeOwnership;
                }
            }
            /// <summary>
            /// Gets the one and only Update permission object
            /// </summary>
            public static Permission Update
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return update;
                }
            }
            /// <summary>
            /// Gets the one and only ViewDefinition permission object
            /// </summary>
            public static Permission ViewDefinition
            {
                get
                {
                    if (!objectPermissionsPopulated)
                    {
                        PopulateObjectPermissions();
                    }

                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return viewDefinition;
                }
            }

            #endregion

            #region database permission properties
            /// <summary>
            /// Gets the one and only AlterAnyApplicationRole permission object
            /// </summary>
            public static Permission AlterAnyApplicationRole
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyApplicationRole;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyAssembly permission object
            /// </summary>
            public static Permission AlterAnyAssembly
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyAssembly;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyAsymmetricKey permission object
            /// </summary>
            public static Permission AlterAnyAsymmetricKey
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyAsymmetricKey;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyCertificate permission object
            /// </summary>
            public static Permission AlterAnyCertificate
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyCertificate;
                }
            }

            /// <summary>
            /// Gets the only and only AlterAnyDatabaseAudit permission object
            /// </summary>
            public static Permission AlterAnyDatabaseAudit
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyDatabaseAudit;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyContract permission object
            /// </summary>
            public static Permission AlterAnyContract
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyContract;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyDatabaseDdlTrigger permission object
            /// </summary>
            public static Permission AlterAnyDatabaseDdlTrigger
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyDatabaseDdlTrigger;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyDatabaseEventNotification permission object
            /// </summary>
            public static Permission AlterAnyDatabaseEventNotification
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyDatabaseEventNotification;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyDataspace permission object
            /// </summary>
            public static Permission AlterAnyDataspace
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyDataspace;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyExternalDataSource permission object
            /// </summary>
            public static Permission AlterAnyExternalDataSource
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyExternalDataSource;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyExternalFileFormat permission object
            /// </summary>
            public static Permission AlterAnyExternalFileFormat
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyExternalFileFormat;
                }
            }

             /// <summary>
            /// Gets the one and only AlterAnyFulltextCatalog permission object
            /// </summary>
            public static Permission AlterAnyFulltextCatalog
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyFulltextCatalog;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyMask permission object
            /// </summary>
            public static Permission AlterAnyMask
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyMask;
            }
            }

            /// <summary>
            /// Gets the one and only AlterAnyMessageType permission object
            /// </summary>
            public static Permission AlterAnyMessageType
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyMessageType;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyRemoteServiceBinding permission object
            /// </summary>
            public static Permission AlterAnyRemoteServiceBinding
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyRemoteServiceBinding;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyRole permission object
            /// </summary>
            public static Permission AlterAnyRole
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyRole;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyRoute permission object
            /// </summary>
            public static Permission AlterAnyRoute
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyRoute;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnySchema permission object
            /// </summary>
            public static Permission AlterAnySchema
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnySchema;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnySecurityPolicy permission object
            /// </summary>
            public static Permission AlterAnySecurityPolicy
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnySecurityPolicy;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyService permission object
            /// </summary>
            public static Permission AlterAnyService
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyService;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnySymmetricKey permission object
            /// </summary>
            public static Permission AlterAnySymmetricKey
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnySymmetricKey;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyUser permission object
            /// </summary>
            public static Permission AlterAnyUser
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnyUser;
                }
            }

            /// <summary>
            /// Gets the one and only Authenticate permission object
            /// </summary>
            public static Permission Authenticate
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return authenticate;
                }
            }

            /// <summary>
            /// Gets the one and only BackupDatabase permission object
            /// </summary>
            public static Permission BackupDatabase
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return backupDatabase;
                }
            }

            /// <summary>
            /// Gets the one and only BackupLog permission object
            /// </summary>
            public static Permission BackupLog
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return backupLog;
                }
            }

            /// <summary>
            /// Gets the one and only Checkpoint permission object
            /// </summary>
            public static Permission Checkpoint
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return checkpoint;
                }
            }

            /// <summary>
            /// Gets the one and only ConnectReplication permission object
            /// </summary>
            public static Permission ConnectReplication
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return connectReplication;
                }
            }

            /// <summary>
            /// Gets the one and only CreateAggregate permission object
            /// </summary>
            public static Permission CreateAggregate
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createAggregate;
                }
            }

            /// <summary>
            /// Gets the one and only CreateAssembly permission object
            /// </summary>
            public static Permission CreateAssembly
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createAssembly;
                }
            }

            /// <summary>
            /// Gets the one and only CreateAsymmetricKey permission object
            /// </summary>
            public static Permission CreateAsymmetricKey
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createAsymmetricKey;
                }
            }

            /// <summary>
            /// Gets the one and only CreateCertificate permission object
            /// </summary>
            public static Permission CreateCertificate
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createCertificate;
                }
            }

            /// <summary>
            /// Gets the one and only CreateContract permission object
            /// </summary>
            public static Permission CreateContract
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createContract;
                }
            }

            /// <summary>
            /// Gets the one and only CreateDatabase permission object
            /// </summary>
            public static Permission CreateDatabase
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createDatabase;
                }
            }

            /// <summary>
            /// Gets the one and only CreateDatabaseDdlEventNotification permission object
            /// </summary>
            public static Permission CreateDatabaseDdlEventNotification
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createDatabaseDdlEventNotification;
                }
            }

            /// <summary>
            /// Gets the one and only CreateDefault permission object
            /// </summary>
            public static Permission CreateDefault
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createDefault;
                }
            }

            /// <summary>
            /// Gets the one and only CreateFulltextCatalog permission object
            /// </summary>
            public static Permission CreateFulltextCatalog
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createFulltextCatalog;
                }
            }

            /// <summary>
            /// Gets the one and only CreateFunction permission object
            /// </summary>
            public static Permission CreateFunction
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createFunction;
                }
            }

            /// <summary>
            /// Gets the one and only CreateMessageType permission object
            /// </summary>
            public static Permission CreateMessageType
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createMessageType;
                }
            }

            /// <summary>
            /// Gets the one and only CreateProcedure permission object
            /// </summary>
            public static Permission CreateProcedure
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createProcedure;
                }
            }

            /// <summary>
            /// Gets the one and only CreateQueue permission object
            /// </summary>
            public static Permission CreateQueue
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createQueue;
                }
            }

            /// <summary>
            /// Gets the one and only CreateRemoteServiceBinding permission object
            /// </summary>
            public static Permission CreateRemoteServiceBinding
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createRemoteServiceBinding;
                }
            }

            /// <summary>
            /// Gets the one and only CreateRole permission object
            /// </summary>
            public static Permission CreateRole
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createRole;
                }
            }

            /// <summary>
            /// Gets the one and only CreateRoute permission object
            /// </summary>
            public static Permission CreateRoute
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createRoute;
                }
            }

            /// <summary>
            /// Gets the one and only CreateRule permission object
            /// </summary>
            public static Permission CreateRule
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createRule;
                }
            }

            /// <summary>
            /// Gets the one and only CreateSchema permission object
            /// </summary>
            public static Permission CreateSchema
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createSchema;
                }
            }

            /// <summary>
            /// Gets the one and only CreateService permission object
            /// </summary>
            public static Permission CreateService
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createService;
                }
            }

            /// <summary>
            /// Gets the one and only CreateSymmetricKey permission object
            /// </summary>
            public static Permission CreateSymmetricKey
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createSymmetricKey;
                }
            }

            /// <summary>
            /// Gets the one and only CreateSynonym permission object
            /// </summary>
            public static Permission CreateSynonym
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createSynonym;
                }
            }

            /// <summary>
            /// Gets the one and only CreateSequence permission object
            /// </summary>
            public static Permission CreateSequence
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createSequence;
                }
            }

            /// <summary>
            /// Gets the one and only CreateTable permission object
            /// </summary>
            public static Permission CreateTable
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createTable;
                }
            }

            /// <summary>
            /// Gets the one and only CreateType permission object
            /// </summary>
            public static Permission CreateType
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createType;
                }
            }

            /// <summary>
            /// Gets the one and only CreateView permission object
            /// </summary>
            public static Permission CreateView
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createView;
                }
            }

            /// <summary>
            /// Gets the one and only CreateXmlSchemaCollection permission object
            /// </summary>
            public static Permission CreateXmlSchemaCollection
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return createXmlSchemaCollection;
                }
            }

            /// <summary>
            /// Gets the one and only ShowPlan permission object
            /// </summary>
            public static Permission ShowPlan
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return showPlan;
                }
            }

            /// <summary>
            /// Gets the one and only SubscribeQueryNotifications permission object
            /// </summary>
            public static Permission SubscribeQueryNotifications
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return subscribeQueryNotifications;
                }
            }

            /// <summary>
            /// Gets the one and only Unmask permission object
            /// </summary>
            public static Permission Unmask
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return unmask;
                }
            }

            /// <summary>
            /// Gets the one and only ViewAnyColumnEncryptionKeyDefinition permission object
            /// </summary>
            public static Permission ViewAnyColumnEncryptionKeyDefinition
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return viewAnyColumnEncryptionKeyDefinition;
                }
            }

            /// <summary>
            /// Gets the one and only ViewAnyColumnMasterKeyDefinition permission object
            /// </summary>
            public static Permission ViewAnyColumnMasterKeyDefinition
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return viewAnyColumnMasterKeyDefinition;
                }
            }

            /// <summary>
            /// Gets the one and only ViewDatabaseState permission object
            /// </summary>
            public static Permission ViewDatabaseState
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return viewDatabaseState;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnySensitivityClassification permission object
            /// </summary>
            public static Permission AlterAnySensitivityClassification
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return alterAnySensitivityClassification;
                }
            }

            /// <summary>
            /// Gets the one and only ViewAnySensitivityClassification permission object
            /// </summary>
            public static Permission ViewAnySensitivityClassification
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return viewAnySensitivityClassification;
                }
            }

            /// <summary>
            /// Gets the  ViewChangeTracking permission object
            /// </summary>
            public static Permission ViewChangeTracking
            {
                get
                {
                    if (!databasePermissionsPopulated)
                    {
                        PopulateDatabasePermissions();
                    }

                    return viewChangeTracking;
                }
            }


            #endregion

            #region server permission properties

            /// <summary>
            /// Gets the one and only AdministerBulkOperations permission object
            /// </summary>
            public static Permission AdministerBulkOperations
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return administerBulkOperations;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyServerAudit permission object
            /// </summary>
            public static Permission AlterAnyServerAudit
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyServerAudit;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyConnection permission object
            /// </summary>
            public static Permission AlterAnyConnection
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyConnection;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyCredential permission object
            /// </summary>
            public static Permission AlterAnyCredential
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyCredential;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyDatabase permission object
            /// </summary>
            public static Permission AlterAnyDatabase
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyDatabase;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyEndpoint permission object
            /// </summary>
            public static Permission AlterAnyEndpoint
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyEndpoint;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyEventNotification permission object
            /// </summary>
            public static Permission AlterAnyEventNotification
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyEventNotification;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyEventSession permission object
            /// </summary>
            public static Permission AlterAnyEventSession
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyEventSession;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyLinkedServer permission object
            /// </summary>
            public static Permission AlterAnyLinkedServer
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyLinkedServer;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyLogin permission object
            /// </summary>
            public static Permission AlterAnyLogin
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyLogin;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyServerRole permission object
            /// </summary>
            public static Permission AlterAnyServerRole
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyServerRole;
                }
            }

            /// <summary>
            /// Gets the one and only AlterResources permission object
            /// </summary>
            public static Permission AlterResources
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterResources;
                }
            }

            /// <summary>
            /// Gets the one and only AlterServerState permission object
            /// </summary>
            public static Permission AlterServerState
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterServerState;
                }
            }

            /// <summary>
            /// Gets the one and only AlterSettings permission object
            /// </summary>
            public static Permission AlterSettings
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterSettings;
                }
            }

            /// <summary>
            /// Gets the one and only AlterTrace permission object
            /// </summary>
            public static Permission AlterTrace
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterTrace;
                }
            }

            /// <summary>
            /// Gets the one and only AuthenticateServer permission object
            /// </summary>
            public static Permission AuthenticateServer
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return authenticateServer;
                }
            }

            /// <summary>
            /// Gets the one and only ConnectSql permission object
            /// </summary>
            public static Permission ConnectSql
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return connectSql;
                }
            }

            /// <summary>
            /// Gets the one and only ControlServer permission object
            /// </summary>
            public static Permission ControlServer
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return controlServer;
                }
            }

            /// <summary>
            /// Gets the one and only CreateAnyDatabase permission object
            /// </summary>
            public static Permission CreateAnyDatabase
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return createAnyDatabase;
                }
            }

            /// <summary>
            /// Gets the one and only CreateDdlEventNotification permission object
            /// </summary>
            public static Permission CreateDdlEventNotification
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return createDdlEventNotification;
                }
            }

            /// <summary>
            /// Gets the one and only CreateEndpoint permission object
            /// </summary>
            public static Permission CreateEndpoint
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return createEndpoint;
                }
            }

            /// <summary>
            /// Gets the one and only CreateTraceEventNotification permission object
            /// </summary>
            public static Permission CreateTraceEventNotification
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return createTraceEventNotification;
                }
            }

            /// <summary>
            /// Gets the one and only CreateServerRole permission object
            /// </summary>
            public static Permission CreateServerRole
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return createServerRole;
                }
            }

            /// <summary>
            /// Gets the one and only ExternalAccess permission object
            /// </summary>
            public static Permission ExternalAccessAssembly
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return externalAccessAssembly;
                }
            }

            /// <summary>
            /// Gets the one and only Shutdown permission object
            /// </summary>
            public static Permission Shutdown
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return shutdown;
                }
            }

            /// <summary>
            /// Gets the one and only ViewAnyDatabase permission object
            /// </summary>
            public static Permission ViewAnyDatabase
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return viewAnyDatabase;
                }
            }

            /// <summary>
            /// Gets the one and only ViewAnyDefinition permission object
            /// </summary>
            public static Permission ViewAnyDefinition
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return viewAnyDefinition;
                }
            }

            /// <summary>
            /// Gets the one and only ViewServerState permission object
            /// </summary>
            public static Permission ViewServerState
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return viewServerState;
                }
            }

            /// <summary>
            /// Gets the one and only UnsafeAssembly permission object
            /// </summary>
            public static Permission UnsafeAssembly
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return unsafeAssembly;
                }
            }

            /// <summary>
            /// Gets the one and only AlterAnyAvailabilityGroup permission object
            /// </summary>
            public static Permission AlterAnyAvailabilityGroup
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return alterAnyAvailabilityGroup;
                }
            }

            /// <summary>
            /// Gets the one and only CreateAvailabilityGroup permission object
            /// </summary>
            public static Permission CreateAvailabilityGroup
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return createAvailabilityGroup;
                }
            }

            /// <summary>
            /// Gets the one and only SelectAllUserSecurable permission object
            /// </summary>
            public static Permission SelectAllUserSecurables
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return selectAllUserSecurables;
                }
            }

            /// <summary>
            /// Gets the one and only ConnectAnyDatabase permission object
            /// </summary>
            public static Permission ConnectAnyDatabase
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return connectAnyDatabase;
                }
            }

            /// <summary>
            /// Gets the one and only ImpersonateAnyLogin permission object
            /// </summary>
            public static Permission ImpersonateAnyLogin
            {
                get
                {
                    if (!serverPermissionsPopulated)
                    {
                        PopulateServerPermissions();
                    }

                    return impersonateAnyLogin;
                }
            }

            #endregion

            #endregion

        }


        /// <summary>
        /// Adapter for applying permissions changes to SMO objects
        /// </summary>
        internal abstract class SmoPermissionsAdapter
        {
            /// <summary>
            /// Grant permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to grant</param>
            /// <param name="principal">The principal to whom the permissions are to be granted</param>
            /// <param name="withGrant"></param>
            public abstract void Grant(List<PermissionState> permissions, Principal principal, bool withGrant);
            /// <summary>
            /// Deny permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to deny</param>
            /// <param name="principal">The principal to whom permissions are to be denied</param>
            public abstract void Deny(List<PermissionState> permissions, Principal principal);
            /// <summary>
            /// Revoke permissions for a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to be revoked</param>
            /// <param name="principal">The principal whose permissions are to be revoked</param>
            public abstract void Revoke(List<PermissionState> permissions, Principal principal);

            /// <summary>
            /// Factory method to create a permissions adapter for a SMO object
            /// </summary>
            /// <param name="obj">The SMO object for which we are creating an adapter</param>
            /// <returns></returns>
            public static SmoPermissionsAdapter CreateAdapter(SqlSmoObject obj)
            {
                if (obj is IObjectPermission)
                {
                    return new ObjectPermissionsAdapter(obj);
                }
                else if (obj is Column column)
                {
                    return new TableViewColumnPermissionsAdapter(column);
                }
                else if (obj is Database database)
                {
                    return new DatabasePermissionsAdapter(database);
                }
                else if (obj is Microsoft.SqlServer.Management.Smo.Server server)
                {
                    return new ServerPermissionsAdapter(server);
                }
                else
                {
                    // STrace.Assert(false, "unexpected SqlSmoObject type");
                    return new NoPermissionsAdapter(obj);
                }
            }
        }


        /// <summary>
        /// Some objects do not have permissions methods yet, but will once interface-based
        /// permissions are implemented in SMO.  Once interface-based permissions are implemented,
        /// class specific adapters will be removed.
        /// </summary>
        internal class NoPermissionsAdapter : SmoPermissionsAdapter
        {
            public NoPermissionsAdapter(SqlSmoObject smoObject)
            {
            }

            public override void Grant(List<PermissionState> permissions, Principal principal, bool withGrant)
            {
            }

            public override void Deny(List<PermissionState> permissions, Principal principal)
            {
            }

            public override void Revoke(List<PermissionState> permissions, Principal principal)
            {
            }
        }


        /// <summary>
        /// Permission adapter for objects implementing IObjectPermission, such as
        /// Stored Procedures, Functions, etc.
        /// </summary>
        internal class ObjectPermissionsAdapter : SmoPermissionsAdapter
        {
            private IObjectPermission perm;
            private SqlSmoObject smoObject;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="obj">The subject of the permissions</param>
            public ObjectPermissionsAdapter(SqlSmoObject obj)
            {
                this.perm = (IObjectPermission)obj;
                this.smoObject = obj;
            }

            /// <summary>
            /// Grant permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to grant</param>
            /// <param name="principal">The principal to whom the permissions are to be granted</param>
            /// <param name="withGrant"></param>
            public override void Grant(List<PermissionState> permissions, Principal principal, bool withGrant)
            {
                foreach (PermissionState ps in permissions)
                {
                    if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                    {
                        if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                        {
                            this.perm.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                             new string[] { principal.Name },
                                             true,
                                             ps.CascadeNeeded);
                        }
                        else
                        {
                            this.perm.Grant(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                            principal.Name,
                                            withGrant);
                        }
                    }
                    else
                    {
                        if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                        {
                            this.perm.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                             principal.Name,
                                             true,
                                             ps.CascadeNeeded,
                                             ps.Grantor);
                        }
                        else
                        {
                            this.perm.Grant(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                            principal.Name,
                                            withGrant,
                                            ps.Grantor);
                        }
                    }
                }
            }

            /// <summary>
            /// Revoke permissions for a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to be revoked</param>
            /// <param name="principal">The principal whose permissions are to be revoked</param>
            public override void Revoke(List<PermissionState> permissions, Principal principal)
            {
                foreach (PermissionState ps in permissions)
                {
                    if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                    {
                        this.perm.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                         new string[] { principal.Name },
                                         false,
                                         ps.CascadeNeeded);
                    }
                    else
                    {
                        this.perm.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                         principal.Name,
                                         false,
                                         ps.CascadeNeeded,
                                         ps.Grantor);
                    }
                }
            }

            /// <summary>
            /// Deny permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to deny</param>
            /// <param name="principal">The principal to whom permissions are to be denied</param>
            public override void Deny(List<PermissionState> permissions, Principal principal)
            {
                foreach (PermissionState ps in permissions)
                {
                    this.perm.Deny(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                       new string[] { principal.Name },
                                       ps.CascadeNeeded);
                }
            }
        }

        /// <summary>
        /// Permissions adapter for a column on a table or view
        /// </summary>
        internal class TableViewColumnPermissionsAdapter : SmoPermissionsAdapter
        {
            private Column column;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="column">The column whose permissions we are modifying</param>
            public TableViewColumnPermissionsAdapter(Column column)
            {
                this.column = column;
            }

            /// <summary>
            /// Grant permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to grant</param>
            /// <param name="principal">The principal to whom the permissions are to be granted</param>
            /// <param name="withGrant"></param>
            public override void Grant(List<PermissionState> permissions, Principal principal, bool withGrant)
            {
                if (0 < permissions.Count)
                {
                    IColumnPermission parent = (IColumnPermission)this.column.Parent;

                    foreach (PermissionState ps in permissions)
                    {
                        if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                        {
                            if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                            {
                                parent.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                              principal.Name,
                                              new string[] { column.Name },
                                              true,
                                              ps.CascadeNeeded);
                            }
                            else
                            {
                                parent.Grant(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                             principal.Name,
                                             new string[] { column.Name },
                                             withGrant);
                            }
                        }
                        else
                        {
                            if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                            {
                                parent.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                              principal.Name,
                                              new string[] { column.Name },
                                              true,
                                              ps.CascadeNeeded,
                                              ps.Grantor);
                            }
                            else
                            {
                                parent.Grant(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                             principal.Name,
                                             new string[] { column.Name },
                                             withGrant,
                                             ps.Grantor);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Revoke permissions for a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to be revoked</param>
            /// <param name="principal">The principal whose permissions are to be revoked</param>
            public override void Revoke(List<PermissionState> permissions, Principal principal)
            {
                if (0 < permissions.Count)
                {
                    IColumnPermission parent = (IColumnPermission)this.column.Parent;
                    foreach (PermissionState ps in permissions)
                    {
                        if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                        {
                            parent.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                          principal.Name,
                                          new string[] { column.Name },
                                          false,
                                          ps.CascadeNeeded);
                        }
                        else
                        {
                            parent.Revoke(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                          principal.Name,
                                          new string[] { column.Name },
                                          false,
                                          ps.CascadeNeeded,
                                          ps.Grantor);
                        }
                    }
                }
            }

            /// <summary>
            /// Deny permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to deny</param>
            /// <param name="principal">The principal to whom permissions are to be denied</param>
            public override void Deny(List<PermissionState> permissions, Principal principal)
            {
                if (0 < permissions.Count)
                {
                    IColumnPermission parent = (IColumnPermission)this.column.Parent;
                    foreach (PermissionState ps in permissions)
                    {
                        parent.Deny(new ObjectPermissionSet(ps.Permission.SmoObjectPermission),
                                    principal.Name,
                                    new string[] { column.Name },
                                    ps.CascadeNeeded);
                    }
                }
            }
        }

        /// <summary>
        /// Permissions adapter for a database
        /// </summary>
        internal class DatabasePermissionsAdapter : SmoPermissionsAdapter
        {
            private Database database;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="database">The object whose permissions we are modifying</param>
            public DatabasePermissionsAdapter(Database database)
            {
                this.database = database;
            }

            /// <summary>
            /// Grant permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to grant</param>
            /// <param name="principal">The principal to whom the permissions are to be granted</param>
            /// <param name="withGrant"></param>
            public override void Grant(List<PermissionState> permissions, Principal principal, bool withGrant)
            {
                foreach (PermissionState ps in permissions)
                {
                    if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                    {
                        if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                        {
                            this.database.Revoke(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                                 principal.Name,
                                                 true,
                                                 ps.CascadeNeeded);
                        }
                        else
                        {
                            this.database.Grant(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                                principal.Name,
                                                withGrant);
                        }
                    }
                    else
                    {
                        if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                        {
                            this.database.Revoke(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                                 principal.Name,
                                                 true,
                                                 ps.CascadeNeeded,
                                                 ps.Grantor);
                        }
                        else
                        {
                            this.database.Grant(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                                principal.Name,
                                                withGrant,
                                                ps.Grantor);
                        }
                    }
                }
            }

            /// <summary>
            /// Revoke permissions for a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to be revoked</param>
            /// <param name="principal">The principal whose permissions are to be revoked</param>
            public override void Revoke(List<PermissionState> permissions, Principal principal)
            {
                foreach (PermissionState ps in permissions)
                {
                    if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                    {
                        this.database.Revoke(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                             principal.Name,
                                             false,
                                             ps.CascadeNeeded);
                    }
                    else
                    {
                        this.database.Revoke(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                             principal.Name,
                                             false,
                                             ps.CascadeNeeded,
                                             ps.Grantor);
                    }
                }
            }

            /// <summary>
            /// Deny permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to deny</param>
            /// <param name="principal">The principal to whom permissions are to be denied</param>
            public override void Deny(List<PermissionState> permissions, Principal principal)
            {
                foreach (PermissionState ps in permissions)
                {
                    this.database.Deny(new DatabasePermissionSet(ps.Permission.SmoDatabasePermission),
                                       principal.Name,
                                       ps.CascadeNeeded);
                }
            }
        }

        /// <summary>
        /// Permissions adapter for a server
        /// </summary>
        internal class ServerPermissionsAdapter : SmoPermissionsAdapter
        {
            private Microsoft.SqlServer.Management.Smo.Server server;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="server">The server whose permissions we are modifying</param>
            public ServerPermissionsAdapter(Microsoft.SqlServer.Management.Smo.Server server)
            {
                this.server = server;
            }

            /// <summary>
            /// Grant permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to grant</param>
            /// <param name="principal">The principal to whom the permissions are to be granted</param>
            /// <param name="withGrant"></param>
            public override void Grant(List<PermissionState> permissions, Principal principal, bool withGrant)
            {
                foreach (PermissionState ps in permissions)
                {
                    if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                    {
                        if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                        {
                            this.server.Revoke(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                               principal.Name,
                                               true,
                                               ps.CascadeNeeded);
                        }
                        else
                        {
                            this.server.Grant(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                              principal.Name,
                                              withGrant);
                        }
                    }
                    else
                    {
                        if (!withGrant && ps.OriginalState == PermissionStatus.WithGrant)
                        {
                            this.server.Revoke(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                               principal.Name,
                                               true,
                                               ps.CascadeNeeded,
                                               ps.Grantor);
                        }
                        else
                        {
                            this.server.Grant(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                              principal.Name,
                                              withGrant,
                                              ps.Grantor);
                        }
                    }
                }
            }

            /// <summary>
            /// Revoke permissions for a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to be revoked</param>
            /// <param name="principal">The principal whose permissions are to be revoked</param>
            public override void Revoke(List<PermissionState> permissions, Principal principal)
            {
                foreach (PermissionState ps in permissions)
                {
                    if (string.IsNullOrEmpty(ps.Grantor) || ps.Grantor == ps.Securable.ExpectedGrantor)
                    {
                        this.server.Revoke(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                           principal.Name,
                                           false,
                                           ps.CascadeNeeded);
                    }
                    else
                    {
                        this.server.Revoke(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                           principal.Name,
                                           false,
                                           ps.CascadeNeeded,
                                           ps.Grantor);
                    }
                }
            }

            /// <summary>
            /// Deny permissions to a principal
            /// </summary>
            /// <param name="permissions">The set of permissions to deny</param>
            /// <param name="principal">The principal to whom permissions are to be denied</param>
            public override void Deny(List<PermissionState> permissions, Principal principal)
            {
                foreach (PermissionState ps in permissions)
                {
                    this.server.Deny(new ServerPermissionSet(ps.Permission.SmoServerPermission),
                                     principal.Name,
                                     ps.CascadeNeeded);
                }
            }
        }
    }
}


