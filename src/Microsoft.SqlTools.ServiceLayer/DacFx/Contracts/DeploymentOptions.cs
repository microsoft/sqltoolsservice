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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// DeploymentOptionProperty class to define deployment options default value, description, and displayNames
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeploymentOptionProperty<T>
    {
        public DeploymentOptionProperty(T value, string description = "", string displayName = "")
        {
            this.Value = value;
            this.Description = description;
            this.DisplayName = displayName;
        }

        /// <summary>
        /// Default and selected value of the deployment option
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Description of the deployment option
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Display name of the deployment option
        /// </summary>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Class to define deployment options.
    /// These property names will be given to the DeploymentOptions interface defined in ADS 'azuredatastudio\extensions\mssql\src\mssql.d.ts' and 'azuredatastudio\extensions\types\vscode-mssql.d.ts'
    /// BooleanOptionsDictionary will automatically gets the newly added boolean properties from DacFx, All other types should be added here and ADS
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        /// <summary>
        /// These default exclude options are for schema compare extension, It require some default options to be excluded for SC operations
        /// Where as the publish operation does not require any defaults, removing all default options for publish is handled in <azuredatastudio>\extensions\sql-database-projects\src\dialogs\publishDatabaseDialog.ts
        /// </summary>
        public DeploymentOptionProperty<string[]> ExcludeObjectTypes { get; set; } = new DeploymentOptionProperty<string[]>
        (
            new string[] {
                Enum.GetName(ObjectType.ServerTriggers),
                Enum.GetName(ObjectType.Routes),
                Enum.GetName(ObjectType.LinkedServerLogins),
                Enum.GetName(ObjectType.Endpoints),
                Enum.GetName(ObjectType.ErrorMessages),
                Enum.GetName(ObjectType.Files),
                Enum.GetName(ObjectType.Logins),
                Enum.GetName(ObjectType.LinkedServers),
                Enum.GetName(ObjectType.Credentials),
                Enum.GetName(ObjectType.DatabaseScopedCredentials),
                Enum.GetName(ObjectType.DatabaseEncryptionKeys),
                Enum.GetName(ObjectType.MasterKeys),
                Enum.GetName(ObjectType.DatabaseAuditSpecifications),
                Enum.GetName(ObjectType.Audits),
                Enum.GetName(ObjectType.ServerAuditSpecifications),
                Enum.GetName(ObjectType.CryptographicProviders),
                Enum.GetName(ObjectType.ServerRoles),
                Enum.GetName(ObjectType.EventSessions),
                Enum.GetName(ObjectType.DatabaseOptions),
                Enum.GetName(ObjectType.EventNotifications),
                Enum.GetName(ObjectType.ServerRoleMembership),
                Enum.GetName(ObjectType.AssemblyFiles)
            }
        );

        /// <summary>
        /// BooleanOptionsDictionary contains all boolean type deployment options
        /// </summary>
        public Dictionary<string, DeploymentOptionProperty<bool>> BooleanOptionsDictionary { get; set; } = new Dictionary<string, DeploymentOptionProperty<bool>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Contains object types enum name and its display name from <DacFx>\Product\Source\DeploymentApi\ObjectTypes.cs Enum
        /// key: optionName, value:DisplayName
        /// </summary>
        public Dictionary<string, string> ObjectTypesDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        #endregion

        public DeploymentOptions()
        {
            DacDeployOptions options = new DacDeployOptions();

            // Adding these defaults to ensure behavior similarity with other tools. Dacfx and SSMS import/export wizards use these defaults.
            // Tracking the full fix : https://github.com/microsoft/azuredatastudio/issues/5599
            options.AllowDropBlockingAssemblies = true;
            options.AllowIncompatiblePlatform = true;
            options.DropObjectsNotInSource = true;
            options.DropPermissionsNotInSource = true;
            options.DropRoleMembersNotInSource = true;
            options.IgnoreKeywordCasing = false;
            options.IgnoreSemicolonBetweenStatements = false;

            // Initializing the default boolean type options to the BooleanOptionsDictionary
            // Not considering DacFx default ExcludeObjectTypes, as it has some STS defaults which needs to be considered here, DacFx defaults are only considered for InitializeFromProfile(), where options are loading from profile
            InitializeBooleanTypeOptions(options);
        }

        public DeploymentOptions(DacDeployOptions options)
        {
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

        /// <summary>
        /// Populates BooleanOptionsDictionary with the boolean type properties
        /// </summary>
        /// <param name="options"></param>
        public void InitializeBooleanTypeOptions(DacDeployOptions options)
        {
            PropertyInfo[] dacDeploymentOptionsProperties = options.GetType().GetProperties();
            foreach (PropertyInfo  prop in dacDeploymentOptionsProperties)
            {
                if (prop != null && prop.PropertyType == typeof(System.Boolean))
                {
                    object setProp = GetDeploymentOptionProp(prop, options);
                    this.BooleanOptionsDictionary[prop.Name] = (DeploymentOptionProperty<bool>)setProp;
                }
            }

            // Preparing object types dictionary
            InitializeObjectTypesDictionary();
        }

        public void SetOptions(DacDeployOptions options)
        {
            // Set the default options properties
            InitializeBooleanTypeOptions(options);
            InitializeNonBooleanTypeOptions(options);
        }

        /// <summary>
        /// Preparing all non boolean properties (except BooleanOptionsDictionary)
        /// </summary>
        /// <param name="options"></param>
        public void InitializeNonBooleanTypeOptions(DacDeployOptions options)
        {
            // preparing remaining properties
            PropertyInfo[] deploymentOptionsProperties = this.GetType().GetProperties();
            foreach (PropertyInfo deployOptionsProp in deploymentOptionsProperties)
            {
                if (deployOptionsProp.Name != nameof(DeploymentOptions.BooleanOptionsDictionary))
                {
                    PropertyInfo prop = options.GetType().GetProperty(deployOptionsProp.Name);
                    object setProp = GetDeploymentOptionProp(prop, options);
                    deployOptionsProp.SetValue(this, setProp);
                }
            }
        }

        /// <summary>
        /// Preparing all object types which are considered as boolean options
        /// </summary>
        public void InitializeObjectTypesDictionary()
        {
            Type objectTypeEnum = typeof(ObjectType);
            foreach (string name in Enum.GetNames(objectTypeEnum))
            {
                MemberInfo[] member = objectTypeEnum.GetMember(name);
                MemberInfo info = member?.FirstOrDefault();
                string displayName = info?.GetCustomAttribute<DisplayAttribute>().GetName();
                if (displayName == null)
                {
                    // not expecting display name for any options as empty string
                    Logger.Write(TraceEventType.Error, string.Format($"Display name is empty for the Object type enum {0}", name));
                }
                else
                {
                    // Add the property to the Dictionary
                    ObjectTypesDictionary[name] = displayName;
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
            DescriptionAttribute descriptionAttribute = prop.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault(); 
            DisplayNameAttribute displayNameAttribute = prop.GetCustomAttributes<DisplayNameAttribute>().FirstOrDefault();
            Type type = val != null ? typeof(DeploymentOptionProperty<>).MakeGenericType(val.GetType())
                : typeof(DeploymentOptionProperty<>).MakeGenericType(prop.PropertyType);

            // DeploymentOptions ExcludeObjectTypes are String[] type and need special casting here
            if (prop.Name == nameof(this.ExcludeObjectTypes))
            {
                type = typeof(DeploymentOptionProperty<string[]>);
                val = val != null ? ConvertObjectTypeToStringArray((ObjectType[])val): new string[] { };
            }

            return Activator.CreateInstance(type, val, 
                (descriptionAttribute != null ? descriptionAttribute.Description : ""), 
                (displayNameAttribute != null ? displayNameAttribute.DisplayName : ""));
        }

        /// <summary>
        /// Converting ObjectType to String[] as the deployemnt options excludeObjectTypes is string[] but the DacFx DacDeployOptions excludeObjectTypes is of ObjectType[]
        /// Loading options from profile and schema compare .scmp file should need this conversion
        /// </summary>
        /// <param name="excludeObjectTypes"></param>
        /// <returns>string[]</returns>
        public string[] ConvertObjectTypeToStringArray(ObjectType[] excludeObjectTypes)
        {
            List<string> returnVal = new List<string>();
            foreach (ObjectType excludeObject in excludeObjectTypes)
            {
                returnVal.Add(excludeObject.ToString());
            }
            return returnVal.ToArray();
        }

        public static DeploymentOptions GetDefaultSchemaCompareOptions()
        {
            return new DeploymentOptions();
        }

        public static DeploymentOptions GetDefaultPublishOptions()
        {
            DeploymentOptions result = new DeploymentOptions();

            result.ExcludeObjectTypes.Value = result.ExcludeObjectTypes.Value.Where(x => x != Enum.GetName(ObjectType.DatabaseScopedCredentials)).ToArray(); // re-include database-scoped credentials

            return result;
        }
    }
}
