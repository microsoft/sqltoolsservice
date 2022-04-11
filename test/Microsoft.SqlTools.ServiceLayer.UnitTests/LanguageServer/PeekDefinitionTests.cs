//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
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
            scriptParseInfo.IsConnected = false;
            // request definition
            var definitionTask = await Task.WhenAny(langService.HandleDefinitionRequest(textDocument, requestContext.Object), Task.Delay(TaskTimeout));
            await definitionTask;
            // verify that send result was called once and send error was not called
            requestContext.Verify(m => m.SendResult(It.IsAny<Location[]>()), Times.Once());
            requestContext.Verify(m => m.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
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
            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, null);
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
            Assert.Throws<NullReferenceException>(() => peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.Table, fullObjectName, objectName, null));
        }
    }
}
