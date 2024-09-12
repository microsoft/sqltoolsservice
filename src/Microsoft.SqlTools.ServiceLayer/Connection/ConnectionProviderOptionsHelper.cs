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
        internal static ConnectionProviderOptions BuildConnectionProviderOptions()
        {
            return new ConnectionProviderOptions
            {
                Options = new ConnectionOption[]
                {
                    new ConnectionOption
                    {
                        Name = "server",
                        DisplayName = SR.ConnectionConfigOptions_server_displayName,
                        Description = SR.ConnectionConfigOptions_server_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        SpecialValueType = ConnectionOption.SpecialValueServerName,
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Source"
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
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "authenticationType",
                        DisplayName = SR.ConnectionConfigOptions_authenticationType_displayName,
                        Description = SR.ConnectionConfigOptions_authenticationType_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        SpecialValueType = ConnectionOption.SpecialValueAuthType,
                        CategoryValues = new CategoryValue[]
                        { new CategoryValue { DisplayName = SR.ConnectionConfigOptions_authenticationType_category_SqlLogin, Name = "SqlLogin" },
                          new CategoryValue { DisplayName = SR.ConnectionConfigOptions_authenticationType_category_Integrated, Name = "Integrated" },
                          new CategoryValue { DisplayName = SR.ConnectionConfigOptions_authenticationType_category_AzureMFA, Name = AzureMFA }
                        },
                        IsIdentity = true,
                        IsRequired = true,
                        GroupName = "Security"
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
                        GroupName = "Security"
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
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "applicationIntent",
                        DisplayName = SR.ConnectionConfigOptions_applicationIntent_displayName,
                        Description = SR.ConnectionConfigOptions_applicationIntent_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        CategoryValues = new CategoryValue[] {
                            new CategoryValue { Name = "ReadWrite", DisplayName = SR.ConnectionConfigOptions_applicationIntent_category_ReadWrite },
                            new CategoryValue { Name = "ReadOnly", DisplayName = SR.ConnectionConfigOptions_applicationIntent_category_ReadOnly }
                        },
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "connectTimeout",
                        DisplayName = SR.ConnectionConfigOptions_connectTimeout_displayName,
                        Description = SR.ConnectionConfigOptions_connectTimeout_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "15",
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "commandTimeout",
                        DisplayName = SR.ConnectionConfigOptions_commandTimeout_displayName,
                        Description = SR.ConnectionConfigOptions_commandTimeout_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "30",
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "currentLanguage",
                        DisplayName = SR.ConnectionConfigOptions_currentLanguage_displayName,
                        Description = SR.ConnectionConfigOptions_currentLanguage_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Initialization"
                    },
                    new ConnectionOption
                    {
                        Name = "columnEncryptionSetting",
                        DisplayName = SR.ConnectionConfigOptions_columnEncryptionSetting_displayName,
                        Description = SR.ConnectionConfigOptions_columnEncryptionSetting_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = "Security",
                        CategoryValues = new CategoryValue[] {
                            new CategoryValue { Name = "Disabled", DisplayName = SR.ConnectionConfigOptions_columnEncryptionSetting_category_Disabled },
                            new CategoryValue { Name = "Enabled", DisplayName = SR.ConnectionConfigOptions_columnEncryptionSetting_category_Enabled }
                        }
                    },
                    new ConnectionOption
                    {
                        Name = "attestationProtocol",
                        DisplayName = SR.ConnectionConfigOptions_attestationProtocol_displayName,
                        Description = SR.ConnectionConfigOptions_attestationProtocol_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = "Security",
                        CategoryValues = new CategoryValue[] {
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_attestationProtocol_category_HGS, Name = "HGS" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_attestationProtocol_category_AAS, Name = "AAS" }
                        }
                    },
                    new ConnectionOption
                    {
                        Name = "enclaveAttestationUrl",
                        DisplayName = SR.ConnectionConfigOptions_enclaveAttestationUrl_displayName,
                        Description = SR.ConnectionConfigOptions_enclaveAttestationUrl_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Security"
                    },
                    new ConnectionOption
                    {
                        Name = "encrypt",
                        DisplayName = SR.ConnectionConfigOptions_encrypt_displayName,
                        Description = SR.ConnectionConfigOptions_encrypt_description,
                        ValueType = ConnectionOption.ValueTypeCategory,
                        GroupName = "Security",
                        CategoryValues = new CategoryValue[] {
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_encrypt_category_Optional, Name = "Optional" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_encrypt_category_Mandatory, Name = "Mandatory" },
                            new CategoryValue { DisplayName = SR.ConnectionConfigOptions_encrypt_category_Strict, Name = "Strict" }
                        }
                    },
                    new ConnectionOption
                    {
                        Name = "persistSecurityInfo",
                        DisplayName = SR.ConnectionConfigOptions_persistSecurityInfo_displayName,
                        Description = SR.ConnectionConfigOptions_persistSecurityInfo_description,
                        GroupName = "Security",
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "trustServerCertificate",
                        DisplayName = SR.ConnectionConfigOptions_trustServerCertificate_displayName,
                        Description = SR.ConnectionConfigOptions_trustServerCertificate_description,
                        GroupName = "Security",
                        ValueType = ConnectionOption.ValueTypeBoolean
                    },
                    new ConnectionOption
                    {
                        Name = "hostNameInCertificate",
                        DisplayName = SR.ConnectionConfigOptions_hostNameInCertificate_displayName,
                        Description = SR.ConnectionConfigOptions_hostNameInCertificate_description,
                        GroupName = "Security",
                        ValueType = ConnectionOption.ValueTypeString,
                    },
                    new ConnectionOption
                    {
                        Name = "attachedDBFileName",
                        DisplayName = SR.ConnectionConfigOptions_attachedDBFileName_displayName,
                        Description = SR.ConnectionConfigOptions_attachedDBFileName_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "contextConnection",
                        DisplayName = SR.ConnectionConfigOptions_contextConnection_displayName,
                        Description = SR.ConnectionConfigOptions_contextConnection_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "port",
                        DisplayName = SR.ConnectionConfigOptions_port_displayName,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "General"
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryCount",
                        DisplayName = SR.ConnectionConfigOptions_connectRetryCount_displayName,
                        Description = SR.ConnectionConfigOptions_connectRetryCount_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "1",
                        GroupName = "Connection Resiliency"
                    },
                    new ConnectionOption
                    {
                        Name = "connectRetryInterval",
                        DisplayName = SR.ConnectionConfigOptions_connectRetryInterval_displayName,
                        Description = SR.ConnectionConfigOptions_connectRetryInterval_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        DefaultValue = "10",
                        GroupName = "Connection Resiliency"

                    },
                    new ConnectionOption
                    {
                        Name = "applicationName",
                        DisplayName = SR.ConnectionConfigOptions_applicationName_displayName,
                        Description = SR.ConnectionConfigOptions_applicationName_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Context",
                        SpecialValueType = ConnectionOption.SpecialValueAppName
                    },
                    new ConnectionOption
                    {
                        Name = "workstationId",
                        DisplayName = SR.ConnectionConfigOptions_workstationId_displayName,
                        Description = SR.ConnectionConfigOptions_workstationId_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Context"
                    },
                    new ConnectionOption
                    {
                        Name = "pooling",
                        DisplayName = SR.ConnectionConfigOptions_pooling_displayName,
                        Description = SR.ConnectionConfigOptions_pooling_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "maxPoolSize",
                        DisplayName = SR.ConnectionConfigOptions_maxPoolSize_displayName,
                        Description = SR.ConnectionConfigOptions_maxPoolSize_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "minPoolSize",
                        DisplayName = SR.ConnectionConfigOptions_minPoolSize_displayName,
                        Description = SR.ConnectionConfigOptions_minPoolSize_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "loadBalanceTimeout",
                        DisplayName = SR.ConnectionConfigOptions_loadBalanceTimeout_displayName,
                        Description = SR.ConnectionConfigOptions_loadBalanceTimeout_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Pooling"
                    },
                    new ConnectionOption
                    {
                        Name = "replication",
                        DisplayName = SR.ConnectionConfigOptions_replication_displayName,
                        Description = SR.ConnectionConfigOptions_replication_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Replication"
                    },
                    new ConnectionOption
                    {
                        Name = "attachDbFilename",
                        DisplayName = SR.ConnectionConfigOptions_attachDbFilename_displayName,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "failoverPartner",
                        DisplayName = SR.ConnectionConfigOptions_failoverPartner_displayName,
                        Description = SR.ConnectionConfigOptions_failoverPartner_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Source"
                    },
                    new ConnectionOption
                    {
                        Name = "multiSubnetFailover",
                        DisplayName = SR.ConnectionConfigOptions_multiSubnetFailover_displayName,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "General"
                    },
                    new ConnectionOption
                    {
                        Name = "multipleActiveResultSets",
                        DisplayName = SR.ConnectionConfigOptions_multipleActiveResultSets_displayName,
                        Description = SR.ConnectionConfigOptions_multipleActiveResultSets_description,
                        ValueType = ConnectionOption.ValueTypeBoolean,
                        GroupName = "Advanced"
                    },
                    new ConnectionOption
                    {
                        Name = "packetSize",
                        DisplayName = SR.ConnectionConfigOptions_packetSize_displayName,
                        Description = SR.ConnectionConfigOptions_packetSize_description,
                        ValueType = ConnectionOption.ValueTypeNumber,
                        GroupName = "Advanced"
                    },
                    new ConnectionOption
                    {
                        Name = "typeSystemVersion",
                        DisplayName = SR.ConnectionConfigOptions_typeSystemVersion_displayName,
                        Description = SR.ConnectionConfigOptions_typeSystemVersion_description,
                        ValueType = ConnectionOption.ValueTypeString,
                        GroupName = "Advanced"
                    }
                }
            };
        }
    }
}
