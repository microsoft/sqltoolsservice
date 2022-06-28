//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using System.Reflection;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Class to define deployment option default value and the description
    /// </summary>
    public class DeploymentOptionProperty<T>
    {
        public DeploymentOptionProperty(T value, string description = "", string propertyName = "")
        {
            this.Value = value;
            this.Description = description;
            this.propertyName = propertyName;
        }

        // Default and selected value of the deployment options
        public T Value { get; set; }

        // Description of the deployment options
        public string Description { get; set; }

        // To original name of the current property
        public string propertyName { get; set; }
    }

    /// <summary>
    /// Class to define deployment options.
    /// Keeping the order and defaults same as DacFx
    /// The default values here should also match the default values in ADS UX
    /// NOTE: When new deployment options are added in DacFx, they need to be added here too
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        // Command timeout to 120 seconds when executing queries against SQL Server.
        public DeploymentOptionProperty<int> CommandTimeout { get; set; } = new DeploymentOptionProperty<int>(120);

        // LongRunningCommandTimeout 0 seconds to wait indefinitely.
        public DeploymentOptionProperty<int> LongRunningCommandTimeout { get; set; } = new DeploymentOptionProperty<int>(0);

        // Wait 60 seconds to lock database when executing queries against SQL Server.
        public DeploymentOptionProperty<int> DatabaseLockTimeout { get; set; } = new DeploymentOptionProperty<int>(60);

        public DeploymentOptionProperty<string> AdditionalDeploymentContributorArguments { get; set; }

        public DeploymentOptionProperty<string> AdditionalDeploymentContributors { get; set; }

        public DeploymentOptionProperty<string> AdditionalDeploymentContributorPaths { get; set; }

        public DeploymentOptionProperty<ObjectType[]> DoNotDropObjectTypes { get; set; }

        public DeploymentOptionProperty<ObjectType[]> ExcludeObjectTypes { get; set; } = new DeploymentOptionProperty<ObjectType[]>
        (
            new ObjectType[] {
                ObjectType.ServerTriggers,
                ObjectType.Routes,
                ObjectType.LinkedServerLogins,
                ObjectType.Endpoints,
                ObjectType.ErrorMessages,
                ObjectType.Files,
                ObjectType.Logins,
                ObjectType.LinkedServers,
                ObjectType.Credentials,
                ObjectType.DatabaseScopedCredentials,
                ObjectType.DatabaseEncryptionKeys,
                ObjectType.MasterKeys,
                ObjectType.DatabaseAuditSpecifications,
                ObjectType.Audits,
                ObjectType.ServerAuditSpecifications,
                ObjectType.CryptographicProviders,
                ObjectType.ServerRoles,
                ObjectType.EventSessions,
                ObjectType.DatabaseOptions,
                ObjectType.EventNotifications,
                ObjectType.ServerRoleMembership,
                ObjectType.AssemblyFiles
            }
        );

        public Dictionary<string, DeploymentOptionProperty<bool>> OptionsMapTable { get; set; }

        public Dictionary<string, int> IncludeObjects;

        #endregion

        public DeploymentOptions()
        {
            DacDeployOptions options = new DacDeployOptions();
            OptionsMapTable = new Dictionary<string, DeploymentOptionProperty<bool>>();

            // Adding these defaults to ensure behavior similarity with other tools. Dacfx and SSMS import/export wizards use these defaults.
            // Tracking the full fix : https://github.com/microsoft/azuredatastudio/issues/5599
            options.AllowDropBlockingAssemblies = true;
            options.AllowIncompatiblePlatform = true;
            options.DropObjectsNotInSource = true;
            options.DropPermissionsNotInSource = true;
            options.DropRoleMembersNotInSource = true;
            options.IgnoreKeywordCasing = false;
            options.IgnoreSemicolonBetweenStatements = false;

            // Set the default options properties
            PopulateOptionsMapTableAndIncludeObjectsDict(options);

            // Exclude exclude object types for default values
            // preparing remaining properties
            PropertyInfo[] deploymentOptionsProperties = this.GetType().GetProperties();
            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                if (deployOptionsProp.Name != "ExcludeObjectTypes" && deployOptionsProp.Name != "OptionsMapTable")
                {
                    var prop = options.GetType().GetProperty(deployOptionsProp.Name);
                    object setProp = GetDeploymentOptionProp(prop, options);
                    deployOptionsProp.SetValue(this, setProp);
                }
            }
        }

        public DeploymentOptions(DacDeployOptions options)
        {
            OptionsMapTable = new Dictionary<string, DeploymentOptionProperty<bool>>();
            
            SetOptions(options);
        }

        /// <summary>
        /// initialize deployment options from the options in a publish profile.xml
        /// </summary>
        /// <param name="options">options created from the profile</param>
        /// <param name="profilePath"></param>
        public async Task InitializeFromProfile(DacDeployOptions options, string profilePath)
        {
            // check if defaults need to be set if they aren't specified in the profile
            string contents = await File.ReadAllTextAsync(profilePath);
            if (!contents.Contains("<AllowDropBlockingAssemblies>"))
            {
                options.AllowDropBlockingAssemblies = true;
            }
            if (!contents.Contains("<AllowIncompatiblePlatform>"))
            {
                options.AllowIncompatiblePlatform = true;
            }
            if (!contents.Contains("<DropObjectsNotInSource>"))
            {
                options.DropObjectsNotInSource = true;
            }
            if (!contents.Contains("<DropPermissionsNotInSource>"))
            {
                options.DropPermissionsNotInSource = true;
            }
            if (!contents.Contains("<DropRoleMembersNotInSource>"))
            {
                options.DropRoleMembersNotInSource = true;
            }
            if (!contents.Contains("<IgnoreKeywordCasing>"))
            {
                options.IgnoreKeywordCasing = false;
            }
            if (!contents.Contains("<IgnoreSemicolonBetweenStatements>"))
            {
                options.IgnoreSemicolonBetweenStatements = false;
            }

            SetOptions(options);
        }

        public void PopulateOptionsMapTableAndIncludeObjectsDict(DacDeployOptions options)
        {
            // To fill the options map table directly from the boolean type DacDeployoptions
            PropertyInfo[] dacDeploymentOptionsProperties = options.GetType().GetProperties();
            foreach (var prop in dacDeploymentOptionsProperties)
            {
                if (prop.PropertyType == typeof(System.Boolean))
                {
                    object setProp = GetDeploymentOptionProp(prop, options);
                    var displayNameAttribute = prop.GetCustomAttributes<DisplayNameAttribute>().FirstOrDefault();
                    this.OptionsMapTable[displayNameAttribute.DisplayName] = (DeploymentOptionProperty<bool>)setProp;
                }
            }

            // Populate Include Objects Dictionary
            PopulateIncludeObjectTypesDictionary();
        }

        /// <summary>
        /// Gets include objects enum names and values as a dictionary
        /// Ex: {key: Aggregate, value: 0}
        /// </summary>
        public void PopulateIncludeObjectTypesDictionary()
        {
            // Set include objects table data
            var objectTypeEnum = typeof(ObjectType);
            IncludeObjects = Enum.GetNames(objectTypeEnum).ToDictionary(t => t, t => (int)System.Enum.Parse(objectTypeEnum, t));
        }

        public void SetOptions(DacDeployOptions options)
        {
            // Set the default options properties
            PopulateOptionsMapTableAndIncludeObjectsDict(options);
            SetGenericDacDeploymentProperties(options);
        }

        /// <summary>
        /// Set values of all non boolean properties
        /// </summary>
        /// <param name="options"></param>
        public void SetGenericDacDeploymentProperties(DacDeployOptions options)
        {
            // preparing remaining properties
            PropertyInfo[] deploymentOptionsProperties = this.GetType().GetProperties();
            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                if (deployOptionsProp.Name != "OptionsMapTable")
                {
                    var prop = options.GetType().GetProperty(deployOptionsProp.Name);
                    object setProp = GetDeploymentOptionProp(prop, options);
                    deployOptionsProp.SetValue(this, setProp);
                }
            }
        }

        /// <summary>
        /// Prepares and returns the value and description of a property
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="options"></param>
        public object GetDeploymentOptionProp(PropertyInfo prop, DacDeployOptions options)
        {
            var val = prop.GetValue(options);
            var descriptionAttribute = prop.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault();
            var displayNameAttribute = prop.GetCustomAttributes<DisplayNameAttribute>(true).FirstOrDefault();
            Type type = val != null ? typeof(DeploymentOptionProperty<>).MakeGenericType(val.GetType()) 
                : typeof(DeploymentOptionProperty<>).MakeGenericType(prop.PropertyType);
           
            object setProp = Activator.CreateInstance(type, val, descriptionAttribute.Description, prop.Name);
            return setProp;
        }

        public static DeploymentOptions GetDefaultSchemaCompareOptions()
        {
            return new DeploymentOptions();
        }

        public static DeploymentOptions GetDefaultPublishOptions()
        {
            DeploymentOptions result = new DeploymentOptions();

            result.ExcludeObjectTypes.Value = result.ExcludeObjectTypes.Value.Where(x => x != ObjectType.DatabaseScopedCredentials).ToArray(); // re-include database-scoped credentials

            return result;
        }
    }
}
