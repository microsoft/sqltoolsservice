//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Represents a Securable in SQL that can generate the TSQL to list
    /// its permissions.
    /// </summary>
    internal class SupportedSecurable
    {
        #region constants
        private const string queryWithColumn = @"SELECT 
	permission_name AS [Permission]
	,subentity_name AS [Column] 
FROM fn_my_permissions(N'{0}', N'{1}')
ORDER BY permission_name, subentity_name;";
        private const string queryWithoutColumn = @"SELECT 
	permission_name AS [Permission]
FROM fn_my_permissions(N'{0}', N'{1}')
ORDER BY permission_name;";
        private const string queryWithoutSecurableName = @"SELECT 
	permission_name AS [Permission]
FROM fn_my_permissions(NULL, N'{1}')
ORDER BY permission_name;";
        #endregion

        #region fields
        /// <summary>
        /// SMO server we are targetting. This is needed so we can generate the correct
        /// query against Table valued functions
        /// </summary>
        private SqlServer.Management.Smo.Server server;
        /// <summary>
        /// Securable we are targetting
        /// </summary>
        private Urn securable;
        /// <summary>
        /// Name of the securable
        /// </summary>
        private string securableName;
        /// <summary>
        /// Schema of the securable. If the securable does not have a schema this will be an Empty string
        /// </summary>
        private string securableSchema;
        /// <summary>
        /// The SMO type of the securable.
        /// </summary>
        private string securableType;
        #endregion

        #region public properties
        /// <summary>
        /// Indicates whether or not the securable will return column information or just a 
        /// list of permissions
        /// </summary>
        public bool HasColumnInformation
        {
            get
            {
                bool hasColumnInformation = false;
                if (securable.Type == "Table" || securable.Type == "View")
                {
                    hasColumnInformation = true;
                }
                else if (securable.Type == "UserDefinedFunction" && this.server != null)
                {
                    UserDefinedFunction function = server.GetSmoObject(this.securable) as UserDefinedFunction;
                    // STrace.Assert(function != null, "Could not get correct SMO object");
                    hasColumnInformation = (function != null && function.FunctionType == UserDefinedFunctionType.Table);
                }
                return hasColumnInformation;
            }
        }
        /// <summary>
        /// name of the securable
        /// </summary>
        public string Name
        {
            get
            {
                return this.securableName;
            }
        }
        /// <summary>
        /// schema of the securable
        /// </summary>
        public string Schema
        {
            get
            {
                return this.securableSchema;
            }
        }
        #endregion

        #region Construction
        /// <summary>
        /// Create a new SupportedSecurable object.
        /// </summary>
        /// <param name="securable">The securable we are targetting</param>
        /// <param name="server">The server where the securable resides. Can be null</param>
        public SupportedSecurable(Urn securable, SqlServer.Management.Smo.Server server)
        {
            if (securable == null)
            {
                throw new ArgumentNullException("securable");
            }

            this.securable = securable;
            this.server = server;

            // get the name
            this.securableName = this.securable.GetAttribute("Name");
            // get the schema from the urn
            this.securableSchema = this.securable.GetAttribute("Schema");
            // convert null to string.empty
            this.securableSchema ??= String.Empty;
            // get the type
            this.securableType = this.securable.Type;
            // check that we were passed good information
            // STrace.Assert(this.securableName != null && this.securableName.Length > 0, "No usable object name available");
            // STrace.Assert(this.securableType != null && this.securableType.Length > 0, "No usable object type available");
        }
        #endregion

        #region public methods
        /// <summary>
        /// Generate a SQL query that would query the permissions on this securable
        /// </summary>
        /// <returns>a string that represents the query</returns>
        public string GetPermissionsForSecurableSyntax()
        {
            string sqlQuery;
            string fullSecurableName = null;
            
            // get the class we will pass
            string securableClass = GetSecurableClassForUrn(this.securable);

            // Do not pass securable name for SERVER securables
            if (securableClass == "SERVER")
            {
                sqlQuery = queryWithoutSecurableName;
            }
            else
            {
                if (this.HasColumnInformation)
                {
                    sqlQuery = queryWithColumn;
                }
                else
                {
                    sqlQuery = queryWithoutColumn;
                }

                if (this.securableSchema.Length > 0)
                {
                    fullSecurableName = String.Format(CultureInfo.InvariantCulture, "[{0}].[{1}]"
                        , SecurableUtils.EscapeString(SecurableUtils.EscapeString(this.securableSchema, "]"), "'")
                        , SecurableUtils.EscapeString(SecurableUtils.EscapeString(this.securableName, "]"), "'"));
                }
                else
                {
                    fullSecurableName = String.Format(CultureInfo.InvariantCulture, "[{0}]"
                        , SecurableUtils.EscapeString(SecurableUtils.EscapeString(this.securableName, "]"), "'"));
                }
            }

            // return the select query
            return String.Format(CultureInfo.InvariantCulture, sqlQuery, fullSecurableName, securableClass);
        }
        #endregion

        #region implementation
        /// <summary>
        /// Finds a type for a Urn that can be passed to fn_my_permissions
        /// </summary>
        /// <param name="type">Urn</param>
        /// <returns>tsql type</returns>
        private static string GetSecurableClassForUrn(Urn securable)
        {
            string securableType;

            // just use a simple switch. If we wanted to be more sophisticated we could use a 
            // chain-of-responsibility pattern
            switch (securable.Type)
            {
                case "ApplicationRole":
                    securableType = "APPLICATION ROLE";
                    break;
                case "SqlAssembly":
                    securableType = "ASSEMBLY";
                    break;
                case "AsymmetricKey":
                    securableType = "ASYMMETRIC KEY";
                    break;
                case "Certificate":
                    securableType = "CERTIFICATE";
                    break;
                case "ServiceContract":
                    securableType = "CONTRACT";
                    break;
                case "Database":
                    securableType = "DATABASE";
                    break;
                case "Endpoint":
                    securableType = "ENDPOINT";
                    break;
                case "ExternalDataSource":
                    securableType = "EXTERNAL DATA SOURCE";
                    break;
                case "ExternalFileFormat":
                    securableType = "EXTERNAL FILE FORMAT";
                    break;
                case "FullTextCatalog":
                    securableType = "FULLTEXT CATALOG";
                    break;
                case "Login":
                    securableType = "LOGIN";
                    break;
                case "MessageType":
                    securableType = "MESSAGE TYPE";
                    break;
                case "AvailabilityGroup":
                    securableType = "AVAILABILITY GROUP";
                    break;
                // the following types map to OBJECT
                case "UserDefinedAggregate":
                case "Check":
                case "Default":
                case "ForeignKey":
                // index is for index and primary key constraints
                case "Index":
                case "StoredProcedure":
                case "UserDefinedFunction":
                case "Rule":
                case "Synonym":
                case "Sequence":
                case "ServiceQueue":
                case "Trigger":
                case "DdlTrigger":
                case "Table":
                case "View":
                case "ExtendedStoredProcedure":
                    securableType = "OBJECT";
                    break;
                case "RemoteServiceBinding":
                    securableType = "REMOTE SERVICE BINDING";
                    break;
                case "Role":
                    securableType = (securable.Parent.Type == "Database") ? "ROLE" : "SERVER ROLE";
                    break;
                case "ServiceRoute":
                    securableType = "ROUTE";
                    break;
                case "Schema":
                    securableType = "SCHEMA";
                    break;
                case "SecurityPolicy":
                    securableType = "SECURITY POLICY";
                    break;
                case "Server":
                    securableType = "SERVER";
                    break;
                case "BrokerService":
                    securableType = "SERVICE";
                    break;
                case "SymmetricKey":
                    securableType = "SYMMETRIC KEY";
                    break;
                case "UserDefinedDataType":
                    securableType = "TYPE";
                    break;
                case "User":
                    securableType = "USER";
                    break;
                case "XmlSchemaCollection":
                    securableType = "XML SCHEMA COLLECTION";
                    break;
                default:
                    // throw if we don't know about something
                    throw new InvalidOperationException();
            }

            return securableType;
        }
        #endregion
    }
}
