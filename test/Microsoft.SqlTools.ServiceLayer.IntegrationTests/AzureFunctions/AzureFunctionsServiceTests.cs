//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.AzureFunctions;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Extensions;
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

            Assert.That(result.Success, Is.True, "Operation should be successful");
            Assert.That(result.ErrorMessage, Is.Null, "There should be no errors");

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsInputBinding.cs"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.That(expectedFileText.NormalizeLineEndings(), Is.EqualTo(actualFileText.NormalizeLineEndings()));
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

            Assert.That(result.Success, Is.True, "Operation should be successful");
            Assert.That(result.ErrorMessage, Is.Null, "There should be no errors");

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsOutputBinding.cs"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.That(expectedFileText.NormalizeLineEndings(), Is.EqualTo(actualFileText.NormalizeLineEndings()));
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

            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsOutputBindingAsync.cs"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.That(actualFileText, Is.EqualTo(expectedFileText));
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

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.ErrorMessage, Is.EqualTo(SR.CouldntFindAzureFunction("noExistingFunction", testFile)));
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

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.ErrorMessage, Is.EqualTo(SR.MoreThanOneAzureFunctionWithName("GetArtists_get", testFile)));
        }

        /// <summary>
        /// Verify getting the names of Azure functions in a file
        /// </summary>
        [Test]
        public void GetAzureFunctionNames()
        {
            string testFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsName.cs");

            GetAzureFunctionsParams parameters = new GetAzureFunctionsParams
            {
                FilePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            Assert.That(result.AzureFunctions.Length, Is.EqualTo(2));
            Assert.That(result.AzureFunctions[0].Name, Is.EqualTo("WithName"));
            Assert.That(result.AzureFunctions[1].Name, Is.EqualTo("{interpolated}String"));
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
                FilePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            Assert.That(result.AzureFunctions.Length, Is.EqualTo(0));
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
                FilePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);

            Exception ex = Assert.Throws<Exception>(() => { operation.GetAzureFunctions(); });
            Assert.That(ex.Message, Is.EqualTo(SR.SqlBindingsNet5NotSupported));
        }

        /// <summary>
        /// Verify getting the routes of a HttpTriggerBinding on Azure Functions in a file
        /// </summary>
        [Test]
        public void GetAzureFunctionsRoute()
        {
            string testFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsRoute.cs");

            GetAzureFunctionsParams parameters = new GetAzureFunctionsParams
            {
                FilePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            Assert.That(result.AzureFunctions.Length, Is.EqualTo(9));
            Assert.That(result.AzureFunctions[0].HttpTriggerBinding!.Route, Is.EqualTo("withRoute"));
            Assert.That(result.AzureFunctions[1].HttpTriggerBinding!.Route, Is.EqualTo("{interpolated}String"));
            Assert.That(result.AzureFunctions[2].HttpTriggerBinding!.Route, Is.EqualTo("$withDollarSigns$"));
            Assert.That(result.AzureFunctions[3].HttpTriggerBinding!.Route, Is.EqualTo("withRouteNoSpaces"));
            Assert.That(result.AzureFunctions[4].HttpTriggerBinding!.Route, Is.EqualTo("withRouteExtraSpaces"));
            Assert.That(result.AzureFunctions[5].HttpTriggerBinding!.Route, Is.Null, "Route specified as null should be null");
            Assert.That(result.AzureFunctions[6].HttpTriggerBinding!.Route, Is.Null, "No route specified should be null");
            Assert.That(result.AzureFunctions[7].HttpTriggerBinding!.Route, Is.EqualTo(""));
            Assert.That(result.AzureFunctions[8].HttpTriggerBinding, Is.Null, "Should not be an HttpTriggerBinding");
        }

        /// <summary>
        /// Verify getting the operations of a HttpTriggerBinding on Azure Functions in a file
        /// </summary>
        [Test]
        public void GetAzureFunctionsOperations()
        {
            string testFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsOperations.cs");

            GetAzureFunctionsParams parameters = new GetAzureFunctionsParams
            {
                FilePath = testFile
            };

            GetAzureFunctionsOperation operation = new GetAzureFunctionsOperation(parameters);
            GetAzureFunctionsResult result = operation.GetAzureFunctions();

            Assert.That(result.AzureFunctions.Length, Is.EqualTo(5));
            Assert.That(result.AzureFunctions[0].HttpTriggerBinding!.Operations, Is.EqualTo(new string[] { "GET" }));
            Assert.That(result.AzureFunctions[1].HttpTriggerBinding!.Operations, Is.EqualTo(new string[] { "GET", "POST" }));
            Assert.That(result.AzureFunctions[2].HttpTriggerBinding!.Operations, Is.EqualTo(Array.Empty<string>()));
            Assert.That(result.AzureFunctions[3].HttpTriggerBinding!.Operations, Is.EqualTo(Array.Empty<string>()));
            Assert.That(result.AzureFunctions[4].HttpTriggerBinding, Is.Null, "Should not be an HttpTriggerBinding");
        }
    }
}
