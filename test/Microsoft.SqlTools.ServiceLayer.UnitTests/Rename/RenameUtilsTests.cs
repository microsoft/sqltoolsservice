//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using Microsoft.SqlTools.ServiceLayer.Rename;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Rename
{
    public class RenameUtilsTests
    {
        [Test]
        public void TestValidateNullParameters()
        {
            ProcessRenameEditRequestParams requestParams = null;
            Assert.Throws<ArgumentNullException>(() => RenameUtils.Validate(requestParams));
        }

        [Test]
        public void TestValidateEmptyNewName()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "", ChangeType.TABLE, "testTable", "dbo", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestValidateEmptyOldName()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "testTable", ChangeType.TABLE, "", "dbo", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestGetUrnTableString()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "C1", ChangeType.TABLE, "test", "dbo", "testTable", "TestDB"
            );
            Assert.AreEqual("Server[@Name='TestServer']/Database[@Name='TestDB']/Table[@Name='testTable' and @Schema='dbo']", RenameUtils.GetURNFromDatabaseSqlObjects(requestParams, "TestServer"));
        }
        [Test]
        public void TestGetUrnColumnString()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "C1", ChangeType.COLUMN, "testColumn", "dbo", "testTable", "TestDB"
            );
            Assert.AreEqual("Server[@Name='TestServer']/Database[@Name='TestDB']/Table[@Name='testTable' and @Schema='dbo']/Column[@Name='testColumn']", RenameUtils.GetURNFromDatabaseSqlObjects(requestParams, "TestServer"));
        }
        private ProcessRenameEditRequestParams InitProcessRenameEditRequestParams(string newName, ChangeType type, string oldName, string schema, string tableName, string database)
        {
            RenameTableChangeInfo changeInfo = new RenameTableChangeInfo
            {
                NewName = newName,
                Type = type
            };
            RenameTableInfo tableInfo = new RenameTableInfo
            {
                Database = database,
                OldName = oldName,
                OwnerUri = "TestDB",
                Schema = schema,
                TableName = tableName
            };
            return new ProcessRenameEditRequestParams
            {
                ChangeInfo = changeInfo,
                TableInfo = tableInfo
            };
        }
    }
}