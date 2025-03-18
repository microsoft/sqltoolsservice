//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Contracts;
using static Microsoft.SqlTools.Utility.SqlConstants;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Helper class for providing metadata about connection options
    /// </summary>         
    internal class ConnectionProviderOptionsHelper
    {
        private static class GroupNames
        {
            public const string General = "general";
            public const string Initialization = "initialization";
            public const string Security = "security";
            public const string Context = "context";
            public const string Pooling = "pooling";
            public const string Resiliency = "resiliency";
        }

        internal static ConnectionProviderOptions BuildConnectionProviderOptions()
        {
            return new ConnectionProviderOptions
            {
                GroupDisplayNames = new()
                {
                    [GroupNames.General] = SR.ConnectionConfigOptions_groups_general,
                    [GroupNames.Initialization] = SR.ConnectionConfigOptions_groups_initialization,
                    [GroupNames.Security] = SR.ConnectionConfigOptions_groups_security,
                    [GroupNames.Context] = SR.ConnectionConfigOptions_groups_context,
                    [GroupNames.Pooling] = SR.ConnectionConfigOptions_groups_pooling,
                    [GroupNames.Resiliency] = SR.ConnectionConfigOptions_groups_resiliency,
                },

                Options =
                [
                    new ConnectionOption
                    {
                        Name = "server",
                        DisplayName = SR.ConnectionConfigOptions_server_displayName,
                        Description = SR.ConnectionConfigOptions_server_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueServerName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "database",
                        DisplayName = SR.ConnectionConfigOptions_database_displayName,
                        Description = SR.ConnectionConfigOptions_database_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueDatabaseName,
                        IsIdentity = true,
                        IsRequired = false,
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "authenticationType",
                        DisplayName = SR.ConnectionConfigOptions_authenticationType_displayName,
                        Description = SR.ConnectionConfigOptions_authenticationType_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        SpecialValueType = ConnectionOption.SpecialValueAuthType,
                        CategoryValues =
                        [
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_authenticationType_category_SqlLogin, Name = "SqlLogin" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_authenticationType_category_Integrated, Name = "Integrated" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_authenticationType_category_AzureMFA, Name = AzureMFA }
                        ],
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "user",
                        DisplayName = SR.ConnectionConfigOptions_user_displayName,
                        Description = SR.ConnectionConfigOptions_user_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueUserName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "password",
                        DisplayName = SR.ConnectionConfigOptions_password_displayName,
                        Description = SR.ConnectionConfigOptions_password_description,
                        ValueType = ConnectionOption.ValueTypePassword,
                        SpecialValueType = ConnectionOption.SpecialValuePasswordName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = GroupNames.General,
                    },
                    new ConnectionOption
                    {
                        Name = "applicationIntent",
                        DisplayName = SR.ConnectionConfigOptions_applicationIntent_displayName,
                        Description = SR.ConnectionConfigOptions_applicationIntent_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        CategoryValues =
                        [
                            new CategoryValue { Name = "ReadWrite", DisplayName = SR.ConnectionConfigOptions_applicationIntent_category_ReadWrite },
                            new CategoryValue { Name = "ReadOnly", DisplayName = SR.ConnectionConfigOptions_applicationIntent_category_ReadOnly }
                        ],
                        GroupName = GroupNames.Initialization
                    },
                    new ConnectionOption
                    {
                        Name = "connectTimeout",
                        DisplayName = SR.ConnectionConfigOptions_connectTimeout_displayName,
                        Description = SR.ConnectionConfigOptions_connectTimeout_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "15",
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "commandTimeout",
                        DisplayName = SR.ConnectionConfigOptions_commandTimeout_displayName,
                        Description = SR.ConnectionConfigOptions_commandTimeout_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "30",
                        GroupName = GroupNames.Initialization
                    },
                    new ConnectionOption
                    {
                        Name = "currentLanguage",
                        DisplayName = SR.ConnectionConfigOptions_currentLanguage_displayName,
                        Description = SR.ConnectionConfigOptions_currentLanguage_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.Initialization
                    },
                    new ConnectionOption
                    {
                        Name = "columnEncryptionSetting",
                        DisplayName = SR.ConnectionConfigOptions_columnEncryptionSetting_displayName,
                        Description = SR.ConnectionConfigOptions_columnEncryptionSetting_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = GroupNames.Security,
                        CategoryValues =
                        [
                            new CategoryValue { Name = "Disabled", DisplayName = SR.ConnectionConfigOptions_common_category_Disabled },
                            new CategoryValue { Name = "Enabled", DisplayName = SR.ConnectionConfigOptions_common_category_Enabled }
                        ]
                    },
                    new ConnectionOption
                    {
                        Name = "secureEnclaves",
                        DisplayName = SR.ConnectionConfigOptions_secureEnclaves_displayName,
                        Description = SR.ConnectionConfigOptions_secureEnclaves_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = GroupNames.Security,
                        CategoryValues =
                        [
                            new CategoryValue { Name = "Disabled", DisplayName = SR.ConnectionConfigOptions_common_category_Disabled },
                            new CategoryValue { Name = "Enabled", DisplayName = SR.ConnectionConfigOptions_common_category_Enabled }
                        ]
                    },
                    new ConnectionOption
                    {
                        Name = "attestationProtocol",
                        DisplayName = SR.ConnectionConfigOptions_attestationProtocol_displayName,
                        Description = SR.ConnectionConfigOptions_attestationProtocol_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = GroupNames.Security,
                        CategoryValues = [
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_attestationProtocol_category_HGS, Name = "HGS" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_attestationProtocol_category_AAS, Name = "AAS" }
                        ]
                    },
                    new ConnectionOption
                    {
                        Name = "enclaveAttestationUrl",
                        DisplayName = SR.ConnectionConfigOptions_enclaveAttestationUrl_displayName,
                        Description = SR.ConnectionConfigOptions_enclaveAttestationUrl_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.Security
                    },
                    new ConnectionOption
                    {
                        Name = "encrypt",
                        DisplayName = SR.ConnectionConfigOptions_encrypt_displayName,
                        Description = SR.ConnectionConfigOptions_encrypt_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = GroupNames.Security,
                        CategoryValues =
                        [
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_encrypt_category_Optional, Name = "Optional" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_encrypt_category_Mandatory, Name = "Mandatory" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_encrypt_category_Strict, Name = "Strict" }
                        ]
                    },
                    new ConnectionOption
                    {
                        Name = "persistSecurityInfo",
                        DisplayName = SR.ConnectionConfigOptions_persistSecurityInfo_displayName,
                        Description = SR.ConnectionConfigOptions_persistSecurityInfo_description,
                        GroupName = GroupNames.Security,
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "trustServerCertificate",
                        DisplayName = SR.ConnectionConfigOptions_trustServerCertificate_displayName,
                        Description = SR.ConnectionConfigOptions_trustServerCertificate_description,
                        GroupName = GroupNames.Security,
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "hostNameInCertificate",
                        DisplayName = SR.ConnectionConfigOptions_hostNameInCertificate_displayName,
                        Description = SR.ConnectionConfigOptions_hostNameInCertificate_description,
                        GroupName = GroupNames.Security,
                        ValueType = ConnectionOption.ValueTypeString,
                    },
                    new ConnectionOption
                    {
                        Name = "contextConnection",
                        DisplayName = SR.ConnectionConfigOptions_contextConnection_displayName,
                        Description = SR.ConnectionConfigOptions_contextConnection_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = GroupNames.Context
                    },
                    new ConnectionOption
                    {
                        Name = "port",
                        DisplayName = SR.ConnectionConfigOptions_port_displayName,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryCount",
                        DisplayName = SR.ConnectionConfigOptions_connectRetryCount_displayName,
                        Description = SR.ConnectionConfigOptions_connectRetryCount_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "1",
                        GroupName = GroupNames.Resiliency
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryInterval",
                        DisplayName = SR.ConnectionConfigOptions_connectRetryInterval_displayName,
                        Description = SR.ConnectionConfigOptions_connectRetryInterval_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "10",
                        GroupName = GroupNames.Resiliency

                    },
                    new ConnectionOption
                    {
                        Name = "applicationName",
                        DisplayName = SR.ConnectionConfigOptions_applicationName_displayName,
                        Description = SR.ConnectionConfigOptions_applicationName_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.General,
                        SpecialValueType = ConnectionOption.SpecialValueAppName
                    },
                    new ConnectionOption
                    {
                        Name = "workstationId",
                        DisplayName = SR.ConnectionConfigOptions_workstationId_displayName,
                        Description = SR.ConnectionConfigOptions_workstationId_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.Context
                    },
                    new ConnectionOption
                    {
                        Name = "pooling",
                        DisplayName = SR.ConnectionConfigOptions_pooling_displayName,
                        Description = SR.ConnectionConfigOptions_pooling_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = GroupNames.Pooling
                    },
                    new ConnectionOption
                    {
                        Name = "maxPoolSize",
                        DisplayName = SR.ConnectionConfigOptions_maxPoolSize_displayName,
                        Description = SR.ConnectionConfigOptions_maxPoolSize_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = GroupNames.Pooling
                    },
                    new ConnectionOption
                    {
                        Name = "minPoolSize",
                        DisplayName = SR.ConnectionConfigOptions_minPoolSize_displayName,
                        Description = SR.ConnectionConfigOptions_minPoolSize_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = GroupNames.Pooling
                    },
                    new ConnectionOption
                    {
                        Name = "loadBalanceTimeout",
                        DisplayName = SR.ConnectionConfigOptions_loadBalanceTimeout_displayName,
                        Description = SR.ConnectionConfigOptions_loadBalanceTimeout_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = GroupNames.Pooling
                    },
                    new ConnectionOption
                    {
                        Name = "replication",
                        DisplayName = SR.ConnectionConfigOptions_replication_displayName,
                        Description = SR.ConnectionConfigOptions_replication_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = GroupNames.Resiliency
                    },
                    new ConnectionOption
                    {
                        Name = "attachDbFilename",
                        DisplayName = SR.ConnectionConfigOptions_attachDbFilename_displayName,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.Context
                    },
                    new ConnectionOption
                    {
                        Name = "failoverPartner",
                        DisplayName = SR.ConnectionConfigOptions_failoverPartner_displayName,
                        Description = SR.ConnectionConfigOptions_failoverPartner_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.Resiliency
                    },
                    new ConnectionOption
                    {
                        Name = "multiSubnetFailover",
                        DisplayName = SR.ConnectionConfigOptions_multiSubnetFailover_displayName,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = GroupNames.General
                    },
                    new ConnectionOption
                    {
                        Name = "multipleActiveResultSets",
                        DisplayName = SR.ConnectionConfigOptions_multipleActiveResultSets_displayName,
                        Description = SR.ConnectionConfigOptions_multipleActiveResultSets_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = GroupNames.Initialization
                    },
                    new ConnectionOption
                    {
                        Name = "packetSize",
                        DisplayName = SR.ConnectionConfigOptions_packetSize_displayName,
                        Description = SR.ConnectionConfigOptions_packetSize_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = GroupNames.Pooling
                    },
                    new ConnectionOption
                    {
                        Name = "typeSystemVersion",
                        DisplayName = SR.ConnectionConfigOptions_typeSystemVersion_displayName,
                        Description = SR.ConnectionConfigOptions_typeSystemVersion_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = GroupNames.Security
                    }
                ]
            };
        }
    }
}
