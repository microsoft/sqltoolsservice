//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.SqlCore.DacFx.Contracts
{
    /// <summary>
    /// Defines the deployment scenario for determining default options
    /// </summary>
    public enum DeploymentScenario
    {
        /// <summary>
        /// Deployment/Publish scenario - uses DacFx native defaults
        /// </summary>
        Deployment = 0,

        /// <summary>
        /// Schema Compare scenario - uses modified defaults
        /// </summary>
        SchemaCompare = 1
    }

    /// <summary>
    /// DeploymentOptionProperty class to define deployment options default value, description, and displayNames
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeploymentOptionProperty<T>
    {
        /// <summary>
        /// Initializes a new <see cref="DeploymentOptionProperty{T}"/> with a value, optional description, and optional display name.
        /// </summary>
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
    /// BooleanOptionsDictionary will automatically get the newly added boolean properties from DacFx.
    /// All other types should be added here explicitly.
    /// </summary>
    public class DeploymentOptions
    {
        #region Properties

        /// <summary>
        /// Default exclude options for schema compare operations
        /// </summary>
        public DeploymentOptionProperty<string[]> ExcludeObjectTypes { get; set; } = new DeploymentOptionProperty<string[]>
        (
            new string[] {
                nameof(ObjectType.ServerTriggers),
                nameof(ObjectType.Routes),
                nameof(ObjectType.LinkedServerLogins),
                nameof(ObjectType.Endpoints),
                nameof(ObjectType.ErrorMessages),
                nameof(ObjectType.Files),
                nameof(ObjectType.Logins),
                nameof(ObjectType.LinkedServers),
                nameof(ObjectType.Credentials),
                nameof(ObjectType.DatabaseScopedCredentials),
                nameof(ObjectType.DatabaseEncryptionKeys),
                nameof(ObjectType.MasterKeys),
                nameof(ObjectType.DatabaseAuditSpecifications),
                nameof(ObjectType.Audits),
                nameof(ObjectType.ServerAuditSpecifications),
                nameof(ObjectType.CryptographicProviders),
                nameof(ObjectType.ServerRoles),
                nameof(ObjectType.EventSessions),
                nameof(ObjectType.DatabaseOptions),
                nameof(ObjectType.EventNotifications),
                nameof(ObjectType.ServerRoleMembership),
                nameof(ObjectType.AssemblyFiles)
            }
        );

        /// <summary>
        /// BooleanOptionsDictionary contains all boolean type deployment options
        /// </summary>
        public Dictionary<string, DeploymentOptionProperty<bool>> BooleanOptionsDictionary { get; set; } = new Dictionary<string, DeploymentOptionProperty<bool>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Contains object types enum name and its display name
        /// </summary>
        public Dictionary<string, string> ObjectTypesDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        #endregion

        /// <summary>
        /// Initializes a new <see cref="DeploymentOptions"/> instance with Schema Compare defaults.
        /// </summary>
        public DeploymentOptions() : this(DeploymentScenario.Deployment)
        {
        }

        /// <summary>
        /// Creates DeploymentOptions with appropriate defaults based on the deployment scenario.
        /// Deployment: Uses DacFx native defaults without any modifications.
        /// Schema Compare: Uses modified defaults that match SSMS behavior (7 specific overrides).
        /// </summary>
        /// <param name="scenario">The deployment scenario</param>
        public DeploymentOptions(DeploymentScenario scenario)
        {
            DacDeployOptions options = new DacDeployOptions();

            // Apply SSMS-matching overrides for Schema Compare scenario
            if (scenario == DeploymentScenario.SchemaCompare)
            {
                // SSMS has different defaults compared to DacFx. We are overriding these 7 defaults to match SSMS behavior.
                // SSMS defaults are defined in DeployModel.cs at .../DACWizard/DeployWizard/DeployModel.cs
                options.AllowDropBlockingAssemblies = true;
                options.AllowIncompatiblePlatform = true;
                options.DropObjectsNotInSource = true;
                options.DropPermissionsNotInSource = true;
                options.DropRoleMembersNotInSource = true;
                options.IgnoreKeywordCasing = false;
                options.IgnoreSemicolonBetweenStatements = false;
            }
            // Deployment scenario uses DacFx native defaults (no overrides needed)

            // Initializing the default boolean type options to the BooleanOptionsDictionary
            // Not considering DacFx default ExcludeObjectTypes, as it has some STS defaults which needs to be considered here, DacFx defaults are only considered for InitializeFromProfile(), where options are loading from profile
            InitializeBooleanTypeOptions(options);
        }

        /// <summary>
        /// Initializes a new <see cref="DeploymentOptions"/> instance populated from an existing <see cref="DacDeployOptions"/> object.
        /// </summary>
        public DeploymentOptions(DacDeployOptions options)
        {
            SetOptions(options);
        }

        /// <summary>
        /// Initialize deployment options from the options in a publish profile.xml
        /// </summary>
        public Task InitializeFromProfile(DacDeployOptions options, string profilePath)
        {
            string contents = File.ReadAllText(profilePath);
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
            return Task.CompletedTask;
        }
        /// <summary>
        /// Initializes the BooleanOptionsDictionary by reflecting over all boolean properties of DacDeployOptions.
        /// </summary>
        public void InitializeBooleanTypeOptions(DacDeployOptions options)
        {
            PropertyInfo[] dacDeploymentOptionsProperties = options.GetType().GetProperties();
            foreach (PropertyInfo prop in dacDeploymentOptionsProperties)
            {
                if (prop != null && prop.PropertyType == typeof(System.Boolean))
                {
                    object setProp = GetDeploymentOptionProp(prop, options);
                    this.BooleanOptionsDictionary[prop.Name] = (DeploymentOptionProperty<bool>)setProp;
                }
            }

            InitializeObjectTypesDictionary();
        }

        /// <summary>
        /// Populates both boolean and non-boolean deployment options from a <see cref="DacDeployOptions"/> instance.
        /// </summary>
        public void SetOptions(DacDeployOptions options)
        {
            InitializeBooleanTypeOptions(options);
            InitializeNonBooleanTypeOptions(options);
        }

        /// <summary>
        /// Preparing all non boolean properties
        /// </summary>
        public void InitializeNonBooleanTypeOptions(DacDeployOptions options)
        {
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
                if (string.IsNullOrEmpty(displayName))
                {
                    Logger.Error(string.Format($"Display name is empty for the Object type enum {0}", name));
                }
                else
                {
                    ObjectTypesDictionary[name] = displayName;
                }
            }
        }

        /// <summary>
        /// Prepares and returns the value and description of a property
        /// </summary>
        public object GetDeploymentOptionProp(PropertyInfo prop, DacDeployOptions options)
        {
            var val = prop.GetValue(options);
            DescriptionAttribute descriptionAttribute = prop.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
            DisplayNameAttribute displayNameAttribute = prop.GetCustomAttributes<DisplayNameAttribute>().FirstOrDefault();
            Type type = val != null ? typeof(DeploymentOptionProperty<>).MakeGenericType(val.GetType())
                : typeof(DeploymentOptionProperty<>).MakeGenericType(prop.PropertyType);

            if (prop.Name == nameof(this.ExcludeObjectTypes))
            {
                type = typeof(DeploymentOptionProperty<string[]>);
                val = val != null ? ConvertObjectTypeToStringArray((ObjectType[])val) : new string[] { };
            }

            return Activator.CreateInstance(type, val,
                (descriptionAttribute != null ? descriptionAttribute.Description : string.Empty),
                (displayNameAttribute != null ? displayNameAttribute.DisplayName : string.Empty));
        }

        /// <summary>
        /// Converting ObjectType to String[]
        /// </summary>
        public string[] ConvertObjectTypeToStringArray(ObjectType[] excludeObjectTypes)
        {
            return excludeObjectTypes.Select(t => t.ToString()).ToArray();
        }

        /// <summary>
        /// Returns a new <see cref="DeploymentOptions"/> instance with default schema compare settings.
        /// </summary>
        public static DeploymentOptions GetDefaultSchemaCompareOptions()
        {
            return new DeploymentOptions();
        }

        /// <summary>
        /// Returns a new <see cref="DeploymentOptions"/> instance with default publish settings, excluding DatabaseScopedCredentials from the exclude list.
        /// </summary>
        public static DeploymentOptions GetDefaultPublishOptions()
        {
            // Publish operations use DacFx native defaults (no SSMS-matching overrides)
            DeploymentOptions result = new DeploymentOptions(DeploymentScenario.Deployment);
            result.ExcludeObjectTypes.Value = result.ExcludeObjectTypes.Value.Where(x => x != ObjectType.DatabaseScopedCredentials.ToString()).ToArray();
            return result;
        }

    }
}
