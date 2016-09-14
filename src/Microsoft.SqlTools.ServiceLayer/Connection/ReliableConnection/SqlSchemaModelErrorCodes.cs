//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    internal static class SqlSchemaModelErrorCodes
    {
        private const int ParserErrorCodeStartIndex = 46000;
        private const int ParserErrorCodeEndIndex = 46499;

        public static bool IsParseErrorCode(int errorCode)
        {
            return
                (errorCode >= ParserErrorCodeStartIndex) &&
                (errorCode <= ParserErrorCodeEndIndex);
        }

        public static bool IsInterpretationErrorCode(int errorCode)
        {
            return
                (errorCode >= Interpretation.InterpretationBaseCode) &&
                (errorCode <= Interpretation.InterpretationEndCode);
        }

        public static bool IsStatementFilterError(int errorCode)
        {
            return
                (errorCode > StatementFilter.StatementFilterBaseCode) &&
                (errorCode <= StatementFilter.StatementFilterMaxErrorCode);
        }

        public static class StatementFilter
        {
            public const int StatementFilterBaseCode = 70000;

            public const int UnrecognizedStatement = StatementFilterBaseCode + 1;
            public const int ServerObject = StatementFilterBaseCode + 2;
            public const int AtMostTwoPartName = StatementFilterBaseCode + 3;
            public const int AlterTableAddColumn = StatementFilterBaseCode + 4;
            public const int ConstraintAll = StatementFilterBaseCode + 5;
            public const int TriggerAll = StatementFilterBaseCode + 6;
            public const int CreateSchemaWithoutName = StatementFilterBaseCode + 7;
            public const int CreateSchemaElements = StatementFilterBaseCode + 8;
            public const int AlterAssembly = StatementFilterBaseCode + 9;
            public const int CreateStoplist = StatementFilterBaseCode + 10;
            public const int UnsupportedPermission = StatementFilterBaseCode + 11;
            public const int TopLevelExecuteWithResultSets = StatementFilterBaseCode + 12;
            public const int AlterTableAddConstraint = StatementFilterBaseCode + 13;
            public const int DatabaseOnlyObjectInServerProject = StatementFilterBaseCode + 14;
            public const int UnsupportedBySqlAzure = StatementFilterBaseCode + 15;
            public const int UnsupportedSecurityObjectKind = StatementFilterBaseCode + 16;
            public const int StatementNotSupportedForCurrentRelease = StatementFilterBaseCode + 17;
            public const int ServerPermissionsNotAllowed = StatementFilterBaseCode + 18;
            public const int DeprecatedSyntax = StatementFilterBaseCode + 19;
            public const int SetRemoteData = StatementFilterBaseCode + 20;
            public const int StatementFilterMaxErrorCode = StatementFilterBaseCode + 499;
        }

        public static class Interpretation
        {
            public const int InterpretationBaseCode = 70500;

            public const int InvalidTopLevelStatement = InterpretationBaseCode + 1;
            public const int InvalidAssemblySource = InterpretationBaseCode + 2;
            public const int InvalidDatabaseName = InterpretationBaseCode + 3;
            public const int OnlyTwoPartNameAllowed = InterpretationBaseCode + 4;
            public const int SecurityObjectCannotBeNull = InterpretationBaseCode + 5;
            public const int UnknownPermission = InterpretationBaseCode + 6;
            public const int UnsupportedAll = InterpretationBaseCode + 7;
            public const int InvalidColumnList = InterpretationBaseCode + 8;
            public const int ColumnsAreNotAllowed = InterpretationBaseCode + 9;
            public const int InvalidDataType = InterpretationBaseCode + 10;
            public const int InvalidObjectName = InterpretationBaseCode + 11;
            public const int InvalidObjectChildName = InterpretationBaseCode + 12;
            public const int NoGlobalTemporarySymmetricKey = InterpretationBaseCode + 13;
            public const int NoGlobalTemporarySymmetricKey_Warning = InterpretationBaseCode + 14;
            public const int NameCannotBeNull = InterpretationBaseCode + 15;
            public const int NameCannotBeNull_Warning = InterpretationBaseCode + 16;
            public const int InvalidLoginName = InterpretationBaseCode + 17;
            public const int InvalidLoginName_Warning = InterpretationBaseCode + 18;
            public const int MoreAliasesThanColumns = InterpretationBaseCode + 19;
            public const int FewerAliasesThanColumns = InterpretationBaseCode + 20;
            public const int InvalidTimestampReturnType = InterpretationBaseCode + 21;
            public const int VariableParameterAtTopLevelStatement = InterpretationBaseCode + 22;
            public const int CannotCreateTempTable = InterpretationBaseCode + 23;
            public const int MultipleNullabilityConstraintError = InterpretationBaseCode + 24;
            public const int MultipleNullabilityConstraintWarning = InterpretationBaseCode + 25;
            public const int ColumnIsntAllowedForAssemblySource = InterpretationBaseCode + 26;
            public const int InvalidUserName = InterpretationBaseCode + 27;
            public const int InvalidWindowsLogin = InterpretationBaseCode + 28;
            public const int InvalidWindowsLogin_Warning = InterpretationBaseCode + 29;
            public const int CannotHaveUsingForPrimaryXmlIndex = InterpretationBaseCode + 30;
            public const int UsingIsRequiredForSecondaryXmlIndex = InterpretationBaseCode + 31;
            public const int XmlIndexTypeIsRequiredForSecondaryXmlIndex = InterpretationBaseCode + 32;
            public const int UnsupportedAlterCryptographicProvider = InterpretationBaseCode + 33;
            public const int HttpForSoapOnly = InterpretationBaseCode + 34;
            public const int UnknownEventTypeOrGroup = InterpretationBaseCode + 35;
            public const int CannotAddLogFileToFilegroup = InterpretationBaseCode + 36;
            public const int BuiltInTypeExpected = InterpretationBaseCode + 37;
            public const int MissingArgument = InterpretationBaseCode + 38;
            public const int InvalidArgument = InterpretationBaseCode + 39;
            public const int IncompleteBoundingBoxCoordinates = InterpretationBaseCode + 40;
            public const int XMaxLessThanXMin = InterpretationBaseCode + 41;
            public const int YMaxLessThanYMin = InterpretationBaseCode + 42;
            public const int InvalidCoordinate = InterpretationBaseCode + 43;
            public const int InvalidValue = InterpretationBaseCode + 44;
            public const int InvalidIdentityValue = InterpretationBaseCode + 45;
            public const int InvalidPriorityLevel = InterpretationBaseCode + 46;
            public const int TriggerIsNotForEvent = InterpretationBaseCode + 47;
            public const int SyntaxError = InterpretationBaseCode + 48;
            public const int UnsupportedPintable = InterpretationBaseCode + 49;
            public const int DuplicateEventType = InterpretationBaseCode + 50;
            public const int ClearAndBasicAreNotAllowed = InterpretationBaseCode + 51;
            public const int AssemblyCorruptErrorCode = InterpretationBaseCode + 57;
            public const int DynamicQuery = InterpretationBaseCode + 58;
            public const int OnlyLcidAllowed = InterpretationBaseCode + 59;
            public const int WildCardNotAllowed = InterpretationBaseCode + 60;
            public const int CannotBindSchema = InterpretationBaseCode + 61;
            public const int TableTypeNotAllowFunctionCall = InterpretationBaseCode + 62;
            public const int ColumnNotAllowed = InterpretationBaseCode + 63;
            public const int OwnerRequiredForEndpoint = InterpretationBaseCode + 64;
            public const int PartitionNumberMustBeInteger = InterpretationBaseCode + 65;
            public const int DuplicatedPartitionNumber = InterpretationBaseCode + 66;
            public const int FromPartitionGreaterThanToPartition = InterpretationBaseCode + 67;
            public const int CannotSpecifyPartitionNumber = InterpretationBaseCode + 68;
            public const int MissingColumnNameError = InterpretationBaseCode + 69;
            public const int MissingColumnNameWarning = InterpretationBaseCode + 70;
            public const int UnknownTableSourceError = InterpretationBaseCode + 71;
            public const int UnknownTableSourceWarning = InterpretationBaseCode + 72;
            public const int TooManyPartsForCteOrAliasError = InterpretationBaseCode + 73;
            public const int TooManyPartsForCteOrAliasWarning = InterpretationBaseCode + 74;
            public const int ServerAuditInvalidQueueDelayValue = InterpretationBaseCode + 75;
            public const int WrongEventType = InterpretationBaseCode + 76;
            public const int CantCreateUddtFromXmlError = InterpretationBaseCode + 77;
            public const int CantCreateUddtFromXmlWarning = InterpretationBaseCode + 78;
            public const int CantCreateUddtFromUddtError = InterpretationBaseCode + 79;
            public const int CantCreateUddtFromUddtWarning = InterpretationBaseCode + 80;
            public const int ForReplicationIsNotSupported = InterpretationBaseCode + 81;
            public const int TooLongIdentifier = InterpretationBaseCode + 82;
            public const int InvalidLanguageTerm = InterpretationBaseCode + 83;
            public const int InvalidParameterOrOption = InterpretationBaseCode + 85;
            public const int TableLevelForeignKeyWithNoColumnsError = InterpretationBaseCode + 86;
            public const int TableLevelForeignKeyWithNoColumnsWarning = InterpretationBaseCode + 87;
            public const int ConstraintEnforcementIsIgnored = InterpretationBaseCode + 88;
            public const int DeprecatedBackupOption = InterpretationBaseCode + 89;
            public const int UndeclaredVariableParameter = InterpretationBaseCode + 90;
            public const int UnsupportedAlgorithm = InterpretationBaseCode + 91;
            public const int InvalidLanguageNameOrAliasWarning = InterpretationBaseCode + 92;
            public const int UnsupportedRevoke = InterpretationBaseCode + 93;
            public const int InvalidPermissionTypeAgainstObject = InterpretationBaseCode + 94;
            public const int InvalidPermissionObjectType = InterpretationBaseCode + 95;
            public const int CannotDetermineSecurableFromPermission = InterpretationBaseCode + 96;
            public const int InvalidColumnListForSecurableType = InterpretationBaseCode + 97;
            public const int InvalidUserDefaultLanguage = InterpretationBaseCode + 98;
            public const int CannotSpecifyGridParameterForAutoGridSpatialIndex = InterpretationBaseCode + 99;
            public const int UnsupportedSpatialTessellationScheme = InterpretationBaseCode + 100;
            public const int CannotSpecifyBoundingBoxForGeography = InterpretationBaseCode + 101;
            public const int InvalidSearchPropertyId = InterpretationBaseCode + 102;
            public const int OnlineSpatialIndex = InterpretationBaseCode + 103;
            public const int SqlCmdVariableInObjectName = InterpretationBaseCode + 104;
            public const int SubqueriesNotAllowed = InterpretationBaseCode + 105;
            public const int ArgumentReplaceNotSupported = InterpretationBaseCode + 106;
            public const int DuplicateArgument = InterpretationBaseCode + 107;
            public const int UnsupportedNoPopulationChangeTrackingOption = InterpretationBaseCode + 108;
            public const int UnsupportedResourceManagerLocationProperty = InterpretationBaseCode + 109;
            public const int RequiredExternalDataSourceLocationPropertyMissing = InterpretationBaseCode + 110;
            public const int UnsupportedSerdeMethodProperty = InterpretationBaseCode + 111;
            public const int UnsupportedFormatOptionsProperty = InterpretationBaseCode + 112;
            public const int RequiredSerdeMethodPropertyMissing = InterpretationBaseCode + 113;
            public const int TableLevelIndexWithNoColumnsError = InterpretationBaseCode + 114;
            public const int TableLevelIndexWithNoColumnsWarning = InterpretationBaseCode + 115;
            public const int InvalidIndexOption = InterpretationBaseCode + 116;
            public const int TypeAndSIDMustBeUsedTogether = InterpretationBaseCode + 117;
            public const int TypeCannotBeUsedWithLoginOption = InterpretationBaseCode + 118;
            public const int InvalidUserType = InterpretationBaseCode + 119;
            public const int InvalidUserSid = InterpretationBaseCode + 120;
            public const int InvalidPartitionFunctionDataType = InterpretationBaseCode + 121;
            public const int RequiredExternalTableLocationPropertyMissing = InterpretationBaseCode + 122;
            public const int UnsupportedRejectSampleValueProperty = InterpretationBaseCode + 123;
            public const int RequiredExternalDataSourceDatabasePropertyMissing = InterpretationBaseCode + 124;            
            public const int RequiredExternalDataSourceShardMapNamePropertyMissing = InterpretationBaseCode + 125;
            public const int InvalidPropertyForExternalDataSourceType = InterpretationBaseCode + 126;
            public const int UnsupportedExternalDataSourceTypeInCurrentPlatform = InterpretationBaseCode + 127;
            public const int UnsupportedExternalTableProperty = InterpretationBaseCode + 128;
            public const int MaskingFunctionIsEmpty = InterpretationBaseCode + 129;
            public const int InvalidMaskingFunctionFormat = InterpretationBaseCode + 130;
            public const int CannotCreateAlwaysEncryptedObject = InterpretationBaseCode + 131;
            public const int ExternalTableSchemaOrObjectNameMissing = InterpretationBaseCode + 132;
            public const int CannotCreateTemporalTableWithoutHistoryTableName = InterpretationBaseCode + 133;
            public const int TemporalPeriodColumnMustNotBeNullable = InterpretationBaseCode + 134;
            public const int InterpretationEndCode = InterpretationBaseCode + 499;
        }

        public static class ModelBuilder
        {
            private const int ModelBuilderBaseCode = 71000;

            public const int CannotFindMainElement = ModelBuilderBaseCode + 1;
            public const int CannotFindColumnSourceGrantForColumnRevoke = ModelBuilderBaseCode + 2;
            public const int AssemblyReferencesNotSupported = ModelBuilderBaseCode + 3;
            public const int NoSourceForColumn = ModelBuilderBaseCode + 5;
            public const int MoreThanOneStatementPerBatch = ModelBuilderBaseCode + 6;
            public const int MaximumSizeExceeded = ModelBuilderBaseCode + 7;
        }

        public static class Validation
        {
            private const int ValidationBaseCode = 71500;

            public const int AllReferencesMustBeResolved = ValidationBaseCode + 1;
            public const int AllReferencesMustBeResolved_Warning = ValidationBaseCode + 2;
            public const int AssemblyVisibilityRule = ValidationBaseCode + 3;
            public const int BreakContinueOnlyInWhile = ValidationBaseCode + 4;
            public const int ClrObjectAssemblyReference_InvalidAssembly = ValidationBaseCode + 5;
            public const int ClrObjectAssemblyReference = ValidationBaseCode + 6;
            public const int ColumnUserDefinedTableType = ValidationBaseCode + 7;
            public const int DuplicateName = ValidationBaseCode + 8;
            public const int DuplicateName_Warning = ValidationBaseCode + 9;
            public const int DuplicateVariableParameterName_TemporaryTable = ValidationBaseCode + 10;
            public const int DuplicateVariableParameterName_Variable = ValidationBaseCode + 11;
            public const int EndPointRule_DATABASE_MIRRORING = ValidationBaseCode + 12;
            public const int EndPointRule_SERVICE_BROKER = ValidationBaseCode + 13;
            public const int ForeignKeyColumnTypeNumberMustMatch_NumberOfColumns = ValidationBaseCode + 14;
            public const int ForeignKeyColumnTypeNumberMustMatch_TypeMismatch = ValidationBaseCode + 15;
            public const int ForeignKeyReferencePKUnique = ValidationBaseCode + 16;
            public const int FullTextIndexColumn =  ValidationBaseCode + 17;
            public const int IdentityColumnValidation_InvalidType = ValidationBaseCode + 18;
            public const int IdentityColumnValidation_MoreThanOneIdentity = ValidationBaseCode + 19;
            public const int InsertIntoIdentityColumn = ValidationBaseCode + 20;
            public const int MatchingSignatureNotFoundInAssembly = ValidationBaseCode + 21;
            public const int MatchingTypeNotFoundInAssembly = ValidationBaseCode + 22;
            public const int MaxColumnInIndexKey = ValidationBaseCode + 25;
            public const int MaxColumnInTable_1024Columns = ValidationBaseCode + 26;
            public const int MultiFullTextIndexOnTable = ValidationBaseCode + 28;
            public const int NonNullPrimaryKey_NonNullSimpleColumn = ValidationBaseCode + 29;
            public const int NonNullPrimaryKey_NotPersistedComputedColumn = ValidationBaseCode + 30;
            public const int OneClusteredIndex = ValidationBaseCode + 31;
            public const int OneMasterKey = ValidationBaseCode + 32;
            public const int OnePrimaryKey = ValidationBaseCode + 33;
            public const int PrimaryXMLIndexClustered = ValidationBaseCode + 34;
            public const int SelectAssignRetrieval = ValidationBaseCode + 35;
            public const int SubroutineParameterReadOnly_NonUDTTReadOnly = ValidationBaseCode + 36;
            public const int SubroutineParameterReadOnly_UDTTReadOnly = ValidationBaseCode + 37;
            public const int UsingXMLIndex = ValidationBaseCode + 38;
            public const int VardecimalOptionRule = ValidationBaseCode + 39;
            public const int WildCardExpansion = ValidationBaseCode + 40;
            public const int WildCardExpansion_Warning = ValidationBaseCode + 41;
            public const int XMLIndexOnlyXMLTypeColumn = ValidationBaseCode + 42;
            public const int TableVariablePrefix = ValidationBaseCode + 44;
            public const int FileStream_FILESTREAMON = ValidationBaseCode + 45;
            public const int FileStream_ROWGUIDCOLUMN = ValidationBaseCode + 46;
            public const int MaxColumnInTable100_Columns = ValidationBaseCode + 47;
            public const int XMLIndexOnlyXMLTypeColumn_SparseColumnSet = ValidationBaseCode + 48;
            public const int ClrObjectAssemblyReference_ParameterTypeMismatch = ValidationBaseCode + 50;
            public const int OneDefaultConstraintPerColumn = ValidationBaseCode + 51;
            public const int PermissionStatementValidation_DuplicatePermissionOnSecurable = ValidationBaseCode + 52;
            public const int PermissionStatementValidation_ConflictingPermissionsOnSecurable = ValidationBaseCode + 53;
            public const int PermissionStatementValidation_ConflictingColumnStatements = ValidationBaseCode + 54;
            public const int PermissionOnObjectSecurableValidation_InvalidPermissionForObject = ValidationBaseCode + 55;
            public const int SequenceValueValidation_ValueOutOfRange = ValidationBaseCode + 56;
            public const int SequenceValueValidation_InvalidDataType = ValidationBaseCode + 57;
            public const int MismatchedName_Warning = ValidationBaseCode + 58;
            public const int DifferentNameCasing_Warning = ValidationBaseCode + 59;
            public const int OneClusteredIndexAzure = ValidationBaseCode + 60;
            public const int AllExternalReferencesMustBeResolved = ValidationBaseCode + 61;
            public const int AllExternalReferencesMustBeResolved_Warning = ValidationBaseCode + 62;
            public const int ExternalObjectWildCardExpansion_Warning = ValidationBaseCode + 63;
            public const int UnsupportedElementForDataPackage = ValidationBaseCode + 64;
            public const int InvalidFileStreamOptions = ValidationBaseCode + 65;
            public const int StorageShouldNotSetOnDifferentInstance = ValidationBaseCode + 66;
            public const int TableShouldNotHaveStorage = ValidationBaseCode + 67;
            public static int MemoryOptimizedObjectsValidation_NonMemoryOptimizedTableCannotBeAccessed = ValidationBaseCode + 68;
            public static int MemoryOptimizedObjectsValidation_SyntaxNotSupportedOnHekatonElement = ValidationBaseCode + 69;
            public static int MemoryOptimizedObjectsValidation_ValidatePrimaryKeyForSchemaAndDataTables = ValidationBaseCode + 70;
            public static int MemoryOptimizedObjectsValidation_ValidatePrimaryKeyForSchemaOnlyTables = ValidationBaseCode + 71;
            public static int MemoryOptimizedObjectsValidation_OnlyNotNullableColumnsOnIndexes = ValidationBaseCode + 72;
            public static int MemoryOptimizedObjectsValidation_HashIndexesOnlyOnMemoryOptimizedObjects = ValidationBaseCode + 73;
            public static int MemoryOptimizedObjectsValidation_OptionOnlyForHashIndexes = ValidationBaseCode + 74;
            public static int IncrementalStatisticsValidation_FilterNotSupported = ValidationBaseCode + 75;
            public static int IncrementalStatisticsValidation_ViewNotSupported = ValidationBaseCode + 76;
            public static int IncrementalStatisticsValidation_IndexNotPartitionAligned = ValidationBaseCode + 77;
            public static int AzureV12SurfaceAreaValidation = ValidationBaseCode + 78;
            public static int DuplicatedTargetObjectReferencesInSecurityPolicy = ValidationBaseCode + 79;
            public static int MultipleSecurityPoliciesOnTargetObject = ValidationBaseCode + 80;
            public static int ExportedRowsMayBeIncomplete = ValidationBaseCode + 81;
            public static int ExportedRowsMayContainSomeMaskedData = ValidationBaseCode + 82;
            public const int EncryptedColumnValidation_EncryptedPrimaryKey = ValidationBaseCode + 83;
            public const int EncryptedColumnValidation_EncryptedUniqueColumn = ValidationBaseCode + 84;
            public const int EncryptedColumnValidation_EncryptedCheckConstraint = ValidationBaseCode + 85;
            public const int EncryptedColumnValidation_PrimaryKeyForeignKeyEncryptionMismatch = ValidationBaseCode + 86;
            public const int EncryptedColumnValidation_UnsupportedDataType = ValidationBaseCode + 87;
            public const int MemoryOptimizedObjectsValidation_UnSupportedOption = ValidationBaseCode + 88;
            public const int MasterKeyExistsForCredential = ValidationBaseCode + 89;
            public const int MemoryOptimizedObjectsValidation_InvalidForeignKeyRelationship = ValidationBaseCode + 90;
            public const int MemoryOptimizedObjectsValidation_UnsupportedForeignKeyReference = ValidationBaseCode + 91;
            public const int EncryptedColumnValidation_RowGuidColumn = ValidationBaseCode + 92;
            public const int EncryptedColumnValidation_EncryptedClusteredIndex = ValidationBaseCode + 93;
            public const int EncryptedColumnValidation_EncryptedNonClusteredIndex = ValidationBaseCode + 94;
            public const int EncryptedColumnValidation_DependentComputedColumn = ValidationBaseCode + 95;
            public const int EncryptedColumnValidation_EncryptedFullTextColumn = ValidationBaseCode + 96;
            public const int EncryptedColumnValidation_EncryptedSparseColumnSet = ValidationBaseCode + 97;
            public const int EncryptedColumnValidation_EncryptedStatisticsColumn = ValidationBaseCode + 98;
            public const int EncryptedColumnValidation_EncryptedPartitionColumn = ValidationBaseCode + 99;
            public const int EncryptedColumnValidation_PrimaryKeyChangeTrackingColumn = ValidationBaseCode + 100;
            public const int EncryptedColumnValidation_ChangeDataCaptureOn = ValidationBaseCode + 101;
            public const int EncryptedColumnValidation_FilestreamColumn = ValidationBaseCode + 102;
            public const int EncryptedColumnValidation_MemoryOptimizedTable = ValidationBaseCode + 103;
            public const int EncryptedColumnValidation_MaskedEncryptedColumn = ValidationBaseCode + 104;
            public const int EncryptedColumnValidation_EncryptedIdentityColumn = ValidationBaseCode + 105;
            public const int EncryptedColumnValidation_EncryptedDefaultConstraint = ValidationBaseCode + 106;
            public const int TemporalValidation_InvalidPeriodSpecification = ValidationBaseCode + 107;
            public const int TemporalValidation_MultipleCurrentTables = ValidationBaseCode + 108;
            public const int TemporalValidation_SchemaMismatch = ValidationBaseCode + 109;
            public const int TemporalValidation_ComputedColumns = ValidationBaseCode + 110;
            public const int TemporalValidation_NoAlwaysEncryptedCols = ValidationBaseCode + 111;
            public static int IndexesOnExternalTable = ValidationBaseCode + 112;
            public static int TriggersOnExternalTable = ValidationBaseCode + 113;
            public const int StretchValidation_ExportBlocked = ValidationBaseCode + 114;
            public const int StretchValidation_ImportBlocked = ValidationBaseCode + 115;
            public const int DeploymentBlocked = ValidationBaseCode + 116;
            public const int NoBlockPredicatesTargetingViews = ValidationBaseCode + 117;
            public const int SchemaBindingOnSecurityPoliciesValidation = ValidationBaseCode + 118;
            public const int SecurityPredicateTargetObjectValidation = ValidationBaseCode + 119;
            public const int TemporalValidation_SchemaMismatch_ColumnCount = ValidationBaseCode + 120;
            public const int AkvValidation_AuthenticationFailed = ValidationBaseCode + 121;
            public const int TemporalValidation_PrimaryKey = ValidationBaseCode + 122;
        }

        public static class SqlMSBuild
        {
            private const int MSBuildBaseCode = 72000;

            public const int FileDoesNotExist = MSBuildBaseCode + 1;
            public const int UnknownDeployError = MSBuildBaseCode + 2;
            public const int InvalidProperty = MSBuildBaseCode + 3;
            public const int CollationError = MSBuildBaseCode + 4;
            public const int InvalidSqlClrDefinition = MSBuildBaseCode + 5;
            public const int SQL_PrePostFatalParserError = MSBuildBaseCode + 6;
            public const int SQL_PrePostSyntaxCheckError = MSBuildBaseCode + 7;
            public const int SQL_PrePostVariableError = MSBuildBaseCode + 8;
            public const int SQL_CycleError = MSBuildBaseCode + 9;
            public const int SQL_NoConnectionStringNoServerVerification = MSBuildBaseCode + 10;
            public const int SQL_VardecimalMismatch = MSBuildBaseCode + 11;
            public const int SQL_NoAlterFileSystemObject = MSBuildBaseCode + 12;
            public const int SQL_SqlCmdVariableOverrideError = MSBuildBaseCode + 13;
            public const int SQL_BatchError = MSBuildBaseCode + 14;
            public const int SQL_DataLossError = MSBuildBaseCode + 15;
            public const int SQL_ExecutionError = MSBuildBaseCode + 16;
            public const int SQL_UncheckedConstraint = MSBuildBaseCode + 17;
            public const int SQL_UnableToImportElements = MSBuildBaseCode + 18;
            public const int SQL_TargetReadOnlyError = MSBuildBaseCode + 19;
            public const int SQL_UnsupportedCompatibilityMode = MSBuildBaseCode + 20;
            public const int SQL_IncompatibleDSPVersions = MSBuildBaseCode + 21;
            public const int SQL_CouldNotLoadSymbols = MSBuildBaseCode + 22;
            public const int SQL_ContainmentlMismatch = MSBuildBaseCode + 23;
            public const int SQL_PrePostExpectedNoTSqlError = MSBuildBaseCode + 24;
            public const int ReferenceErrorCode = MSBuildBaseCode + 25;
            public const int FileError = MSBuildBaseCode + 26;
            public const int MissingReference = MSBuildBaseCode + 27;
            public const int SerializationError = MSBuildBaseCode + 28;
            public const int DeploymentContributorVerificationError = MSBuildBaseCode + 29;
            public const int Deployment_PossibleRuntimeError = MSBuildBaseCode + 30;
            public const int Deployment_BlockingDependency = MSBuildBaseCode + 31;
            public const int Deployment_TargetObjectLoss = MSBuildBaseCode + 32;
            public const int Deployment_MissingDependency = MSBuildBaseCode + 33;
            public const int Deployment_PossibleDataLoss = MSBuildBaseCode + 34;
            public const int Deployment_NotSupportedOperation = MSBuildBaseCode + 35;
            public const int Deployment_Information = MSBuildBaseCode + 36;
            public const int Deployment_UnsupportedDSP = MSBuildBaseCode + 37;
            public const int Deployment_SkipManagementScopedChange = MSBuildBaseCode + 38;
            public const int StaticCodeAnalysis_GeneralException = MSBuildBaseCode + 39;
            public const int StaticCodeAnalysis_ResultsFileIOException = MSBuildBaseCode + 40;
            public const int StaticCodeAnalysis_FailToCreateTaskHost = MSBuildBaseCode + 41;
            public const int StaticCodeAnalysis_InvalidDataSchemaModel = MSBuildBaseCode + 42;
            public const int StaticCodeAnalysis_InvalidElement = MSBuildBaseCode + 43;
            public const int Deployment_NoClusteredIndex = MSBuildBaseCode + 44;
            public const int Deployment_DetailedScriptExecutionError = MSBuildBaseCode + 45;
        }

        public static class Refactoring
        {
            private const int RefactoringBaseCode = 72500;

            public const int FailedToLoadFile = RefactoringBaseCode + 1;
        }

        /// <summary>
        /// These codes are used to message specific actions for extract and deployment operations.
        /// The primary consumer of these codes is the Import/Export service.
        /// </summary>
        public static class ServiceActions
        {
            public const int ServiceActionsBaseCode = 73000;
            public const int ServiceActionsMaxCode = 73000 + 0xFF;

            // Note: These codes are defined so that the lower 3 bits indicate one of three
            //       event stages: Started (0x01), Done/Complete (0x02), Done/Failed (0x04)
            public const int DeployInitializeStart = ServiceActionsBaseCode + 0x01;
            public const int DeployInitializeSuccess = ServiceActionsBaseCode + 0x02;
            public const int DeployInitializeFailure = ServiceActionsBaseCode + 0x04;

            public const int DeployAnalysisStart = ServiceActionsBaseCode + 0x11;
            public const int DeployAnalysisSuccess = ServiceActionsBaseCode + 0x12;
            public const int DeployAnalysisFailure = ServiceActionsBaseCode + 0x14;
            
            public const int DeployExecuteScriptStart = ServiceActionsBaseCode + 0x21;
            public const int DeployExecuteScriptSuccess = ServiceActionsBaseCode + 0x22;
            public const int DeployExecuteScriptFailure = ServiceActionsBaseCode + 0x24;
            
            public const int DataImportStart = ServiceActionsBaseCode + 0x41;
            public const int DataImportSuccess = ServiceActionsBaseCode + 0x42;
            public const int DataImportFailure = ServiceActionsBaseCode + 0x44;
            
            public const int ExtractSchemaStart = ServiceActionsBaseCode + 0x61;
            public const int ExtractSchemaSuccess = ServiceActionsBaseCode + 0x62;
            public const int ExtractSchemaFailure = ServiceActionsBaseCode + 0x64;
            
            public const int ExportVerifyStart = ServiceActionsBaseCode + 0x71;
            public const int ExportVerifySuccess = ServiceActionsBaseCode + 0x72;
            public const int ExportVerifyFailure = ServiceActionsBaseCode + 0x74;
            
            public const int ExportDataStart = ServiceActionsBaseCode + 0x81;
            public const int ExportDataSuccess = ServiceActionsBaseCode + 0x82;
            public const int ExportDataFailure = ServiceActionsBaseCode + 0x84;

            public const int EnableIndexesDataStart = ServiceActionsBaseCode + 0xb1;
            public const int EnableIndexesDataSuccess = ServiceActionsBaseCode + 0xb2;
            public const int EnableIndexesDataFailure = ServiceActionsBaseCode + 0xb4;

            public const int DisableIndexesDataStart = ServiceActionsBaseCode + 0xc1;
            public const int DisableIndexesDataSuccess = ServiceActionsBaseCode + 0xc2;
            public const int DisableIndexesDataFailure = ServiceActionsBaseCode + 0xc4;

            public const int EnableIndexDataStart = ServiceActionsBaseCode + 0xd1;
            public const int EnableIndexDataSuccess = ServiceActionsBaseCode + 0xd2;
            public const int EnableIndexDataFailure = ServiceActionsBaseCode + 0xd4;

            public const int DisableIndexDataStart = ServiceActionsBaseCode + 0xe1;
            public const int DisableIndexDataSuccess = ServiceActionsBaseCode + 0xe2;
            public const int DisableIndexDataFailure = ServiceActionsBaseCode + 0xe4;
            
            public const int ColumnEncryptionDataMigrationStart = ServiceActionsBaseCode + 0xf1;
            public const int ColumnEncryptionDataMigrationSuccess = ServiceActionsBaseCode + 0xf2;
            public const int ColumnEncryptionDataMigrationFailure = ServiceActionsBaseCode + 0xf4;
            
            // These codes do not set the lower 3 bits
            public const int ConnectionRetry = ServiceActionsBaseCode + 0x90;
            public const int CommandRetry = ServiceActionsBaseCode + 0x91;
            public const int GeneralProgress = ServiceActionsBaseCode + 0x92;
            public const int TypeFidelityLoss = ServiceActionsBaseCode + 0x93;
            public const int TableProgress = ServiceActionsBaseCode + 0x94;
            public const int ImportBlocked = ServiceActionsBaseCode + 0x95;
            public const int DataPrecisionLoss = ServiceActionsBaseCode + 0x96;
            public const int DataRowCount = ServiceActionsBaseCode + 0x98;

            public const int DataException = ServiceActionsBaseCode + 0xA0;
            public const int LogEntry = ServiceActionsBaseCode + 0xA1;
            public const int GeneralInfo = ServiceActionsBaseCode + 0xA2;


        }
    }
}
