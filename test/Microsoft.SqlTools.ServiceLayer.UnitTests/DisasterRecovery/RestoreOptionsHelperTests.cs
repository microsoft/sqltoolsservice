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
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class RestoreOptionsHelperTests
    {
        [Fact]
        public void VerifyOptionsCreatedSuccessfullyIsResponse()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
        }

        [Fact]
        public void RelocateAllFilesShouldBeReadOnlyGivenNoDbFiles()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["DbFiles"] = new List<DbFile>();
            Mock <IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
            Assert.True(result[RestoreOptionsHelper.RelocateDbFiles].IsReadOnly);
        }

        [Fact]
        public void BackupTailLogShouldBeReadOnlyTailLogBackupNotPossible()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["IsTailLogBackupPossible"] = false;
            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
            Assert.True(result[RestoreOptionsHelper.BackupTailLog].IsReadOnly);
            Assert.True(result[RestoreOptionsHelper.TailLogBackupFile].IsReadOnly);
        }

        [Fact]
        public void TailLogWithNoRecoveryShouldBeReadOnlyTailLogBackupWithNoRecoveryNotPossible()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options["IsTailLogBackupWithNoRecoveryPossible"] = false;
            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
            Assert.True(result[RestoreOptionsHelper.TailLogWithNoRecovery].IsReadOnly);
        }

        [Fact]
        public void StandbyFileShouldNotBeReadOnlyGivenRecoveryStateWithStandBy()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithStandBy;
            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
            Assert.False(result[RestoreOptionsHelper.StandbyFile].IsReadOnly);
        }

        [Fact]
        public void KeppReplicationShouldNotBeReadOnlyGivenRecoveryStateWithNoRecovery()
        {
            GeneralRequestDetails optionValues = CreateOptionsTestData();
            optionValues.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;
            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(optionValues);

            Dictionary<string, RestorePlanDetailInfo> result = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);
            Assert.NotNull(result);
            VerifyOptions(result, optionValues);
            Assert.True(result[RestoreOptionsHelper.KeepReplication].IsReadOnly);
        }

        [Fact]
        public void KeppReplicationShouldSetToDefaultValueGivenRecoveryStateWithNoRecovery()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
            restoreParams.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithNoRecovery;

            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);
            Dictionary<string, RestorePlanDetailInfo> options = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);

            restoreParams.Options[RestoreOptionsHelper.KeepReplication] = true;

            bool actual = RestoreOptionsHelper.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication, options, restoreDatabaseTaskDataObject.Object);
            bool expected = (bool)options[RestoreOptionsHelper.KeepReplication].DefaultValue;

            Assert.Equal(actual, expected);
        }

        [Fact]
        public void KeppReplicationShouldSetToValueInRequestGivenRecoveryStateWithRecovery()
        {
            RestoreParams restoreParams = CreateOptionsTestData();
           
            restoreParams.Options[RestoreOptionsHelper.RecoveryState] = DatabaseRecoveryState.WithRecovery;
            Mock<IRestoreDatabaseTaskDataObject> restoreDatabaseTaskDataObject = CreateRestoreDatabaseTaskDataObject(restoreParams);
            Dictionary<string, RestorePlanDetailInfo> options = RestoreOptionsHelper.CreateRestorePlanOptions(restoreDatabaseTaskDataObject.Object);

            restoreParams.Options[RestoreOptionsHelper.KeepReplication] = true;

            bool actual = RestoreOptionsHelper.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication, options, restoreDatabaseTaskDataObject.Object);
            bool expected = true;
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
            optionValues.Options.Add("LogFilesFolder", "Log file folder");
            optionValues.Options.Add("RelocateAllFiles", false);
            optionValues.Options.Add("TailLogBackupFile", "tail log backup file");
            optionValues.Options.Add("TailLogWithNoRecovery", false);
            optionValues.Options.Add("BackupTailLog", false);
            optionValues.Options.Add(RestoreOptionsHelper.KeepReplication, false);
            optionValues.Options.Add("ReplaceDatabase", false);
            optionValues.Options.Add("SetRestrictedUser", false);
            optionValues.Options.Add("StandbyFile", "Stand by file");
            optionValues.Options.Add(RestoreOptionsHelper.RecoveryState, DatabaseRecoveryState.WithNoRecovery.ToString());
            return optionValues;
        }

        private Mock<IRestoreDatabaseTaskDataObject> CreateRestoreDatabaseTaskDataObject(GeneralRequestDetails optionValues)
        {
            var restoreDataObject = new Mock<IRestoreDatabaseTaskDataObject>();
            restoreDataObject.Setup(x => x.CloseExistingConnections).Returns(optionValues.GetOptionValue<bool>(RestoreOptionsHelper.CloseExistingConnections));
            restoreDataObject.Setup(x => x.DataFilesFolder).Returns(optionValues.GetOptionValue<string>(RestoreOptionsHelper.DataFileFolder));
            restoreDataObject.Setup(x => x.DbFiles).Returns(optionValues.GetOptionValue<List<DbFile>>("DbFiles"));
            restoreDataObject.Setup(x => x.DefaultDataFileFolder).Returns(optionValues.GetOptionValue<string>("DefaultDataFileFolder"));
            restoreDataObject.Setup(x => x.DefaultLogFileFolder).Returns(optionValues.GetOptionValue<string>("DefaultLogFileFolder"));
            restoreDataObject.Setup(x => x.IsTailLogBackupPossible(It.IsAny<string>())).Returns(optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            restoreDataObject.Setup(x => x.IsTailLogBackupWithNoRecoveryPossible(It.IsAny<string>())).Returns(optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible"));
            restoreDataObject.Setup(x => x.GetDefaultStandbyFile(It.IsAny<string>())).Returns(optionValues.GetOptionValue<string>("GetDefaultStandbyFile"));
            restoreDataObject.Setup(x => x.GetDefaultTailLogbackupFile(It.IsAny<string>())).Returns(optionValues.GetOptionValue<string>("GetDefaultTailLogbackupFile"));
            restoreDataObject.Setup(x => x.LogFilesFolder).Returns(optionValues.GetOptionValue<string>("LogFilesFolder"));
            restoreDataObject.Setup(x => x.RelocateAllFiles).Returns(optionValues.GetOptionValue<bool>("RelocateAllFiles"));
            restoreDataObject.Setup(x => x.TailLogBackupFile).Returns(optionValues.GetOptionValue<string>("TailLogBackupFile"));
            restoreDataObject.Setup(x => x.TailLogWithNoRecovery).Returns(optionValues.GetOptionValue<bool>("TailLogWithNoRecovery"));
            restoreDataObject.Setup(x => x.BackupTailLog).Returns(optionValues.GetOptionValue<bool>("BackupTailLog"));
            restoreDataObject.Setup(x => x.RestoreParams).Returns(optionValues as RestoreParams);
            restoreDataObject.Setup(x => x.RestorePlan).Returns(() => null);
            RestoreOptions restoreOptions = new RestoreOptions();
            restoreOptions.KeepReplication = optionValues.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication);
            restoreOptions.ReplaceDatabase = optionValues.GetOptionValue<bool>("ReplaceDatabase");
            restoreOptions.SetRestrictedUser = optionValues.GetOptionValue<bool>("SetRestrictedUser");
            restoreOptions.StandByFile = optionValues.GetOptionValue<string>("StandbyFile");
            restoreOptions.RecoveryState = optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState);
            restoreDataObject.Setup(x => x.RestoreOptions).Returns(restoreOptions);


            return restoreDataObject;
        }

        private void VerifyOptions(Dictionary<string, RestorePlanDetailInfo> optionInResponse, GeneralRequestDetails optionValues)
        {
            RestorePlanDetailInfo planDetailInfo = optionInResponse[RestoreOptionsHelper.DataFileFolder];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.DataFileFolder);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("RelocateAllFiles"));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>(RestoreOptionsHelper.DataFileFolder));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("DefaultDataFileFolder"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.LogFileFolder];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.LogFileFolder);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("RelocateAllFiles"));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>("LogFilesFolder"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("DefaultLogFileFolder"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.RelocateDbFiles];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.RelocateDbFiles);
            Assert.Equal(planDetailInfo.IsReadOnly, (optionValues.GetOptionValue<List<DbFile>>("DbFiles").Count == 0));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("LogFilesFolder"));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.ReplaceDatabase];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.ReplaceDatabase);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("ReplaceDatabase"));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.KeepReplication];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.KeepReplication);
            Assert.Equal(planDetailInfo.IsReadOnly, optionValues.GetOptionValue<DatabaseRecoveryState>(RestoreOptionsHelper.RecoveryState) == DatabaseRecoveryState.WithNoRecovery);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.SetRestrictedUser];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.SetRestrictedUser);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("SetRestrictedUser"));
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
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>("StandbyFile"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("GetDefaultStandbyFile"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.BackupTailLog];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.BackupTailLog);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("BackupTailLog"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.TailLogBackupFile];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.TailLogBackupFile);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupPossible"));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<string>("TailLogBackupFile"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<string>("GetDefaultTailLogbackupFile"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.TailLogWithNoRecovery];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.TailLogWithNoRecovery);
            Assert.Equal(planDetailInfo.IsReadOnly, !optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible"));
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("TailLogWithNoRecovery"));
            Assert.Equal(planDetailInfo.DefaultValue, optionValues.GetOptionValue<bool>("IsTailLogBackupWithNoRecoveryPossible"));
            Assert.Equal(planDetailInfo.IsVisiable, true);

            planDetailInfo = optionInResponse[RestoreOptionsHelper.CloseExistingConnections];
            Assert.Equal(planDetailInfo.Name, RestoreOptionsHelper.CloseExistingConnections);
            Assert.Equal(planDetailInfo.IsReadOnly, false);
            Assert.Equal(planDetailInfo.CurrentValue, optionValues.GetOptionValue<bool>("CloseExistingConnections"));
            Assert.Equal(planDetailInfo.DefaultValue, false);
            Assert.Equal(planDetailInfo.IsVisiable, true);
        }
        
    }
}
