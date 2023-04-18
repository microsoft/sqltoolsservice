//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Resources;
using System.Text;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// An enumeration of the SQL object types the search dialog knows how to look for
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    enum SearchableObjectType
    {
        AggregateFunction           = 0,
        ApplicationRole             = 1,
        Assembly                    = 2,
        Database                    = 3,
        DatabaseRole                = 4,
        Endpoint                    = 5,
        ExtendedStoredProcedure     = 6,
        FunctionInline              = 7,
        FunctionScalar              = 8,
        FunctionTable               = 9,
        Login                       = 10,
        Schema                      = 11,
        Server                      = 12,
        ServerRole                  = 13,
        StoredProcedure             = 14,
        Synonym                     = 15,
        Table                       = 16,
        //Trigger                       = 17,
        User                        = 17,
        View                        = 18,
        XmlSchemaCollection         = 19,
        Rule                        = 20,
        Default                     = 21,
        AgentJob                    = 22,
        Credential                  = 23,
        SymmetricKey                = 24,
        AsymmetricKey               = 25,
        Certificate                 = 26,
        UserDefinedDataType         = 27, 
        FullTextCatalog             = 28,
        LoginOnly                   = 29,
        UserDefinedTableType        = 30,
        ServiceQueue                = 31,
        Sequence                    = 32,
        AvailabilityGroup           = 33,
        SecurityPolicy              = 34,
        ExternalDataSource          = 35,
        ExternalFileFormat          = 36,
        LastType                    = 37    // do not add object types after LastType - insert ahead of LastType instead
    }

    /// <summary>
    /// Type-safe collection of SearchableObjectTypes
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SearchableObjectTypeCollection : System.Collections.CollectionBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="types">The types the collection should initially contain</param>
        public SearchableObjectTypeCollection(params SearchableObjectType[] types)
        {
            if (types != null)
            {
                foreach (SearchableObjectType type in types)
                {
                    this.List.Add(type);
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SearchableObjectTypeCollection() {}

        /// <summary>
        /// Indexer
        /// </summary>
#pragma warning disable IDE0026 // Use expression body for indexer
        public SearchableObjectType this[int index]
        {
            get
            {
                return ((SearchableObjectType) this.List[index]);
            }
        }
#pragma warning restore IDE0026 // Use expression body for indexer

        /// <summary>
        /// Add a type to the collection
        /// </summary>
        /// <param name="type">The type to add</param>
        /// <returns>The index of the type in the collection</returns>
        public int                  Add(SearchableObjectType type)
        {
            return this.List.Add(type);
        }

        /// <summary>
        /// Get the index of the type in the collection
        /// </summary>
        /// <param name="type">The type to find</param>
        /// <returns>The type's index</returns>
        public int                  IndexOf(SearchableObjectType type)
        {
            return this.List.IndexOf(type);
        }

        /// <summary>
        /// Insert a type into the collection at a particular index
        /// </summary>
        /// <param name="index">The index to insert the type</param>
        /// <param name="type">The type to insert</param>
        public void                 Insert(int index, SearchableObjectType type)
        {
            this.List.Insert(index, type);
        }

        /// <summary>
        /// Does the collection contain the type?
        /// </summary>
        /// <param name="type">The type to check for</param>
        /// <returns>True if the type is in the collection, false otherwise</returns>
        public bool                 Contains(SearchableObjectType type)
        {
            return this.List.Contains(type);
        }

        protected override void OnInsert(int index, Object value)  
        {
            if (value.GetType() != typeof(SearchableObjectType))
            {
                // STrace.Assert(false, "value is not a SearchableObjectType");
                throw new ArgumentException();
            }
        }

        protected override void OnRemove(int index, Object value)  
        {
            if (value.GetType() != typeof(SearchableObjectType))
            {
                // STrace.Assert(false, "value is not a SearchableObjectType");
                throw new ArgumentException();
            }
        }

        protected override void OnSet(int index, Object oldValue, Object newValue)  
        {
            if (newValue.GetType() != typeof(SearchableObjectType))
            {
                // STrace.Assert(false, "newValue is not a SearchableObjectType");
                throw new ArgumentException();
            }
        }

        protected override void OnValidate(Object value)  
        {
            if (value.GetType() != typeof(SearchableObjectType))
            {
                // STrace.Assert(false, "value is not a SearchableObjectType");
                throw new ArgumentException();
            }
        }


    }


    /// <summary>
    /// Descriptive information for each SQL object type
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SearchableObjectTypeDescription
    {
        // private Image   image;
        private string  typeNameSingular;
        private string  typeNamePlural;
        private string  urnObjectType;
        private string  specialRestrictions;
        private string  disallowSystemObjectsRestriction;
        private bool    isDatabaseObject;
        private bool    isSchemaObject;
        private const int YUKON = 9;

        /// <summary>
        /// The bitmap associated with the searchable object type
        /// </summary>
        // public Image Image
        // {
        //     get
        //     {
        //         return this.image;
        //     }
        // }

        /// <summary>
        /// The singular display name for the object type
        /// </summary>
        public string DisplayTypeNameSingular
        {
            get
            {
                return this.typeNameSingular;
            }
        }

        /// <summary>
        /// The plural display name for the object type
        /// </summary>
        public string DisplayTypeNamePlural
        {
            get
            {
                return this.typeNamePlural;
            }
        }

        /// <summary>
        /// Whether the object type is a server object (i.e. not contained by a database)
        /// </summary>
        public bool IsServerObject
        {
            get
            {
                return !this.isDatabaseObject;
            }
        }

        /// <summary>
        /// Whether the object type is a database object (i.e. contained by a database)
        /// </summary>
        public bool IsDatabaseObject
        {
            get
            {
                return this.isDatabaseObject;
            }
        }

        /// <summary>
        /// Whether the object type is a schema object (i.e. contained by a schema)
        /// </summary>
        public bool IsSchemaObject
        {
            get
            {
                return this.isSchemaObject;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="image">Bitmap for the object type</param>
        /// <param name="typeNameKey">The key to look up localized type names, e.g. "objectType.functionTable"</param>
        /// <param name="urnObjectType">The URN object type substring, e.g. "UserDefinedFunction"</param>
        /// <param name="specialRestrictions">Any special clauses needed for the URN, e.g. "@FunctionType='2'"</param>
        /// <param name="disallowSystemObjectsRestriction">Clause to restrict selection to non-system objects, e.g. "@IsSystemObject='false'"</param>
        /// <param name="isDatabaseObject">Whether the object is contained by a database</param>
        /// <param name="isSchemaObject">Whether the object is contained byt a schema</param>
        private SearchableObjectTypeDescription(
            // Image           image, 
            string          typeNameKey, 
            string          urnObjectType, 
            string          specialRestrictions, 
            string          disallowSystemObjectsRestriction,
            bool            isDatabaseObject,
            bool            isSchemaObject,
            ResourceManager resourceManager)
        {
            // STrace.Assert(image != null, "image is null");
            // STrace.Assert((typeNameKey != null) && (typeNameKey.Length != 0), "typeNameKey is null or empty");
            // STrace.Assert(specialRestrictions != null, "specialRestrictions is null");
            // STrace.Assert(disallowSystemObjectsRestriction != null, "disallowSystemObjectsRestriction is null");
            // STrace.Assert(resourceManager != null, "resourceManager is null");
            
            // this.image                              = image;
            this.urnObjectType                      = urnObjectType;
            this.specialRestrictions                = specialRestrictions;
            this.disallowSystemObjectsRestriction   = disallowSystemObjectsRestriction;
            this.isDatabaseObject                   = isDatabaseObject;
            this.isSchemaObject                     = isSchemaObject;

            this.typeNamePlural         = resourceManager.GetString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.plural", typeNameKey));
            this.typeNameSingular       =  resourceManager.GetString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.singular", typeNameKey));
        
            // STrace.Assert((this.typeNamePlural != null) && (this.typeNamePlural.Length != 0), "could not get plural type name");
            // STrace.Assert((this.typeNameSingular != null) && (this.typeNameSingular.Length != 0), "could not get singular type name");
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="image">Bitmap for the object type</param>
        /// <param name="typeNameKey">The key to look up localized type names, e.g. "objectType.functionTable"</param>
        /// <param name="urnObjectType">The URN object type substring, e.g. "UserDefinedFunction"</param>
        /// <param name="isDatabaseObject">Whether the object is contained by a database</param>
        /// <param name="isSchemaObject">Whether the object is contained byt a schema</param>
        private SearchableObjectTypeDescription(
            // Image           image, 
            string          typeNameKey, 
            string          urnObjectType, 
            bool            isDatabaseObject,
            bool            isSchemaObject,
            ResourceManager resourceManager)
        {
            // STrace.Assert(image != null, "image is null");
            // STrace.Assert((typeNameKey != null) && (typeNameKey.Length != 0), "typeNameKey is null or empty");
            // STrace.Assert(resourceManager != null, "resourceManager is null");

            // this.image = image;
            this.urnObjectType                      = urnObjectType;
            this.specialRestrictions                = String.Empty;
            this.disallowSystemObjectsRestriction   = String.Empty;
            this.isDatabaseObject                   = isDatabaseObject;
            this.isSchemaObject                     = isSchemaObject;

            this.typeNamePlural         = resourceManager.GetString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.plural", typeNameKey));
            this.typeNameSingular       =  resourceManager.GetString(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.singular", typeNameKey));

            // STrace.Assert((this.typeNamePlural != null) && (this.typeNamePlural.Length != 0), "could not get plural type name");
            // STrace.Assert((this.typeNameSingular != null) && (this.typeNameSingular.Length != 0), "could not get singular type name");
        }

        /// <summary>
        /// Get a URN string to enumerate instances of the object type
        /// </summary>
        /// <remarks>
        /// The version is used to enumerate all server objects of a particular type
        /// </remarks>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the results</param>
        /// <returns>The URN string to enumerate the type</returns>
        internal string     GetSearchUrn(bool includeSystemObjects)
        {
            // STrace.Assert(!this.isDatabaseObject, "wrong overload for enumerating database objects");

            StringBuilder builder = new StringBuilder("Server");

            if (0 != this.urnObjectType.Length)
            {
                builder.AppendFormat("/{0}", this.urnObjectType);
            }

            this.AppendRestrictions(
                builder,
                String.Empty,
                false,
                String.Empty,
                false,
                includeSystemObjects);

            return builder.ToString();
        }

        /// <summary>
        /// Get a URN string to enumerate instances of the object type
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all database objects of a particular type.
        /// </remarks>
        /// <param name="databaseName">The name of the database containing the objects that are to be enumerated</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the results</param>
        /// <returns>The URN string to enumerate the type</returns>
        internal string     GetSearchUrn(string databaseName, bool includeSystemObjects)
        {
            // STrace.Assert(this.isDatabaseObject, "wrong overload for enumerating server objects");
            // STrace.Assert(databaseName.Length != 0, "database name is empty");

            StringBuilder builder = new StringBuilder("Server");

            builder.AppendFormat("/Database[@Name='{0}']", Urn.EscapeString(databaseName));

            if (0 != this.urnObjectType.Length)
            {
                builder.AppendFormat("/{0}", this.urnObjectType);
            }

            this.AppendRestrictions(
                builder,
                String.Empty,
                false,
                String.Empty,
                false,
                includeSystemObjects);

            return builder.ToString();
        }

        /// <summary>
        /// Get a URN string to enumerate instances of the object type
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all server objects of a particular type with
        /// a particular name.
        /// </remarks>
        /// <param name="name">The name of the object to enumerate</param>
        /// <param name="exactName">True if the name is complete, false if it is a fragment the name should contain</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the results</param>
        /// <returns>The URN string to enumerate the type</returns>
        internal string     GetSearchUrn(string name, bool exactName, bool includeSystemObjects)
        {
            // STrace.Assert(!this.isDatabaseObject, "wrong overload for enumerating database objects");
            // STrace.Assert(name.Length != 0, "name is empty");

            StringBuilder builder = new StringBuilder("Server");

            if (0 != this.urnObjectType.Length)
            {
                builder.AppendFormat("/{0}", this.urnObjectType);
            }

            this.AppendRestrictions(
                builder,
                name,
                exactName,
                String.Empty,
                false,
                includeSystemObjects);

            return builder.ToString();
        }

        /// <summary>
        /// Get a URN string to enumerate instances of the object type
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all database objects of a particular type with
        /// a particular name.
        /// </remarks>
        /// <param name="databaseName">The name of the database containing the objects that are to be enumerated</param>
        /// <param name="name">The name of the object to enumerate</param>
        /// <param name="exactName">True if the name is complete, false if it is a fragment the name should contain</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the results</param>
        /// <returns>The URN string to enumerate the type</returns>
        internal string     GetSearchUrn(string databaseName, string name, bool exactName, bool includeSystemObjects)
        {
            // STrace.Assert(this.isDatabaseObject, "wrong overload for enumerating database objects");
            // STrace.Assert(databaseName.Length != 0, "database name is empty");

            StringBuilder builder = new StringBuilder("Server");

            builder.AppendFormat("/Database[@Name='{0}']/{1}", Urn.EscapeString(databaseName), this.urnObjectType);

            this.AppendRestrictions(
                builder,
                name,
                exactName,
                String.Empty,
                false,
                includeSystemObjects);

            return builder.ToString();
        }

        /// <summary>
        /// Get a URN string to enumerate instances of the object type
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all database objects of a particular type with
        /// a particular name and schema.  If name or schema is omitted, all names/schemas will be matched.
        /// </remarks>
        /// <param name="databaseName">The name of the database containing the objects that are to be enumerated</param>
        /// <param name="name">The name of the object to enumerate</param>
        /// <param name="exactName">True if the name is complete, false if it is a fragment that the name should contain</param>
        /// <param name="schema">The schema of the object to enumerate</param>
        /// <param name="exactSchema">True if the schema name is complete, false if it is a fragment that the schema name should contain</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the results</param>
        /// <returns>The URN string to enumerate the type</returns>
        internal string     GetSearchUrn(
            string  databaseName, 
            string  name, 
            bool    exactName, 
            string  schema, 
            bool    exactSchema,
            bool    includeSystemObjects)
        {
            // STrace.Assert(this.isDatabaseObject, "wrong overload for enumerating server objects");
            // STrace.Assert(databaseName.Length != 0, "database name is empty");

            StringBuilder builder = new StringBuilder("Server");

            builder.AppendFormat("/Database[@Name='{0}']/{1}", Urn.EscapeString(databaseName), this.urnObjectType);

            this.AppendRestrictions(
                builder,
                name,
                exactName,
                schema,
                exactSchema,
                includeSystemObjects);

            return builder.ToString();
        }

        /// <summary>
        /// Append the restriction portion of the URN (e.g. "[@Name='foo']") to the input StringBuilder
        /// </summary>
        /// <param name="builder">The StringBuilder that is forming the URN string</param>
        /// <param name="name">The name of the object to enumerate</param>
        /// <param name="exactName">True if the name is complete, false if it is a fragment that the name should contain</param>
        /// <param name="schema">The schema of the object to enumerate</param>
        /// <param name="exactSchema">True if the schema name is complete, false if it is a fragment that the schema name should contain</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the results</param>
        private void        AppendRestrictions(
            StringBuilder   builder,
            string          name, 
            bool            exactName, 
            string          schema, 
            bool            exactSchema, 
            bool            includeSystemObjects)
        {
            bool    hasNameRestriction          = (name.Length != 0);
            bool    hasSchemaRestriction        = (schema.Length != 0);
            bool    hasSpecialRestriction       = (0 != this.specialRestrictions.Length);
            bool    hasSystemObjectRestriction  = (!includeSystemObjects && (this.disallowSystemObjectsRestriction.Length != 0));

            if (hasNameRestriction || hasSchemaRestriction || hasSpecialRestriction || hasSystemObjectRestriction)
            {
                bool restrictionApplied = false;
                builder.Append("[");
    
                if (hasNameRestriction)
                {
                    if (exactName)
                    {
                        builder.AppendFormat("@Name='{0}'", Urn.EscapeString(name));
                    }
                    else
                    {
                        builder.AppendFormat("contains(@Name, '{0}')", Urn.EscapeString(name));
                    }  

                    restrictionApplied = true;
                }

                if (hasSchemaRestriction)
                {
                    if (restrictionApplied)
                    {
                        builder.Append(" and ");
                    }
                    
                    if (exactSchema)
                    {
                        builder.AppendFormat("@Schema='{0}'", Urn.EscapeString(schema));
                    }
                    else
                    {
                        builder.AppendFormat("contains(@Schema, '{0}')", Urn.EscapeString(schema));
                    }  

                    restrictionApplied = true;
                }
    
                if (hasSpecialRestriction)
                {
                    if (restrictionApplied)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, " and ({0}) ", this.specialRestrictions);
                    }
                    else
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, this.specialRestrictions);
                    }

                    restrictionApplied = true;
                }

                if (hasSystemObjectRestriction)
                {
                    if (restrictionApplied)
                    {
                        builder.Append(" and ");
                    }
                    
                    builder.Append(this.disallowSystemObjectsRestriction);
                }
    
                builder.Append("]");
            }
        }
        

        private static HybridDictionary typeToDescription;
        private static bool firstVersionSpecificTypeInfoUpdate = true;

        /// <summary>
        /// Updates the typedescription with version specific information.
        /// </summary>
        private static void UpdateVersionSpecificTypeDescription(Version serverVersion)
        {
            if (SearchableObjectTypeDescription.firstVersionSpecificTypeInfoUpdate)
            {
                ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", typeof(SearchableObjectTypeDescription).Assembly);
                // Color transparent = ResourceUtils.StandardBitmapTransparentColor;

                //Re-writing the value of SearchableObjectType.ServerRole in the Dictionary.
                SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ServerRole] =
                    new SearchableObjectTypeDescription(
                        // // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("Flexible_server_role.ico")).ToBitmap(),
                        "objectType.serverRole",
                        "Role",
                        string.Empty,
                        Utils.IsSql11OrLater(serverVersion.Major)
                            ? "@IsFixedRole=false()"
                            : serverVersion.Major >= YUKON
                                ? "@ID=2" //Public server role's ID is 2.
                                : string.Empty,
                        false,
                        false,
                        resourceManager);

                firstVersionSpecificTypeInfoUpdate = false;
            }
        }

        /// <summary>
        /// Static constructor
        /// </summary>
        static SearchableObjectTypeDescription()
        {
            if (typeToDescription == null)
            {
                SearchableObjectTypeDescription.typeToDescription = new HybridDictionary(25);
            }

            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", typeof(SearchableObjectTypeDescription).Assembly);
            // Color transparent = ResourceUtils.StandardBitmapTransparentColor;

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.AggregateFunction] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("ScalarValuedFunction.ico")).ToBitmap(),
                    "objectType.aggregateFunction",
                    "UserDefinedAggregate",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ApplicationRole] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("application_role_16x.ico")).ToBitmap(),
                    "objectType.applicationRole",
                    "ApplicationRole",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Assembly] =
                new SearchableObjectTypeDescription(
                    // // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("assemblies.ico")).ToBitmap(),
                    "objectType.assembly",
                    "SqlAssembly",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.AsymmetricKey] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("asymmetric_key.ico")).ToBitmap(),
                    "objectType.asymmetricKey",
                    "AsymmetricKey",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Certificate] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("certificate.ico")).ToBitmap(),
                    "objectType.certificate",
                    "Certificate",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Database] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("database.ico")).ToBitmap(),
                    "objectType.database",
                    "Database",
                    String.Empty,
                    "@IsSystemObject=false()",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.AgentJob] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("jobs.ico")).ToBitmap(),
                    "objectType.agentjob",
                    "JobServer/Job",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.DatabaseRole] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("database_roles_16x.ico")).ToBitmap(),
                    "objectType.databaseRole",
                    "Role",
                    String.Empty,
                    "@IsFixedRole=false()",
                    true,
                    false,
                    resourceManager);

            //Without version info, we can't have system object Urn as it differs with version.
            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ServerRole] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("Flexible_server_role.ico")).ToBitmap(),
                    "objectType.serverRole",
                    "Role",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Endpoint] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("endpoint.ico")).ToBitmap(),
                    "objectType.endpoint",
                    "Endpoint",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ExtendedStoredProcedure] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("user_extended_stored_proc.ico")).ToBitmap(),
                    "objectType.extendedStoredProcedure",
                    "ExtendedStoredProcedure",
                    String.Empty,
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ExternalDataSource] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("ExternalDataSource.ico")).ToBitmap(),
                    "objectType.externalDataSource",
                    "ExternalDataSource",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ExternalFileFormat] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("ExternalFileFormat.ico")).ToBitmap(),
                    "objectType.externalFileFormat",
                    "ExternalFileFormat",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.FullTextCatalog] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("full_text_catalog.ico")).ToBitmap(),
                    "objectType.fullTextCatalog",
                    "FullTextCatalog",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.FunctionInline] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("table_valued_function.ico")).ToBitmap(),
                    "objectType.functionInline",
                    "UserDefinedFunction",
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "@FunctionType='{0}'", (int)UserDefinedFunctionType.Inline),
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.FunctionScalar] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("ScalarValuedFunction.ico")).ToBitmap(),
                    "objectType.functionScalar",
                    "UserDefinedFunction",
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "@FunctionType='{0}'", (int)UserDefinedFunctionType.Scalar),
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.FunctionTable] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("table_valued_function.ico")).ToBitmap(),
                    "objectType.functionTable",
                    "UserDefinedFunction",
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "@FunctionType='{0}'", (int)UserDefinedFunctionType.Table),
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Login] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("log_in_16x.ico")).ToBitmap(),
                    "objectType.login",
                    "Login",
                    String.Empty,
                    "@IsSystemObject=false()",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.LoginOnly] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("log_in_16x.ico")).ToBitmap(),
                    "objectType.login",
                    "Login",
                    "@LoginType = 2 or @LoginType = 0",
                    "@IsSystemObject=false()",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Schema] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("database_schema.ico")).ToBitmap(),
                    "objectType.schema",
                    "Schema",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Server] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("server.ico")).ToBitmap(),
                    "objectType.server",
                    String.Empty,
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.SecurityPolicy] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("securitypolicy.ico")).ToBitmap(),
                    "objectType.securityPolicy",
                    "SecurityPolicy",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.ServiceQueue] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("queue.ico")).ToBitmap(),
                    "objectType.serviceQueue",
                    "ServiceBroker/ServiceQueue",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.StoredProcedure] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("stored_procedure.ico")).ToBitmap(),
                    "objectType.storedProcedure",
                    "StoredProcedure",
                    String.Empty,
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Synonym] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("synonym.ico")).ToBitmap(),
                    "objectType.synonym",
                    "Synonym",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Sequence] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("sequence.ico")).ToBitmap(),
                    "objectType.sequence",
                    "Sequence",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Table] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("table.ico")).ToBitmap(),
                    "objectType.table",
                    "Table",
                    String.Empty,
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.User] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("user_16x.ico")).ToBitmap(),
                    "objectType.user",
                    "User",
                    String.Empty,
                    "(@IsSystemObject=false() or @Name='guest')",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.UserDefinedDataType] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("user_defined_data_type.ico")).ToBitmap(),
                    "objectType.userDefinedDataType",
                    "UserDefinedDataType",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.View] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("view.ico")).ToBitmap(),
                    "objectType.view",
                    "View",
                    String.Empty,
                    "@IsSystemObject=false()",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.XmlSchemaCollection] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("XML_schemas.ico")).ToBitmap(),
                    "objectType.xmlSchemaCollection",
                    "XmlSchemaCollection",
                    true,
                    true,
                    resourceManager);
            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Rule] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("rule.ico")).ToBitmap(),
                    "objectType.rule",
                    "Rule",
                    true,
                    true,
                    resourceManager);
            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Default] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("defaults_16x.ico")).ToBitmap(),
                    "objectType.default",
                    "Default",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.Credential] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("credential.ico")).ToBitmap(),
                    "objectType.credential",
                    "Credential",
                    false,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.SymmetricKey] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("symmetric_key.ico")).ToBitmap(),
                    "objectType.symmetricKey",
                    "SymmetricKey",
                    true,
                    false,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.UserDefinedTableType] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("table.ico")).ToBitmap(),
                    "objectType.userDefinedTableType",
                    "UserDefinedTableType",
                    true,
                    true,
                    resourceManager);

            SearchableObjectTypeDescription.typeToDescription[SearchableObjectType.AvailabilityGroup] =
                new SearchableObjectTypeDescription(
                    // DpiUtil.GetScaledIcon(ResourceUtils.LoadIcon("Availability_Group.ico")).ToBitmap(),
                    "objectType.AvailabilityGroup",
                    "AvailabilityGroup",
                    false,
                    false,
                    resourceManager);
        }

        /// <summary>
        /// Get the type description for a particular searchable object type
        /// </summary>
        /// <param name="objectType">The searchable object type</param>
        /// <returns>The type description</returns>
        public static SearchableObjectTypeDescription   GetDescription(SearchableObjectType objectType)
        {
            // STrace.Assert(
                // SearchableObjectTypeDescription.typeToDescription.Contains(objectType), 
                // "unexpected object type - did you add the object type description?");
            
            return (SearchableObjectTypeDescription) SearchableObjectTypeDescription.typeToDescription[objectType];
        }

        /// <summary>
        /// Get the type description for a particular searchable object type
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="objectType">The searchable object type</param>
        /// <returns>The type description</returns>
        public static SearchableObjectTypeDescription GetDescription(object connectionInfo, SearchableObjectType objectType)
        {
            // STrace.Assert(
                // SearchableObjectTypeDescription.typeToDescription.Contains(objectType),
                // "unexpected object type - did you add the object type description?");

            SearchableObjectTypeDescription.UpdateVersionSpecificTypeDescription(PermissionsData.Securable.GetServerVersion(connectionInfo));
            return (SearchableObjectTypeDescription)SearchableObjectTypeDescription.typeToDescription[objectType];
        }
    }

    /// <summary>
    /// Selection status for an object type
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SearchableObjectTypeSelection
    {
        private SearchableObjectType    objectType;
        private bool                    isSelected;

        /// <summary>
        /// The object type
        /// </summary>
        public SearchableObjectType SearchableObjectType
        {
            get
            {
                return this.objectType;
            }
        }

        /// <summary>
        /// Whether the object type has been selected
        /// </summary>
        public bool                 IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                this.isSelected = value;
            }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="objectType">The type of the object</param>
        /// <param name="isSelected">Whether it is selected</param>
        public SearchableObjectTypeSelection(SearchableObjectType objectType, bool isSelected)
        {
            this.objectType = objectType;
            this.isSelected = isSelected;
        }
    }

    /// <summary>
    /// A SQL server object
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SearchableObject : IComparable
    {
        private SearchableObjectType                objectType;
        private Urn                                 urn;
        private System.Globalization.CompareOptions compareOptions;

        /// <summary>
        /// The type of the object
        /// </summary>
        public SearchableObjectType SearchableObjectType
        {
            get
            {
                return this.objectType;
            }
        }

        /// <summary>
        /// The display text for the type of the object
        /// </summary>
        public string               TypeName
        {
            get
            {
                return SearchableObjectTypeDescription.GetDescription(objectType).DisplayTypeNameSingular;
            }
        }
        /// <summary>
        /// The name of the object
        /// </summary>
        public string               Name
        {
            get
            {
                return this.urn.GetAttribute("Name");
            }
        }

        /// <summary>
        /// The schema of the object
        /// </summary>
        public string               Schema
        {
            get
            {
                string result = String.Empty;
                
                if (SearchableObjectTypeDescription.GetDescription(this.objectType).IsSchemaObject)
                {
                    result = this.urn.GetAttribute("Schema");
                }

                return result;
            }
        }

        /// <summary>
        /// The name of the database containing the object, or an empty string
        /// if database doesn't apply
        /// </summary>
        public string               DatabaseName
        {
            get
            {
                string result = String.Empty;

                if (!SearchableObjectTypeDescription.GetDescription(this.objectType).IsServerObject)
                {
                    Urn currentUrn = this.urn;

                    while (
                        (currentUrn != null) && 
                        (0 != String.Compare(currentUrn.Type, "Database", StringComparison.OrdinalIgnoreCase)))
                    {
                        currentUrn = currentUrn.Parent;

                        // STrace.Assert(currentUrn != null, "ran off the top of the xpath");
                    }
                    
                    if (currentUrn != null)
                    {
                        // STrace.Assert(
                            // (0 == String.Compare(currentUrn.Type, "Database", StringComparison.OrdinalIgnoreCase)),
                            // "currentUrn is not a database!");

                        result = currentUrn.GetAttribute("Name");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The URN for the object
        /// </summary>
        public Urn                  Urn
        {
            get
            {
                return this.urn;
            }
        }

        /// <summary>
        /// The icon for the object
        /// </summary>
        // public Image                Image
        // {
        //     get
        //     {
        //         return SearchableObjectTypeDescription.GetDescription(this.objectType).Image;
        //     }
        // }

        /// <summary>
        /// The CompareOptions that should be used to compare this SearchableObject
        /// </summary>
        internal System.Globalization.CompareOptions CompareOptions
        {
            get
            {
                return this.compareOptions;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="objectType">The type of the object</param>
        /// <param name="urn">The urn for the object</param>
        /// <param param name="sqlCollation">The collation used to compare object names</param>
        public SearchableObject(SearchableObjectType objectType, string urn, string sqlCollation)
        {
            this.objectType = objectType;
            this.urn        = new Urn(urn);
            this.compareOptions = SqlSupport.GetCompareOptionsFromCollation(sqlCollation);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="objectType">The type of the object</param>
        /// <param name="urn">The urn for the object</param>
        /// <param name="compareOptions">The CompareOptions used to compare object names</param>
        public SearchableObject(SearchableObjectType objectType, string urn, System.Globalization.CompareOptions compareOptions)
        {
            this.objectType     = objectType;
            this.urn            = urn;
            this.compareOptions = compareOptions;
        }

        /// <summary>
        /// Determine whether this object should be considered less than, equal to, or greater than another object
        /// </summary>
        /// <param name="obj">The object to compare to</param>
        /// <returns>less than zero if this is less, 0 if equal, greater than zero if this is greater</returns>
        public int CompareTo(object obj)
        {
            int result = 0;
            
            if (obj.GetType() == this.GetType())
            {
                SearchableObject                    other       = (SearchableObject) obj;
                SqlCollationSensitiveStringComparer comparer    = new SqlCollationSensitiveStringComparer(this.compareOptions);

                string thisSchema   = this.Schema;
                string otherSchema  = other.Schema;

                if ((thisSchema.Length != 0) && (otherSchema.Length != 0))
                {
                    result = comparer.Compare(thisSchema, otherSchema);
                }
                else if ((thisSchema.Length == 0) && (otherSchema.Length == 0))
                {
                    result = 0;
                }
                else if (thisSchema.Length == 0) 
                {
                    result = 1;
                }
                else
                {
                    result = -1;
                }

                if (0 == result)
                {
                    result = comparer.Compare(this.Name, other.Name);
                }

                if (0 == result)
                {
                    result = this.objectType.CompareTo(((SearchableObject) obj).objectType);
                }
            }
            else
            {
                throw new ArgumentException("unexpected object type", "obj");
            }

            return result;
        }

        /// <summary>
        /// Get the object's two-part name (e.g. "[schema].[name]") or one-part name
        /// for non-schema objects (e.g. "[name]")
        /// </summary>
        /// <returns>The formatted multi-part name</returns>
        public override string ToString()
        {
            return (new ObjectName(this)).ToString();   
        }

        /// <summary>
        /// Search for all object instances matching the input criteria and add them to the collection
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all server objects of a particular type
        /// </remarks>
        /// <param name="collection>The collection to which found objects are to be added</param>
        /// <param name="type">The type of objects to search for</param>
        /// <param name="connectionInfo">Connection information used to enumerate objects</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the search results</param>
        /// <returns>True if any objects were found, false otherwise</returns>
        public static bool Search(
            SearchableObjectCollection  collection, 
            SearchableObjectType        type, 
            object                      connectionInfo,
            bool                        includeSystemObjects)
        {
            string urn = SearchableObjectTypeDescription.GetDescription(connectionInfo, type).GetSearchUrn(includeSystemObjects);
            return SearchImpl(collection, type, connectionInfo, urn, String.Empty);
        }

        /// <summary>
        /// Search for all object instances matching the input criteria and add them to the collection
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all database objects of a particular type
        /// </remarks>
        /// <param name="collection>The collection to which found objects are to be added</param>
        /// <param name="type">The type of objects to search for</param>
        /// <param name="connectionInfo">Connection information used to enumerate objects</param>
        /// <param name="databaseName">The name of the database containing the objects</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the search results</param>
        /// <returns>True if any objects were found, false otherwise</returns>
        public static bool Search(
            SearchableObjectCollection  collection, 
            SearchableObjectType        type, 
            object                      connectionInfo, 
            string                      databaseName,
            bool                        includeSystemObjects)
        {
            string urn = SearchableObjectTypeDescription.GetDescription(connectionInfo, type).GetSearchUrn(databaseName, includeSystemObjects);
            return SearchImpl(collection, type, connectionInfo, urn, databaseName);
        }

        /// <summary>
        /// Search for all object instances matching the input criteria and add them to the collection
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all server objects of a particular type with a
        /// particular name
        /// </remarks>
        /// <param name="collection>The collection to which found objects are to be added</param>
        /// <param name="type">The type of objects to search for</param>
        /// <param name="connectionInfo">Connection information used to enumerate objects</param>
        /// <param name="objectName">The name of the object we're looking for</param>
        /// <param name"exactNameMatch">
        /// If true, search for an object whose name exactly matches objectName, 
        /// otherwise search for an object whose name contains objectName
        /// </param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the search results</param>
        /// <returns>True if any objects were found, false otherwise</returns>
        public static bool Search(
            SearchableObjectCollection  collection, 
            SearchableObjectType        type, 
            object                      connectionInfo, 
            string                      objectName, 
            bool                        exactNameMatch,
            bool                        includeSystemObjects)
        {
            string urn = SearchableObjectTypeDescription.GetDescription(connectionInfo, type).GetSearchUrn(
                objectName, 
                exactNameMatch, 
                includeSystemObjects);

            return SearchImpl(collection, type, connectionInfo, urn, String.Empty);
        }

        /// <summary>
        /// Search for all object instances matching the input criteria and add them to the collection
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all non-schema database objects of a particular type with a
        /// particular name.
        /// </remarks>
        /// <param name="collection>The collection to which found objects are to be added</param>
        /// <param name="type">The type of objects to search for</param>
        /// <param name="connectionInfo">Connection information used to enumerate objects</param>
        /// <param name="databaseName">The name of the database containing the objects</param>
        /// <param name="objectName">The name of the object we're looking for</param>
        /// <param name"exactNameMatch">
        /// If true, search for an object whose name exactly matches objectName, 
        /// otherwise search for an object whose name contains objectName
        /// </param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the search results</param>
        /// <returns>True if any objects were found, false otherwise</returns>
        public static bool Search(
            SearchableObjectCollection  collection, 
            SearchableObjectType        type, 
            object                      connectionInfo, 
            string                      databaseName,
            string                      objectName, 
            bool                        exactNameMatch,
            bool                        includeSystemObjects)
        {
            string urn = SearchableObjectTypeDescription.GetDescription(connectionInfo, type).GetSearchUrn(
                databaseName, 
                objectName, 
                exactNameMatch, 
                includeSystemObjects);

            return SearchImpl(collection, type, connectionInfo, urn, databaseName);
        }

        /// <summary>
        /// Search for all object instances matching the input criteria and add them to the collection
        /// </summary>
        /// <remarks>
        /// This overload is used to enumerate all database objects of a particular type with a
        /// particular object name and/or schema name.
        /// </remarks>
        /// <param name="collection>The collection to which found objects are to be added</param>
        /// <param name="type">The type of objects to search for</param>
        /// <param name="connectionInfo">Connection information used to enumerate objects</param>
        /// <param name="databaseName">The name of the database containing the objects</param>
        /// <param name="objectName">The name of the object we're looking for</param>
        /// <param name"exactNameMatch">
        /// If true, search for an object whose name exactly matches objectName, 
        /// otherwise search for an object whose name contains objectName
        /// </param>
        /// <param name="schemaName">The schema of the object we're looking for</param>
        /// <param name"exactSchemaMatch">
        /// If true, search for an object whose schema exactly matches schemaName, 
        /// otherwise search for an object whose schema contains schemaName
        /// </param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in the search results</param>
        /// <returns>True if any objects were found, false otherwise</returns>
        public static bool Search(
            SearchableObjectCollection  collection, 
            SearchableObjectType        type, 
            object                      connectionInfo, 
            string                      databaseName,
            string                      objectName, 
            bool                        exactNameMatch,
            string                      schemaName,
            bool                        exactSchemaMatch,
            bool                        includeSystemObjects)
        {
            string urn = SearchableObjectTypeDescription.GetDescription(connectionInfo, type).GetSearchUrn(
                databaseName, 
                objectName, 
                exactNameMatch,
                schemaName,
                exactSchemaMatch, 
                includeSystemObjects);

            return SearchImpl(collection, type, connectionInfo, urn, databaseName);
        }

        /// <summary>
        /// Get a SearchableObject for the input fully-qualified name.  If no such object
        /// exists on the server, then null is returned.
        /// </summary>
        /// <param name="type">The type of object to get</param>
        /// <param name="connectionInfo">The connection information to be used by the SMO enumerator</param>
        /// <param name="databaseName">The name of the database containing the object, or null if the object is not contained by a database</param>
        /// <param name="fullObjectName">The fully-qualified name of the object (e.g. [Sales].[SalesSchemaCollection])</param>
        /// <returns>The SearchableObject if such an object exists on the server; otherwise null</returns>
        public static SearchableObject GetSearchableObject(
            SearchableObjectType    type,
            object                  connectionInfo,
            string                  databaseName,
            string                  fullObjectName)
        {
            SearchableObjectCollection  results     = new SearchableObjectCollection();
            ObjectName                  objectName  = null;
            int                         startIndex  = 0;

            if ((fullObjectName != null) && (fullObjectName.Length != 0) &&
                SearchableObject.GetNextName(fullObjectName, ref startIndex, CompareOptions.Ordinal, out objectName))
            {
                SearchableObjectTypeDescription typeDescription = SearchableObjectTypeDescription.GetDescription(connectionInfo, type);

                if (typeDescription.IsDatabaseObject)
                {
                    // STrace.Assert(
                        // ((databaseName != null) && (databaseName.Length != 0)),
                        // "database name not provided for database object");

                    if (typeDescription.IsSchemaObject)
                    {
                        string schemaName = String.Empty;

                        // if the client specified a schema, use it
                        if (objectName.Schema.Length != 0)
                        {
                            schemaName = objectName.Schema;
                        }
                        else
                        {
                            // otherwise use the default schema in the database
                            string urn = String.Format(
                                "Server/Database[@Name='{0}']",
                                Urn.EscapeString(databaseName));

                            string[] fields = new string[] { "DefaultSchema" };

                            DataTable dt = (new Enumerator()).Process(connectionInfo, new Request(urn, fields));
                            // STrace.Assert(dt.Rows.Count == 1, "unexpected number of rows returned");

                            if (0 < dt.Rows.Count)
                            {
                                schemaName = dt.Rows[0][0].ToString();
                            }
                        }

                        // STrace.Assert(schemaName.Length != 0, "schema name is empty");

                        SearchableObject.Search(
                            results,
                            type,
                            connectionInfo,
                            databaseName,
                            objectName.Name,
                            true,
                            schemaName,
                            (schemaName.Length != 0),
                            true);
                    }
                    else
                    {
                        SearchableObject.Search(
                            results,
                            type,
                            connectionInfo,
                            databaseName,
                            objectName.Name,
                            true,
                            true);
                    }
                }
                else
                {
                    SearchableObject.Search(
                        results,
                        type,
                        connectionInfo,
                        objectName.Name,
                        true,
                        true);
                }
            }

            // STrace.Assert(results.Count <= 1, "more than one object found for an 'exact match' search");
            SearchableObject result = (results.Count != 0) ? results[0] : null;
            return result;
        }

        /// <summary>
        /// Get the collation for a particular database or server
        /// </summary>
        /// <param name="connectionInfo">The connection information to communicate with the server</param>
        /// <param name="databaseName">The name of the database, or null to get the server collation</param>
        /// <returns>The name of the database or server collation</returns>
        public static string GetSqlCollation(object connectionInfo, string databaseName)
        {
            string      result      = String.Empty;
            Enumerator  enumerator  = new Enumerator();
            Request     request     = new Request();

            if (databaseName != null && databaseName.Length != 0)
            {
                request.Urn = String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(databaseName));
            }
            else
            {
                request.Urn = "Server/Information";
            }

            request.Fields = new string[] { "Collation" };

            System.Data.DataTable queryResults = enumerator.Process(connectionInfo, request);
            // STrace.Assert(queryResults.Rows.Count == 1, "unexpected number of collations returned");
            if (queryResults.Rows.Count != 0)
            {
                result = queryResults.Rows[0][0].ToString();
            }

            return result;
       }

        /// <summary>
        /// Search for object instances and add them to the collection
        /// </summary>
        /// <param name="collection>The collection to which found objects are to be added</param>
        /// <param name="type">The type of objects to search for</param>
        /// <param name="connectionInfo">Connection information used to enumerate objects</param>
        /// <param name"urn">the urn describing the objects to enumerate</param>
        /// <returns>True if any objects were found, false otherwise</returns>
        private static bool SearchImpl(
            SearchableObjectCollection  collection, 
            SearchableObjectType        type, 
            object                      connectionInfo, 
            string                      urn,
            string                      databaseName)
        {
            string          sqlCollation    = GetSqlCollation(connectionInfo, databaseName);
            CompareOptions  compareOptions  = SqlSupport.GetCompareOptionsFromCollation(sqlCollation);
            Enumerator      enumerator      = new Enumerator();
            Request         request         = new Request();

            request.Fields  = new string[] { "Urn" };
            request.Urn     = new Urn(urn);

            DataTable   enumeratedUrns  = enumerator.Process(connectionInfo, request);
            bool        foundObjects    = (0 < enumeratedUrns.Rows.Count);

            for (int i = 0; i < enumeratedUrns.Rows.Count; ++i)
            {
                string enumeratedUrn = enumeratedUrns.Rows[i]["Urn"].ToString();
                // STrace.Assert(enumeratedUrn.Length != 0, "received empty urn string");

                SearchableObject enumeratedObject = new SearchableObject(type, enumeratedUrn, compareOptions);
                // STrace.Assert(!collection.Contains(enumeratedObject), "enumerated object is already in the result");
                collection.Add(enumeratedObject);
            }
            
            return foundObjects;
        }


        /// <summary>
        /// Peel the next multi-part identifier from a string of semi-colon delimited identifiers
        /// </summary>
        /// <param name="names">The semi-colon delimited string of identifiers</param>
        /// <param name="startIndex">The index in the names string where the next identifier begins</param>
        /// <param name="objectName">The multi-part identifier that was peeled</param>
        /// <returns>True if there was a next identifier, false otherwise</returns>
        internal static bool GetNextName(
            string          names, 
            ref int         startIndex, 
            CompareOptions  compareOptions, 
            out ObjectName  objectName)
        {
            objectName = new ObjectName(compareOptions);

            while (startIndex < names.Length)
            {
                string token = SearchableObject.GetNextToken(names, ref startIndex);

                if ((0 == token.Length) || (';' == token[0]))
                {
                    break;
                }
                else if ('.' == token[0])
                {
                    // do nothing - skip part separator
                }
                else
                {
                    objectName.Add(Unquote(token));
                }
            }

            return (objectName.PartCount != 0);
        }

        /// <summary>
        /// Convert a quoted identifier part to a non-quoted identifier part.  Non-quoted parts are
        /// returned unchanged.
        /// </summary>
        /// <param name="identifierPart">The identifier part to unquote</param>
        /// <returns>The unquoted equivalent to the input identfier part</returns>
        private static string Unquote(string identifierPart)
        {
            string result = identifierPart;

            if ((identifierPart != null) && (2 < identifierPart.Length))
            {
                if ((('[' == identifierPart[0]) && (']' == identifierPart[identifierPart.Length - 1])) ||
                    (('"' == identifierPart[0]) && ('"' == identifierPart[identifierPart.Length - 1])))
                {
                    result = identifierPart.Substring(1, identifierPart.Length - 2);

                    if ('[' == identifierPart[0])
                    {
                        result = result.Replace("]]", "]");
                    }
                    else if ('"' == identifierPart[0])
                    {
                        result = result.Replace("\"\"", "\"");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the next token from the source string.  Tokens are '.', ';', or identifier parts.
        /// Identifier parts may or may not be quoted.
        /// </summary>
        /// <param name="source">The string from which to extract tokens</param>
        /// <param name="nextStartIndex">The index in the string where the next token starts.</param>
        /// <returns>The next token string in the string, or Empty if there are no more tokens</returns>
        private static string GetNextToken(string source, ref int nextStartIndex)
        {
            string result = String.Empty;


            if ((source != null) && (0 < (source.Length - nextStartIndex)))
            {
                // eat any leading space characters
                while ((nextStartIndex < source.Length) && Char.IsWhiteSpace(source[nextStartIndex]))
                {
                    ++nextStartIndex;
                }

                if (nextStartIndex != source.Length)
                {
                    char firstCharacter = source[nextStartIndex];
                    int lastCharacterIndex = nextStartIndex;
                    char[] separators = new char[] { '.', ';' };

                    if (('.' == firstCharacter) || (';' == firstCharacter))
                    {
                        // do nothing - just return the one character
                    }
                    else if (('[' == firstCharacter) || ('"' == firstCharacter))
                    {
                        char terminatingQuoteCharacter = ('[' == firstCharacter) ? ']' : '"';

                        lastCharacterIndex = GetNextIndex(source, nextStartIndex + 1, terminatingQuoteCharacter, true);

                        if (-1 == lastCharacterIndex)
                        {
                            lastCharacterIndex = source.IndexOfAny(separators, nextStartIndex + 1);
                        }

                        if (-1 == lastCharacterIndex)
                        {
                            lastCharacterIndex = source.Length - 1;
                        }

                    }
                    else
                    {
                        int separatorIndex = source.IndexOfAny(separators, nextStartIndex + 1);

                        if (-1 == separatorIndex)
                        {
                            lastCharacterIndex = source.Length - 1;
                        }
                        else
                        {
                            lastCharacterIndex = separatorIndex - 1;
                        }
                    }

                    int length = lastCharacterIndex - nextStartIndex + 1;
                    result = source.Substring(nextStartIndex, length).Trim();
                    nextStartIndex = lastCharacterIndex + 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Find the index of the next occurance of the input character in the string.  This differs
        /// from String.IndexOf() in that the caller can specify that escaped characters should not
        /// be matched.
        /// </summary>
        /// <param name="source">The string to search for the character</param>
        /// <param name="startIndex">The index in the string at which the search should start</param>
        /// <param name="character">The character to search for</param>
        /// <param name="ignoreDoubledCharacters">If true, characters that are escaped (e.g. "]]") are skipped in the search</param>
        /// <returns>The index at which the next occurance of character occurs in the string, or -1 if there is no such occurance</returns>
        private static int GetNextIndex(
            string  source,
            int     startIndex,
            char    character,
            bool    ignoreDoubledCharacters)
        {
            int index = startIndex;

            while (index < source.Length)
            {
                if (source[index] == character)
                {
                    // if we aren't ignoring escaped (doubled) characters or if we've
                    // reached the end of the string, return the index
                    if (!ignoreDoubledCharacters || 
                        (index == (source.Length - 1)) || 
                        (character != source[index + 1])) 
                    {
                        return index;
                    }
                    
                    if (((index + 1) < source.Length) && (character == source[index + 1]))
                    {
                        // skip the doubled character
                        ++index;
                    }
                }

                ++index;
            }

            return -1;
        }
    }

    /// <summary>
    /// A collection of SearchableObjects
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SearchableObjectCollection : ICollection
    {
        private SortedList data;

        /// <summary>
        /// Constructor
        /// </summary>
        public              SearchableObjectCollection()
        {
            data = new SortedList();
        }
        
        /// <summary>
        /// Indexer
        /// </summary>
#pragma warning disable IDE0026 // Use expression body for indexer
        public  SearchableObject    this[int index]
        {
            get
            {
                return ((SearchableObject) this.data.GetByIndex(index));
            }
        }
#pragma warning restore IDE0026 // Use expression body for indexer

        /// <summary>
        /// Add an object to the collection
        /// </summary>
        /// <param name="value">the object to add</param>
        public  void        Add(SearchableObject value)
        {
            if (!this.data.Contains(value))
            {
                this.data.Add(value, value);
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Get the index of the object in the collection
        /// </summary>
        /// <param name="value">The object for which to find the index</param>
        /// <returns>The object's index</returns>
        public  int         IndexOf(SearchableObject value)
        {
            return this.data.IndexOfKey(value);
        }

        /// <summary>
        /// Remove an object from the collection
        /// </summary>
        /// <param name="value">The object to remove</param>
        public  void        Remove(SearchableObject value)
        {
            if (this.data.Contains(value))
            {
                this.data.Remove(value);
            }

            this.NotifyObservers();
        }

        /// <summary>
        /// Does the collection contain a particular object
        /// </summary>
        /// <param name="value">The object for which to check</param>
        /// <returns>true if the object is in the collection, false otherwise</returns>
        public  bool        Contains(SearchableObject value)
        {
            return this.data.Contains(value);
        }
        /// <summary>
        /// Notify iterators that the collection has changed
        /// </summary>
        private void        NotifyObservers()
        {
            if (null != this.OnInvalidateEnumerator)
            {
                this.OnInvalidateEnumerator();
            }
        }

        
        /// <summary>
        /// Delegate declaration for delegates that will be called when the collection changes
        /// </summary>
        internal delegate   void                    InvalidateEnumerator();
        
        /// <summary>
        /// Event that is fired when the collection changes
        /// </summary>
        internal event      InvalidateEnumerator    OnInvalidateEnumerator;

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
            return new SearchableObjectCollectionEnumerator(this);
        }


        #endregion
    }

    /// <summary>
    /// An enumerator for SearchableObjectCollections
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SearchableObjectCollectionEnumerator : IEnumerator
    {
        private SearchableObjectCollection  collection;
        private int                         currentIndex;
        private SearchableObject            currentObject;
        private bool                        isValid;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="collection">The collection to enumerate</param>
        internal SearchableObjectCollectionEnumerator(SearchableObjectCollection collection)
        {
            this.collection     = collection;
            this.currentIndex   = -1;
            this.currentObject  = null;
            this.isValid        = true;

            this.collection.OnInvalidateEnumerator += new SearchableObjectCollection.InvalidateEnumerator(this.Invalidate);
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
            
            this.currentIndex   = -1;
            this.currentObject  = null;
        }

        /// <summary>
        /// The current SearchableObject
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
                this.currentObject  = this.collection[this.currentIndex];
                result              = true;
            }

            return result;
        }

        #endregion

    }

    /// <summary>
    /// Internal class that encapsulates a multi-part name.  
    /// </summary>
    /// <remarks>
    /// The name is assumed to be in one of the following forms: 
    /// [Name], [Schema].[Name], [Database].[Schema].[Name], 
    /// or [Server].[Database].[Schema].[Name]
    /// </remarks>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class ObjectName
    {
        private ArrayList                           parts;
        private SqlCollationSensitiveStringComparer nameComparer;

        /// <summary>
        /// The name portion of the multi-part name
        /// </summary>
        public string   Name
        {
            get
            {
                string result = String.Empty;

                if (this.parts.Count != 0)
                {
                    int namePartIndex = this.parts.Count - 1;
                    result = (string) this.parts[namePartIndex];
                }

                return result;
            }

            set
            {
                if (this.parts.Count == 0)
                {
                    this.parts.Add(value);
                }
                else
                {
                    int namePartIndex = this.parts.Count - 1;
                    this.parts[namePartIndex] = value;
                }
            }
        }

        /// <summary>
        /// The schema portion of the multi-part name
        /// </summary>
        public string   Schema
        {
            get
            {
                string result = String.Empty;

                if (1 < this.parts.Count)
                {
                    int schemaPartIndex = this.parts.Count - 2;
                    result = (string) this.parts[schemaPartIndex];
                }

                return result;
            }

            set
            {
                if (this.parts.Count == 0)
                {
                    this.parts.Add(value);
                    this.parts.Add(String.Empty);
                }
                else if (this.parts.Count == 1)
                {
                    this.parts.Insert(0, value);
                }
                else
                {
                    int schemaPartIndex = this.parts.Count - 2;
                    this.parts[schemaPartIndex] = value;
                }
            }
        }
                
        /// <summary>
        /// The number of parts in the name
        /// </summary>
        public int      PartCount
        {
            get
            {
                return this.parts.Count;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="compareOptions">CompareOptions to use when comparing ObjectName objects</param>
        public ObjectName(CompareOptions compareOptions)
        {
            this.parts          = new ArrayList();
            this.nameComparer   = new SqlCollationSensitiveStringComparer(compareOptions);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schema">The object schema</param>
        /// <param name="name">The object name</param>
        /// <param name="compareOptions">CompareOptions to use when comparing ObjectName objects</param>
        public ObjectName(string schema, string name, CompareOptions compareOptions)
        {
            this.parts          = new ArrayList();
            this.nameComparer   = new SqlCollationSensitiveStringComparer(compareOptions);

            this.parts.Add(schema);
            this.parts.Add(name);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="searchableObject">The seachable object to get the name from</param>
        public ObjectName(SearchableObject searchableObject)
        {
            this.parts          = new ArrayList();
            this.nameComparer   = new SqlCollationSensitiveStringComparer(searchableObject.CompareOptions);

            if (searchableObject.Schema.Length != 0)
            {
                this.parts.Add(searchableObject.Schema);
            }

            this.parts.Add(searchableObject.Name);
        }

        /// <summary>
        /// Add a name part to the multi-part name
        /// </summary>
        /// <param name="namePart">The name-part to add</param>
        public void             Add(string namePart)
        {
            this.parts.Add(namePart);
        }

        /// <summary>
        /// Get a display string for the multi-part name
        /// </summary>
        /// <returns>The string</returns>
        public override string  ToString()
        {
            StringBuilder   result      = new StringBuilder();
            bool            isFirstPart = true;
            
            foreach (string part in this.parts)
            {
                if (isFirstPart)
                {
                    isFirstPart = false;
                }
                else
                {
                    result.Append(".");
                }

                result.Append(this.Quote(part));
            }

            return result.ToString();
        }

        /// <summary>
        /// Compare the values of this name with another name
        /// </summary>
        /// <param name="other">The name to compare to</param>
        /// <returns>True if the name values are the same; otherwise false</returns>
        public bool             IsSameAs(ObjectName other)
        {
            bool result = true;

            if (this.PartCount == other.PartCount)
            {
                for (int i = 0; i < this.PartCount; ++i)
                {
                    if (0 != this.nameComparer.Compare(this.parts[i], other.parts[i]))
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Quote the input name part
        /// </summary>
        /// <param name="unquotedString">The name part to quote</param>
        /// <returns>The quoted name part</returns>
        private string          Quote(string unquotedString)
        {
            string escaped = unquotedString.Replace("]", "]]");
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0}]", escaped);
        }

    }
    

}
