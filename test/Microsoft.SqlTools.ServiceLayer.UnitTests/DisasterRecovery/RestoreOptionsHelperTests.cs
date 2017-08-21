//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class RestoreOptionsHelperTests
    {
        [Fact]
        public void VerifyOptionsCreatedSuccessfullyIsResponse()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
        }

        [Fact]
        public void RelocateAllFilesShouldBeReadOnlyGivenNoDbFiles()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["DbFiles"] = new List<DbFile>();
            optionValues.Options[RestoreOptionsHelper.RelocateDbFiles] = true;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.RelocateDbFiles].IsReadOnly);
        }

        [Fact]
        public void DataFileFolderShouldBeReadOnlyGivenRelocateAllFilesSetToFalse()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["DbFiles"] = new List<DbFile>() { new DbFile("", '1', "") };
            optionValues.Options[RestoreOptionsHelper.RelocateDbFiles] = false;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.DataFileFolder].IsReadOnly);
            Assert.True(result[RestoreOptionsHelper.LogFileFolder].IsReadOnly);
        }

        [Fact]
        public void DataFileFolderShouldBeCurrentValueGivenRelocateAllFilesSetToTrue()
        {
            string dataFile = "data files";
            string logFile = "log files";
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["DbFiles"] = new List<DbFile>() { new DbFile("", '1', "") };
            optionValues.Options[RestoreOptionsHelper.RelocateDbFiles] = true;
            optionValues.Options[RestoreOptionsHelper.DataFileFolder] = dataFile;
            optionValues.Options[RestoreOptionsHelper.LogFileFolder] = logFile;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.False(result[RestoreOptionsHelper.DataFileFolder].IsReadOnly);
            Assert.False(result[RestoreOptionsHelper.LogFileFolder].IsReadOnly);
            Assert.Equal(result[RestoreOptionsHelper.DataFileFolder].CurrentValue, dataFile);
            Assert.Equal(result[RestoreOptionsHelper.LogFileFolder].CurrentValue, logFile);
        }


        [Fact]
        public void KeepReplicationShouldBeReadOnlyGivenRecoveryStateWithNoRecovery()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.KeepReplication].IsReadOnly);
        }

        [Fact]
        public void StandbyFileShouldBeReadOnlyGivenRecoveryStateNotWithStandBy()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.StandbyFile].IsReadOnly);
        }

        [Fact]
        public void BackupTailLogShouldBeReadOnlyTailLogBackupNotPossible()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["IsTailLogBackupPossible"] = false;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.BackupTailLog].IsReadOnly);
            Assert.True(result[RestoreOptionsHelper.TailLogBackupFile].IsReadOnly);
        }

        [Fact]
        public void TailLogWithNoRecoveryShouldBeReadOnlyTailLogBackupWithNoRecoveryNotPossible()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["IsTailLogBackupWithNoRecoveryPossible"] = false;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.TailLogWithNoRecovery].IsReadOnly);
        }

        [Fact]
        public void StandbyFileShouldNotBeReadOnlyGivenRecoveryStateWithStandBy()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithStandBy;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.False(result[RestoreOptionsHelper.StandbyFile].IsReadOnly);
        }

        [Fact]
        public void CloseExistingConnectionsShouldNotBeReadOnlyGivenCanDropExistingConnectionsSetToTrue()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["CanDropExistingConnections"] = true;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.False(result[RestoreOptionsHelper.CloseExistingConnections].IsReadOnly);
        }

        [Fact]
        public void CloseExistingConnectionsShouldBeReadOnlyGivenCanDropExistingConnectionsSetToFalse()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["CanDropExistingConnections"] = false;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.CloseExistingConnections].IsReadOnly);
        }

        [Fact]
        public void KeepReplicationShouldNotBeReadOnlyGivenRecoveryStateWithNoRecovery()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.KeepReplication].IsReadOnly);
        }

        [Fact]
        public void KeepReplicationShouldSetToDefaultValueGivenRecoveryStateWithNoRecovery()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            restoreParams.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;

            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);
            Dictionary<string, RestorePlanDetailInfo> options = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);

            restoreParams.Options[RestoreOptionsHelper.KeepReplication] = true;

            RestoreOptionsHelper.UpdateOptionsInPlan(restoreDatabaseTaskDataObject);
            bool actual = restoreDatabaseTaskDataObject.RestoreOptions.KeepReplication;
            bool expected = (bool)options[RestoreOptionsHelper.KeepReplication].DefaultValue;

            Assert.Equal(actual, expected);
        }

        [Fact]
        public void KeepReplicationShouldSetToValueInRequestGivenRecoveryStateWithRecovery()
        {
            RestoreParams restoreParams = CreateOptionsTestData();

            restoreParams.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);
            Dictionary<string, RestorePlanDetailInfo> options = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);

            restoreParams.Options[RestoreOptionsHelper.KeepReplication] = true;
            RestoreOptionsHelper.UpdateOptionsInPlan(restoreDatabaseTaskDataObject);

            bool actual = restoreDatabaseTaskDataObject.RestoreOptions.KeepReplication;
            bool expected = true;
            Assert.Equal(actual, expected);

        }

        [Fact]
        public void SourceDatabaseNameShouldSetToDefaultIfNotValid()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            string defaultDbName = "default";
            string currentDbName = "db3";
            restoreParams.Options["SourceDbNames"] = new List<string> { "db1", "db2" };
            restoreParams.Options["DefaultSourceDbName"] = defaultDbName;
            restoreParams.Options[RestoreOptionsHelper.SourceDatabaseName] = currentDbName;

            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);
            
            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.SourceDatabaseName, restoreDatabaseTaskDataObject);

            string actual = restoreDatabaseTaskDataObject.SourceDatabaseName;
            string expected = defaultDbName;
            Assert.Equal(actual, expected);
        }

        [Fact]
        public void SourceDatabaseNameShouldStayTheSameIfValid()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            string defaultDbName = "default";
            string currentDbName = "db3";
            restoreParams.Options["SourceDbNames"] = new List<string> { "db1", "db2", "db3" };
            restoreParams.Options["DefaultSourceDbName"] = defaultDbName;
            restoreParams.Options[RestoreOptionsHelper.SourceDatabaseName] = currentDbName;

            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);

            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.SourceDatabaseName, restoreDatabaseTaskDataObject);

            string actual = restoreDatabaseTaskDataObject.SourceDatabaseName;
            string expected = currentDbName;
            Assert.Equal(actual, expected);
        }

        [Fact]
        public void TargetDatabaseNameShouldSetToDefaultIfNotValid()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            string defaultDbName = "default";
            string currentDbName = "db3";
            restoreParams.Options["DefaultTargetDbName"] = defaultDbName;
            restoreParams.Options[RestoreOptionsHelper.TargetDatabaseName] = currentDbName;
            restoreParams.Options["CanChangeTargetDatabase"] = false;

            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);

            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.TargetDatabaseName, restoreDatabaseTaskDataObject);

            string actual = restoreDatabaseTaskDataObject.TargetDatabaseName;
            string expected = defaultDbName;
            Assert.Equal(actual, expected);
        }

        [Fact]
        public void TargetDatabaseNameShouldStayTheSameIfValid()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            string defaultDbName = "default";
            string currentDbName = "db3";
            restoreParams.Options["DefaultTargetDbName"] = defaultDbName;
            restoreParams.Options[RestoreOptionsHelper.TargetDatabaseName] = currentDbName;
            restoreParams.Options["CanChangeTargetDatabase"] = true;

            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);

            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.TargetDatabaseName, restoreDatabaseTaskDataObject);

            string actual = restoreDatabaseTaskDataObject.TargetDatabaseName;
            string expected = currentDbName;
            Assert.Equal(actual, expected);
        }


        private RestoreParams CreateOptionsTestData()
        {
            RestoreParams optionValues = new RestoreParams();
            optionValues.Options.Add(RestoreOptionsHelper.CloseExistingConnections, false);
            optionValues.Options.Add(RestoreOptionsHelper.DataFileFolder, "Data file folder");
            optionValues.Options.Add("DbFiles", new List<DbFile>() { new DbFile("", '1', "") });
            optionValues.Options.Add("DefaultDataFileFolder", "Default data file folder");
            optionValues.Options.Add("DefaultLogFileFolder", "Default log file folder");
            optionValues.Options.Add("IsTailLogBackupPossible", true);
            optionValues.Options.Add("IsTailLogBackupWithNoRecoveryPossible", true);
            optionValues.Options.Add("GetDefaultStandbyFile", "default standby file");
            optionValues.Options.Add("GetDefaultTailLogbackupFile", "default tail log backup file");
            optionValues.Options.Add(RestoreOptionsHelper.LogFileFolder, "Log file folder");
            optionValues.Options.Add(RestoreOptionsHelper.RelocateDbFiles, true);
            optionValues.Options.Add("TailLogBackupFile", "tail log backup file");
            optionValues.Options.Add("TailLogWithNoRecovery", false);
            optionValues.Options.Add(RestoreOptionsHelper.BackupTailLog, false);
            optionValues.Options.Add(RestoreOptionsHelper.KeepReplication, false);
            optionValues.Options.Add(RestoreOptionsHelper.ReplaceDatabase, false);
            optionValues.Options.Add(RestoreOptionsHelper.SetRestrictedUser, false);
            optionValues.Options.Add(RestoreOptionsHelper.StandbyFile, "Stand by file");
            optionValues.Options.Add(RestoreOptionsHelper.RecoveryState, DatabaseRecoveryState.WithNoRecovery.ToString());
            optionValues.Options.Add(RestoreOptionsHelper.TargetDatabaseName, "target db name");
            optionValues.Options.Add(RestoreOptionsHelper.SourceDatabaseName, "source db name");
            optionValues.Options.Add("CanChangeTargetDatabase", true);
            optionValues.Options.Add("DefaultSourceDbName", "DefaultSourceDbName");
            optionValues.Options.Add("DefaultTargetDbName", "DefaultTargetDbName");
            optionValues.Options.Add("SourceDbNames", new List<string>());
            optionValues.Options.Add("CanDropExistingConnections", true);
            return optionValues;
        }

        private IRestoreDatabaseTaskDataObject CreateRestoreDatabaseTaskDataObject(GeneralRequestDetails optionValues)
        {
            var restoreDataObject = new RestoreDatabaseTaskDataObjectStub();
            restoreDataObject.CloseExistingConnections = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.CloseExistingConnections);
            restoreDataObject.DataFilesFolder = optionValues.GetOptionValue<string>(RestoreOptionsHelper.DataFileFolder);
            restoreDataObject.DbFiles = optionValues.GetOptionValue<List<DbFile>>("DbFiles");
            restoreDataObject.DefaultDataFileFolder = optionValues.GetOptionValue<string>("DefaultDataFileFolder");
            restoreDataObject.DefaultLogFileFolder = optionValues.GetOptionValue<string>("DefaultLogFileFolder");
            restoreDataObject.IsTailLogBackupPossible = optionValues.GetOptionValue<bool>("IsTailLogBackupPossible");
            restoreDataObject.IsTailLogBackupWithNoRecoveryPossible = optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible");
            restoreDataObject.DefaultStandbyFile = optionValues.GetOptionValue<string>("GetDefaultStandbyFile");
            restoreDataObject.DefaultTailLogbackupFile = optionValues.GetOptionValue<string>("GetDefaultTailLogbackupFile");
            restoreDataObject.LogFilesFolder = optionValues.GetOptionValue<string>(RestoreOptionsHelper.LogFileFolder);
            restoreDataObject.RelocateAllFiles = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles);
            restoreDataObject.TailLogBackupFile = optionValues.GetOptionValue<string>("TailLogBackupFile");
            restoreDataObject.SourceDatabaseName = optionValues.GetOptionValue<string>(RestoreOptionsHelper.SourceDatabaseName);
            restoreDataObject.TargetDatabaseName = optionValues.GetOptionValue<string>(RestoreOptionsHelper.TargetDatabaseName);
            restoreDataObject.TailLogWithNoRecovery = optionValues.GetOptionValue<bool>("TailLogWithNoRecovery");
            restoreDataObject.CanChangeTargetDatabase = optionValues.GetOptionValue<bool>("CanChangeTargetDatabase");
            restoreDataObject.DefaultSourceDbName = optionValues.GetOptionValue<string>("DefaultSourceDbName");
            restoreDataObject.SourceDbNames = optionValues.GetOptionValue<List<string>>("SourceDbNames");
            restoreDataObject.DefaultTargetDbName = optionValues.GetOptionValue<string>("DefaultTargetDbName");
            restoreDataObject.BackupTailLog = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog);
            restoreDataObject.CanDropExistingConnections = optionValues.GetOptionValue<bool>("CanDropExistingConnections");
            restoreDataObject.RestoreParams = optionValues as RestoreParams;
            restoreDataObject.RestorePlan = null;
            RestoreOptions restoreOptions = new RestoreOptions();
            restoreOptions.KeepReplication = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication);
            restoreOptions.ReplaceDatabase = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.ReplaceDatabase);
            restoreOptions.SetRestrictedUser = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.SetRestrictedUser);
            restoreOptions.StandByFile = optionValues.GetOptionValue<string>(RestoreOptionsHelper.StandbyFile);
            restoreOptions.RecoveryState = optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState);
            restoreDataObject.RestoreOptions = restoreOptions;


            return restoreDataObject;
        }

        private void VerifyOptions(Dictionary<string, RestorePlanDetailInfo> optionInResponse, GeneralRequestDetails optionValues)
        {
            RestorePlanDetailInfo planDetailInfo = optionInResponse[RestoreOptionsHelper.DataFileFolder];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.DataFileFolder);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.DataFileFolder));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("DefaultDataFileFolder"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.LogFileFolder];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.LogFileFolder);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.LogFileFolder));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("DefaultLogFileFolder"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.RelocateDbFiles];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.RelocateDbFiles);
            Assert.Equal(planDetailInfo.IsReadOnly, (optionValues.GetOptionValue<List<DbFile>>("DbFiles").Count == 0));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.ReplaceDatabase];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.ReplaceDatabase);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.ReplaceDatabase));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.KeepReplication];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.KeepReplication);
            Assert.Equal(planDetailInfo.IsReadOnly,  optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState) == DatabaseRecoveryState.WithNoRecovery);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.SetRestrictedUser];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.SetRestrictedUser);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.SetRestrictedUser));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.RecoveryState];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.RecoveryState);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState).ToString());
            Assert.Equal(planDetailInfo.DefaultValue, DatabaseRecoveryState.WithRecovery.ToString());
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.StandbyFile];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.StandbyFile);
            Assert.Equal(planDetailInfo.IsReadOnly, optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState) != DatabaseRecoveryState.WithStandBy);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.StandbyFile));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("GetDefaultStandbyFile"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.BackupTailLog];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.BackupTailLog);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.TailLogBackupFile];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.TailLogBackupFile);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupPossible")
                | !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>("TailLogBackupFile"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("GetDefaultTailLogbackupFile"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.TailLogWithNoRecovery];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.TailLogWithNoRecovery);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible") 
                | !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("TailLogWithNoRecovery"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.CloseExistingConnections];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.CloseExistingConnections);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.CloseExistingConnections));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);
        }
        
    }
}
