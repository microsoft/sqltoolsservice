//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Data model for the EffectivePermmisions UI
    /// </summary>
    internal class EffectivePermissionsData
    {
        #region fields
        private CDataContainer dataContainer;
        // urn we are targetting
        private Urn urn;
        // securable we are targetting
        private SupportedSecurable securable;
        // the principal we will execute as
        private string principalName = string.Empty;
        // indicates whether or not we should execute as a login or user
        private bool executeAsLogin;
        #endregion

        #region properties
        /// <summary>
        /// True if the query for this principal will return column information
        /// </summary>
        public bool HasColumnInformation
        {
            get
            {
                // STrace.Assert(this.securable != null, "invalid object state");
                return this.securable.HasColumnInformation;
            }
        }
        /// <summary>
        /// The name of the principal we are checking permissions for
        /// </summary>
        public string PrincipalName
        {
            get
            {
                return this.principalName;
            }
        }
        /// <summary>
        /// the display name of the securable we are querying. If the securable has a schema it will be
        /// schema.securable, otherwise just securable
        /// </summary>
        public string SecurableDisplayName
        {
            get
            {
                // STrace.Assert(this.securable != null, "invalid object state");
                string displayName;
                if (this.securable.Schema.Length > 0)
                {
                    displayName = String.Format(CultureInfo.CurrentCulture
                        , "{0}.{1}"
                    , this.securable.Schema
                    , this.securable.Name);
                }
                else
                {
                    displayName = this.securable.Name;
                }
                return displayName;
            }
        }
        #endregion

        #region constructors
        /// <summary>
        /// Construct an EffectivePermissionsData object
        /// </summary>
        /// <param name="dataContainer">CDataContainer that represents the principal we are querying</param>
        public EffectivePermissionsData(CDataContainer dataContainer)
        {
            if (dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }

            this.dataContainer = dataContainer;

            Initialize();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Query the effective permissions for principal against the securable
        /// </summary>
        /// <returns>Dataset representing the permissions</returns>
        public DataSet QueryEffectivePermissions()
        {
            // get a connection
            ServerConnection serverConnection = this.dataContainer.ServerConnection;

            // see if we need to set the context to a particular db
            string databaseName = GetDatabaseName(this.urn);

            if (databaseName.Length > 0)
            {
                serverConnection.ExecuteNonQuery(
                    String.Format(CultureInfo.InvariantCulture
                        , "USE [{0}]"
                        , SecurableUtils.EscapeString(databaseName, "]")));
            }

            // get the securable query
            string securableQuery = this.securable.GetPermissionsForSecurableSyntax();

            // merge the securableQuery with the EXECUTE AS context
            string sqlQuery = 
            String.Format(CultureInfo.InvariantCulture,
                @"EXECUTE AS {0} = N'{1}';
{2}
REVERT;"
                , this.executeAsLogin ? "LOGIN" : "USER"
            , Urn.EscapeString(this.principalName)
            , securableQuery);

            return serverConnection.ExecuteWithResults(sqlQuery);
        }
        #endregion

        #region implementation
        /// <summary>
        /// Initialize the object
        /// </summary>
        private void Initialize()
        {
            // STrace.Assert(this.dataContainer != null);

            STParameters parameters = new STParameters(this.dataContainer.Document);

            // get the Urn of the securable. This must be set
            string securableUrn = string.Empty;
            parameters.GetParam("urn", ref securableUrn);

            // cannot proceed if there is no object to work on
            if (securableUrn == null || securableUrn.Length == 0)
            {
                throw new InvalidOperationException();
            }

            this.urn = new Urn(securableUrn);

            // get a supported securable for this object
            this.securable = new SupportedSecurable(this.urn, this.dataContainer.Server);

            // get the user we will be executing as
            parameters.GetParam("executeas", ref this.principalName);
            string executeAsType = String.Empty;
            parameters.GetParam("executetype", ref executeAsType);

            this.executeAsLogin = (executeAsType == "login");

            // if no override is supplied then we will just execute as self
            if (this.principalName == null || this.principalName.Length == 0)
            {
                // STrace.Assert(false, "Principal was not supplied. Defaulting to login");
                this.principalName = this.dataContainer.ServerConnection.TrueLogin;
                this.executeAsLogin = true;
            }
        }
        /// <summary>
        /// Get the database name if any that contains the securable
        /// </summary>
        /// <param name="urn">Securable</param>
        /// <returns>Database name that contains the securable, or an empty string if this is a server
        /// scoped object</returns>
        private string GetDatabaseName(Urn urn)
        {
            String databaseName = string.Empty;
            // otherwise try and find the database
            while (urn != null && urn.Type != "Database")
            {
                urn = urn.Parent;
            }

            if (urn != null)
            {
                databaseName = urn.GetAttribute("Name");
            }

            return databaseName;
        }
        #endregion
    }
}
