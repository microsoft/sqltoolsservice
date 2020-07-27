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
using Microsoft.SqlTools.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class RestoreOptionsHelperTests
    {
        [Test]
        public void VerifyOptionsCreatedSuccessfullyIsResponse()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
        }

        [Test]
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

        [Test]
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

        [Test]
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
            Assert.AreEqual(result[RestoreOptionsHelper.DataFileFolder].CurrentValue, dataFile);
            Assert.AreEqual(result[RestoreOptionsHelper.LogFileFolder].CurrentValue, logFile);
        }


        [Test]
        public void KeepReplicationShouldBeReadOnlyGivenRecoveryStateWithNoRecovery()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.KeepReplication].IsReadOnly);
        }

        [Test]
        public void StandbyFileShouldBeReadOnlyGivenRecoveryStateNotWithStandBy()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.StandbyFile].IsReadOnly);
        }

        [Test]
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

        [Test]
        public void TailLogWithNoRecoveryShouldBeReadOnlyTailLogBackupWithNoRecoveryNotPossible()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["IsTailLogBackupWithNoRecoveryPossible"] = false;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.TailLogWithNoRecovery].IsReadOnly);
        }

        [Test]
        public void StandbyFileShouldNotBeReadOnlyGivenRecoveryStateWithStandBy()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithStandBy;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.False(result[RestoreOptionsHelper.StandbyFile].IsReadOnly);
        }

        [Test]
        public void CloseExistingConnectionsShouldNotBeReadOnlyGivenCanDropExistingConnectionsSetToTrue()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["CanDropExistingConnections"] = true;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.False(result[RestoreOptionsHelper.CloseExistingConnections].IsReadOnly);
        }

        [Test]
        public void CloseExistingConnectionsShouldBeReadOnlyGivenCanDropExistingConnectionsSetToFalse()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["CanDropExistingConnections"] = false;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.CloseExistingConnections].IsReadOnly);
        }

        [Test]
        public void KeepReplicationShouldNotBeReadOnlyGivenRecoveryStateWithNoRecovery()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject);
            Assert.NotNull(result);
            Assert.True(result[RestoreOptionsHelper.KeepReplication].IsReadOnly);
        }

        [Test]
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

            Assert.AreEqual(actual, expected);
        }

        [Test]
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
            Assert.AreEqual(actual, expected);

        }

        [Test]
        public void SourceDatabaseNameShouldSetToDefaultIfNotValid()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            string defaultDbName = "default";
            string currentDbName = null;
            restoreParams.Options["SourceDbNames"] = new List<string> { "db1", "db2" };
            restoreParams.Options["DefaultSourceDbName"] = defaultDbName;
            restoreParams.Options[RestoreOptionsHelper.SourceDatabaseName] = currentDbName;

            IRestoreDatabaseTaskDataObject restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);
            
            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.SourceDatabaseName, restoreDatabaseTaskDataObject);

            string actual = restoreDatabaseTaskDataObject.SourceDatabaseName;
            string expected = defaultDbName;
            Assert.AreEqual(actual, expected);
        }

        [Test]
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
            Assert.AreEqual(actual, expected);
        }

        [Test]
        public void TargetDatabaseNameShouldBeWhatIsRequested()
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
            string expected = currentDbName;
            Assert.AreEqual(actual, expected);
        }

        [Test]
        public void TargetDatabaseNameShouldBeWhatIsRequested2()
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
            Assert.AreEqual(actual, expected);
        }


        private RestoreParams CreateOptionsTestData()
        {
            RestoreParams optionValues = new RestoreParams();
            optionValues.Options.Add(RestoreOptionsHelper.CloseExistingConnections, false);
            optionValues.Options.Add(RestoreOptionsHelper.DataFileFolder, "Data file folder");
            optionValues.Options.Add("DbFiles", new List<DbFile>() { new DbFile("", '1', "") });
            optionValues.Options.Add("DefaultDataFileFolder", "Default data file folder");
            optionValues.Options.Add("DefaultLogFileFolder", "Default log file folder");
            optionValues.Options.Add("DefaultBackupFolder", "Default backup folder");
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
            restoreDataObject.DefaultBackupFolder = optionValues.GetOptionValue<string>("DefaultBackupFolder");
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
            restoreDataObject.OverwriteTargetDatabase = optionValues.GetOptionValue<bool>("CanChangeTargetDatabase");
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
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.DataFileFolder);
            Assert.AreEqual(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles));
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.DataFileFolder));
            Assert.AreEqual(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("DefaultDataFileFolder"));
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.LogFileFolder];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.LogFileFolder);
            Assert.AreEqual(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles));
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.LogFileFolder));
            Assert.AreEqual(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("DefaultLogFileFolder"));
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.RelocateDbFiles];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.RelocateDbFiles);
            Assert.AreEqual(planDetailInfo.IsReadOnly, (optionValues.GetOptionValue<List<DbFile>>("DbFiles").Count == 0));
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles));
            Assert.AreEqual(false, planDetailInfo.DefaultValue);
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.ReplaceDatabase];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.ReplaceDatabase);
            Assert.AreEqual(false, planDetailInfo.IsReadOnly);
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.ReplaceDatabase));
            Assert.AreEqual(false, planDetailInfo.DefaultValue);
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.KeepReplication];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.KeepReplication);
            Assert.AreEqual(planDetailInfo.IsReadOnly,  optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState) == DatabaseRecoveryState.WithNoRecovery);
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication));
            Assert.AreEqual(false, planDetailInfo.DefaultValue);
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.SetRestrictedUser];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.SetRestrictedUser);
            Assert.AreEqual(false, planDetailInfo.IsReadOnly);
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.SetRestrictedUser));
            Assert.AreEqual(false, planDetailInfo.DefaultValue);
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.RecoveryState];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.RecoveryState);
            Assert.AreEqual(false, planDetailInfo.IsReadOnly);
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState).ToString());
            Assert.AreEqual(planDetailInfo.DefaultValue, DatabaseRecoveryState.WithRecovery.ToString());
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.StandbyFile];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.StandbyFile);
            Assert.AreEqual(planDetailInfo.IsReadOnly, optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState) != DatabaseRecoveryState.WithStandBy);
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.StandbyFile));
            Assert.AreEqual(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("GetDefaultStandbyFile"));
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.BackupTailLog];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.BackupTailLog);
            Assert.AreEqual(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog));
            Assert.AreEqual(planDetailInfo.DefaultValue, optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.TailLogBackupFile];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.TailLogBackupFile);
            Assert.AreEqual(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupPossible")
                | !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog));
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>("TailLogBackupFile"));
            Assert.AreEqual(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("GetDefaultTailLogbackupFile"));
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.TailLogWithNoRecovery];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.TailLogWithNoRecovery);
            Assert.AreEqual(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible") 
                | !optionValues.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog));
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("TailLogWithNoRecovery"));
            Assert.AreEqual(planDetailInfo.DefaultValue, optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible"));
            Assert.AreEqual(true, planDetailInfo.IsVisiable);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.CloseExistingConnections];
            Assert.AreEqual(planDetailInfo.Name, RestoreOptionsHelper.CloseExistingConnections);
            Assert.AreEqual(false, planDetailInfo.IsReadOnly);
            Assert.AreEqual(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.CloseExistingConnections));
            Assert.AreEqual(false, planDetailInfo.DefaultValue);
            Assert.AreEqual(true, planDetailInfo.IsVisiable);
        }
        
    }
}
