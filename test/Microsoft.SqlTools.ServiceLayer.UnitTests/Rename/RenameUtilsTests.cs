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
        public void TestValidateInvalidNewName()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "test}Table", ChangeType.TABLE, "test", "dbo", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestValidateInvalidSchema()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "testTable", ChangeType.TABLE, "test", "db]o", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestValidateInvalidNewNameTolong()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "Lorem_ipsumdolorsitametconsetetursadipscingelitrseddiamnonumyeirmodtemporinviduntutlaboreetdoloremagnaaatestToLongForTableNameIt_", ChangeType.TABLE, "test", "dbo", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestValidateInvalidOldNameTolong()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "testTable", ChangeType.TABLE, "Lorem_ipsumdolorsitametconsetetursadipscingelitrseddiamnonumyeirmodtemporinviduntutlaboreetdoloremagnaaatestToLongForTableNameIt_", "dbo", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestValidateInvalidSchemaTolong()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "testTable", ChangeType.TABLE, "test", "Lorem_ipsumdolorsitametconsetetursadipscingelitrseddiamnonumyeirmodtemporinviduntutlaboreetdoloremagnaaatestToLongForTableNameIt_", "testTable", "TestDB"
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameUtils.Validate(requestParams));
        }
        [Test]
        public void TestValidateInvalidDatabaseTolong()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "testTable", ChangeType.TABLE, "test", "dbo", "testTable", "Lorem_ipsumdolorsitametconsetetursadipscingelitrseddiamnonumyeirmodtemporinviduntutlaboreetdoloremagnaaatestToLongForTableNameIt_"
            );
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameUtils.Validate(requestParams));
        }

        [Test]
        public void TestGetRenameSQLCommandTable()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "testTable", ChangeType.TABLE, "test", "dbo", "testTable", "TestDB"
            );
            string expected = @"
                USE [TestDB];
                    EXEC sp_rename @objname = 'dbo.test', @newname = 'testTable';
            ";
            string res = RenameUtils.GetRenameSQLCommand(requestParams);
            Assert.AreEqual(expected.Replace("\n", "").Replace("\r", ""), res.Replace("\n", "").Replace("\r", ""));
        }
        [Test]
        public void TestGetRenameSQLCommandColumn()
        {
            ProcessRenameEditRequestParams requestParams = this.InitProcessRenameEditRequestParams(
                "C1", ChangeType.COLUMN, "test", "dbo", "testTable", "TestDB"
            );
            string expected = @"
                USE [TestDB];
                    EXEC sp_rename @objname = 'dbo.testTable.test', @newname = 'C1', @objtype ='COLUMN';
            ";
            string res = RenameUtils.GetRenameSQLCommand(requestParams);
            Assert.AreEqual(expected.Replace("\n", "").Replace("\r", ""), res.Replace("\n", "").Replace("\r", ""));
        }
        [Test]
        public void TestCombineTableNameWithSchemaWithOldNames()
        {
            Assert.AreEqual("dbo.test.c1", RenameUtils.CombineTableNameWithSchema("dbo", "test", "c1"));
        }
        [Test]
        public void TestCombineTableNameWithSchemaWithOldNamesBrackets()
        {
            Assert.AreEqual("dbo.test.c1", RenameUtils.CombineTableNameWithSchema("[dbo]", "[test]", "[c1]"));
        }
        [Test]
        public void TestCombineTableNameWithSchemaWithOldNamesBracketsSpace()
        {
            Assert.AreEqual("dbo.test.c1", RenameUtils.CombineTableNameWithSchema(" [dbo] ", " [test] ", " [c1] "));
        }
        public void TestCombineTableNameWithSchema()
        {
            Assert.AreEqual("dbo.test", RenameUtils.CombineTableNameWithSchema("dbo", "test"));
        }
        [Test]
        public void TestCombineTableNameWithSchemaWithBrackets()
        {
            Assert.AreEqual("dbo.test", RenameUtils.CombineTableNameWithSchema("[dbo]", "[test]"));
        }
        [Test]
        public void TestCombineTableNameWithSchemaWithBracketsSpace()
        {
            Assert.AreEqual("dbo.test", RenameUtils.CombineTableNameWithSchema(" [dbo] ", " [test] "));
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
                Id = "1",
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