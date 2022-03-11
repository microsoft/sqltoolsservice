//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.AzureFunctions;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using NUnit.Framework;
using System;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.AzureFunctions
{
    class AzureFunctionsServiceTests
    {
        private string testAzureFunctionsFolder = Path.Combine("..", "..", "..", "AzureFunctions", "AzureFunctionTestFiles");

        /// <summary>
        /// Verify input binding gets added
        /// </summary>
        [Test]
        public void AddSqlInputBinding()
        {
            // copy the original file because the input binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.cs");
            string testFile = Path.Join(Path.GetTempPath(), $"InsertSqlInputBinding-{DateTime.Now.ToString("yyyy - dd - MM--HH - mm - ss")}.cs");
            File.Copy(originalFile, testFile, true);

            AddSqlBindingParams parameters = new AddSqlBindingParams
            {
                bindingType = BindingType.input,
                filePath = testFile,
                functionName = "GetArtists_get",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.True(result.Success);
            Assert.IsNull(result.ErrorMessage);

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsInputBinding.cs"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.AreEqual(expectedFileText, actualFileText);
        }

        /// <summary>
        /// Verify output binding gets added
        /// </summary>
        [Test]
        public void AddSqlOutputBinding()
        {
            // copy the original file because the output binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.cs");
            string testFile = Path.Join(Path.GetTempPath(), $"InsertSqlOutputBinding-{DateTime.Now.ToString("yyyy - dd - MM--HH - mm - ss")}.cs");
            File.Copy(originalFile, testFile, true);

            AddSqlBindingParams parameters = new AddSqlBindingParams
            {
                bindingType = BindingType.output,
                filePath = testFile,
                functionName = "NewArtist_post",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.True(result.Success);
            Assert.IsNull(result.ErrorMessage);

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsOutputBinding.cs"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.AreEqual(expectedFileText, actualFileText);
        }

        /// <summary>
        /// Verify output binding gets added for an async method
        /// </summary>
        [Test]
        public void AddSqlOutputBindingAsync()
        {
            // copy the original file because the output binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.cs");
            string testFile = Path.Join(Path.GetTempPath(), $"InsertSqlOutputBindingAsync-{DateTime.Now.ToString("yyyy - dd - MM--HH - mm - ss")}.cs");
            File.Copy(originalFile, testFile, true);

            AddSqlBindingParams parameters = new AddSqlBindingParams
            {
                bindingType = BindingType.output,
                filePath = testFile,
                functionName = "NewArtists_post",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.True(result.Success);
            Assert.IsNull(result.ErrorMessage);

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsOutputBindingAsync.cs"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.AreEqual(expectedFileText, actualFileText);
        }

        /// <summary>
        /// Verify what happens when specified azure function isn't found
        /// </summary>
        [Test]
        public void NoAzureFunctionForSqlBinding()
        {
            // copy the original file because the input binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.cs");
            string testFile = Path.Join(Path.GetTempPath(), $"NoAzureFunctionForSqlBinding-{DateTime.Now.ToString("yyyy - dd - MM--HH - mm - ss")}.cs");
            File.Copy(originalFile, testFile, true);

            AddSqlBindingParams parameters = new AddSqlBindingParams
            {
                bindingType = BindingType.input,
                filePath = testFile,
                functionName = "noExistingFunction",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(result.ErrorMessage.Equals(SR.CouldntFindAzureFunction("noExistingFunction", testFile)));
        }

        /// <summary>
        /// Verify what happens when there's more than one Azure function with the specified name in the file
        /// </summary>
        [Test]
        public void MoreThanOneAzureFunctionWithSpecifiedName()
        {
            // copy the original file because the input binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsMultipleSameFunction.cs");
            string testFile = Path.Join(Path.GetTempPath(), $"MoreThanOneAzureFunctionWithSpecifiedName-{DateTime.Now.ToString("yyyy - dd - MM--HH - mm - ss")}.cs");
            File.Copy(originalFile, testFile, true);

            AddSqlBindingParams parameters = new AddSqlBindingParams
            {
                bindingType = BindingType.input,
                filePath = testFile,
                functionName = "GetArtists_get",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            AddSqlBindingOperation operation = new AddSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(result.ErrorMessage.Equals(SR.MoreThanOneAzureFunctionWithName("GetArtists_get", testFile)));
        }

        /// <summary>
        /// Verify getting the names of Azure functions in a file
        /// </summary>
        [Test]
        public void GetAzureFunctions()
        {
            string testFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.cs");

            GetAzureFunctionsParams parameters = new GetAzureFunctionsParams
            {
                filePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.AreEqual(3, result.azureFunctions.Length);
            Assert.AreEqual(result.azureFunctions[0], "GetArtists_get");
            Assert.AreEqual(result.azureFunctions[1], "NewArtist_post");
            Assert.AreEqual(result.azureFunctions[2], "NewArtists_post");
        }

        /// <summary>
        /// Verify there are no errors when a file doesn't have any Azure functions
        /// </summary>
        [Test]
        public void GetAzureFunctionsWhenNoFunctions()
        {
            // make blank file
            string testFile = Path.Join(Path.GetTempPath(), $"NoAzureFunctions-{DateTime.Now.ToString("yyyy - dd - MM--HH - mm - ss")}.cs");
            FileStream fstream = File.Create(testFile);
            fstream.Close();
            GetAzureFunctionsParams parameters = new GetAzureFunctionsParams
            {
                filePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.AreEqual(0, result.azureFunctions.Length);
        }

        /// <summary>
        /// Verify there are no errors when a file doesn't have any Azure functions
        /// </summary>
        [Test]
        public void GetAzureFunctionsNet5()
        {
            string testFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNet5.cs");

            GetAzureFunctionsParams parameters = new GetAzureFunctionsParams
            {
                filePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);

            Exception ex = Assert.Throws<Exception>(() => { operation.GetAzureFunctions(); });
            Assert.AreEqual(SR.SqlBindingsNet5NotSupported, ex.Message);
        }
    }
}
