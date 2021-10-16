/**************************************************************
*  Copyright (C) Microsoft Corporation. All rights reserved.  *
**************************************************************/

namespace Microsoft.Data.Tools.Components.Diagnostics
{
    /// <summary>
    /// Enumeration of values used as trace event identifiers that semantically represent the major categories of product features.
    /// </summary>
    internal enum SqlTraceId : uint
    {
        CoreServices = 0,                        // Taxonomy – Core Services (-TSQL Model)
        TSqlModel = 1,                           // Taxonomy Core Services.TSQL Model
        LanguageServices = 2,                    // Taxonomy – TSQL Language Services
        VSShell = 3,                             // Taxonomy Designers & Explorers, Productivity Tools, VS Integration (for TSQL, Table Designer and Entity Designer)
        EntityDesigner = 4,                      // Entity Designer generic bucket (for Entity Designer scenarios that do not fit into one of the categories below)
        EntityDesigner_ForwardIntegration = 5,   // Entity Designer creating DB scripts from a model (Model First)
        EntityDesigner_GenerateFromModel = 6,    // Entity Designer changing something external to the EDMX using the model as input; build, validation, codegen, connection strings etc
        EntityDesigner_ModelChanges = 7,         // Entity Designer creating/deleting/updating model elements using any Entity Designer window or dialog
        EntityDesigner_OData = 8,                // Entity Designer OData code
        EntityDesigner_ReverseIntegration = 9,   // Entity Designer creating a model from a database
        TableDesigner = 10,                      // Table Designer generic bucket (for Table Designer scenarios that do not fit into one of the categories below)
        TableDesigner_ReadFromModel = 11,        // Table Designer where the user is reading from the model
        TableDesigner_WriteToModel = 12,         // Table Designer where the user is writing to the model
        AppDB = 13,                              // Application-Database Integration
        QueryResults = 14,                       // QueryResults
        Debugger = 15,                           // Debugger
        TSqlEditorAndLanguageServices = 16,      // T-Sql Editor & Language Services (intellisense, snippets, formatting...)
        SchemaCompare = 17,                      // Schema Compare
        CommandlineTooling = 18,                 // SqlPackage.exe and WebDeploy Provider
        DacApi = 19,                             // DAC public API
        PdwExtensions = 20,                      // SQL PDW Extensions
        UnitTesting = 21,                        // Unit Testing
        Extensibility = 22,                      // MEF Extensibility
        DataCompare = 23,                        // Data Compare
        Telemetry = 24,                          // Telemetry
        ConnectionDialog = 25,                   //Connection Dialog
        AlwaysEncryptedKeysDialog = 26,          //Always Encrypted Keys Dialogs

        InternalTest = 99                        // internal, non-shipping test code       
    }
}
