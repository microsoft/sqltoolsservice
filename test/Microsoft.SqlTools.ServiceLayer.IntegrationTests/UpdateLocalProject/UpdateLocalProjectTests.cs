//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using NUnit.Framework;

using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;

using Microsoft.SqlTools.ServiceLayer.UpdateLocalProject;
using Microsoft.SqlTools.ServiceLayer.UpdateLocalProject.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.UpdateLocalProject
{
    class UpdateLocalProjectTests
    {
        /// <summary>
        /// Verifies that applying changes works when there are no changes
        /// </summary>
        [Test]
        public async Task NoChangesTest()
        {
            // set up test
            SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "UpdateLocalProjectTest");
            string[] startQueries = new[]
            {
                "create table TestTable (testStr varchar, testInt tinyint);"
            };
            string[] endQueries = Array.Empty<string>();
            string folderStructure = "file";
            
            SetUpTest(startQueries, endQueries, testDb, folderStructure, out UpdateLocalProjectParams parameters);

            // run test
            UpdateLocalProjectOperation operation = new(parameters, null, testDb.ConnectionString);
            UpdateLocalProjectResult result = operation.UpdateLocalProject();

            // verify results
            VerifyResult(result, GenerateEmptyResult());

            // clean up
            Cleanup(testDb, parameters.ProjectPath);
        }

        /// <summary>
        /// Verifies that applying changes works when one elements is changed
        /// </summary>
        [Test]
        public async Task ChangeTest()
        {
            // set up test
            SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "UpdateLocalProjectTest");
            string[] startQueries = new[]
            {
                "create table ChangedTable (testStr varchar, testInt tinyint);"
            };
            string[] endQueries = new[]
            {
                "exec sp_rename 'dbo.ChangedTable.testStr', 'chTestStr', 'COLUMN';"
            };
            string folderStructure = "flat";

            SetUpTest(startQueries, endQueries, testDb, folderStructure, out UpdateLocalProjectParams parameters);

            // run test
            UpdateLocalProjectOperation operation = new(parameters, null, testDb.ConnectionString);
            UpdateLocalProjectResult result = operation.UpdateLocalProject();

            // verify results
            VerifyResult(result, GenerateResult(Array.Empty<string>(), Array.Empty<string>(), new string[] { "" }));

            result = operation.UpdateLocalProject();
            VerifyResult(result, GenerateEmptyResult());

            // clean up
            Cleanup(testDb, parameters.ProjectPath);
        }

        /// <summary>
        /// Verifies that applying changes works when elements are added, dropped, or changed
        /// </summary>
        [Test]
        public async Task ChangeAddDropTest()
        {
            // set up test
            SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "UpdateLocalProjectTest");
            string[] startQueries = new[]
            {
                "create table ChangedTable (testStr varchar, testInt tinyint);",
                "create table DroppedTable (testStr varchar, testInt tinyint);"
            };
            string[] endQueries = new[]
            {
                "drop table dbo.DroppedTable;",
                "create table AddedTable (testStr varchar, testInt tinyint);",
                "exec sp_rename 'dbo.ChangedTable.testStr', 'chTestStr', 'COLUMN';",
                "alter table dbo.ChangedTable drop column testInt;",
                "alter table dbo.ChangedTable add testDate DATE NULL;"
            };
            string folderStructure = "flat";

            SetUpTest(startQueries, endQueries, testDb, folderStructure, out UpdateLocalProjectParams parameters);

            // run test
            UpdateLocalProjectOperation operation = new(parameters, null, testDb.ConnectionString);
            UpdateLocalProjectResult result = operation.UpdateLocalProject();

            // verify results
            VerifyResult(result, GenerateResult(new string[] { "" }, new string[] { "" }, new string[] { "" }));

            operation.UpdateTargetScripts(GetTargetScripts(parameters.ProjectPath));
            result = operation.UpdateLocalProject();
            VerifyResult(result, GenerateEmptyResult());

            // clean up
            Cleanup(testDb, parameters.ProjectPath);
        }

        /// <summary>
        /// Sets up the environment needed for all tests
        /// </summary>
        private static void SetUpTest(string[] startQueries, string[] endQueries, SqlTestDb testDb, string folderStructure,
                                      out UpdateLocalProjectParams parameters)
        {
            // run start queries in test database
            RunQueries(testDb, startQueries);

            // create path for test project
            string testProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UpdateLocalProjectTest");
            if (Directory.Exists(testProjectPath))
            {
                ProjectCleanup(testProjectPath);
            }
            Directory.CreateDirectory(testProjectPath);

            // convert folder structure to extract target
            DacExtractTarget extractTarget = DacExtractTarget.Flat;
            string packageFilePath = testProjectPath;

            switch (folderStructure)
            {
                case "file":
                    extractTarget = DacExtractTarget.File;
                    packageFilePath = Path.Combine(testProjectPath, string.Format("{0}.sql", testDb.DatabaseName));
                    break;
                case "objectType":
                    extractTarget = DacExtractTarget.ObjectType;
                    break;
                case "schema":
                    extractTarget = DacExtractTarget.Schema;
                    break;
                case "schema/objectType":
                    extractTarget = DacExtractTarget.SchemaObjectType;
                    break;
            }

            // extract test database content to test project
            ExtractParams eParams = new ExtractParams
            {
                DatabaseName = testDb.DatabaseName,
                PackageFilePath = packageFilePath,
                ApplicationName = "test",
                ApplicationVersion = "1.0.0.0",
                ExtractTarget = extractTarget
            };

            DacFxService dacfxService = new();
            ExtractOperation operation = new(eParams, LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo);
            dacfxService.PerformOperation(operation, TaskExecutionMode.Execute);

            // get .sql files in test project
            string[] targetScripts = GetTargetScripts(testProjectPath);

            // run end queries in test database
            RunQueries(testDb, endQueries);

            // get parameters to output
            parameters = new UpdateLocalProjectParams
            {
                TargetScripts = targetScripts,
                FolderStructure = folderStructure,
                ProjectPath = testProjectPath,
                OwnerUri = null,
                Version = "SqlServer2016"
            };
        }

        /// <summary>
        /// Gets a list of all the .sql scripts in a local project
        /// </summary>
        private static string[] GetTargetScripts(string testProjectPath)
        {
            List<string> targetScriptsList = new();
            List<string> directories = new() { testProjectPath };

            // iterate through subdirectories
            while (directories.Count != 0)
            {
                // only care about .sql files
                foreach (string f in Directory.GetFiles(directories[0]))
                {
                    if (f.EndsWith(".sql"))
                    {
                        targetScriptsList.Add(f);
                    }
                }

                // queue up subdirectories
                foreach (string d in Directory.GetDirectories(directories[0]))
                {
                    directories.Add(d);
                }

                // pop current directory
                directories.RemoveAt(0);
            }

            return targetScriptsList.ToArray();
        }

        /// <summary>
        /// Executes the passed in SQL queries in the passed in database
        /// </summary>
        private static void RunQueries(SqlTestDb testDb, string[] queries)
        {
            foreach (string q in queries)
            {
                testDb.RunQuery(q);
            }
        }

        /// <summary>
        /// Cleans up the test database and the test project
        /// </summary>
        private static void Cleanup(SqlTestDb testDb, string projectPath)
        {
            testDb.Cleanup();
            ProjectCleanup(projectPath);
        }

        /// <summary>
        /// Cleans up the test project
        /// </summary>
        private static void ProjectCleanup(string directoryPath)
        {
            string[] files = Directory.GetFiles(directoryPath);
            string[] directories = Directory.GetDirectories(directoryPath);

            // delete all the files
            foreach (string f in files)
            {
                File.Delete(f);
            }

            // recurse on subdirectories
            foreach (string d in directories)
            {
                ProjectCleanup(d);
            }

            // delete directory
            Directory.Delete(directoryPath);
        }

        /// <summary>
        /// Creates an UpdateLocalProjectResult object used to verify the result
        /// </summary>
        private static UpdateLocalProjectResult GenerateResult(string[] addedFiles, string[] deletedFiles, string[] changedFiles)
        {
            return new UpdateLocalProjectResult()
            {
                AddedFiles = addedFiles,
                DeletedFiles = deletedFiles,
                ChangedFiles = changedFiles
            };
        }

        /// <summary>
        /// Creates an empty UpdateLocalProjectResult object
        /// </summary>
        private static UpdateLocalProjectResult GenerateEmptyResult()
        {
            return GenerateResult(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        }

        /// <summary>
        /// Asserts that result matches expected result
        /// </summary>
        private static void VerifyResult(UpdateLocalProjectResult result, UpdateLocalProjectResult expectedResult)
        {
            Assert.AreEqual(result.AddedFiles.Length, expectedResult.AddedFiles.Length);
            Assert.AreEqual(result.DeletedFiles.Length, expectedResult.DeletedFiles.Length);
            Assert.AreEqual(result.ChangedFiles.Length, expectedResult.ChangedFiles.Length);
        }
    }
}
