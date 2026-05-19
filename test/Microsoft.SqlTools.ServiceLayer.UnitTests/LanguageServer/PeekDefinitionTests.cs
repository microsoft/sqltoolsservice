//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using NUnit.Framework;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the language service peek definition/ go to definition feature
    /// </summary>
    public class PeekDefinitionTests : LanguageServiceTestBase<Location>
    {
        /// <summary>
        /// Tests the definition event handler. When called with no active connection, an error is sent
        /// </summary>
        [Test]
        public async Task DefinitionsHandlerWithNoConnectionTest()
        {
            InitializeTestObjects();
            scriptParseInfo.BindingContextKind = BindingContextKindEnum.None;
            // request definition
            var definitionTask = await Task.WhenAny(langService.HandleDefinitionRequest(textDocument, requestContext.Object), Task.Delay(TaskTimeout));
            await definitionTask;
            // verify that send result was called once and send error was not called
            requestContext.Verify(m => m.SendResult(It.IsAny<Location[]>()), Times.Once());
            requestContext.Verify(m => m.SendError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }

        /// <summary>
        /// Tests creating location objects on windows and non-windows systems
        /// </summary>
        [Test]
        public void GetLocationFromFileForValidFilePathTest()
        {
            string filePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\test\\script.sql" : "/test/script.sql";
            Scripter peekDefinition = new Scripter(null, null);
            Location[] locations = peekDefinition.GetLocationFromFile(filePath, 0);

            string expectedFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "file:///C:/test/script.sql" : "file:/test/script.sql";
            Assert.AreEqual(locations[0].Uri, expectedFilePath);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a valid database name
        /// </summary>
        [Test]
        public void GetSchemaFromDatabaseQualifiedNameWithValidNameTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string validDatabaseQualifiedName = "master.test.test_table";
            string objectName = "test_table";
            string expectedSchemaName = "test";

            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.AreEqual(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a valid object name and no schema
        /// </summary>
        [Test]
        public void GetSchemaFromDatabaseQualifiedNameWithNoSchemaTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string validDatabaseQualifiedName = "test_table";
            string objectName = "test_table";
            string expectedSchemaName = "dbo";

            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.AreEqual(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a invalid database name
        /// </summary>
        [Test]
        public void GetSchemaFromDatabaseQualifiedNameWithInvalidNameTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string validDatabaseQualifiedName = "x.y.z";
            string objectName = "test_table";
            string expectedSchemaName = "dbo";

            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.AreEqual(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test deletion of peek definition scripts for a valid temp folder that exists
        /// </summary>
        [Test]
        public void DeletePeekDefinitionScriptsTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            Assert.True(Directory.Exists(FileUtilities.PeekDefinitionTempFolder));
            LanguageService.Instance.DeletePeekDefinitionScripts();
            Assert.False(Directory.Exists(FileUtilities.PeekDefinitionTempFolder));
        }

        /// <summary>
        /// Test deletion of peek definition scripts for a temp folder that does not exist
        /// </summary>
        [Test]
        public void DeletePeekDefinitionScriptsWhenFolderDoesNotExistTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            FileUtilities.SafeDirectoryDelete(FileUtilities.PeekDefinitionTempFolder, true);
            Assert.False(Directory.Exists(FileUtilities.PeekDefinitionTempFolder));
            // Expected not to throw any exception
            LanguageService.Instance.DeletePeekDefinitionScripts();
        }

        [Test]
        public async Task GetPeekDefinitionTempFolder_IsThreadSafe()
        {
            string originalPeekDefinitionTempFolder = FileUtilities.PeekDefinitionTempFolder;
            bool originalPeekDefinitionTempFolderCreated = FileUtilities.PeekDefinitionTempFolderCreated;
            string testPeekDefinitionTempFolder = Path.Combine(Path.GetTempPath(), $"mssql_definition_test_{Guid.NewGuid():N}");

            FileUtilities.PeekDefinitionTempFolder = testPeekDefinitionTempFolder;
            FileUtilities.PeekDefinitionTempFolderCreated = false;

            try
            {
                Task<string>[] tempFolderTasks = new Task<string>[8];
                for (int i = 0; i < tempFolderTasks.Length; i++)
                {
                    tempFolderTasks[i] = Task.Run(FileUtilities.GetPeekDefinitionTempFolder);
                }

                string[] tempFolders = await Task.WhenAll(tempFolderTasks);

                Assert.True(FileUtilities.PeekDefinitionTempFolderCreated);
                Assert.True(Directory.Exists(FileUtilities.PeekDefinitionTempFolder));
                Assert.AreEqual(tempFolders[0], FileUtilities.PeekDefinitionTempFolder);
                StringAssert.StartsWith(testPeekDefinitionTempFolder + "_", tempFolders[0]);

                for (int i = 1; i < tempFolders.Length; i++)
                {
                    Assert.AreEqual(tempFolders[0], tempFolders[i]);
                }
            }
            finally
            {
                if (Directory.Exists(FileUtilities.PeekDefinitionTempFolder))
                {
                    FileUtilities.SafeDirectoryDelete(FileUtilities.PeekDefinitionTempFolder, true);
                }

                FileUtilities.PeekDefinitionTempFolder = originalPeekDefinitionTempFolder;
                FileUtilities.PeekDefinitionTempFolderCreated = originalPeekDefinitionTempFolderCreated;
            }
        }

        [Test]
        public void CreateFileName_ReturnsUniqueNamePerRequest()
        {
            MethodInfo createFileNameMethod = typeof(Scripter).GetMethod("CreateFileName", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(createFileNameMethod);

            var identifier = new Sql3PartIdentifier
            {
                DatabaseName = "master",
                SchemaName = "dbo",
                ObjectName = "testTable"
            };

            string firstFileName = (string)createFileNameMethod.Invoke(null, new object[] { identifier });
            string secondFileName = (string)createFileNameMethod.Invoke(null, new object[] { identifier });

            Assert.AreNotEqual(firstFileName, secondFileName);
            StringAssert.StartsWith("master.dbo.testTable_", firstFileName);
            StringAssert.StartsWith("master.dbo.testTable_", secondFileName);
            StringAssert.EndsWith(".sql", firstFileName);
            StringAssert.EndsWith(".sql", secondFileName);
        }

        /// <summary>
        /// Test extracting the full object name from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string containing the object name
        /// Expect the full object name (database.schema.objectName)
        /// </summary>
        [Test]
        public void GetFullObjectNameFromQuickInfoWithValidStringsTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "testTable";
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            string expected = "master.dbo.testTable";
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test extracting the full object name from quickInfoText with case insensitive comparison.
        /// Given a valid object name string and a vaild quickInfo string containing the object name
        /// Expect the full object name (database.schema.objectName)
        /// </summary>
        [Test]
        public void GetFullObjectNameFromQuickInfoWithValidStringsandIgnoreCaseTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "testtable";
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.OrdinalIgnoreCase);
            string expected = "master.dbo.testTable";
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test extracting the full object name from quickInfoText.
        /// Given a null object name string and a vaild quickInfo string containing the object name( and vice versa)
        /// Expect null
        /// </summary>
        [Test]
        public void GetFullObjectNameFromQuickInfoWithNullStringsTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string expected = null;

            string objectName = null;
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual(expected, result);

            quickInfoText = null;
            objectName = "tableName";
            result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual(expected, result);

            quickInfoText = null;
            objectName = null;
            result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test extracting the full object name from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string that does not contain the object name
        /// Expect null
        /// </summary>
        [Test]
        public void GetFullObjectNameFromQuickInfoWithIncorrectObjectNameTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "test";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            string expected = null;
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test extracting the object type from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string containing the object name
        /// Expect correct object type
        /// </summary>
        [Test]
        public void GetTokenTypeFromQuickInfoWithValidStringsTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "tableName";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            string expected = "table";
            Assert.AreEqual(expected, result);
        }


        /// <summary>
        /// Test extracting the object type from quickInfoText with case insensitive comparison.
        /// Given a valid object name string and a vaild quickInfo string containing the object name
        /// Expect correct object type
        /// </summary>
        [Test]
        public void GetTokenTypeFromQuickInfoWithValidStringsandIgnoreCaseTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "tablename";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.OrdinalIgnoreCase);
            string expected = "table";
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test extracting theobject type from quickInfoText.
        /// Given a null object name string and a vaild quickInfo string containing the object name( and vice versa)
        /// Expect null
        /// </summary>
        [Test]
        public void GetTokenTypeFromQuickInfoWithNullStringsTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string expected = null;

            string objectName = null;
            string quickInfoText = "table master.dbo.testTable";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual(expected, result);

            quickInfoText = null;
            objectName = "tableName";
            result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual(expected, result);

            quickInfoText = null;
            objectName = null;
            result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test extracting the object type from quickInfoText.
        /// Given a valid object name string and a vaild quickInfo string that does not containthe object name
        /// Expect null
        /// </summary>
        [Test]
        public void GetTokenTypeFromQuickInfoWithIncorrectObjectNameTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "test";
            string quickInfoText = "table master.dbo.tableName";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            string expected = null;
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test getting definition using quickInfo text without a live connection
        /// Expect an error result (because you cannot script without a live connection)
        /// </summary>
        [Test]
        public void GetDefinitionUsingQuickInfoWithoutConnectionTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "tableName";
            string quickInfoText = "table master.dbo.tableName";
            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, new Sql3PartIdentifier { ObjectName = objectName });
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Test getting definition using declaration Type without a live connection
        /// Expect an error result (because you cannot script without a live connection)
        /// </summary>
        [Test]
        public void GetDefinitionUsingDeclarationItemWithoutConnectionTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "tableName";
            string fullObjectName = "master.dbo.tableName";
            Assert.Throws<NullReferenceException>(() => peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.Table, fullObjectName, new Sql3PartIdentifier { ObjectName = objectName }));
        }

        /// <summary>
        /// Scalar-valued functions must be a supported declaration type so that Go to Definition
        /// proceeds to the SMO scripting path rather than returning "type not supported".
        /// The NullReferenceException is expected here because there is no live connection
        /// (same behaviour as the Table test above).
        /// </summary>
        [Test]
        public void GetDefinitionUsingDeclarationTypeScalarFunctionIsSupportedTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "myScalarFn";
            string fullObjectName = "master.dbo.myScalarFn";
            // Should throw NullReferenceException (scripting path entered), NOT return a
            // "type not supported" DefinitionResult, confirming ScalarValuedFunction is mapped.
            Assert.Throws<NullReferenceException>(() =>
                peekDefinition.GetDefinitionUsingDeclarationType(
                    DeclarationType.ScalarValuedFunction,
                    fullObjectName,
                    new Sql3PartIdentifier { ObjectName = objectName, SchemaName = "dbo" }));
        }

        /// <summary>
        /// Table-valued functions must be a supported declaration type so that Go to Definition
        /// proceeds to the SMO scripting path rather than returning "type not supported".
        /// </summary>
        [Test]
        public void GetDefinitionUsingDeclarationTypeTableValuedFunctionIsSupportedTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "myTVF";
            string fullObjectName = "master.dbo.myTVF";
            Assert.Throws<NullReferenceException>(() =>
                peekDefinition.GetDefinitionUsingDeclarationType(
                    DeclarationType.TableValuedFunction,
                    fullObjectName,
                    new Sql3PartIdentifier { ObjectName = objectName, SchemaName = "dbo" }));
        }

        /// <summary>
        /// GetTokenTypeFromQuickInfo should extract "scalar-valued function" from a function quickInfo string.
        /// </summary>
        [Test]
        public void GetTokenTypeFromQuickInfoScalarFunctionTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "pd_addTwo";
            string quickInfoText = "scalar-valued function master.dbo.pd_addTwo";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual("scalar-valued function", result);
        }

        /// <summary>
        /// GetTokenTypeFromQuickInfo should extract "table-valued function" from a TVF quickInfo string.
        /// </summary>
        [Test]
        public void GetTokenTypeFromQuickInfoTableValuedFunctionTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "pd_returnTable";
            string quickInfoText = "table-valued function master.dbo.pd_returnTable";
            string result = peekDefinition.GetTokenTypeFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual("table-valued function", result);
        }

        /// <summary>
        /// GetFullObjectNameFromQuickInfo should return the fully-qualified name from a scalar function quickInfo string.
        /// </summary>
        [Test]
        public void GetFullObjectNameFromQuickInfoScalarFunctionTest()
        {
            Scripter peekDefinition = new Scripter(null, null);
            string objectName = "pd_addTwo";
            string quickInfoText = "scalar-valued function master.dbo.pd_addTwo";
            string result = peekDefinition.GetFullObjectNameFromQuickInfo(quickInfoText, objectName, StringComparison.Ordinal);
            Assert.AreEqual("master.dbo.pd_addTwo", result);
        }
    }
}
