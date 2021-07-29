using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions;
using Microsoft.SqlTools.ServiceLayer.AzureFunctions.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.AzureFunctions
{
    class AzureFunctionsServiceTests
    {
        private string testAzureFunctionsFolder = Path.Combine("..", "..", "..", "AzureFunctions", "AzureFunctionTestFiles");

        /// <summary>
        /// Verify input binding gets added
        /// </summary>
        [Test]
        public async Task InsertSqlInputBinding()
        {
            // copy the original file because the input binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.ts");
            string testFile = Path.Join(Path.GetTempPath(), string.Format("InsertSqlInputBinding-{0}.ts", DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")));
            File.Copy(originalFile, testFile, true);

            InsertSqlBindingParams parameters = new InsertSqlBindingParams
            {
                bindingType = BindingType.input,
                filePath = testFile,
                functionName = "GetArtists_get",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            InsertSqlBindingOperation operation = new InsertSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.True(result.Success);
            Assert.IsNull(result.ErrorMessage);

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsInputBinding.ts"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.AreEqual(expectedFileText, actualFileText);
        }

        /// <summary>
        /// Verify output binding gets added
        /// </summary>
        [Test]
        public async Task InsertSqlOutputBinding()
        {
            // copy the original file because the output binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.ts");
            string testFile = Path.Join(Path.GetTempPath(), string.Format("InsertSqlOutputBinding-{0}.ts", DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")));
            File.Copy(originalFile, testFile, true);

            InsertSqlBindingParams parameters = new InsertSqlBindingParams
            {
                bindingType = BindingType.output,
                filePath = testFile,
                functionName = "NewArtist_post",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            InsertSqlBindingOperation operation = new InsertSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.True(result.Success);
            Assert.IsNull(result.ErrorMessage);

            string expectedFileText = File.ReadAllText(Path.Join(testAzureFunctionsFolder, "AzureFunctionsOutputBinding.ts"));
            string actualFileText = File.ReadAllText(testFile);
            Assert.AreEqual(expectedFileText, actualFileText);
        }

        /// <summary>
        /// Verify what happens when specified azure function isn't found
        /// </summary>
        [Test]
        public async Task NoAzureFunctionForSqlBinding()
        {
            // copy the original file because the input binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsNoBindings.ts");
            string testFile = Path.Join(Path.GetTempPath(), string.Format("NoAzureFunctionForSqlBinding-{0}.ts", DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")));
            File.Copy(originalFile, testFile, true);

            InsertSqlBindingParams parameters = new InsertSqlBindingParams
            {
                bindingType = BindingType.input,
                filePath = testFile,
                functionName = "noExistingFunction",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            InsertSqlBindingOperation operation = new InsertSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(result.ErrorMessage.Equals($"Couldn't find Azure function with FunctionName noExistingFunction in {testFile}"));
        }

        /// <summary>
        /// Verify what happens when there's more than one Azure function with the specified name in the file
        /// </summary>
        [Test]
        public async Task MoreThanOneAzureFunctionWithSpecifiedName()
        {
            // copy the original file because the input binding will be inserted into the file
            string originalFile = Path.Join(testAzureFunctionsFolder, "AzureFunctionsMultipleSameFunction.ts");
            string testFile = Path.Join(Path.GetTempPath(), string.Format("MoreThanOneAzureFunctionWithSpecifiedName-{0}.ts", DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")));
            File.Copy(originalFile, testFile, true);

            InsertSqlBindingParams parameters = new InsertSqlBindingParams
            {
                bindingType = BindingType.input,
                filePath = testFile,
                functionName = "GetArtists_get",
                objectName = "[dbo].[table1]",
                connectionStringSetting = "SqlConnectionString"
            };

            InsertSqlBindingOperation operation = new InsertSqlBindingOperation(parameters);
            ResultStatus result = operation.AddBinding();

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(result.ErrorMessage.Equals($"More than one Azure function found with the FunctionName GetArtists_get in {testFile}"));
        }
    }
}
