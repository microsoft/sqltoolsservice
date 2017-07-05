//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class SrTests
    {
        /// <summary>
        /// Simple "test" to access string resources
        /// The purpose of this test is for code coverage.  It's probably better to just 
        /// exclude string resources in the code coverage report than maintain this test.
        /// </summary>
        [Fact]
        public void SrStringsTest()
        {
            var culture = SR.Culture;
            SR.Culture = culture;
            Assert.True(SR.Culture == culture);

            var connectionServiceListDbErrorNullOwnerUri = SR.ConnectionServiceListDbErrorNullOwnerUri;
            var connectionParamsValidateNullConnection = SR.ConnectionParamsValidateNullConnection;            
            var queryServiceCancelDisposeFailed = SR.QueryServiceCancelDisposeFailed;
            var queryServiceQueryCancelled = SR.QueryServiceQueryCancelled;
            var queryServiceDataReaderByteCountInvalid = SR.QueryServiceDataReaderByteCountInvalid;
            var queryServiceDataReaderCharCountInvalid = SR.QueryServiceDataReaderCharCountInvalid;
            var queryServiceDataReaderXmlCountInvalid = SR.QueryServiceDataReaderXmlCountInvalid;
            var queryServiceFileWrapperReadOnly = SR.QueryServiceFileWrapperReadOnly;
            var queryServiceAffectedOneRow = SR.QueryServiceAffectedOneRow;
            var queryServiceMessageSenderNotSql = SR.QueryServiceMessageSenderNotSql;
            var queryServiceResultSetNotRead = SR.QueryServiceResultSetNotRead;
            var queryServiceResultSetNoColumnSchema = SR.QueryServiceResultSetNoColumnSchema;
            var connectionServiceListDbErrorNotConnected = SR.ConnectionServiceListDbErrorNotConnected("..");
            var connectionServiceConnStringInvalidAuthType = SR.ConnectionServiceConnStringInvalidAuthType("..");
            var connectionServiceConnStringInvalidIntent = SR.ConnectionServiceConnStringInvalidIntent("..");
            var queryServiceAffectedRows = SR.QueryServiceAffectedRows(10);
            var queryServiceErrorFormat = SR.QueryServiceErrorFormat(1, 1, 1, 1, "\n", "..");
            var queryServiceQueryFailed = SR.QueryServiceQueryFailed("..");
            var workspaceServiceBufferPositionOutOfOrder = SR.WorkspaceServiceBufferPositionOutOfOrder(1, 2, 3, 4);
            var treeNodeError = SR.TreeNodeError;
            var serverNodeConnectionError = SR.ServerNodeConnectionError;
            var schemaHierarchyAggregates = SR.SchemaHierarchy_Aggregates;
            var SchemaHierarchy_ServerRoles = SR.SchemaHierarchy_ServerRoles;
            var SchemaHierarchy_ApplicationRoles = SR.SchemaHierarchy_ApplicationRoles;
            var SchemaHierarchy_Assemblies = SR.SchemaHierarchy_Assemblies;
            var SchemaHierarchy_AssemblyFiles = SR.SchemaHierarchy_AssemblyFiles;
            var SchemaHierarchy_AsymmetricKeys = SR.SchemaHierarchy_AsymmetricKeys;
            var SchemaHierarchy_DatabaseAsymmetricKeys = SR.SchemaHierarchy_DatabaseAsymmetricKeys;
            var SchemaHierarchy_DataCompressionOptions = SR.SchemaHierarchy_DataCompressionOptions;
            var SchemaHierarchy_Certificates = SR.SchemaHierarchy_Certificates;
            var SchemaHierarchy_FileTables = SR.SchemaHierarchy_FileTables;
            var SchemaHierarchy_DatabaseCertificates = SR.SchemaHierarchy_DatabaseCertificates;
            var SchemaHierarchy_CheckConstraints = SR.SchemaHierarchy_CheckConstraints;
            var SchemaHierarchy_Columns = SR.SchemaHierarchy_Columns;
            var SchemaHierarchy_Constraints = SR.SchemaHierarchy_Constraints;
            var SchemaHierarchy_Contracts = SR.SchemaHierarchy_Contracts;
            var SchemaHierarchy_Credentials = SR.SchemaHierarchy_Credentials;
            var SchemaHierarchy_ErrorMessages = SR.SchemaHierarchy_ErrorMessages;
            var SchemaHierarchy_ServerRoleMembership = SR.SchemaHierarchy_ServerRoleMembership;
            var SchemaHierarchy_DatabaseOptions = SR.SchemaHierarchy_DatabaseOptions;
            var SchemaHierarchy_DatabaseRoles = SR.SchemaHierarchy_DatabaseRoles;
            var SchemaHierarchy_RoleMemberships = SR.SchemaHierarchy_RoleMemberships;
            var SchemaHierarchy_DatabaseTriggers = SR.SchemaHierarchy_DatabaseTriggers;
            var SchemaHierarchy_DefaultConstraints = SR.SchemaHierarchy_DefaultConstraints;
            var SchemaHierarchy_Defaults = SR.SchemaHierarchy_Defaults;
            var SchemaHierarchy_Sequences = SR.SchemaHierarchy_Sequences;
            var SchemaHierarchy_Endpoints = SR.SchemaHierarchy_Endpoints;
            var SchemaHierarchy_EventNotifications = SR.SchemaHierarchy_EventNotifications;
            var SchemaHierarchy_ServerEventNotifications = SR.SchemaHierarchy_ServerEventNotifications;
            var SchemaHierarchy_ExtendedProperties = SR.SchemaHierarchy_ExtendedProperties;
            var SchemaHierarchy_FileGroups = SR.SchemaHierarchy_FileGroups;
            var SchemaHierarchy_ForeignKeys = SR.SchemaHierarchy_ForeignKeys;
            var SchemaHierarchy_FullTextCatalogs = SR.SchemaHierarchy_FullTextCatalogs;
            var SchemaHierarchy_FullTextIndexes = SR.SchemaHierarchy_FullTextIndexes;
            var SchemaHierarchy_Functions = SR.SchemaHierarchy_Functions;
            var SchemaHierarchy_Indexes = SR.SchemaHierarchy_Indexes;
            var SchemaHierarchy_InlineFunctions = SR.SchemaHierarchy_InlineFunctions;
            var SchemaHierarchy_Keys = SR.SchemaHierarchy_Keys;
            var SchemaHierarchy_LinkedServers = SR.SchemaHierarchy_LinkedServers;
            var SchemaHierarchy_LinkedServerLogins = SR.SchemaHierarchy_LinkedServerLogins;
            var SchemaHierarchy_Logins = SR.SchemaHierarchy_Logins;
            var SchemaHierarchy_MasterKey = SR.SchemaHierarchy_MasterKey;
            var SchemaHierarchy_MasterKeys = SR.SchemaHierarchy_MasterKeys;
            var SchemaHierarchy_MessageTypes = SR.SchemaHierarchy_MessageTypes;
            var SchemaHierarchy_MultiSelectFunctions = SR.SchemaHierarchy_MultiSelectFunctions;
            var SchemaHierarchy_Parameters = SR.SchemaHierarchy_Parameters;
            var SchemaHierarchy_PartitionFunctions = SR.SchemaHierarchy_PartitionFunctions;
            var SchemaHierarchy_PartitionSchemes = SR.SchemaHierarchy_PartitionSchemes;
            var SchemaHierarchy_Permissions = SR.SchemaHierarchy_Permissions;
            var SchemaHierarchy_PrimaryKeys = SR.SchemaHierarchy_PrimaryKeys;
            var schemaHierarchyPrimaryKeys = SR.SchemaHierarchy_PrimaryKeys;
            var schemaHierarchyProgrammability = SR.SchemaHierarchy_Programmability;
            var schemaHierarchyQueues = SR.SchemaHierarchy_Queues;
            var schemaHierarchyRemoteServiceBindings = SR.SchemaHierarchy_RemoteServiceBindings;
            var schemaHierarchyReturnedColumns = SR.SchemaHierarchy_ReturnedColumns;
            var schemaHierarchyRoles = SR.SchemaHierarchy_Roles;
            var schemaHierarchyRoutes = SR.SchemaHierarchy_Routes;
            var schemaHierarchyRules = SR.SchemaHierarchy_Rules;
            var schemaHierarchySchemas = SR.SchemaHierarchy_Schemas;
            var schemaHierarchySecurity = SR.SchemaHierarchy_Security;
            var schemaHierarchyServerObjects = SR.SchemaHierarchy_ServerObjects;
            var schemaHierarchyManagement = SR.SchemaHierarchy_Management;
            var schemaHierarchyServerTriggers = SR.SchemaHierarchy_ServerTriggers;
            var schemaHierarchyServiceBroker = SR.SchemaHierarchy_ServiceBroker;
            var schemaHierarchyServices = SR.SchemaHierarchy_Services;
            var schemaHierarchySignatures = SR.SchemaHierarchy_LogFiles;
            var schemaHierarchyStatistics = SR.SchemaHierarchy_Statistics;
            var schemaHierarchyStorage = SR.SchemaHierarchy_Storage;
            var schemaHierarchyStoredProcedures = SR.SchemaHierarchy_StoredProcedures;
            var schemaHierarchySymmetricKeys = SR.SchemaHierarchy_SymmetricKeys;
            var schemaHierarchySynonyms = SR.SchemaHierarchy_Synonyms;
            var schemaHierarchyTables = SR.SchemaHierarchy_Tables;
            var schemaHierarchyTriggers = SR.SchemaHierarchy_Triggers;
            var schemaHierarchyTypes = SR.SchemaHierarchy_Types;
            var schemaHierarchyUniqueKeys = SR.SchemaHierarchy_UniqueKeys;
            var schemaHierarchyUserDefinedDataTypes = SR.SchemaHierarchy_UserDefinedDataTypes;
            var schemaHierarchyUserDefinedTypes = SR.SchemaHierarchy_UserDefinedTypes;
            var schemaHierarchyUsers = SR.SchemaHierarchy_Users;
            var schemaHierarchyViews = SR.SchemaHierarchy_Views;
            var schemaHierarchyXmlIndexes = SR.SchemaHierarchy_XmlIndexes;
            var schemaHierarchyXMLSchemaCollections = SR.SchemaHierarchy_XMLSchemaCollections;
            var schemaHierarchyUserDefinedTableTypes = SR.SchemaHierarchy_UserDefinedTableTypes;
            var schemaHierarchyFilegroupFiles = SR.SchemaHierarchy_FilegroupFiles;
            var missingCaption = SR.MissingCaption;
            var schemaHierarchyBrokerPriorities = SR.SchemaHierarchy_BrokerPriorities;
            var schemaHierarchyCryptographicProviders = SR.SchemaHierarchy_CryptographicProviders;
            var schemaHierarchyDatabaseAuditSpecifications = SR.SchemaHierarchy_DatabaseAuditSpecifications;
            var schemaHierarchyDatabaseEncryptionKeys = SR.SchemaHierarchy_DatabaseEncryptionKeys;
            var schemaHierarchyEventSessions = SR.SchemaHierarchy_EventSessions;
            var schemaHierarchyFullTextStopLists = SR.SchemaHierarchy_FullTextStopLists;
            var schemaHierarchyResourcePools = SR.SchemaHierarchy_ResourcePools;
            var schemaHierarchyServerAudits = SR.SchemaHierarchy_ServerAudits;
            var schemaHierarchyServerAuditSpecifications = SR.SchemaHierarchy_ServerAuditSpecifications;
            var schemaHierarchySpatialIndexes = SR.SchemaHierarchy_SpatialIndexes;
            var schemaHierarchyWorkloadGroups = SR.SchemaHierarchy_WorkloadGroups;
            var schemaHierarchySqlFiles = SR.SchemaHierarchy_SqlFiles;
            var schemaHierarchyServerFunctions = SR.SchemaHierarchy_ServerFunctions;
            var schemaHierarchySqlType = SR.SchemaHierarchy_SqlType;
            var schemaHierarchyServerOptions = SR.SchemaHierarchy_ServerOptions;
            var schemaHierarchyDatabaseDiagrams = SR.SchemaHierarchy_DatabaseDiagrams;
            var schemaHierarchySystemTables = SR.SchemaHierarchy_SystemTables;
            var schemaHierarchyDatabases = SR.SchemaHierarchy_Databases;
            var schemaHierarchySystemContracts = SR.SchemaHierarchy_SystemContracts;
            var schemaHierarchySystemDatabases = SR.SchemaHierarchy_SystemDatabases;
            var schemaHierarchySystemMessageTypes = SR.SchemaHierarchy_SystemMessageTypes;
            var schemaHierarchySystemQueues = SR.SchemaHierarchy_SystemQueues;
            var schemaHierarchySystemServices = SR.SchemaHierarchy_SystemServices;
            var schemaHierarchySystemStoredProcedures = SR.SchemaHierarchy_SystemStoredProcedures;
            var schemaHierarchySystemViews = SR.SchemaHierarchy_SystemViews;
            var schemaHierarchyDataTierApplications = SR.SchemaHierarchy_DataTierApplications;
            var schemaHierarchyExtendedStoredProcedures = SR.SchemaHierarchy_ExtendedStoredProcedures;
            var schemaHierarchySystemAggregateFunctions = SR.SchemaHierarchy_SystemAggregateFunctions;
            var schemaHierarchySystemApproximateNumerics = SR.SchemaHierarchy_SystemApproximateNumerics;
            var schemaHierarchySystemBinaryStrings = SR.SchemaHierarchy_SystemBinaryStrings;
            var schemaHierarchySystemCharacterStrings = SR.SchemaHierarchy_SystemCharacterStrings;
            var schemaHierarchySystemCLRDataTypes = SR.SchemaHierarchy_SystemCLRDataTypes;
            var schemaHierarchySystemConfigurationFunctions = SR.SchemaHierarchy_SystemConfigurationFunctions;
            var schemaHierarchySystemCursorFunctions = SR.SchemaHierarchy_SystemCursorFunctions;
            var schemaHierarchySystemDataTypes = SR.SchemaHierarchy_SystemDataTypes;
            var schemaHierarchySystemDateAndTime = SR.SchemaHierarchy_SystemDateAndTime;
            var schemaHierarchySystemDateAndTimeFunctions = SR.SchemaHierarchy_SystemDateAndTimeFunctions;
            var schemaHierarchySystemExactNumerics = SR.SchemaHierarchy_SystemExactNumerics;
            var schemaHierarchySystemFunctions = SR.SchemaHierarchy_SystemFunctions;
            var schemaHierarchySystemHierarchyIdFunctions = SR.SchemaHierarchy_SystemHierarchyIdFunctions;
            var schemaHierarchySystemMathematicalFunctions = SR.SchemaHierarchy_SystemMathematicalFunctions;
            var schemaHierarchySystemMetadataFunctionions = SR.SchemaHierarchy_SystemMetadataFunctions;
            var schemaHierarchySystemOtherDataTypes = SR.SchemaHierarchy_SystemOtherDataTypes;
            var schemaHierarchySystemOtherFunctions = SR.SchemaHierarchy_SystemOtherFunctions;
            var schemaHierarchySystemRowsetFunctions = SR.SchemaHierarchy_SystemRowsetFunctions;
            var schemaHierarchySystemSecurityFunctions = SR.SchemaHierarchy_SystemSecurityFunctions;
            var schemaHierarchySystemSpatialDataTypes = SR.SchemaHierarchy_SystemSpatialDataTypes;
            var schemaHierarchySystemStringFunctions = SR.SchemaHierarchy_SystemStringFunctions;
            var schemaHierarchySystemSystemStatisticalFunctions = SR.SchemaHierarchy_SystemSystemStatisticalFunctions;
            var schemaHierarchySystemTextAndImageFunctions = SR.SchemaHierarchy_SystemTextAndImageFunctions;
            var schemaHierarchySystemUnicodeCharacterStrings = SR.SchemaHierarchy_SystemUnicodeCharacterStrings;
            var schemaHierarchyAggregateFunctions = SR.SchemaHierarchy_AggregateFunctions;
            var schemaHierarchyScalarValuedFunctions = SR.SchemaHierarchy_ScalarValuedFunctions;
            var schemaHierarchyTableValuedFunctions = SR.SchemaHierarchy_TableValuedFunctions;
            var schemaHierarchySystemExtendedStoredProcedures = SR.SchemaHierarchy_SystemExtendedStoredProcedures;
            var schemaHierarchyBuiltInType = SR.SchemaHierarchy_BuiltInType;
            var schemaHierarchyBuiltInServerRole = SR.SchemaHierarchy_BuiltInServerRole;
            var schemaHierarchyUserWithPassword = SR.SchemaHierarchy_UserWithPassword;
            var schemaHierarchySearchPropertyList = SR.SchemaHierarchy_SearchPropertyList;
            var schemaHierarchySecurityPolicies = SR.SchemaHierarchy_SecurityPolicies;
            var schemaHierarchySecurityPredicates = SR.SchemaHierarchy_SecurityPredicates;
            var schemaHierarchyServerRole = SR.SchemaHierarchy_ServerRole;
            var schemaHierarchySearchPropertyLists = SR.SchemaHierarchy_SearchPropertyLists;
            var schemaHierarchyColumnStoreIndexes = SR.SchemaHierarchy_ColumnStoreIndexes;
            var schemaHierarchyTableTypeIndexes = SR.SchemaHierarchy_TableTypeIndexes;
            var schemaHierarchyServerInstance = SR.SchemaHierarchy_Server;
            var schemaHierarchySelectiveXmlIndexes = SR.SchemaHierarchy_SelectiveXmlIndexes;
            var schemaHierarchyXmlNamespaces = SR.SchemaHierarchy_XmlNamespaces;
            var schemaHierarchyXmlTypedPromotedPaths = SR.SchemaHierarchy_XmlTypedPromotedPaths;
            var schemaHierarchySqlTypedPromotedPaths = SR.SchemaHierarchy_SqlTypedPromotedPaths;
            var schemaHierarchyDatabaseScopedCredentials = SR.SchemaHierarchy_DatabaseScopedCredentials;
            var schemaHierarchyExternalDataSources = SR.SchemaHierarchy_ExternalDataSources;
            var schemaHierarchyExternalFileFormats = SR.SchemaHierarchy_ExternalFileFormats;
            var schemaHierarchyExternalResources = SR.SchemaHierarchy_ExternalResources;
            var schemaHierarchyExternalTables = SR.SchemaHierarchy_ExternalTables;
            var schemaHierarchyAlwaysEncryptedKeys = SR.SchemaHierarchy_AlwaysEncryptedKeys;
            var schemaHierarchyColumnMasterKeys = SR.SchemaHierarchy_ColumnMasterKeys;
            var schemaHierarchyColumnEncryptionKeys = SR.SchemaHierarchy_ColumnEncryptionKeys;
        }

        [Fact]
        public void SrStringsTestWithEnLocalization()
        {
            string locale = "en";
            var args = new string[] { "--locale", locale };
            CommandOptions options = new CommandOptions(args);
            Assert.Equal(SR.Culture.Name, options.Locale);
            Assert.Equal(options.Locale, locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.Equal(TestLocalizationConstant, "test");
        }

        // [Fact]
        public void SrStringsTestWithEsLocalization()
        {
            string locale = "es";
            var args = new string[] { "--locale", locale };
            CommandOptions options = new CommandOptions(args);
            Assert.Equal(SR.Culture.Name, options.Locale);
            Assert.Equal(options.Locale, locale);

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.Equal(TestLocalizationConstant, "prueba");

            // Reset the locale
            SrStringsTestWithEnLocalization(); 
        }

        [Fact]
        public void SrStringsTestWithNullLocalization()
        {
            SR.Culture = null;
            var args = new string[] { "" };
            CommandOptions options = new CommandOptions(args);
            Assert.Null(SR.Culture);
            Assert.Equal(options.Locale, "");

            var TestLocalizationConstant = SR.TestLocalizationConstant;
            Assert.Equal(TestLocalizationConstant, "test");
        }
    }
}
